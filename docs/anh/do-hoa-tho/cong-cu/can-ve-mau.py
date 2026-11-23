# Cắt tấm 4 ô (nền #2E5E49) và căn từng thỏ về đúng khung thỏ mẫu 320x343 theo vị trí hai mắt; in độ trùng dáng thân.
# usage: python3 -I can-ve-mau.py sheet.png outdir id1,id2,id3,id4
import sys, os
import numpy as np
from PIL import Image
from scipy import ndimage as ndi
R='/Users/hiquyennek/Documents/VuonTraNam/'
sheet,out,ids=sys.argv[1],sys.argv[2],sys.argv[3].split(',')
os.makedirs(out,exist_ok=True)
def eyes(rgba):
    a=np.asarray(rgba).astype(float); al=a[...,3]>128; H,W=al.shape
    lum=a[...,:3]@[0.3,0.59,0.11]; inner=ndi.binary_erosion(al,iterations=5)
    lab,n=ndi.label((lum<70)&inner); c=[]
    for i,sl in enumerate(ndi.find_objects(lab),1):
        m=lab[sl]==i; ar=m.sum(); h=sl[0].stop-sl[0].start; w=sl[1].stop-sl[1].start
        if 20<=ar<=1500 and 0.5<h/max(w,1)<3.2 and m.mean()>0.45:
            cy,cx=ndi.center_of_mass(lab==i); c.append((cy,cx,ar))
    best=None
    for i in range(len(c)):
        for j in range(i+1,len(c)):
            (y1,x1,a1),(y2,x2,a2)=c[i],c[j]; d=abs(x1-x2)
            if abs(y1-y2)<0.25*d and 0.4<a1/a2<2.5 and d>15 and min(y1,y2)<H*0.6:
                s=abs(y1-y2)/d+abs(np.log(a1/a2))
                if best is None or s<best[0]: best=(s,sorted([(x1,y1),(x2,y2)]))
    return best and best[1]
master=Image.open(R+'Assets/VuonTra/Art/Final/kind_tho-bong.png').convert('RGBA')
ME=eyes(master); print('master eyes',ME)
mm=np.asarray(master)[...,3]>128
im=Image.open(sheet).convert('RGB'); a=np.asarray(im).astype(int); H,W,_=a.shape
bg=np.median(np.concatenate([a[:6].reshape(-1,3),a[-6:].reshape(-1,3)]),axis=0)
dist=np.sqrt(((a-bg)**2).sum(-1)); fg=dist>30
fg=ndi.binary_opening(fg,iterations=2); fg=ndi.binary_fill_holes(fg)
soft=ndi.gaussian_filter(fg.astype(float),1.0)
rgba=np.dstack([np.asarray(im),(soft*255).astype('uint8')])
n=len(ids); cw=W/n
for k,kid in enumerate(ids):
    x0,x1=int(k*cw),int((k+1)*cw)
    cell=Image.fromarray(rgba[:,x0:x1].copy(),'RGBA')
    lab,nn=ndi.label(fg[:,x0:x1]); sizes=ndi.sum(np.ones_like(lab),lab,range(1,nn+1))
    keep=np.isin(lab,[i+1 for i,s in enumerate(sizes) if s>0.02*sizes.max()])
    c=np.asarray(cell).copy(); c[...,3]=(c[...,3]*keep).astype('uint8'); cell=Image.fromarray(c,'RGBA')
    E=eyes(cell)
    if not E: print(kid,'KHONG THAY MAT'); cell.save(os.path.join(out,'_loi_'+kid+'.png')); continue
    (ax,ay),(bx,by)=E; (mx1,my1),(mx2,my2)=ME
    s=(mx2-mx1)/(bx-ax); tx=mx1-ax*s; ty=my1-ay*s
    # chân: đặt đáy thân trùng đáy thỏ mẫu thay vì theo mắt nếu lệch quá
    big=cell.resize((max(1,int(cell.width*s)),max(1,int(cell.height*s))),Image.LANCZOS)
    canvas=Image.new('RGBA',(320,343),(0,0,0,0)); canvas.alpha_composite(big,(int(round(tx)),int(round(ty))))
    m=np.asarray(canvas)[...,3]>128
    iou=(m[95:]&mm[95:]).sum()/(m[95:]|mm[95:]).sum()
    canvas.save(os.path.join(out,'kind_%s.png'%kid)); print(kid,'scale %.3f'%s,'IoU than %.3f'%iou)
