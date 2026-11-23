# Thỏ mẫu tách lớp (khung pivot) — nguyên mẫu ngoài Unity.
# Khuôn cắt định nghĩa MỘT lần trên thỏ mẫu (Thỏ Bông 320x343); mọi loài cùng dáng dùng chung khuôn.
import math, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage as ndi
sys.path.insert(0,'/tmp/dht')
from ghep import R
W,H=320,343
HEAD=dict(cx=148,cy=142,rx=76,ry=60)     # elip đầu
NECK=(148,196); FEET=(160,343)
EARS={'L':(138,96),'R':(196,104)}        # gốc tai (pivot)
SHOULDER=(186,214)                        # vai tay gần (phía mặt)
LOP={'tho-cup','tho-cup-kem','tho-cup-socola','tho-cup-suong','tho-dao','tho-khoai-mon','tho-viet-quat','tho-dau-tay'}
def load(kid): return np.asarray(Image.open(R+'Assets/VuonTra/Art/Final/kind_%s.png'%kid).convert('RGBA')).astype(float)
def soft(m,r=1.2): return ndi.gaussian_filter(m.astype(float),r)
def parts(kid):
    a=load(kid); al=a[...,3]/255
    yy,xx=np.mgrid[0:H,0:W]
    inhead=((xx-HEAD['cx'])/HEAD['rx'])**2+((yy-HEAD['cy'])/HEAD['ry'])**2<=1
    if kid in LOP:
        headm=(yy<200)|((yy<232)&((xx<104)|(xx>204)))
        earm=np.zeros_like(headm)
    else:
        incore=((xx-HEAD['cx'])/(HEAD['rx']-7))**2+((yy-HEAD['cy'])/(HEAD['ry']-7))**2<=1
        earm=(~incore)&(yy<120)
        headm=inhead|((yy<200)&~earm&(yy>60))
        headm&=(yy<202)
    earm=soft(earm); headm=soft(headm)
    def layer(m): o=a.copy(); o[...,3]=a[...,3]*m; return o
    ears=layer(earm); head=layer(np.clip(headm-earm*(1-soft(inhead)),0,1))
    # thân: phủ vùng đầu bằng màu lông (không nét) để khi đầu nghiêng không lộ viền cũ
    body=a.copy()
    furpix=a[(yy>215)&(yy<300)&(al>0.95)][:,:3]; lum=furpix@[0.3,0.59,0.11]
    fur=np.median(furpix[lum>np.percentile(lum,40)],axis=0)
    shrink=((xx-HEAD['cx'])/(HEAD['rx']-10))**2+((yy-HEAD['cy'])/(HEAD['ry']-10))**2<=1
    fill=soft(shrink,2.5)*(yy>NECK[1]-70)
    body[...,:3]=body[...,:3]*(1-fill[...,None])+fur*fill[...,None]
    body[...,3]=np.maximum(body[...,3]*(1-np.clip(earm+ (yy<100)*1.0,0,1)), 0)
    return {k:Image.fromarray(np.clip(v,0,255).astype('uint8'),'RGBA') for k,v in dict(ears=ears,head=head,body=body).items()}, tuple(int(c) for c in fur)
def place(canvas, img, pivot_src, pivot_dst, angle=0, sx=1, sy=1):
    """dán img (toạ độ 320x343) sao cho pivot_src tới pivot_dst sau khi co giãn + xoay quanh pivot"""
    w,h=img.size; im=img.resize((max(1,int(w*sx)),max(1,int(h*sy))),Image.LANCZOS)
    px,py=pivot_src[0]*sx,pivot_src[1]*sy
    pad=int(math.hypot(*im.size))+4
    big=Image.new('RGBA',(pad*2,pad*2),(0,0,0,0)); big.alpha_composite(im,(int(pad-px),int(pad-py)))
    big=big.rotate(angle,resample=Image.BICUBIC,center=(pad,pad))
    canvas.alpha_composite(big,(int(pivot_dst[0]-pad),int(pivot_dst[1]-pad)))
def xf(p, pivot, angle, sx=1, sy=1, dst=None):
    x=(p[0]-pivot[0])*sx; y=(p[1]-pivot[1])*sy; a=math.radians(-angle)
    X=x*math.cos(a)-y*math.sin(a); Y=x*math.sin(a)+y*math.cos(a)
    d=dst or pivot; return (d[0]+X, d[1]+Y)
