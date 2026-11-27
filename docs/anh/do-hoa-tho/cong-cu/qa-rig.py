import json
import sys

from PIL import Image, ImageDraw

d = sys.argv[1]
out = sys.argv[2]
L = {n: Image.open(f"{d}/{n}.png") for n in ("Body_Full", "Ears_Back", "Head", "NearArm", "Hat_Crown", "Hat_Beanie", "Master")}
P = json.load(open(f"{d}/pivots.json"))
BGS = [(255, 244, 224), (61, 131, 105), (27, 30, 69)]
INK = (58, 42, 77)
T = 300


def rot(im, ang, c):
    return im.rotate(ang, resample=Image.BICUBIC, center=tuple(c))


def pose(head=0, ears=0, arm=0, hat=None):
    ears_im = rot(L["Ears_Back"], ears, P["ear_base"])
    head_group = Image.new("RGBA", (512, 512))
    head_group.alpha_composite(ears_im)
    head_group.alpha_composite(L["Head"])
    if hat:
        head_group.alpha_composite(L[hat])
    c = Image.new("RGBA", (512, 512))
    c.alpha_composite(L["Body_Full"])
    c.alpha_composite(rot(head_group, -head, P["neck"]))
    c.alpha_composite(rot(L["NearArm"], arm, P["shoulder"]))
    return c


def tile(im, bg, label):
    t = Image.new("RGBA", (512, 512), bg + (255,))
    t.alpha_composite(im)
    t = t.resize((T, T), Image.LANCZOS)
    full = Image.new("RGB", (T, T + 26), (255, 244, 224))
    full.paste(t, (0, 26))
    ImageDraw.Draw(full).text((6, 6), label, fill=INK)
    return full


row1 = [(L[n], BGS[0], n) for n in ("Body_Full", "Ears_Back", "Head", "NearArm")]
row1 += [(L["Master"], bg, f"Master {k}") for k, bg in zip(("light", "forest", "night"), BGS)]
row2 = [(pose(head=-12), BGS[1], "Head -12"), (pose(head=12), BGS[1], "Head +12"),
        (pose(ears=-15), BGS[1], "Ears -15"), (pose(ears=15), BGS[1], "Ears +15"),
        (pose(arm=-25), BGS[1], "NearArm -25"), (pose(arm=45), BGS[1], "NearArm +45"),
        (pose(), BGS[0], "rest")]
row3 = [(pose(hat="Hat_Crown"), BGS[1], "Crown"), (pose(head=-12, hat="Hat_Crown"), BGS[1], "Crown head -12"),
        (pose(head=12, hat="Hat_Crown"), BGS[1], "Crown head +12"), (pose(hat="Hat_Beanie"), BGS[1], "Beanie"),
        (pose(head=-12, hat="Hat_Beanie"), BGS[1], "Beanie head -12"), (pose(head=12, hat="Hat_Beanie"), BGS[2], "Beanie head +12"),
        (pose(head=8, ears=-10, arm=30, hat="Hat_Beanie"), BGS[2], "combined")]
rows = [row1, row2, row3]
sheet = Image.new("RGB", (7 * (T + 8) + 8, len(rows) * (T + 34) + 8), (255, 244, 224))
for r, row in enumerate(rows):
    for c, (im, bg, label) in enumerate(row):
        sheet.paste(tile(im, bg, label), (8 + c * (T + 8), 8 + r * (T + 34)))
sheet.save(out)
