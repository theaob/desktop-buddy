"""Full pose set for the realistic cats, at the app's 30x20 sprite size.
Bodies, legs and tails are drawn from simple shapes; heads are hand-drawn pixel templates.
Each pose is a grid of body-part regions; real.py's coats and shading colour it."""
import math

W, H = 30, 20
PAD = 8         # shapes are drawn this far past the frame, so a pose that spills over can be moved back in
GROUND = 18.5   # bottom edge of the paws

# Head templates, facing right. G = face (mask area), x = closed eyelid, m = open mouth, o = tongue.
HEADS = {
  'adult': dict(neck=(3, 6), rows={
    'open': [
      "..F..E....",
      ".FF.EeE...",
      ".HHHHeEH..",
      ".HHHHGGG..",
      "HHHHGYKGG.",
      "HHHHGGGMMn",
      "HHHHGGMM..",
      ".HHHGM....",
    ],
    'closed': [
      "..F..E....",
      ".FF.EeE...",
      ".HHHHeEH..",
      ".HHHHGGG..",
      "HHHHGxxGG.",
      "HHHHGGGMMn",
      "HHHHGGMM..",
      ".HHHGM....",
    ],
    'up': [
      "..F..E....",
      ".FF.EeE...",
      ".HHHHeEH..",
      ".HHHHYKG..",
      "HHHHGGGGG.",
      "HHHHGGGMMn",
      "HHHHGGMM..",
      ".HHHGM....",
    ],
    'yawn': [
      "..F..E....",
      ".FF.EeE...",
      ".HHHHeEH..",
      ".HHHHGGG..",
      "HHHHGxxGG.",
      "HHHHGGGMMn",
      "HHHHGGmmm.",
      ".HHHGmoo..",
      ".HHHGMM...",
    ],
  }),
  'kitten': dict(neck=(2, 7), rows={
    'open': [
      "..F...E....",
      ".FF..EeE...",
      ".HHHHHeEH..",
      "HHHHHHGGGG.",
      "HHHHHGYYKG.",
      "HHHHHGGGMMn",
      "HHHHHGGMMM.",
      ".HHHHGGMM..",
    ],
    'closed': [
      "..F...E....",
      ".FF..EeE...",
      ".HHHHHeEH..",
      "HHHHHHGGGG.",
      "HHHHHGxxxG.",
      "HHHHHGGGMMn",
      "HHHHHGGMMM.",
      ".HHHHGGMM..",
    ],
    'up': [
      "..F...E....",
      ".FF..EeE...",
      ".HHHHHeEH..",
      "HHHHHGYYKG.",
      "HHHHHGGGGG.",
      "HHHHHGGGMMn",
      "HHHHHGGMMM.",
      ".HHHHGGMM..",
    ],
    'yawn': [
      "..F...E....",
      ".FF..EeE...",
      ".HHHHHeEH..",
      "HHHHHHGGGG.",
      "HHHHHGxxxG.",
      "HHHHHGGGMMn",
      "HHHHHGGmmm.",
      ".HHHHGmoo..",
      ".HHHHGMM...",
    ],
  }),
}

HEART = ["h.h", "hhh", ".h."]
ZED = ["zzz", ".z.", "zzz"]

