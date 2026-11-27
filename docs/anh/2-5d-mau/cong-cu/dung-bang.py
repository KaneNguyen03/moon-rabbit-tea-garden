import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

GRASS = (0x3D, 0x83, 0x69)
SHEET_BG = (0x2E, 0x5E, 0x49)
NIGHT = (0x1B, 0x1E, 0x45)
CREAM = (0xFF, 0xF4, 0xE0)
INK = (0x3A, 0x2A, 0x4D)
LANTERN = (0xFF, 0xC8, 0x66)
FONT = "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"
FONT_BOLD = "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"
SCREEN_PX_PER_METRE = 124
RABBIT_METRES = 1.15
RABBIT_SOURCE_PX = 448


def font(size, bold=False):
    return ImageFont.truetype(FONT_BOLD if bold else FONT, size)


def opaque_box(im):
    return im.getchannel("A").point(lambda v: 255 if v > 24 else 0).getbbox()


def trimmed(im):
    return im.crop(opaque_box(im))


def rabbit(layer_dir):
    order = ["Ears", "Body", "Head", "ArmL", "ArmR"]
    canvas = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    for name in order:
        canvas.alpha_composite(Image.open(Path(layer_dir) / f"{name}.png").convert("RGBA"))
    return canvas


def outline_px(im):
    a = np.array(im.convert("RGBA")).astype(int)
    alpha = a[:, :, 3] > 128
    lum = a[:, :, 0] * 0.3 + a[:, :, 1] * 0.59 + a[:, :, 2] * 0.11
    runs = []
    for y in range(0, a.shape[0], 4):
        xs = np.where(alpha[y])[0]
        if len(xs) < 10:
            continue
        x, run = xs[0], 0
        while x < a.shape[1] and alpha[y, x] and lum[y, x] < 95:
            run += 1
            x += 1
        if run:
            runs.append(run)
    return float(np.median(runs)) if runs else 0.0


def style_ref(layer_dir, stall, lantern, out):
    pieces = [(rabbit(layer_dir), 1.0), (Image.open(stall).convert("RGBA"), 2.0), (Image.open(lantern).convert("RGBA"), 1.5)]
    unit = 300
    sheet = Image.new("RGB", (1536, 768), SHEET_BG)
    x = 60
    for im, rabbits in pieces:
        im = trimmed(im)
        h = int(unit * rabbits)
        w = int(im.width * h / im.height)
        im = im.resize((w, h), Image.LANCZOS)
        sheet.paste(im, (x, 708 - h), im)
        x += w + 90
    sheet.save(out)


def light_pool(out, size=512):
    yy, xx = np.mgrid[0:size, 0:size].astype(float)
    r = np.hypot(xx - size / 2 + 0.5, yy - size / 2 + 0.5) / (size / 2)
    fall = np.clip(1 - r, 0, 1)
    alpha = (fall ** 1.6) * 0.45
    core = np.clip(1 - r / 0.35, 0, 1) ** 2
    rgb = np.zeros((size, size, 3))
    for i in range(3):
        rgb[:, :, i] = LANTERN[i] * (1 - core) + CREAM[i] * core
    img = np.dstack([rgb, alpha * 255]).astype(np.uint8)
    Image.fromarray(img, "RGBA").save(out)


def label(draw, xy, text, size=28, fill=CREAM, bold=False):
    draw.text(xy, text, font=font(size, bold), fill=fill)


def before_board(panels, out, title):
    height = 1280
    gap = 36
    scaled = []
    for path, caption in panels:
        im = Image.open(path).convert("RGB")
        w = int(im.width * height / im.height)
        scaled.append((im.resize((w, height), Image.LANCZOS), caption))
    width = sum(im.width for im, _ in scaled) + gap * (len(scaled) + 1)
    board = Image.new("RGB", (width, height + 230), NIGHT)
    draw = ImageDraw.Draw(board)
    label(draw, (gap, 28), title, 44, bold=True)
    x = gap
    for im, caption in scaled:
        board.paste(im, (x, 110))
        lines = caption.split("\n")
        for i, line in enumerate(lines):
            label(draw, (x, 110 + height + 18 + i * 34), line, 26)
        x += im.width + gap
    board.save(out)


def audit_sheet(items, rabbit_im, out):
    zoom = 2
    px_per_m = SCREEN_PX_PER_METRE * zoom
    cell_w = 300
    ground_y = 760
    cols = 7
    rows = (len(items) + cols) // cols
    board = Image.new("RGB", (cols * cell_w + 40, rows * 880 + 150), GRASS)
    draw = ImageDraw.Draw(board)
    label(draw, (24, 24), "Hiện trạng 2.5D theo đúng tỉ lệ màn hình (phóng 2x so với zoom mặc định, 124 px/m)", 34, bold=True)
    label(draw, (24, 74), "Số dưới mỗi hình: nét viền ngoài đo trên màn hình ở zoom mặc định. Chuẩn mục 9: 2-3 px.", 26)
    entries = [("tho-kem (rig)", rabbit_im, RABBIT_METRES, "Thỏ Kem, mẫu nét")] + items
    for k, (name, im, metres, note) in enumerate(entries):
        col, row = k % cols, k // cols
        box = trimmed(im)
        h = max(8, int(metres * px_per_m))
        w = int(box.width * h / box.height)
        shown = box.resize((w, h), Image.BILINEAR)
        x0 = 20 + col * cell_w
        y0 = 150 + row * 880
        if w > cell_w - 10:
            f = (cell_w - 10) / w
            shown = shown.resize((int(w * f), int(h * f)), Image.BILINEAR)
        board.paste(shown, (x0 + (cell_w - shown.width) // 2, y0 + ground_y - shown.height - 60), shown)
        screen_line = outline_px(box) * (metres * SCREEN_PX_PER_METRE) / box.height
        draw.line((x0 + 10, y0 + ground_y - 60, x0 + cell_w - 10, y0 + ground_y - 60), fill=INK, width=2)
        label(draw, (x0 + 6, y0 + ground_y - 48), name, 22, bold=True)
        label(draw, (x0 + 6, y0 + ground_y - 20), f"{metres:.2f} m · nét {screen_line:.1f} px", 22)
        label(draw, (x0 + 6, y0 + ground_y + 8), note, 20)
    board.save(out)


def main():
    spec = json.loads(Path(sys.argv[1]).read_text())
    kind = spec["kind"]
    if kind == "style-ref":
        style_ref(spec["rabbit_layers"], spec["stall"], spec["lantern"], spec["out"])
    elif kind == "light-pool":
        light_pool(spec["out"], spec.get("size", 512))
    elif kind == "before":
        before_board([(p["path"], p["caption"]) for p in spec["panels"]], spec["out"], spec["title"])
    elif kind == "audit":
        items = [(i["name"], Image.open(i["path"]).convert("RGBA"), i["metres"], i["note"]) for i in spec["items"]]
        audit_sheet(items, rabbit(spec["rabbit_layers"]), spec["out"])


if __name__ == "__main__":
    main()
