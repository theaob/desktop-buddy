"""Bird buddies on the cat pose kit's canvas: same 30x20 frame and the same 36 pose names (so they map onto
the app's CatPose set), redrawn for a bird body: hops instead of walking, a four-beat wingbeat instead of a
gallop, preening instead of grooming, and bird-only extras (pecking, singing, flapping, gliding, perching).

Regions: B back, C breast, H head, A cap, G cheek/face, M mask/eye stripe, R crest, W wing, P flight
feathers, V far wing, T tail, L/N near/far leg, k beak, q lower beak, m open mouth, K eye, Y eye ring,
x closed eye, h heart, z sleep mark.
Run from this folder: python3 birds.py <outdir>"""
import math, os, sys
import posekit
from posekit import W, H, GROUND

# Beaks, facing right, anchored at the front edge of the head (row 0 = eye height + 'dy').
BEAKS = {
    # name: closed, open, up
    'cone':  dict(dy=1, closed=["kk.", "qq."], open=["kk.", "mm.", "qq."], up=[".k", "kk", "q."]),
    'thin':  dict(dy=1, closed=["kkk"], open=["kkk", "m..", "qq."], up=["..k", ".k.", "q.."]),
    'hook':  dict(dy=0, closed=["kk.", "kkk", ".k."], open=["kk.", "kkk", "m.k", "q.."], up=[".k", "kk", "kq"]),
    'crow':  dict(dy=1, closed=["kkkk", "qqq."], open=["kkkk", "mm..", "qqq."], up=["..k", ".kk", "kq."]),
    'short': dict(dy=1, closed=["kk"], open=["kk", "m.", "q."], up=[".k", "q."]),
    'dove':  dict(dy=1, closed=["kk."], open=["kk", "m.", "q."], up=[".k", "k."]),
}

HEART = posekit.HEART
ZED = posekit.ZED
NOTE = [".nn", ".n.", "nn.", "nn."]
NOTE2 = ["n.", "nn", "n.", "n."]
SEED = ["s"]

