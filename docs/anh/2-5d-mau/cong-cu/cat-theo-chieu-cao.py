import sys

import numpy as np
from PIL import Image
from scipy import ndimage as ndi

BACKGROUND_DISTANCE = 26
SPECK_PIXELS = 40
HOLE_PIXELS = 30
BLEED_STEPS = 8


def background_colour(a):
    border = np.concatenate([a[:8].reshape(-1, 3), a[-8:].reshape(-1, 3), a[:, :8].reshape(-1, 3), a[:, -8:].reshape(-1, 3)])
    return np.median(border, axis=0)


def cut(a):
    bg = background_colour(a)
    dist = np.sqrt(((a - bg) ** 2).sum(-1))
    near = dist < BACKGROUND_DISTANCE
    lab, _ = ndi.label(near)
    edge = np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))
    outside = np.isin(lab, edge[edge > 0])
    hole_labels, hole_count = ndi.label(near & ~outside)
    hole_sizes = ndi.sum(near & ~outside, hole_labels, range(1, hole_count + 1))
    holes = np.isin(hole_labels, [i + 1 for i, s in enumerate(hole_sizes) if s >= HOLE_PIXELS])
    outside = outside | holes
    body = ~outside
    parts, count = ndi.label(body)
    sizes = ndi.sum(body, parts, range(1, count + 1))
    keep = np.isin(parts, [i + 1 for i, s in enumerate(sizes) if s >= SPECK_PIXELS])
    soft = np.clip((dist - BACKGROUND_DISTANCE * 0.5) / BACKGROUND_DISTANCE, 0, 1)
    alpha = np.where(keep, np.maximum(soft, ndi.binary_erosion(keep, iterations=2)), 0)
    return alpha, bg


def unmix(a, alpha, bg):
    out = a.copy()
    partial = (alpha > 0.05) & (alpha < 0.999)
    for c in range(3):
        channel = out[:, :, c]
        channel[partial] = np.clip((a[:, :, c][partial] - bg[c] * (1 - alpha[partial])) / alpha[partial], 0, 255)
    return out


def bleed(rgb, alpha):
    solid = alpha > 0.5
    rgb = rgb.copy()
    for _ in range(BLEED_STEPS):
        grown = ndi.binary_dilation(solid)
        ring = grown & ~solid
        if not ring.any():
            break
        for c in range(3):
            total = ndi.uniform_filter(np.where(solid, rgb[:, :, c], 0), 3)
            weight = ndi.uniform_filter(solid.astype(float), 3)
            rgb[:, :, c][ring] = total[ring] / np.maximum(weight[ring], 1e-6)
        solid = grown
    return rgb


def main():
    source, target, height = sys.argv[1], sys.argv[2], int(sys.argv[3])
    a = np.asarray(Image.open(source).convert("RGB")).astype(float)
    alpha, bg = cut(a)
    rgb = bleed(unmix(a, alpha, bg), alpha)
    rgba = np.dstack([rgb, alpha * 255]).clip(0, 255).astype(np.uint8)
    im = Image.fromarray(rgba, "RGBA")
    im = im.crop(im.getchannel("A").point(lambda v: 255 if v > 8 else 0).getbbox())
    width = max(4, round(im.width * height / im.height / 4) * 4)
    height = round(height / 4) * 4
    im = im.resize((width, height), Image.LANCZOS)
    pad = 8
    framed = Image.new("RGBA", (width + 2 * pad, height + pad), (0, 0, 0, 0))
    framed.paste(im, (pad, pad))
    framed.save(target)
    print(target, framed.size)


if __name__ == "__main__":
    main()
