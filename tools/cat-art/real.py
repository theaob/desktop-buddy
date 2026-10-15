"""Realistic pixel-art takes on the five concepts, at the app's 30x20 sprite size.
Poses are drawn as body-part regions; coat colour, top-down fur shading and a coloured
(not black) outline are applied automatically."""
import colorsys
from PIL import Image

W, H = 30, 20

def place(*parts, dx=0):
    g = [['.'] * W for _ in range(H)]
    for r, c, s in parts:
        for i, ch in enumerate(s):
            if ch != ' ':
                g[r][c + i + dx] = ch
    return g

def over(g, *parts):
    for r, c, s in parts:
        for i, ch in enumerate(s):
            if ch != ' ':
                g[r][c + i] = ch
    return g

# Regions: H head, E near ear, F far ear, e inner ear, Y eye, K pupil, M muzzle, n nose,
# B body, C chest/belly, J haunch, L near leg, N far leg, T tail
def adult_head(dx=0):
    return [
        (2,21,"F"),(2,24,"E"),
        (3,20,"FF"),(3,23,"EeE"),
        (4,20,"HHHHeEH"),
        (5,20,"HHHHHHH"),
        (6,19,"HHHHHYKHH"),
        (7,19,"HHHHHHHMMn"),
        (8,19,"HHHHHHMM"),
        (9,20,"HHHMM"),
    ], dx

def adult_walk(a=True, belly=False):
    head, _ = adult_head()
    parts = [
        (4,1,"T"),(5,1,"TT"),(6,1,"TT"),(7,2,"TT"),(8,3,"TTTT"),(9,4,"TT"),
        (8,7,"BBBBBBBBBBBB"),
        (9,6,"BBBBBBBBBBBBBB"),
        (10,5,"JJJBBBBBBBBBBBBBCCC"),
        (11,5,"JJJJBBBBBBBBBBBBCCC"),
        (12,5,"JJJJBBBBBBBBBBBBBCC"),
        (13,6,"JJJCCCCCCCCBBBBBB"),
    ] + head
    if belly:
        parts.append((14,10,"CCCCCC"))
    if a:
        parts += [(14,9,"NN"),(15,10,"NN"),(16,10,"NN"),(17,10,"NN"),(18,10,"NNN"),
                  (14,17,"NN"),(15,17,"NN"),(16,16,"NN"),(17,16,"NN"),(18,15,"NNN"),
                  (14,6,"LLL"),(15,5,"LL"),(16,4,"LL"),(17,4,"LL"),(18,4,"LLL"),
                  (14,20,"LL"),(15,20,"LL"),(16,21,"LL"),(17,21,"LL"),(18,21,"LLL")]
    else:
        parts += [(14,6,"NN"),(15,5,"NN"),(16,4,"NN"),(17,4,"NN"),(18,3,"NNN"),
                  (14,21,"NN"),(15,22,"NN"),(16,22,"NN"),(17,23,"NN"),(18,23,"NNN"),
                  (14,8,"LLL"),(15,9,"LL"),(16,10,"LL"),(17,10,"LL"),(18,10,"LLL"),
                  (14,19,"LL"),(15,18,"LL"),(16,17,"LL"),(17,17,"LL"),(18,16,"LLL")]
    return place(*parts)

def adult_sit():
    head, _ = adult_head()
    g = place(*head, dx=-3)
    return over(g,
        (8,15,"B"),
        (9,13,"BBBB"),
        (10,11,"BBBBBBBBCCC"),
        (11,10,"BBBBBBBBBCCC"),
        (12,9,"BBBBJJJJJBCCC"),
        (13,9,"BBJJJJJJJJJCLLN"),
        (14,8,"BBJJJJJJJJJJCLLN"),
        (15,8,"BJJJJJJJJJJJCLLN"),
        (16,8,"BJJJJJJJJJJJCLLN"),
        (17,8,"BBJJJJJJJJJJCLLN"),
        (18,9,"BBBBBBBBLLLCLLLN"),
        (18,2,"TTTTTTT"),(17,1,"TT"),(16,1,"T"))

# Kitten: smaller body, bigger head, shorter legs and tail
def kitten_head(dx=0, dy=0):
    return [(r + dy, c + dx, s) for r, c, s in [
        (4,19,"F"),(4,23,"E"),
        (5,18,"FF"),(5,22,"EeE"),
        (6,18,"HHHHeEHH"),
        (7,17,"HHHHHHHHHH"),
        (8,17,"HHHHHYYKHH"),
        (9,17,"HHHHHHHHMMn"),
        (10,17,"HHHHHHHMMM"),
        (11,18,"HHHHHHMM"),
    ]]

