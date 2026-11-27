import shutil
import sys
from pathlib import Path

from PIL import Image
from psd_tools import PSDImage
from psd_tools.api.layers import PixelLayer
from psd_tools.constants import Compression

CANVAS = 512
KINDS = [("Kem", "kem"), ("TaiCup", "taicup"), ("DomXam", "domxam"), ("Tim", "tim")]
VIEWS = [("Front", "truoc", ["Body", "Head", "ArmL", "ArmR"]), ("Side", "ba-phan-tu", ["Body", "Head", "Arm"])]


def pack(layer_dir, prefix, names, out):
    psd = PSDImage.new("RGBA", (CANVAS, CANVAS))
    for name in names:
        image = Image.open(layer_dir / f"{name}.png").convert("RGBA")
        assert image.size == (CANVAS, CANVAS), layer_dir / name
        left, top, right, bottom = image.getbbox()
        psd.append(PixelLayer.frompil(image.crop((left, top, right, bottom)), psd, prefix + name, top, left, Compression.RAW))
    psd._record.header.version = 2
    psd.save(out)


def main(cuts, art_root):
    cuts, art_root = Path(cuts), Path(art_root)
    for kind, art in KINDS:
        dirs = [cuts / f"{art}-{suffix}" for _, suffix, _ in VIEWS]
        if not all((d / "pivots.json").exists() for d in dirs):
            print("skip", kind)
            continue
        folder = art_root / kind
        folder.mkdir(parents=True, exist_ok=True)
        for (view, _, names), layer_dir in zip(VIEWS, dirs):
            pack(layer_dir, view if view == "Front" else "Side", names, folder / f"{kind}{view}.psb")
            shutil.copyfile(layer_dir / "pivots.json", folder / f"{kind}{view}.pivots.json")
        print("packed", kind)


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
