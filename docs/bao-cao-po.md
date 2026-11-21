# Báo cáo gửi PO

Mỗi bước xong được nối thêm một đoạn vào cuối file này. Việc cuối cùng của mỗi bước là chạy toàn bộ test EditMode; sau đó cây làm việc để yên.

## R5 — Khoảnh khắc gọi thỏ (cổng trăng), kèm ba sửa nhỏ của R4

- Test: 266/266 EditMode qua (chạy sau lần sửa file cuối cùng trong Assets).
- Thông điệp commit đề xuất: `A called rabbit steps out of the moon gate; new kinds are marked in the album`
  - Thân: resting rabbits sway and doze; the swing's rabbit sits on its seat in front of the frame.
  - Kết thúc: `Co-Authored-By: Claude Fable 5.2 <noreply@anthropic.com>`
- Ảnh (docs/anh/po-r5/): `18a-gate.png` (cổng và ánh sáng), `18b-shadow.png` (bóng thỏ bước ra), `18c-arrived.png` (lộ hình, tên, "Mới!"), `8c-album-top.png`, `8d-album-bottom.png` (Sổ thỏ), `17-rest-bottom.png` (góc nghỉ sau khi sửa xích đu, có "z z").

### Đã làm
- `SummonMomentView`: bấm Gọi hoặc Ghép thì cổng trăng (`prop_cong-trang.png`, đã chép vào Art/Final) bật lên giữa màn hình trên nền tối; ánh sáng trong cổng và quầng sau cổng loé màu bậc; bóng đen của con thỏ bước ra và lớn dần; bóng tan thành hình loài, hiện tên, tấm "loài · bậc". Dài 2,3 giây. Chạm lần một nhảy tới lúc lộ hình, chạm lần hai đóng. Thay cho băng thông báo cũ.
- Loài mới (`Rabbit.FirstOfKind`): nhãn "Mới!" ở góc; trong Sổ thỏ ô của loài đó viền vàng và có thẻ "Mới" cho tới khi mở trang loài.
- Xích đu: thỏ vẽ phía trước xích đu, chân đặt trên mặt đệm, thu còn 82%.
- Thỏ ở chỗ nghỉ: lắc lư chậm, thỉnh thoảng hiện bong bóng "z z" (khoảng 30% thời gian, mỗi con lệch pha).
- Sổ thỏ, ô loài chưa gặp: vốn đã là bóng đen của CHÍNH loài đó (tô đen hình loài, không phải một bóng chung). 42 loài cũ trông giống nhau vì chúng chung một dáng; 30 loài dáng mới thì thấy rõ dáng riêng (xem `8d-album-bottom.png`). Không phải sửa gì.

### Test mới
- Gọi thì màn mở, bắt đầu là bóng, đúng hình loài; chạy hết thì lộ hình và có "Mới!"; chạm sau khi lộ thì đóng; chạm giữa chừng thì nhảy tới cuối, chạm nữa mới đóng; ô trong sổ có thẻ "Mới" và mất khi mở trang loài.
- Ghép ra loài đã gặp thì không có "Mới!".

### Chưa đạt
- Mới soi ba khung hình tĩnh của màn cổng trăng; chưa xem chuyển động trong bản build. Bản Mac ở Build/Mac vẫn là bản R2.
- Bậc Bạc màu xám nhạt nên ánh sáng trong cổng gần như không đổi màu.
- Không có âm thanh riêng theo bậc (dùng tiếng "sting" sẵn có).
- Dấu "Mới" trong sổ chỉ nhớ trong phiên chơi, không lưu vào save.
- Thỏ đi dạo vẫn hiện ra và biến mất tức thì; thỏ ngồi không có mũ hay tên.

### Hình cần vẽ
- Chưa cần thêm cho R5. (Xích đu hai lớp không còn cần.)

### Việc kế tiếp
- R6: bảng số cấp vườn và mô phỏng sẽ được ghi vào file này trước khi code giao diện.

## R5 bổ sung — hiệu ứng bậc Bạc, bản Mac mới

- Test: 266/266 EditMode qua, chạy SAU lần build và sau lần sửa file cuối trong Assets.
- Thông điệp commit đề xuất: `Silver arrivals get a white light and a shower of glints`
  - Kết thúc: `Co-Authored-By: Claude Fable 5.2 <noreply@anthropic.com>`
