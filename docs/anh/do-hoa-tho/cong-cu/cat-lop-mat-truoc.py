import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage as ndi

OUT = 512
FEET_Y = 466


def load(path):
    return np.array(Image.open(path).convert("RGB")).astype(np.float32) / 255


def background(img):
    edge = np.concatenate([img[:24].reshape(-1, 3), img[-24:].reshape(-1, 3), img[:, :24].reshape(-1, 3), img[:, -24:].reshape(-1, 3)])
    return np.median(edge, 0)


def unmix(img, bg, lo=0.08, hi=0.26):
    d = np.abs(img - bg).sum(2)
    a = np.clip((d - lo) / (hi - lo), 0, 1)
    col = (img - (1 - a[..., None]) * bg) / np.maximum(a[..., None], 1e-3)
    return np.clip(col, 0, 1), a


def components(mask, keep):
    lab, n = ndi.label(mask)
    if n == 0:
        return []
    sizes = ndi.sum(mask, lab, range(1, n + 1))
    ids = np.argsort(sizes)[::-1][:keep] + 1
    return [lab == i for i in ids]


def solid(mask, open_r=2, close_r=4):
    mask = ndi.binary_opening(mask, iterations=open_r)
    mask = ndi.binary_closing(mask, iterations=close_r)
    return ndi.binary_fill_holes(mask)


def soften(mask, r=1.2):
    return np.clip(ndi.gaussian_filter(mask.astype(np.float32), r) * 1.6 - 0.3, 0, 1)


def layer(col, alpha, mask):
    a = alpha * soften(mask)
    return np.concatenate([col, a[..., None]], 2)