class Canvas:
    def __init__(self, kind, head=None, chunk=1.0, legw=1.0, accessory=None):
        self.kind = kind
        self.headset = head or kind
        self.chunk, self.legw, self.accessory = chunk, legw, accessory
        self.k = 1.0 if kind == 'adult' else 0.78
        self.g = [['.'] * W for _ in range(H)]
        self.extra = {}   # pixels drawn past the frame edge
    # pose coordinates are written for the adult; the kitten is a scaled-down copy on the same ground
    def X(self, x): return 15 + (x - 15) * self.k
    def Y(self, y): return GROUND - (GROUND - y) * self.k
    def put(self, r, c, ch, under=False):
        if 0 <= r < H and 0 <= c < W:
            if not under or self.g[r][c] == '.':
                self.g[r][c] = ch
        elif not under or (r, c) not in self.extra:
            self.extra[(r, c)] = ch
    def ellipse(self, cx, cy, rx, ry, ch, angle=0.0, under=False):
        if ch in 'BJ': ry *= self.chunk
        cx, cy, rx, ry = self.X(cx), self.Y(cy), rx * self.k, ry * self.k
        ca, sa = math.cos(math.radians(angle)), math.sin(math.radians(angle))
        for r in range(-PAD, H + PAD):
            for c in range(-PAD, W + PAD):
                dx, dy = c + 0.5 - cx, r + 0.5 - cy
                u, v = dx * ca + dy * sa, -dx * sa + dy * ca
                if (u / rx) ** 2 + (v / ry) ** 2 <= 1:
                    self.put(r, c, ch, under)
    def line(self, pts, w0, w1=None, ch='T', under=False):
        w1 = w0 if w1 is None else w1
        pts = [(self.X(x), self.Y(y)) for x, y in pts]
        segs = list(zip(pts, pts[1:]))
        total = sum(math.dist(a, b) for a, b in segs) or 1
        for r in range(-PAD, H + PAD):
            for c in range(-PAD, W + PAD):
                p = (c + 0.5, r + 0.5)
                run = 0
                for a, b in segs:
                    L = math.dist(a, b) or 1e-6
                    t = max(0, min(1, ((p[0] - a[0]) * (b[0] - a[0]) + (p[1] - a[1]) * (b[1] - a[1])) / L ** 2))
                    q = (a[0] + t * (b[0] - a[0]), a[1] + t * (b[1] - a[1]))
                    w = (w0 + (w1 - w0) * (run + t * L) / total) * max(self.k, 0.85)
                    if math.dist(p, q) <= w / 2:
                        self.put(r, c, ch, under); break
                    run += L
    def leg(self, pts, near=True, w=2.2):
        self.line(pts, w * self.legw, w * 0.9 * self.legw, 'L' if near else 'N')
    def head(self, nx, ny, variant='open', flip=False, dx=0):
        t = HEADS[self.headset]
        rows = t['rows'][variant]
        if flip: rows = rows[::-1]
        ax, ay = t['neck']
        if flip: ay = len(rows) - 1 - ay
        c0, r0 = round(self.X(nx)) - ax + dx, round(self.Y(ny)) - ay
        self.headbox = (r0, c0, len(rows), max(len(row) for row in rows))
        for i, row in enumerate(rows):
            for j, ch in enumerate(row):
                if ch != '.':
                    self.put(r0 + i, c0 + j, ch)
        if self.accessory and not flip:
            art, (ar, ac) = self.accessory
            if r0 + ar + len(art) <= H - 1:   # leave it off when the head is too low for it to show
                self.sprite(art, r0 + ar, c0 + ac)
    def sprite(self, art, r0, c0):
        for i, row in enumerate(art):
            for j, ch in enumerate(row):
                if ch != '.':
                    self.put(r0 + i, c0 + j, ch)

# ---------------- poses ----------------
def standing(cv, legs, tail, eyes='open', body_y=10.6, head=(21.5, 8.2), arch=0, lift=0):
    y = body_y - lift
    for pts in legs['far']: cv.leg([(a, b - lift) for a, b in pts], near=False)
    cv.line([(a, b - lift) for a, b in tail], 2.2, 1.4, 'T')
    cv.ellipse(13.5, y - arch * 0.5, 8.0, 2.9 + arch * 0.5, 'B')
    cv.ellipse(14.5, y + 2.0, 5.5, 0.9, 'C')
    cv.ellipse(8.2, y + 0.2, 3.0, 2.6, 'J')
    cv.ellipse(19.8, y + 0.8, 2.2, 2.3, 'C')
    cv.line([(19.0, y - 1.0), (head[0], head[1] - lift)], 3.6, 3.2, 'B')
    for pts in legs['near']: cv.leg([(a, b - lift) for a, b in pts], near=True)
    cv.head(head[0], head[1] - lift, eyes)

def walk_legs(phase):
    if phase == 0:
        return dict(far=[[(9.5, 12), (10.5, 15.5), (10.5, 18)], [(18, 12), (17.5, 15.5), (16.5, 18)]],
                    near=[[(7.5, 12), (6, 15.5), (5, 18)], [(20, 12), (20.5, 15.5), (21.5, 18)]])
    return dict(far=[[(7.5, 12), (6, 15.5), (4.5, 18)], [(20, 12), (21, 15.5), (22.5, 18)]],
                near=[[(9, 12), (10.5, 15.5), (11, 18)], [(18.5, 12), (18, 15.5), (17, 18)]])