- Ảnh (docs/anh/po-r5/, đã chụp lại): `18a-gate.png` (bậc Bạc: ánh trắng và hạt lấp lánh), `18b-shadow.png`, `18c-arrived.png`.
- Bản Mac: `Build/Mac/VuonTraTho.app`, build thành công, đang mở, Player.log không có exception. Gồm mọi thứ tới R5.

### Đã làm
- Cổng trăng: mọi lần gọi có 4 hạt lấp lánh quanh cổng theo màu bậc; riêng bậc Bạc ánh sáng trong cổng là trắng, quầng sáng mạnh hơn và có đủ 10 hạt, nhấp nháy to hơn.

### Lưu ý cho lần commit
- Build làm đổi `Assets/AddressableAssetsData/OSX/addressables_content_state.bin` và xoá `link.xml` cùng `.meta` của nó. Ba mục này do Unity sinh ra khi build, không phải thay đổi của bước này; đừng đưa việc xoá `link.xml` vào commit.

### Chưa đạt
- Bản Mac chưa ai thử tay. Chuyển động cổng trăng mới được soi qua khung hình tĩnh.

## R6 bước 1 — Cấp vườn và đợt mở đất: bảng số và mô phỏng (luật trong Core, chưa có giao diện)

- Test: 294/294 EditMode qua (chạy sau lần sửa file cuối cùng trong Assets).
- Thông điệp commit đề xuất: `Garden level and expansion waves in the rules, with a pacing simulation (save v8)`
  - Thân: experience from station levels, milestones, rabbits, new kinds and quests; a clover for every level; waves of two plots gated by level, rabbits and carrots; old saves keep their land and are given the level it needs. Not yet wired into the screens.
  - Kết thúc: `Co-Authored-By: Claude Fable 5.2 <noreply@anthropic.com>`
- Ảnh: chưa có (bước này chỉ có luật). Số liệu mô phỏng: `Logs/garden-pacing.txt` và `Logs/garden-pacing-tireless.txt` (sinh lại mỗi lần chạy test).

### Bảng số đề xuất (đang nằm trong `Core/Garden.cs`)

Nguồn điểm kinh nghiệm:

| Nguồn | Điểm |
|---|---|
| Nâng trạm một cấp | 1 |
| Mở một trạm mới | 10 |
| Vượt một mốc trạm (10, 25, 50, 100, …) | 15 |
| Gọi một con thỏ | 5 |
| Gặp một loài mới (gọi hoặc ghép) | 20 |
| Nhận thưởng một nhiệm vụ | 10 |

Đường cong: từ cấp L lên L+1 cần `180 + 5 × L` điểm. Tổng điểm để tới cấp 3 là 375, cấp 5 là 770, cấp 6 là 975, cấp 10 là 1.845, cấp 15 là 3.045. Trần cấp 99.

Quà lên cấp: mỗi cấp vườn tặng 1 cỏ bốn lá (để vườn lớn lên thì luôn gọi thêm được thỏ).

Đợt mở đất (bắt đầu 4 ô, 2 chỗ nghỉ):

| Đợt | Ô đất sau đợt | Cấp vườn | Thỏ đang có | Loài đã gặp | Cà rốt | Chỗ nghỉ sau đợt |
|---|---|---|---|---|---|---|
| 1 | 6 | 3 | 3 | – | 0,65M | 3 |
| 2 | 8 | 6 | 6 | – | 120M | 4 |
| 3 | 10 | 10 | 10 | – | 24,1B | 5 |
| 4 | 12 | 15 | – | 8 | 5,33T | 6 |

Cà rốt = tổng giá hai ô theo luật Plots hiện có (mỗi ô bằng 25% giá mở trạm đứng trên nó). Khung cấp/thỏ/loài giữ đúng đề xuất của PO.

Save cũ (version 8 thêm `gardenXp`): không lấy lại ô đất nào; điểm được tính bù từ tiến độ sẵn có (cấp trạm đã mua, trạm đã mở, mốc đã vượt, thỏ, loài, nhiệm vụ) và không bao giờ thấp hơn cấp của đợt đất đang có (5–6 ô → cấp 3, 7–8 → cấp 6, 9–10 → cấp 10, 11–12 → cấp 15). Phần tính bù không tặng cỏ.

### Kết quả mô phỏng (test `GardenPacingTests`, chạy trên dữ liệu thật của game)

Người chơi thong thả: 1 chạm/giây, 3 giây mua một lần thứ rẻ nhất, 30 giây xem nhiệm vụ một lần, gọi thỏ ngay khi có cỏ, không ghép. Ngày 1 chơi 20 phút rồi 4 lần ghé 5 phút cách nhau 2 giờ.

