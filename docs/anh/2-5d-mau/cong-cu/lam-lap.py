import sys

import numpy as np
from PIL import Image


def seam_weight(n, band):
    t = np.arange(n, dtype=float)
    d = np.minimum(t, n - 1 - t)
    return np.clip(1 - d / band, 0, 1)


def make_tileable(a, horizontal_only):
    h, w = a.shape[:2]
    band_x = w * 0.25
    band_y = h * 0.25
    shifted = np.roll(a, w // 2, axis=1)
    wx = seam_weight(w, band_x)[None, :]
    if horizontal_only:
        mix = np.repeat(wx, h, axis=0)
    else:
        shifted = np.roll(shifted, h // 2, axis=0)
        wy = seam_weight(h, band_y)[:, None]
        mix = np.maximum(wx, wy)
    mix = mix[:, :, None]
    return a * (1 - mix) + shifted * mix


def main():
    source, target = sys.argv[1], sys.argv[2]
    horizontal_only = len(sys.argv) > 3 and sys.argv[3] == "ngang"
    a = np.asarray(Image.open(source).convert("RGB")).astype(float)
    out = make_tileable(a, horizontal_only)
    Image.fromarray(out.clip(0, 255).astype(np.uint8), "RGB").save(target)
    print(target)


if __name__ == "__main__":
    main()