class BirdCanvas(posekit.Canvas):
    def __init__(self, spec):
        super().__init__('adult')
        self.s = spec
        self.k = spec['size']
        self.props = []      # (art, row, col) drawn on top: notes, seeds
    def X(self, x): return 15 + (x - 15) * self.k
    def Y(self, y): return GROUND - (GROUND - y) * self.k
    def poly(self, pts, ch, under=False):
        pts = [(self.X(x), self.Y(y)) for x, y in pts]
        for r in range(-posekit.PAD, H + posekit.PAD):
            for c in range(-posekit.PAD, W + posekit.PAD):
                px, py, inside = c + 0.5, r + 0.5, False
                for (x1, y1), (x2, y2) in zip(pts, pts[1:] + pts[:1]):
                    if (y1 > py) != (y2 > py) and px < x1 + (py - y1) * (x2 - x1) / (y2 - y1):
                        inside = not inside
                if inside: self.put(r, c, ch, under)
    def over(self, pts, w, only, ch):
        """Paint a line only over pixels that are already region 'only'."""
        keep = [row[:] for row in self.g]
        self.line(pts, w, w, '#')
        for r in range(H):
            for c in range(W):
                if self.g[r][c] == '#':
                    self.g[r][c] = ch if keep[r][c] == only else keep[r][c]
        self.extra = {p: v for p, v in self.extra.items() if v != '#'}
    def prop(self, art, x, y):
        self.props.append((art, round(self.Y(y)) - len(art) // 2, round(self.X(x)) - len(art[0]) // 2))

    # ---- parts ----
    def head(self, hx, hy, eye='open', beak='closed', flip=False, tilt=0, tuck=False):
        """Round head with the bird's own pattern, an eye and a beak. flip faces left."""
        s, k = self.s, self.k
        r = s.get('head', 3.0)
        cx, cy = self.X(hx), self.Y(hy)
        R = r * k
        cells = []
        for rr in range(-posekit.PAD, H + posekit.PAD):
            for cc in range(-posekit.PAD, W + posekit.PAD):
                dx, dy = (cc + 0.5 - cx) / R, (rr + 0.5 - cy) / (R * 0.95)
                if dx * dx + dy * dy <= 1:
                    cells.append((rr, cc, -dx if flip else dx, dy))
        pat = s.get('pattern', {})
        for rr, cc, dx, dy in cells:
            ch = 'H'
            if 'cap' in pat and dy < pat['cap']: ch = 'A'
            if 'face' in pat and dx > pat['face'][0] and dy > pat['face'][1]: ch = 'G'
            if 'cheek' in pat and (dx - pat['cheek'][0]) ** 2 + (dy - pat['cheek'][1]) ** 2 < pat['cheek'][2] ** 2: ch = 'G'
            if 'mask' in pat and abs(dy - pat['mask'][0]) < pat['mask'][1] and dx > pat['mask'][2]: ch = 'M'
            if 'bib' in pat and dy > pat['bib'][0] and dx > pat['bib'][1]: ch = 'M'
            self.put(rr, cc, ch)
        # crest: a few feathers swept back from the crown
        if s.get('crest'):
            sx = -1 if flip else 1
            top = cy - R * 0.9
            for i, (ox, oy) in enumerate(s['crest']):
                self.put(round(top + oy * k - 0.5), round(cx + sx * ox * k - 0.5), 'R')
        # eye
        ex = cx + (1 if not flip else -1) * R * s.get('eyex', 0.38)
        ey = cy - R * 0.12
        er, ec = math.floor(ey), math.floor(ex)
        if tuck: eye = 'closed'
        if s.get('ring') and eye == 'open':
            for dr, dc in ((0, -1), (0, 1), (-1, 0), (1, 0)):
                if self.g[er + dr][ec + dc] if 0 <= er + dr < H and 0 <= ec + dc < W else False:
                    self.put(er + dr, ec + dc, 'Y')
        self.put(er, ec, {'open': 'K', 'closed': 'x', 'up': 'K', 'happy': 'x'}[eye])
        if eye == 'up' and 0 <= er - 1 < H:
            self.put(er, ec, self.g[er + 1][ec] if 0 <= er + 1 < H else 'H'); self.put(er - 1, ec, 'K')
        # beak
        if not tuck:
            b = BEAKS[s['beak']]
            art = b[beak]
            front = cx + R * (1 if not flip else -1)
            r0 = er - b['dy'] + 1 if beak != 'up' else er - len(art) + 2
            for i, row in enumerate(art):
                for j, ch in enumerate(row):
                    if ch == '.': continue
                    c = math.floor(front - 0.3) + j if not flip else math.ceil(front + 0.3) - 1 - j
                    self.put(r0 + i, c, ch)
        rs = [p[0] for p in cells]; cs = [p[1] for p in cells]
        self.headbox = (min(rs), min(cs), max(rs) - min(rs) + 1, max(cs) - min(cs) + 1)

    def tail(self, root, angle, length=None, under=False):
        s = self.s
        L = length if length is not None else s.get('tail', 4.5)
        a = math.radians(angle)
        room = GROUND - 1.4 - root[1]   # a hanging tail swings back rather than poke through the taskbar
        if L * math.sin(a) > room:
            a = math.asin(max(-1.0, min(1.0, room / L)))
        tip = (root[0] - L * math.cos(a), root[1] + L * math.sin(a))
        w0, w1 = s.get('tailw', (2.6, 1.8))
        self.line([root, tip], w0, w1, 'T', under)
        if s.get('fan'):   # a fanned tail end
            self.line([((root[0] + tip[0]) / 2, (root[1] + tip[1]) / 2), tip], w1 + 0.8, w1 + 1.4, 'T', under)

    def legs(self, x, top, style='stand', near=True):
        """Thin legs and forward toes, ending on the ground."""
        L = self.s.get('legs', 2.6)
        ch_w = 1.15
        if style == 'stand':
            self.line([(x, top), (x, GROUND - 0.7)], ch_w, ch_w, 'L' if near else 'N')
            self.line([(x - 0.6, GROUND - 0.6), (x + 1.8, GROUND - 0.6)], 1.0, 1.0, 'L' if near else 'N')
        elif style == 'bent':
            self.line([(x, top), (x - 1.0, top + 1.1), (x, GROUND - 0.7)], ch_w, ch_w, 'L' if near else 'N')
            self.line([(x - 0.6, GROUND - 0.6), (x + 1.8, GROUND - 0.6)], 1.0, 1.0, 'L' if near else 'N')
        elif style == 'tuck':   # in the air, toes curled back under the body
            self.line([(x, top), (x - 1.4, top + 1.0)], ch_w, ch_w, 'L' if near else 'N')
        elif style == 'dangle':
            self.line([(x, top), (x - 0.4, top + 2.0), (x + 0.6, top + 2.8)], ch_w, ch_w, 'L' if near else 'N')
        elif style == 'reach':   # reaching down to land
            self.line([(x, top), (x + 0.8, top + 2.2), (x + 2.0, top + 2.6)], ch_w, ch_w, 'L' if near else 'N')

    def wing(self, sh, state, angle=0):
        """Near wing from the shoulder point sh. Folded wings lie along the body; open ones are drawn as
        a covert panel plus long flight feathers."""
        x, y = sh
        span = self.s.get('wing', 1.0)
        if state == 'fold':
            a = math.radians(14 + angle)
            ux, uy = -math.cos(a), math.sin(a)      # along the wing, backwards and down
            self.ellipse(x + ux * 2.2, y + uy * 2.2 + 0.4, 3.4 * span, 2.0, 'W', angle=-(14 + angle))
            self.line([(x + ux * 4.0 * span, y + uy * 4.0 * span + 0.6), (x + ux * 6.6 * span, y + uy * 6.6 * span + 0.8)], 2.2, 0.9, 'P')
            if self.s.get('wingbar'):
                bx, by = x + ux * 2.6 * span, y + uy * 2.6 * span + 0.4
                self.over([(bx + uy * 1.6, by - ux * 1.6 * -1 - 1.4), (bx - uy * 1.6, by + 1.4)], 1.0, 'W', 'X')
        elif state == 'droop':   # loosened, tips lowered (fluffed up or tired)
            self.ellipse(x - 2.3, y + 1.6, 3.8 * span, 2.3, 'W', angle=-6)
            self.line([(x - 4.2 * span, y + 2.6), (x - 6.4 * span, y + 4.4)], 2.2, 0.9, 'P')
        else:
            # (covert tip, primary tip) relative to the shoulder, per stroke position
            tips = {
                'up':     ((-2.2, -4.2), (-4.2, -8.0)),
                'high':   ((-3.2, -3.0), (-7.0, -5.2)),
                'level':  ((-4.2, -0.6), (-8.6, -0.4)),
                'down':   ((-2.6, 2.8), (-4.6, 6.2)),
                'stretch': ((-3.6, 2.2), (-7.6, 4.6)),   # one wing stretched down and back
                'spread': ((-2.0, -3.6), (-2.8, -7.6)),
                'fwd':    ((1.6, -3.2), (3.4, -6.6)),    # braking: swept forward
            }[state]
            (cx, cy), (px, py) = tips
            cx, cy, px, py = cx * span, cy * span, px * span, py * span
            # the panel: a wide quad from the shoulder out to the covert tip
            nx, ny = -cy, cx
            n = math.hypot(nx, ny) or 1
            nx, ny = nx / n * 1.6, ny / n * 1.6
            self.poly([(x + nx, y + ny), (x - nx, y - ny), (x + cx - nx * 0.8, y + cy - ny * 0.8),
                       (x + cx + nx * 0.8, y + cy + ny * 0.8)], 'W')
            self.line([(x + cx * 0.85, y + cy * 0.85), (x + px, y + py)], 2.6, 1.0, 'P')
            self.line([(x + cx * 0.6, y + cy * 0.6), (x + px * 0.82 + nx * 0.6, y + py * 0.82 + ny * 0.6)], 1.8, 0.9, 'P')

    def far_wing(self, sh, state):
        """The far wing only shows when it is raised above the back."""
        x, y = sh
        span = self.s.get('wing', 1.0)
        tips = {'up': (-1.0, -8.2), 'high': (-5.6, -6.0), 'spread': (0.4, -7.8), 'fwd': (3.8, -6.0),
                'level': (-7.6, -2.0)}
        if state not in tips: return
        px, py = tips[state]
        px, py = px * span, py * span
        self.line([(x + 1.0, y), (x + px * 0.5 + 0.6, y + py * 0.5), (x + px + 0.8, y + py)], 2.8, 1.2, 'V')

    def body(self, cx, cy, tilt=-18, fluff=0.0, rx=None, ry=None):
        s = self.s
        rx = (rx or s.get('bodyw', 4.6)) + fluff * 0.6
        ry = (ry or s.get('bodyh', 3.6)) + fluff
        self.ellipse(cx, cy, rx, ry, 'B', angle=tilt)
        a = math.radians(tilt)
        bx, by = cx + rx * 0.45 * math.cos(a), cy + rx * 0.45 * math.sin(a) + ry * 0.25
        self.ellipse(bx, by, rx * 0.55, ry * 0.85, 'C', angle=tilt)

# ---------------- the standing bird ----------------
def stand(cv, eye='open', beak='closed', head=None, wing='fold', tail=34, legs='stand', fluff=0.0,
          lift=0.0, body=(14.4, 11.2), tilt=-30, heart=None, zed=None, far='none'):
    """The base pose: body tilted up at the front, head above the breast, tail behind and below."""
    s = cv.s
    bx, by = body[0], body[1] - lift
    hx, hy = head if head else (17.9, 6.8)
    hy -= lift
    rx = s.get('bodyw', 4.6)
    a = math.radians(-tilt)
    if far != 'none': cv.far_wing((bx + 0.8, by - 2.0), far)
    leg_top = by + 2.4
    if legs != 'none':
        cv.legs(bx - 0.6, leg_top, legs, near=False)
    cv.tail((bx - rx * 0.7 * math.cos(a), by + rx * 0.7 * math.sin(a)), tail)
    cv.body(bx, by, tilt, fluff)
    mx, my = (bx + hx) / 2, (by + hy) / 2
    cv.ellipse(mx + 0.7, my + 0.4, 2.2 + fluff * 0.3, 2.4, 'C')   # throat
    cv.ellipse(mx - 0.5, my, 2.2, 2.2, 'B')                      # nape
    cv.head(hx, hy, eye, beak)
    if legs != 'none':
        cv.legs(bx + 0.8, leg_top, legs, near=True)
    if wing: cv.wing((bx + 1.6, by - 1.6), wing, angle=-(tilt + 18) * 0.6)
    if heart is not None: cv.sprite(HEART, heart, 24)
    if zed is not None: cv.sprite(ZED, zed, 22)

def hop(cv, phase):
    """Hopping along: crouched and pushing off, then up in the air with the feet tucked."""
    if phase == 0:
        stand(cv, legs='bent', body=(14.4, 12.0), head=(18.3, 7.8), tail=22)
    else:
        stand(cv, legs='tuck', lift=2.0, head=(18.0, 6.8), tail=42, wing='fold')

# four-beat wingbeat for flying across the taskbar (the 'Run' frames)
FLAP = [
    dict(wing='up', far='up', y=9.6, tail=4),
    dict(wing='level', far='level', y=10.2, tail=2),
    dict(wing='down', far='none', y=10.8, tail=0),
    dict(wing='high', far='high', y=10.0, tail=6),
]

def fly(cv, phase, glide=False):
    f = FLAP[phase] if not glide else dict(wing='level', far='level', y=10.0, tail=2)
    s = cv.s
    y = f['y']
    cv.far_wing((15.6, y - 1.4), f['far'])
    cv.tail((15.0 - s.get('bodyw', 5.0) * 0.8, y + 0.4), f['tail'])
    cv.legs(14.2, y + 2.4, 'tuck', near=False)
    cv.body(15.0, y, tilt=-4, ry=s.get('bodyh', 3.6) * 0.82)
    cv.ellipse(18.6, y - 1.0, 2.2, 2.2, 'C')
    cv.head(20.2, y - 2.0)
    cv.legs(15.2, y + 2.4, 'tuck')
    cv.wing((16.4, y - 1.2), f['wing'])

def flap_in_place(cv, phase):
    """Standing on the ground beating both wings (shaking out, or showing off)."""
    stand(cv, wing='spread' if phase == 0 else 'down', far='spread' if phase == 0 else 'none',
          head=(18.2, 6.6), tail=40 if phase == 0 else 26, lift=0.6 if phase == 0 else 0)

# --------------- the app's pose set ---------------
def sit(cv, **kw): stand(cv, **kw)

def nap(cv, breath):
    """Fluffed into a ball on the ground, head sunk into the shoulders."""
    s = cv.s
    cv.tail((10.6, 16.4), 6)
    cv.body(14.5, 15.0 - breath * 0.3, tilt=-6, fluff=0.9 + breath * 0.3)
    cv.head(18.0, 11.6 - breath * 0.3, 'closed')
    cv.wing((16.0, 13.4 - breath * 0.3), 'droop')

def loaf(cv, eye='open'):
    """Sitting down on the ground like a brooding hen: legs hidden, body low."""
    cv.tail((10.2, 15.6), 12)
    cv.body(14.5, 15.0, tilt=-10, fluff=0.4)
    cv.ellipse(17.2, 13.2, 2.3, 2.4, 'C')
    cv.head(18.4, 10.4, eye)
    cv.wing((16.4, 13.0), 'fold')

def curl(cv):
    """Asleep with the head turned back and tucked into the back feathers."""
    cv.tail((10.2, 15.8), 10)
    cv.body(14.5, 15.0, tilt=-8, fluff=1.0)
    cv.head(15.6, 11.4, 'closed', flip=True, tuck=True)
    cv.wing((16.2, 13.0), 'droop')

def drowsy(cv):
    stand(cv, eye='closed', fluff=0.7, head=(17.8, 7.8), wing='droop', tail=28)

def preen(cv, phase):
    """Grooming: head turned back, beak running through the wing feathers."""
    cv.legs(13.8, 13.6, 'stand', near=False)
    cv.tail((11.0, 13.2), 32)
    cv.body(14.4, 11.4, -24)
    cv.legs(15.2, 13.6, 'stand')
    cv.wing((16.0, 9.8), 'fold' if phase == 0 else 'stretch', angle=4)
    cv.head(15.0 if phase == 0 else 14.0, 7.6 if phase == 0 else 8.6, 'closed', flip=True)

def scratch(cv, phase):
    """Scratching the head with a foot brought up past the wing."""
    cv.legs(13.8, 13.6, 'stand', near=False)
    cv.tail((10.8, 13.0), 30)
    cv.body(14.4, 11.4, -30)
    cv.ellipse(16.8, 9.4, 2.2, 2.4, 'C')
    hy = 6.8 if phase == 0 else 7.4
    cv.head(18.0, hy, 'closed')
    foot = (19.0, 9.6) if phase == 0 else (18.8, 10.4)
    cv.line([(15.4, 13.2), (17.4, 12.4), foot], 1.15, 1.15, 'L')
    cv.wing((16.0, 9.6), 'fold', angle=7)

def peck(cv, phase, seeds=False):
    """Pecking at the ground: phase 0 head lowered to look, phase 1 beak on the ground."""
    cv.legs(11.8, 13.8, 'stand', near=False)
    cv.tail((8.8, 11.0), -12)
    cv.body(12.8, 12.4, tilt=16)
    if phase == 0:
        cv.ellipse(16.6, 13.2, 2.2, 2.2, 'C')
        cv.head(18.4, 13.2)
    else:
        cv.ellipse(16.8, 14.0, 2.2, 2.2, 'C')
        cv.head(18.6, 15.4)
    cv.legs(13.2, 13.8, 'stand')
    cv.wing((14.8, 10.8), 'fold', angle=-30)
    if seeds:
        for x in (21.6, 23.4, 25.0):
            cv.prop(SEED, x, 18.0)

def bat(cv):
    """Lunging forward with a peck at something in the air (the cat's paw bat)."""
    cv.legs(13.6, 13.8, 'stand', near=False)
    cv.tail((10.0, 12.2), 14)
    cv.body(14.6, 11.8, tilt=-6)
    cv.ellipse(18.4, 10.4, 2.3, 2.2, 'C')
    cv.head(20.6, 9.0, beak='open')
    cv.legs(15.0, 13.8, 'bent')
    cv.wing((16.4, 10.2), 'fold', angle=-6)

def startle(cv):
    """Alarmed: tall and thin, crest and wings half up, beak open."""
    stand(cv, beak='open', head=(17.4, 5.2), body=(14.6, 10.6), tilt=-46, wing='high', far='high', tail=52)

def lookup(cv):
    stand(cv, eye='up', beak='up', head=(17.3, 6.4), tail=38)

def stretch(cv):
    """The wing-and-leg stretch: one wing and one leg stretched down and back."""
    cv.legs(14.6, 13.6, 'stand', near=False)
    cv.tail((10.4, 13.2), 34)
    cv.body(14.4, 11.2, -30)
    cv.ellipse(16.8, 9.4, 2.2, 2.4, 'C')
    cv.head(17.9, 6.8, 'closed')
    cv.line([(13.8, 13.4), (11.6, 15.4), (9.4, 16.4)], 1.15, 1.15, 'L')
    cv.wing((15.4, 9.6), 'stretch')

def yawn(cv): stand(cv, eye='closed', beak='open', head=(17.8, 6.6))

def sit_tail(cv):
    """A tail flick: tail cocked up."""
    stand(cv, tail=-6)

def leap(cv):
    """Taking off: wings driving down, legs pushing off."""
    stand(cv, wing='up', far='up', lift=2.6, legs='dangle', head=(18.6, 6.4), tilt=-34, tail=44)

def land(cv):
    """Landing: wings swept forward to brake, legs reaching for the ground."""
    stand(cv, wing='fwd', far='fwd', legs='bent', head=(17.6, 7.0), tilt=-42, tail=50, body=(14.2, 11.6))

def crouch(cv, phase):
    """Getting ready to jump: low, wings slightly open, bobbing."""
    stand(cv, legs='bent', body=(14.4, 12.6 + phase * 0.4), head=(18.6, 8.4 + phase * 0.6), tilt=-14,
          tail=16, wing='fold')

def fall(cv, phase):
    stand(cv, wing='up' if phase == 0 else 'down', far='up' if phase == 0 else 'none', legs='dangle',
          lift=2.2, beak='open', head=(18.2, 6.4), tail=46 if phase == 0 else 24)

def dangle(cv):
    """Held up by the mouse: wings out, feet dangling, a bit ruffled."""
    stand(cv, wing='spread', far='spread', legs='dangle', lift=3.0, head=(17.8, 6.4), tilt=-50,
          body=(14.8, 11.6), tail=66, beak='open')

def held_belly(cv):
    stand(cv, wing='down', legs='dangle', lift=2.2, head=(18.2, 6.8), tilt=-38, tail=56, eye='closed')

def belly(cv, phase):
    """Sunbathing: flopped on one side, wing and tail fanned out, beak open."""
    s = cv.s
    cv.tail((10.0, 16.4), -4 + phase * 4, length=s.get('tail', 4.5) + 0.8)
    cv.body(14.6, 15.8, tilt=0, fluff=0.6, ry=s.get('bodyh', 3.6) * 0.8)
    cv.ellipse(18.6, 15.2, 2.2, 2.0, 'C')
    cv.head(20.0, 13.4, 'closed', beak='open' if phase else 'closed')
    cv.wing((16.6, 13.6), 'spread' if phase == 0 else 'high')

def petted(cv, heart):
    stand(cv, eye='closed', fluff=0.5, head=(17.8, 7.0), heart=heart, wing='droop', tail=30)

def sing(cv, phase):
    """Singing: head thrown back, beak open, notes floating up."""
    stand(cv, eye='closed', beak='open' if phase == 0 else 'up', head=(17.6, 6.0 if phase == 0 else 5.6),
          tail=42 - phase * 4, tilt=-38)
    cv.prop(NOTE if phase == 0 else NOTE2, 25.6, 4.6 if phase == 0 else 3.8)

def perch(cv, phase):
    """Perched on the top edge of a window: toes hooked over the edge, tail hanging down."""
    s = cv.s
    cv.tail((11.0, 13.4), 74 + phase * 6, length=s.get('tail', 4.5) + 1)
    cv.body(14.4, 11.8, -20)
    cv.ellipse(16.8, 9.8, 2.2, 2.4, 'C')
    cv.head(17.9, 7.2, 'open' if phase == 0 else 'closed')
    cv.line([(15.0, 14.6), (15.4, 16.4), (16.8, 17.6)], 1.15, 1.15, 'L')
    cv.wing((15.8, 10.0), 'fold', angle=0)

POSES = {
    'WalkA': lambda cv: hop(cv, 0), 'WalkB': lambda cv: hop(cv, 1),
    'Sit': lambda cv: stand(cv), 'SitBlink': lambda cv: stand(cv, eye='closed'),
    'NapA': lambda cv: (nap(cv, 0), cv.sprite(ZED, 3, 22)), 'NapB': lambda cv: (nap(cv, 1), cv.sprite(ZED, 2, 22)),
    'PettedA': lambda cv: petted(cv, 2), 'PettedB': lambda cv: petted(cv, 1),
    'Dangle': dangle, 'HeldBelly': held_belly, 'Leap': leap,
    'CrouchA': lambda cv: crouch(cv, 0), 'CrouchB': lambda cv: crouch(cv, 1),
    'ScratchA': lambda cv: scratch(cv, 0), 'ScratchB': lambda cv: scratch(cv, 1),
    'Bat': bat, 'LookUp': lookup,
    'FallA': lambda cv: fall(cv, 0), 'FallB': lambda cv: fall(cv, 1), 'Land': land,
    'Drowsy': drowsy, 'Stretch': stretch, 'Yawn': yawn, 'SitTail': sit_tail,
    'GroomA': lambda cv: preen(cv, 0), 'GroomB': lambda cv: preen(cv, 1),
    'Loaf': loaf, 'LoafBlink': lambda cv: loaf(cv, 'closed'),
    'BellyA': lambda cv: belly(cv, 0), 'BellyB': lambda cv: belly(cv, 1),
    'Startle': startle, 'Curl': curl,
    'RunA': lambda cv: fly(cv, 0), 'RunB': lambda cv: fly(cv, 1),
    'RunC': lambda cv: fly(cv, 2), 'RunD': lambda cv: fly(cv, 3),
}
# bird-only extras, not in the app's CatPose set yet
BIRD_POSES = {
    'PeckA': lambda cv: peck(cv, 0, True), 'PeckB': lambda cv: peck(cv, 1, True),
    'SingA': lambda cv: sing(cv, 0), 'SingB': lambda cv: sing(cv, 1),
    'FlapA': lambda cv: flap_in_place(cv, 0), 'FlapB': lambda cv: flap_in_place(cv, 1),
    'Glide': lambda cv: fly(cv, 0, glide=True),
    'PerchA': lambda cv: perch(cv, 0), 'PerchB': lambda cv: perch(cv, 1),
}
POSES.update(BIRD_POSES)
# the bird extras the app has (CatPose); gliding and perching are drawn but not used yet
APP_POSES = [p for p in POSES if p not in ('Glide', 'PerchA', 'PerchB')]

AIRBORNE = posekit.AIRBORNE | {'WalkB', 'FlapA', 'Glide', 'RunB', 'RunD'}

def pose_canvas(name, spec):
    """The pose, moved back into the frame. A big bird with its wings up and tail down can be taller than the
    frame, so then its wings fold in a little and its tail shortens until it fits."""
    for k in (1.0, 0.9, 0.8, 0.7, 0.6, 0.5, 0.4):
        smaller = {'wing': spec.get('wing', 1.0) * k, 'tail': spec.get('tail', 4.5) * (0.4 + 0.6 * k)}
        if spec.get('crest'):   # a startled crest flattens a little too
            smaller['crest'] = [(x, y * (0.5 + 0.5 * k)) for x, y in spec['crest']]
        cv = _pose_canvas(name, spec | smaller)
        o = posekit.overflow(cv)
        if not o['bottom'] or (o['bottom'] == 1 and name not in AIRBORNE):
            return cv
    return cv

def _pose_canvas(name, spec):
    cv = BirdCanvas(spec)
    POSES[name](cv)
    before = cv.headbox
    o = posekit.overflow(cv)
    dx = -o['right'] if o['right'] and not o['left'] else o['left'] if o['left'] and not o['right'] else 0
    dy = o['top'] if o['top'] else 0
    if dx or dy:
        px = {(r, c): cv.g[r][c] for r in range(H) for c in range(W) if cv.g[r][c] != '.'}
        px.update(cv.extra)
        cv.g = [['.'] * W for _ in range(H)]; cv.extra = {}
        for (r, c), ch in px.items(): cv.put(r + dy, c + dx, ch)
        r0, c0, h, w = cv.headbox
        cv.headbox = (r0 + dy, c0 + dx, h, w)
        cv.props = [(a, r + dy, c + dx) for a, r, c in cv.props]
    return cv
