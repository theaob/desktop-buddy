"""Four original dog concepts in every app pose: per-dog pose sheets, a side-by-side preview GIF of the
whole pose set, and a walk + gallop GIF. Run from this folder: python3 dogs.py <outdir>"""
import math, os, sys
from PIL import Image, ImageDraw, ImageFont
import dogkit
import final
from final import Coat, noise
from real import rgb, shade

W, H = dogkit.W, dogkit.H

class Solid(Coat):
    """One colour all over, a lighter muzzle and chest, darker ears."""
    def __init__(self, base, light, ear, **kw):
        super().__init__(base, **kw); self.light, self.ear = rgb(light), rgb(ear)
    def fur(self, reg, r, c, paw):
        if reg in 'GM': return self.light
        if reg == 'C': return shade(self.base, 1.08)
        if reg in 'EF': return self.ear
        return self.base

class Mantle(Coat):
    """Dark mantle over the back and head top, light face, chest, legs and paws (a sled-dog pattern)."""
    def __init__(self, base, light, **kw):
        super().__init__(base, **kw); self.light = rgb(light)
    def fur(self, reg, r, c, paw):
        if reg in 'GMC' or reg in 'LN': return self.light
        return self.base

class Saddle(Coat):
    """Tan dog with a dark saddle, dark ears and a dark tail; light beard."""
    def __init__(self, tan, dark, beard, **kw):
        super().__init__(tan, **kw); self.dark, self.beard = rgb(dark), rgb(beard)
    def fur(self, reg, r, c, paw):
        if reg in 'GM': return self.beard
        if reg in 'BEFT': return self.dark
        return self.base

class Spotted(Coat):
    """White with small dark spots, one dark ear."""
    def __init__(self, base, spot, **kw):
        super().__init__(base, **kw); self.spot = rgb(spot)
    def fur(self, reg, r, c, paw):
        if reg == 'E': return self.spot
        if reg in 'BJTHLN' and not paw and noise(c / 1.6, r / 1.6, 7) > 0.74: return self.spot
        return self.base

class Fawn(Coat):
    """Fawn with a dark mask and a lighter chest."""
    def __init__(self, base, mask, chest, **kw):
        super().__init__(base, **kw); self.mask, self.chest = rgb(mask), rgb(chest)
    def fur(self, reg, r, c, paw):
        if reg in 'GM': return self.mask
        if reg in 'EF': return shade(self.mask, 1.15)
        if reg == 'C': return self.chest
        return self.base

class Socks(Coat):
    """Red coat with a white muzzle, chest, belly and socks."""
    def __init__(self, base, light, **kw):
        super().__init__(base, **kw); self.light = rgb(light)
    def fur(self, reg, r, c, paw):
        if reg in 'GMC' or (reg in 'LN' and paw): return self.light
        return self.base

class Patched(Coat):
    """White with large liver patches, liver head and ears, white blaze."""
    def __init__(self, base, patch, **kw):
        super().__init__(base, **kw); self.patch = rgb(patch)
    def fur(self, reg, r, c, paw):
        if reg in 'HEF': return self.patch
        if reg in 'BJT' and noise(c / 3.2, r / 2.6, 11) > 0.55: return self.patch
        if reg in 'LN' and not paw and noise(c / 1.4, r / 1.4, 5) > 0.8: return self.patch
        return self.base