def main(src, out):
    out = Path(out)
    out.mkdir(parents=True, exist_ok=True)
    front = load(f"{src}/front.png")
    bg = background(front)
    col, alpha = unmix(front, bg)
    fg = solid(alpha > 0.5)
    fg_noarms = solid(unmix(load(f"{src}/plate_noarms.png"), bg)[1] > 0.5)
    fg_noears = solid(unmix(load(f"{src}/plate_noears.png"), bg)[1] > 0.5)
    body_col, body_alpha = unmix(load(f"{src}/plate_nohead.png"), bg)
    head_col, head_alpha = unmix(load(f"{src}/plate_noears.png"), bg)
    fg_body = solid(body_alpha > 0.5)

    ys, xs = np.nonzero(fg)
    feet = ys.max()
    top = ys.min()

    arms_col, arms_alpha = unmix(load(f"{src}/plate_armsonly.png"), bg)
    arm_mask = solid(arms_alpha > 0.5, 1, 2)
    arms = sorted(components(solid(arm_mask, 1, 2), 2), key=lambda m: np.nonzero(m)[1].mean())

    ear_mask = ndi.binary_dilation(fg & ~ndi.binary_dilation(fg_noears, iterations=2), iterations=3) & fg
    ears_parts = components(solid(ear_mask, 1, 2), 2)
    ears = np.zeros_like(fg)
    for m in ears_parts:
        ears |= m

    neck_rows = np.nonzero(fg_body.any(1))[0]
    body_top = neck_rows.min()
    ink = (col.sum(2) < 1.2) & (alpha > 0.6)
    head = np.zeros_like(fg)
    lo, hi = body_top, body_top + int((feet - body_top) * 0.45)
    cuts = np.full(fg.shape[1], lo)
    for x in range(fg.shape[1]):
        rows = np.nonzero(ink[lo:hi, x])[0]
        if rows.size:
            cuts[x] = lo + rows.max() + 4
    cols = np.nonzero(fg_noears[lo:hi].any(0))[0]
    span = cols.max() - cols.min()
    middle = cols[(cols > cols.min() + span * 0.2) & (cols < cols.max() - span * 0.2)]
    fit = np.polyfit(middle, ndi.median_filter(cuts, size=41)[middle], 2)
    cuts = np.round(np.polyval(fit, np.arange(fg.shape[1]))).astype(int)
    for x in cols:
        head[: cuts[x], x] = fg_noears[: cuts[x], x]
    head = solid(components(ndi.binary_opening(head, iterations=8), 1)[0], 1, 3) & fg_noears

    ear_col = col.copy()
    ear_alpha = alpha.copy()
    core = components(solid(fg & (alpha > 0.9) & ~ndi.binary_dilation(fg_noears, iterations=5), 1, 2), 2)
    ears = core[0] | core[1]
    reach = 90
    fill_rows = 10
    ears_ext = ears.copy()
    for x in np.nonzero(ears.any(0))[0]:
        rows = np.nonzero(ears[:, x])[0]
        bottom = rows.max()
        source = max(rows.min(), bottom - 3)
        ear_col[bottom - 6: bottom + reach, x] = col[source, x]
        ear_alpha[bottom - 6: bottom + reach, x] = 1.0
        ears_ext[bottom - 6: bottom + reach, x] = True
    extension = ears_ext & ~ears & ndi.binary_dilation(head, iterations=7)
    extension = ndi.binary_erosion(extension, structure=np.ones((1, 3)), iterations=4)
    extension &= ndi.binary_erosion(ndi.binary_dilation(head, iterations=7), structure=np.ones((1, 3)), iterations=16)
    ears_ext = ears | extension
    closed = ndi.binary_closing(ears_ext, structure=np.ones((31, 1))) & (ears_ext | ndi.binary_dilation(head, iterations=3))
    extension |= closed & ~ears_ext
    ears_ext |= closed
    light = ears & (col.sum(2) > 2.4) & (np.abs(col[..., 0] - col[..., 1]) < 0.12)
    cream = np.median(col[light], 0)
    seam = extension | (ears & ~ndi.binary_erosion(ears, structure=np.array([[0], [1], [1]]), iterations=5) & ndi.binary_dilation(extension, iterations=2))
    ear_col[seam] = cream
    ear_alpha[seam] = 1.0
    ear_alpha = np.maximum(ear_alpha, ndi.binary_erosion(ears_ext, iterations=3).astype(np.float32))
    base = np.nonzero(ears.any(1))[0].max()

    layers = {
        "Body": layer(body_col, body_alpha, fg_body),
        "Head": layer(head_col, head_alpha, head),
        "Ears": layer(ear_col, ear_alpha, ears_ext),
        "ArmL": layer(arms_col, arms_alpha, arms[0]),
        "ArmR": layer(arms_col, arms_alpha, arms[1]),
    }

    scale = (FEET_Y - 18) / (feet - top)
    cx = xs.mean()
    size = int(round(front.shape[1] * scale))
    ox = OUT / 2 - cx * scale
    oy = FEET_Y - feet * scale

    def place(arr):
        im = Image.fromarray((np.clip(arr, 0, 1) * 255).astype(np.uint8), "RGBA")
        premult = np.array(im).astype(np.float32)
        premult[..., :3] *= premult[..., 3:4] / 255
        im = Image.fromarray(premult.astype(np.uint8), "RGBA").resize((size, size), Image.LANCZOS)
        p = np.array(im).astype(np.float32)
        p[..., :3] /= np.maximum(p[..., 3:4] / 255, 1e-3)
        im = Image.fromarray(np.clip(p, 0, 255).astype(np.uint8), "RGBA")
        canvas = Image.new("RGBA", (OUT, OUT))
        canvas.alpha_composite(im, (int(round(ox)), int(round(oy))))
        return canvas

    for name, arr in layers.items():
        place(arr).save(out / f"{name}.png")

    def to_out(x, y):
        return [round(x * scale + ox, 1), round(y * scale + oy, 1)]

    def shoulder(m):
        yy, xx = np.nonzero(m)
        top_y = yy.min()
        row = xx[yy < top_y + 6]
        return to_out(row.mean(), top_y + 4)

    def paw(m):
        yy, xx = np.nonzero(m)
        bot = yy.max()
        row = xx[yy > bot - 10]
        return to_out(row.mean(), bot - 10)

    hy, hx = np.nonzero(head)
    pivots = {
        "canvas": OUT,
        "feet": to_out(cx, feet),
        "neck": to_out(cx, hy.max() - (hy.max() - hy.min()) * 0.08),
        "head_top": to_out(cx, hy.min()),
        "ear_base": to_out(cx, base),
        "ears_top": to_out(cx, top),
        "shoulder_l": shoulder(arms[0]),
        "paw_l": paw(arms[0]),
        "shoulder_r": shoulder(arms[1]),
        "paw_r": paw(arms[1]),
        "body_top": to_out(cx, body_top),
    }
    (out / "pivots.json").write_text(json.dumps(pivots, indent=2))
    sheet = Image.new("RGBA", (OUT * 6, OUT), (46, 94, 73, 255))
    full = Image.new("RGBA", (OUT, OUT))
    for name in ["Ears", "Body", "Head", "ArmL", "ArmR"]:
        im = Image.open(out / f"{name}.png")
        full.alpha_composite(im)
    for i, name in enumerate(["Body", "Head", "Ears", "ArmL", "ArmR"]):
        sheet.alpha_composite(Image.open(out / f"{name}.png"), (i * OUT, 0))
    sheet.alpha_composite(full, (5 * OUT, 0))
    sheet.save(out / "_sheet.png")
    print(json.dumps(pivots))


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
