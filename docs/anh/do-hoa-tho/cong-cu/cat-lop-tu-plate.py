import json
import os
import sys

import numpy as np
from PIL import Image
from scipy import ndimage as ndi

SRC = 1254
OUT = 512
FEET_Y = 466
SCALE = 0.39


def load(path):
    return np.array(Image.open(path).convert("RGB")).astype(np.float32) / 255


def background(img):
    edge = np.concatenate([img[:24].reshape(-1, 3), img[-24:].reshape(-1, 3), img[:, :24].reshape(-1, 3)])
    return np.median(edge, 0)


def unmix(img, bg, lo=0.06, hi=0.22):
    d = np.abs(img - bg).sum(2)
    a = np.clip((d - lo) / (hi - lo), 0, 1)
    col = (img - (1 - a[..., None]) * bg) / np.maximum(a[..., None], 1e-3)
    return np.clip(col, 0, 1), a


def diff(a, b, blur=2.5):
    return ndi.gaussian_filter(np.abs(a - b).sum(2), blur)


def largest(mask, keep=1):
    lab, n = ndi.label(mask)
    if n == 0:
        return mask
    sizes = ndi.sum(mask, lab, range(1, n + 1))
    ids = np.argsort(sizes)[::-1][:keep] + 1
    return np.isin(lab, ids)


def clean(mask, open_r=3, close_r=5, keep=1):
    mask = ndi.binary_opening(mask, iterations=open_r)
    mask = ndi.binary_closing(mask, iterations=close_r)
    return ndi.binary_fill_holes(largest(mask, keep))


def feather(mask, r):
    return np.clip(ndi.gaussian_filter(mask.astype(np.float32), r) * 2 - 0.5, 0, 1) if r else mask.astype(np.float32)


def rgba(col, a):
    return np.concatenate([col, a[..., None]], 2)


def place(layer):
    a = layer[..., 3:4]
    pm = np.concatenate([layer[..., :3] * a, a], 2)
    im = Image.fromarray((pm * 255 + 0.5).astype(np.uint8), "RGBA")
    size = round(SRC * SCALE)
    im = im.resize((size, size), Image.LANCZOS)
    pm = np.array(im).astype(np.float32) / 255
    return pm


def compose(pm, ox, oy):
    canvas = np.zeros((OUT, OUT, 4), np.float32)
    h, w = pm.shape[:2]
    x0, y0 = max(0, ox), max(0, oy)
    x1, y1 = min(OUT, ox + w), min(OUT, oy + h)
    canvas[y0:y1, x0:x1] = pm[y0 - oy:y1 - oy, x0 - ox:x1 - ox]
    a = canvas[..., 3:4]
    col = np.where(a > 1e-3, canvas[..., :3] / np.maximum(a, 1e-3), 0)
    return Image.fromarray((np.clip(np.concatenate([col, a], 2), 0, 1) * 255 + 0.5).astype(np.uint8), "RGBA")


def main(plates, out_dir):
    os.makedirs(out_dir, exist_ok=True)
    M = load(f"{plates}/master_a.png")
    NE = load(f"{plates}/plate_noears.png")
    NA = load(f"{plates}/plate_noarm.png")
    NH = load(f"{plates}/plate_nohead.png")
    bg = background(M)
    cM, aM = unmix(M, bg)
    cNE, aNE = unmix(NE, bg)
    cNH, aNH = unmix(NH, bg)
    cNA, _ = unmix(NA, bg)
    yy = np.mgrid[0:SRC, 0:SRC][0]

    fgM, fgNE, fgNH = aM > 0.5, aNE > 0.5, aNH > 0.5
    lum_m = M @ np.array([0.3, 0.55, 0.15], np.float32)
    body_top = np.where(fgNH.any(1))[0].min()

    HO = load(f"{plates}/plate_headonly.png")
    cHO, aHO = unmix(HO, bg)
    head = clean(aHO > 0.5, 2, 4)
    head_a = np.minimum(feather(ndi.binary_dilation(head, iterations=1), 1.0), aHO)

    ears = clean(fgM & ~fgNE & (yy < body_top), 5, 3, keep=2)
    ears_a = np.minimum(feather(ears, 1.0), aM)
    ears_col = cM.copy()
    cols = np.where(ears.any(0))[0]
    for x in cols:
        ys = np.where(ears[:, x])[0]
        y0 = ys.max() - 6
        if aM[y0, x] < 0.97 or lum_m[y0, x] < 0.42:
            continue
        span = np.arange(y0, min(y0 + 90, SRC))
        span = span[head[span, x]]
        ears_col[span, x] = cM[y0, x]
        ears_a[span, x] = 1.0

    arm = clean((diff(M, NA, 3) > 0.3) & fgM, 3, 6)
    ys = np.where(arm.any(1))[0]
    arm_top, arm_bot = ys.min(), ys.max()
    ramp = np.clip((yy - arm_top) / (0.28 * (arm_bot - arm_top)), 0, 1)
    arm_a = np.minimum(feather(ndi.binary_dilation(arm, iterations=3), 1.5), aM) * ramp
    shoulder_x = np.where(arm[arm_top + 8])[0].mean()

    arm_soft = feather(ndi.binary_dilation(arm, iterations=8), 4)[..., None]
    body_col = cNH * (1 - arm_soft) + cNA * arm_soft
    body_a = aNH

    hats = {}
    for name, path in (("Hat_Crown", "wear_crown"), ("Hat_Beanie", "wear_beanie")):
        if not os.path.exists(f"{path}.png"):
            continue
        W = load(f"{path}.png")
        cW, aW = unmix(W, bg)
        hat = clean((diff(W, M, 2) > 0.35) & (aW > 0.5) & (yy < body_top), 2, 6)
        hats[name] = rgba(cW, np.minimum(feather(ndi.binary_dilation(hat, iterations=1), 1.0), aW))

    layers = dict(
        Body_Full=rgba(body_col, body_a),
        Ears_Back=rgba(ears_col, ears_a),
        Head=rgba(cHO, head_a),
        NearArm=rgba(cM, arm_a),
        **hats,
    )
    ys, xs = np.where(fgM)
    ox = round(OUT / 2 - (xs.min() + xs.max()) / 2 * SCALE)
    oy = round(FEET_Y - ys.max() * SCALE)
    for name, layer in layers.items():
        compose(place(layer), ox, oy).save(f"{out_dir}/{name}.png")
    master = Image.new("RGBA", (OUT, OUT))
    for name in ("Body_Full", "Ears_Back", "Head", "NearArm"):
        master.alpha_composite(Image.open(f"{out_dir}/{name}.png"))
    master.save(f"{out_dir}/Master.png")

    def p(x, y):
        return [round(x * SCALE + ox, 1), round(y * SCALE + oy, 1)]

    hy, hx = np.where(head)
    ey, ex = np.where(ears)
    pivots = dict(
        canvas=OUT, ppu=100, sprite_pivot=[0.5, 0.1],
        neck=p(hx.mean(), hy.max() - 25),
        ear_base=p(ex.mean(), ey.max()),
        shoulder=p(shoulder_x, arm_top + 10),
    )
    json.dump(pivots, open(f"{out_dir}/pivots.json", "w"), indent=2)
    print(pivots)


if __name__ == "__main__":
    os.chdir(sys.argv[1])
    main(".", sys.argv[2])
