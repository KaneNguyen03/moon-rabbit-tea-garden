import sys, os, json
import numpy as np
from PIL import Image, ImageFilter
SRC, OUT = sys.argv[1], sys.argv[2]
def rgb2hsv(rgb):
    r,g,b = rgb[...,0],rgb[...,1],rgb[...,2]; mx=rgb.max(-1); mn=rgb.min(-1); d=mx-mn
    h=np.zeros_like(mx); m=d>1e-6
    h=np.where(m&(mx==r),((g-b)/np.maximum(d,1e-6))%6,h)
    h=np.where(m&(mx==g)&(mx!=r),(b-r)/np.maximum(d,1e-6)+2,h)
    h=np.where(m&(mx==b)&(mx!=r)&(mx!=g),(r-g)/np.maximum(d,1e-6)+4,h)
    return np.stack([h*60%360, np.where(mx>0,d/np.maximum(mx,1e-6),0), mx],-1)
def hsv2rgb(hsv):
    h,s,v=hsv[...,0]/60.0,hsv[...,1],hsv[...,2]; i=np.floor(h)%6; f=h-np.floor(h)
    p=v*(1-s); q=v*(1-s*f); t=v*(1-s*(1-f))
    conds=[i==0,i==1,i==2,i==3,i==4,i==5]
    r=np.select(conds,[v,q,p,p,t,v]); g=np.select(conds,[t,v,v,q,p,p]); b=np.select(conds,[p,p,t,v,v,q])
    return np.stack([r,g,b],-1)
def dominant(hsv, alpha):
    m=(alpha>0.5)&(hsv[...,1]>0.10)&(hsv[...,2]>0.35)
    if m.sum()<200: return None
    hist,_=np.histogram(hsv[...,0][m],bins=72,range=(0,360),weights=hsv[...,1][m])
    k=np.convolve(np.r_[hist[-2:],hist,hist[:2]],np.ones(5),'valid'); return (np.argmax(k)+0.5)*5
