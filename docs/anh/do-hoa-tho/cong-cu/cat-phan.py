import sys

import numpy as np
from scipy import ndimage as ndi

from cat_lop import FEET_Y, OUT, SCALE, background, clean, compose, feather, load, place, rgba, unmix

master, plate, out_png, mode = sys.argv[1:5]
M = load(master)
_, aM = unmix(M, background(M))
P = load(plate)
cP, aP = unmix(P, background(P))
yy = np.mgrid[0:P.shape[0], 0:P.shape[1]][0]
keep = 1 if mode in ("arm", "head") else 3
mask = clean(aP > 0.5, 1, 3, keep=keep)

if mode == "arm":
    top = np.where(mask.any(1))[0].min()
    ref = top + 40
    for y in range(top, ref):
        take = mask[ref] & (aP[y] > 0.02)
        cP[y][take] = cP[ref][take]
    alpha = np.minimum(feather(ndi.binary_erosion(mask, iterations=3), 1.0), aP) * np.clip((yy - top - 6) / 22.0, 0, 1)
else:
    alpha = np.minimum(feather(ndi.binary_dilation(mask, iterations=1), 1.0), aP)

ys, xs = np.where(aM > 0.5)
ox = round(OUT / 2 - (xs.min() + xs.max()) / 2 * SCALE)
oy = round(FEET_Y - ys.max() * SCALE)
compose(place(rgba(cP, alpha)), ox, oy).save(out_png)
print(out_png)