WALK_TAIL = [(5.5, 9.5), (3, 8), (2, 5.5), (2.5, 3.5)]

def walk(cv, phase): standing(cv, walk_legs(phase), WALK_TAIL)

def run(cv, phase):
    if phase == 0:   # stretched out
        legs = dict(far=[[(8, 12), (5, 14.5), (2.5, 16)], [(20, 11.5), (23.5, 13.5), (26, 15)]],
                    near=[[(9, 12), (6, 15), (3.5, 17.5)], [(19, 12), (22.5, 15), (25, 17.5)]])
        standing(cv, legs, [(5.5, 9.5), (3, 9), (1, 8)], body_y=10.8, head=(22.5, 8.6))
    else:            # gathered under
        legs = dict(far=[[(9, 12), (12, 15), (14, 17)], [(18, 12), (16, 15), (13.5, 16.5)]],
                    near=[[(8, 12), (11, 15.5), (13, 18)], [(19, 12), (17, 15.5), (15, 18)]])
        standing(cv, legs, [(5.5, 9.5), (3, 7.5), (1.5, 6.5)], body_y=10.4, arch=1, head=(21.5, 8.2))

def held_belly(cv):
    legs = dict(far=[[(9, 12), (9.5, 15), (10, 17)], [(18.5, 12), (18.5, 15), (19, 17)]],
                near=[[(7.5, 12), (7.5, 15), (7.5, 17.5)], [(20, 12), (20.5, 15), (20.5, 17.5)]])
    standing(cv, legs, [(5.5, 10), (4, 13), (3.5, 16)], lift=1)

def startle(cv):
    legs = dict(far=[[(9, 11), (9.5, 15), (10, 18)], [(18, 11), (18, 15), (18, 18)]],
                near=[[(7.5, 11), (7, 15), (6.5, 18)], [(19.5, 11), (20, 15), (20.5, 18)]])
    standing(cv, legs, [(5.5, 8.5), (4, 6), (3.5, 3.5), (3.5, 2.2)], body_y=9.6, arch=2, head=(21.5, 7.6))
    cv.line([(4.3, 7), (3.6, 2.4)], 3.4, 2.8, 'T')   # bristled tail

def fall(cv, phase):
    if phase == 0:
        legs = dict(far=[[(9, 11.5), (11, 14), (12.5, 16.5)], [(18.5, 11.5), (21, 13), (23.5, 13.5)]],
                    near=[[(7.5, 11.5), (5, 14), (3, 15)], [(19.5, 11.5), (22, 14.5), (24, 16.5)]])
        standing(cv, legs, [(5.5, 9), (3, 6.5), (3.5, 3.5)], body_y=10.2, head=(21.5, 7.8))
    else:
        legs = dict(far=[[(9, 11.5), (7, 14.5), (5, 16.5)], [(18.5, 11.5), (20, 14.5), (21, 17)]],
                    near=[[(7.5, 11.5), (9, 14.5), (10, 17)], [(19.5, 11.5), (22.5, 12.5), (25, 12)]])
        standing(cv, legs, [(5.5, 9), (2.5, 8), (1.5, 5)], body_y=10.2, head=(21.5, 7.8))

def leap(cv):
    cv.leg([(9, 12), (6, 14), (3.5, 15)], near=False)
    cv.line([(6, 10.5), (3.5, 10), (2, 9)], 2.2, 1.4, 'T')
    cv.ellipse(14, 10.5, 8.2, 2.7, 'B', angle=-14)
    cv.ellipse(15, 12, 5, 0.9, 'C', angle=-14)
    cv.ellipse(8.5, 11.8, 3, 2.5, 'J')
    cv.ellipse(20.5, 8.6, 2.2, 2.2, 'C')
    cv.leg([(20, 8.5), (23.5, 9.5), (26.5, 10.5)], near=False)
    cv.leg([(8, 12.5), (5.5, 15), (3, 16.3)])
    cv.leg([(20.5, 9.5), (24, 11.5), (27, 12.5)])
    cv.head(22.5, 6.5)

