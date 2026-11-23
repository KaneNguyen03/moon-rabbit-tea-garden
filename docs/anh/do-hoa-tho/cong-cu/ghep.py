# Ghép mũ lên thỏ đúng như RabbitGridView.PlaceHat (portrait, art = 343 px).
import re, os, sys
from PIL import Image, ImageDraw, ImageFont
R='/Users/hiquyennek/Documents/VuonTraNam/'
src=open(R+'Assets/VuonTra/Core/RabbitKinds.cs').read()
KINDS=[]
for m in re.finditer(r'new RabbitKind\((?:"([^"]+)"|NewMoon\.MoonKind),\s*(\d),\s*RabbitTrade\.(\w+),\s*"([^"]+)"[^\n]*', src):
    line=m.group(0); kid=m.group(1) or 'tho-trang-non'
    hx=re.search(r'hatX:\s*(-?[\d.]+)f',line); hy=re.search(r'hatY:\s*(-?[\d.]+)f',line)
    KINDS.append(dict(id=kid,tier=int(m.group(2)),trade=m.group(3),name=m.group(4),
        hx=float(hx.group(1)) if hx else 0., hy=float(hy.group(1)) if hy else 0.,
        nohat='noHat: true' in line, own='ownOutline: true' in line, glow='ownGlow: true' in line))
HEADTOP=0.755; HEADX=150.5/320; ASPECT=320/343; HATLIFT=0.117; HATSIZE=0.44
HATS={'non-la':0.0,'vong-hoa':0.06,'mu-len':0.0,'no-trang':0.05,'mu-sao':0.02,'vuong-mien':0.04}
DYES=[('hong','F28BB5'),('dao','FFB088'),('vang-chanh','F5E36B'),('bac-ha','7FD9B0'),('xanh-troi','7EC2F0'),('oai-huong','B49BE0'),('kem','FFF0C8'),('than','6A6478')]
def kind_img(kid):
    g=R+'Assets/VuonTra/Art/Generated/kind_%s.png'%kid
    return Image.open(g if os.path.exists(g) else R+'Assets/VuonTra/Art/Final/kind_%s.png'%kid).convert('RGBA')
def hat_img(h): return Image.open(R+'Assets/VuonTra/Art/Final/hat_%s.png'%h).convert('RGBA')
def hexc(h): return tuple(int(h[i:i+2],16) for i in (0,2,4))
def fur(dye):
    if dye is None: return (255,255,255)
    c=hexc(dye); return tuple(round(255*0.4+v*0.6) for v in c)
def tint(im,col):
    r,g,b,a=im.split()
    r=r.point(lambda v:v*col[0]//255); g=g.point(lambda v:v*col[1]//255); b=b.point(lambda v:v*col[2]//255)
    return Image.merge('RGBA',(r,g,b,a))
def place(k,hat,art=343):
    """Returns (x_center, y_bottom_from_box_bottom, w, h) of the hat in box pixels, as the code does."""
    ic=hat_img(hat); w=h=art*HATSIZE; ratio=ic.height/ic.width
    if ratio<=1: h=w*ratio
    else: w=h/ratio
    across=(HEADX-0.5+k['hx'])*art*ASPECT
    headline=(HEADTOP+k['hy'])*art
    y=headline+(HATLIFT-HATSIZE/2+HATS[hat])*art
    return art/2+across, y, w, h
def portrait(k,hat=None,dye=None,art=343,bg=(250,244,232,255)):
    box=Image.new('RGBA',(art,art),bg)
    im=kind_img(k['id']); im=tint(im,fur(dye)) if dye else im
    sw=round(art*320/343); im=im.resize((sw,art),Image.LANCZOS)
    box.alpha_composite(im,((art-sw)//2,0))
    if hat and not k['nohat']:
        cx,yb,w,h=place(k,hat,art); hi=hat_img(hat).resize((max(1,round(w)),max(1,round(h))),Image.LANCZOS)
        box.alpha_composite(hi,(round(cx-w/2),round(art-yb-h)))
    return box
