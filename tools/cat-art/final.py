"""Colours the pose kit with each cat's coat and renders pose sheets and animated previews."""
import math, sys
from PIL import Image, ImageDraw, ImageFont
import posekit
from real import rgb, shade

W, H = posekit.W, posekit.H

def noise(x, y, seed=0):
    """Smooth value noise, for calico patches."""
    def h(i, j): return (math.sin(i * 127.1 + j * 311.7 + seed * 74.7) * 43758.5453) % 1
    x0, y0 = math.floor(x), math.floor(y); fx, fy = x - x0, y - y0
    fx, fy = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy)
    a, b, c, d = h(x0, y0), h(x0 + 1, y0), h(x0, y0 + 1), h(x0 + 1, y0 + 1)
    return a + (b - a) * fx + (c - a) * fy + (a - b - c + d) * fx * fy

class Coat:
    def __init__(self, base, eye, nose, inner, white='#f1ede6'):
        self.base, self.eye, self.nose, self.inner, self.white = map(rgb, (base, eye, nose, inner, white))
    def fur(self, reg, r, c, paw): return self.base

class Tabby(Coat):
    def __init__(self, base, stripe, **kw): super().__init__(base, **kw); self.stripe = rgb(stripe)
    def fur(self, reg, r, c, paw):
        if reg in 'CM' or (reg in 'LN' and paw): return self.white
        if reg in 'BJ' and c % 4 == 0: return self.stripe
        if reg == 'T' and (r + c) % 3 == 0: return self.stripe
        if reg in 'LN' and r % 3 == 0: return self.stripe
        if reg == 'H' and c % 3 == 0: return self.stripe
        return self.base

class Tuxedo(Coat):
    def fur(self, reg, r, c, paw):
        if reg in 'CM' or (reg in 'LN' and paw): return self.white
        return self.base

class Point(Coat):
    def __init__(self, base, point, **kw): super().__init__(base, **kw); self.point = rgb(point)
    def fur(self, reg, r, c, paw):
        if reg in 'EFTMG' or (reg in 'LN' and paw): return self.point
        if reg in 'LN': return shade(self.base, 0.9)
        return self.base

class Calico(Coat):
    def __init__(self, orange, black, **kw): super().__init__(**kw); self.orange, self.black = rgb(orange), rgb(black)
    def fur(self, reg, r, c, paw):
        if reg in 'CMGL': return self.base
        if reg in 'HEF': return self.orange
        n = noise(c / 4.0, r / 3.0, 3)
        if reg in 'BJTN' and n > 0.62: return self.orange
        if reg in 'BJT' and n < 0.22: return self.black
        return self.base

CATS = [
    ('Bean', 'Ginger tabby with white paws', 'adult', False,
     Tabby('#d9894a', '#a95e2a', eye='#c9b23c', nose='#d98a8a', inner='#d79a9a')),
    ('Wisp', 'Black shorthair with amber eyes', 'adult', False,
     Coat('#2d2b36', eye='#e0a832', nose='#3c3640', inner='#5a4450')),
    ('Puff', 'Long-haired colorpoint with blue eyes', 'adult', True,
     Point('#ecdfc8', '#6f5242', eye='#6fa8e8', nose='#5a4038', inner='#c99a96')),
    ('Pebble', 'Black and white tuxedo with green eyes', 'adult', False,
     Tuxedo('#25252d', eye='#98c45a', nose='#d89aa2', inner='#8a6a74')),
    ('Sprout', 'Calico kitten with blue-grey eyes', 'kitten', False,
     Calico('#d98a45', '#35302e', base='#f3eee6', eye='#7aa6c8', nose='#e39aa6', inner='#e3a6ae')),
]

FUR = set('HGEFBCJLNTM')

def fluffy(g):
    """Long fur: a soft fringe under the belly and a fuller tail."""
    src = [row[:] for row in g]; g = [row[:] for row in g]
    for r in range(1, H - 1):
        for c in range(1, W - 1):
            ch = src[r][c]
            if ch in 'BC' and src[r + 1][c] == '.' and c % 3 == 0:
                g[r + 1][c] = ch
            if ch == 'T':
                for dr, dc in ((-1, 0), (0, -1)):
                    if src[r + dr][c + dc] == '.':
                        g[r + dr][c + dc] = 'T'
    return g

def paws(g):
    """The bottom two pixels of every leg column."""
    out = set()
    for c in range(W):
        rows = [r for r in range(H) if g[r][c] in 'LN']
        if rows:
            out |= {(r, c) for r in rows if r >= max(rows) - 1}
    return out