def dangle(cv):
    cv.leg([(17, 15), (17.5, 16.8), (18, 17.4)], near=False)
    cv.line([(14.5, 15.5), (14, 16.8), (13.5, 17.4)], 2, 1.4, 'T')
    cv.ellipse(16, 12.8, 3.0, 4.0, 'B')
    cv.ellipse(17.2, 13, 1.6, 3.0, 'C')
    cv.ellipse(15.5, 15, 2.6, 2.0, 'J')
    cv.leg([(18, 10.8), (19, 13.2), (19.5, 15)], near=False)
    cv.leg([(17.8, 10.8), (18.5, 13.5), (18.5, 15.5)])
    cv.leg([(16, 16), (16.5, 17), (16.5, 17.4)])
    cv.head(15.5, 7.8)

# sitting family
def sitting(cv, eyes='open', tail='ground', head=(16.5, 8.3), front='down', hind='down', heart=None):
    if tail == 'ground':
        cv.line([(10, 17.8), (5, 17.9), (2, 17.6), (1.3, 16)], 2.2, 1.6, 'T')
    elif tail == 'up':
        cv.line([(10, 17.8), (5, 17.5), (2.5, 15.5), (2.3, 12.5)], 2.2, 1.6, 'T')
    cv.leg([(20.6, 12.5), (20.8, 15.5), (21.2, 18)], near=False, w=2.0)
    cv.ellipse(13.2, 13.5, 4.8, 5.2, 'B')
    cv.ellipse(17.2, 12.2, 3.0, 4.4, 'C')
    if hind == 'down':
        cv.ellipse(12.6, 15.4, 4.2, 3.0, 'J')
        cv.line([(15, 17.6), (18, 17.8)], 1.8, 1.8, 'L')
    elif hind in ('scratchA', 'scratchB'):
        cv.ellipse(12.6, 15.4, 4.2, 3.0, 'J')
        tip = (14.5, 6.5) if hind == 'scratchA' else (15, 7.8)
        cv.line([(13.5, 14), (11, 9.5), tip], 2.2, 2.0, 'L')
    if front == 'down':
        cv.leg([(19, 12), (19.3, 15.5), (19.5, 18)], w=2.2)
    elif front == 'bat':
        cv.leg([(18.8, 12.5), (22, 11.5), (25, 10.5)], w=2.2)
    elif front in ('groomA', 'groomB'):
        tip = (21.5, 9) if front == 'groomA' else (21, 10.5)
        cv.leg([(18.8, 13), (21, 12.5), tip], w=2.2)
    cv.line([(14.5, 10), head], 3.6, 3.2, 'B')
    hx, hy = head
    cv.head(hx, hy, eyes)
    if front in ('groomA', 'groomB'):
        cv.leg([(20.5, 11.5), (21.5, 9.5) if front == 'groomA' else (21, 10.5)], w=2.0)
    if heart is not None:
        cv.sprite(HEART, heart, 24 if cv.kind == 'adult' else 22)

def groom(cv, phase):
    sitting(cv, eyes='closed', head=(16.5, 9.3), front='groomA' if phase == 0 else 'groomB')

# lying family
def lying(cv, eyes='open', head=(21.5, 13.6), breath=0.0, tail='back', rump=0.0, paws=True, zed=None):
    if tail == 'back':
        cv.line([(6, 16.5), (3.5, 17.5), (2.2, 17.6)], 2.2, 1.6, 'T')
    elif tail == 'up':
        cv.line([(5.5, 13), (3.5, 10), (3, 7), (4, 5)], 2.2, 1.6, 'T')
    cv.ellipse(13.5, 15.6 - breath * 0.5 - rump * 0.4, 8.3, 2.8 + breath * 0.5, 'B')
    cv.ellipse(8, 15.5 - rump, 3.2, 2.5, 'J')
    cv.ellipse(15, 17.6, 5.5, 0.8, 'C')
    if paws:
        cv.line([(19, 17.6), (23.5, 17.8)], 1.8, 1.8, 'L')
    hx, hy = head
    cv.line([(18.5, 15), (hx, hy)], 3.6, 3.2, 'B')
    cv.head(hx, hy, eyes)
    if zed is not None:
        cv.sprite(ZED, zed, 23 if cv.kind == 'adult' else 21)

