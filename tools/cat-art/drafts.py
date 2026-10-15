"""The five first-draft cats (flat colours, dark outline, accessories) in all 34 app poses.
Uses posekit's poses with each cat's own hand-drawn head and proportions."""
import math, sys
from PIL import Image, ImageDraw, ImageFont
import posekit
from real import rgb, shade

W, H = posekit.W, posekit.H

def front(eyes_open, eyes_closed, eyes_up, top, mid, mouth_rows, yawn_rows, neck):
    """A front-facing head: top rows, then an eye pair of rows, then the muzzle rows."""
    return dict(neck=neck, rows={
        'open': top + eyes_open + mid + mouth_rows,
        'closed': top + eyes_closed + mid + mouth_rows,
        'up': top + eyes_up + mid + mouth_rows,
        'yawn': top + eyes_closed + mid + yawn_rows,
    })

# Bean: round ginger face, two big eyes, blush, white muzzle
posekit.HEADS['bean'] = front(
    ["HwYHHHHHwYH", "HYYHHHHHYYH"], ["HHHHHHHHHHH", "HxxHHHHHxxH"], ["HYYHHHHHYYH", "HwwHHHHHwwH"],
    [".E.......E.", "EeEHSHSHEeE", "HHHHHHHHHHH"], [],
    ["HpHHMnMHHpH", "HHHMMMMMHHH", ".HHHHHHHHH."],
    ["HpHHMnMHHpH", "HHHMmmmMHHH", ".HHHmooHHH."], neck=(5, 7))
# Puff: cream ruff around a dark face mask, blue eyes
posekit.HEADS['puff'] = front(
    ["HGwYGGGwYGH", "HGYYGGGYYGH"], ["HGGGGGGGGGH", "HGxxGGGxxGH"], ["HGYYGGGYYGH", "HGwwGGGwwGH"],
    [".E.......E.", "EeEHHHHHEeE", "HGGGGGGGGGH"], [],
    ["HHGGGnGGGHH", ".HHGGGGGHH.", "..HHHHHHH.."],
    ["HHGGGnGGGHH", ".HHGmmmGHH.", "..HHmooHH.."], neck=(5, 7))
# Pebble: black face with a white blaze, green eyes
posekit.HEADS['pebble'] = front(
    ["HwYHGGGHwYH", "HYYHGGGHYYH"], ["HHHHGGGHHHH", "HxxHGGGHxxH"], ["HYYHGGGHYYH", "HwwHGGGHwwH"],
    [".E.......E.", "EeEHHHHHEeE", "HHHHHGHHHHH"], [],
    ["HHHGGnGGHHH", "HHHGGGGGHHH", ".HHHHHHHHH."],
    ["HHHGGnGGHHH", "HHHGmmmGHHH", ".HHHmooHHH."], neck=(5, 7))
# Sprout: calico kitten face, orange left half, white right half, a leaf on top
posekit.HEADS['sprout'] = front(
    ["HwYHGGGGwYG", "HYYGGGGGYYG"], ["HHHHGGGGGGG", "HxxGGGGGxxG"], ["HYYHGGGGYYG", "HwwGGGGGwwG"],
    [".....vV....", "......V....", ".E.......G.", "EeEHHGGGGeG", "HHHHHGGGGGG"], [],
    ["GpGGMnMGGpG", "GGGMMMMMGGG", ".GGGGGGGGG."],
    ["GpGGMnMGGpG", "GGGMmmmMGGG", ".GGGmooGGG."], neck=(5, 9))
# Wisp: narrow profile head with tall ears and amber eyes
posekit.HEADS['wisp'] = dict(neck=(3, 7), rows={
    'open':   ["..E...E...", ".EeE.EeE..", ".HSHHHHH..", "HHHHHHHHH.", "HHHHHHYYH.", "HHHHHHYKHH", "HHHHHHHHHn", ".HHHHHHHH."],
    'closed': ["..E...E...", ".EeE.EeE..", ".HSHHHHH..", "HHHHHHHHH.", "HHHHHHHHH.", "HHHHHHxxHH", "HHHHHHHHHn", ".HHHHHHHH."],
    'up':     ["..E...E...", ".EeE.EeE..", ".HSHHHHH..", "HHHHHHYKH.", "HHHHHHYYH.", "HHHHHHHHHH", "HHHHHHHHHn", ".HHHHHHHH."],
    'yawn':   ["..E...E...", ".EeE.EeE..", ".HSHHHHH..", "HHHHHHHHH.", "HHHHHHHHH.", "HHHHHHxxHH", "HHHHHHmmon", ".HHHHHmoo.", "..HHHHH..."],
})

