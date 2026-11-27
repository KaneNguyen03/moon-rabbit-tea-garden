import sys

import numpy as np
from PIL import Image
from scipy import ndimage as ndi

from cat_lop import SCALE, FEET_Y, OUT, background, clean, compose, diff, feather, largest, load, place, rgba, unmix

plates, wear, out_png, pot_in, pot_out = sys.argv[1:6]
M = load(f"{plates}/master_a.png")
NA = load(f"{plates}/plate_noarm.png")
W = load(wear)
bg = background(M)
_, aM = unmix(M, bg)
cW, aW = unmix(W, bg)
yy = np.mgrid[0:M.shape[0], 0:M.shape[1]][0]

cW, aW = unmix(W, background(W))
arm = clean(aW > 0.5, 1, 3, keep=2)
top = np.where(arm.any(1))[0].min()
fur = np.median(cW[ndi.binary_erosion(arm, iterations=8)], 0)
tinted = arm & (cW[..., 1] > cW[..., 0] - 0.02)
cW[tinted] = fur
ref = top + 40
for y in range(top, ref):
    take = arm[ref] & (aW[y] > 0.02)
    cW[y][take] = cW[ref][take]
alpha = np.minimum(feather(ndi.binary_erosion(arm, iterations=3), 1.0), aW) * np.clip((yy - top - 6) / 22.0, 0, 1)

ys, xs = np.where(aM > 0.5)
ox = round(OUT / 2 - (xs.min() + xs.max()) / 2 * SCALE)
oy = round(FEET_Y - ys.max() * SCALE)
compose(place(rgba(cW, alpha)), ox, oy).save(out_png)

P = load(pot_in)
cP, aP = unmix(P, background(P))
pot = clean(aP > 0.5, 2, 4)
im = Image.fromarray((np.clip(np.concatenate([cP, np.minimum(feather(pot, 1.0), aP)[..., None]], 2), 0, 1) * 255).astype(np.uint8), "RGBA")
box = im.getbbox()
im = im.crop(box)
mouth_rows = np.where(((cP[..., 0] > 0.35) & (cP[..., 1] < 0.3) & (cP[..., 2] < 0.2) & pot).any(1))[0]
mouth_y = (mouth_rows.min() + mouth_rows.max()) / 2 - box[1]
im.save(pot_out)
arm_img = np.array(Image.open(out_png))[..., 3]
tip = np.where((arm_img > 128).any(1))[0].max()
print({"pot_size": im.size, "pot_mouth_from_top": round(mouth_y, 1), "pot_mouth_fraction_from_bottom": round(1 - mouth_y / im.size[1], 3), "ladle_tip_y": int(tip)})