def loaf(cv, eyes='open'):
    cv.line([(7, 17.6), (12, 17.9), (14.5, 17.8)], 2.0, 1.6, 'T')
    cv.ellipse(13.2, 14.6, 7.5, 3.9, 'B')
    cv.ellipse(9.2, 15.2, 3.3, 2.8, 'J')
    cv.ellipse(19.2, 14.8, 2.2, 3.2, 'C')
    cv.line([(18.5, 17.7), (21.5, 17.8)], 1.8, 1.8, 'L')
    cv.line([(17.5, 12.5), (19.5, 10.5)], 3.6, 3.2, 'B')
    cv.head(19.5, 10.5, eyes)

def curl(cv):
    cv.ellipse(13.8, 15.2, 7.0, 3.6, 'B')
    cv.ellipse(10, 15, 3.4, 2.6, 'J')
    cv.head(17.5, 15.6, 'closed')
    cv.line([(7, 18), (13, 18.2), (19, 18), (22, 17.2)], 2.2, 1.6, 'T')

def crouch(cv, phase):
    cv.leg([(17.5, 15.5), (19.5, 17.5), (21.5, 18)], near=False, w=2.0)
    cv.line([(5.5, 14.5), (3, 15), (2, 14 if phase == 0 else 15.5)], 2.2, 1.4, 'T')
    cv.ellipse(13.2, 15.6 - (0.6 if phase else 0), 8.2, 2.4, 'B', angle=3 if phase else 0)
    cv.ellipse(8, 15.0 - (0.8 if phase else 0), 3.2, 2.6, 'J')
    cv.ellipse(9, 17.6, 2.6, 0.9, 'L')
    cv.line([(19, 17.6), (24, 17.8)], 1.8, 1.8, 'L')
    cv.line([(18.5, 15), (22, 14)], 3.6, 3.2, 'B')
    cv.head(22, 14)

def land(cv):
    cv.leg([(9, 16), (11.5, 17.8)], near=False, w=2.0)
    cv.line([(5.5, 15), (3, 13.5), (2, 11)], 2.2, 1.4, 'T')
    cv.ellipse(13.5, 15.8, 8.0, 2.4, 'B')
    cv.ellipse(8, 15.6, 3.2, 2.4, 'J')
    cv.leg([(7, 16.5), (4.5, 18)])
    cv.leg([(19, 16), (17.5, 18)], near=False, w=2.0)
    cv.leg([(20, 16), (23, 18)])
    cv.line([(18.5, 15), (21.5, 13.5)], 3.6, 3.2, 'B')
    cv.head(21.5, 13.5)

def stretch(cv):
    cv.leg([(9, 12), (9.5, 15), (10, 18)], near=False)
    cv.line([(6, 10), (4, 7), (3.5, 4), (4.5, 2)], 2.2, 1.6, 'T')
    cv.ellipse(13.5, 12.8, 8.0, 2.7, 'B', angle=22)
    cv.ellipse(8.5, 11.4, 3, 2.6, 'J')
    cv.ellipse(18.5, 15.6, 2.0, 1.8, 'C')
    cv.leg([(7.5, 12), (7, 15), (6.5, 18)])
    cv.line([(18, 17.4), (25.5, 17.8)], 2.0, 1.8, 'N')
    cv.line([(18.5, 17.8), (26.5, 18)], 2.0, 1.8, 'L')
    cv.line([(18, 15), (21, 15)], 3.4, 3.2, 'B')
    cv.head(21, 15.5, 'closed')

def drowsy(cv):
    lying(cv, eyes='closed', head=(21.5, 14.6))

def belly(cv, phase):
    cv.line([(6, 16.5), (3.5, 17.5), (2.2, 17)], 2.2, 1.6, 'T')
    cv.ellipse(13.5, 16, 8.0, 2.6, 'B')
    cv.ellipse(14, 14.4, 5.5, 1.1, 'C')
    a = 0 if phase == 0 else 1.2
    cv.leg([(8.5, 14.5), (7.5 - a, 12), (8.5 - a, 10.5)])
    cv.leg([(10.5, 14.5), (11 + a, 12), (10 + a, 10.8)], near=False)
    cv.leg([(17.5, 14.5), (18.5 - a, 11.8), (17.5 - a, 10.5)], near=False)
    cv.leg([(19.5, 14.5), (20.5 + a, 12), (21.5 + a, 11)])
    if cv.headset in ('adult', 'kitten', 'wisp'):
        cv.head(21.8, 11.5 if cv.kind == 'adult' else 9.0, 'closed', flip=True)
    else:   # a front-facing head just rests on the ground
        cv.head(21.5, 17.6, 'closed')

