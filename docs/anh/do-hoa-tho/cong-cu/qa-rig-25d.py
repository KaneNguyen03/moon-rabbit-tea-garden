import json
import sys
from pathlib import Path

from PIL import Image, ImageDraw

PX_PER_UNIT = 100
FLAT_W, FLAT_H = 1500, 1680
CENTER = (5.4, -3.35)
COL_STEP, ROW_STEP = 3.6, 5.6
VIEWS = ["Front", "3/4 left", "3/4 right"]


def feet_pixel(col, row):
    x = (col * COL_STEP - CENTER[0]) * PX_PER_UNIT + FLAT_W / 2
    y = FLAT_H / 2 - (-row * ROW_STEP - CENTER[1]) * PX_PER_UNIT
    return x, y


def cell(frame, col, row):
    x, y = feet_pixel(col, row)
    padded = Image.new("RGB", (frame.width + 400, frame.height + 400), frame.getpixel((2, frame.height - 2)))
    padded.paste(frame, (200, 200))
    return padded.crop((int(x + 30), int(y + 200 - 500), int(x + 370), int(y + 200 + 30)))


def gif(frames, out, scale):
    small = [f.convert("RGB").resize((int(f.width * scale), int(f.height * scale)), Image.LANCZOS) for f in frames[::2]]
    palette = small[len(small) // 2].quantize(colors=128, method=Image.Quantize.MEDIANCUT)
    small = [f.quantize(palette=palette, dither=Image.Dither.NONE) for f in small]
    small[0].save(out, save_all=True, append_images=small[1:], duration=int(2000 / 24), loop=0, optimize=True)


def main(logs, out):
    logs, out = Path(logs), Path(out)
    out.mkdir(parents=True, exist_ok=True)
    table = json.loads((logs / "beats.json").read_text())
    fps = table["fps"]
    flat = sorted((logs / "flat").glob("f*.png"))
    tilted = sorted((logs / "tilted").glob("f*.png"))
    gif([Image.open(p) for p in flat], out / "cream-25d-loops.gif", 0.4)
    gif([Image.open(p) for p in tilted], out / "cream-25d-tilted40.gif", 0.5)
    Image.open(tilted[len(tilted) // 3]).save(out / "cream-25d-tilted40.png")

    rows = []
    for col, loop in enumerate(table["loops"]):
        times = [0.0] + [b["pose"] for b in loop["beats"]]
        labels = ["rest"] + [b["name"] for b in loop["beats"]]
        for row in range(2):
            rows.append((f"{loop['clip']} - {VIEWS[row]}", [(labels[i], cell(Image.open(flat[min(len(flat) - 1, round(t * fps))]), col, row)) for i, t in enumerate(times)]))
    width = 340 * 5 + 200
    sheet = Image.new("RGB", (width, 580 * len(rows)), (36, 74, 58))
    draw = ImageDraw.Draw(sheet)
    for r, (title, cells) in enumerate(rows):
        draw.text((10, r * 580 + 260), title.replace(" - ", "\n"), fill=(250, 240, 220))
        for c, (label, image) in enumerate(cells):
            sheet.paste(image, (200 + c * 340, r * 580))
            draw.text((205 + c * 340, r * 580 + 540), label[:44], fill=(250, 240, 220))
    sheet.save(out / "cream-25d-keyframes.png")
    print(out)


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])


def layer_sheet(layers, out):
    backgrounds = [None, (61, 131, 105), (43, 36, 32)]
    files = sorted(Path(layers).rglob("*.png"))
    files = [f for f in files if not f.name.startswith("_") and f.parent.name != "plates"]
    size = 200
    sheet = Image.new("RGB", (size * 3 + 220, size * len(files) + size * 2), (36, 74, 58))
    draw = ImageDraw.Draw(sheet)
    for r, path in enumerate(files):
        image = Image.open(path).convert("RGBA")
        image = image.crop(image.getbbox())
        image.thumbnail((size - 10, size - 10), Image.LANCZOS)
        draw.text((6, r * size + size // 2), path.parent.name + "/" + path.stem, fill=(250, 240, 220))
        for c, colour in enumerate(backgrounds):
            if colour is None:
                tile = Image.new("RGB", (size, size), (200, 200, 200))
                d = ImageDraw.Draw(tile)
                for y in range(0, size, 16):
                    for x in range(0, size, 16):
                        if (x + y) // 16 % 2:
                            d.rectangle((x, y, x + 15, y + 15), fill=(245, 245, 245))
            else:
                tile = Image.new("RGB", (size, size), colour)
            tile.paste(image, ((size - image.width) // 2, (size - image.height) // 2), image)
            sheet.paste(tile, (220 + c * size, r * size))
    worst = Image.open(Path(layers) / "truoc" / "Head.png").convert("RGBA")
    box = worst.getbbox()
    patch = worst.crop((box[0], box[3] - 50, box[0] + 100, box[3]))
    zoom = Image.new("RGBA", patch.size, (43, 36, 32, 255))
    zoom.alpha_composite(patch)
    zoom = zoom.resize((patch.width * 4, patch.height * 4), Image.NEAREST)
    draw.text((6, len(files) * size + 10), "truoc/Head neck edge 400%", fill=(250, 240, 220))
    sheet.paste(zoom.convert("RGB"), (220, len(files) * size + 10))
    sheet.save(out)
