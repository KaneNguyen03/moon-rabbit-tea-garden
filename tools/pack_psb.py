import sys
from pathlib import Path
from PIL import Image
from psd_tools import PSDImage
from psd_tools.api.layers import Group, PixelLayer
from psd_tools.constants import Compression

ART = Path(sys.argv[1])
OUT = Path(sys.argv[2])
CANVAS = 512

BUNNY = ART / "Bunny" / "tho-bong"
FINAL = ART / "Final"


def scaled(path, width=None, height=None, flip=False, turn=False):
    image = Image.open(path).convert("RGBA")
    image = image.crop(image.getbbox())
    if flip:
        image = image.transpose(Image.Transpose.FLIP_TOP_BOTTOM)
    if turn:
        image = image.transpose(Image.Transpose.ROTATE_90)
    if width:
        height = round(image.height * width / image.width)
    else:
        width = round(image.width * height / image.height)
    return image.resize((width, height), Image.Resampling.LANCZOS)


def bunny_part(name):
    image = Image.open(BUNNY / f"{name}.png").convert("RGBA")
    left, top, right, bottom = image.getbbox()
    return image.crop((left, top, right, bottom)), left, top


def centered_on(image, center_x, base_y):
    return image, round(center_x - image.width / 2), round(base_y - image.height)


def gripped_at(image, grip_x, grip_y, grip_fraction):
    return image, round(grip_x - image.width / 2), round(grip_y - image.height * grip_fraction)


psd = PSDImage.new("RGBA", (CANVAS, CANVAS))


def add(parent, name, placed):
    image, left, top = placed
    layer = PixelLayer.frompil(image, psd, name, top, left, Compression.RAW)
    parent.append(layer)
    return layer


add(psd, "Ears", bunny_part("ears"))
add(psd, "Body", bunny_part("body"))
add(psd, "Head", bunny_part("head"))

hat = Group.new(name="Hat", parent=psd)
add(hat, "VuongMien", centered_on(scaled(FINAL / "hat_vuong-mien.png", width=96), 246, 214))
add(hat, "VongHoa", centered_on(scaled(FINAL / "hat_vong-hoa.png", width=150), 244, 222))

tool = Group.new(name="Tool", parent=psd)
add(tool, "Spoon", gripped_at(scaled(FINAL / "tool_brewer.png", height=132, flip=True), 296, 409, 0.32))
add(tool, "RollingPin", gripped_at(scaled(FINAL / "tool_baker.png", width=130, turn=True), 296, 412, 0.5))
add(tool, "Basket", gripped_at(scaled(FINAL / "tool_gatherer.png", height=118, flip=True), 296, 409, 0.2))
add(tool, "Telescope", gripped_at(scaled(FINAL / "tool_stargazer.png", height=112, flip=True), 296, 409, 0.45))
add(psd, "Arm", bunny_part("arm"))
add(psd, "Paw", bunny_part("paw"))

psd._record.header.version = 2
psd.save(OUT)
print(OUT, [layer.name for layer in psd.descendants()])

preview = psd.composite(layer_filter=lambda layer: layer.name not in ("VongHoa", "RollingPin", "Basket", "Telescope") and layer.is_visible())
preview.save(OUT.with_suffix(".preview.png"))
