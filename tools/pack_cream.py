import sys
from pathlib import Path

from PIL import Image
from psd_tools import PSDImage
from psd_tools.api.layers import Group, PixelLayer
from psd_tools.constants import Compression

ART = Path(sys.argv[1])
OUT = Path(sys.argv[2])
CANVAS = 512

psd = PSDImage.new("RGBA", (CANVAS, CANVAS))


def add(parent, name, file):
    image = Image.open(ART / file).convert("RGBA")
    assert image.size == (CANVAS, CANVAS), file
    left, top, right, bottom = image.getbbox()
    layer = PixelLayer.frompil(image.crop((left, top, right, bottom)), psd, name, top, left, Compression.RAW)
    parent.append(layer)


add(psd, "Body_Full", "Body_Full.png")
add(psd, "Ears_Back", "Ears_Back.png")
add(psd, "Head", "Head.png")
hat = Group.new(name="Hat", parent=psd)
add(hat, "Crown", "Hat_Crown.png")
add(hat, "Beanie", "Hat_Beanie.png")
arm = Group.new(name="NearArm_Job", parent=psd)
add(arm, "ArmIdle", "NearArm.png")
if (ART / "NearArm_Brew.png").exists():
    add(arm, "ArmBrew", "NearArm_Brew.png")

psd._record.header.version = 2
psd.save(OUT)
print(OUT, [layer.name for layer in psd.descendants()])
psd.composite(layer_filter=lambda layer: layer.name not in ("Beanie", "ArmBrew") and layer.is_visible()).save(OUT.with_suffix(".preview.png"))
