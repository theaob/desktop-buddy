"""Dog buddies on the cat pose kit: same 30x20 frame, same 36 poses (so they map onto the app's CatPose set),
with dog heads (longer muzzles, a stop, dog ears), dog tails and panting while running.
Run from this folder: python3 dogkit.py <outdir>"""
import math, os, sys
import posekit
from posekit import W, H

# ---- heads, facing right. H head, G muzzle/face, E near ear, F far ear, e inner ear,
# Y eye, K pupil, M chin, n nose. Variants are derived from 'open' below.
DOG_HEADS = {
  'floppy': dict(neck=(5, 7), tongue=[(8, 7), (9, 7)], rows=[
      "...FF......",
      "..FHHHH....",
      ".HHHHHHH...",
      "EEHHHHHHH..",
      "EEEHHHYKH..",
      "EEEEHHHGGGn",
      "EEEEHHGGGGG",
      ".EEEHGMMM..",
      "..EE.GM....",
  ], yawn={5: "EEEEHHHGGGn", 6: "EEEEHHGmmm.", 7: ".EEEHGmoo..", 8: "..EE.GMMM.."}),
  'prick': dict(neck=(3, 7), tongue=[(8, 7), (9, 7)], rows=[
      ".F..E......",
      ".FF.EE.....",
      ".FFEeEE....",
      ".HHHeEHH...",
      "HHHHHGYKG..",
      "HHHHHHGGGGn",
      "HHHHHGGGGGG",
      ".HHHGGMMM..",
      "..HHGM.....",
  ], yawn={6: "HHHHHGGmmm.", 7: ".HHHGGmoo..", 8: "..HHGMMM..."}),
  'fold': dict(neck=(3, 6), tongue=[(8, 8), (9, 8)], rows=[
      "..FF.EEE...",
      ".HHHHHEEE..",
      ".HHHHHHHE..",
      "HHHHHHYKH..",
      "HHHHHHHGGGn",
      "HHHHHGGGGGG",
      ".HHHHGGGGG.",
      "..HHHGGGG..",
  ], yawn={5: "HHHHHGGmmm.", 6: ".HHHHGmoo..", 7: "..HHHGGGG.."}),
  'tipped': dict(neck=(3, 7), tongue=[(8, 7), (9, 7)], rows=[
      "..F..EE....",
      ".FF.EeEE...",
      ".FFEeEH....",
      ".HHHHHHH...",
      "HHHHHGYKG..",
      "HHHHHHGGGGn",
      "HHHHHGGGGGG",
      ".HHHGGMMM..",
      "..HHGM.....",
  ], yawn={6: "HHHHHGGmmm.", 7: ".HHHGGmoo..", 8: "..HHGMMM..."}),
  'square': dict(neck=(3, 6), tongue=[(8, 7), (9, 7)], rows=[
      "..FF..EE...",
      ".FHHHHEEE..",
      ".HHHHHHHH..",
      "HHHHHGYKH..",
      "HHHHHGGGGn.",
      "HHHHHGGGGG.",
      "HHHHHGMMMG.",
      ".HHHHGGGG..",
  ], yawn={5: "HHHHHGGmmm.", 6: "HHHHHGmoo..", 7: ".HHHHGMMM.."}),
  'longear': dict(neck=(5, 7), tongue=[(8, 7), (9, 7)], rows=[
      "...FF......",
      "..FHHHH....",
      ".HHHHHHH...",
      "EEHHHHHHH..",
      "EEEHHHYKH..",
      "EEEEHHHGGGn",
      "EEEEHHGGGGG",
      "EEEEHGMMM..",
      ".EEEEGM....",
      ".EEE.......",
  ], yawn={5: "EEEEHHHGGGn", 6: "EEEEHHGmmm.", 7: "EEEEHGmoo..", 8: ".EEEEGMMM.."}),
  'poodle': dict(neck=(5, 6), tongue=[(7, 8), (8, 8)], rows=[
      "..HHHH.....",
      ".HHHHHH....",
      ".HHHHHHH...",
      "EEEHHHYKH..",
      "EEEEHHHGGGG",
      "EEEEHHGGGGn",
      "EEEEEHGMMM.",
      "EEEEE.GM...",
      ".EEE.......",
  ], yawn={5: "EEEEHHGGGGn", 6: "EEEEEHGmmm.", 7: "EEEEE.Gmoo.", 8: ".EEE..GMM.."}),
}

# where a bone sits in each head template's mouth (row, col)
MOUTH = {'floppy': (7, 7), 'prick': (7, 7), 'fold': (6, 8), 'tipped': (7, 7), 'square': (6, 7), 'longear': (7, 7), 'poodle': (6, 8)}

