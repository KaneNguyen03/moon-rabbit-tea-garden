import numpy as np, os
from PIL import Image, ImageDraw, ImageFilter
from ghep import *
from neo import *
import sau
# Kiểu "mũ nhỏ cài giữa hai tai": mũ hẹp hơn khoảng giữa hai tai, nghiêng theo hướng mặt, tai luôn đè lên mũ.
MINI={
 'non-la':    dict(w=0.92,sink=0.10,dx=0.00,tilt=-10,ears=True, img='hat_non-la_worn'),
 'mu-len':    dict(w=0.98,sink=0.26,dx=0.00,tilt=-6, ears=True),
 'vuong-mien':dict(w=0.62,sink=0.08,dx=0.00,tilt=-8, ears=True),
 'vong-hoa':  dict(w=1.02,sink=0.14,dx=0.00,tilt=-6, ears=True),
 'no-trang':  dict(w=0.56,sink=0.10,dx=0.24,tilt=-14,ears=False),
 'mu-sao':    dict(w=0.92,sink=0.12,dx=0.00,tilt=-12,ears=True),
}
FACING={}  # +1 quay phải (đa số), 0 thẳng
def facing(kid): return 1
def portrait3(k,hat=None,dye=None,art=343,bg=(250,244,232,255)):
    box=Image.new('RGBA',(art,art),bg)
    im=kind_img(k['id'])
    if dye: im=sau.dye_keep(im,dye)
    s=art/343; sw=round(320*s); ox=(art-sw)//2
    im=im.resize((sw,art),Image.LANCZOS); box.alpha_composite(im,(ox,0))
    if hat and k['id'] not in NOHAT:
        cx,crown,hw=anchor(k['id']); st=MINI[hat]; n=st.get('img','hat_'+hat)
        p='/tmp/dht/'+n+'.png'; hi=Image.open(p if os.path.exists(p) else R+'Assets/VuonTra/Art/Final/'+n+'.png').convert('RGBA')
        w=st['w']*hw*s; h=w*hi.height/hi.width
        hr=hi.resize((max(1,round(w)),max(1,round(h))),Image.LANCZOS)
        # pivot = đáy giữa mũ (chỗ ngồi trên đầu); nghiêng quanh điểm đó
        pad=Image.new('RGBA',(hr.width*2,hr.height*2),(0,0,0,0)); pad.alpha_composite(hr,(hr.width//2,0))
        rot=pad.rotate(st['tilt']*facing(k['id']),resample=Image.BICUBIC,center=(hr.width,hr.height))
        px=ox+(cx+st['dx']*hw)*s; py=(crown+st['sink']*hw)*s
        # bóng
        sh=Image.new('L',(art,art),0); dd=ImageDraw.Draw(sh); ew=w*0.7; eh=max(5,hw*s*0.09)
        dd.ellipse([px-ew/2,py-eh*0.6,px+ew/2,py+eh*0.4],fill=int(255*0.28)); sh=sh.filter(ImageFilter.GaussianBlur(eh*0.35))
        body=Image.new('L',(art,art),0); body.paste(im.split()[3],(ox,0))
        sh=Image.fromarray((np.asarray(sh,float)*np.asarray(body,float)/255).astype('uint8'))
        layer=Image.new('RGBA',(art,art),(0x3A,0x2A,0x4D,0)); layer.putalpha(sh); box.alpha_composite(layer)
        box.alpha_composite(rot,(round(px-hr.width),round(py-hr.height)))
        if st['ears']:
            cut=crown*s; rows=np.arange(art)[:,None]
            m=np.clip((cut-rows)/(4*s),0,1)*np.ones((1,sw))
            ear=im.copy(); a=np.asarray(ear.split()[3],float)*m; ear.putalpha(Image.fromarray(a.astype('uint8')))
            box.alpha_composite(ear,(ox,0))
    return box
