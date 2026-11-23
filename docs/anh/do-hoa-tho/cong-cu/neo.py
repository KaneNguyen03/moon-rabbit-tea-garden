# Neo đầu từng loài, đo tay trên hình 320x343 (toạ độ ảnh, y từ trên xuống): giữa đầu x, đỉnh sọ y, bề ngang đầu.
BASE=(150,86,140)
MEAS={
'tho-lun':(168,98,130),'tho-con':(152,72,125),'tho-va':(175,64,130),'tho-dong':(190,95,105),
'tho-long-xoan':(168,78,140),'tho-banh-day':(155,68,150),'tho-dom':(172,95,120),'tho-kem-chay':(185,98,115),
'tho-tai-dai':(168,100,130),'tho-angora':(160,86,130),'tho-latte':(165,76,135),'tho-dau':(170,72,140),
'tho-flan':(170,68,140),'tho-su-tu':(172,72,150),'tho-hoa-dao':(168,100,135),'tho-sim':(165,92,140),
'tho-may':(172,88,145),'tho-reu':(183,82,125),'tho-gac':(180,95,130),'tho-den-long':(200,78,135),
'tho-tien':(170,98,140),'tho-dom-dom':(180,84,145),'tho-pha-le':(168,112,135),'tho-ba-duoi':(205,98,140),
'tho-hang-nga':(180,102,140),'tho-mat-troi':(180,105,135),'tho-cuc-quang':(180,95,150),'tho-ngoc':(172,95,140),
'tho-rong':(155,85,140),'tho-chom-sao':(172,85,150),'tho-den-ong-sao':(160,80,145),'tho-ao-dai':(170,80,145),
'tho-noel':(160,75,140),'tho-trang-non':(135,88,140),
}
# Đồ có sẵn trên đầu / ở cổ: không cho đội mũ / đeo bùa.
NOHAT={'tho-gac':'gạc','tho-mat-troi':'lửa','tho-rong':'sừng','tho-noel':'mũ Noel','tho-hang-nga':'trăng cài trán'}
NOCHARM={'tho-noel':'khăn quàng','tho-ao-dai':'áo dài','tho-hang-nga':'mặt trăng đeo ngực'}
def anchor(kid): return MEAS.get(kid,BASE)
# Mũ: rộng (× bề ngang đầu), lún (đáy mũ dưới đỉnh sọ, × bề ngang đầu), lệch ngang (× bề ngang đầu), tai đè lên mũ?
SEAT={
 'non-la':    dict(w=1.50,sink=0.16,dx=0.00,ears=False, img='hat_non-la_worn'),
 'mu-len':    dict(w=1.06,sink=0.30,dx=0.00,ears=True),
 'vuong-mien':dict(w=0.66,sink=0.10,dx=0.00,ears=True),
 'vong-hoa':  dict(w=1.08,sink=0.16,dx=0.00,ears=True),
 'no-trang':  dict(w=0.62,sink=0.12,dx=0.26,ears=False),
 'mu-sao':    dict(w=1.30,sink=0.18,dx=0.02,ears=False),
}