def render(g, coat):
    pw = paws(g)
    col = [[None] * W for _ in range(H)]
    fixed = {'Y': coat.eye, 'K': shade(coat.eye, 0.22), 'n': coat.nose, 'e': coat.inner,
             'x': shade(coat.base, 0.35), 'm': rgb('#5a2a30'), 'o': rgb('#e8828e'),
             'h': rgb('#e5484d'), 'z': rgb('#7fb2f0')}
    for r in range(H):
        for c in range(W):
            reg = g[r][c]
            if reg == '.': continue
            if reg in fixed: col[r][c] = fixed[reg]; continue
            base = coat.fur(reg, r, c, (r, c) in pw)
            above = g[r - 1][c] if r > 0 else '.'
            below = g[r + 1][c] if r < H - 1 else '.'
            f = 1.0
            if above in '.hz': f = 1.2
            elif below == '.': f = 0.8
            elif r + 2 < H and g[r + 2][c] == '.' and reg in 'BCJ': f = 0.9
            if reg in 'NF': f *= 0.72
            if reg == 'J' and above not in 'J.': f = max(f, 1.12)
            if reg == 'L' and above not in 'L.' and f == 1.0: f = 1.08
            if below in 'Yx': f *= 0.75
            col[r][c] = shade(base, f, 0.0 if f >= 1 else -0.01)
    img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    for r in range(H):
        for c in range(W):
            if g[r][c] != '.': continue
            for dr, dc in ((1, 0), (0, -1), (0, 1), (-1, 0)):
                rr, cc = r + dr, c + dc
                if 0 <= rr < H and 0 <= cc < W and col[rr][cc] is not None and g[rr][cc] != 'z':
                    img.putpixel((c, r), shade(col[rr][cc], 0.42, -0.02) + (255,)); break
    for r in range(H):
        for c in range(W):
            if col[r][c] is not None: img.putpixel((c, r), col[r][c] + (255,))
    return img

def frames_for(kind, fluff, coat):
    out = {}
    for name in posekit.POSES:
        g = posekit.pose_grid(name, kind)
        if fluff: g = fluffy(g)
        out[name] = render(g, coat)
    return out

# The order the app plays things in, for the preview GIFs: (pose, frames to hold at 0.2 s each)
SHOW = ([('WalkA', 1), ('WalkB', 1)] * 4 + [('Sit', 4), ('SitBlink', 1), ('Sit', 3), ('SitTail', 2), ('Sit', 2)]
        + [('GroomA', 2), ('GroomB', 2)] * 2 + [('LookUp', 4), ('Bat', 3), ('Sit', 2)]
        + [('CrouchA', 1), ('CrouchB', 1)] * 3 + [('Leap', 3), ('Land', 3)]
        + [('RunA', 1), ('RunB', 1)] * 4 + [('Loaf', 5), ('LoafBlink', 2), ('Loaf', 3)]
        + [('NapA', 4), ('NapB', 4)] * 2 + [('Curl', 6), ('Drowsy', 4), ('Stretch', 5), ('Yawn', 4)]
        + [('PettedA', 1), ('PettedB', 1)] * 3 + [('BellyA', 2), ('BellyB', 2)] * 2 + [('Startle', 5)]
        + [('ScratchA', 1), ('ScratchB', 1)] * 4 + [('Dangle', 4), ('HeldBelly', 4), ('FallA', 1), ('FallB', 1)] * 1)

if __name__ == '__main__':
    out = sys.argv[1]
    bg = (232, 236, 244, 255)
    font = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf', 20)
    small = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf', 13)
    S, cols = 5, 7
    names = list(posekit.POSES)
    all_frames = {}
    for name, desc, kind, fluff, coat in CATS:
        fr = frames_for(kind, fluff, coat); all_frames[name] = fr
        cw, ch = W * S + 12, H * S + 26
        rows = math.ceil(len(names) / cols)
        sheet = Image.new('RGBA', (16 + cols * cw, 56 + rows * ch), bg)
        d = ImageDraw.Draw(sheet)
        d.text((16, 12), f'{name}: {desc}', fill=(40, 36, 52, 255), font=font)
        for i, p in enumerate(names):
            x, y = 16 + (i % cols) * cw, 52 + (i // cols) * ch
            sheet.alpha_composite(fr[p].resize((W * S, H * S), Image.NEAREST), (x, y))
            d.text((x, y + H * S + 4), p, fill=(90, 90, 110, 255), font=small)
        sheet.save(f'{out}/{name.lower()}-poses.png')
    # one GIF with all five cats acting out the same day
    Z = 4
    gif = []
    for pose, hold in SHOW:
        fr = Image.new('RGBA', (len(CATS) * (W + 4) * Z, (H + 4) * Z), bg)
        for i, (name, *_ ) in enumerate(CATS):
            fr.alpha_composite(all_frames[name][pose].resize((W * Z, H * Z), Image.NEAREST), (8 + i * (W + 4) * Z, 8))
        gif.append((fr.convert('RGB'), hold * 200))
    gif[0][0].save(f'{out}/all-cats-poses.gif', save_all=True, append_images=[g for g, _ in gif[1:]],
                   duration=[t for _, t in gif], loop=0)
