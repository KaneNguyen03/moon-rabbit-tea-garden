import subprocess, sys, os, json
import numpy as np
from PIL import Image
from scipy import ndimage as ndi

SHEET = '/tmp/agy-art/sheet-129-loi.png'
CUT = '/Users/hiquyennek/Documents/VuonTraNam/docs/anh/po-do-vat/cat-luoi-cung-ti-le.py'
RAW = '/tmp/mistfix/loi-raw'
OUT = sys.argv[1]
SIDES = {'loi_thang': 'lr', 'loi_cong': 'lb', 'loi_nga-ba': 'lrb', 'loi_dau': 'l'}
BG = np.array([0x2E, 0x5E, 0x49])
INK = np.array([0x3A, 0x2A, 0x4D])
K = 64

subprocess.run([sys.executable, CUT, SHEET, RAW, '2', '2', 'loi_thang,loi_cong,loi_nga-ba,loi_dau'], check=True, capture_output=True)


def defringe(a):
    for _ in range(4):
        rgb, al = a[:, :, :3], a[:, :, 3]
        bgish = np.sqrt(((rgb - BG) ** 2).sum(-1)) < 34
        clear = al < 8
        kill = ndi.binary_dilation(clear) & ~clear & bgish
        if not kill.any():
            break
        a[kill, 3] = 0
    rgb, al = a[:, :, :3], a[:, :, 3]
    solid = (al >= 250) & ~(np.sqrt(((rgb - BG) ** 2).sum(-1)) < 34)
    _, (iy, ix) = ndi.distance_transform_edt(~solid, return_indices=True)
    edge = (al > 0) & ~solid
    rgb[edge] = rgb[iy[edge], ix[edge]]
    lum = rgb.sum(-1)
    dark = (lum < 90) & (al > 0)
    rgb[dark] = INK * (0.55 + 0.45 * np.clip(lum[dark] / 90.0, 0, 1)[:, None])
    return a


def bbox(a):
    ys, xs = np.nonzero(a[:, :, 3] > 0)
    return a[ys.min():ys.max() + 1, xs.min():xs.max() + 1]


def trim(a, sides, t=6):
    h, w, _ = a.shape
    return a[(t if 't' in sides else 0):h - (t if 'b' in sides else 0), (t if 'l' in sides else 0):w - (t if 'r' in sides else 0)]


def rows(col):
    r = np.nonzero(col > 128)[0]
    return int(r.min()), int(r.max())


def as_left(a, side):
    return {'l': a, 'r': a[:, ::-1], 'b': np.rot90(a, -1), 't': np.rot90(a, 1)}[side].copy()


def from_left(a, side):
    return {'l': a, 'r': a[:, ::-1], 'b': np.rot90(a, 1), 't': np.rot90(a, -1)}[side].copy()


pieces = {n: bbox(trim(bbox(defringe(np.asarray(Image.open(f'{RAW}/{n}.png').convert('RGBA')).astype(float))), s)) for n, s in SIDES.items()}

s = pieces['loi_thang']
h, w, _ = s.shape
rolled = np.roll(s, w // 2, axis=1)
x = np.arange(w)
m = np.clip(np.minimum(x, w - 1 - x) / (w * 0.3), 0, 1)[None, :, None]
straight = s * m + rolled * (1 - m)
pieces['loi_thang'] = straight
band = straight[:, :K]
top, bottom = rows(straight[:, 0, 3])
centre = (top + bottom) / 2

report = {'loi_thang': {'l': [top, bottom], 'r': list(rows(straight[:, -1, 3]))}}
for name, sides in SIDES.items():
    if name == 'loi_thang':
        continue
    a = pieces[name]
    report[name] = {}
    for side in sides:
        q = as_left(a, side)
        t, b = rows(q[:, 0, 3])
        offset = int(round((t + b) / 2 - centre))
        pad_top = max(0, -offset)
        pad_bottom = max(0, offset + h - q.shape[0])
        q = np.concatenate([np.zeros((pad_top, q.shape[1], 4)), q, np.zeros((pad_bottom, q.shape[1], 4))], 0)
        start = offset + pad_top
        canvas = np.zeros((q.shape[0], K, 4))
        canvas[start:start + h] = band
        wgt = (np.arange(K)[::-1] / (K - 1))[None, :, None]
        q[:, :K] = q[:, :K] * (1 - wgt) + canvas * wgt
        a = from_left(q, side)
    pieces[name] = a

for name, a in pieces.items():
    sides = SIDES[name]
    H, W, _ = a.shape
    PH, PW = (-H) % 4, (-W) % 4
    left = 0 if 'l' in sides else PW
    if 'l' in sides and 'r' in sides:
        a = a[:, :W - PW] if PW else a
        PW, left = 0, 0
    top = PH if 'b' in sides else 0
    out = np.zeros((H + PH, a.shape[1] + PW, 4))
    out[top:top + H, left:left + a.shape[1]] = a
    Image.fromarray(out.clip(0, 255).astype(np.uint8)).save(f'{OUT}/{name}.png')
    edges = {}
    if 'l' in sides: edges['l'] = list(rows(out[:, 0, 3]))
    if 'r' in sides: edges['r'] = list(rows(out[:, -1, 3]))
    if 'b' in sides: edges['b'] = list(rows(out[-1, :, 3]))
    report[name] = {'size': list(out.shape[1::-1]), 'edges': edges}

json.dump(report, open(f'{OUT}/noi.json', 'w'), indent=1)
print(json.dumps(report))