class Curly(Coat):
    """An all-over curly coat: tight light and dark flecks, a clipped muzzle."""
    def __init__(self, base, **kw):
        super().__init__(base, **kw)
    def fur(self, reg, r, c, paw):
        if reg in 'GM': return shade(self.base, 1.12)
        base = shade(self.base, 0.86) if reg in 'EF' else self.base
        k = (r * 2 + c * 3 + (r // 2)) % 5
        return shade(base, 1.16) if k == 0 else shade(base, 0.82) if k == 2 else base

# name, description, body size, head, tail, long fur, coat
DOGS = [
    ('Mocha', 'Chocolate floppy-eared retriever mix, thick otter tail', 'adult', 'floppy', 'otter', False,
     Solid('#7d4c2e', '#a8774f', '#47281a', eye='#d29a46', nose='#2c1d18', inner='#4a2c20')),
    ('Juniper', 'Grey and white sled dog, prick ears, curled plume tail', 'adult', 'prick', 'curl', True,
     Mantle('#6f7784', '#eeeae4', eye='#78b8ea', nose='#2a2a30', inner='#d9a8a4')),
    ('Domino', 'White spotted dog with one dark ear and a whip tail', 'adult', 'floppy', 'whip', False,
     Spotted('#f1ede6', '#2e2b30', eye='#6a4630', nose='#2b2629', inner='#3a3438')),
    ('Pip', 'Scruffy tan terrier puppy with a dark saddle and a beard', 'kitten', 'fold', 'carrot', False,
     Saddle('#c08a52', '#3d3733', '#e2cfae', eye='#3a2a22', nose='#231e1c', inner='#2e2826')),
    ('Toast', 'Red fox-faced dog with white socks and a bobbed tail', 'adult', 'prick', 'stub', False,
     Socks('#c9693a', '#f4ece0', eye='#7a4a26', nose='#2a2224', inner='#e2a088')),
    ('Clover', 'Black and white shepherd with tipped ears and a bushy low tail', 'adult', 'tipped', 'brush', True,
     Mantle('#2c2b31', '#f0ece6', eye='#8a5a32', nose='#1f1d22', inner='#c99a9a')),
    ('Waffles', 'Stocky fawn dog with a dark mask, blunt muzzle and bobbed tail', 'adult', 'square', 'stub', False,
     Fawn('#d1a56c', '#3a302c', '#e8cfa4', eye='#5a3a22', nose='#1e1a1a', inner='#3a302c')),
    ('Hazel', 'Liver and white spaniel puppy with long wavy ears', 'kitten', 'longear', 'otter', True,
     Patched('#f3eee6', '#7a3f2a', eye='#5a3420', nose='#5a2e22', inner='#6a3524')),
    ('Peaches', 'Apricot poodle with a curly coat, topknot, long ears and a pompom tail', 'adult', 'poodle', 'pom', True,
     Curly('#e9a56c', eye='#3a2418', nose='#3a2620', inner='#c47e52')),
]

BONE_COL = rgb('#efe5cf')

def draw_props(img, props):
    """Bones go over the dog, with top-down shading and a warm outline."""
    cells = {}
    for art, r0, c0 in props:
        for i, row in enumerate(art):
            for j, ch in enumerate(row):
                if ch != '.': cells[(r0 + i, c0 + j)] = ch
    for (r, c) in cells:
        assert 0 <= r < H and 0 <= c < W, ('bone out of frame', r, c)
    out = shade(BONE_COL, 0.5, -0.01)
    for (r, c) in cells:
        for dr, dc in ((1, 0), (0, -1), (0, 1), (-1, 0)):
            p = (r + dr, c + dc)
            if p not in cells and 0 <= p[0] < H and 0 <= p[1] < W:
                img.putpixel((p[1], p[0]), out + (255,))
    for (r, c) in cells:
        f = 1.0 if (r - 1, c) in cells else 1.1
        if (r + 1, c) not in cells: f = 0.82
        img.putpixel((c, r), shade(BONE_COL, f) + (255,))
    return img

def frames_for(kind, head, tail, fluff, coat):
    out = {}
    for name in dogkit.POSES:
        cv = dogkit.pose_canvas(name, kind, head, tail)
        g = final.fluffy(cv.g) if fluff else cv.g
        try: out[name] = draw_props(final.render(g, coat), cv.props)
        except AssertionError as e: raise SystemExit(f'{name} {head}: {e}')
    return out

if __name__ == '__main__':
    out = sys.argv[1]
    os.makedirs(out, exist_ok=True)
    bg = (232, 236, 244, 255)
    font = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf', 20)
    small = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf', 13)
    S, cols = 5, 7
    names = list(dogkit.POSES)
    all_frames = {}
    for name, desc, kind, head, tail, fluff, coat in DOGS:
        fr = frames_for(kind, head, tail, fluff, coat); all_frames[name] = fr
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

    def strip(seq, Z, path, labels=False):
        gif = []
        top = 22 if labels else 0
        for pose, hold in seq:
            fr = Image.new('RGBA', (len(DOGS) * (W + 4) * Z, (H + 4) * Z + top), bg)
            d = ImageDraw.Draw(fr)
            for i, (name, *_) in enumerate(DOGS):
                x = 8 + i * (W + 4) * Z
                if labels: d.text((x, 4), name, fill=(40, 36, 52, 255), font=small)
                fr.alpha_composite(all_frames[name][pose].resize((W * Z, H * Z), Image.NEAREST), (x, 8 + top))
            gif.append((fr.convert('RGB'), hold * 200))
        gif[0][0].save(path, save_all=True, append_images=[g for g, _ in gif[1:]],
                       duration=[t for _, t in gif], loop=0)

    BONE_SHOW = ([('CarryA', 1), ('CarryB', 1)] * 4 + [('PlayBow', 5), ('Toss', 3), ('Catch', 4)]
                 + [('ChewA', 2), ('ChewB', 2)] * 4)
    strip(final.SHOW + BONE_SHOW, 3, f'{out}/all-dogs-poses.gif')
    strip(BONE_SHOW, 3, f'{out}/dogs-bone.gif', labels=True)
    walk = [('WalkA', 1), ('WalkB', 1)] * 6 + [('RunA', 0.4), ('RunB', 0.4), ('RunC', 0.4), ('RunD', 0.4)] * 8
    strip(walk, 3, f'{out}/dog-concepts-walk.gif', labels=True)
    # an overview: each dog's walk, sit, nap, petted and gallop frames
    pick = ['WalkA', 'Sit', 'PettedA', 'NapA', 'RunA', 'CarryA', 'ChewA']
    S2 = 5
    cw, ch = W * S2 + 10, H * S2 + 12
    sheet = Image.new('RGBA', (16 + len(pick) * cw, 30 + len(DOGS) * (ch + 46)), bg)
    d = ImageDraw.Draw(sheet)
    y = 16
    for name, desc, *_ in DOGS:
        d.text((16, y), name, fill=(40, 36, 52, 255), font=font)
        d.text((16, y + 24), desc, fill=(90, 90, 110, 255), font=small)
        y += 46
        for i, p in enumerate(pick):
            sheet.alpha_composite(all_frames[name][p].resize((W * S2, H * S2), Image.NEAREST), (16 + i * cw, y))
        y += ch
    sheet.save(f'{out}/dog-concepts.png')
