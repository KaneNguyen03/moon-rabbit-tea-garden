import os
import re
import sys
import unicodedata

from fontTools.ttLib import TTFont

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..")
FONTS = os.path.join(ROOT, "Assets", "VuonTra", "Fonts")
STRINGS = os.path.join(ROOT, "Assets", "VuonTra", "Editor", "L10nStrings.cs")
TEXT_FACES = ("Nunito-Regular.ttf", "Nunito-Bold.ttf", "Baloo2-Regular.ttf", "Baloo2-Bold.ttf")
SYMBOL_FACES = ("NotoSansSymbols2-Subset.ttf", "NotoSansSymbols-Subset.ttf")
LICENCES = ("Nunito-OFL.txt", "Baloo2-OFL.txt", "NotoSansSymbols-OFL.txt")
VOWELS = "aăâeêioôơuưyAĂÂEÊIOÔƠUƯY"
TONES = ("", "̀", "́", "̉", "̃", "̣")


def vietnamese():
    letters = {unicodedata.normalize("NFC", vowel + tone) for vowel in VOWELS for tone in TONES}
    return letters | set("đĐ")


def covered(face):
    return set(TTFont(os.path.join(FONTS, face)).getBestCmap())


def string_literals(path):
    with open(path, encoding="utf-8") as source:
        text = source.read()
    found = set()
    for literal in re.findall(r'"((?:[^"\\\n]|\\.)*)"', text):
        literal = re.sub(r"\\u([0-9A-Fa-f]{4})", lambda match: chr(int(match.group(1), 16)), literal)
        found |= {c for c in literal if not c.isspace() and c != "\\"}
    return found


def main():
    problems = []
    for licence in LICENCES:
        if not os.path.isfile(os.path.join(FONTS, licence)):
            problems.append(f"{licence} is missing beside the fonts")
    for face in TEXT_FACES:
        cmap = covered(face)
        missing = sorted(c for c in vietnamese() if ord(c) not in cmap)
        if missing:
            problems.append(f"{face} lacks Vietnamese letters: {' '.join(missing)}")
        if "fvar" in TTFont(os.path.join(FONTS, face)):
            problems.append(f"{face} is a variable font; Unity's uGUI text needs a static face")
    symbols = set().union(*(covered(face) for face in SYMBOL_FACES))
    used = string_literals(STRINGS)
    for face in TEXT_FACES:
        drawn = covered(face) | symbols
        missing = sorted(c for c in used if ord(c) not in drawn)
        if missing:
            problems.append(f"{face} with the symbol faces cannot draw: " + " ".join(f"{c} U+{ord(c):04X}" for c in missing))
    for problem in problems:
        print("fonts: " + problem)
    if problems:
        return 1
    print(f"fonts: ok ({len(TEXT_FACES)} text faces carry all {len(vietnamese())} Vietnamese letters; {len(used)} characters of the string tables are drawn by bundled faces)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
