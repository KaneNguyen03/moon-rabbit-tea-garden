# Bends one ear of a kind picture: every ear pixel turns about the ear's root by an angle that grows from 0 at
# the root to `deg` at the tip (by distance from the root), so the root stays joined to the head exactly as drawn.
# Pixels outside the ear box (head, body) are not touched.
# usage: bend.py id 'x,y x,y ...'(ear polygon) rootX rootY deg(clockwise) length out.png
import sys; from PIL import Image; import numpy as np
from scipy import ndimage as ndi
from PIL import ImageDraw
k = sys.argv[1]; poly = [tuple(map(float, p.split(','))) for p in sys.argv[2].split()]; rx, ry, deg, L = map(float, sys.argv[3:7]); out = sys.argv[7]
a = np.asarray(Image.open(f'/Users/hiquyennek/Documents/VuonTraNam/Assets/VuonTra/Art/Final/kind_{k}.png').convert('RGBA')).astype(float)
H, W = a.shape[:2]; yy, xx = np.mgrid[:H, :W].astype(float)
m = Image.new('L', (W, H), 0); ImageDraw.Draw(m).polygon(poly, fill=255); box = np.asarray(m) > 0
near = ndi.binary_dilation(box, iterations=8)
src = box | (near & (np.hypot(xx - rx, yy - ry) < 0.4 * L))   # a little head around the box, so seams sample real fur
ear = np.where(src[..., None], a, 0)
rest = np.where((box & (np.hypot(xx - rx, yy - ry) > 0.3 * L))[..., None], 0, a)   # near the root the ear barely moves: keep it as drawn
r = np.hypot(xx - rx, yy - ry); s = np.clip(r / L, 0, 1); th = np.radians(deg) * s * s * (3 - 2 * s)
# output (x,y) at angle phi takes the ear pixel at angle phi - th (same radius); clockwise on screen = +angle with y down
c, sn = np.cos(-th), np.sin(-th)
sx = rx + (xx - rx) * c - (yy - ry) * sn; sy = ry + (xx - rx) * sn + (yy - ry) * c
warped = np.stack([ndi.map_coordinates(ear[..., i] * (ear[..., 3] / 255 if i < 3 else 1), [sy, sx], order=1, mode='constant') for i in range(4)], -1)
al = warped[..., 3:4] / 255; rgb = np.where(al > 0, warped[..., :3] / np.maximum(al, 1e-6), 0)
# optional 8th arg: a polyline along the head edge the ear used to cover; it gets an ink line like the drawing's own
if len(sys.argv) > 8:
    line = [tuple(map(float, p.split(','))) for p in sys.argv[8].split()]
    op = a[..., 3] > 200; lum = a[..., :3].mean(-1); ink = np.median(a[op & (lum < np.percentile(lum[op], 3))][:, :3], axis=0)
    Z = 4; m2 = Image.new('L', (W * Z, H * Z), 0); ImageDraw.Draw(m2).line([(x * Z, y * Z) for x, y in line], fill=255, width=int(2.6 * Z), joint='curve')
    lm = np.asarray(m2.resize((W, H), Image.LANCZOS)).astype(float)[..., None] / 255 * (rest[..., 3:4] > 0)
    rest[..., :3] = rest[..., :3] * (1 - lm) + ink * lm
# composite warped ear over the rest
ra = rest[..., 3:4] / 255; oa = al + ra * (1 - al)
orgb = (rgb * al + rest[..., :3] * ra * (1 - al)) / np.maximum(oa, 1e-6)
res = np.dstack([orgb, oa * 255])
touched = box | src | (warped[..., 3] > 0) | (np.any(rest != a, -1))
res = np.where(touched[..., None], res, a)               # everything else stays byte-for-byte as drawn
Image.fromarray(res.round().clip(0, 255).astype(np.uint8)).save(out)
