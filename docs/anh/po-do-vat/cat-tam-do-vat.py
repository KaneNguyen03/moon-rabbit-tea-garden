# Cuts a 3x2 sheet of props on a flat grey background into six transparent PNGs (longest side 512).
import sys, os
import numpy as np
from PIL import Image, ImageFilter
from scipy import ndimage as ndi
sheet, out, names = sys.argv[1], sys.argv[2], sys.argv[3].split(',')
im = Image.open(sheet).convert('RGB'); a = np.asarray(im).astype(np.int16); H, W, _ = a.shape
bg = np.median(np.concatenate([a[:8].reshape(-1,3), a[-8:].reshape(-1,3), a[:, :8].reshape(-1,3), a[:, -8:].reshape(-1,3)]), axis=0)
dist = np.sqrt(((a - bg) ** 2).sum(-1)); near = dist < 26
lab, n = ndi.label(near)
edge = np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]])); edge = edge[edge > 0]
bgmask = np.isin(lab, edge)
holes = near & ~bgmask; hl, hn = ndi.label(holes)
for i in range(1, hn + 1):
    m = hl == i; s = m.sum()
    if s > 600 and dist[m].std() < 3.5 and dist[m].mean() < 7: bgmask |= m
bgmask[H - 190:, W - 190:] = True
fg = ndi.binary_opening(~bgmask, iterations=2)
objs, k = ndi.label(fg)
cell = np.zeros((H, W), np.int8)
for i, sl in enumerate(ndi.find_objects(objs), 1):
    m = objs[sl] == i
    if m.sum() < 12: continue
    cy = (sl[0].start + sl[0].stop) / 2; cx = (sl[1].start + sl[1].stop) / 2
    c = (1 if cy > H / 2 else 0) * 3 + min(2, int(cx / (W / 3)))
    cell[sl][m] = c + 1
soft = np.asarray(Image.fromarray((~bgmask * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.1)))
rgba = np.dstack([np.asarray(im), soft])
for c, name in enumerate(names, 1):
    mask = cell == c; ys, xs = np.nonzero(mask)
    y0, y1, x0, x1 = max(0, ys.min() - 4), min(H, ys.max() + 5), max(0, xs.min() - 4), min(W, xs.max() + 5)
    crop = rgba[y0:y1, x0:x1].copy(); keep = ndi.binary_dilation(mask[y0:y1, x0:x1], iterations=3)
    crop[..., 3] = np.where(keep, crop[..., 3], 0)
    o = Image.fromarray(crop, 'RGBA'); s = 512 / max(o.size)
    o = o.resize((round(o.width * s), round(o.height * s)), Image.LANCZOS); o.save(os.path.join(out, name + '.png'))
    print(name, o.size)