| Mốc | Lúc đạt |
|---|---|
| Cấp vườn 3 | 4 phút 0 giây |
| Cấp vườn 5 | 10 phút 3 giây |
| Cấp vườn 6 | 15 phút 30 giây |
| Cấp vườn 10 | 8 giờ 39 phút (cuối ngày 1) |
| Cấp vườn 15 | 45 giờ (ngày thứ hai) |

| Đợt | Đủ cấp | Đủ thỏ | Mở |
|---|---|---|---|
| 1 | 4 phút 0 giây | 1 phút 30 giây | 4 phút 3 giây |
| 2 | 15 phút 30 giây | 4 phút 0 giây | 15 phút 33 giây |
| 3 | 8 giờ 39 phút | 8 phút 9 giây | 8 giờ 39 phút |
| 4 | 45 giờ 13 phút | 5 phút 0 giây (8 loài) | 45 giờ 13 phút |

Cuối ngày 1: cấp 10, 30 thỏ, 18 loài đã gặp, 10 ô đất. Cuối ngày 7: cấp 21, 59 thỏ, 27 loài, 12 ô đất.

Kết luận: đạt cả hai mốc PO nêu (cấp 5 quanh phút thứ 10, cấp 10 trong ngày đầu), và ở mọi đợt số thỏ đã đủ từ trước khi đủ cấp, tức không đợt nào kẹt vì thiếu thỏ. Cà rốt cũng không chặn: mỗi đợt được mở trong vài giây sau khi đủ cấp. Test khẳng định ba điều này; số sai lệch là test đỏ.

### Điều PO cần biết (không phải lỗi của R6, nhưng mô phỏng làm lộ ra)

- **Nền kinh tế đang rất nhanh.** Một người chơi không nghỉ tay (3 chạm/giây, 4 lần mua mỗi giây) mở đủ 12 ô đất trong chưa đầy 7 phút và tới cấp vườn 22 sau 20 phút. Ngay cả người chơi thong thả cũng có 6 trạm và 22 thỏ sau 20 phút, 10/12 ô đất cuối ngày 1. Nguyên nhân chính là phần thưởng nhiệm vụ tính bằng "N giây thu nhập" (300 tới 3.600 giây mỗi nhiệm vụ) cộng với hệ số của thỏ. Đường cong cấp vườn ở trên được chỉnh cho người chơi thong thả; người chơi nhanh sẽ vượt xa.
- Vì điểm kinh nghiệm dồn về mấy phút đầu (cấp trạm rẻ), mình phải dùng đường cong gần như phẳng (mỗi cấp 185–255 điểm) mới đạt cùng lúc "cấp 5 ở phút 10" và "cấp 10 cuối ngày 1". Đường cong dốc kiểu `25 × L` cho cấp 5 sau 23 giây.
- Số thỏ có được nhanh hơn nhiều so với ngưỡng (14 thỏ ở phút 10 so với ngưỡng 3 và 6). Ngưỡng thỏ hiện gần như không chặn ai; muốn nó có nghĩa thì nâng ngưỡng hoặc giảm cỏ từ nhiệm vụ, PO quyết.
- Quà 1 cỏ mỗi cấp vườn là mình thêm để bảo đảm "không bao giờ kẹt"; với số liệu trên nó không còn cần thiết. Bỏ hay giữ, PO quyết (một hằng số `Garden.CloversPerLevel`).

### Chưa làm trong bước này
- Chưa có giao diện: huy hiệu cấp trên HUD, khoảnh khắc lên cấp, thẻ "đợt mở rộng kế tiếp" với ba điều kiện, sương và rừng che đất chưa mở.
- Nút mua đất trong game vẫn chạy theo luật cũ (mua từng ô bằng cà rốt), và chỗ nghỉ vẫn mua bằng cà rốt trong tab Thỏ. Bước 2 của R6 sẽ chuyển cả hai sang đợt mở đất.
- Điểm kinh nghiệm đã được cộng thật trong game từ bây giờ (và quà cỏ khi lên cấp), dù người chơi chưa thấy con số.

### Hình cần vẽ cho bước 2 của R6
- Huy hiệu cấp vườn (một hình tròn hoặc lá có chỗ cho con số); nếu không có mình vẽ bằng code.
- Sương hoặc mây che vùng đất chưa mở (một mảng mềm, lặp lại được); nếu không có mình dùng lớp sương vẽ bằng code và cây sẵn có.