def hdist(a,b): d=np.abs(a-b)%360; return np.minimum(d,360-d)
# id, vi, en, tier, trade, base, target hue or None, sat, val, tint(low-sat only) or None, glow
V=[
 ("tho-dau-do","Thỏ Đậu Đỏ","Red Bean Rabbit",0,"Baker","tho-bong",None,1,1,(.90,.62,.60),None),
 ("tho-ca-phe","Thỏ Cà Phê","Coffee Rabbit",0,"Brewer","tho-bong",None,1,1,(.74,.57,.45),None),
 ("tho-tro","Thỏ Tro","Ash Rabbit",0,"Gatherer","tho-me",None,1,.66,None,None),
 ("tho-cat","Thỏ Cát","Sand Rabbit",0,"Stargazer","tho-me",None,1,1.05,(1.0,.90,.70),None),
 ("tho-bap","Thỏ Bắp","Corn Rabbit",0,"Baker","tho-bong",None,1,1,(1.0,.92,.58),None),
 ("tho-khoi","Thỏ Khói","Smoke Rabbit",0,"Stargazer","tho-bong",None,1,1,(.78,.83,.93),None),
 ("tho-hong-phan","Thỏ Hồng Phấn","Blush Rabbit",0,"Brewer","tho-bong",None,1,1,(1.0,.84,.88),None),
 ("tho-ca-cao","Thỏ Ca Cao","Cocoa Rabbit",0,"Baker","tho-me",None,1,1,(.70,.50,.40),None),
 ("tho-cup-kem","Thỏ Cụp Kem","Cream Lop",1,"Gatherer","tho-cup",None,.30,1.18,None,None),
 ("tho-cup-socola","Thỏ Cụp Sô Cô La","Chocolate Lop",1,"Baker","tho-cup",None,1.0,.58,None,None),
 ("tho-bac-ha","Thỏ Bạc Hà","Mint Rabbit",1,"Brewer","tho-bong",None,1,1,(.72,.95,.86),None),
 ("tho-chanh","Thỏ Chanh","Lemon Rabbit",1,"Brewer","tho-matcha",56,1.9,1.05,None,None),
 ("tho-cup-suong","Thỏ Cụp Sương","Misty Lop",1,"Stargazer","tho-cup",222,.38,1.14,None,None),
 ("tho-la-phong","Thỏ Lá Phong","Maple Rabbit",1,"Gatherer","tho-matcha",22,2.3,1.0,None,None),
 ("tho-dao","Thỏ Đào","Peach Lop",1,"Baker","tho-cup",14,.62,1.16,None,None),
 ("tho-tuyet-hong","Thỏ Tuyết Hồng","Rose Snow Rabbit",2,"Brewer","tho-suong",None,1,1,(1.0,.84,.90),None),
 ("tho-bao","Thỏ Bão","Storm Rabbit",2,"Stargazer","tho-suong",None,1,1,(.60,.66,.82),None),
 ("tho-tra-sua","Thỏ Trà Sữa","Milk Tea Rabbit",2,"Brewer","tho-o-long",None,.55,1.14,None,None),
 ("tho-muc","Thỏ Mực","Ink Rabbit",2,"Gatherer","tho-o-long",None,.10,.52,None,None),
 ("tho-cam","Thỏ Cam","Tangerine Rabbit",2,"Baker","tho-o-long",26,1.7,1.12,None,None),
 ("tho-bang","Thỏ Băng","Ice Rabbit",2,"Stargazer","tho-suong",None,1,1,(.72,.93,1.0),None),
 ("tho-anh-dao","Thỏ Anh Đào","Cherry Blossom Rabbit",3,"Gatherer","tho-oai-huong",336,1.3,1.0,None,None),
 ("tho-luu-ly","Thỏ Lưu Ly","Forget-me-not Rabbit",3,"Gatherer","tho-oai-huong",214,1.45,1.0,None,None),
 # Việt Quất và Dâu Tây: xoay toàn ảnh làm lông kem đổi màu theo (#14); hai con này giờ xuất bằng sua-14-viet-quat-dau-tay.py
 # (chỉ xoay vùng tím của tai và lưng, giữ lông kem và má). Hai dòng dưới giữ cho sheet30 đủ 30 con.
 ("tho-viet-quat","Thỏ Việt Quất","Blueberry Lop",3,"Baker","tho-khoai-mon",-38,1.25,.88,None,None),
 ("tho-dau-tay","Thỏ Dâu Tây","Strawberry Lop",3,"Baker","tho-khoai-mon",44,1.2,1.0,None,None),
 ("tho-hoa-nhai","Thỏ Hoa Nhài","Jasmine Rabbit",3,"Brewer","tho-oai-huong",50,.45,1.08,None,None),
 ("tho-ho-phach","Thỏ Hổ Phách","Amber Rabbit",4,"Baker","tho-mat-ong",24,1.25,.92,None,(255,190,90)),
 ("tho-ngan-ha","Thỏ Ngân Hà","Galaxy Rabbit",4,"Stargazer","tho-sao",278,1.15,1.12,None,(200,160,255)),
 ("tho-bach-kim","Thỏ Bạch Kim","Platinum Rabbit",4,"Brewer","tho-mat-ong",215,.10,1.14,None,(220,235,255)),
 ("tho-nguyet-thuc","Thỏ Nguyệt Thực","Eclipse Rabbit",5,"Stargazer","tho-trang",None,1,1,(.52,.24,.34),(255,140,90)),
]
GLOBAL={'tho-khoai-mon'}
def make(base,target,sat,val,tint):
    im=Image.open(os.path.join(SRC,f'kind_{base}.png')).convert('RGBA'); a=np.asarray(im).astype(np.float32)/255
    rgb,alpha=a[...,:3],a[...,3]; hsv=rgb2hsv(rgb); dom=dominant(hsv,alpha)
    out=hsv.copy()
    if base in GLOBAL:
        out[...,0]=(hsv[...,0]+target)%360; out[...,1]=np.clip(hsv[...,1]*sat,0,1); out[...,2]=np.clip(hsv[...,2]*val,0,1)
    elif dom is not None:
        w=np.clip((40-hdist(hsv[...,0],dom))/5,0,1)   # fur family only; blush, ears, marks keep their hue
        if target is not None: out[...,0]=np.where(w>0,(hsv[...,0]+(target-dom)*w)%360,hsv[...,0])
        out[...,1]=np.clip(hsv[...,1]*(1+(sat-1)*w),0,1)
        out[...,2]=np.clip(hsv[...,2]*(1+(val-1)*np.maximum(w,(hsv[...,1]<0.12)*1.0)),0,1)
    else:
        out[...,2]=np.clip(hsv[...,2]*val,0,1)
    res=hsv2rgb(out)
    if tint is not None:
        w=np.clip((0.30-hsv[...,1])/0.10,0,1)[...,None]   # pale fur and dark line only; blush and marks stay
        res=res*(1-w)+np.clip(res*np.array(tint),0,1)*w
    return Image.fromarray((np.dstack([res,alpha])*255).round().astype(np.uint8),'RGBA')
def glow(im,color):
    halo=im.split()[3].filter(ImageFilter.GaussianBlur(9)).point(lambda v:int(v*0.7))
    hl=Image.new('RGBA',im.size,color+(0,)); hl.putalpha(halo); return Image.alpha_composite(hl,im)
meta=[]
for (id_,vi,en,tier,trade,base,tg,s,v,t,g) in V:
    o=make(base,tg,s,v,t)
    if g: o=glow(o,g)
    o.save(os.path.join(OUT,f'kind_{id_}.png')); meta.append(dict(id=id_,vi=vi,en=en,tier=tier,trade=trade,base=base))
json.dump(meta,open(os.path.join(OUT,'kinds30.json'),'w'),ensure_ascii=False,indent=1)
cw,ch=330,360; sheet=Image.new('RGBA',(cw*10,ch*3),(45,105,86,255))
for i,m in enumerate(meta):
    sheet.alpha_composite(Image.open(os.path.join(OUT,f"kind_{m['id']}.png")),((i%10)*cw+5,(i//10)*ch+8))
sheet.save(os.path.join(OUT,'sheet30.png')); sheet.resize((1980,648),Image.LANCZOS).save(os.path.join(OUT,'sheet30-small.png'))
print('ok',len(meta))
