"""Renders the animated GIFs for the project website (site/assets) from the app's own pixel art.

It reads the frames straight out of the C# files the app ships (CatPixels.cs for the original cat,
CatBreedArt.cs for the other ten), so the website always shows exactly the cats in the app.

    python3 site_art.py            # writes ../../site/assets
"""
import os, re, sys
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
CORE = os.path.join(HERE, '..', '..', 'src', 'DesktopBuddy.Core')
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, '..', '..', 'site', 'assets')
W, H = 30, 20
FRAME_MS = 200

ROW = re.compile(r'"([^"]{%d})"' % W)
POSE = re.compile(r'\[CatPose\.(\w+)\]\s*=\s*new\[\]\s*\{(.*?)\}', re.S)
COLOR = re.compile(r"\['(.)'\]\s*=\s*0x([0-9A-Fa-f]{8})")

def argb(hex8):
    v = int(hex8, 16)
    return ((v >> 16) & 255, (v >> 8) & 255, v & 255, (v >> 24) & 255)

def poses(text):
    out = {}
    for name, body in POSE.findall(text):
        rows = ROW.findall(body)
        if len(rows) == H: out[name] = rows
    return out

def original_cat():
    """The original cat in its default look: a blue-grey tabby (CatPixels.ColorsFor(default))."""
    src = open(os.path.join(CORE, 'CatPixels.cs'), encoding='utf-8').read()
    fur, dark, light = '#8E9AB4', '#6A7492', '#B9C3DA'
    hx = lambda s: (int(s[1:3], 16), int(s[3:5], 16), int(s[5:7], 16), 255)
    pal = {'k': '#2A2433', 'f': fur, 'd': dark, 't': dark, 's': fur, 'a': fur, 'b': dark, 'l': light,
           'c': '#F4EADD', 'p': '#F2A0B2', 'g': '#6CD09A', 'w': '#FFFFFF', 'r': '#E0564F', 'y': '#F5C542',
           'h': '#E5484D', 'z': '#7FB2F0', 'q': '#8FD0F5'}
    frames = src[src.index('Dictionary<CatPose, string[]> Frames'):]
    return {k: hx(v) for k, v in pal.items()}, poses(frames)

def breed_cats():
    src = open(os.path.join(CORE, 'CatBreedArt.cs'), encoding='utf-8').read()
    parts = re.split(r'\[CatBreed\.(\w+)\]\s*=\s*new\(', src)[1:]
    cats = {}
    for name, body in zip(parts[::2], parts[1::2]):
        pal_end = body.index('new Dictionary<CatPose')
        cats[name] = ({k: argb(v) for k, v in COLOR.findall(body[:pal_end])}, poses(body[pal_end:]))
    return cats

def sprite(cat, pose, z):
    pal, fr = cat
    img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    for r, row in enumerate(fr[pose]):
        for c, ch in enumerate(row):
            if ch in pal: img.putpixel((c, r), pal[ch])
    return img.resize((W * z, H * z), Image.NEAREST)

def save_gif(path, frames):
    """frames: list of (RGBA image, ms). Fully transparent pixels stay transparent."""
    pal_frames = []
    for img, _ in frames:
        alpha = img.getchannel('A')
        p = img.convert('RGB').quantize(colors=255, method=Image.Quantize.FASTOCTREE, dither=Image.Dither.NONE)
        p.paste(255, mask=Image.eval(alpha, lambda a: 255 if a < 128 else 0))
        pal_frames.append(p)
    pal_frames[0].save(path, save_all=True, append_images=pal_frames[1:], duration=[ms for _, ms in frames],
                       loop=0, transparency=255, disposal=2, optimize=False)

def seq(*steps):
    """steps: (pose, frames to hold). Expands to a flat list of poses, one per 0.2 s."""
    return [p for pose, n in steps for p in [pose] * n]

def loop(cat, poses_, z=4):
    return [(sprite(cat, p, z), FRAME_MS) for p in poses_]

# ---- the hero: a cat living on a Windows-style taskbar ----

