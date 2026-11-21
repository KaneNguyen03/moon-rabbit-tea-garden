# Cuts a 3x2 rabbit sheet on a flat grey background into six transparent sprites in a 320x343 frame.
import sys, os, json
import numpy as np
from PIL import Image, ImageFilter
from scipy import ndimage as ndi
sheet, out, ids = sys.argv[1], sys.argv[2], sys.argv[3].split(',')
FW, FH, TALL = 320, 343, 334
im = Image.open(sheet).convert('RGB'); a = np.asarray(im).astype(np.int16)
H, W, _ = a.shape
bg = np.median(np.concatenate([a[:8].reshape(-1,3), a[-8:].reshape(-1,3), a[:, :8].reshape(-1,3), a[:, -8:].reshape(-1,3)]), axis=0)
dist = np.sqrt(((a - bg) ** 2).sum(-1))
near = dist < 26
lab, n = ndi.label(near)
edge = np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]])); edge = edge[edge > 0]
bgmask = np.isin(lab, edge)
# pockets of background enclosed by a limb or ears (large flat grey holes)
holes = near & ~bgmask
hl, hn = ndi.label(holes)
if hn:
    sizes = ndi.sum(holes, hl, range(1, hn + 1))
    flat = ndi.standard_deviation(dist, hl, range(1, hn + 1))
    for i, (s, f) in enumerate(zip(sizes, flat), 1):
        if s > 600 and f < 3.5 and ndi.mean(dist, hl, i) < 7: bgmask |= hl == i
fg = ~bgmask
fg[H - 190:, W - 190:] = False  # the generator's corner mark
fg = ndi.binary_opening(fg, iterations=2)
objs, k = ndi.label(fg)
sizes = ndi.sum(fg, objs, range(1, k + 1))
big = [i + 1 for i, s in enumerate(sizes) if s > 0.004 * H * W]
boxes = ndi.find_objects(objs)
main = []
for i in big:
    sl = boxes[i - 1]; main.append([i, sl[0].start, sl[0].stop, sl[1].start, sl[1].stop])
# attach small bits (sparkles, glow) to the nearest big object if close
for i, s in enumerate(sizes, 1):
    if i in big or s < 12: continue
    sl = boxes[i - 1]; cy = (sl[0].start + sl[0].stop) / 2; cx = (sl[1].start + sl[1].stop) / 2
    best = min(main, key=lambda m: max(m[1] - cy, cy - m[2], 0) + max(m[3] - cx, cx - m[4], 0))
    gap = max(best[1] - cy, cy - best[2], 0) + max(best[3] - cx, cx - best[4], 0)
    if gap < 70:
        objs[objs == i] = best[0]
main.sort(key=lambda m: (0 if (m[1] + m[2]) / 2 < H / 2 else 1, (m[3] + m[4]) / 2))
if len(main) != len(ids): print('WARN objects', len(main), 'ids', len(ids))
bgmask[H - 190:, W - 190:] = True
soft = Image.fromarray((~bgmask * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.1))
rgba = np.dstack([np.asarray(im), np.asarray(soft)])
crops = []
for m in main[:len(ids)]:
    mask = objs == m[0]
    ys, xs = np.nonzero(mask); y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    pad = 4; y0 = max(0, y0 - pad); x0 = max(0, x0 - pad); y1 = min(H, y1 + pad); x1 = min(W, x1 + pad)
    c = rgba[y0:y1, x0:x1].copy()
    keep = ndi.binary_dilation(mask[y0:y1, x0:x1], iterations=3)
    c[..., 3] = np.where(keep, c[..., 3], 0)
    crops.append(Image.fromarray(c, 'RGBA'))
tallest = max(c.height for c in crops)
scale = TALL / tallest
info = []
for id_, c in zip(ids, crops):
    s = min(scale, (FW - 6) / c.width, (FH - 4) / c.height)
    r = c.resize((max(1, round(c.width * s)), max(1, round(c.height * s))), Image.LANCZOS)
    frame = Image.new('RGBA', (FW, FH), (0, 0, 0, 0))
    # centre on the feet: columns of the bottom 12% of the sprite
    al = np.asarray(r)[..., 3]; rows = max(1, int(r.height * 0.12)); cols = np.nonzero(al[-rows:].max(0) > 40)[0]
    feet = (cols.min() + cols.max()) / 2 if len(cols) else r.width / 2
    x = int(round(150.5 - feet)); x = min(max(x, 2), FW - r.width - 2) if r.width < FW - 4 else (FW - r.width) // 2
    frame.alpha_composite(r, (x, FH - r.height - 1))
    frame.save(os.path.join(out, f'kind_{id_}.png'))
    info.append(dict(id=id_, scale=round(s / scale, 3), w=r.width, h=r.height))
print(json.dumps(info))