def _variants(t):
    rows = t['rows']
    closed = [r.replace('Y', 'x').replace('K', 'x') for r in rows]
    up = [list(r) for r in rows]
    for i, r in enumerate(rows):
        for j, ch in enumerate(r):
            if ch in 'YK' and i > 0:
                up[i][j] = 'G' if 'G' in r[max(0, j - 1):j + 2] else 'H'
                up[i - 1][j] = ch
    up = [''.join(r) for r in up]
    yawn = list(closed)
    for i, r in t['yawn'].items():
        while len(yawn) <= i: yawn.append('.' * 11)
        yawn[i] = r
    def tongue(base):
        g = [list(r.ljust(11, '.')) for r in base]
        for i, j in t['tongue']:
            while len(g) <= i: g.append(['.'] * 11)
            g[i][j] = 'o'
        return [''.join(r) for r in g]
    return {'open': rows, 'closed': closed, 'up': up, 'yawn': yawn,
            'pant': tongue(rows), 'happy': tongue(closed)}

for name, t in DOG_HEADS.items():
    posekit.HEADS[name] = dict(neck=t['neck'], rows=_variants(t))

# ---- tails: each dog reshapes the cat's tail stroke from its root
def rot(v, deg):
    a = math.radians(deg)
    return (v[0] * math.cos(a) - v[1] * math.sin(a), v[0] * math.sin(a) + v[1] * math.cos(a))

TAILS = {
    'otter':  dict(scale=1.0, turn=-38, w=(2.8, 1.5)),   # thick, carried low and level
    'whip':   dict(scale=1.0, turn=-22, w=(2.1, 1.3)),   # thin and long
    'carrot': dict(scale=0.55, turn=12, w=(2.2, 1.6)),    # short and upright
    'curl':   dict(scale=0.8, turn=0, w=(2.5, 1.5)),      # curls over the back when raised
    'stub':   dict(scale=0.3, turn=0, w=(2.6, 2.2)),      # a little bobbed tail
    'pom':    dict(scale=0.5, turn=18, w=(1.8, 1.4)),     # short and upright, with a pompom
    'brush':  dict(scale=0.95, turn=-58, w=(2.7, 1.7)),   # long, bushy, carried low
}

# ---- the bone: 'w' bone, drawn over the dog after the pose is fitted
BONE = ["ww....ww", ".wwwwww.", "ww....ww"]
BONE_UP = ["w.w", "www", ".w.", ".w.", ".w.", "www", "w.w"]
BONE_TILT = ["ww.....", "www....", "..ww...", "...ww..", "....www", ".....ww"]

