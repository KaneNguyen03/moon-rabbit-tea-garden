import sys, numpy as np
from PIL import Image
sys.path.insert(0, '/tmp/fix14')
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
def hdist(a,b): d=np.abs(a-b)%360; return np.minimum(d,360-d)
src, out = sys.argv[1], sys.argv[2]
im=Image.open(src+'/kind_tho-khoai-mon.png').convert('RGBA'); a=np.asarray(im).astype(np.float32)/255
rgb,alpha=a[...,:3],a[...,3]; hsv=rgb2hsv(rgb)
# The taro rabbit's coloured parts are its violet ears and back; the body is cream and the cheeks pink. Only the violet family turns.
violet=260.0
w=np.clip((45-hdist(hsv[...,0],violet))/10,0,1)*np.clip((hsv[...,1]-0.08)/0.10,0,1)
def variant(name, target, sat, val):
    o=hsv.copy()
    spread=((hsv[...,0]-violet+180)%360)-180  # how far each pixel sits from the violet core
    o[...,0]=np.where(w>0,(hsv[...,0]*(1-w)+(target+spread*0.35)*w)%360,hsv[...,0])
    o[...,1]=np.clip(hsv[...,1]*(1+(sat-1)*w),0,1)
    o[...,2]=np.clip(hsv[...,2]*(1+(val-1)*w),0,1)
    res=hsv2rgb(o)
    Image.fromarray((np.dstack([res,alpha])*255).round().astype(np.uint8),'RGBA').save(out+'/kind_'+name+'.png')
variant('tho-viet-quat', 228, 2.0, 0.78)   # blueberry: deep blue-violet, darker
variant('tho-dau-tay', 348, 2.4, 1.0)      # strawberry: pink-red
print('ok')
