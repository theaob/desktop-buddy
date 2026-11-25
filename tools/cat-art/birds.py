"""Original bird concepts in every app pose plus bird extras: per-bird pose sheets, a side-by-side GIF of the
whole pose set, a hop + fly GIF and an extras GIF. Run from this folder: python3 birds.py <outdir>"""
import math, os, sys
from PIL import Image, ImageDraw, ImageFont
import birdkit
import final
from final import Coat, noise
from real import rgb, shade

W, H = birdkit.W, birdkit.H

class Plumage(Coat):
    """Colours by region. cols maps region letters to colours; 'B' is the base. Optional patterns:
    streaks on the back, barring on the head and wings, a pale wing bar, a sheen on the back."""
    def __init__(self, cols, eye='#2a2226', streak=None, bars=None, wingbar=None, sheen=None, belly=None):
        self.cols = {k: rgb(v) for k, v in cols.items()}
        super().__init__(cols['B'], eye=eye, nose=cols.get('k', '#333333'), inner='#000000')
        self.streak = rgb(streak) if streak else None
        self.bars = rgb(bars) if bars else None
        self.wingbar = rgb(wingbar) if wingbar else None
        self.sheen = rgb(sheen) if sheen else None
        self.bellyc = rgb(belly) if belly else None
    def fur(self, reg, r, c, paw):
        cols = self.cols
        if reg == 'V': return shade(cols.get('W', self.base), 0.72)
        base = cols.get(reg) or {'A': cols.get('H'), 'G': cols.get('H'), 'M': cols.get('H'),
                                 'R': cols.get('A', cols.get('H')), 'P': cols.get('W'),
                                 'q': cols.get('k'), 'N': cols.get('L')}.get(reg) or self.base
        if reg == 'C' and self.bellyc and r >= 15: return self.bellyc
        if self.streak and reg in 'BW' and (c + r // 2) % 3 == 0: return self.streak
        if self.bars and reg in 'HAW' and r % 2 == 0 and reg != 'G': return self.bars
        if reg == 'X': return self.wingbar or cols.get('W', self.base)
        if self.sheen and reg in 'WPT' and (c - r) % 3 == 0: return self.sheen
        return base

# name, description, shape, plumage
BIRDS = [
    ('Ember', 'Plump songbird, olive-brown back with a glowing orange face and breast',
     dict(size=0.92, beak='thin', head=2.9, bodyw=5.2, bodyh=3.9, tail=4.6, legs=2.6,
          pattern=dict(face=(-0.25, -0.35))),
     Plumage({'B': '#7a6a52', 'H': '#7a6a52', 'G': '#e8743a', 'C': '#ec7b3e', 'W': '#6c5c46', 'P': '#4a3f33',
              'T': '#5e5040', 'k': '#3a3230', 'L': '#8a6a5a'}, belly='#f1e6d6')),
    ('Blueberry', 'Tiny round tit with a blue cap and wings, white cheeks and a lemon breast',
     dict(size=0.8, beak='short', wingbar=True, head=3.0, bodyw=4.8, bodyh=3.9, tail=4.0, legs=2.4,
          pattern=dict(cap=-0.3, cheek=(0.15, 0.3, 0.55), mask=(-0.1, 0.14, -0.9))),
     Plumage({'B': '#8a9a5a', 'H': '#f4f4f0', 'A': '#3f7fd0', 'G': '#f6f6f2', 'M': '#26324a', 'C': '#f2d23e',
              'W': '#3f7fd0', 'P': '#2f5ea0', 'T': '#3a6ab0', 'k': '#2e2e36', 'L': '#6a7488'}, wingbar='#e8eef6')),
    ('Acorn', 'Cheeky streaky-brown sparrow with a grey crown and a black bib',
     dict(size=0.86, beak='cone', wingbar=True, head=2.9, bodyw=5.0, bodyh=3.7, tail=4.4, legs=2.6,
          pattern=dict(cap=-0.35, cheek=(0.1, 0.25, 0.45), bib=(0.55, 0.15))),
     Plumage({'B': '#a8743e', 'H': '#8a5a34', 'A': '#8f8f96', 'G': '#e6dccb', 'M': '#2e2a2a', 'C': '#d8cfbf',
              'W': '#9a6634', 'P': '#5e4028', 'T': '#6e4a2c', 'k': '#3a3434', 'L': '#c2967a'},
             streak='#4e3420', wingbar='#efe6d6')),
    ('Sunny', 'Grey parrot with a sunny yellow face, orange cheek spots and a tall crest',
     dict(size=1.0, beak='hook', head=2.9, bodyw=4.8, bodyh=3.5, tail=7.0, tailw=(2.4, 1.2), legs=2.4,
          crest=[(0.2, -1), (-0.3, -2), (-0.8, -3), (-1.4, -3.6), (0.6, -1), (0, -2), (-0.5, -3)],
          pattern=dict(face=(-0.45, -1.2), cheek=(0.35, 0.35, 0.32))),
     Plumage({'B': '#8c8e96', 'H': '#8c8e96', 'G': '#f4d64a', 'A': '#f4d64a', 'R': '#f0cc3a', 'C': '#9a9ca4',
              'W': '#7c7e86', 'P': '#5c5e66', 'T': '#6a6c74', 'k': '#b8aaa0', 'L': '#a89a96'},
             wingbar='#eeeeee')),
    ('Kiwi', 'Bright green budgie with a yellow barred head and a blue tail',
     dict(size=0.86, beak='hook', head=2.8, bodyw=4.8, bodyh=3.5, tail=6.6, tailw=(2.2, 1.1), legs=2.4,
          pattern=dict(face=(-0.1, -0.45))),
     Plumage({'B': '#5cc04a', 'H': '#f0e04a', 'G': '#f4e660', 'C': '#62c84e', 'W': '#e8d84a', 'P': '#2e3a2a',
              'T': '#3a6ad0', 'k': '#e0c070', 'L': '#8a90a8'}, bars='#2a2a26')),
    ('Skye', 'Sky-blue budgie with a white barred head and a deep cobalt tail',
     dict(size=0.86, beak='hook', head=2.8, bodyw=4.8, bodyh=3.5, tail=6.6, tailw=(2.2, 1.1), legs=2.4,
          pattern=dict(face=(-0.1, -0.45), cheek=(0.3, 0.45, 0.22))),
     Plumage({'B': '#5aa8e6', 'H': '#f2f4f6', 'G': '#f6f8fa', 'C': '#62b2ec', 'W': '#e8ecf0', 'P': '#2a2e3a',
              'T': '#2a4ab0', 'k': '#e8c890', 'L': '#8a90a8'}, bars='#2a2a30')),
    ('Inky', 'Glossy black crow with a blue-violet sheen and a stout beak',
     dict(size=1.18, beak='crow', ring=True, head=2.8, bodyw=5.0, bodyh=3.5, tail=5.4, legs=3.0),
     Plumage({'B': '#26242e', 'H': '#2a2832', 'C': '#2e2c36', 'W': '#22202a', 'P': '#1c1a24',
              'T': '#1e1c26', 'k': '#2e2c30', 'L': '#2e2c34'}, eye='#7a7480', sheen='#3a4470')),
    ('Slate', 'Soft grey pigeon with a shimmering green neck and two dark wing bars',
     dict(size=1.04, beak='dove', wingbar=True, head=2.5, bodyw=5.4, bodyh=3.8, tail=5.0, legs=2.4,
          pattern=dict(bib=(0.25, -0.9))),
     Plumage({'B': '#9aa0ae', 'H': '#7e8494', 'M': '#5a9a8a', 'C': '#8e8aa8', 'W': '#a8aebb', 'P': '#4e525e',
              'T': '#5e6270', 'k': '#3a3436', 'L': '#d27a7a'}, eye='#d88a3a', wingbar='#4a4e5a')),
    ('Nugget', 'Fluffy yellow chick with stubby wings and big feet',
     dict(size=0.72, beak='short', head=3.4, bodyw=4.6, bodyh=4.0, tail=1.6, tailw=(2.4, 2.0), legs=2.0,
          wing=0.6),
     Plumage({'B': '#f6d54a', 'H': '#f8dc56', 'C': '#fae27a', 'W': '#f0c63a', 'P': '#e8b830',
              'T': '#eec440', 'k': '#f0902e', 'L': '#f0902e'})),
    ('Rosehip', 'Crested red finch with a black mask and a thick coral beak',
     dict(size=0.96, beak='cone', head=2.9, bodyw=5.0, bodyh=3.7, tail=5.6, legs=2.6,
          crest=[(0.0, -1), (-0.5, -2), (-1.0, -3), (-0.6, -1), (-1.2, -2)],
          pattern=dict(face=(0.3, -0.4))),
     Plumage({'B': '#c83a34', 'H': '#d8443a', 'A': '#d8443a', 'R': '#e04a3e', 'G': '#24202a', 'C': '#e0503e',
              'W': '#a83030', 'P': '#7e2a2a', 'T': '#9a2e2e', 'k': '#f08a6a', 'L': '#9a7a78'})),
]

FIXED = {'n': rgb('#4a5aa0'), 's': rgb('#c89a5a')}

def draw_props(img, props):
    """Music notes and seeds go over the bird, each with its own darker outline."""
    cells = {}
    for art, r0, c0 in props:
        for i, row in enumerate(art):
            for j, ch in enumerate(row):
                if ch != '.': cells[(r0 + i, c0 + j)] = ch
    for (r, c), ch in cells.items():
        assert 0 <= r < H and 0 <= c < W, ('prop out of frame', r, c)
        if ch == 's':
            img.putpixel((c, r), FIXED['s'] + (255,))
            if c + 1 < W and (r, c + 1) not in cells: img.putpixel((c + 1, r), shade(FIXED['s'], 0.6) + (255,))
        else:
            img.putpixel((c, r), FIXED[ch] + (255,))
    return img

def frames_for(spec, coat):
    out = {}
    for name in birdkit.POSES:
        cv = birdkit.pose_canvas(name, spec)
        try:
            out[name] = draw_props(final.render(cv.g, coat), cv.props)
        except AssertionError as e:
            raise SystemExit(f'{name}: {e}')
    return out

SHOW = final.SHOW
EXTRA_SHOW = ([('PeckA', 2), ('PeckB', 1), ('PeckA', 1), ('PeckB', 1)] * 2 + [('SingA', 2), ('SingB', 2)] * 3
              + [('FlapA', 1), ('FlapB', 1)] * 4 + [('PerchA', 4), ('PerchB', 1), ('PerchA', 3)]
              + [('RunA', 0.5), ('RunB', 0.5), ('RunC', 0.5), ('RunD', 0.5)] * 2 + [('Glide', 4)]
              + [('RunA', 0.5), ('RunB', 0.5), ('RunC', 0.5), ('RunD', 0.5)] * 2)

if __name__ == '__main__':
    out = sys.argv[1]
    os.makedirs(out, exist_ok=True)
    bg = (232, 236, 244, 255)
    font = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf', 20)
    small = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf', 13)
    S, cols = 5, 9
    names = list(birdkit.POSES)
    only = sys.argv[2:]   # optional: just these birds
    birds = [b for b in BIRDS if not only or b[0] in only]
    all_frames = {}
    for name, desc, spec, coat in birds:
        fr = frames_for(spec, coat); all_frames[name] = fr
        cw, ch = W * S + 12, H * S + 26
        rows = math.ceil(len(names) / cols)
        sheet = Image.new('RGBA', (16 + cols * cw, 56 + rows * ch), bg)
        d = ImageDraw.Draw(sheet)
        d.text((16, 12), f'{name}: {desc}', fill=(40, 36, 52, 255), font=font)
        for i, p in enumerate(names):
            x, y = 16 + (i % cols) * cw, 52 + (i // cols) * ch
            sheet.alpha_composite(fr[p].resize((W * S, H * S), Image.NEAREST), (x, y))
            d.text((x, y + H * S + 4), p + (' *' if p in birdkit.BIRD_POSES else ''), fill=(90, 90, 110, 255), font=small)
        sheet.save(f'{out}/{name.lower()}-poses.png')

    def strip(seq, Z, path, labels=False):
        gif = []
        top = 22 if labels else 0
        for pose, hold in seq:
            fr = Image.new('RGBA', (len(birds) * (W + 4) * Z, (H + 4) * Z + top), bg)
            d = ImageDraw.Draw(fr)
            for i, (name, *_) in enumerate(birds):
                x = 8 + i * (W + 4) * Z
                if labels: d.text((x, 4), name, fill=(40, 36, 52, 255), font=small)
                fr.alpha_composite(all_frames[name][pose].resize((W * Z, H * Z), Image.NEAREST), (x, 8 + top))
            gif.append((fr.convert('RGB'), round(hold * 200)))
        gif[0][0].save(path, save_all=True, append_images=[g for g, _ in gif[1:]],
                       duration=[t for _, t in gif], loop=0)

    strip(SHOW + EXTRA_SHOW, 3, f'{out}/all-birds-poses.gif')
    strip(EXTRA_SHOW, 3, f'{out}/birds-extras.gif', labels=True)
    move = [('WalkA', 1), ('WalkB', 1)] * 6 + [('RunA', 0.5), ('RunB', 0.5), ('RunC', 0.5), ('RunD', 0.5)] * 8
    strip(move, 3, f'{out}/bird-concepts-hop-fly.gif', labels=True)
    pick = ['Sit', 'WalkB', 'PettedA', 'NapA', 'RunA', 'RunC', 'PeckB', 'SingA', 'GroomB']
    S2 = 5
    cw, ch = W * S2 + 10, H * S2 + 12
    sheet = Image.new('RGBA', (16 + len(pick) * cw, 30 + len(birds) * (ch + 46)), bg)
    d = ImageDraw.Draw(sheet)
    y = 16
    for name, desc, *_ in birds:
        d.text((16, y), name, fill=(40, 36, 52, 255), font=font)
        d.text((16, y + 24), desc, fill=(90, 90, 110, 255), font=small)
        y += 46
        for i, p in enumerate(pick):
            sheet.alpha_composite(all_frames[name][p].resize((W * S2, H * S2), Image.NEAREST), (16 + i * cw, y))
        y += ch
    sheet.save(f'{out}/bird-concepts.png')