class DogCanvas(posekit.Canvas):
    def __init__(self, kind, head, tail, **kw):
        super().__init__(kind, head=head, **kw)
        self.tail, self.panting = TAILS[tail] | {'name': tail}, False
        self.props = []      # (art, row, col) drawn on top, moved along with the pose
        self.bite = False    # a bone in the mouth
    def line(self, pts, w0, w1=None, ch='T', under=False):
        if ch != 'T':
            return super().line(pts, w0, w1, ch, under)
        t, root = self.tail, pts[0]
        tip = pts[-1]
        if t['name'] == 'curl' and root[1] - tip[1] > 4:
            new = [root, (root[0] - 1.6, root[1] - 2.4), (root[0] - 0.6, root[1] - 4.6),
                   (root[0] + 1.6, root[1] - 5.0), (root[0] + 2.8, root[1] - 3.9)]
        else:
            # a tail lying along the ground stays there; a raised one is carried at the dog's own angle
            turn = t['turn'] if root[1] - min(y for _, y in pts) > 2.5 else 0
            new = [root] + [tuple(a + b for a, b in zip(root, rot(((x - root[0]) * t['scale'], (y - root[1]) * t['scale']), turn)))
                            for x, y in pts[1:]]
        new = [(max(2.6, x), min(17.4, y)) for x, y in new]   # never past the frame edge or into the ground
        super().line(new, *t['w'], 'T', under)
        if t['name'] == 'pom':
            self.ellipse(*new[-1], 1.9, 1.9, 'T', under=under)
    def head(self, nx, ny, variant='open', flip=False, dx=0):
        if self.panting and variant == 'open': variant = 'pant'
        if getattr(self, 'force', None): variant = self.force
        # dog muzzles are longer than cat noses: pull the head back onto the neck rather than past the frame
        t = posekit.HEADS[self.headset]
        right = round(self.X(nx)) - t['neck'][0] + dx + max(map(len, t['rows'][variant])) - 1
        dx -= max(0, right - (W - 2) + (2 if self.bite else 0))
        super().head(nx, ny, variant, flip, dx)
        if self.bite:
            r0, c0, *_ = self.headbox
            mr, mc = MOUTH[self.headset]
            self.props.append((BONE, r0 + mr - 1, c0 + mc - 3))
    def prop(self, art, x, y):
        """A prop centred on a point in pose coordinates."""
        self.props.append((art, round(self.Y(y)) - len(art) // 2, round(self.X(x)) - len(art[0]) // 2))

def belly(cv, phase):
    """The cat's belly-up pose, with the dog's head always rolled upside down."""
    cv.line([(6, 16.5), (3.5, 17.5), (2.2, 17)], 2.2, 1.6, 'T')
    cv.ellipse(13.5, 16, 8.0, 2.6, 'B')
    cv.ellipse(14, 14.4, 5.5, 1.1, 'C')
    a = 0 if phase == 0 else 1.2
    cv.leg([(8.5, 14.5), (7.5 - a, 12), (8.5 - a, 10.5)])
    cv.leg([(10.5, 14.5), (11 + a, 12), (10 + a, 10.8)], near=False)
    cv.leg([(17.5, 14.5), (18.5 - a, 11.8), (17.5 - a, 10.5)], near=False)
    cv.leg([(19.5, 14.5), (20.5 + a, 12), (21.5 + a, 11)])
    cv.head(21.8, 12.5 if cv.kind == 'adult' else 10.0, 'happy', flip=True)

def panting(fn):
    def go(cv):
        cv.panting = True
        fn(cv)
    return go

def biting(fn):
    def go(cv):
        cv.bite = True
        fn(cv)
    return go

def chew(cv, phase):
    """Lying down with the bone between the front paws, gnawing on it."""
    cv.prop(BONE_TILT, 24.5, 16.2 if phase == 0 else 16.6)
    posekit.lying(cv, eyes='closed' if phase else 'open', head=(21.2, 14.2 if phase == 0 else 14.8))

def toss(cv):
    posekit.sitting(cv, eyes='up', head=(16.5, 7.6))
    r0, c0, *_ = cv.headbox
    cv.props.append((BONE_UP, 1, c0 + 10))   # tossed up, just in front of the nose

def bow(cv):
    """Play bow (the stretch, eyes open) with the bone in the mouth: come and get it."""
    cv.force, cv.bite = 'open', True
    posekit.stretch(cv)

POSES = dict(posekit.POSES)
POSES.update({
    'BellyA': lambda cv: belly(cv, 0), 'BellyB': lambda cv: belly(cv, 1),
    'PettedA': lambda cv: posekit.sitting(cv, eyes='happy', heart=2),
    'PettedB': lambda cv: posekit.sitting(cv, eyes='happy', heart=1),
    **{k: panting(posekit.POSES[k]) for k in ('RunA', 'RunB', 'RunC', 'RunD')},
})
# new, dog-only poses with the bone
BONE_POSES = {
    'CarryA': biting(lambda cv: posekit.walk(cv, 0)), 'CarryB': biting(lambda cv: posekit.walk(cv, 1)),
    'ChewA': lambda cv: chew(cv, 0), 'ChewB': lambda cv: chew(cv, 1),
    'Toss': toss, 'Catch': biting(lambda cv: posekit.sitting(cv)), 'PlayBow': bow,
}
POSES.update(BONE_POSES)

def pose_canvas(name, kind, head, tail, **kw):
    cv = DogCanvas(kind, head, tail, **kw)
    POSES[name](cv)
    before = cv.headbox
    posekit.fit(cv, name)
    dr, dc = cv.headbox[0] - before[0], cv.headbox[1] - before[1]
    cv.props = [(a, r + dr, c + dc) for a, r, c in cv.props]
    if cv.props:   # slide the whole picture so the bone (and its outline) stays in the frame
        cols = [c + j for a, r, c in cv.props for j in range(len(a[0]))]
        rows = [r + i for a, r, c in cv.props for i in range(len(a))]
        sx = min(0, W - 2 - max(cols)) or max(0, 1 - min(cols))
        sy = min(0, H - 2 - max(rows)) or max(0, 1 - min(rows))
        if sx or sy:
            px = {(r, c): cv.g[r][c] for r in range(H) for c in range(W) if cv.g[r][c] != '.'}
            cv.g = [['.'] * W for _ in range(H)]
            for (r, c), ch in px.items():
                cv.put(r + sy, c + sx, ch)
            cv.props = [(a, r + sy, c + sx) for a, r, c in cv.props]
    return cv

def pose_grid(name, kind, head, tail, **kw):
    return pose_canvas(name, kind, head, tail, **kw).g
