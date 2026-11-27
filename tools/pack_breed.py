import sys
from pathlib import Path

from PIL import Image
from psd_tools import PSDImage
from psd_tools.api.layers import Group, PixelLayer
from psd_tools.constants import Compression

ART = Path(sys.argv[1])
OUT = Path(sys.argv[2])
BASE = len(sys.argv) > 3
LOP = Path(sys.argv[3]) if BASE else ART / "Ears_Lop.png"
CANVAS = 512

ARMS = [("ArmIdle", "NearArm.png"), ("ArmBrew", "NearArm_Brew.png"), ("ArmBake", "NearArm_Bake.png"),
        ("ArmGather", "NearArm_Gather.png"), ("ArmStargaze", "NearArm_Stargaze.png"), ("ArmDoze", "NearArm_Doze.png")]
HATS = [("Crown", "Hat_Crown.png"), ("Beanie", "Hat_Beanie.png"), ("NonLa", "Hat_NonLa.png"), ("Bow", "Hat_Bow.png")]

psd = PSDImage.new("RGBA", (CANVAS, CANVAS))


def add(parent, name, path):
    image = Image.open(path).convert("RGBA")
    assert image.size == (CANVAS, CANVAS), path
    left, top, right, bottom = image.getbbox()
    parent.append(PixelLayer.frompil(image.crop((left, top, right, bottom)), psd, name, top, left, Compression.RAW))


def group(name, items):
    g = Group.new(name=name, parent=psd)
    for layer, file in items:
        add(g, layer, ART / file)


add(psd, "Body_Full", ART / "Body_Full.png")
if BASE:
    group("Outfit", [("Apron", "Outfit_Apron.png")])
add(psd, "Ears_Back", ART / "Ears_Back.png")
group("Head", [("HeadAwake", "Head.png"), ("HeadDoze", "Head_Doze.png")])
if LOP.exists():
    add(psd, "Ears_Lop", LOP)
if BASE:
    group("Hat", HATS)
for hat, file in HATS:
    fitted = ART / file.replace(".png", "_Lop.png")
    if fitted.exists():
        add(psd, hat + "Lop", fitted)
group("NearArm_Job", ARMS)

psd._record.header.version = 2
psd.save(OUT)
Path(str(OUT) + ".meta").unlink(missing_ok=True)
print(OUT, [layer.name for layer in psd.descendants()])
