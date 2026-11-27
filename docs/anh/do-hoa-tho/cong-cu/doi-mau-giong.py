import json
import sys

import numpy as np
from PIL import Image

SHEET = 1024
CELL = SHEET // 3
BG = (46, 94, 73, 255)


def build(cells, sheet_png, layout_json):
    sheet = Image.new("RGBA", (SHEET, SHEET), BG)
    layout = []
    for i, cell in enumerate(cells):
        cx, cy = (i % 3) * CELL, (i // 3) * CELL
        width = CELL // len(cell)
        for k, (name, path) in enumerate(cell):
            im = Image.open(path).convert("RGBA")
            box = im.getbbox()
            crop = im.crop(box)
            assert crop.width <= width - 6 and crop.height <= CELL - 6, (name, crop.size)
            ox = cx + k * width + (width - crop.width) // 2
            oy = cy + (CELL - crop.height) // 2
            sheet.alpha_composite(crop, (ox, oy))
            layout.append(dict(name=name, path=path, box=box, at=[ox, oy]))
    sheet.convert("RGB").save(sheet_png)
    json.dump(layout, open(layout_json, "w"), indent=1)


def cut(edited_png, layout_json, out_dir):
    from scipy import ndimage as ndi
    edited = np.array(Image.open(edited_png).convert("RGB").resize((SHEET, SHEET), Image.LANCZOS)).astype(np.float32) / 255
    bg = np.array(BG[:3], np.float32) / 255
    green = np.abs(edited - bg).sum(2) < 0.22
    for part in json.load(open(layout_json)):
        src = np.array(Image.open(part["path"]).convert("RGBA")).astype(np.float32) / 255
        l, t, r, b = part["box"]
        ox, oy = part["at"]
        s_rgb, s_a = src[t:b, l:r, :3], src[t:b, l:r, 3]
        solid = s_a > 0.5
        best, shift = -1.0, (0, 0)
        for dy in range(-8, 9):
            for dx in range(-8, 9):
                y0, x0 = oy + dy, ox + dx
                if y0 < 0 or x0 < 0 or y0 + b - t > SHEET or x0 + r - l > SHEET:
                    continue
                fg = ~green[y0:y0 + b - t, x0:x0 + r - l]
                score = (fg & solid).sum() / max((fg | solid).sum(), 1)
                if score > best:
                    best, shift = score, (dx, dy)
        dx, dy = shift
        e_rgb = edited[oy + dy:oy + dy + b - t, ox + dx:ox + dx + r - l].copy()
        reliable = ~ndi.binary_dilation(green[oy + dy:oy + dy + b - t, ox + dx:ox + dx + r - l], iterations=2) & solid
        X = np.concatenate([s_rgb[reliable], np.ones((reliable.sum(), 1), np.float32)], 1)
        coef, *_ = np.linalg.lstsq(X, e_rgb[reliable], rcond=None)
        mapped = np.clip(np.concatenate([s_rgb, np.ones(s_rgb.shape[:2] + (1,), np.float32)], 2) @ coef, 0, 1)
        w = ndi.gaussian_filter(reliable.astype(np.float32), 1.5)[..., None]
        colour = e_rgb * w + mapped * (1 - w)
        lum = s_rgb @ np.array([0.3, 0.55, 0.15], np.float32)
        ink = np.clip((0.42 - lum) / 0.12, 0, 1)[..., None]
        colour = colour * (1 - ink) + s_rgb * ink
        canvas = np.zeros((512, 512, 4), np.float32)
        canvas[t:b, l:r, :3] = colour
        canvas[..., 3] = src[..., 3]
        Image.fromarray((canvas * 255 + 0.5).astype(np.uint8), "RGBA").save(f"{out_dir}/{part['name']}.png")
        print(part["name"], "shift", shift, "iou", round(best, 3), "reliable", round(reliable.sum() / max(solid.sum(), 1), 3))


if __name__ == "__main__":
    if sys.argv[1] == "build":
        cells = [[tuple(p.split("=", 1)) for p in c.split("+")] for c in sys.argv[4:]]
        build(cells, sys.argv[2], sys.argv[3])
    else:
        cut(sys.argv[2], sys.argv[3], sys.argv[4])