def kitten_walk(a=True):
    parts = [(7,6,"T"),(8,6,"T"),(9,6,"TT"),(10,7,"TT"),
             (10,9,"BBBBBBBB"),
             (11,8,"JJBBBBBBBBB"),
             (12,8,"JJJBBBBBBBCCC"),
             (13,8,"JJJBBBBBBBCCC"),
             (14,9,"JJCCCCCCBBB")] + kitten_head()
    if a:
        parts += [(15,11,"NN"),(16,12,"NN"),(17,12,"NN"),(18,12,"NNN"),
                  (15,17,"NN"),(16,16,"NN"),(17,16,"NN"),(18,15,"NNN"),
                  (15,8,"LL"),(16,8,"LL"),(17,7,"LL"),(18,7,"LLL"),
                  (15,19,"LL"),(16,20,"LL"),(17,20,"LL"),(18,20,"LLL")]
    else:
        parts += [(15,8,"NN"),(16,8,"NN"),(17,7,"NN"),(18,6,"NNN"),
                  (15,19,"NN"),(16,20,"NN"),(17,20,"NN"),(18,21,"NNN"),
                  (15,10,"LL"),(16,11,"LL"),(17,11,"LL"),(18,11,"LLL"),
                  (15,17,"LL"),(16,17,"LL"),(17,16,"LL"),(18,16,"LLL")]
    return place(*parts)

def kitten_sit():
    g = place(*kitten_head(dx=-2, dy=1))
    return over(g,
        (13,13,"BBBBCCC"),
        (14,12,"BJJJJCCLLN"),
        (15,11,"BJJJJJCLLN"),
        (16,11,"BJJJJJCLLN"),
        (17,11,"BJJJJJCLLN"),
        (18,11,"BBBBLLLCLLLN"),
        (18,5,"TTTTTT"),(17,4,"TT"))

def fluffy(g):
    """Long fur: a ragged fringe under the belly, a fuller tail and a chest ruff."""
    g = [row[:] for row in g]
    src = [row[:] for row in g]
    for r in range(1, H - 1):
        for c in range(1, W - 1):
            ch = src[r][c]
            if ch in 'BCJ' and src[r + 1][c] in '.' and c % 3 == 0 and r < 14:
                g[r + 1][c] = 'C' if ch == 'C' else 'B'
            if ch == 'T':
                for dr, dc in ((0, -1), (-1, 0), (1, 0)):
                    if src[r + dr][c + dc] == '.':
                        g[r + dr][c + dc] = 'T'
            if ch == 'C' and src[r][c + 1] == '.' and r % 2 == 1:
                g[r][c + 1] = 'C'
    return g

# ---------------- colour helpers ----------------
def rgb(h): h = h.lstrip('#'); return tuple(int(h[i:i+2], 16) for i in (0, 2, 4))

def shade(c, f, hue_shift=0.0):
    h, l, s = colorsys.rgb_to_hls(*[x / 255 for x in c])
    l = max(0, min(1, l * f if f < 1 else l + (1 - l) * (f - 1)))
    h = (h + hue_shift) % 1
    return tuple(round(x * 255) for x in colorsys.hls_to_rgb(h, l, s))

FUR = set('HEFBCJLNTM')

class Coat:
    def __init__(self, base, eye, nose='#c98a8a', inner='#d79a9a', light_hue=0.0):
        self.base, self.eye, self.nose, self.inner = rgb(base), rgb(eye), rgb(nose), rgb(inner)
    def color(self, reg, r, c, pose):  # base colour of a fur pixel before shading
        return self.base

class Tabby(Coat):
    def __init__(self, base, stripe, white, **kw):
        super().__init__(base, **kw); self.stripe, self.white = rgb(stripe), rgb(white)
    def color(self, reg, r, c, pose):
        if reg in 'CM': return self.white
        if reg in 'LN' and pose.get('paw', {}).get((r, c)): return self.white
        if reg in 'BJ' and c % 4 == 0 and r <= 12: return self.stripe
        if reg == 'T' and (r + c) % 3 == 0: return self.stripe
        if reg == 'H' and r in (4, 5) and c % 2 == 1: return self.stripe
        if reg in 'LN' and r % 3 == 0: return self.stripe
        return self.base

class Tuxedo(Coat):
    def __init__(self, base, white, **kw):
        super().__init__(base, **kw); self.white = rgb(white)
    def color(self, reg, r, c, pose):
        if reg in 'CM': return self.white
        if reg in 'LN' and pose.get('paw', {}).get((r, c)): return self.white
        return self.base

class Point(Coat):
    def __init__(self, base, point, **kw):
        super().__init__(base, **kw); self.point = rgb(point)
    def color(self, reg, r, c, pose):
        if reg in 'EFTM': return self.point
        if reg == 'H' and pose['face'](r, c): return self.point
        if reg in 'LN' and pose.get('paw', {}).get((r, c)) is not None: return self.point
        return self.base