POSES = {
    'WalkA': lambda cv: walk(cv, 0), 'WalkB': lambda cv: walk(cv, 1),
    'Sit': lambda cv: sitting(cv), 'SitBlink': lambda cv: sitting(cv, eyes='closed'),
    'NapA': lambda cv: lying(cv, eyes='closed', head=(21.5, 14.6), zed=3),
    'NapB': lambda cv: lying(cv, eyes='closed', head=(21.5, 14.6), breath=0.8, zed=2),
    'PettedA': lambda cv: sitting(cv, eyes='closed', heart=2), 'PettedB': lambda cv: sitting(cv, eyes='closed', heart=1),
    'Dangle': dangle, 'HeldBelly': held_belly, 'Leap': leap,
    'CrouchA': lambda cv: crouch(cv, 0), 'CrouchB': lambda cv: crouch(cv, 1),
    'ScratchA': lambda cv: sitting(cv, eyes='closed', hind='scratchA'),
    'ScratchB': lambda cv: sitting(cv, eyes='closed', hind='scratchB'),
    'Bat': lambda cv: sitting(cv, front='bat'),
    'LookUp': lambda cv: sitting(cv, eyes='up', head=(16.5, 7.6)),
    'FallA': lambda cv: fall(cv, 0), 'FallB': lambda cv: fall(cv, 1), 'Land': land,
    'Drowsy': drowsy, 'Stretch': stretch, 'Yawn': lambda cv: sitting(cv, eyes='yawn'),
    'SitTail': lambda cv: sitting(cv, tail='up'),
    'GroomA': lambda cv: groom(cv, 0), 'GroomB': lambda cv: groom(cv, 1),
    'Loaf': loaf, 'LoafBlink': lambda cv: loaf(cv, 'closed'),
    'BellyA': lambda cv: belly(cv, 0), 'BellyB': lambda cv: belly(cv, 1),
    'Startle': startle, 'Curl': curl,
    'RunA': lambda cv: run(cv, 0), 'RunB': lambda cv: run(cv, 1),
}

# Poses where the cat is off the ground, so it can be moved down to keep its ears in the frame.
AIRBORNE = {'Leap', 'FallA', 'FallB', 'Dangle', 'HeldBelly'}

def overflow(cv):
    """How far the cat (not hearts or sleep marks) spills past a frame that leaves one pixel for the outline."""
    pts = [(r, c) for r in range(H) for c in range(W) if cv.g[r][c] not in '.hz']
    pts += [p for p, ch in cv.extra.items() if ch not in 'hz']
    rs, cs = [r for r, _ in pts], [c for _, c in pts]
    return dict(top=max(0, 1 - min(rs)), bottom=max(0, max(rs) - (H - 2)),
                left=max(0, 1 - min(cs)), right=max(0, max(cs) - (W - 2)))

def fit(cv, name):
    """Moves a pose that spills over one side back into the frame, keeping grounded cats on the ground."""
    o = overflow(cv)
    dx = 0
    if o['right'] and not o['left']:
        dx = -o['right']
    elif o['left'] and not o['right']:
        dx = o['left']
    dy = o['top'] if name in AIRBORNE and o['top'] else 0
    if not (dx or dy):
        return
    pixels = {(r, c): cv.g[r][c] for r in range(H) for c in range(W) if cv.g[r][c] != '.'}
    pixels.update(cv.extra)
    cv.g = [['.'] * W for _ in range(H)]
    cv.extra = {}
    for (r, c), ch in pixels.items():
        cv.put(r + dy, c + dx, ch)
    r0, c0, h, w = cv.headbox
    cv.headbox = (r0 + dy, c0 + dx, h, w)

def pose_canvas(name, kind, **kw):
    cv = Canvas(kind, **kw)
    POSES[name](cv)
    fit(cv, name)
    return cv

def pose_grid(name, kind, **kw):
    return pose_canvas(name, kind, **kw).g
