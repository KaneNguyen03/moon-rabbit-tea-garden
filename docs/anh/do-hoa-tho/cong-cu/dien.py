import math, sys
from PIL import Image, ImageDraw, ImageFont, ImageFilter
from rig import *
INK=(0x3A,0x2A,0x4D)
TOOLS={t:Image.open(R+'docs/anh/po-2/tool_%s.png'%t).convert('RGBA') for t in ['brewer','baker','gatherer','stargazer']}
def arm(fur, length=58, width=30):
    """cánh tay gần: con nhộng lông từ vai (trên) xuống bàn tay (dưới), viền mực"""
    S=4; L=int(length*S); Wd=int(width*S); im=Image.new('RGBA',(Wd+8*S,L+8*S),(0,0,0,0)); d=ImageDraw.Draw(im)
    d.rounded_rectangle([4*S,4*S,4*S+Wd,4*S+L],radius=Wd//2,fill=INK+(255,))
    o=int(2.4*S); d.rounded_rectangle([4*S+o,4*S+o,4*S+Wd-o,4*S+L-o],radius=Wd//2-o,fill=tuple(fur)+(255,))
    shade=tuple(int(c*0.86) for c in fur)
    d.rounded_rectangle([4*S+Wd*0.55,4*S+L*0.15,4*S+Wd-o,4*S+L-o-Wd*0.3],radius=Wd//4,fill=shade+(120,))
    for k in (0.36,0.64): d.line([(4*S+Wd*k,4*S+L-Wd*0.38),(4*S+Wd*k,4*S+L-o-2*S)],fill=INK+(170,),width=int(1.6*S))
    im=im.filter(ImageFilter.GaussianBlur(0.6*S)).resize((im.width//S,im.height//S),Image.LANCZOS)
    return im, (im.width/2, 4+width/2), (im.width/2, im.height-4-width/2)   # ảnh, vai, bàn tay
# nhịp từng nghề: góc thân, co giãn, nâng, góc đầu (trễ), góc tai (trễ hơn), góc tay, góc dụng cụ
def pose(trade,t):
    s=lambda p=0:math.sin(2*math.pi*t-p)
    c=lambda p=0:math.cos(2*math.pi*t-p)
    if trade=='brewer':   # khuấy: tay trước bụng, thìa cắm xuống nồi, vòng tròn
        return dict(body=-2*s(), sx=1-0.012*s()**2, sy=1+0.012*s()**2, lift=0, head=-5+2.5*s(0.5), ears=-3+7*s(1.2),
                    arm=18+12*s(), tool=180+16*c(), steam=True)
    if trade=='baker':    # cán: nhún xuống mỗi nhịp, cây cán ngang ấn xuống
        k=max(0,s())**2
        return dict(body=-3*k, sx=1+0.05*k, sy=1-0.075*k, lift=0, head=-4-5*max(0,s(0.5))**2, ears=10*max(0,s(1.0))**2,
                    arm=30-18*k, tool=-90+4*s(), flour=k)
    if trade=='gatherer': # hái: nhón chân với tay lên phía phải, rổ đưa
        k=(1-math.cos(2*math.pi*t))/2
        return dict(body=-3*s(), sx=1-0.03*k, sy=1+0.05*k, lift=12*k, head=6+5*k, ears=5-14*k, arm=125+35*k, tool=10*s(0.6), leaf=True)
    return dict(body=5+1.5*s(), sx=1, sy=1+0.01*s(), lift=0, head=10+2*s(0.4), ears=-3*s(1.2), arm=158+3*s(), tool=-42+4*s(), star=True)
def frame(kid,trade,t,size=360,bg=(250,244,232,255)):
    P,fur=parts(kid); p=pose(trade,t)
    C=Image.new('RGBA',(size,size),bg)
    off=((size-W)//2, size-H-16)
    feet=(FEET[0]+off[0],FEET[1]+off[1]-p['lift'])
    sh=Image.new('L',(size,size),0); ImageDraw.Draw(sh).ellipse([size/2-95+p['lift'],size-34,size/2+95-p['lift'],size-12],fill=int(70-2*p['lift']))
    C.paste(Image.new('RGBA',(size,size),INK+(255,)),(0,0),sh.filter(ImageFilter.GaussianBlur(6)))
    # thân
    place(C,P['body'],FEET,feet,angle=p['body'],sx=p['sx'],sy=p['sy'])
    neck=xf(NECK,FEET,p['body'],p['sx'],p['sy'],feet)
    # tai (sau đầu với tai cụp thì nằm trong đầu)
    ang_h=p['body']+p['head']
    for name in ['ears']:
        base=(167,100); bd=xf(base,NECK,ang_h,dst=neck)
        place(C,P['ears'],base,bd,angle=ang_h+p['ears'])
    place(C,P['head'],NECK,neck,angle=ang_h)
    # tay gần + dụng cụ, xoay quanh vai
    sh_pt=xf(SHOULDER,FEET,p['body'],p['sx'],p['sy'],feet)
    a_img,a_sh,a_hand=arm(fur,length=0.17*343)
    hand=(sh_pt[0]+ (a_hand[1]-a_sh[1])*math.sin(math.radians(-p['arm'])) , sh_pt[1]+(a_hand[1]-a_sh[1])*math.cos(math.radians(-p['arm'])))
    hand=xf((a_hand[0],a_hand[1]),a_sh,p['arm'],dst=sh_pt)
    tl=TOOLS[trade]; L={'brewer':120,'baker':130,'gatherer':105,'stargazer':95}[trade]
    tw=int(tl.width*L/tl.height); tool=tl.resize((tw,L),Image.LANCZOS)
    grip={'brewer':0.78,'baker':0.5,'gatherer':0.86,'stargazer':0.7}[trade]
    d=ImageDraw.Draw(C,'RGBA')
    u=t
    for i in range(4):
        v=(u+i/4)%1
        if p.get('steam'):
            x=hand[0]+40+12*math.sin(2*math.pi*(v*1.3+i)); y=hand[1]+30-110*v; r=8+10*v
            d.ellipse([x-r,y-r*0.7,x+r,y+r*0.7],fill=(255,255,255,int(170*(1-v))))
        if p.get('flour',0)>0.4:
            a=i*1.7+0.4; x=hand[0]+math.cos(a)*40*(0.4+v); y=hand[1]+50-abs(math.sin(a))*26*(0.4+v); r=4.5
            d.ellipse([x-r,y-r,x+r,y+r],fill=(255,250,240,220))
        if p.get('leaf'):
            x=hand[0]+40-60*v+10*math.sin(6*v+i); y=hand[1]-130+150*v
            d.ellipse([x-8,y-3.5,x+8,y+3.5],fill=(111,191,138,int(240*(1-0.5*v))),outline=INK+(150,))
        if p.get('star'):
            x=hand[0]+70+35*math.cos(i*1.7)+20*v; y=hand[1]-110-30*math.sin(i*2.1)-20*v; s2=7*(1-abs(2*v-1))+1
            d.polygon([(x,y-s2*2),(x+s2*.5,y-s2*.5),(x+s2*2,y),(x+s2*.5,y+s2*.5),(x,y+s2*2),(x-s2*.5,y+s2*.5),(x-s2*2,y),(x-s2*.5,y-s2*.5)],fill=(255,200,102,235))
    place(C,tool,(tw/2,L*grip),hand,angle=p['tool'])
    pw=Image.open('/Users/hiquyennek/Documents/VuonTraNam-mu/Assets/VuonTra/Art/Final/paw.png').convert('RGBA')
    import numpy as _np
    a=_np.asarray(pw).astype(float); a[...,:3]*=_np.array(fur)/255; pw=Image.fromarray(a.astype('uint8'))
    pw=pw.resize((int(0.085*343),int(0.075*343)),Image.LANCZOS)
    C.alpha_composite(pw,(int(hand[0]-pw.width/2),int(hand[1]-pw.height/2)))
    return C
if __name__=='__main__':
    kinds=sys.argv[1].split(','); out=sys.argv[2]; N=int(sys.argv[3]) if len(sys.argv)>3 else 16; c=300
    F=ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial Bold.ttf',20)
    names={'brewer':'Pha trà','baker':'Bếp bánh','gatherer':'Hái lá','stargazer':'Ngắm sao'}
    frames=[]
    for f in range(N):
        sheet=Image.new('RGBA',(c*4,c*len(kinds)+34),(250,244,232,255)); d=ImageDraw.Draw(sheet)
        for j,tr in enumerate(names): d.text((j*c+c//2-40,8),names[tr],font=F,fill=INK)
        for r,k in enumerate(kinds):
            for j,tr in enumerate(names):
                sheet.alpha_composite(frame(k,tr,f/N).resize((c,c),Image.LANCZOS),(j*c,34+r*c))
        frames.append(sheet.convert('RGB'))
    frames[0].save(out,save_all=True,append_images=frames[1:],duration=int(1000/N),loop=0)
    frames[N//4].save(out.replace('.gif','_khung.png'))