def taskbar_scene(cat, z=4, width=760):
    bar_h, sky_h = 12 * z, H * z + 6 * z
    hgt = sky_h + bar_h
    def backdrop():
        img = Image.new('RGBA', (width, hgt), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        top = sky_h
        d.rectangle([0, top, width, hgt], fill=(32, 34, 46, 255))
        d.line([0, top, width, top], fill=(70, 74, 96, 255), width=z // 2)
        # centred app icons, like the Windows 11 taskbar
        icons = [(76, 150, 240), (240, 190, 70), (110, 200, 140), (230, 110, 120), (170, 130, 230)]
        size, gap = 6 * z, 3 * z
        x0 = width // 2 - (len(icons) * (size + gap) - gap) // 2
        for i, colr in enumerate(icons):
            x = x0 + i * (size + gap); y = top + (bar_h - size) // 2
            d.rectangle([x, y, x + size - 1, y + size - 1], fill=colr + (255,))
            if i == 0:  # start button: four squares
                h = size // 2
                d.rectangle([x + h - z // 2, y, x + h + z // 2 - 1, y + size - 1], fill=(32, 34, 46, 255))
                d.rectangle([x, y + h - z // 2, x + size - 1, y + h + z // 2 - 1], fill=(32, 34, 46, 255))
        # clock on the right
        for i in range(4):
            d.rectangle([width - 22 * z + i * 4 * z, top + 4 * z, width - 22 * z + i * 4 * z + 2 * z, top + 8 * z],
                        fill=(200, 205, 220, 255))
        return img
    bg = backdrop()
    ground = sky_h - H * z  # the cat's paws touch the top of the taskbar
    frames = []
    def add(pose, x, n=1):
        for _ in range(n):
            f = bg.copy(); f.alpha_composite(sprite(cat, pose, z), (int(x), ground)); frames.append((f, FRAME_MS))
    step = 2.5 * z
    x = -W * z
    walk = ['WalkA', 'WalkB']
    i = 0
    while x < width * 0.32:
        add(walk[i % 2], x); x += step; i += 1
    for pose, n in [('Sit', 6), ('SitBlink', 1), ('Sit', 4), ('SitTail', 3), ('Sit', 2),
                    ('GroomA', 2), ('GroomB', 2), ('GroomA', 2), ('GroomB', 2), ('Sit', 3),
                    ('Yawn', 4), ('Loaf', 4), ('LoafBlink', 1), ('Loaf', 3)]:
        add(pose, x, n)
    for _ in range(4): add('NapA', x, 4); add('NapB', x, 4)
    for pose, n in [('Stretch', 5), ('Sit', 3), ('LookUp', 4), ('CrouchA', 1), ('CrouchB', 1),
                    ('CrouchA', 1), ('CrouchB', 1)]:
        add(pose, x, n)
    add('Leap', x, 1); x += 6 * z; add('Leap', x, 1); x += 6 * z; add('Land', x, 3)
    run = ['RunA', 'RunB', 'RunC', 'RunD']
    i = 0
    while x < width:
        add(run[i % 4], x); x += 5 * z; i += 1
    add('WalkA', width, 3)  # a beat with the bar empty before it loops
    return frames

def main():
    os.makedirs(os.path.join(OUT, 'cats'), exist_ok=True)
    os.makedirs(os.path.join(OUT, 'moves'), exist_ok=True)
    original = original_cat()
    cats = {'Original': original, **breed_cats()}

    save_gif(os.path.join(OUT, 'hero.gif'), taskbar_scene(original))

    # one tile per cat: walk a little, sit, blink
    tile = seq(('WalkA', 1), ('WalkB', 1), ('WalkA', 1), ('WalkB', 1), ('WalkA', 1), ('WalkB', 1),
               ('Sit', 6), ('SitBlink', 1), ('Sit', 4))
    for name, cat in cats.items():
        save_gif(os.path.join(OUT, 'cats', f'{name.lower()}.gif'), loop(cat, tile))

    moves = {
        'nap': seq(('NapA', 4), ('NapB', 4)),
        'pounce': seq(('LookUp', 4), ('CrouchA', 1), ('CrouchB', 1), ('CrouchA', 1), ('CrouchB', 1), ('Leap', 3), ('Land', 3), ('Sit', 3)),
        'petted': seq(('PettedA', 1), ('PettedB', 1)),
        'belly': seq(('BellyA', 2), ('BellyB', 2)),
        'scratch': seq(('ScratchA', 1), ('ScratchB', 1)),
        'run': seq(('RunA', 1), ('RunB', 1), ('RunC', 1), ('RunD', 1)),
        'groom': seq(('GroomA', 2), ('GroomB', 2), ('Sit', 3), ('Bat', 3), ('Sit', 2)),
        'woken': seq(('NapA', 4), ('NapB', 4), ('Startle', 4), ('Yawn', 4), ('Sit', 3)),
    }
    for name, poses_ in moves.items():
        save_gif(os.path.join(OUT, 'moves', f'{name}.gif'), loop(original, poses_))

    # a still for link previews (Open Graph), on an opaque background
    og = Image.new('RGBA', (1200, 630), (236, 240, 248, 255))
    d = ImageDraw.Draw(og)
    d.rectangle([0, 470, 1200, 630], fill=(32, 34, 46, 255))
    og.alpha_composite(sprite(original, 'Sit', 14), (600 - W * 7, 470 - H * 14))
    og.convert('RGB').save(os.path.join(OUT, 'og.png'))
    # favicon: the sitting cat's head
    icon = sprite(original, 'Sit', 1).crop((11, 0, 27, 16)).resize((64, 64), Image.NEAREST)
    icon.save(os.path.join(OUT, 'favicon.png'))

if __name__ == '__main__':
    main()
