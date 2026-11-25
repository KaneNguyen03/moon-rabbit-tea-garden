import sys

from PIL import Image, ImageDraw, ImageFont

FONT = "/System/Library/Fonts/Supplemental/Arial Unicode.ttf"
SIZE = (1080, 1920)
BAND = 140
GAP = 24
NIGHT = (0x1B, 0x1E, 0x45)
PAPER = (0xFF, 0xF4, 0xE0)


def panel(path, label, font):
    image = Image.open(path).convert("RGB").resize(SIZE, Image.LANCZOS)
    framed = Image.new("RGB", (SIZE[0], SIZE[1] + BAND), NIGHT)
    framed.paste(image, (0, BAND))
    draw = ImageDraw.Draw(framed)
    width = draw.textlength(label, font=font)
    draw.text(((SIZE[0] - width) / 2, (BAND - font.size) / 2 - 6), label, font=font, fill=PAPER)
    return framed


def main(left, right, out):
    font = ImageFont.truetype(FONT, 64)
    a = panel(left, "2D hiện tại", font)
    b = panel(right, "2.5D spike", font)
    sheet = Image.new("RGB", (a.width * 2 + GAP, a.height), NIGHT)
    sheet.paste(a, (0, 0))
    sheet.paste(b, (a.width + GAP, 0))
    sheet.save(out)


if __name__ == "__main__":
    main(*sys.argv[1:4])