BANDANA = (["uUuuuuuUu", ".uuuuUuu.", "..uuuUu..", "...uu...."], (8, 1))
CHEST_TUFT = None

class Flat:
    def __init__(self, k, base, dark, light, white, eye, nose='#e86a7a', inner='#f39aa8', point=None,
                 orange=None, black=None, extra=None):
        self.k, self.base, self.dark, self.light, self.white, self.eye = map(rgb, (k, base, dark, light, white, eye))
        self.nose, self.inner = rgb(nose), rgb(inner)
        self.point = rgb(point) if point else None
        self.orange = rgb(orange) if orange else None
        self.black = rgb(black) if black else None
        self.extra = {ch: rgb(v) for ch, v in (extra or {}).items()}

def noise(x, y, seed=0):
    def h(i, j): return (math.sin(i * 127.1 + j * 311.7 + seed * 74.7) * 43758.5453) % 1
    x0, y0 = math.floor(x), math.floor(y); fx, fy = x - x0, y - y0
    fx, fy = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy)
    a, b, c, d = h(x0, y0), h(x0 + 1, y0), h(x0, y0 + 1), h(x0 + 1, y0 + 1)
    return a + (b - a) * fx + (c - a) * fy + (a - b - c + d) * fx * fy

def fur(name, coat, reg, r, c, paw, top):
    if name == 'Bean':
        if reg in 'CM' or (reg in 'LN' and paw): return coat.white
        if reg in 'BJ' and top and c % 3 == 0: return coat.dark
        if reg in 'BJ' and r <= 11 and c % 3 == 0: return coat.dark
        if reg == 'T' and r % 2 == 0: return coat.dark
        if reg == 'S': return coat.dark
        return coat.base
    if name == 'Wisp':
        if reg == 'C' and c >= 17 and r <= 12 and (r + c) % 2 == 0: return coat.white
        if reg == 'S': return coat.light
        return coat.base
    if name == 'Puff':
        if reg in 'ETG' or (reg in 'LN' and paw) or reg == 'M': return coat.point
        return coat.base
    if name == 'Pebble':
        if reg in 'CMG' or (reg in 'LN' and paw): return coat.white
        return coat.base
    if name == 'Sprout':
        if reg in 'CMGL': return coat.white
        if reg in 'HE': return coat.orange
        n = noise(c / 4.0, r / 3.0, 3)
        if reg in 'BJTN' and n > 0.6: return coat.orange
        if reg in 'BJT' and n < 0.24: return coat.black
        return coat.white
    return coat.base

def render(name, g, coat):
    pw = set()
    for c in range(W):
        rows = [r for r in range(H) if g[r][c] in 'LN']
        if rows: pw |= {(r, c) for r in rows if r >= max(rows) - 1}
    fixed = {'Y': coat.eye, 'K': rgb('#1a1420'), 'w': rgb('#ffffff'), 'n': coat.nose, 'e': coat.inner,
             'p': coat.inner, 'x': coat.k, 'm': rgb('#5a2a30'), 'o': rgb('#e8828e'),
             'h': rgb('#e5484d'), 'z': rgb('#7fb2f0'), 'u': rgb('#2fb3a8'), 'U': rgb('#1e8078'),
             'v': rgb('#6cc060'), 'V': rgb('#3f8a3c')}
    if coat.eye == rgb('#3a2420'): fixed['K'] = coat.eye
    img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    col = [[None] * W for _ in range(H)]
    for r in range(H):
        for c in range(W):
            reg = g[r][c]
            if reg == '.': continue
            if reg in fixed: col[r][c] = fixed[reg]; continue
            top = r == 0 or g[r - 1][c] == '.'
            base = fur(name, coat, reg, r, c, (r, c) in pw, top)
            if reg in 'NF': base = shade(base, 0.78)
            elif top and reg in 'BH' and base == coat.base: base = coat.light
            col[r][c] = base
    for r in range(H):
        for c in range(W):
            if g[r][c] != '.': continue
            for dr, dc in ((1, 0), (0, -1), (0, 1), (-1, 0)):
                rr, cc = r + dr, c + dc
                if 0 <= rr < H and 0 <= cc < W and col[rr][cc] is not None and g[rr][cc] != 'z':
                    img.putpixel((c, r), coat.k + (255,)); break
    for r in range(H):
        for c in range(W):
            if col[r][c] is not None: img.putpixel((c, r), col[r][c] + (255,))
    return img

