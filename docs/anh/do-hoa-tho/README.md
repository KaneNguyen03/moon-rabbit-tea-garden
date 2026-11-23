# Đồ hoạ thỏ: mặc đồ và hoạt ảnh làm việc (23/11/2026)

Chủ project chê hai điểm: "con thỏ mặc đồ khi ghép ảnh nhìn kì lắm" và "chưa có animation cho các con thỏ làm việc gì", sau đó chốt hướng **một thỏ mẫu gốc tách lớp** (kiểu Cats & Soup) cho cả 76 loài.

## Vì sao trước đây mặc đồ trông kì

`mu/truoc-sau-6-loai.png`: bên trái là cách ghép cũ, bên phải là cách mới.

1. Mũ có một cỡ và một chỗ cố định cho cả 76 hình, trong khi đầu mỗi loài to nhỏ, cao thấp khác nhau.
2. Nón lá có dây quai rủ xuống che mắt. Nón lá và mũ phù thuỷ đè mất tai.
3. Hình mũ vẽ góc nhìn từ dưới lên, còn thỏ vẽ nhìn ngang. Mũ đứng thẳng trên cả thỏ quay 3/4.
4. Nhuộm lông bằng cách nhân màu cả hình làm lông xỉn thành màu bùn (xanh lên thỏ nâu ra rêu tối).
5. Loài đã có đồ sẵn (Noel có mũ và khăn, Gạc có gạc...) vẫn bị đội thêm mũ.

## Đã sửa trong engine (nhánh `art-rabbits`, worktree `../VuonTraNam-mu`)

- **Không cho mặc lên đồ có sẵn**: `RabbitKind.NoHat` / `NoCharm`; `Items.CanWear`; `Items.Equip` từ chối; save cũ và thỏ gộp ra loài đó tự cất đồ vào túi; ô mũ/bùa trên thẻ thỏ ghi "có sẵn" và không nhận chạm.
  - Không đội mũ: Gạc (gạc), Mặt Trời (lửa), Rồng (sừng), Noel (mũ Noel), Hằng Nga (trăng cài trán).
  - Không đeo bùa: Noel (khăn), Áo Dài (cổ áo), Hằng Nga (mặt trăng đeo ngực).
- **Mũ ngồi trên đầu từng loài**: neo đầu đo tay trên từng hình (`mu/neo-dau-76-loai.csv` → `HeadX/HeadTop/HeadWidth`). Mỗi mũ có `HatSeat`: rộng theo bề ngang đầu, độ lún, độ lệch, độ nghiêng theo hướng mặt, và cờ tai xuyên qua mũ. Có bóng mềm dưới vành. Nón lá khi đội dùng `hat_non-la_worn.png` (bỏ dây quai). Tai dựng sau mũ; riêng mũ len thì tai xuyên qua mũ.
- **Nhuộm giữ nét**: shader `VuonTra/UI Dye Fur` lấy sắc của thuốc nhuộm, giữ độ sáng của lông; nét mực và mắt giữ nguyên.
- **Thỏ mẫu tách lớp (khung pivot)**: Thỏ Bông là thỏ mẫu. 42 loài trùng dáng (34 tai dựng, 8 tai cụp) được seed tự cắt thành `kind_<id>_body/_head/_ears.png` trên cùng khung 320×343 (`RabbitParts`, `RabbitRig`). Ở trạm, cây lớp là: Thân (nhún từ chân) → HeadPivot (gật quanh cổ: tai, đầu, bóng mũ, mũ, tai xuyên mũ) → Arm (xoay quanh vai: dụng cụ nghề, bàn tay theo màu lông) → hạt theo nghề.
- **Hoạt ảnh bốn nghề** (`RabbitRig.Work`, một vòng mỗi giây, gấp đôi khi tăng tốc, đứng yên khi giảm chuyển động):
  - Pha trà: thìa cắm xuống nồi, khuấy vòng tròn, có hơi nước bốc lên.
  - Bếp bánh: nhún xuống mỗi nhịp, cây cán ấn ngang, có bụi bột.
  - Hái lá: nhón chân với tay lên bên phải, có lá rơi.
  - Ngắm sao: ngả ra sau, ống nhòm áp mắt, có sao lấp lánh.
  - Đầu gật trễ nhịp so với thân, tai trễ hơn nữa. Xem `hoat-anh/lam-viec-4-nghe.gif` (bản ghép ngoài Unity, cùng các con số trong engine).

## Còn lại

- **34 loài dáng riêng** đang được vẽ lại theo đúng thỏ mẫu bằng Antigravity: vẽ theo tấm 4 con, rồi căn về khung bằng vị trí mắt (`can-ve-mau.py`). Khi có hình, đổi `Shape` của loài sang `Master` là seed tự cắt lớp. Trong lúc chờ, các loài này vẫn vẽ nguyên hình và mũ đặt theo neo đầu.
- **Cánh tay vẽ tay** cho bốn nghề (lông trắng, tô theo màu lông) sẽ thay bàn tay tròn tạm. Máy vẽ ảnh báo hết chỗ từ 22:50 ngày 23/11, đang có vòng tự thử lại.
- Áo, phụ kiện mặt, đồ sau lưng: thêm các khe lớp theo cùng khung khi có hình.

## Công cụ (`cong-cu/`)

Các script Python dùng để ghép thử ngoài Unity. Chúng đọc hình trong `Assets/VuonTra/Art/Final`; vài script trỏ tới `/tmp/dht` và cần sửa đường dẫn khi chạy lại.
- `ghep.py`: ghép mũ đúng như code cũ (tấm "trước").
- `neo.py`: neo đầu đo tay và bảng `SEAT`.
- `sau2.py`: ghép kiểu mới (tấm "sau").
- `rig.py`, `dien.py`: cắt lớp thỏ mẫu và dựng GIF bốn nghề.
- `can-ve-mau.py`: cắt tấm Antigravity và căn từng thỏ về khung thỏ mẫu theo vị trí hai mắt, kèm in độ trùng dáng.
