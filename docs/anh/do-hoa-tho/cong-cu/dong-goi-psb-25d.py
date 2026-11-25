import sys
from pathlib import Path

from PIL import Image
from psd_tools import PSDImage
from psd_tools.api.layers import PixelLayer
from psd_tools.constants import Compression

CANVAS = 512
SOLID = {"SideArm"}

VIEWS = {
    "Front": [("FrontEars", "truoc/Ears.png"), ("FrontBody", "truoc/Body.png"), ("FrontHead", "truoc/Head.png"),
              ("FrontArmL", "truoc/ArmL.png"), ("FrontArmR", "truoc/ArmR.png")],
    "Side": [("SideBody", "ba-phan-tu/Body_Full.png"), ("SideEars", "ba-phan-tu/Ears_Back.png"),
             ("SideHead", "ba-phan-tu/Head.png"), ("SideArm", "ba-phan-tu/NearArm.png")],
}


def pack(art, view, out):
    psd = PSDImage.new("RGBA", (CANVAS, CANVAS))
    for name, file in VIEWS[view]:
        image = Image.open(art / file).convert("RGBA")
        if name in SOLID:
            alpha = image.getchannel("A").point(lambda a: 0 if a < 12 else min(255, a * 4))
            image.putalpha(alpha)
        assert image.size == (CANVAS, CANVAS), file
        left, top, right, bottom = image.getbbox()
        psd.append(PixelLayer.frompil(image.crop((left, top, right, bottom)), psd, name, top, left, Compression.RAW))
    psd._record.header.version = 2
    psd.save(out)
    print(out, [layer.name for layer in psd.descendants()])


if __name__ == "__main__":
    art, outdir = Path(sys.argv[1]), Path(sys.argv[2])
    outdir.mkdir(parents=True, exist_ok=True)
    for view in VIEWS:
        pack(art, view, outdir / f"Cream{view}.psb")
