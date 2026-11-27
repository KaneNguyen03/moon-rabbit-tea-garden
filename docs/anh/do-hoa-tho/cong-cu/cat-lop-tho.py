import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage as ndi

OUT = 512
SCALE = 0.5


def load(path):
    return np.array(Image.open(path).convert("RGB")).astype(np.float32) / 255


def background(img):
    edge = np.concatenate([img[:24].reshape(-1, 3), img[:, :24].reshape(-1, 3), img[:, -24:].reshape(-1, 3)])
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
    return [lab == i for i in np.argsort(sizes)[::-1][:keep] + 1]


def solid(mask, open_r=2, close_r=4):
    mask = ndi.binary_opening(mask, iterations=open_r)
    mask = ndi.binary_closing(mask, iterations=close_r)
    return ndi.binary_fill_holes(mask)


def soften(mask, r=1.1):
    return np.clip(ndi.gaussian_filter(mask.astype(np.float32), r) * 1.6 - 0.3, 0, 1)


def rgba(col, alpha, mask):
    return np.concatenate([col, (alpha * soften(mask))[..., None]], 2)


def head_above_collar(col, alpha, fg, arms):
    hue = (col[..., 1] - col[..., 2] > 0.15) & (col[..., 1] > col[..., 0] - 0.03) & (alpha > 0.85)
    collar = components(ndi.binary_opening(hue, iterations=2), 1)[0]
    width = fg.shape[1]
    cols = np.nonzero(collar.any(0))[0]
    tops = np.array([np.nonzero(collar[:, x])[0].min() for x in cols])
    span = cols.max() - cols.min()
    keep = (cols > cols.min() + span * 0.12) & (cols < cols.max() - span * 0.12)
    fit = np.polyfit(cols[keep], tops[keep], 2)
    cuts = np.round(np.polyval(fit, np.arange(width))).astype(int) + 3
    head = np.zeros_like(fg)
    for x in np.nonzero(fg.any(0))[0]:
        head[: cuts[x], x] = fg[: cuts[x], x]
    for mask in arms:
        head &= ~ndi.binary_dilation(mask, iterations=2)
    return solid(components(ndi.binary_opening(head, iterations=4), 1)[0], 1, 3) & fg


def main(master_path, body_path, arms_path, head_path, view, out_dir, lop):
    out = Path(out_dir)
    out.mkdir(parents=True, exist_ok=True)
    master = load(master_path)
    bg = background(master)
    col, alpha = unmix(master, bg)
    fg = solid(alpha > 0.5)
    body_col, body_alpha = unmix(load(body_path), bg)
    body_fg = solid(body_alpha > 0.5)
    arms_col, arms_alpha = unmix(load(arms_path), bg)
    arm_count = 2 if view == "front" else 1
    arms = sorted(components(solid(arms_alpha > 0.5, 1, 2), arm_count), key=lambda m: np.nonzero(m)[1].mean())

    if Path(head_path).exists():
        head_col, head_alpha = unmix(load(head_path), bg)
        head = solid(components(solid(head_alpha > 0.5, 2, 3), 1)[0], 1, 2)
    else:
        head_col, head_alpha = col, alpha
        head = head_above_collar(col, alpha, fg, arms)
    cuts = np.full(fg.shape[1], np.nonzero(head.any(1))[0].max())
    for x in np.nonzero(head.any(0))[0]:
        cuts[x] = np.nonzero(head[:, x])[0].max()

    layers = {"Body": rgba(body_col, body_alpha, body_fg), "Head": rgba(head_col, head_alpha, head)}
    names = ["ArmL", "ArmR"] if view == "front" else ["Arm"]
    for name, mask in zip(names, arms):
        layers[name] = rgba(arms_col, arms_alpha, mask)

    ys, xs = np.nonzero(fg)
    feet = ys.max()
    cx = np.nonzero(body_fg[feet - 60])[0].mean()
    size = int(round(master.shape[1] * SCALE))

    def place(arr):
        premult = (np.clip(arr, 0, 1) * 255).astype(np.float32)
        premult[..., :3] *= premult[..., 3:4] / 255
        im = Image.fromarray(premult.astype(np.uint8), "RGBA").resize((size, size), Image.LANCZOS)
        p = np.array(im).astype(np.float32)
        p[..., :3] /= np.maximum(p[..., 3:4] / 255, 1e-3)
        canvas = Image.new("RGBA", (OUT, OUT))
        canvas.alpha_composite(Image.fromarray(np.clip(p, 0, 255).astype(np.uint8), "RGBA"))
        return canvas

    for name, arr in layers.items():
        place(arr).save(out / f"{name}.png")

    def at(x, y):
        return [round(float(x) * SCALE, 1), round(float(y) * SCALE, 1)]

    hy, hx = np.nonzero(head)
    widths = head.sum(1)
    crown = int(np.nonzero(widths > widths.max() * 0.62)[0].min())
    head_cx = hx.mean()
    neck_y = cuts[int(round(head_cx))] - 14
    pivots = {
        "feet": at(cx, feet),
        "hips": at(cx, feet - (feet - neck_y) * 0.3),
        "neck": at(head_cx, neck_y),
        "crown": at(head_cx, crown),
        "top": at(head_cx, hy.min()),
        "lop": lop,
        "arms": [],
    }
    for name, mask in zip(names, arms):
        my, mx = np.nonzero(mask)
        top = my.min()
        bottom = my.max()
        pivots["arms"].append({
            "name": name,
            "shoulder": at(mx[my < top + 10].mean(), top + 6),
            "paw": at(mx[my > bottom - 12].mean(), bottom - 10),
        })
    (out / "pivots.json").write_text(json.dumps(pivots, indent=2))

    sheet = Image.new("RGBA", (OUT * (len(layers) + 1), OUT), (46, 94, 73, 255))
    full = Image.new("RGBA", (OUT, OUT))
    for i, name in enumerate(layers):
        im = Image.open(out / f"{name}.png")
        sheet.alpha_composite(im, (i * OUT, 0))
    for name in ["Body", "Head"] + names:
        full.alpha_composite(Image.open(out / f"{name}.png"))
    sheet.alpha_composite(full, (len(layers) * OUT, 0))
    sheet.save(out / "_sheet.png")
    print(out, json.dumps(pivots))


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4], sys.argv[5], sys.argv[6], sys.argv[7] == "lop")