def fluffy(g):
    src = [row[:] for row in g]; g = [row[:] for row in g]
    for r in range(1, H - 1):
        for c in range(1, W - 1):
            if src[r][c] == 'T':
                for dr, dc in ((-1, 0), (0, -1)):
                    if src[r + dr][c + dc] == '.': g[r + dr][c + dc] = 'T'
    return g

DRAFTS = [
    ('Bean', 'Chunky ginger, round face, white socks', dict(kind='adult', head='bean', chunk=1.3, legw=1.2),
     Flat('#3a2420', '#f0a050', '#d27a34', '#f8c890', '#fbf3e6', '#3a2420')),
    ('Wisp', 'Lanky black cat, tall ears, amber eyes', dict(kind='adult', head='wisp', chunk=0.85, legw=0.8),
     Flat('#100e18', '#3a3850', '#2a2838', '#5a5878', '#e8e6f2', '#f2c84a', nose='#c47a92', inner='#c47a92')),
    ('Puff', 'Fluffy colorpoint, dark mask, blue eyes', dict(kind='adult', head='puff', chunk=1.15),
     Flat('#4a3628', '#f4e8d4', '#e2ceb0', '#fffaf0', '#f4e8d4', '#6ab0f0', nose='#4a3628', inner='#e8a0a8', point='#8a6448')),
    ('Pebble', 'Tuxedo with a white blaze and teal bandana', dict(kind='adult', head='pebble', accessory=BANDANA),
     Flat('#14141c', '#2e3040', '#2e3040', '#4a4e64', '#f4f4f8', '#8ad06a', nose='#f2a0b0', inner='#f2a0b0')),
    ('Sprout', 'Calico kitten with a leaf sprout', dict(kind='kitten', head='sprout'),
     Flat('#2c2420', '#fbf6ee', '#e4dcd0', '#ffffff', '#fbf6ee', '#2c2420', nose='#ee7f92', inner='#f4a0b0',
          orange='#ec9548', black='#3c3434')),
]

def frames_for(name, kw, coat):
    out = {}
    for pose in posekit.POSES:
        g = posekit.pose_grid(pose, **kw)
        if name == 'Puff': g = fluffy(g)
        out[pose] = render(name, g, coat)
    return out

if __name__ == '__main__':
    import final
    out = sys.argv[1]
    bg = (232, 236, 244, 255)
    font = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf', 20)
    small = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf', 13)
    S, cols = 5, 7
    names = list(posekit.POSES)
    allf = {}
    for name, desc, kw, coat in DRAFTS:
        fr = frames_for(name, kw, coat); allf[name] = fr
        cw, ch = W * S + 12, H * S + 26
        rows = math.ceil(len(names) / cols)
        sheet = Image.new('RGBA', (16 + cols * cw, 56 + rows * ch), bg)
        d = ImageDraw.Draw(sheet)
        d.text((16, 12), f'{name} (first draft): {desc}', fill=(40, 36, 52, 255), font=font)
        for i, p in enumerate(names):
            x, y = 16 + (i % cols) * cw, 52 + (i // cols) * ch
            sheet.alpha_composite(fr[p].resize((W * S, H * S), Image.NEAREST), (x, y))
            d.text((x, y + H * S + 4), p, fill=(90, 90, 110, 255), font=small)
        sheet.save(f'{out}/{name.lower()}-draft-poses.png')
    Z = 4; gif = []
    for pose, hold in final.SHOW:
        fr = Image.new('RGBA', (len(DRAFTS) * (W + 4) * Z, (H + 4) * Z), bg)
        for i, (name, *_) in enumerate(DRAFTS):
            fr.alpha_composite(allf[name][pose].resize((W * Z, H * Z), Image.NEAREST), (8 + i * (W + 4) * Z, 8))
        gif.append((fr.convert('RGB'), hold * 200))
    gif[0][0].save(f'{out}/all-drafts-poses.gif', save_all=True, append_images=[g for g, _ in gif[1:]],
                   duration=[t for _, t in gif], loop=0)
