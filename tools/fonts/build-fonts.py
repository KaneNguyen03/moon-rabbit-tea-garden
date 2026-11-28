import hashlib
import os
import sys

from fontTools import subset
from fontTools.ttLib import TTFont
from fontTools.varLib import instancer

TEXT_FAMILIES = (("Nunito", "Nunito[wght].ttf"), ("Baloo2", "Baloo2[wght].ttf"))
TEXT_STYLES = (("Regular", 400), ("Bold", 700))
LINE_ASCENT, LINE_DESCENT = 1011, -353
SYMBOL_UNITS_PER_EM = 1250
SYMBOL_SUBSETS = (
    ("NotoSansSymbols2-Subset.ttf", "NotoSansSymbols2-Regular.ttf", None, "▲▶▼◀★♥✎✓✦"),
    ("NotoSansSymbols-Subset.ttf", "NotoSansSymbols[wght].ttf", 400, "→♪"),
)


def load(path, weight):
    font = TTFont(path, recalcTimestamp=False)
    if weight is None:
        return font
    return instancer.instantiateVariableFont(font, {"wght": weight}, updateFontNames=True)


def share_line_metrics(font):
    scale = font["head"].unitsPerEm / 1000
    ascent, descent = round(LINE_ASCENT * scale), round(LINE_DESCENT * scale)
    font["hhea"].ascent, font["hhea"].descent, font["hhea"].lineGap = ascent, descent, 0
    font["OS/2"].sTypoAscender, font["OS/2"].sTypoDescender, font["OS/2"].sTypoLineGap = ascent, descent, 0


def report(path):
    with open(path, "rb") as written:
        print(hashlib.sha256(written.read()).hexdigest(), os.path.basename(path))


def main(source_dir, out_dir):
    for family, source in TEXT_FAMILIES:
        for style, weight in TEXT_STYLES:
            path = os.path.join(out_dir, f"{family}-{style}.ttf")
            font = load(os.path.join(source_dir, source), weight)
            share_line_metrics(font)
            font.save(path)
            report(path)
    for name, source, weight, symbols in SYMBOL_SUBSETS:
        font = load(os.path.join(source_dir, source), weight)
        options = subset.Options(layout_features=[], name_IDs=["*"], notdef_outline=True)
        cutter = subset.Subsetter(options)
        cutter.populate(text=symbols)
        cutter.subset(font)
        font["head"].unitsPerEm = SYMBOL_UNITS_PER_EM
        share_line_metrics(font)
        path = os.path.join(out_dir, name)
        font.save(path)
        report(path)


if __name__ == "__main__":
    if len(sys.argv) != 3:
        sys.exit("usage: build-fonts.py <folder with the four upstream fonts> <output folder>")
    main(sys.argv[1], sys.argv[2])