class Calico(Coat):
    def __init__(self, base, orange, black, **kw):
        super().__init__(base, **kw); self.orange, self.black = rgb(orange), rgb(black)
    def color(self, reg, r, c, pose):
        if reg in 'CML': return self.base
        x = pose['ox'] + c
        if reg in 'EF' or (reg == 'H' and x - pose['hx'] < 3): return self.orange
        if reg in 'BJT' and x < 11: return self.orange
        if reg == 'B' and 13 <= x <= 15 and r <= 12 + pose['oy']: return self.black
        if reg == 'T' and r % 2 == 0: return self.black
        return self.base

def paws(g, depth):
    """Bottom `depth` rows of each leg column."""
    out = {}
    for c in range(W):
        rows = [r for r in range(H) if g[r][c] in 'LN']
        for r in rows:
            if r >= max(rows) - depth + 1:
                out[(r, c)] = True
    return out

def render(g, coat, pose):
    img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    col = [[None] * W for _ in range(H)]
    # top/bottom of the whole silhouette in each column for top-down light
    for c in range(W):
        filled = [r for r in range(H) if g[r][c] != '.']
        for r in filled:
            reg = g[r][c]
            if reg == 'Y': col[r][c] = coat.eye; continue
            if reg == 'K': col[r][c] = shade(coat.eye, 0.25); continue
            if reg == 'n': col[r][c] = coat.nose; continue
            if reg == 'e': col[r][c] = coat.inner; continue
            base = coat.color(reg, r, c, pose)
            above = g[r - 1][c] if r > 0 else '.'
            below = g[r + 1][c] if r < H - 1 else '.'
            f = 1.0
            if above == '.': f = 1.22                        # lit top edge
            elif below == '.': f = 0.80                      # shadowed underside
            elif r + 2 < H and g[r + 2][c] == '.' and reg in 'BCJ': f = 0.9
            if reg in 'NF': f *= 0.72                        # far legs/ear sit in shadow
            if reg == 'J' and above not in 'J.': f = max(f, 1.12)  # rim light on the haunch
            if reg == 'T' and below == '.': f = 0.82
            if below == 'Y': f *= 0.75                       # brow over the eye
            col[r][c] = shade(base, f, 0.0 if f >= 1 else -0.01)
    # coloured outline: a deep shade of the neighbouring fur
    for r in range(H):
        for c in range(W):
            if g[r][c] != '.':
                continue
            for dr, dc in ((1, 0), (0, -1), (0, 1), (-1, 0)):
                rr, cc = r + dr, c + dc
                if 0 <= rr < H and 0 <= cc < W and col[rr][cc] is not None and g[rr][cc] != '.':
                    img.putpixel((c, r), shade(col[rr][cc], 0.42, -0.02) + (255,))
                    break
    for r in range(H):
        for c in range(W):
            if col[r][c] is not None:
                img.putpixel((c, r), col[r][c] + (255,))
    return img

def frames(kind, coat, fluff=False, belly=False):
    if kind == 'adult':
        gs = [adult_walk(True, belly), adult_walk(False, belly), adult_sit()]
        info = [dict(ox=0, oy=0, hx=19), dict(ox=0, oy=0, hx=19), dict(ox=3, oy=-3, hx=19)]
    else:
        gs = [kitten_walk(True), kitten_walk(False), kitten_sit()]
        info = [dict(ox=0, oy=0, hx=17), dict(ox=0, oy=0, hx=17), dict(ox=-1, oy=1, hx=17)]
    if fluff:
        gs = [fluffy(g) for g in gs]
    out = []
    for g, inf in zip(gs, info):
        inf['paw'] = paws(g, 2)
        hx = inf['hx'] - (3 if inf is info[2] and kind == 'adult' else 2 if inf is info[2] else 0)
        inf['face'] = lambda r, c, hx=hx: c >= hx + 3 or r >= 7
        out.append(render(g, coat, inf))
    return out

REALISTIC = {
    'Bean':   lambda: frames('adult', Tabby('#d9894a', '#a95e2a', '#f1e4d2', eye='#c9b23c', nose='#d98a8a'), belly=True),
    'Wisp':   lambda: frames('adult', Coat('#2d2b36', eye='#e0a832', nose='#3c3640', inner='#5a4450')),
    'Puff':   lambda: frames('adult', Point('#ecdfc8', '#6f5242', eye='#6fa8e8', nose='#5a4038', inner='#c99a96'), fluff=True),
    'Pebble': lambda: frames('adult', Tuxedo('#25252d', '#efeef2', eye='#98c45a', nose='#d89aa2', inner='#8a6a74')),
    'Sprout': lambda: frames('kitten', Calico('#f3eee6', '#d98a45', '#35302e', eye='#7aa6c8', nose='#e39aa6', inner='#e3a6ae')),
}
