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

## R6 bước 2 — Giao diện cấp vườn, mở đất theo đợt, sương che đất chưa mở

- Test: 290/290 EditMode qua (chạy sau lần sửa file cuối cùng trong Assets). Số test giảm từ 294 vì bỏ các test mua từng ô đất và mua chỗ nghỉ bằng cà rốt, thay bằng test theo đợt.
- Thông điệp commit đề xuất: `The garden widens in waves: level badge, three conditions, mist over land not yet opened`
  - Thân: buying land and rest places now go through Expansion; the HUD shows the garden level in a ring; a level gained is announced; resting places come with each wave.
  - Kết thúc: `Co-Authored-By: Claude Fable 5.2 <noreply@anthropic.com>`
- Ảnh (docs/anh/po-r6/): `2c-top.png` (huy hiệu cấp 5 trên HUD; sương và cụm cây trên đất chưa mở ở đầu vườn; trạm kế tiếp hiện bóng trong sương), `z2-default.png`, `9c-worn.png` (HUD + tấm trạm), `5-stations.png`, `8-rabbits.png` (dòng chỗ nghỉ không còn nút mua), `15-all-far.png`.

### Đã làm
- Mua đất: `Plots.CanBuy/CostOfNext/TryBuy` giờ đi qua `Expansion` (đợt 2 ô, cần đủ cấp vườn, thỏ hoặc loài, và cà rốt). Thẻ "Mở rộng vườn · đợt N" trong tấm Trạm và tấm trạm của ô "Đất trống" ghi ba điều kiện trên một dòng; điều kiện đạt màu xanh, chưa đạt màu nâu đỏ kèm "(đang N)". Nút chỉ sáng khi đủ cả ba.
- Chỗ nghỉ: bỏ mua bằng cà rốt (`RestSpots.TryBuy` và nút trong tab Thỏ đã xoá); mỗi đợt mở đất mở thêm một chỗ. Dòng trong tab Thỏ chỉ còn thông tin.
- HUD: huy hiệu cấp vườn ở bên trái ô cà rốt, vẫn một hàng (ô cà rốt và thanh Trà lực hẹp lại một chút). Vòng quanh con số đầy dần tới cấp kế. Chạm huy hiệu hiện "Cấp vườn N · còn X điểm nữa lên cấp" (R10 sẽ thay bằng màn hồ sơ).
- Lên cấp vườn: băng thông báo "Vườn lên cấp N! Tặng 1 cỏ bốn lá", huy hiệu nảy lên.
- Sương: mỗi ô đất chưa mở có một mảng sương trên mặt đất; ô nào chưa có trạm nào được vẽ thì có thêm hai cây đứng trên chính ô đó. Trạm kế tiếp vẫn hiện bóng xám trong sương. Khi mở rộng, sương của hai ô tan dần trong 1,2 giây.

### Test mới và sửa
- Trạm thứ năm chờ vườn mở rộng: chỉ có cà rốt thì nút tắt và điều kiện chưa đạt được đánh dấu; đủ cấp 3 và 3 thỏ thì mở được, ra 6 ô và 3 chỗ nghỉ.
- Tấm trạm của ô đất trống: nút "Mở rộng", rồi "Mở trạm".
- HUD hiện đúng cấp và vòng tiến độ; sương nằm trên ô chưa mở và tan sau khi mở rộng.
- Mô phỏng `GardenPacingTests` vẫn qua với luật mở đất mới (cấp 5 ở phút 10, cấp 10 cuối ngày 1, không kẹt vì thỏ).

### Chưa đạt
- Chưa có ảnh chụp riêng của thẻ mở rộng với ba điều kiện (chỉ có test); ảnh `5-stations.png` cắt ngay trên thẻ đó.
- "Cây lùi ra" khi mở rộng hiện là cây biến mất tức thì, chỉ sương là tan dần.
- Sương và cây dùng hình sẵn có (quầng sáng và cây trang trí tô tối), chưa có hình sương riêng.
- Save cũ có nhiều chỗ nghỉ hơn số đợt cho phép thì giữ nguyên; save cũ có ít hơn thì KHÔNG được bù chỗ nghỉ theo số ô đất đang có (chỉ bù cấp vườn).
- Chưa build lại bản Mac sau R6.

### Tồn đọng
- tồn đọng: cân lại tốc độ nền kinh tế (thưởng nhiệm vụ tính theo giây thu nhập) — xem đoạn "R6 bước 1".
- tồn đọng: quyết định giữ hay bỏ quà 1 cỏ mỗi cấp vườn.
- tồn đọng: bù chỗ nghỉ cho save cũ theo số ô đất đang có.
- tồn đọng: hình sương riêng và huy hiệu cấp vườn vẽ tay.

## Lỗi #35 — Thỏ phình to khi bấm nhanh

- Test: 293/293 EditMode qua, chạy SAU lần build và sau lần sửa file cuối trong Assets. Trạng thái này cũng là trạng thái hoàn tất phần R6 bước 2 nằm dở trong feb6c8f: toàn bộ test qua trên nó.
- Thông điệp commit đề xuất: `Fast taps no longer swell a station: pops start from a fixed size; double-tap zoom moves to open ground`
  - Thân: completes the half-landed step 2 of R6 from feb6c8f (level badge, expansion conditions, mist).
  - Kết thúc: `Co-Authored-By: Claude Fable 5.2 <noreply@anthropic.com>`
- Bản Mac: `Build/Mac/VuonTraTho.app`, build thành công, đang mở, Player.log không có exception. Gồm R6 và bản sửa này.

### Test đỏ trước khi sửa (chạy trên code chưa sửa, cả bốn đều đỏ)
- `Upgrading_A_Station_Fast_Leaves_It_The_Size_It_Was`: nâng 30 lần cách nhau 0,05 giây, tỉ lệ trạm còn lại 1,099 thay vì 1,0.
- `Tapping_A_Rabbit_Fast_Neither_Zooms_Nor_Swells_It`: ngay lần chạm thứ hai mức zoom nhảy sang Gần.
- `Taps_And_Upgrades_Mixed_Together_Fast_Leave_Everything_As_It_Was`: mức zoom nhảy sang Gần.
- `A_Double_Tap_On_A_Station_Does_Not_Zoom_But_One_On_Open_Ground_Does` (thay test chạm đúp cũ): chạm đúp trạm vẫn zoom.
Sau khi sửa cả bốn xanh.

### Sửa
1. Tỉ lệ gốc cố định: `StationActorView.BaseScale` được đặt một lần lúc dựng (= `StationSize(i)`). Hiệu ứng phồng khi nâng cấp là `Bump()`, được tính mỗi khung hình từ `BaseScale`; lần bấm mới chỉ khởi động lại hiệu ứng đang chạy. `GardenScreen.OnBuy` không còn đọc `localScale` hiện tại.
2. Chạm đúp vào thỏ hoặc trạm không zoom nữa. Chạm đúp vào khoảng đất trống thì zoom vào chỗ đó, chạm đúp lần nữa thì ra. Zoom còn lại: nút kính lúp, Cmd/Ctrl + cuộn, chụm hai ngón.

### Kết quả rà các hiệu ứng nảy khác (nguyên tắc: không hiệu ứng nào lấy trạng thái đang chuyển động làm gốc)
- Nút bấm (`ChunkyButton.Press`): đặt offset tuyệt đối theo `LipHeight`. Đạt.
- Ô cà rốt phồng khi nhận tiền: `Vector3.one × (1 + 0,25 × pop)`. Đạt.
- Huy hiệu cấp vườn khi lên cấp: `Vector3.one × …`. Đạt.
- Đạo cụ và thỏ ở trạm (`StationActorView.Animate`): tính lại mỗi khung hình từ các bộ đếm, không cộng dồn. Đạt.
- Tấm trạm, tấm sheet, bảng chọn đồ, bảng đổi tên, thẻ tạm nghỉ: tween từ một số cố định (0,8–0,92) về 1. Đạt.
- Băng thông báo (`FeelLayer.Moment`): từ 0 tới 1, có token chống chồng. Đạt.
- Thỏ ở chỗ nghỉ: góc lắc tính từ đồng hồ. Đạt.
- Màn cổng trăng: mọi thứ tính từ thời gian `_t`. Đạt.
- Thỏ đi dạo: `localScale` chỉ là ±1 theo hướng đi. Đạt.
- Chỗ duy nhất vi phạm là `OnBuy`, đã sửa.

### Chưa đạt
- Bản Mac chưa ai bấm thử; test mô phỏng bấm nhanh bằng cách gọi nút và đẩy thời gian, không phải chuột thật.
- Chạm đúp đất trống lấy vị trí con trỏ; trong test không có con trỏ nên zoom quanh giữa màn hình.

### Tồn đọng
- tồn đọng: không đọc được issue trên GitHub từ phiên này. `gh issue view` với đăng nhập mặc định báo không thấy repo (tài khoản mặc định là tài khoản công việc). Mình không tự đổi sang token của tài khoản khác khi chủ project chưa bảo. Nhờ PO chép nội dung các issue cần làm (#24, #22, #23, #28, #29) vào một file trong docs/, hoặc để chủ project cho phép.

## Sau #35 — Huy hiệu cấp vườn, sương, hình bùa, bù chỗ nghỉ cho save cũ

- Test: 298/298 EditMode qua, chạy SAU lần build và sau lần sửa file cuối trong Assets.
- Thông điệp commit đề xuất: `Drawn level badge, mist banks and charm pictures; old saves get the rest places their land brings`
  - Kết thúc: `Co-Authored-By: Claude Fable 5.2 <noreply@anthropic.com>`
- Ảnh (docs/anh/po-r6/, đã chụp lại): `2c-top.png` (huy hiệu mới trên HUD, sương dạng mây thấp trên đất chưa mở), `6-upgrades.png` (bốn hình bùa), `z2-default.png`, `15-all-far.png`.
- Bản Mac: `Build/Mac/VuonTraTho.app`, build thành công, đang mở, Player.log không có exception. Gồm bản sửa #35 và các thay đổi này.

### Đã làm
- Huy hiệu cấp vườn dùng `icon_cap-vuon.png`; con số vẽ đè lên nửa dưới; vòng tiến độ thành quầng vàng chạy quanh viền huy hiệu.
- Sương dùng `decor_suong.png`: mỗi ô đất chưa mở có bốn mảng chồng nhau, lệch cỡ, lệch độ mờ, mảng lẻ lật ngang. Khi mở rộng, các mảng trôi sang hai bên và mờ dần trong 1,4 giây.
- Tab Bùa: bốn ký tự +, », ×2, Zz được thay bằng bốn hình `bua_*.png` (trường `UpgradeData.Icon`; thiếu hình thì vẫn rơi về ký tự).
- Save cũ: số chỗ nghỉ sau khi đọc = lớn nhất giữa số đã lưu và `2 + số đợt đất đã mở`, tối đa 6. Không save nào mất chỗ nghỉ đã có.
- Sáu file hình đã chép vào `Assets/VuonTra/Art/Final/`.

### Test mới
- `An_Old_Save_Has_At_Least_The_Places_To_Rest_Its_Land_Brings`: 4 ô → 2 chỗ; 6 ô → 3; 7 ô → 4; 12 ô → 6; 6 ô nhưng đã có 5 chỗ → giữ 5.

### Chưa đạt
- Sương có viền nét rõ như mây vẽ, nên trông giống "đám mây nằm trên đất" hơn là sương mỏng; mình đã hạ độ đậm xuống khoảng 46–74% mỗi mảng. Nếu PO muốn mềm hơn thì cần hình không viền.
- Vòng tiến độ quanh huy hiệu vẽ tay khó thấy hơn bản vòng tròn trơn (chỉ lộ một viền vàng mỏng).
- Bản Mac chưa ai bấm thử.

### Tồn đọng
- tồn đọng: vẫn chưa đọc được issue trên GitHub từ phiên này (xem đoạn "Lỗi #35"); cần nguyên văn #24, #22, #23, #28, #29 trong một file để làm U1 đúng tiêu chí.

## U1 — #24 Quy tắc nút và chữ dùng chung

- Test: 298/298 EditMode qua (chạy sau lần sửa file cuối cùng trong Assets; sau đó chỉ chụp ảnh).
- Thông điệp commit đề xuất: `Shared button ranks and a three-size type scale; a button you cannot use yet says what is missing`
  - Kết thúc: `Co-Authored-By: Claude Fable 5.2 <noreply@anthropic.com>`
- Ba việc nhỏ PO nhắc lại (huy hiệu + sương, hình bùa, bù chỗ nghỉ cho save cũ) ĐÃ có trong cây làm việc từ bước trước, xem đoạn "Sau #35"; chúng sẽ lên cùng commit kế tiếp.
- Ảnh trước và sau (docs/anh/po-u1/): `24-truoc-*.png` và `24-sau-*.png` cho 5-stations, 6-upgrades, 7-quests, 8-rabbits, 8b-detail, 9c-worn.

### Đã làm (đối chiếu với nguyên văn #24 trong docs/issues-po.md)
- Ba cấp nút, đặt trong `UiKit`: chính = `Chunky` (cam, nổi); phụ = `UiKit.Secondary` (kem, viền mực, phẳng); chữ = `UiKit.TextButton` (chỉ chữ, có gạch chân). Đã chuyển nút Ghép và nút Cho nghỉ sang cấp phụ.
- Việc chưa làm được không còn là nút mờ: `ChunkyButton.SetEnabled(false)` làm nút nằm phẳng thành một tấm màu be, bỏ viền nổi và bóng, và nơi gọi điền thứ còn thiếu:
  - Trạm, Bùa, tấm trạm, Lên cấp thỏ: "Còn thiếu" + số cà rốt còn thiếu (giá trừ số đang có).
  - Nhiệm vụ: phần trăm đã xong ("74%") thay cho "Chưa xong".
  - Gọi thỏ: "Cần 1 cỏ".
  - Mở rộng vườn: ba điều kiện đã có từ R6.
- Thang chữ ba cỡ trong `UiKit`: `TextSmall` 22, `TextBody` 26, `TextTitle` 36. Đã áp cho 46 chỗ trong các thẻ và tấm (Trạm, Bùa, Nhiệm vụ, lưới thỏ, tấm chi tiết thỏ, bảng chọn đồ, bảng đổi tên): cỡ 19–23 về 22, 24–30 về 26, 32–40 về 36. Cỡ lớn hơn 40 giữ làm cỡ trưng bày (số cà rốt, tên trên tấm chi tiết, màn tiêu đề, cổng trăng).
- Chữ phụ: `Palette.InkSoft` đổi từ 58% mực sang 64% mực, đạt ngưỡng "không nhạt hơn 60% mực tím" ở mọi nơi dùng nó.
- Khoảng cách và bo góc: thêm hằng số `UiKit.CardGap` 18 và `UiKit.CardPad` 28 làm chuẩn.

### Chưa đạt so với #24
- "Mỗi dòng một ý chính; số quan trọng nhất to và đậm, phần còn lại thành chip nhỏ": CHƯA làm. Dòng phụ của trạm vẫn nhồi ba thông tin ("3,20K cà rốt / 1,5 giây · ×2 ở cấp 50"). Việc này trùng với #25 (tab Trạm) nên mình để cho U2.
- Thang chữ chưa áp cho `GardenScreen` (HUD, nhãn trong vườn, tấm trạm, thanh tab) và các màn Shell, Feel; các chỗ đó vẫn dùng số cỡ riêng.
- `CardGap` và `CardPad` mới là hằng số; các thẻ hiện có chưa được sửa để dùng chúng (khoảng cách hiện đã gần bằng nhau nhưng chưa thống nhất bằng code).
- Nút cấp "chữ" đã có nhưng chưa có chỗ nào dùng.
- Ảnh "sau" của tab Trạm không có ví dụ "Còn thiếu" vì vườn trong ảnh mẫu đang thừa cà rốt; phần đó mới được kiểm qua code, chưa có ảnh.
- Chưa kiểm tràn chữ với chuỗi dài nhất ở tiếng Anh sau khi đổi cỡ chữ (chỉ soi ảnh tiếng Việt).

### Tồn đọng
- tồn đọng: áp thang chữ ba cỡ cho GardenScreen, Shell và Feel.
- tồn đọng: tách dòng phụ của trạm thành số chính + chip (làm cùng #25).
- tồn đọng: tuỳ chọn chữ to và kiểm tràn chữ tiếng Anh sau khi đổi cỡ (thuộc #33).

## U1 — #22 Tấm trượt từ dưới lên (kèm sửa hình bùa)

- Test: 303/303 EditMode qua (chạy sau lần sửa file cuối cùng trong Assets). Thêm 5 test mới trong `GardenScreenTests`.
- Thông điệp commit đề xuất: `Lists open as a bottom sheet at 60% with a grab handle; the garden and HUD stay in view; charm pictures fill their tile`
  - Kết thúc: `Co-Authored-By: Claude Fable 5.2 <noreply@anthropic.com>`
- Commit này gồm cả #24 (chưa commit ở bước trước) và phần sửa hình bùa PO nhắc.
- Ảnh (docs/anh/po-u1/):
  - Trước: `22-truoc-5-stations.png`, `22-truoc-6-upgrades.png`, `22-truoc-7-quests.png`, `22-truoc-8-rabbits.png`.
  - Sau: `22-sau-5-stations.png`, `22-sau-6-upgrades.png`, `22-sau-7-quests.png`, `22-sau-8-rabbits.png`, `22-sau-keo-het-co.png`, `22-sau-cham-dong-toi-tram.png`.
  - Chuỗi khung hình lúc mở (6 khung liên tiếp, khung 0 là trước khi mở): `22-mo-khung-0.png` … `22-mo-khung-5.png`. Khung 3 cho thấy tấm vọt quá một chút rồi lùi về (độ nảy).

### Đã làm (đối chiếu với nguyên văn #22)
- Tấm mở cao 60% màn hình (1152/1920), vườn vẫn thấy và vẫn chạy ở phía trên. Số bay "+cà rốt" của trạm vẫn hiện nếu trạm nằm trên mép tấm.
- Tay nắm ở đầu tấm; cả dải tiêu đề là vùng nắm. Kéo lên quá nửa đường hoặc hất lên thì mở hết cỡ (dừng ngay dưới HUD). Kéo xuống rõ rệt hoặc hất xuống thì đóng; hất xuống khi đang mở hết cỡ thì về 60% trước. Chạm ra ngoài (phần vườn) thì đóng. Nút × vẫn giữ cho người dùng chuột.
- Mở: trượt lên 0,25 giây, vọt quá khoảng 3,7% rồi về. Đóng: trượt xuống 0,2 giây.
- HUD không còn bị xám và vẫn bấm được (tấm nằm dưới HUD và thanh tab trong thứ tự vẽ). Không còn lớp phủ toàn màn; chỉ một dải tối nhạt cao khoảng 210 ngay trên mép tấm.
- Tab Trạm: chạm vào thẻ một trạm (không phải nút Nâng) thì vườn trượt tới trạm đó, đặt nó vào giữa dải vườn còn thấy, trạm nảy một lần. Vườn được phép nâng lên thêm khi tấm đang mở để các trạm thấp nhất (ví dụ trạm 12) cũng lên được trên mép tấm; đóng tấm thì vườn tự về trong biên.
- Mở tấm thì tấm trạm trong vườn tự đóng (hai thứ không chồng nhau ở đáy màn hình).
- Hình bùa: bỏ đĩa màu, hình lấp đầy ô (ô 148). Bùa chưa có hình vẫn dùng đĩa màu + chữ.
- Giữ nguyên tắc từ #35: mọi chuyển động của tấm đi tới một đích cố định (0, 60%, hết cỡ); mở lại luôn về đúng 60%.

### Chưa đạt so với #22
- Kéo tay nắm mới được kiểm bằng test gọi thẳng `DragSheet`/`ReleaseSheet` và bằng ảnh; CHƯA thử bằng ngón tay hoặc chuột trong bản Mac (bản Mac chưa build lại, sẽ build cuối U1).
- Vuốt xuống chỉ đóng được khi bắt đầu từ dải tiêu đề của tấm. Vuốt xuống trong danh sách vẫn là cuộn danh sách, kể cả khi danh sách đang ở đầu.
- Khi tấm đang mở, vườn phía trên không kéo hay thu phóng được (chạm vào vườn là đóng tấm).
- Thanh tab vẫn là bốn nút tròn rời, giờ đứng trên chân tấm; lúc tấm đang trượt lên thấy các dòng chạy sau các nút (khung 1). Sẽ hết khi làm #23 (thanh tab liền mạch).
- Tấm chi tiết thỏ, bảng chọn đồ, bảng đổi tên vẫn là hộp giữa màn hình có lớp phủ tối toàn màn (#22 chỉ nói bốn tab).

### Tồn đọng
- tồn đọng: vuốt xuống từ đầu danh sách cũng đóng tấm.
- tồn đọng: cho kéo vườn khi tấm đang mở ở 60%.
- tồn đọng: tấm chi tiết thỏ và các bảng chọn chuyển sang cùng kiểu tấm trượt (xem lại ở #26).

## U1 — #23 Thanh tab liền mạch

- Test: 305/305 EditMode qua (chạy sau lần sửa file cuối cùng trong Assets). Thêm 2 test mới.
- Thông điệp commit đề xuất: `Tab bar is one plate inside the safe area; the chosen tab grows onto a raised disc with a bold name; the quest badge bounces when its count grows`
  - Kết thúc: `Co-Authored-By: Claude Fable 5.2 <noreply@anthropic.com>`
- Ảnh (docs/anh/po-u1/): trước `23-truoc-2-garden.png`, `23-truoc-5-stations.png`, `23-truoc-11-garden-en.png`; sau `23-sau-2-garden.png`, `23-sau-5-stations.png`, `23-sau-7-quests.png`, `23-sau-11-garden-en.png`.

### Đã làm (đối chiếu với nguyên văn #23)
- Một tấm nền kem liền cho cả thanh, bo góc, có viền nổi và bóng, rộng gần hết màn hình (lề 20 mỗi bên).
- Vùng an toàn: thanh đứng cách mép an toàn dưới của máy 16 (đọc `Screen.safeArea` mỗi khung hình, nên xoay máy hay đổi máy đều theo); danh sách trong tấm trượt cũng dừng phía trên thanh theo cùng số đó.
- Tab đang chọn: icon to từ 84 lên 108, đứng trên một đĩa vàng nổi nhô lên khỏi thanh, nhãn đậm và màu mực. Tab khác khi có tab đang chọn: icon mờ còn 55%, nhãn thường. Không mở tấm nào thì cả bốn tab rõ như nhau. Chuyển trạng thái mất 0,16 giây.
- Nhãn cỡ 26 (trước là 20).
- Huy hiệu số: chỉ hiện khi có phần thưởng nhiệm vụ nhận được; khi con số tăng thì nảy một lần 0,32 giây rồi về đúng cỡ gốc.
- Cả ô tab (một phần tư thanh) đều bấm được, không chỉ icon.

### Chưa đạt so với #23
- Vùng an toàn mới kiểm bằng code; chưa thấy trên máy có thanh home (ảnh mẫu và bản Mac có vùng an toàn bằng 0).
- Chỉ tab Nhiệm vụ có huy hiệu. Tab Thỏ chưa báo khi đủ cỏ để gọi thỏ, tab Trạm và Bùa chưa báo khi đủ tiền mua.
- Hằng số chừa chỗ cho thanh tab trong vườn (`TabCover` 250) chưa giảm theo thanh mới (cao 186), nên tấm trạm trong vườn đứng cách thanh một khe khoảng 60.
- Lúc tấm trượt đang đi lên, thấy mép tấm ở khe 16 dưới thanh tab.

### Tồn đọng
- tồn đọng: huy hiệu cho tab Thỏ (đủ cỏ gọi thỏ, có bộ ghép được).
- tồn đọng: giảm `TabCover` cho khớp thanh mới và kiểm lại vị trí tấm trạm.

## U1 — #28 Chuyển động (phần cơ bản)

- Test: 311/311 EditMode qua (chạy sau lần sửa file cuối cùng trong Assets). Thêm 6 test mới.
- Thông điệp commit đề xuất: `Shared motion table; buttons sink under the finger; list rows arrive one by one; the camera glides from the moon pond on start; a Reduce motion setting`
  - Kết thúc: `Co-Authored-By: Claude Fable 5.2 <noreply@anthropic.com>`
- Ảnh (docs/anh/po-u1/):
  - Camera lướt lúc mở game, 5 khung cách nhau 0,2 giây: `28-luot-khung-1.png` … `28-luot-khung-5.png` (màn tiêu đề trước và sau: `28-truoc-1-title.png`, `28-sau-1-title.png`).
  - Dòng hiện lần lượt khi mở tấm, 5 khung cách nhau 0,06 giây: `28-dong-hien-khung-1.png` … `28-dong-hien-khung-5.png`.
  - Cài đặt có nút mới: `28-truoc-10-pause.png`, `28-sau-10-pause.png`.
  - Nút lún khi chạm: KHÔNG có ảnh (cần ngón tay hoặc chuột thật); chỉ có test.

### Phần mình chọn làm trong "cơ bản" (đối chiếu với nguyên văn #28)
- Ràng buộc: bảng thời lượng dùng chung là lớp `Motion` trong `Ease.cs` (Quick 0,16 · Tap 0,18 · Panel 0,25 · PanelOut 0,2 · Travel 0,3 · Glide 1,0 · Stagger 0,035), thêm đường cong `InOutCubic` và `OutBackLight`. Có test bảo đảm không thời lượng nào vượt 0,3 giây trừ cú lướt mở game (cú lướt không chặn thao tác).
- Ràng buộc: nút "Giảm chuyển động" trong Cài đặt, lưu trong máy. Bật lên thì tấm, camera, HUD tới nơi ngay, không nảy, dòng không hiện lần lượt, không có cú lướt mở game.
- Camera: đổi zoom, bay tới trạm, trượt tới trạm khi chọn đều tăng tốc rồi giảm tốc (`InOutCubic`), 0,25–0,3 giây. Mở game: sau màn tiêu đề vườn đứng ở ao trăng, bấm bắt đầu thì lướt xuống ấm trà trong 1 giây; kéo vườn, bấm zoom, chọn trạm hay mở tấm đều lập tức ngắt cú lướt.
- Kéo có quán tính và nảy ở biên: đã có từ trước (ScrollRect kiểu đàn hồi), không sửa.
- Giao diện: nút nổi (cam) lún xuống ngay khi ngón tay chạm và nảy lại khi nhả (`PressDip`), luôn đi giữa hai vị trí cố định. Tấm trượt vào (#22). Danh sách: tối đa 8 dòng đầu hiện lần lượt, cách nhau 0,035 giây, khi mở tấm hoặc đổi tab.
- Con số cà rốt chạy số: đã có từ trước, không sửa.

### Chưa làm (để cho phần còn lại của #28 ở U2)
- Thỏ: thở, chớp mắt, vẫy tai; trái tim hoặc nốt nhạc khi chạm; đi dạo theo lối mòn; đi từ chỗ nghỉ tới trạm.
- Phần thưởng: cà rốt nảy khỏi trạm bay về HUD và ô cà rốt phồng lên; loé sáng khi nâng cấp; vòng sao khi qua mốc.
- Cảnh nền: tán cây đung đưa, đèn lồng lắc, mặt ao gợn, đom đóm bay theo đàn.
- Nút cấp phụ (viền mực, phẳng) và các ô tab chưa có phản hồi lún; chỉ nút nổi có.
- Các bảng giữa màn hình (chi tiết thỏ, chọn đồ, đổi tên, tạm nghỉ) vẫn dùng thời lượng riêng, chưa chuyển sang bảng `Motion`.
- "Giảm chuyển động" chưa tắt các chuyển động trong vườn (trạm nhún, thỏ dạo, đom đóm).

### Tồn đọng
- tồn đọng: chuyển mọi thời lượng còn viết số trực tiếp (FeelLayer, ShellView, RabbitDetailView, ItemPickerView, RenameView, SummonMomentView) sang bảng `Motion`.
- tồn đọng: "Giảm chuyển động" áp cho chuyển động trong vườn.

## U1 — #29 Phản hồi khi chạm + build bản Mac

- Test: 314/314 EditMode qua (chạy sau lần sửa file cuối cùng trong Assets và sau khi build). Thêm 3 test mới.
- Bản Mac: đã build lại (`Build/Mac/VuonTraTho.app`, 133,6 MB), gồm toàn bộ U1 (#24, #22, #23, #28 cơ bản, #29). Mở lên chạy được, Player.log không có exception. Mình CHƯA tự bấm thử trong bản Mac (không điều khiển được chuột trong app), nên kéo tay nắm, nút lún và tiếng động chưa có ai nghe và thử bằng tay.
- Thông điệp commit đề xuất: `A sound for each kind of touch; a button you cannot afford shakes and the purse flashes red; haptics with a switch in Settings`
  - Kết thúc: `Co-Authored-By: Claude Fable 5.2 <noreply@anthropic.com>`
- File mới: `Assets/VuonTra/Game/Audio/Haptics.cs` (+ .meta). Bảng chuỗi đã seed lại (thêm `pause.haptics`).
- Ảnh (docs/anh/po-u1/): thiếu tiền, 3 khung cách nhau 0,07 giây `29-thieu-tien-khung-1..3.png` (số cà rốt đỏ, tấm "Còn thiếu" lệch ngang; đây cũng là ảnh "Còn thiếu" còn nợ từ #24); Cài đặt `29-truoc-10-pause.png`, `29-sau-10-pause.png`, `29-sau-14-pause-en.png`.

### Đã làm (đối chiếu với nguyên văn #29)
- Tiếng riêng, đều tổng hợp bằng code: chạm thỏ (hai tiếng chíp), nâng cấp (blip), qua mốc (bốn chuông đi lên), mở trạm hoặc mở đất (ba nốt), nhận thưởng nhiệm vụ và quà vắng mặt (hai tiếng xu), mở tấm và đóng tấm (tiếng gió lên, xuống), thiếu tiền (hai nốt trầm đi xuống). Có test kiểm đúng tiếng cho từng thao tác.
- Thiếu tiền: chạm nút "Còn thiếu" thì mặt nút lắc ngang 0,28 giây, số cà rốt trên HUD nháy đỏ hai nhịp và ô cà rốt phồng nhẹ, kèm tiếng. Áp cho nút Nâng ở tab Trạm, tấm trạm trong vườn, nút Học bùa, nút Mở rộng vườn (chỉ nháy đỏ khi thứ thiếu là cà rốt).
- Rung: lớp `Haptics` với hai mức (nhẹ 12 ms cho nâng cấp trạm và lên cấp thỏ; vừa 28 ms cho qua mốc và gọi thỏ). Công tắc "Rung khi chạm" trong Cài đặt, lưu trong máy.
- Thanh âm lượng riêng cho nhạc và hiệu ứng: đã có từ trước.

### Chưa đạt so với #29
- Rung CHƯA chạy trên máy nào: Android có code gọi bộ rung nhưng chưa build và chưa thử, có thể còn thiếu quyền VIBRATE trong manifest; iPhone chưa có gì (cần một cầu nối native nhỏ). Trên Mac không có rung.
- Các tiếng mới chưa ai nghe: mình không nghe được âm thanh, chỉ kiểm rằng đúng tiếng được gọi. Cần chủ project nghe trong bản Mac và nói tiếng nào chói hoặc dở.
- Nút Lên cấp thỏ và nút Gọi thỏ khi thiếu chưa lắc (chưa nối).
- Ghép thỏ, học bùa, bắt sao vàng, tăng tốc vẫn dùng chung tiếng ba nốt với mở trạm.

### Tồn đọng
- tồn đọng: rung trên iPhone (cầu nối native) và quyền rung trên Android, làm khi build cho điện thoại.
- tồn đọng: lắc khi thiếu cho nút Lên cấp thỏ và Gọi thỏ.
- tồn đọng: tiếng riêng cho ghép thỏ và học bùa.

## #43 — Trạm trống không hiện thỏ, mỗi trạng thái một lời mời

- Test: 325/325 EditMode qua (chạy sau lần sửa cuối trong Assets). Thêm 11 test mới (3 core, 8 màn hình).
- Commit `738da68`, đã push lên origin main. Issue #43 đã đóng, #16 đã tick.
- File mới: `Assets/VuonTra/Game/UI/WorkerPickerView.cs` (+ .meta), hai hình giữ chỗ `Art/Placeholder/plot_sign.png` và `pad.png` (vẽ trong `PlaceholderArt.Plots`). Catalog thêm `PlotSign`, `Pad`. Bảng chuỗi seed lại (10 key mới: `plot.build`, `plot.short`, `land.expand`, `pad.hint`, `worker.*`).
- Ảnh (docs/anh/po-43/): `43-truoc-vuon.png` (vườn cũ, thỏ mặc định đứng ở mọi trạm), `43-sau-vuon-tram-trong.png` (tấm đệm ở trạm trống, biển giá ở ô chưa xây, cọc biển dưới sương), `43-sau-the-dat-chua-xay.png`, `43-sau-chon-tho.png` (danh sách chọn thỏ, đúng nghề lên đầu), `43-sau-tho-dang-lam.png`.

### Đã làm (đối chiếu với nguyên văn #43)
- Ba trạng thái tách hẳn trong `StationActorView` (`StationLook.Land / Unbuilt / Open`): đất chưa mở rộng chỉ còn cọc biển mờ "Mở rộng vườn" dưới sương; ô chưa xây có nền đất, cọc biển, bóng mờ nhạt của đạo cụ, biển giá "Xây · 120" (cam và nảy khi đủ tiền, "Còn thiếu N" trên tấm xám khi thiếu); trạm đã xây mà chưa có thỏ thì đạo cụ chạy đủ màu, chỗ thỏ là tấm đệm viền chấm có dấu "+" và nhãn "Xếp thỏ +".
- Thỏ chỉ được vẽ khi có thỏ của người chơi ở đó. Thỏ mặc định của trạm không còn trong vườn; `StationData.Rabbit` và `WorkFrames` vẫn giữ trong catalog cho #2.
- Chạm tấm đệm, hoặc ô "ai làm ở đây" trên tấm trạm, mở `WorkerPickerView`: thỏ đúng nghề lên đầu, nền vàng, "×1,5"; nút "Xếp vào"; xếp xong tự đóng. Chưa có thỏ thì nói đi làm nhiệm vụ.
- Ngủ gật chỉ xảy ra ở trạm có thỏ (`GardenScreen.DozeSomeone`).
- Người mới: `Rabbits.GiftFirst` tặng một Thỏ Bông ở ấm trà khi không có save (và khi Chơi lại từ đầu). Save cũ không có thỏ giữ nguyên. `GameRunner.SaveDirectory` cho test trỏ vào thư mục tạm để thử cả hai đường thật.
- Huy hiệu trên tab Thỏ: số trạm đã mở chưa có thỏ (`Rabbits.StationsWithoutWorker`).
- Thu nhập trạm không có thỏ không đổi (test).

### Chưa đạt so với #43
- Hai hình mới là hình giữ chỗ vẽ bằng code, chưa phải vẽ tay. Thay bằng file cùng tên trong `Art/Final/` rồi seed.
- Đất chưa mở rộng còn một cọc biển dưới sương (issue viết "chỉ có sương và cây"); giữ để thẻ Mở rộng còn chạm được từ vườn.
- Chưa ai bấm thử tấm đệm và danh sách chọn thỏ bằng tay trong bản Mac.

### Tồn đọng
- tồn đọng: vẽ tay nền đất có cọc biển và tấm đệm trống.

## #25 — Tab Trạm: thu nhập mỗi giây là con số chính, giữ tay để nâng liên tục

- Test: 329/329 EditMode qua (chạy sau lần sửa cuối trong Assets). Thêm 4 test.
- Commit `9bdf90f`, đã push. Issue #25 đã đóng, #16 đã ghi.
- Bảng chuỗi seed lại (3 key mới: `row.tonext`, `row.topmilestone`, `row.feeds`).
- Ảnh (docs/anh/po-25/): `25-truoc-5-stations.png`, `25-sau-5-stations.png`, `25-sau-22-full.png` (tấm kéo hết cỡ, sáu dòng).

### Đã làm (đối chiếu với nguyên văn #25)
- `StationRowView` làm lại: hình 164, con số chính "+2,13K/giây" (cỡ tiêu đề, màu cam), tên trạm cỡ thân, dòng nhỏ "3,20K cà rốt / 1,5 giây", thanh mốc có chữ "Còn 23 cấp tới ×2" (hoặc "Đã qua mọi mốc"), chỉ hiện ở trạm đã mở.
- Trạm nguyên liệu: con số chính "×3,4", dòng "Tiếp sức cho" kèm hình nhỏ các trạm nó tiếp sức.
- Giữ tay nâng liên tục, nhanh dần: `PressDip.Repeat` (0,4 giây chờ, 0,26 giây rồi nhanh thêm 18% mỗi lần, tối thiểu 0,07 giây). Mỗi lần lặp lấy mốc từ lần trước, không từ thời điểm hiện tại. Thả tay sau khi đã lặp thì cú chạm kết thúc bị nuốt, không mua thêm. Mọi nút chunky giờ đi qua `PressDip.Fire` (nút `Button.onClick` → `Dip.Fire` → `Pressed`), nên test gọi `onClick.Invoke()` vẫn chạy như cũ.
- Trạm kế tiếp để mở có nền vàng và nút "Mở trạm"; trạm sau nó nền xám. Trạm chưa mở vốn đã nằm cuối vì mở theo thứ tự.
- Chạm dòng cuộn vườn tới trạm: có sẵn từ #22.

### Chưa đạt so với #25
- Chưa ai giữ tay thật để cảm nhịp lặp; các con số nhịp nằm trong `PressDip` (hằng `HoldDelay`, `FirstInterval`, `LeastInterval`, `QuickerBy`), chỉnh một chỗ.

### Tồn đọng
- tồn đọng: cảm nhịp giữ nút trên máy thật, chỉnh bốn hằng trong `PressDip` nếu quá nhanh hoặc quá chậm.

## #27 — Tab Nhiệm vụ: thứ nhận được lên đầu, thưởng bay về ví

- Test: 334/334 EditMode qua (chạy sau lần sửa cuối trong Assets). Thêm 5 test.
- Commit `1307309`, đã push. Issue #27 đã đóng, #16 đã tick (cả dòng #25/#27).
- Hình mới: `Art/Placeholder/icon_clover.png` (giữ chỗ, vẽ trong `PlaceholderArt`), catalog thêm `Clover`. Bảng chuỗi seed lại (3 key: `quests.today.refresh`, `quests.ready`, `quests.claimall`).
- Ảnh (docs/anh/po-27/): `27-truoc-7-quests.png`, `27-sau-7-quests.png`, `27-bay-1..3.png` (cà rốt bay lên ví, cỏ bay tới tab Thỏ).

### Đã làm (đối chiếu với nguyên văn #27)
- Nhóm "Nhận được" ở đầu danh sách với nút "Nhận tất cả" (`GardenScreen.OnClaimAll` gọi `OnClaim` cho từng nhiệm vụ xong). Dòng nhận được nền vàng. `OrderQuestRows` sắp lại thứ tự con mỗi lần refresh, chỉ dời dòng nào lệch chỗ.
- `QuestRowView` làm lại: chưa xong thì chỉ thanh + số (nút ẩn hẳn); xong thì nút "Nhận" và ẩn thanh; đã nhận (nhiệm vụ ngày) thì tấm "Đã nhận" phẳng và dòng mờ.
- Thưởng là hai chip có hình: cà rốt và cỏ bốn lá. Chip cỏ ẩn khi thưởng không có cỏ.
- "Hôm nay · làm mới sau N giờ M phút": `GardenScreen.SecondsToNextDay(DateTimeOffset)` tính tới nửa đêm giờ máy; `Span()` dùng chung với bảng "lúc bạn đi vắng".
- `FeelLayer.Fly(from, to, sprite, ...)`: vài bản sao bay theo cung bezier, mỗi cái trễ 0,05 giây, cái cuối hạ cánh thì gọi `onArrive`. Cà rốt → ví (ví phồng + "+N" + vụn cà rốt), cỏ → icon tab Thỏ (`_tabPop[3]`). Điểm xuất phát đọc trước khi claim vì claim làm dòng dời chỗ hoặc ẩn.

### Chưa đạt so với #27
- Không có chip cà rốt vàng: game chưa có cà rốt vàng (#9, mục Cà rốt vàng và Tiệm trong #16).
- Cỏ bốn lá bay tới tab Thỏ chứ không tới "ô trên HUD", vì HUD không có ô cỏ.
- Hình cỏ bốn lá là giữ chỗ.

### Tồn đọng
- tồn đọng: khi làm Cà rốt vàng và Tiệm, thêm chip cà rốt vàng vào `QuestRowView` và đích bay cho nó.

## #36 — Nâng cấp nhanh: Tới mốc, Nâng tất cả, giữ lại một phần, thỏ Tối đa

- Test: 350/350 EditMode qua (chạy sau lần sửa cuối trong Assets). Thêm 10 test core (`UpgradePlanTests.cs`, file mới) và 6 test màn hình.
- Commit `d7e6324` (Core, save lên v9: `buyMode` nhận thêm -1, thêm `spendShare`) và `6591a5b` (giao diện), đã push. Issue #36 đã đóng, #16 đã tick.
- File mới: `Assets/VuonTra/Core/UpgradePlan.cs`, `Assets/VuonTra/Game/UI/UpgradeAllView.cs` (+ .meta). Bảng chuỗi seed lại (11 key: `mode.milestone`, `upgradeall.*`, `rabbits.levelmax`).
- Ảnh (docs/anh/po-36/): `36-truoc-*.png`, `36-sau-5-stations.png` (dòng Nâng tất cả, công tắc bốn mức), `36-sau-nang-tat-ca.png` (bảng tóm tắt), `36-sau-the-tram-mo.png` (tấm trạm có công tắc), `36-sau-8b-detail.png` (nút Tối đa của thỏ).

### Đã làm (đối chiếu với nguyên văn #36)
- `Simulation.BuyToMilestone` (-1): vừa đủ tới mốc kế; `Simulation.Budget(s)` = ví × `SpendShare` dùng cho Tối đa, Tới mốc, Nâng tất cả và Tối đa của thỏ.
- `UpgradePlan.Plan/Apply/Total`: tham lam theo (Δ thu nhập mỗi giây) / (cà rốt), cụm tới mốc kế tối đa 10 cấp, trên bản nháp của `Levels`; `Apply` mua lại theo giá lúc bấm xác nhận, từ chối nếu không đủ. Test chứng minh giống hệt bấm +1 từng lần.
- `Rabbits.LevelsAffordable/LevelUpMax`: tới trần của bậc, trong ngân sách.
- `GardenScreen.BuildModeBar` dựng cùng một công tắc bốn mức cho thanh tab và tấm trạm (`_modeButtons` giờ là danh sách, hai bộ nút cùng đọc `BuyMode`). Tấm trạm cao 340 (trước 280); `SelectStation` cho vườn trượt quá chân một đoạn bằng tấm để trạm thấp nhất vẫn ló lên trên tấm.
- Dòng "Nâng tất cả" đứng đầu tab Trạm (dòng Plot lùi một chỉ số, test đã chỉnh), mở `UpgradeAllView`.
- `StationActorView`: số cấp chạy lên tới cấp mới trong `Motion.Panel`; giảm cấp hoặc đổi trạng thái thì nhảy thẳng.

### Chưa đạt so với #36
- Trạm chưa có trần cấp để "không vượt trần" (mục "Trần cấp trạm 500" trong #16 chưa làm); thỏ thì có.
- Chưa ai bấm tay bảng Nâng tất cả và công tắc trên tấm trạm.

### Tồn đọng
- tồn đọng: khi làm "Trần cấp trạm 500", `Simulation.BuyCount`, `Economy.MaxAffordable` và `UpgradePlan.Plan` cần chặn ở trần (hiện không có trần).

## #37 — Khay sản phẩm (luật và khay; còn hình)

- Test: 365/365 EditMode qua (chạy sau lần sửa cuối trong Assets). Thêm 11 test core (`TrayTests.cs`, file mới) và 4 test màn hình.
- Commit `5178cd2` (Core, save v10) và `7296124` (khay), đã push. Issue #37 để mở (còn hình), đã comment; #16 đã ghi chú.
- File mới: `Assets/VuonTra/Core/Tray.cs`, `Assets/VuonTra/Game/UI/TrayView.cs` (+ .meta). `StationData` thêm `Product` (hình món, seed nhặt `Art/Final/product_<id>.png`). Bảng chuỗi seed lại (`tray.full`).
- Ảnh (docs/anh/po-37/): `37-truoc-vuon.png`, `37-sau-vuon-khay.png` (khay trên thanh tab, một món vàng), `37-ban-1..2.png` (bán: cà rốt bay lên ví).

### Đã làm (đối chiếu với nguyên văn #37)
- `Tray`: 5 ô (tối đa 8 trong save), chồng 5; mỗi 75 giây làm việc một món (`Simulation.Tick` gọi `Tray.Advance`), giữ mẻ khi hết chỗ; bán = 15 giây thu nhập của trạm × số món; vuốt bán hết; vắng mặt làm đầy tới sức chứa (`ClaimAway` → `FillAway`); món vàng 4%, 2 mỗi ngày (`GoldenToday` reset trong `RollDaily`), bán thêm 1 cỏ.
- `GameRunner`: sự kiện `ProductMade`, `SellProduct`, `SellAllProducts`; `_rng` cấp xúc xắc cho món vàng.
- `TrayView`: ô tròn, hình món (đạo cụ thay thế), huy hiệu "×N", viền vàng, nhãn "Khay đầy", kéo ngang ≥ 120 để bán hết. Khay chỉ hiện khi đang chơi, không mở tấm trượt, không mở tấm trạm, và có gì trong khay.
- `GardenScreen.OnProductMade`: món bay từ trạm xuống ô; `PaySale`: cà rốt bay lên ví, cỏ bay tới tab Thỏ.
- Nhịp: người nhặt hết được thêm khoảng 20% (test giữ 15–25%).

### Chưa đạt so với #37
- Chưa vẽ 7 món và khung khay (khay dùng hình đạo cụ của trạm).
- Chưa thử tay.

### Tồn đọng
- tồn đọng: vẽ `product_<id>.png` cho 7 trạm pha chế và khung khay, bỏ vào `Art/Final/`, seed.
- tồn đọng: cách mở rộng khay lên 8 ô (bùa hoặc Tiệm, #44).

## #18 — Chụp ảnh vườn, lát đầu (bản Mac)

- Test: 369/369 EditMode qua (chạy sau lần sửa cuối trong Assets). Thêm 2 test core (`PhotosTests.cs`, file mới) và 2 test màn hình.
- Commit `1830282`, đã push. Issue #18 để mở (còn khung ảnh, công tắc, album, nhiệm vụ, mobile), đã comment; #16 đã ghi chú.
- File mới: `Assets/VuonTra/Core/Photos.cs`. Save thêm `photoDay`, `photosTaken` (vẫn v10). `Sfx.Shutter`, `FeelLayer.Flash(color)`. Bảng chuỗi seed lại (11 key `photo.*`).
- Ảnh (docs/anh/po-18/): `18-chup-khung.png`, `18-chup-xem.png`.

### Đã làm (đối chiếu với nguyên văn #18)
- Nút máy ảnh trên HUD; `GardenScreen.EnterPhotoMode/ExitPhotoMode/Snap`; chụp bằng `Cam.Render` vào RenderTexture cỡ `Screen`, ghi PNG vào `~/Pictures/VuonTraTho/` (`PhotoFolder` cho test và preview trỏ chỗ khác).
- Tấm xem lại với "Mở thư mục" (`open -R`), "Chép ảnh" (osascript, chép PNG vào clipboard), "Đóng".
- Quà ngày: `Photos.Take(s, day)` dùng cùng số ngày `GameRunner.Today` với nhiệm vụ ngày.

### Chưa đạt so với #18
- Khung ảnh và hai công tắc (tên thỏ, đom đóm), album 12 ảnh, nhiệm vụ ăn theo, quà bằng cà rốt vàng (#44), phần mobile.
- "Mở thư mục" và "Chép ảnh" chưa ai bấm thật trong bản Mac.

### Tồn đọng
- tồn đọng: phần còn lại của #18 như trên; trong batch render ảnh chụp ra khổ ngang vì `Screen` của Unity batch không phải cửa sổ game, trong bản Mac thật thì theo cửa sổ.

## #17 — Hồ sơ người chơi và lớp giao tiếp tài khoản (phần làm được ngay)

- Test: 385/385 EditMode qua (chạy sau lần sửa cuối trong Assets). Thêm 11 test core (`ProfileTests.cs`, file mới) và 1 test màn hình.
- Commit `691d3fa`, đã push. Issue #17 để mở (phần cần Unity Cloud / tài khoản nhà phát triển), đã comment, nhãn chờ-PO-duyệt.
- File mới: `Assets/VuonTra/Core/Profile.cs` (hồ sơ, mã người chơi, `IAccountService` + `LocalOnlyAccount`), `Assets/VuonTra/Game/UI/ProfileView.cs`. Save lên v11 (`profile`: name, avatar, code); save cũ đọc được, được cấp mã khi chưa có. Bảng chuỗi seed lại (16 key `profile.*`, `backup.*`, `pause.profile`).
- Ảnh (docs/anh/po-17/): `17-truoc-10-pause.png`, `17-sau-10-pause.png`, `17-sau-profile.png`, `17-sau-profile-en.png`.

### Đã làm (đối chiếu với nguyên văn #17, phần "làm được ngay")
- Hồ sơ lưu trên máy: tên vườn (tối đa 16 ký tự, làm sạch như tên thỏ), thỏ đại diện (chọn trong thỏ đang có; thỏ bị ghép mất thì rơi về con đầu), mã người chơi `THO-` + 6 ký tự không nhầm lẫn (bỏ 0/O/1/I), cấp vườn tính từ `GardenXp`.
- Màn hồ sơ: hàng đầu tấm Tạm nghỉ (thỏ, tên vườn, mã) chạm mở; trong đó chạm hình để đổi thỏ, bút để đặt tên (dùng chung tấm đặt tên với thỏ, tiêu đề đổi theo), nút Chép mã (clipboard, đổi chữ "Đã chép" 1,5 giây), dòng cấp vườn · số thỏ · số trạm.
- Lớp giao tiếp tài khoản: `IAccountService` (Provider, State, CanLink, LinkedName, Link, Unlink, Changed) với bản `LocalOnlyAccount`; màn hồ sơ hiện "Sao lưu: Chỉ trên máy này", nút Liên kết tài khoản tắt kèm dòng "Sắp có". Game Center / Play Games (#39, #40) và lõi đồng bộ (#38) cắm vào đây.

### Chưa đạt so với #17
- Tài khoản khách tự tạo "không hỏi gì": chưa có gì để đăng ký nên chưa làm; mã người chơi đóng vai định danh trên máy.
- Thưởng một lần khi liên kết: chưa có nhà cung cấp nào để liên kết, để cùng #38.
- Đồng bộ và hỏi giữ bản nào khi lệch: thuộc #38.

### Tồn đọng
- tồn đọng: phần cần chủ project trong #17 (liên kết Unity Cloud hoặc đi theo hướng #39/#40); Chép mã chưa ai bấm thật trong bản Mac.

## #46 — Thang độ hiếm chung: 6 bậc, mỗi bậc có số sao

- Test: 392/392 EditMode qua (chạy sau lần sửa cuối trong Assets). Thêm 2 test core (`RarityTests.cs`, file mới) và 1 test màn hình.
- Commit `7bb0528`, đã push. Issue #46 (mới tạo cho mục "Thang độ hiếm chung" trong #16) đã comment, nhãn chờ-PO-duyệt.
- File mới: `Assets/VuonTra/Core/Rarity.cs` (6 bậc, số sao = bậc + 1). `Rabbits.TierCount` đọc từ đó. `L10n.Rarity(tier)` = tên bậc + sao; `UiKit.Stars` = dải sao trên nền giấy. Không đổi save, không thêm chuỗi.
- Ảnh (docs/anh/po-46/): `46-truoc-chon-tho.png`, `46-truoc-9-items.png`, `46-sau-8b-detail.png`, `46-sau-8c-album.png`, `46-sau-9-items.png`, `46-sau-43-chon-tho.png`, `46-sau-18c-arrived.png`.

### Đã làm (đối chiếu với #46)
- Thang chung trong Core; thỏ và vật phẩm cùng 6 bậc (test).
- Mọi dòng ghi bậc có sao: tấm thỏ ("Thỏ Chòm Sao · bậc Đỏ ★★★★★★"), trang loài, dòng ghép, tiêu đề nhóm trong sổ ("Bạc ★ · 4/14"), danh sách bùa (dòng dưới tên: "Vàng ★★★★★ · +45%…"), khoảnh khắc gọi thỏ, quà.
- Dải sao trên mọi khung theo bậc: ô thỏ trong lưới, tấm thỏ, ô loài trong sổ (kể cả loài chưa gặp), ô bùa, hàng chọn thỏ cho trạm.

### Chưa đạt
- Tên bậc trong danh sách bùa chuyển xuống dòng dưới tên (tên + bậc một dòng bị tràn ở "Mũ đầu bếp · Xanh dương ★★★").
- Chưa có khung vẽ riêng cho từng bậc (#33 nêu "sao hoặc khung"); chọn sao vì rẻ và đọc được ở mọi cỡ.

### Tồn đọng
- tồn đọng: nhãn tên thỏ trên vườn (chip màu bậc) chưa có sao; để lượt UI 2 nếu PO muốn.

## #47 — Trần cấp trạm 500

- Test: 403/403 EditMode qua (chạy sau lần sửa cuối trong Assets). Thêm 5 test core (`StationCapTests.cs`, file mới) và 1 test màn hình.
- Commit `283c820`, đã push. Issue #47 (mới tạo cho mục "Trần cấp trạm 500" trong #16) đã comment, nhãn chờ-PO-duyệt.
- `Economy.MaxLevel = 500`, `Economy.LevelsLeft`; `Simulation.BuyCount` kẹp theo đó (0 ở trần) và `TryBuy` từ chối; `MaxAffordable` không vượt trần nên Nâng tất cả cũng dừng. Chuỗi mới `row.maxlevel` (seed lại). Không đổi save.
- Ảnh (docs/anh/po-47/): `47-sau-tab-tram.png`, `47-sau-tam-tram.png`.

### Đã làm (đối chiếu với #47)
- Mọi chế độ nâng dừng ở 500; không tiêu tiền ở trần (test core).
- Nút nâng ở tab Trạm và tấm trạm ghi "Cấp tối đa" và tắt; mũi tên trong vườn tắt (test màn hình).

### Tồn đọng
- tồn đọng: chưa có "khoảnh khắc" khi chạm trần (một dòng chúc mừng); save cũ có trạm quá 500 (không thể xảy ra với bản này) chỉ bị khoá nâng, không bị hạ cấp.

## #48 — Sao của thỏ mỗi 10 cấp

- Test: 410/410 EditMode qua (chạy sau lần sửa cuối trong Assets). Thêm 2 test core (trong `RabbitsTests.cs`) và 1 test màn hình.
- Commit `6b7b075`, đã push. Issue #48 (mới tạo cho mục "Sao của thỏ mỗi 10 cấp" trong #16) đã comment, nhãn chờ-PO-duyệt.
- `Rabbits.Stars`, `Rabbits.StarsEarned`; `L10n.LevelStars` (" ☆☆"). Hai chuỗi `moment.star.*` (seed lại). Không đổi save, không đổi hệ số.
- Ảnh (docs/anh/po-48/): `48-sau-tam-tho.png`, `48-sau-luoi-tho.png`, `48-sau-nhan-vuon.png` (dữ liệu chụp đặt cấp theo chỉ số nên vài thỏ Bạc vượt trần 10; chỉ là dữ liệu preview).

### Đã làm (đối chiếu với #48)
- Mỗi 10 cấp một sao rỗng ☆, trần theo bậc (Bạc tối đa 1, Đỏ tối đa 6) (test core).
- Sao hiện sau cấp ở ô thỏ trong lưới, dòng cấp trên tấm thỏ, và sau tên trên nhãn trong vườn (test màn hình).
- Lên cấp qua mốc 10 (một cấp hay Tối đa) thì băng rôn "Bé … được sao thứ n!" kèm tiếng (test màn hình).
- Hệ số thỏ không đổi.

### Tồn đọng
- tồn đọng: hàng chọn thỏ cho trạm và hồ sơ chưa ghi sao cấp (chỉ ba chỗ trên); ☆ là ký tự chữ, chưa phải hình vẽ.

## #26 — Tab Bùa: hiệu lực hiện tại → sau một cấp, thanh cấp bằng chấm

- Test: 412/412 EditMode qua (chạy sau lần sửa cuối trong Assets). Thêm 1 test core (`UpgradesTests.cs`) và 1 test màn hình.
- Commit `a754d5f`, đã push. Issue #26 đã comment (phần hình đã xong từ `52be412`), nhãn chờ-PO-duyệt.
- `Upgrades.EffectAt(u, level)`; `UpgradeRowView.EffectText`; 6 chuỗi `upgrade.effect.*`, `upgrade.next`, `upgrade.full` (seed lại). Không đổi save.
- Ảnh (docs/anh/po-26/): `26-truoc-6-upgrades.png`, `26-sau-6-upgrades.png`.

### Đã làm (đối chiếu với nguyên văn #26)
- Bốn hình bùa vẽ cùng nét: đã có từ `52be412` (không thuộc phiên này).
- Mỗi thẻ ghi hiệu lực hiện tại và sau khi học thêm một cấp: dòng "+0,2 giây mỗi chạm → +0,4 giây mỗi chạm"; chưa học thì "×1 cà rốt → ×2 cà rốt"; đầy thì "+10 giờ vắng mặt · đã đầy" (test hai thứ tiếng).
- Thanh cấp dạng chấm (1/10, 5 chấm cho Giấc ngủ dài) thay chip chữ (test).

### Tồn đọng
- tồn đọng: dòng mô tả cũ ("Mỗi lần chạm làm nhanh thêm 0,2 giây") vẫn giữ phía trên dòng hiệu lực; nếu PO thấy thừa thì bỏ.

## #32 — HUD: ô cà rốt và thanh Trà lực (hai ý đầu)

- Test: 416/416 EditMode qua (chạy sau lần sửa cuối trong Assets). Thêm 2 test core (`EconomyTests.cs`) và 2 test màn hình.
- Commit `386be8e`, đã push. Issue #32 để mở (còn hai ý: cách viết số lớn + chạm giữ xem số đầy đủ; gom cụm nút góc phải), đã comment, nhãn chờ-PO-duyệt.
- File mới: `Assets/VuonTra/Game/UI/IncomeView.cs`. `Economy.StationIncomePerSecond(defs, s, i, out thỏ, out nguyên liệu)`, `Energy.SecondsUntilBoost`. 9 chuỗi `income.*`, `energy.wait` (seed lại). Không đổi save.
- Ảnh (docs/anh/po-32/): `32-truoc-4-boost.png`, `32-sau-32-income.png`.

### Đã làm (đối chiếu với nguyên văn #32)
- Ô cà rốt: chạm mở bảng "Cà rốt đến từ đâu": một dòng mỗi trạm pha đang mở (hình, tên, +N/giây, "thỏ ×a · nguyên liệu ×b"), rồi Bùa ×, Tốc độ cả vườn ×, Đang tăng tốc ×2 (khi có), Tổng cộng (test màn hình + test core cho phép tính).
- Thanh Trà lực: chưa đủ thì chạm hiện dòng nổi "Còn N phút nữa là đủ tăng tốc" và không tiêu gì; đủ thì nút thở nhẹ (±2,5%) cho tới khi bấm (test màn hình). Trong lúc tăng tốc thỏ đã làm nhanh gấp đôi từ trước; chưa thêm viền sáng.

### Chưa đạt so với #32
- Viền sáng cả vườn khi tăng tốc; một cách viết số lớn thống nhất + chạm giữ xem số đầy đủ; gom cụm nút góc phải.

### Tồn đọng
- tồn đọng: các ý trên; bảng thu nhập chưa ghi mốc ×2 đang áp dụng cho từng trạm (đã nằm trong số).

## #31 — Di chuyển trong vườn: mũi tên mép màn hình và nút về ấm trà (hai ý)

- Test: 417/417 EditMode qua (chạy sau lần sửa cuối trong Assets). Thêm 1 test màn hình.
- Commit `ceca860`, đã push. Issue #31 để mở (còn: vuốt trái/phải trên tấm trạm; chạm vùng ở zoom xa; nhớ vị trí và zoom), đã comment, nhãn chờ-PO-duyệt.
- `GardenScreen.BuildEdges/UpdateEdges/FlyTo`; `StationActorView.CanUpgradeShown`. Không chuỗi mới, không đổi save.
- Ảnh (docs/anh/po-31/): `31-sau-mui-ten.png` (viên "▲ 2" dưới HUD khi nhìn chân vườn với ví đầy), `31-sau-ve-am-tra.png` (zoom gần ở chân vườn: "▲ 7" và nút ấm trà góc phải).

### Đã làm (đối chiếu với nguyên văn #31)
- Mũi tên ở mép trên và mép dưới khi ngoài tầm nhìn có trạm nâng được hoặc thỏ ngủ gật: viên "▲ N" dưới HUD và "▼ N" trên khay; chạm thì bay tới trạm gần nhất phía đó (test). "Thưởng đang chờ" chưa tính (chưa có nguồn thưởng ngoài màn hình ngoài khay, mà khay luôn ở dưới).
- Nút "về ấm trà": nút tròn hình ấm trà ở góc phải trên khay, hiện khi ấm trà ngoài tầm nhìn, chạm thì bay về (test). Ẩn khi tấm trượt mở.

### Chưa đạt so với #31
- Vuốt trái/phải trên tấm trạm để nhảy trạm kế; chạm một vùng ở zoom xa để phóng vào; nhớ vị trí và mức zoom lần trước.

### Tồn đọng
- tồn đọng: ba ý trên; mũi tên chưa tính thỏ ngủ gật ở chỗ nghỉ (chỉ thỏ đang làm ở trạm).

## #31 — Hai ý nữa: chạm một lần ở zoom xa, nhớ vị trí và zoom (save v12)

- Test: 425/425 EditMode qua (chạy sau lần sửa cuối trong Assets). Thêm 1 test save và 2 test màn hình.
- Commit `57b1541` (commit gộp với quà liên kết #17 của session kia, vì hunk xen kẽ trong cùng file), đã push. Issue #31 để mở (còn: vuốt trái/phải trên tấm trạm), đã comment thêm.
- Save lên v12 (`view`: x, y, zoom); save cũ mở ở ấm trà như trước; zoom lạ bị bỏ. `GardenScreen.SetViewRaw` (glide không tự huỷ). Không chuỗi mới, không ảnh mới (không đổi hình).

### Đã làm (đối chiếu với nguyên văn #31)
- Ở mức zoom xa: chạm một vùng thì phóng vào vùng đó: một chạm trên đất trống ở zoom xa về zoom thường quanh chỗ chạm (trong bản chạy lấy toạ độ chạm; test lấy giữa màn hình). Ở zoom thường vẫn cần chạm hai lần như cũ.
- Nhớ vị trí và mức zoom lần trước: mỗi khung hình đang chơi ghi điểm vườn dưới giữa màn hình và zoom vào state (lưu theo autosave 5 giây); mở lại game, máy quay lướt từ hồ trăng tới đúng chỗ và zoom đó thay vì ấm trà (test).

### Tồn đọng
- tồn đọng: vuốt trái/phải trên tấm trạm (ý cuối của #31).

## #31 — Ý cuối: vuốt trái/phải trên tấm trạm

- Test: 426/426 EditMode qua (chạy sau lần sửa cuối trong Assets). Thêm 1 test màn hình.
- Commit `3567671`, đã push. Issue #31 đã đủ năm ý, comment cuối, nhãn chờ-PO-duyệt.
- File mới: `Assets/VuonTra/Game/UI/CardSwipe.cs` (bắt vuốt ngang trên mặt tấm, ngưỡng 80 px, bỏ qua vuốt chủ yếu theo chiều dọc). `GardenScreen.StepStation(±1)`. Không chuỗi mới, không ảnh mới (không đổi hình).

### Đã làm (đối chiếu với nguyên văn #31)
- Trên tấm trạm: vuốt trái sang trạm kế, vuốt phải về trạm trước, vườn cuộn theo; dừng ở hai đầu, không vòng; nhích nhẹ hoặc vuốt dọc không tính (test).

### Tồn đọng
- tồn đọng: cử chỉ thật trên bản Mac/điện thoại chưa ai thử (test gọi thẳng OnEndDrag với toạ độ giả).

## #22 — Hai ý còn lại: vuốt xuống ở đâu cũng đóng, vườn phía trên vẫn chạm và kéo được

- Test: 428/428 EditMode qua (chạy sau lần sửa cuối trong Assets). Thêm 2 test màn hình, sửa 1 test (chạm ra ngoài giờ là chạm vào vườn).
- Commit `1c59ccd`, đã push. Issue #22 để mở (còn: tấm chi tiết thỏ / chọn đồ / đổi tên chuyển sang kiểu tấm trượt; thử tay kéo tay nắm), đã comment, nhãn chờ-PO-duyệt.
- File mới: `Assets/VuonTra/Game/UI/SheetPull.cs` (gắn lên mỗi danh sách trong tấm). Bỏ lớp "Outside" che vườn. Không chuỗi mới, không ảnh mới (không đổi hình).

### Đã làm (đối chiếu với bình luận "Còn lại" trong #22)
- Vuốt xuống ở bất kỳ đâu trên tấm để đóng khi danh sách đang ở đầu: danh sách đang ở đầu + ngón đi xuống thì kéo tấm theo (danh sách đứng yên), thả ra thì theo luật cũ (thấp thì đóng, nhanh thì về 60%); danh sách đang cuộn dở thì vẫn cuộn danh sách; vuốt lên ở đầu danh sách vẫn là của danh sách (test).
- Kéo và chạm được phần vườn phía trên khi tấm đang mở: không còn lớp chắn; vườn kéo được (ScrollRect không bị tắt khi tấm mở); chạm đất trống thì đóng tấm; chạm một trạm thì tấm nhường chỗ cho thẻ trạm đó (test).

### Chưa đạt so với #22
- Tấm chi tiết thỏ, bảng chọn đồ, đổi tên vẫn là hộp giữa màn có phủ tối.
- Thử tay kéo tay nắm và vuốt danh sách trên bản Mac.

### Tồn đọng
- tồn đọng: hai ý trên.

## #23 — Ba ý còn lại: tab không chọn rõ hơn, tấm trạm sát thanh tab, huy hiệu tab Trạm và tab Thỏ

- Test: 430/430 EditMode qua (chạy sau lần sửa cuối trong Assets). Thêm 2 test màn hình.
- Commit `9cff4c9`, đã push. Issue #23 để mở (còn: xem vùng an toàn trên máy thật có tai thỏ), đã comment, nhãn chờ-PO-duyệt.
- `TabCover` tính từ thanh tab (khe 16 + cao 170 + 10). Không chuỗi mới.
- Ảnh (docs/anh/po-23/): `23-truoc-tam-tram.png`, `23-sau-tam-tram.png`, `23-sau-thanh-tab.png`.

### Đã làm (đối chiếu với mục "Còn lại" trong #23)
- Tab không được chọn: icon giữ 68% thay vì 55%, nhãn đậm màu hơn (test: trong khoảng 0,6–0,7).
- Tấm trạm hạ sát thanh tab: đáy tấm cách đỉnh thanh ≤ 16 px (test); khay sản phẩm vẫn ẩn khi có tấm trạm.
- Huy hiệu tab Trạm: số trạm mở được ngay với số cà rốt đang có. Huy hiệu tab Thỏ: trạm thiếu thỏ + số bậc ghép được + loài mới chưa xem (test).

### Chưa đạt so với #23
- Vùng an toàn trên máy thật có tai thỏ: cần chủ project.

### Tồn đọng
- tồn đọng: ý trên.

## #32 — Hai ý còn lại: chạm giữ xem số đầy đủ, gom cụm nút góc phải

- Test: 442/442 EditMode qua (chạy sau lần sửa cuối trong Assets). Thêm 2 test core (`BigNumTests.cs`) và 1 test màn hình.
- Commit `40443d7`, đã push. Issue #32 còn một ý (viền sáng cả vườn khi tăng tốc), đã comment, nhãn chờ-PO-duyệt.
- `BigNum.ToFullString(group)`, `L10n.Full` (vi dùng dấu chấm, en dấu phẩy); chuỗi `purse.full` (seed lại). `GardenScreen.PursePointer` + đếm giữ trong `Advance`.

### Đã làm (đối chiếu với nguyên văn #32)
- Con số lớn dùng chung một cách viết: đã vậy từ trước (`Short()` ở mọi nơi). Chạm giữ ô cà rốt 0,45 giây thì số đầy đủ nổi lên ("13.912.345 cà rốt"); quá 15 chữ số thì vẫn dạng ngắn vì double không giữ được chữ số cuối; thả ra sau khi giữ không mở bảng thu nhập (test).
- Cụm nút góc phải (máy ảnh, zoom, cài đặt) đứng trên một tấm nền chung, có chỗ cho nút thứ tư (test có tấm).

### Chưa đạt so với #32
- Viền sáng cả vườn trong lúc tăng tốc (thỏ đã làm nhanh gấp đôi từ trước).

### Tồn đọng
- tồn đọng: ý trên; giữ ngón thật trên bản Mac chưa ai thử.

## #24 — Hai ý còn lại: thang chữ cho màn vườn, khung ngoài, băng thông báo; khoảng cách thẻ dùng chung

- Test: 450/450 EditMode qua (chạy sau lần sửa cuối trong Assets). Không thêm test: đổi cỡ chữ và khoảng cách, không đổi hành vi; kiểm bằng mắt trên ảnh hai thứ tiếng.
- Commit `3cebd43`, đã push. Issue #24 đã comment, nhãn chờ-PO-duyệt. Còn "soi tràn chữ tiếng Anh với chuỗi dài nhất" thuộc #33.
- 40 chỗ `AddText` trong `GardenScreen`, `ShellView`, `FeelLayer` đổi từ số cứng sang `UiKit.TextSmall/Body/Title` (20–23 → 22; 24–28 → 26; 30–36 → 36); cỡ hiển thị lớn (38+: số ví, tiêu đề băng, tên game) giữ nguyên. Mọi danh sách (tấm trượt, bảng chọn đồ, chọn thỏ, thu nhập) dùng `UiKit.CardPad`/`CardGap`.
- Ảnh (docs/anh/po-24/): `24-truoc-10-pause.png`, `24-truoc-2-garden.png`, `24-sau-10-pause.png`, `24-sau-14-pause-en.png`, `24-sau-2-garden.png`, `24-sau-11-garden-en.png`.

### Đã làm (đối chiếu với mục "Còn lại" trong #24)
- Mỗi dòng một ý chính, số chính to, chip: đã làm trong #25 và #27 (không thuộc commit này).
- Áp thang chữ cho màn vườn, khung ngoài và các băng thông báo: xong như trên.
- Khoảng cách và bo góc thẻ dùng chung: các danh sách dùng hằng số chung; bo góc đi theo sprite `panel` dùng chung từ trước.
- Soi tràn chữ tiếng Anh: đã xem các khung vi/en sau khi đổi cỡ (liệt kê bên dưới); soi toàn bộ với chuỗi dài nhất để cho #33.

### Tồn đọng
- tồn đọng: #33 (soi tràn toàn bộ, chữ to tuỳ chọn).

## #28 — Bốn ý còn lại: thỏ nảy và trái tim khi chạm, cà rốt bay về ví, đạo cụ loé khi nâng, vòng sao qua mốc

- Test: 455/455 EditMode qua (chạy sau lần sửa cuối trong Assets). Thêm 3 test màn hình.
- Commit `33f9b5a`, đã push. Issue #28 để mở (còn: thỏ chớp mắt/vẫy tai (cần hình), thỏ đi tới trạm khi được xếp, cảnh nền đung đưa, thử tay), đã comment, nhãn chờ-PO-duyệt.
- `StationActorView.Flash` (ánh sáng sau trạm nở ra 0,3 giây), `GardenScreen.PlayPayout` (công khai để test và preview gọi). Không chuỗi mới.
- Ảnh (docs/anh/po-28/): `28-sau-bay-1.png`, `28-sau-bay-2.png`, `28-sau-bay-3.png` (ba khung cà rốt bay từ ấm trà về ví).

### Đã làm (đối chiếu với mục "Còn lại" trong #28)
- Kéo vườn có quán tính và nảy biên: đã có từ trước (ScrollRect Elastic, quán tính mặc định); không đổi.
- Thỏ nảy và hiện trái tim khi chạm: chạm trạm có thỏ thì thỏ nhảy một cái và ♥ nổi lên, về đúng chỗ cũ (test). Thở đã có; chớp mắt, vẫy tai cần khung hình vẽ (chưa).
- Cà rốt bay từ trạm về ô cà rốt, ô phồng khi nhận: mỗi lần trạm trả, hai cà rốt bay về ví, ví phồng khi chúng tới (test); con số chạy đã có từ trước; quá 12 thứ đang bay thì bỏ bớt để vườn đông không ngập.
- Nâng cấp: đạo cụ nảy (có từ trước) và ánh sáng loé; qua mốc ×2 có vòng 14 sao vàng bung ra quanh trạm (test).

### Chưa đạt so với #28
- Thỏ đi dạo đi thật theo lối và đi tới trạm khi được xếp; cảnh nền (tán cây, đèn lồng, mặt ao, đom đóm theo đàn); thử tay.

### Tồn đọng
- tồn đọng: các ý trên; "vòng sao" dùng chấm tròn màu vàng, chưa có hình ngôi sao.

## #22 — Ý còn lại: tấm chi tiết thỏ, bảng chọn đồ, chọn thỏ thành tấm trượt

- Test: 471/471 EditMode qua (chạy sau lần sửa cuối trong Assets). Thêm 1 test màn hình.
- Commit `bdf2647`, đã push. Issue #22 đã đủ các ý trừ thử tay; comment, nhãn chờ-PO-duyệt.
- File mới: `Assets/VuonTra/Game/UI/BottomSheet.cs` (tấm trượt dùng chung: trượt lên có nảy nhẹ, tay nắm, kéo xuống hoặc chạm ra ngoài để đóng, chỉ phủ tối nhẹ). `RabbitDetailView`, `ItemPickerView`, `WorkerPickerView` đặt nội dung lên tấm này; đường dẫn `Canvas/RabbitSheet/Card`, `Canvas/Picker/Card`, `Canvas/WorkerPicker/Card` giữ nguyên.
- Ảnh (docs/anh/po-22/): `22-truoc-8b-detail.png`, `22-truoc-9-items.png`, `22-sau-8b-detail.png`, `22-sau-9-items.png`, `22-sau-43-chon-tho.png`.

### Đã làm (đối chiếu với mục "Còn lại" trong #22)
- Tấm chi tiết thỏ, bảng chọn đồ, chọn thỏ cho trạm: trượt từ dưới lên (0,25 s, nảy nhẹ), có tay nắm, kéo xuống quá 30% hoặc hất nhanh thì đóng, kéo ít thì bật lại, chạm ra ngoài đóng; phủ tối nhẹ (18%) thay vì scrim đậm (test).
- Tấm đổi tên giữ ở giữa-trên màn hình vì bàn phím điện thoại che phần dưới.

### Tồn đọng
- tồn đọng: thử tay kéo tay nắm trên bản Mac (chủ project).

## Bàn giao (22/11, phiên code dừng tại đây)

### Trạng thái
- Đã commit và push (PO làm): tới a011c73, gồm #24, #22 + hình bùa, #23, #28 phần cơ bản.
- CHƯA commit nhưng ĐÃ XONG và test xanh: #29 (mục "U1 — #29" ngay phía trên). 314/314 EditMode, chạy sau lần sửa cuối trong Assets. Mình không gỡ phần này vì nó hoàn chỉnh, không dở dang; PO commit nó là cây làm việc sạch. File của #29: `Assets/VuonTra/Game/Audio/Haptics.cs` (+ .meta, mới), `Sfx.cs`, `PressDip.cs`, `UiKit.cs`, `ShellView.cs`, `GardenScreen.cs`, `L10nStrings.cs` + bảng chuỗi đã seed, `PreviewShot.cs`, `GardenScreenTests.cs`.
- Bản Mac `Build/Mac/VuonTraTho.app` đã build từ cây làm việc này (gồm cả #29). Mở chạy được, không exception. Chưa ai bấm thử hay nghe tiếng.
- Không có việc nào đang làm dở. #25 và #27 mới chỉ đọc đề, chưa sửa dòng code nào.

### Chưa đụng tới
- #25 (tab Trạm), #27 (tab Nhiệm vụ).
- Phần còn lại của #22 (vuốt xuống ở đâu cũng đóng; kéo/chạm vườn khi tấm mở; các bảng giữa màn chuyển sang tấm trượt), #23 (tab không chọn bớt nhạt; hạ tấm trạm sát thanh tab bằng cách giảm `TabCover`; huy hiệu cho tab Thỏ và Trạm), #24 (thang chữ cho GardenScreen/Shell/Feel; số chính + chip), #28 (thỏ, phần thưởng bay về HUD, cảnh nền), #29 (rung trên máy thật; lắc cho nút Lên cấp thỏ và Gọi thỏ).
- #36, R7, #37, R8, R9, R10, B1–B5, U2 (#26, #31, #32), U3 (#30).

### Lưu ý cho người làm tiếp
- UNITY=`/Applications/Unity/Hub/Editor/6000.6.10f1/Unity.app/Contents/MacOS/Unity`. Đóng Unity Editor trước khi chạy lệnh batch.
- Test: `"$UNITY" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults "$PWD/TestResults.xml" -logFile "$PWD/Logs/test.log"` (không thêm `-quit`). Lỗi biên dịch: `grep "error CS" Logs/test.log`. Test luôn là việc cuối cùng đụng tới Assets.
- Seed nội dung và bảng chuỗi (bắt buộc sau khi sửa `L10nStrings.cs` hoặc dữ liệu): `"$UNITY" -batchmode -projectPath . -executeMethod VuonTra.EditorTools.ContentSeeder.Seed -quit -logFile "$PWD/Logs/seed.log"`.
- Chụp ảnh: `-executeMethod VuonTra.EditorTools.PreviewShot.Render -quit` → `Logs/preview/*.png`; chuyển động thì chụp nhiều khung liên tiếp bằng `Step` + `Capture` trong `PreviewShot.cs`.
- Build Mac: `pkill -f "VuonTraTho.app/Contents/MacOS"` rồi `-executeMethod VuonTra.EditorTools.BuildScript.BuildMac -quit`.
- Không commit `Assets/AddressableAssetsData/*`, `.agent/`, `.agents/`, `.github/`, `skills-lock.json`, `Build/`.
- Trong test, `Motion` phải viết alias `using Motion = VuonTra.Game.UI.Motion;` vì trùng tên `UnityEngine.Motion`.
- Nguyên tắc từ #35: mọi chuyển động đi giữa các giá trị cố định, không lấy trạng thái đang động làm gốc.
- Player.log của bản Mac có một dòng "Curl error 35" lúc khởi động; chưa rõ nguồn, code game không gọi mạng.

## Bàn giao (22/11 05:40, phiên đêm không người trực dừng tại đây)

### Trạng thái
- main = origin/main, cây làm việc sạch trừ phần Unity tự sinh (`Assets/AddressableAssetsData/*`, `.agent/`, `.agents/`, `.github/`, `skills-lock.json`, để nguyên như dặn).
- Test: 369/369 EditMode qua (mốc đầu đêm 314). Mỗi commit tính năng đều chạy test xanh trước khi commit.
- Bản Mac `Build/Mac/VuonTraTho.app` build lại lúc 05:33 từ đúng HEAD (`1830282` phần code), "Build Finished, Result: Success". Mở lên chạy 12 giây, Player.log không có exception. Chưa ai bấm thử.
- Save lên v10 (v9: `buyMode` -1 và `spendShare`; v10: khay sản phẩm, `photoDay`). Save cũ đọc được, test có.

### Issue đã xong và đã đóng (theo thứ tự trong #16)
- #43 Trạm trống không hiện thỏ: `738da68`. Thỏ chỉ hiện khi có thỏ của người chơi; tấm đệm "+" mở danh sách chọn thỏ; ô chưa xây có biển giá; người mới được tặng Thỏ Bông ở ấm trà; huy hiệu tab Thỏ.
- #25 Tab Trạm: `9bdf90f`. Thu nhập mỗi giây là số chính, "Còn N cấp tới ×2", hình các trạm được tiếp sức, giữ tay nâng liên tục nhanh dần.
- #27 Tab Nhiệm vụ: `1307309`. Nhận được lên đầu + Nhận tất cả, chưa xong không có nút, chip thưởng có hình (có hình cỏ bốn lá mới), đồng hồ tới lúc làm mới, thưởng bay về ví.
- #36 Nâng cấp nhanh: `d7e6324` + `6591a5b`. Tới mốc, công tắc trên tấm trạm, Nâng tất cả có tóm tắt, giữ lại 100/50/25%, thỏ Tối đa, số cấp chạy lên.

### Issue làm một phần, để mở
- #37 Khay sản phẩm: `5178cd2` + `7296124`. Luật và khay xong, chỉnh nhịp cho người nhặt hết được thêm ~20%. Còn vẽ 7 món và khung khay (`Art/Final/product_<id>.png`, catalog đã chừa chỗ).
- #18 Chụp ảnh (bản Mac): `1830282`. Nút máy ảnh, ẩn HUD, chụp ra PNG vào ~/Pictures/VuonTraTho, xem lại, Mở thư mục, Chép ảnh, quà ngày +1 cỏ. Còn khung ảnh, công tắc tên thỏ/đom đóm, album 12 ảnh, nhiệm vụ ăn theo, mobile.

### Bỏ qua và vì sao
- "Cà rốt vàng và Tiệm": chưa có issue và cần chủ project quyết giá, nguồn cà rốt vàng, có bán tiền thật không. Đã tạo #44 ghi đủ các phần và câu hỏi.
- "Trò chơi nhỏ đầu tiên: Giã bánh trăng": chưa có mô tả cách chơi. Đã tạo #45 với các câu hỏi và một gợi ý để chọn nhanh.
- #17 (hồ sơ người chơi, phần làm được ngay): hết giờ, chưa đụng.
- Các mục sau trong #16 (thang độ hiếm, mốc trạm 25/100/300, sao thỏ, trần cấp 500, màn lúc đi vắng, lượt UI 2, hướng dẫn 60 giây): chưa đụng.

### Cần chủ project thử tay hoặc quyết
- Bấm thử trong bản Mac: tấm đệm "Xếp thỏ +" và danh sách chọn thỏ (#43); giữ nút nâng xem nhịp lặp có dễ chịu không (#25, hằng trong `PressDip`); Nhận tất cả và thưởng bay (#27); công tắc Tới mốc / Tối đa và bảng Nâng tất cả (#36); chạm và vuốt khay (#37); nút máy ảnh, "Mở thư mục", "Chép ảnh" (#18, hai nút này gọi lệnh hệ thống, chỉ chạy được trong bản Mac).
- Quyết: #44 (cà rốt vàng và Tiệm), #45 (Giã bánh trăng), và có chấp nhận cọc biển nhỏ dưới sương ở đất chưa mở rộng không (#43, lệch so với "chỉ sương và cây").
- Hình vẽ tay thay giữ chỗ: `plot_sign.png`, `pad.png` (#43), `icon_clover.png` (#27), `product_<id>.png` ×7 và khung khay (#37). Bỏ vào `Art/Final/` rồi seed.

### Lưu ý kỹ thuật mới
- Trong test EditMode, `GameRunner.StateChanged` không nối tới màn hình (nối trong `Start()`, không chạy ở edit mode): sau khi đổi state phải gọi `screen.Refresh()` tay trước khi đọc nhãn.
- Nút chunky giờ đi `Button.onClick → PressDip.Fire → Pressed`; test gọi `onClick.Invoke()` vẫn chạy. Giữ tay lặp: `PressDip.Repeat`.
- `FeelLayer.Fly(from, to, sprite, ...)` dùng chung cho mọi thứ bay (thưởng, món, cà rốt bán); hai đầu cố định lúc bắt đầu.
- `docs/issues-po.md` là bản chép do session PO giữ, đêm nay không sửa; GitHub là nguồn đúng (đóng 25, 27, 36, 43; mới 44, 45).
- Lệnh test/seed/chụp/build như mục Bàn giao trước.

## #17 — Bổ sung: quà liên kết tài khoản và lớp lưu mây "chỉ trên máy"

- Test: 425/425 EditMode qua (verify.sh của session kia chạy trên cây gộp). Thêm 4 test core (`ProfileTests.cs`) và 1 test màn hình.
- Commit `57b1541` (gộp với #31 phần 2 của session "Issue và kiểm thử Unity", vì hunk hai bên xen kẽ trong `SaveCodec.cs`, `GardenScreen.cs`, `GardenScreenTests.cs`). Issue #17 đã comment, vẫn mở cho phần cần chủ project.
- Bảng chuỗi seed lại (`backup.reward`, `backup.rewarded`). Save vẫn v11, khối `profile` thêm `linkRewarded`.

### Đã làm (đối chiếu với nguyên văn #17)
- `Profile.OnLinked(s)`: tặng `LinkRewardClovers` (3) đúng một lần cho mỗi vườn; `GameRunner.LinkAccount(onDone(ok, gift))` gọi `Account.Link` rồi trả quà; `GardenScreen` hiện "+3 cỏ bốn lá vì đã liên kết"; `ProfileView` ghi quà sắp có dưới nút Liên kết (ẩn khi đã nhận hoặc đã liên kết).
- `ICloudSave` + `LocalOnlyCloudSave`, `GameRunner.Cloud`; `SaveSummary.Of`, `SaveSync.Choose` (Same / KeepLocal / KeepRemote / Ask) để #38 và màn hỏi người chơi dùng chung một luật.

### Chưa đạt so với #17
- Như trước: phần Unity Cloud / nhà cung cấp / chính sách cần chủ project; đồng bộ thật thuộc #38.
- Chưa có màn "giữ bản nào" vì chưa có bản thứ hai để so; luật đã sẵn.

### Tồn đọng
- tồn đọng: số cỏ thưởng liên kết (3) là mình chọn, chủ project duyệt.

## #18 — Phần còn lại của bản Mac: khung ảnh, công tắc, album, nhiệm vụ

- Test: 436/436 EditMode qua (verify.sh: seed, 78 ảnh, biên dịch, test). Thêm 3 test core (`PhotosTests.cs`) và 3 test màn hình.
- Commit `8ba94fd`, đã push. Issue #18 đã comment; vẫn mở cho quà cà rốt vàng (#44) và phần mobile.
- Ba quest asset mới `Content/Quests/anh-*.asset` (seed); `QuestMetric` thêm PhotosTaken, PhotosWithGoldRabbit, PhotosWithFullRest; save thêm `photosGold`, `photosRest`, `album` (vẫn v12). Bảng chuỗi seed lại (3 key quest, 8 key photo.*).
- Ảnh (docs/anh/po-18/): `18-sau-khung-anh.png`, `18-sau-xem-co-khung.png`, `18-sau-album.png`.

### Đã làm (đối chiếu với nguyên văn #18)
- Khung: lớp `PhotoFrame` trong canvas (dưới thanh chụp, trên vườn): viền giấy kem, đường mực, tên vườn, ngày, logo chữ. Ảnh chụp mang khung vì chụp qua camera của game.
- Công tắc: `GardenScreen.PhotoFrameOn/PhotoNamesOn/PhotoFirefliesOn` (PlayerPrefs); `ApplyPhotoOptions` tắt lớp "Fireflies" và nhãn "Tier" của thỏ ở trạm trong chế độ chụp.
- Album: `Photos.Take(s, day, path)` giữ 12 đường dẫn mới nhất; `GardenScreen.OpenAlbum` đọc file bằng `Texture2D.LoadImage`, `Photos.Tidy` bỏ file mất; chạm mở tấm xem lại.
- Nhiệm vụ: `Photos.Take` đếm ảnh có thỏ bậc ≥ Vàng (`Photos.GoldTier` = 4) và ảnh khi `RestSpots == Max`; ba thành tựu trong `MetaSeeder`.

### Chưa đạt so với #18
- Quà cà rốt vàng (#44); phần mobile (#41).

### Tồn đọng
- tồn đọng: album đọc lại file mỗi lần mở (12 ảnh, nhanh); nếu sau này album dài hơn thì nên cache thumbnail.

## #30 — Hướng dẫn 60 giây đầu

- Test: 450/450 EditMode qua (verify.sh: seed, 81 ảnh, biên dịch, test). Thêm 5 test core (`TutorialTests.cs`, file mới) và 3 test màn hình.
- Commit `c7c629d`, đã push. Issue #30 đã comment (bảng đối chiếu), để chủ project duyệt.
- File mới: `Core/Tutorial.cs`, `UI/TutorialView.cs`, hình giữ chỗ `icon_hand.png` (catalog `Hand`). Save thêm `tutorial`, `tutorialClover` (vẫn v12). Bảng chuỗi seed lại (12 key `tut.*`).
- Ảnh (docs/anh/po-30/): `30-buoc-1.png` (bàn tay chỉ ấm trà, lỗ mờ), `30-buoc-2.png` (nút Nâng trên tấm), `30-buoc-3.png` (nút Mở trạm trong tab).

### Đã làm (đối chiếu với nguyên văn #30)
- `Tutorial.Met/Advance/Skip/GrantCallClover`: sáu bước đọc từ state; `GameRunner.Changed` và `Tap` gọi `Advance`; `SkipTutorial`, `GrantTutorialClover`.
- `TutorialView`: dòng trên đỉnh ("1/6 · …", Bỏ qua), bàn tay bám mục tiêu (`GetWorldCorners` → lỗ bốn tấm), không tấm nào bắt chạm. `GardenScreen.UpdateGuide/GuideTarget` chọn mục tiêu theo bước và theo tấm đang mở.
- Thứ tự bước 4 và 5 đổi so với nguyên văn (nhận nhiệm vụ trước, gọi thỏ sau) và bước gọi tặng 1 cỏ khi ví trống: ghi trên issue để chủ project duyệt.

### Chưa đạt so với #30
- Không có: mọi ý đều có mặt; hai điểm lệch (thứ tự, cỏ tặng) chờ duyệt.

### Tồn đọng
- tồn đọng: trong batch preview, nhãn "Bỏ qua" dựng lúc màn hình đang tiếng Anh nên khung 30-buoc hiện "Skip" (quirk đã ghi ở mục Bàn giao của session kia); trong game không ảnh hưởng.

## #14 — Màu Thỏ Việt Quất và Thỏ Dâu Tây

- Commit `aae36e3`, đã push; verify.sh xanh 452/452 (chung lượt với #13). Issue đã comment, nhãn chờ-PO-duyệt.
- Nguyên nhân: xoay màu toàn ảnh cho thỏ Khoai Môn làm lông kem và má đổi theo. Script mới `docs/anh/po-loai-tho/30-bien-the/sua-14-viet-quat-dau-tay.py` chỉ đổi vùng tím của tai và lưng, nén dải màu quanh màu đích. Hai file trong `Art/Final` và bản sao trong docs đã thay; `Art/Generated` tự sinh lại khi seed.

## #13 — Khoảnh khắc gọi thỏ: tiếng theo bậc, dấu Mới lưu

- Test: 452/452 EditMode qua (verify.sh). Thêm 1 test core, 1 test màn hình.
- Commit `38ef8b7`, đã push. Issue đã comment, nhãn chờ-PO-duyệt.
- `Sfx.Summon(tier)`: sáu clip (2 + bậc nốt, mỗi bậc cao hơn một cung, Vàng/Đỏ dày hơn); `OnSummon` và `OnMerge` dùng nó thay `Sting`.
- `GameState.FreshKinds` (save `freshKinds`, chỉ giữ loài có trong sổ); `Rabbits.See` thêm, `Rabbits.LookAt` bỏ; `GameRunner.LookAtKind`; `GardenScreen.IsFreshKind` đọc từ state. Sửa `SaveCodec` không gọi `See` khi đọc sổ (nếu không mọi loài thành mới sau khi mở lại).

### Chưa đạt
- Tiếng chưa ai nghe (mình không nghe được); cần chủ project nghe trong bản Mac.

## #21 — Ngày và đêm theo giờ thật (phần hình ảnh)

- Test: 470/470 EditMode qua (verify.sh: seed, 86 ảnh, biên dịch, test). Thêm 3 test core (`DaylightTests.cs`, file mới), 1 test màn hình.
- Commit `79a6558`, đã push. Issue #21 đã comment, nhãn chờ-PO-duyệt. #5 chỉ comment (cần bundle gốc macOS, để ưu tiên thấp như issue ghi).
- File mới: `Core/Daylight.cs` (PhaseAt, NightAt với sàn 0,35, FirefliesAt). `GardenScreen.ClockHours` (đồng hồ thay được), `UpdateDaylight` gọi trong `Refresh`: lớp "Daylight" dưới HUD, quầng trăng mờ dần, số đom đóm. PreviewShot đặt giờ 22 cho mọi khung, thêm `21-ban-ngay`, `21-hoang-hon`.
- Ảnh (docs/anh/po-21/): `21-dem.png`, `21-hoang-hon.png`, `21-ban-ngay.png`.

### Chưa đạt
- Thỏ hiếm ghé tiệm ban đêm: chờ #44. Độ đậm lớp phủ chưa ai xem bằng mắt vào ban ngày thật.

## #32 — Ý cuối: viền sáng khi tăng tốc (và kiểm #29)

- Test: 472/472 EditMode qua (verify.sh). Thêm 1 test màn hình.
- Commit `6d65055`, đã push. #32 đã comment (đủ bốn ý, chờ duyệt). #29: comment xác nhận hai thanh âm lượng đã có sẵn, không sửa.
- `GardenScreen`: lớp "BoostEdge" (bốn dải `UiKit.FadeUp` màu rêu, hai dải dọc xoay ±90°), `UpdateBoostEdge` trong `Advance`: lên/xuống 0,4 giây, thở khi chạy, tắt thở khi Giảm chuyển động.
- Ảnh (docs/anh/po-32/): `32-sau-vien-sang.png`.

## #44 lượt 1 — Cà rốt vàng và Tiệm (quà ngày, Gọi vàng)

- Test: 481/481 EditMode qua (verify.sh: seed, 87 ảnh, biên dịch, test; GardenPacingTests vẫn qua). Thêm 7 test core (`ShopTests.cs`, file mới), 2 test màn hình.
- Commit `4fc84eb`, đã push. Issue #44 đã comment (bảng đối chiếu lượt 1), nhãn chờ-PO-duyệt. Save lên v13.
- File mới: `Core/Shop.cs`, `UI/ShopView.cs`; Art/Final thêm `prop_tiem.png`, `icon_ca-rot-vang.png`, `icon_hop-qua.png` (từ docs/anh/po-do-vat); catalog `GoldenCarrot`, `GiftBox`, `ShopProp`; 5 quest asset mới (loai-tim, loai-vang, loai-do, dat-12, tho-max). Bảng chuỗi seed lại (~22 key).
- `Rabbits.Arrive` tách từ `Summon` để hai cách gọi chung một đường; `Rabbits.RollTier(roll, odds)`. `Tray.Sell/SellAll` giờ trả `golden`; `Photos.GiftGolden`; `Quests.RewardGolden` + ba metric mới; `Garden.AddXp` cộng cà rốt vàng mỗi 5 cấp.
- `GardenScreen`: quầy "Shop" trong `_street` cạnh ao (thêm vào `standing` để xếp lớp), chip "Hud/Golden", `OpenShop/OnDailyGift/OnSummonGolden`, `PaySale` bay cà rốt vàng về chip. `GameRunner.TodayNumber`, `GiftWaiting`, `ClaimDailyGift`, `SummonGoldenRabbit`.
- Ảnh (docs/anh/po-44/): `44-sau-tiem.png`, `44-sau-quay-tiem-mep-ao.png`.

### Chưa đạt
- Lượt 2 (thỏ ghé tiệm) và lượt 3 (mũ, nhuộm) chưa làm. Chưa thử tay.

## #45 — Giã bánh trăng: trò chơi nhỏ đầu tiên

- Test: 507/507 EditMode qua (chạy sau lần sửa cuối trong Assets). Thêm 25 test luật (`PoundingTests.cs`) và 1 test màn hình.
- Commit `7ce83c9`, đã push. Issue #45 để mở, đã comment đối chiếu từng ý đã duyệt, nhãn chờ-PO-duyệt. Save v14 (khối `pounding`); save cũ mở ra còn đủ 3 lượt, 0 ván.
- Luật trong `Core/Pounding.cs`: lịch nhịp là hàm của chỉ số nhịp (1 giây, mỗi 5 nhịp nhanh thêm 10 %, ván ~25 giây), chấm Chuẩn ±80 ms / Được ±160 ms / Méo, chuỗi Dẻo sau 5 Chuẩn (+5, rồi 4 điểm mỗi nhịp, Được giữ Dẻo, Méo đứt), tối đa 120, sao 40/75/105, 3 lượt/ngày làm mới theo ngày (lùi đồng hồ không thêm lượt), mở ở cấp vườn 3, thưởng = thu nhập/giây × hệ số × 60 × điểm/120, thỏ Bếp bánh cấp cao nhất đang làm giã cùng ×1,5, lượt có sao đầu ngày +1 cỏ, ván 3 sao đầu ngày +1 cà rốt vàng.
- Màn hình `PoundingView`: cối đứng giữa, vòng thu nhỏ về bánh theo nhịp, chạm đâu cũng tính, cối nén xuống khi giã, chữ Chuẩn/Được/Méo, pill "Dẻo!", bánh sạm khi Méo, rung ngang 0,25 giây khi Méo (tắt khi Giảm chuyển động), tấm kết quả sao/điểm/cà rốt/cỏ/vàng/thỏ giã cùng. Cối trong vườn (`Canvas/Garden/Content/Mortar`) cạnh ấm trà, biển "Giã bánh · còn N lượt" / "Mai giã tiếp".
- Hai thành tựu mới: `gia-3sao` (Giã bánh 3 sao lần đầu), `gia-30` (Giã 30 ván). Hình: `prop_coi-gia-banh.png` đã có; chày tách, bánh ba trạng thái, bánh méo chưa vẽ (cối hiện dùng hình có sẵn chày, bánh là đĩa vàng).
- Ảnh (docs/anh/po-45/): `45-vong-chay.png` (vòng đang khép về bánh), `45-deo.png` (sau 6 Chuẩn, Dẻo!), `45-ket-qua.png` (tấm kết quả). Chuỗi en ngắn hơn vi, cùng khung.

### Đã làm (đối chiếu với đề xuất đã duyệt trong #45)
- Bấm theo nhịp, ván 25 giây 30 nhịp, vòng thu nhỏ về bánh, nhanh dần, Chuẩn/Được/Méo, Dẻo!, 120 điểm, 3 sao: đủ, có test.
- Cối cạnh ấm trà, hiện từ cấp vườn 3, chạm cối để chơi, không nút HUD: đủ.
- 3 lượt/ngày, thưởng 60 giây thu nhập × điểm/120, cỏ cho lượt có sao đầu ngày, cà rốt vàng cho ván 3 sao đầu ngày: đủ, có test.
- Thỏ Bếp bánh cấp cao nhất đang làm giã cùng ×1,5, hiện bên cối: đủ, có test.
- Tắt rung khi Giảm chuyển động: đủ, có test.

### Chưa đạt so với #45
- Hình chày tách khỏi cối, bánh ba trạng thái và bánh méo (cần vẽ). Thử tay nhịp có cảm giác đúng chưa (cửa sổ ±80/±160 ms là số đề xuất).

## #44 lượt 2 — Thỏ ghé tiệm

- Test: 511/511 EditMode qua (verify.sh: seed, 90 ảnh, biên dịch, test). Thêm 3 test luật (`ShopTests.cs`), 1 test màn hình.
- Commit `5119c0a`, đã push. Issue #44 đã comment (bảng đối chiếu lượt 2), nhãn chờ-PO-duyệt. Save không đổi phiên bản, thêm `visitorBought`, `visitorSeen` (save cũ mở ra: chưa mua, chưa xem).
- Luật trong `Core/Shop.cs`: `VisitorFor(state, ngày)` băm ngày và mã người chơi nên cùng ngày luôn ra cùng một bé; bậc 60/30/9/1 từ Xanh dương lên Đỏ; `PickKind` chọn loài trong bậc, loài chưa có trong sổ nặng ×3; giá 15/40/120/400; `BuyVisitor` chỉ mua đúng ngày, một lần, trả thỏ qua `Rabbits.ArriveKind` (mới, `Arrive` giờ gọi qua nó).
- Tấm Tiệm cao 1230: mục "Thỏ ghé tiệm hôm nay" giữa quà ngày và Gọi vàng, có chân dung (dùng `RabbitGridView.Portrait`), tên loài, bậc với sao, nghề, giờ còn lại, nhãn "Loài chưa gặp!", nút Mua / Còn thiếu / Đã về vườn. Huy hiệu "!" trên quầy khi có quà chưa nhận hoặc thỏ hôm nay chưa xem; mở tiệm là xem (`GameRunner.MarkVisitorSeen`). Mua xong bé ra cổng trăng như thỏ gọi bằng cỏ.
- Ảnh (docs/anh/po-44/): `44-sau-tiem-tho-ghe.png` (tiệm với thỏ ghé, 12 cà rốt vàng, còn thiếu 3).

### Chưa đạt
- Lượt 3 (mũ, nhuộm) chờ 6 hình vẽ. Chưa thử tay trên máy thật.

## #28 — Cảnh nền đung đưa, thỏ đi thật

- Test: 514/514 EditMode qua (chạy sau lần sửa cuối trong Assets). Thêm 3 test màn hình; 4 test cũ chờ thỏ đi tới trạm rồi mới soi.
- Commit `fcf3f64`, đã push. Issue #28 để mở (còn chớp mắt/vẫy tai cần hình, thử tay), đã comment, nhãn chờ-PO-duyệt.
- Cảnh nền (`GardenScreen.UpdateScenery`): cây nghiêng từ gốc ±1,6° chậm (có thêm run nhỏ ±0,4°), đèn lồng treo từ móc lắc ±3° và ánh đèn chập chờn 72–100 %, mặt ao thở nhẹ và hai vòng sáng loang ra rồi tan mỗi 2,8 giây, đom đóm bay theo 6 đàn, mỗi đàn trôi từ đèn này sang đèn kế rồi về trong 26 giây. Bật Giảm chuyển động thì tất cả đứng yên (test).
- Thỏ đi dạo: từ ghế đi ra đầu lối, dọc lối đi về, rồi về ghế, cùng một tốc độ suốt đường (`StrollPoint`); chu kỳ 48 giây, nửa đầu đi dạo. Không còn hiện và biến tức thì (test: lúc bắt đầu đứng đúng chỗ ghế, 2 giây sau đã đi xa nhưng không nhảy cóc).
- Thỏ được xếp vào trạm: đi bộ từ ghế (hoặc trạm cũ) tới chỗ đứng ở trạm (`Traveler`, 300 px/giây, 0,4–2,4 giây), nhún theo bước, tới nơi thì hiện ra và nhảy một cái; trong lúc đi trạm không hiện pad lẫn thỏ. Giảm chuyển động: có mặt ngay (test). `StationActorView.SetAway`.
- Ảnh (docs/anh/po-28/): `28-tho-di-tram.png` (thỏ tím đang đi từ ghế lên ấm trà, vẽ dưới các biển cấp).

### Đã làm (đối chiếu với mục "Còn lại" trong #28)
- Thỏ đi dạo đi thật theo lối từ chỗ nghỉ ra rồi quay về, nhún theo nhịp bước: đủ.
- Thỏ được xếp thì đi tới trạm: đủ.
- Tán cây đung đưa chậm, đèn lồng lắc và chập chờn, mặt ao gợn, đom đóm bay theo đàn: đủ (vòng gợn dùng ảnh ánh sáng có sẵn, chưa có hình gợn nước riêng).

### Chưa đạt so với #28
- Thỏ chớp mắt, vẫy tai (cần khung hình vẽ); hình ngôi sao riêng cho vòng sao; thử tay trên máy thật.

## #33 — Dễ đọc và dễ dùng: chữ to, vùng chạm, vùng an toàn, tương phản

- Test: 522/522 EditMode qua (verify.sh: seed, 92 ảnh, biên dịch, test). File mới `AccessibilityTests.cs` (10 test: chữ to, vùng an toàn, vùng chạm, tràn chữ vi/en × thường/chữ to, tương phản WCAG).
- Commit `d48ede5`, đã push. Issue #33 đã comment (bảng đối chiếu 6 ý), nhãn chờ-PO-duyệt.
- Chữ to: `UiKit.LargeText` (PlayerPrefs `text.large`), `UiKit.TextScale` 1,2 nhân vào mọi `AddText`; công tắc "Chữ to" trên thẻ Tạm nghỉ (thẻ cao 1290), `UiKit.TextChanged` → `GardenScreen.RebuildUi` như đổi ngôn ngữ. Pill chế độ mua, biển "Thêm thỏ", nhãn Trà lực, nhãn "Loài chưa gặp" nới theo (best fit).
- Vùng chạm: `UiKit.MinTouch = 88`; `UiKit.Touchable(rt, w, h)` đặt tấm bắt chạm ẩn cho pill nằm trong dải chật. 40 nút dưới mức đã sửa (đóng, nút phụ, pill chế độ / ảnh / tỷ lệ / ngôn ngữ / bỏ qua / mép cuộn, tab con Thỏ).
- Vùng an toàn: `GardenScreen.SafeInsets` (dưới, trên) thay `SafeBottom`; HUD hạ theo tai thỏ trong `PlaceTabBar`.
- Tương phản: `Palette.InkSoft` 64 % → 70 % mực (4,4:1 → 5,2:1 trên thẻ). Chữ kem trên nút cam 2,0:1 để PO quyết (mực trên cam được 6,2:1).
- Ảnh (docs/anh/po-33/): `33-truoc-tam-nghi.png`, `33-sau-tam-nghi.png`, `33-sau-chu-to.png`, `33-sau-vuon.png`. PreviewShot thêm khung `33-chu-to`.

### Chưa đạt
- Màu chữ trên nút chính (chờ PO). Chưa thử tay trên máy thật, chưa đo trên máy có tai thỏ thật.

## #34 — Mặt đất có lớp lang, bóng đổ, tán cây trượt chậm, vệt trăng, trang trí có chủ đích

- Test: 526/526 EditMode qua (chạy sau lần sửa cuối trong Assets). Thêm 4 test màn hình.
- Commit `ab2aab2`, đã push. Issue #34 để mở (còn hình gợn nước riêng, thử tay), đã comment đối chiếu từng ý, nhãn chờ-PO-duyệt.
- Lớp đất (`GardenScreen.BuildGroundLayers`, vẽ trước mọi thứ đứng): dải đất mòn dưới các viên đá bước của mỗi lối; vòng 14 phiến đá lát quanh ấm trà và 6 phiến bên trong; thảm hoa nhỏ ở 6 chỗ cỏ thoáng nhất (quét lưới, chọn chỗ xa trạm và ghế nhất, cách nhau ≥ 260); vệt trăng chéo (`bg_moonbeam`) đổ xuống ấm trà và vũng sáng dưới chân, thở nhẹ (đứng yên khi Giảm chuyển động).
- Bóng đổ mềm (`Shade`, 28 % đêm) dưới mọi cây, bụi, đèn lồng, nấm, gốc, bàn trà, bồn hoa, đèn đá; vẽ ngay sau vật theo cùng thứ tự xa gần (test).
- Tán cây viền (hàng trên, hàng dưới, hai cột bên) nằm trên lớp `Canopy` riêng, trượt bằng 0,9 lần mặt đất tính từ góc nhìn nghỉ ở ấm trà (test đo đúng 0,9). Hàng trên hạ 90 để thấy tán chứ không chỉ thân.
- Trang trí có chủ đích: mỗi trạm thử lần lượt 12 chỗ ưu tiên rồi một vành 200–320 quanh trạm, lấy chỗ đầu tiên không đè prop/pad/biển cấp/tên của trạm nào, ghế, ao, cối, tiệm: bồn hoa (`decor_bon-hoa`), nấm, đá; hai đèn đá (`decor_den-da`) ở góc nghỉ. Rải ngẫu nhiên còn 24 thay vì 60.
- Cối giã dời lên trên tên ấm trà (`Heart + (-10, 300)`), không còn đè biển cấp của giỏ lá (test).
- Ảnh (docs/anh/po-34/): `34-nen-tam.png` (quanh ấm trà: đá lát, vũng trăng, thảm hoa, bồn hoa), `34-nen-tren.png` (đầu vườn: tán cây, bóng, vệt trăng, bồn hoa cạnh tiệm), `34-vuon.png` (khung vườn chung).

### Đã làm (đối chiếu với #34)
- Mặt đất có lớp lang: cỏ, lối mòn đất, thảm hoa, đá lát quanh ấm trà: đủ.
- Vật ở dưới che vật ở trên theo thứ tự xa gần; bóng đổ mềm dưới thỏ và đồ vật: đủ (thỏ đã có bóng từ trước; đồ vật thêm bóng).
- Tán cây viền quanh trượt chậm hơn mặt đất khi kéo: đủ (0,9).
- Vệt sáng trăng chiếu xuống ấm trà: đủ.
- Trang trí rải có chủ đích quanh từng cụm trạm; dùng đèn đá, bồn hoa trong po-do-vat: đủ.

### Chưa đạt so với #34
- Chưa có hình gợn nước riêng (vòng gợn dùng ảnh ánh sáng); thử tay cảm giác chiều sâu khi kéo trên máy thật.

## #19 — Chạm và vuốt thỏ, mức thân thiết (agent song song, nhánh agent-19)

- Test: 559/559 EditMode qua trên main sau khi gộp (verify.sh: seed, 98 ảnh, biên dịch, test). Nhánh riêng 533/533: +5 test luật (`RabbitsTests.cs`), +1 save, +5 màn hình.
- Commit `5a76632` (nhánh agent-19), gộp vào main qua `4a48d81`…`23cf0c1`, đã push. Issue #19 đã comment (bảng đối chiếu 8 ý), nhãn chờ-PO-duyệt. Save không đổi phiên bản, mỗi thỏ thêm `affection`, `petDay`.
- Luật trong `Core/Rabbits.cs`: `Rabbit.Affection` 0–5, `PetDay`; `Rabbits.Pet(s, r, ngày, wake)`: lần vuốt đầu mỗi ngày +1 tim và +5 Trà lực (`PetEnergy`), đánh thức thỏ gật trả 8 (`DozeEnergy`, dời từ GardenScreen) và tính là lần vuốt của ngày; tới 3 tim mở câu nói (`AffectionLine`), tới 5 tim tặng 1 cỏ đúng một lần (`AffectionGiftClovers`); ghép ba giữ mức cao nhất; `Rabbits.Hearts(n)`.
- Màn hình: `UiKit.Strokable` (kéo qua hình thỏ = một lần vuốt, vẫn cuộn vườn, nhấc tay không tính chạm); chạm/vuốt thỏ ở trạm, chỗ nghỉ (`Seat n/Art`) và đang đi dạo (`Walker n/Art`); tim bay lên, "+5 trà lực"; thỏ ngáp vươn người 0,6 s (`Motion.Yawn`) trước khi gật, tắt khi Giảm chuyển động; bong bóng câu nói `Canvas/Garden/Content/BondLine` 3 s (`Motion.Speech`), câu theo nghề `bond.line.<nghề>`; tấm khoảnh khắc tặng cỏ. Ô tim trên thẻ thỏ (`RabbitSheet/Card/Body/Bond`) và ô thỏ ("Cấp 3 ☆ ♥♥").
- Ảnh (docs/anh/po-19/): `19-sau-vuot-tho.png`, `19-sau-tim-the.png`.

### Chưa đạt
- Cảnh thỏ ngồi uống trà (cần tranh). Mới 1 câu nói mỗi nghề; quà chỉ là cỏ bốn lá. Chưa thử tay trên máy thật.

## #10 phần 1 — Hứng lá trà (agent song song, nhánh agent-10a)

- Test: 559/559 EditMode qua trên main sau khi gộp. Nhánh riêng 544/544: +21 test luật (`LeavesTests.cs`), +1 màn hình.
- Commit `aa7215f` (nhánh agent-10a), gộp vào main qua `4a48d81`…`23cf0c1`, đã push. Issue #10 đã comment phần 1 (để mở chờ phần 2). Save vẫn v14, thêm khối `leaves` sau `pounding`; save cũ mở ra đủ 3 lượt.
- Luật trong `Core/Leaves.cs`: ván 30 giây, 48 vật rơi theo lịch từ seed (ngày × 1000003 + số ván): 36 lá (+1), 4 lá vàng (+2), 8 quả thông (−3, sàn 0, choáng 0,5 s); rơi 2,8 → 2,0 s nhanh dần; bề rộng hứng ±0,12; tối đa 44, sao 15/29/38; `MiniGameRecord` (khối lượt/ngày dùng chung cho từng trò: 3 lượt, làm mới theo ngày, lùi đồng hồ không thêm); mở ở cấp vườn 4; thưởng = thu nhập/giây × hệ số × 60 × điểm/44; lượt có sao đầu ngày +1 cỏ (cờ riêng); không cà rốt vàng.
- Màn hình `LeavesView` (`Canvas/Leaves`): tán cây, lá/quả thông đặt theo đồng hồ mỗi khung (bể 12 hình), giỏ kéo theo ngón hoặc chạm để trượt, +1 / +2 lá vàng! / Ui! −3, giỏ lắc sạm khi dính quả thông (tắt khi Giảm chuyển động), tấm kết quả. Giỏ trong vườn (`Canvas/Garden/Content/Basket`) cạnh trạm Giỏ lá trà, biển "Hứng lá · còn N lượt" / "Mai hứng tiếp". Thành tựu `la-3sao`, `la-30`; `QuestMetric.LeavesThreeStar`, `LeavesGames` cuối enum.
- Ảnh (docs/anh/po-10/): `10-sau-hung-la.png`, `10-sau-hung-la-ket-qua.png`.

### Chưa đạt
- Lá, lá vàng, quả thông là hình tròn tô màu; tán cây và giỏ dùng hình sẵn có. Chưa thử tay. Phần 2 "Đêm đom đóm" đang làm trên nhánh agent-10b.

## #38 — Lưu mây: lõi đồng bộ save, không phụ thuộc nhà cung cấp

- Test: 578/578 EditMode qua (chạy sau lần sửa cuối trong Assets). Thêm 16 test lõi (`CloudSaveTests.cs`) và 3 test màn hình.
- Commit `d88b60c`, đã push. Issue #38 để mở (phần nhà cung cấp thật là #39/#40), đã comment đối chiếu từng ý, nhãn chờ-PO-duyệt.
- Lõi (`Core/CloudSave.cs`): bản trên mây là phong bì `{cloud, version, savedAt, level, rabbits, kinds, played, seen, code, check, save}`; `check` là FNV-1a trên phiên bản + thời điểm + nội dung; bản cắt cụt, sửa tay, không phải phong bì → Corrupt (bỏ qua, báo, ghi lại bản trên máy); phiên bản mới hơn → để yên, không ghi đè. `CloudSync` quyết bằng `SaveSync.Choose`: giống hệt → không làm gì; máy tiến xa hơn → đẩy lên; mây tiến xa hơn hoặc mỗi bên hơn một điểm → `NeedsChoice`, gợi ý bản cấp vườn cao hơn, rồi nhiều thỏ hơn, rồi mới hơn; không bao giờ tự thay bản trên máy. Lúc nào: mở game, ra nền, mốc lớn (thêm đất, thỏ Vàng trở lên, mua ở Tiệm), hẹn giờ tối đa 180 giây một lần; nâng cấp thường không đồng bộ (test). Mất mạng: Offline, bản trên máy nguyên, lần sau thử lại (test). `MemoryCloudSave` (mây trong bộ nhớ, bật/tắt mạng) cho test, preview và bản chưa có nhà cung cấp.
- Không mất save trên máy: `SaveStore.Backup/WriteBackup/ReadBackup` (`save.backup.json`); trước khi lấy bản trên mây, bản trên máy được chép sang sao lưu; "Khôi phục bản sao lưu trên máy" trong Hồ sơ đổi chỗ hai bản (không mất bản nào).
- Màn hình: tấm `CloudChoice` ("Hai bản vườn khác nhau") hai cột Trên máy / Trên mây với cấp vườn, số thỏ · loài, đã chơi, lần cuối; pill "Gợi ý" và nút màu cam ở bản được gợi ý; hai nút "Giữ bản trên máy" / "Lấy bản trên mây". Hồ sơ (mục Sao lưu) thêm dòng trạng thái ("Chưa có nơi lưu mây trên bản này" / "Chưa lưu lên mây" / "Đã lưu lên mây lúc HH:mm dd/MM" / "Không có mạng…" / "Bản trên mây hỏng…" / "…bản game mới hơn…"), nút "Lưu ngay", "Khôi phục từ mây", và dòng khôi phục sao lưu trên máy khi có.
- Save: thêm `playSeconds` (tổng thời gian chơi, cộng mỗi khung khi không tạm nghỉ; save cũ = 0), không tăng version. `SaveSummary` thêm Kinds, PlaySeconds.
- Ảnh (docs/anh/po-38/): `38-chon-ban.png` (tấm chọn bản, mây tiến xa hơn được gợi ý), `38-ho-so-may.png` (Hồ sơ sau khi Lưu ngay).

### Đã làm (đối chiếu với #38)
- Lớp `ICloudSave` tải lên/tải xuống, thông tin bản trên mây (thời điểm, cấp vườn, số thỏ, số loài, tổng thời gian chơi): đủ (phong bì + `SaveSummary`).
- Khi nào đồng bộ (mở game, ra nền, mốc lớn, tối đa vài phút một lần, không mỗi lần nâng cấp): đủ, có test.
- Hai bản lệch: không tự ghi đè, tấm so hai bản, gợi ý bản tiến xa hơn: đủ, có test.
- Không bao giờ mất save trên máy, khôi phục được trong Cài đặt: đủ (sao lưu trước khi thay; dòng khôi phục trong Hồ sơ, mở từ Tạm nghỉ).
- Không có mạng: đủ, có test.
- Chống gian lận nhẹ (phiên bản + mã kiểm, bản hỏng bỏ qua và báo): đủ, có test.
- Cài đặt: trạng thái, Lưu ngay, Khôi phục từ mây: đủ (đặt ở Hồ sơ, mục Sao lưu, vì tấm Tạm nghỉ đã đầy).
- Test lõi: chọn bản khi lệch, khứ hồi qua mây giả, bản hỏng, save cũ phiên bản thấp, mất mạng giữa chừng: đủ.

### Chưa đạt so với #38
- Chưa có nhà cung cấp thật (Game Center / Play Games là #39/#40): `Cloud` mặc định vẫn `LocalOnlyCloudSave`, mọi nút nói rõ "Chưa có nơi lưu mây trên bản này". Thử tay tấm chọn bản trên máy thật.

## Mốc trạm nhìn thấy được ở cấp 25, 100, 300 (#16; issue #49 mở trước khi có quy ước mới)

- Test: 584/584 EditMode qua (chạy sau lần sửa cuối trong Assets). Thêm 2 test màn hình; 2 test cũ đổi sang `L10n.StationLevel` vì biển cấp nay mang dấu bậc.
- Commit `f2ca812`, đã push. Comment đối chiếu trong #16 (dòng đã tick); #49 để mở cho PO đóng hoặc gộp, nhãn chờ-PO-duyệt.
- `Economy.Ranks = {25, 100, 300}`, `Economy.Rank(level)` 0–3. `StationActorView.SetRank`: bậc 1 dây 5 đèn nhỏ xen màu treo trên đạo cụ; bậc 2 thêm hai đèn đá nhỏ hai bên và biển cấp ánh vàng; bậc 3 thêm vầng sáng sau đạo cụ thở chậm (đứng yên khi Giảm chuyển động). `L10n.StationLevel`: "Cấp 120 ✦✦" trên biển trong vườn, tấm trạm và hàng tab Trạm. Vừa qua bậc: băng rôn "Trạm lên bậc N!" (thay băng rôn ×2 ở cùng cấp), vòng sao lớn hơn, bậc mặc lên ngay không chờ refresh. Trạm chưa mở không mang bậc. Save cũ ở cấp cao mở ra đã có bậc (bậc đọc từ cấp, không lưu thêm).
- Ảnh (docs/anh/po-49/): `49-bac-tram.png` (ấm trà bậc 1, giỏ lá bậc 2, nồi mứt bậc 3).

### Chưa đạt / cần PO quyết
- Chủ project chưa chọn kiểu: điều phối gợi ý băng cờ theo accent trạm; mình làm dây đèn/đèn đá/vầng sáng vì không cần hình mới. Đổi kiểu chỉ chạm `SetRank`. Chưa thử tay.

## #41 — Bản build iOS và Android, phần chuẩn bị không cần SDK

- Test: 584/584 EditMode qua. Thêm `BuildSettingsTests.cs` (4 test đọc lại cài đặt từ project).
- Commit `b9a7a62`, đã push. #41 để mở, đã tick các ô làm được và ghi rõ phần cần chủ project; nhãn chờ-PO-duyệt + cần-chủ-project.
- Máy này chỉ có module MacStandalone và WebGL (`PlaybackEngines`), không có iOS/Android: không build mobile được. Đã làm phần code: `BuildScript.BuildIOS` (Xcode project ở `Build/iOS`), `BuildAndroid` (APK ở `Build/Android/VuonTraTho.apk`), chung một hàm với bản Mac; thiếu module thì dừng ngay với thông báo cài module (exit 2). `tools/build.sh mac|ios|android`.
- `ContentSeeder.ApplyMobileSettings` (chạy khi seed): bundle id `com.vuontratho.game` cho iOS và Android (cùng Standalone), `bundleVersion` 0.1.0 và số build 1 cho cả ba (`BuildScript.Version/BuildNumber`, một chỗ để tăng), chỉ dọc (tắt xoay ngang và lộn ngược), ẩn thanh trạng thái, iPhone+iPad, Android minSdk 26, IL2CPP + ARM64. Icon mặc định cho mọi nền tảng và màn hình chờ (nền đêm của art bible, hình 2 giây) từ `icon_app.png` vẽ ghép từ thỏ trắng có sẵn + trăng + đồi cỏ (docs/anh/po-41/41-icon-app.png). Icon theo từng cỡ của iOS/Android chỉ gán được khi có module (có try/catch, seed lại sau khi cài).
- Chưa làm (cần chủ project): cài module iOS và Android qua Unity Hub cùng Xcode và Android SDK/NDK/JDK; chạy build thật; chạm thật, hiệu năng, dung lượng, TestFlight/Internal testing. Icon và màn hình chờ nên có hình vẽ riêng theo art bible thay cho bản ghép.

## #10 phần 2 — Đêm đom đóm (agent song song, nhánh agent-10b)

- Test: 617/617 EditMode qua trên main sau khi gộp (verify.sh: seed, 105 ảnh, biên dịch, test). Nhánh riêng 561/561: +15 test luật (`FirefliesTests.cs`), +1 màn hình.
- Commit `1227642` (nhánh agent-10b), gộp vào main qua `e282242`, đã push. Issue #10 đã comment phần 2, nhãn chờ-PO-duyệt (cả hai trò xong về luật và màn hình). Save vẫn v14, thêm khối `fireflies` sau `leaves`; `MiniGameRecord` thêm `CloversToday`, `GoldenToday`.
- Luật trong `Core/Fireflies.cs`: 20 giây, đàn 24 con (3 vàng) theo seed = ngày × 1000003 + số ván; mỗi con hiện trong một cửa sổ 4–7 giây, bay ngang với sóng sin riêng, vị trí là hàm thuần `PositionAt(fly, t)`; `Catch(x, y, now)` bắt con gần nhất trong bán kính 0,09, thường +1, vàng +3, trượt không mất; tối đa 30, sao 10/20/26; 3 lượt/ngày; mở ở cấp vườn 5; thưởng cà rốt = thu nhập/giây × hệ số × 20 × điểm/30; cỏ = điểm/10 (tối đa 3), 3 sao +1 cà rốt vàng; trần ngày 3 cỏ và 1 vàng, ván bị trần vẫn nhận cà rốt và tấm kết quả ghi rõ.
- Màn hình `FirefliesView` (`Canvas/Fireflies`): màn đêm phủ vườn, đom đóm là chấm sáng đặt theo đồng hồ mỗi khung (pool 24), nhấp nháy lệch pha (tắt khi Giảm chuyển động), chạm đâu cũng tính, con bị bắt phồng rồi mờ kèm "+1" / "+3 vàng!", lọ dưới đáy sáng dần, tấm kết quả. Lọ trong vườn (`Canvas/Garden/Content/Jar`) trên gốc cây cạnh ao, biển "Đom đóm · còn N lượt" / "Mai bắt tiếp". Thành tựu `dom-3sao`, `dom-30`.
- Ảnh (docs/anh/po-10/): `10-sau-dom-dom.png`, `10-sau-dom-dom-ket-qua.png`.

### Chưa đạt
- Chưa có hình lọ và đom đóm riêng. Chưa thử tay bán kính chạm trên máy thật.

## #20 — Khách ghé vườn và đơn hàng trà (agent song song, nhánh agent-20)

- Test: 617/617 EditMode qua trên main sau khi gộp. Nhánh riêng 538/538: +13 test luật (`OrdersTests.cs`), +3 màn hình.
- Commit `f1c35a7` (nhánh agent-20), gộp vào main qua `e282242`, đã push. Issue #20 đã comment (bảng 7 ý), nhãn chờ-PO-duyệt. Save vẫn v14, thêm khối `orders`; save cũ: khách đầu đến sau 1 phút. Khi gộp, `OrdersDelivered` dời xuống cuối enum (sau các metric đom đóm) nên seed viết lại hai quest asset; dòng mở hồ sơ trong `GardenScreen` giữ lại tham số `Runner.CloudStatus` của #38.
- Luật trong `Core/Orders.cs`: khách đầu sau 60 giây mở game, rồi mỗi 240 giây khi không có khách (đồng hồ chỉ chạy lúc đang chơi); đơn 1–2 dòng từ các trạm pha đang mở, mỗi món 1–3 (không quá một chồng ở khay); khách là một loài thỏ, loài chưa gặp nặng ×3; chờ 180 giây rồi đi, không phạt; tối đa 8 khách/ngày. Giao đủ: lấy món thường ở khay (món vàng giữ lại), trả giá bán các món + 90 giây thu nhập × hệ số, tính là đã thu; 1/3 đơn kèm 1 cỏ (quyết lúc đặt, ghi trên tấm).
- Màn hình (`GardenScreen` vùng `#region Guests…`, `OrderView`): bé thỏ (hình đúng loài) đi từ mép trái tới trước quầy tiệm (2,4 s; Giảm chuyển động thì tức thì), bong bóng ghi món × số (xanh khi khay đủ), pill "Khách!" trên HUD (chạm: lướt tới khách, mở tấm). Tấm `Canvas/Order/Card`: chân dung, "Chờ thêm m:ss", từng dòng có/cần, phần thưởng, nút "Giao" chỉ sáng khi đủ. Giao xong: cà rốt về ví, cỏ về tab Thỏ, băng rôn "Khách hài lòng!", thỏ cúi chào rồi đi. Mở lại game khi khách đang chờ: khách đứng sẵn.
- Nhiệm vụ: `ngay-don` "Giao 2 đơn trà cho khách" (ngày, +1 cỏ), `don-20` (+2 cỏ); `QuestMetric.OrdersDelivered` cuối enum; `DailyOrdersBase` chốt trong `RollDaily`. Đom đóm vàng (đã có) là thành viên cùng họ, không đổi code.
- Ảnh (docs/anh/po-20/): `20-sau-khach.png`, `20-sau-don.png`.

### Chưa đạt
- Chưa thử tay trên máy thật; bong bóng chưa có hình riêng; một khách một lúc (chưa có hàng).

## #45 nối hình — chày, bột, bánh, cối trống

- Test: 618/618 EditMode qua. Thêm 1 test màn hình (bột → đang giã → méo một lúc sau Méo → bánh xong trên tấm kết quả; chày hạ xuống rồi về chỗ).
- Commit `7ca9091`, đã push. Comment lên #45; nhãn chờ-PO-duyệt giữ nguyên.
- Catalog thêm MortarEmpty, Pestle, Dough, DoughPounded, Cake, CakeDented (seed từ `prop_coi-trong`, `prop_chay`, `banh_bot`, `banh_dang-gia`, `banh_xong`, `banh_meo`). Trong ván: cối trống, chày riêng nghiêng 10° treo trên bột, hạ 60 đơn vị khi giã rồi về (hai mốc cố định); bột thô tới nhịp 15, đang giã từ nhịp 15, méo 0,6 giây sau mỗi Méo, bánh xong khi hết ván; tấm kết quả có hình bánh (xong từ 1 sao, méo khi 0 sao). Cối trong vườn vẫn `prop_coi-gia-banh`.
- Ảnh (docs/anh/po-45/): `45-vong-chay.png`, `45-deo.png`, `45-ket-qua.png` cập nhật.

## #37 nối hình — bảy món và khay gỗ

- Test: 618/618 EditMode qua; test khay cũ đổi sang kiểm đúng hình món (`product_am-tra`) và đĩa gỗ (`khay_go`).
- Commit `a7897ba`, đã push. Comment đối chiếu lên #37, nhãn chờ-PO-duyệt (mọi ý làm được bằng code đã có; còn chạm/vuốt thật).
- Bảy `product_<id>.png` chép vào Art/Final, seed tự gán `StationData.Product`; khay dùng `khay_go` làm đĩa dưới từng món (khung khay vẫn tấm giấy chuẩn vì khay gỗ tròn không kéo dài được thành khung 5 ô).
- Ảnh (docs/anh/po-37/): `37-khay-hinh.png`.

## #44 lượt 3 — Đồ cho thỏ: mũ chỉ để đẹp và nhuộm lông (agent song song, nhánh agent-44)

- Test: 622/622 EditMode qua trên main sau khi gộp (verify.sh: seed, 107 ảnh, biên dịch, test). Nhánh riêng 621/621: +2 test luật (`ShopTests.cs`), +2 màn hình, 1 khẳng định thêm vào test quà nhiệm vụ. Các audit dễ đọc vẫn xanh.
- Commit `7b81fe9` (nhánh agent-44), gộp vào main qua `9763ceb`, đã push. Issue #44 đã comment lượt 3 (bảng 7 ý); cả ba lượt xong, nhãn chờ-PO-duyệt giữ nguyên. Save không đổi phiên bản, thêm `dye` cho từng thỏ; item có `effect` 3 (`None`).
- Luật trong `Core/Items.cs`, `Core/Shop.cs`: `ItemEffect.None`, `ItemDef.Price/Tier/HatLift`; `Shop.BuyItem` (mỗi loại một chiếc, trả cà rốt vàng, về tủ đồ chưa đội); `Shop.Dyes` 8 màu, `DyeRabbit` 8 củ mỗi lần, `Wash` miễn phí. Sửa luôn lỗi phát hiện khi làm: quà nhiệm vụ chỉ rút trong món có `Price = 0`, không rút mũ của tiệm.
- Tấm Tiệm cao 1560: mục "Đồ cho thỏ" giữa Gọi vàng và Kiếm ở, 6 ô mũ (hình, tên, giá), chạm hỏi rồi chạm lại trong 2 giây là mua, "Đã có" khi đã mua. Tấm thỏ cao 1520: hàng "Nhuộm lông · 8 cà rốt vàng mỗi lần" với 8 chấm (vòng mực quanh màu đang mang, vùng chạm 88), nút chữ "Gội lại".
- Màu nhuộm phủ 60 % lên trắng rồi nhân vào hình loài (`RabbitGridView.Fur`), áp ở lưới, tấm, trạm, ghế, thỏ đi dạo, thỏ đang đi tới trạm, thẻ trạm. Mũ đặt theo tỉ lệ hình, chân mũ trên đường đỉnh đầu + `HatLift` riêng (`RabbitGridView.PlaceHat`, dùng chung lưới và trạm).
- Hình: 6 file `hat_*.png` từ docs/anh/po-44 (vẽ bằng Antigravity) vào Art/Final. Ảnh (docs/anh/po-44/): `44-sau-do-cho-tho.png`, `44-sau-nhuom.png`.

### Chưa đạt
- Chưa thử tay trên máy thật (hai chạm trong 2 giây; màu nhuộm nhẹ trên thỏ tối).

## #2 và #3 — Thỏ cầm dụng cụ nghề; thỏ nghỉ ngồi, nằm, đội mũ, có tên (bản code, chờ hình vẽ)

- Test: 632/632 EditMode qua. Thêm 2 test màn hình.
- Commit `8438799`, đã push. Comment đối chiếu lên #2 và #3, nhãn chờ-PO-duyệt (chủ project đồng ý làm bản code trước, hình vẽ sau).
- #2: `StationActorView` có "Tool" (icon nghề của TRẠM, `catalog.TradeIcon(defs[i].Trade)`) treo ở tay gần của thỏ, đi theo thỏ khi nhún/nhảy; `MoveTool`: Brewer khuấy (xoay ±22° quanh tay), Baker nhào (ấn xuống 10), Gatherer vung giỏ (±12°), nghề còn lại nghiêng chậm ±8°; thỏ ngủ gật thì hạ dụng cụ; Giảm chuyển động thì đứng yên. 72 loài đều có vì dụng cụ nằm ngoài hình thỏ.
- #3: `PoseSeated`: ghế gốc cây, thảm, ghế nấm, xích đu → dáng ngồi (ép 1,06×0,86, hạ 6); võng → nằm (xoay −72°, 0,92, dời 12/26); mũ đặt lên đầu theo đúng cách tấm thỏ đặt (`RabbitGridView.PlaceHat`), xoay theo dáng; biển tên dưới ghế.
- Ảnh: `docs/anh/po-2/2-dung-cu-nghe.png`, `docs/anh/po-3/3-dang-nghi.png`.

### Chưa đạt
- Hình vẽ dáng ngồi/nằm và khung động tác nghề cho từng dáng thân (#2, #3 ghi "hoặc"); khi có hình thì thay ở `PoseSeated` và `MoveTool`.

## #15 — Đổi tên app sang Moon Rabbit Tea Garden (mặc định, chờ chủ project duyệt)

- Commit `fb68262`, đã push; 632/632. `BuildScript.ProductName = "Moon Rabbit Tea Garden"`, seed ghi vào `PlayerSettings.productName`; màn tiêu đề tiếng Việt giữ "Vườn Trà Thỏ Trăng". Thư mục log Mac đổi theo (`~/Library/Logs/VuonTraTho/Moon Rabbit Tea Garden/`), `tools/verify.sh --build` đọc đúng chỗ mới; save không đổi chỗ (theo bundle id). Test `BuildSettingsTests` kiểm tên.

## #42 phần code — Quyền riêng tư: chính sách, xoá dữ liệu, liên hệ (mặc định, chờ chủ project duyệt)

- Test: 633/633 EditMode qua. Thêm 1 test màn hình (mở chính sách, dòng liên hệ, xoá sau hai lần xác nhận: bản mây, save, sao lưu đi hết, vườn bắt đầu lại).
- Commit `18c21d1`, đã push. Comment lên #42 tick ba ô; điều phối tick trên issue; nhãn chờ-PO-duyệt.
- Bản nháp chính sách (vi + en) ở `docs/chinh-sach-quyen-rieng-tu.md`: game không thu thập gì, không máy chủ riêng, lưu mây là tài khoản của chính người chơi, xoá và xuất dữ liệu, trẻ em, liên hệ. Chủ project duyệt nội dung và đặt lên một địa chỉ công khai rồi ghi vào `AppInfo.PrivacyUrl` (đang trỏ vào file trong repo private, chỉ là giữ chỗ). `AppInfo.SupportEmail` giữ chỗ `hotro@vuontratho.game`.
- Hồ sơ (thẻ cao 1580): mục "Quyền riêng tư" với nút "Đọc chính sách quyền riêng tư" (`ProfileView.OpenUrl`), dòng "Cần giúp? Gửi mã người chơi tới …" (mã đã có nút Chép mã), "Xoá dữ liệu của tôi": chạm 1 → "Chạm lần nữa…", chạm 2 → "Không hoàn tác được. Chạm lần cuối…", chạm 3 → `GameRunner.DeleteMyData`: `ICloudSave.Delete` (mới, `LocalOnlyCloudSave`/`MemoryCloudSave` có), `SaveStore.DeleteAll` (save, tmp, backup, bad), `ResetGame`; nửa chừng bỏ 6 giây thì quên.
- Ảnh: `docs/anh/po-42/42-ho-so.png`.

### Chưa đạt (cần chủ project)
- Khai báo dữ liệu thu thập cho hai cửa hàng và nhóm tuổi mục tiêu; địa chỉ công khai của chính sách và email hỗ trợ thật.

## #8 — Thỏ sự kiện theo mùa (bản code, hình ứng viên 1 mỗi mùa)

- Test: 651/652 EditMode qua (1 inconclusive: test "xếp đúng nghề" bỏ qua vì bộ trạm giả trong test không có nghề). Thêm `SeasonTests.cs` (9 test: mốc ngày kể cả năm nhuận, ngày cuối mùa, loài mùa chỉ có trong mùa, thỏ ghé tiệm ≈ 1/3 ngày trong mùa và không bao giờ ngoài mùa, quà ngày cuối mùa tặng một lần, gọi vàng 50 % ở bậc Vàng trong mùa, ngày không lùi và lưu save, nhiệm vụ mùa chỉ trong mùa).
- Commit `75f72fc`, đã push. Comment đối chiếu lên #8, nhãn chờ-PO-duyệt (chủ project đồng ý làm bản code trước).
- `Core/Seasons.cs`: `Season` (None/MidAutumn/Tet/Christmas), `SeasonOf(day)` theo lịch máy (1/9–30/9, 15/1–28(29)/2, 1/12–31/12), `IsLastDay`, `Clamp(s, day)` với `GameState.MaxDaySeen` (save "maxDay"): `GameRunner.TodayNumber` giờ không bao giờ lùi, dùng chung cho quà ngày, thỏ ghé tiệm, gọi vàng và (qua TodayNumber) các trò nhỏ.
- Ba loài bậc Vàng có `Season`: `tho-den-ong-sao` (Ngắm sao), `tho-ao-dai` (Pha trà), `tho-noel` (Bếp bánh); hình chép từ docs/anh/po-8 (ứng viên 1), viền sinh tự động. `RabbitKinds.OfTier(tier, season)` chỉ đưa loài mùa khi đúng mùa; gọi thường không bao giờ ra loài mùa.
- Ba cách gặp: `Shop.VisitorFor` 1/3 ngày trong mùa là loài mùa (giá bậc Vàng); `Shop.ClaimDailyGift(s, day, name, out seasonRabbit)` ngày cuối mùa tặng loài mùa nếu chưa có (`GameRunner.SeasonArrived`); `Shop.SummonGolden(…, day)` 50 % khi rơi bậc Vàng trong mùa.
- Nhiệm vụ: `QuestDef.Season`, 9 nhiệm vụ `mua-gap/nghe/anh-<mùa>` thưởng 3/5/10 cà rốt vàng, chỉ hiện và chỉ nhận được trong mùa (`Quests.IsAvailable`); metric `SeasonKindMet`, `SeasonKindInTrade`, `PhotosWithSeasonRabbit` (save "photosSeason").
- Sổ thỏ: mục "Thỏ theo mùa · N/3" sau các bậc; loài chưa gặp ghi "Chỉ có trong Trung thu/Tết/Giáng sinh".
- Ảnh: `docs/anh/po-8/8-so-tho-mua.png`.

### Chưa đạt
- Ứng viên 2 mỗi mùa để PO quyết. Chưa thử tay.
- Nối thêm (`8062d34`, 653/654): băng rôn "Mùa … đã về!" một lần mỗi mùa mỗi năm (`Seasons.ShouldGreet`, save "seasonGreeted", `GameRunner.GreetSeason`/`SeasonBegan`, kèm tiếng); thỏ mùa từ quà ngày cuối mùa bước ra cổng trăng như mọi lần gọi (`SeasonArrived` → `Welcome`); vòm cổng trăng treo hình mùa (`decor_cong-trung-thu/tet/giang-sinh` của điều phối, `GameCatalog.GateDecor`, `SummonMomentView.SetSeasonDecor`), lắc ±2,5°, đứng yên khi Giảm chuyển động, ngoài mùa không treo (test). Ảnh `docs/anh/po-8/8-cong-midautumn/tet/christmas.png`.

## #6 — Cân lại kinh tế sau khi có đủ nguồn thu (agent song song, nhánh agent-6)

- Test: 662/663 EditMode qua trên main sau khi gộp (1 inconclusive có chủ ý của #8; verify.sh: seed, 113 ảnh, biên dịch, test). Nhánh riêng 631/631: +1 test mô phỏng (`EconomySimulationTests.cs`).
- Commit `310c418` (nhánh agent-6), gộp vào main qua `82f725c`…`6f2e1d3`, đã push. Issue #6 đã comment (bảng trước/sau, hằng số, bốn điểm cần PO quyết), nhãn chờ-PO-duyệt. Bảng đầy đủ: `docs/anh/po-6/6-mo-phong-truoc.txt`, `6-mo-phong-sau.txt`.
- Mô phỏng: người thong thả 20 + 10 phút/ngày × 7 ngày và người không nghỉ tay 2 giờ, chơi đủ khay, ba trò chơi, đơn khách, tiệm (quà ngày, ảnh, gọi vàng, thỏ ghé), trà lực, nâng cấp thỏ; cà rốt, cỏ, vàng chia theo nguồn. Phát hiện: vắng mặt chiếm 95–99 % cà rốt một ngày của người thong thả, nên tỉ lệ theo nguồn đo trên phần kiếm lúc đang chơi.
- Hằng số: thưởng nhiệm vụ theo giây thu nhập ÷ 10 (cả 9 nhiệm vụ mùa của #8: 60 → 6 giây, cà rốt vàng của chúng giữ nguyên); đường cong cấp vườn `100 + 14 × L` (cấp 5 = 540, 10 = 1 530, 15 = 2 870); cà rốt vàng mỗi 5 cấp 3 → 2; thành tựu vàng 5 → 3. Test ghim số cũ sửa theo; `GardenPacingTests` giữ nguyên ngưỡng.
- Nhịp sau khi đổi (thong thả): cấp 5 ở 7:15 (trước 12:25); cấp 10 lúc 8 h 29 tối ngày 1 (trước sáng ngày 2); 12 ô đất ngày 3; nhiệm vụ 36 % → 16 % cà rốt ở phút 10 / 20 (trước 80 / 66); trò chơi + đơn 20 % cà rốt lúc chơi ngày 1; 5 lượt gọi vàng tuần đầu rồi ~2–3/tuần.
- Kỹ thuật: Core thuần C# nên agent biên dịch Core + test mô phỏng bằng .NET SDK kèm trong Unity thành console (ngoài repo, `/tmp/econsim`) để thử hằng số trong vài giây; bảng trùng từng số với Unity.

### Chưa đạt / cần PO quyết
- Người chơi không nghỉ tay vẫn đủ 12 ô ở phút 9 (mỗi cấp mua = 1 điểm) — cần trần điểm nếu muốn ~2 giờ. Giá thỏ/đất phẳng nên thành miễn phí sau phút 5. Bùa Bàn tay ấm làm chạm thành 63 % thu nhập người chơi nhanh. Cỏ nhiều nhưng không lạm phát, để nguyên. Chưa thử tay.

## #11 — Trang trí vườn: 8 chỗ đặt cố định, 4 món, màu đèn lồng (agent song song, nhánh agent-11)

- Test: 662/663 EditMode qua trên main sau khi gộp. Nhánh riêng 638/638: +5 test luật (`DecorTests.cs`), +3 màn hình.
- Commit `1ce7ded` (nhánh agent-11), gộp vào main qua `82f725c`…`6f2e1d3`, đã push. Issue #11 đã comment (đề xuất trước khi code, rồi bảng đối chiếu 9 ý), nhãn chờ-PO-duyệt. Save thêm khối `decor` (không tăng phiên bản); save cũ mở nguyên.
- Luật trong `Core/Decor.cs`: 8 chỗ (`Spots`), 4 món (Đèn đá 15, Bồn hoa 20, Ghế nấm 30, Thảm picnic 40 cà rốt vàng; mua lặp, tổng tối đa 8), `Buy/Place/Store/Swap`, màu đèn lồng 4 màu × 10 cà rốt vàng (`SetLanternColor`). Nhãn "Ghế nằm" trong đề xuất đổi thành "Ghế nấm" vì hình `decor_ghe-nam` là ghế nấm.
- Màn hình: kệ "Trang trí vườn" trong Tiệm dưới "Đồ cho thỏ" (tấm cao 1770), 4 ô kiểu ô mũ + hàng 4 chấm màu đèn; 8 chỗ `Canvas/Garden/Content/Spot 0..7` chỉ hiện "+" mờ khi có món trong kho, chạm mở tấm `Canvas/Decor/Card` (Đặt/Đổi/Cất vào kho), bung sáng + blip; màu đèn nhuộm mọi `Lantern` và vầng sáng. Chỗ đặt cách trạm, ao và nhau ≥ 200; trang trí sẵn của vườn né chỗ.
- Ảnh (docs/anh/po-11/): `11-sau-trang-tri.png`, `11-sau-ke-trang-tri.png`.

### Chưa đạt
- Kiểu ao (ngoài phạm vi). Chưa thử tay. Tấm Tiệm đã dài, kệ sau nên tách tab.

## #1 — Mùa trăng mới: làm lại vườn để lấy hệ số vĩnh viễn (mặc định theo đề xuất, chờ chủ project duyệt)

- Test: 676/677 EditMode qua. Thêm `NewMoonTests` (9 test lõi: ngưỡng cấp 30 +5 mỗi mùa, trần mùa 7, không đủ cấp thì không đổi gì, giữ thỏ/đồ/cà rốt vàng/cỏ/album, về đầu cà rốt/trạm/ô đất/chỗ nghỉ/cấp vườn/khay/lượt chơi, thỏ ở trạm đóng về nghỉ không mất, Thỏ Trăng Non chỉ đến một lần và chỉ qua mùa mới, +25 %/mùa vào `Modifiers.IncomeMultiplier` và vào tiền trạm trả, save đọc/ghi `season` và save cũ = mùa 0, tấm so sánh đọc số thật), 1 test nhịp (`GardenPacingTests`: mùa 1 kiếm nhiều hơn mùa 0, vẫn qua mốc cấp 5 sau 10 phút), 4 test màn hình (hàng trong Tạm nghỉ khoá/mở theo cấp, tấm so sánh đúng số và giữ ngắn/trượt tay không mở khoá, giữ đủ rồi bấm lại → mùa 1, khoảnh khắc, Thỏ Trăng Non tới, chip "Mùa 1", hệ số 1,25; chạm bỏ qua khoảnh khắc).
- Commit `b53d5a4`, đã push. Comment lên #1 bảng tiêu chí; nhãn chờ-PO-duyệt.
- Lõi (`Core/NewMoon.cs`): mở khi cấp vườn ≥ 30 + 5 × số mùa đã qua (mùa 7 là mùa cuối, cần cấp 60). `Begin` giữ mọi thỏ (tên, cấp, mũ, màu), vật phẩm, cỏ, cà rốt vàng, chuỗi quà, thành tựu, album, hồ sơ, cài đặt; về đầu: cà rốt 0, trạm về 0 (ấm trà cấp 1), ô đất 4, chỗ nghỉ 2, cấp vườn 1, khay, trà lực đầy, đơn khách, lượt trò chơi nhỏ trong ngày. Thỏ ở trạm đã đóng về nghỉ; thỏ ở ấm trà giữ chỗ. Quà: +3 cỏ, +5 cà rốt vàng, lần đầu thêm **Thỏ Trăng Non** (`tho-trang-non`, bậc Tím, Ngắm sao, `NewMoonOnly`: không bao giờ ra từ cỏ, Tiệm hay ghép; album xếp vào mục "Thỏ dịp đặc biệt" với ghi chú "Chỉ đến cùng Mùa trăng mới"; chưa có hình riêng → dùng hình bậc Tím, cần vẽ `kind_tho-trang-non.png`).
- Hệ số: `NewMoon.Bonus = 1 + 0,25 × mùa` nhân vào `Modifiers.IncomeMultiplier` nên tiền trạm, nhiệm vụ, khay, trò chơi nhỏ, đơn khách và lúc đi vắng đều hưởng. Save: trường `season` (không tăng version), save cũ = mùa 0; `GameRunner.BeginNewMoon` quay lại mục tiêu cà rốt ngày theo vườn nhỏ.
- Màn hình: thẻ Tạm nghỉ cao 1400, thêm hàng "Mùa trăng mới" (nút vàng đèn lồng + huy hiệu "Đủ cấp" khi đủ; chưa đủ thì một dòng "Mùa trăng mới mở ở cấp vườn 30"; hết 7 mùa ghi "Đã qua đủ bảy mùa trăng"). Tấm so sánh `NewMoonView` ba ô Giữ / Về đầu / Được với số thật của save; nút bắt đầu cần **giữ 1,5 giây** (mặt nút đầy dần) rồi **bấm lần nữa** ("Bấm lần nữa để bắt đầu mùa N"); ngón trượt ra ngoài thì huỷ; "Để sau" đóng. Khoảnh khắc `NewMoonMomentView` 3 giây: trăng tròn lặn sau đồi, trăng non mọc lại cùng "Mùa N", chạm bỏ qua (giảm chuyển động: 0,6 giây không di chuyển); xong thì về ấm trà, vườn 4 ô với sương, Thỏ Trăng Non bước qua cổng trăng. HUD: chip "Mùa N" dưới huy hiệu cấp; hồ sơ ghi "Mùa N, thu nhập +25 %"; khung ảnh chụp ghi thêm "· Mùa N".
- Ảnh: `docs/anh/po-1/`: `1-mua-trang-nut` (hàng trong Tạm nghỉ), `1-mua-trang-so-sanh`, `1-mua-trang-dang-giu`, `1-mua-trang-mo-khoa`, `1-mua-trang-trang-lan`, `1-mua-trang-trang-moc`, `1-mua-trang-tho-moi`, `1-mua-trang-vuon-moi`.

### Chưa đạt (cần chủ project)
- Bốn ô "Chủ project cần chốt" trong `docs/de-xuat-1.md` đang theo đề xuất (cấp 30 / +25 % tuyến tính / ô đất và chỗ nghỉ về đầu / có Thỏ Trăng Non); đổi số nào thì chỉ đổi hằng số trong `NewMoon`. Hình riêng cho Thỏ Trăng Non và huy hiệu mùa vẽ tay.

## #33 phần cuối — Chữ trên nút chính đổi sang mực (nhánh agent-33)

- Test: 687/688 EditMode qua trên main sau khi gộp. Commit `09f8a94`, ff vào main qua `daad647`, đã push. Issue #33 đã comment và đóng theo quyết định mặc định.
- `Palette.OnCarrot = Ink`; `ChunkyButton.InkFor` trả mực cho cả nút cam lẫn tấm phẳng; 40 nhãn trên mặt nút cam và pill đang chọn (chế độ mua, ngôn ngữ, ảnh, tỷ lệ Nâng tất cả, tab Sổ thỏ, gợi ý chọn bản) đổi theo; test tương phản ghim 6,2:1. Huy hiệu đếm và chip cấp giữ nguyên (không phải nút chính). Ảnh: `docs/anh/po-33/33-truoc-nut-cam.png`, `33-sau-nut-cam.png`, `33-sau-nut-cam-tam-nghi.png`.

## #39 phần code — Đăng nhập Game Center và lưu mây iCloud trên iOS (agent song song, nhánh agent-39)

- Test: 687/688 EditMode qua trên main sau khi gộp; +8 test (`PlatformTests.cs`): seam ngoài iOS, kho iCloud ngoài iOS trung thực, tên mặc định, nhớ/quên đăng nhập, bị từ chối, vườn lấy tên GC một lần, save quá lớn không crash, kích thước vườn tối đa.
- Commit `59d075c` (nhánh agent-39), gộp vào main qua `a4c23d1`…`daad647`, đã push. Issue #39 đã comment (bảng 8 ý), nhãn chờ-PO-duyệt. **Chưa chạy trên máy thật** (máy không có module iOS/Xcode).
- Game Center (`Game/Platform/GameCenterAccount.cs`): tự đăng nhập khi mở; nhớ trong PlayerPrefs nên Hồ sơ ghi "Đã liên kết · tên" ngay; một lần kiểm tra thất bại không mất liên kết; bỏ liên kết chỉ quên trên máy. Vườn chưa đặt tên lấy tên Game Center một lần (`NameFor`, `GameRunner.AdoptLinkedName`).
- iCloud (`ICloudKvsCloudSave.cs` + `Plugins/iOS/CloudKvs.mm`): một khoá `vuontra.save`; máy khác ghi → đồng bộ trong 5 giây (`SyncReason.Remote`); chưa đăng nhập iCloud → dòng mây hướng dẫn. `ICloudSave.MaxBytes` + `CloudSync` đo phong bì, quá lớn → `SyncOutcome.TooLarge`. Đo: vườn thường 3 827 byte, vườn tối đa 30 748 byte (3 % của 1 MB).
- Build: `IOSCapabilities` trong `BuildScript.cs` thêm Game Center + iCloud KVS và entitlements khi build iOS; `docs/build-ios.md` ghi bước App ID / App Store Connect và cách thử.

### Chưa đạt
- Chưa biên dịch bằng Xcode, chưa thử trên iPhone. Chưa có nút "Bỏ liên kết".

## #5 — Chụm trackpad để zoom trên bản Mac (agent song song, nhánh agent-5)

- Test: 687/688 EditMode qua trên main sau khi gộp; +3 test (hàm thuần kẹp Xa/Gần, Editor im lặng, `SimulatePinch`). `verify.sh --build` xanh trên nhánh: bundle trong `VuonTraTho.app/Contents/Plugins`, Player.log không có `DllNotFoundException`.
- Commit `73d1611` (nhánh agent-5), gộp vào main qua `d9aa5c7`…`daad647`, đã push. Issue #5 đã comment, nhãn chờ-PO-duyệt.
- Bundle gốc `Assets/Plugins/macOS/PinchZoom.bundle` (nguồn `PinchZoom/PinchZoom.m` 52 dòng, dựng bằng `tools/build-pinch-bundle.sh`, universal): `NSEvent` local monitor nghe magnify và smart-magnify, cộng dồn độ phóng và vị trí con trỏ theo tỷ lệ cửa sổ; chỉ bật cho Standalone OSX. `TrackpadPinch.cs` (`Game/Platform`) đọc mỗi khung; `GardenScreen` chụm → `ApplyZoom(PinchZoom(_zoom, m), điểm dưới con trỏ)`, buông 0,25 s gióng về mức gần nhất; chạm đôi hai ngón → Gần ↔ Thường. `PinchGain = 1`: một lần chụm trọn đi hết Xa → Gần.
- Kỹ thuật: `codesign` từ chối file mới tạo trong `Documents` (xattr provenance) nên script link và ký trong thư mục tạm; `.m` có `.meta` tắt mọi nền để không bị kéo vào project Xcode iOS.

### Chưa đạt
- Chưa thử tay cử chỉ thật; chỉnh độ nhạy bằng `PinchGain` nếu cần. Editor Mac không nhận chụm (cố ý).

## #40 phần code — Android: Google Play Games và Saved Games (agent song song, nhánh agent-40)

- Test: 703/704 EditMode qua trên main sau khi gộp (+16 `PlayGamesTests.cs`); `verify.sh --build` xanh trên nhánh (tham chiếu assembly Play Games không ảnh hưởng player Mac).
- Commit `6ae4d55` (nhánh agent-40), gộp vào main qua `095dcdc` kèm sửa nhỏ (`SavedGamesCloudSave.MaxBytes` theo giao diện của #39), đã push. Issue #40 đã comment (bảng 9 ý + việc trên Play Console), nhãn chờ-PO-duyệt. **Chưa chạy trên máy thật** (máy không có module Android).
- Gói: `com.google.play.games` 2.3.0 + `com.google.external-dependency-manager` 1.2.190 qua UPM từ GitHub (`Packages/manifest.json`, lock cập nhật); asmdef bật define `VUONTRA_PLAY_GAMES` khi có gói; mọi chỗ gọi plugin sau `#if UNITY_ANDROID && !UNITY_EDITOR && VUONTRA_PLAY_GAMES`.
- Code (`Game/Platform/`): seam `IPlayGames` (test bằng bản giả), `PlayGamesAccount` (đăng nhập lặng khi mở, nút Liên kết hỏi ra mặt, nhớ PlayerPrefs, tên vườn mặc định), `SavedGamesCloudSave` (snapshot `vuon` chứa phong bì `CloudSync`, mô tả "Cấp vườn N · M thỏ", bìa = ảnh vườn mới nhất ≤ 800 KB, từ chối bản quá 3 MB, xung đột `UseLongestPlaytime`), `AndroidServices.Install` (cùng chỗ với `PlatformServices.Install` trong `GameRunner.Init`), `PlayGamesPlugin` (bộ chuyển sang plugin thật). Save tối đa đo được 80 106 byte, phong bì 94 325 byte (3 % của 3 MB).
- Tài liệu: `docs/build-android.md` (gói, sơ đồ code, việc trên Play Console, checklist máy thật).

### Chưa đạt (cần chủ project)
- Play Console (app, Play Games Services, OAuth client + SHA-1, bật Saved Games, tester), Android Setup trong Unity có module Android, Resolve EDM4U, build AAB thử nội bộ. Ảnh bìa có thể trống nếu ảnh chụp > 800 KB. Thành tựu và bảng xếp hạng chưa làm.

## #9 phần code — Mua cà rốt vàng bằng tiền thật: seam cửa hàng, ba gói, thẻ mua, khôi phục (mặc định, chờ chủ project duyệt)

- Test: 709/710 EditMode qua. Thêm `StorefrontTests` (4 test: ba gói 50/150/500 và gói hời; cộng một lần cho mỗi mã giao dịch, gói lạ không cộng; save nhớ 64 mã gần nhất và đọc lại không cộng lặp; cửa hàng giả trả tiền ngay, có thể bắt huỷ/lỗi, không sẵn sàng thì Unavailable, khôi phục, giao lại sau khởi động) và 2 test màn hình (nút "Mua thêm" ở hàng ví trong Tiệm mở thẻ gói với giá của cửa hàng, mua gói 1 cộng 50 và bay về chip, huỷ không cộng, cửa hàng giao lại cùng mã không cộng lặp, nút Khôi phục; cửa hàng chưa sẵn sàng thì gói mờ, giá "—", dòng trạng thái nói rõ).
- Commit `cdde124`, đã push. Comment lên #9 bảng tiêu chí; nhãn giữ cần-chủ-project (tài khoản và giá thật), thêm chờ-PO-duyệt.
- Lõi (`Core/Storefront.cs`): `GoldenPack` × 3 (`vuontra.golden.50/150/500`, gói giữa là "Hời nhất"), `IStorefront` (Ready, PriceOf, Buy, Restore, sự kiện Delivered cho giao dịch cửa hàng tự giao sau khởi động), `FakeStorefront` (giá giữ chỗ 25.000/69.000/199.000 ₫, chỉ để nhìn), `Storefront.Grant` cộng cà rốt vàng một lần cho mỗi mã giao dịch (save: `packs`, `paid` 64 mã gần nhất). Chỉ cà rốt vàng bán bằng tiền; cà rốt thường, thỏ, cỏ không bao giờ.
- `GameRunner.Store` (mặc định cửa hàng giả ở editor, test, bản Mac), `BuyGolden`, `RestorePurchases`; bản iOS/Android lắp `Game/Store/UnityStorefront.cs` (Unity IAP 4.15 đang cài: `IDetailedStoreListener`, ba consumable, `InitiatePurchase`, Pending khi chưa ai nhận, Restore qua `IAppleExtensions`/`IGooglePlayStoreExtensions`), seam `IReceiptCheck`: mặc định nhận mọi hoá đơn có nội dung; `TangleReceiptCheck` dùng `CrossPlatformValidator` chỉ biên dịch khi bật define `RECEIPT_VALIDATION` sau khi chủ project tạo tangle (Services > In-App Purchasing > Receipt Validation Obfuscator với khoá Google Play). `VuonTra.Game.asmdef` tham chiếu `UnityEngine.Purchasing(.Stores/.Security)` và define `UNITY_PURCHASING` theo gói.
- Màn hình: Tiệm có nút "Mua thêm" cuối hàng ví → thẻ `GoldenPacksView` "Mua cà rốt vàng": dòng nói cà rốt vàng dùng vào đâu và tỉ lệ gọi đã in ở Tiệm, ba hàng gói (icon, số, giá của cửa hàng, huy hiệu Hời nhất), dòng trạng thái (đang mở cửa hàng / đã cộng / đã huỷ / lỗi / đã cộng từ trước / chưa sẵn sàng), nút "Khôi phục giao dịch" (App Store yêu cầu), Đóng. Mua xong: tiếng thưởng, cà rốt bay về chip, chữ "+N cà rốt vàng".
- Ảnh: `docs/anh/po-9/9-tiem-mua-them.png`, `9-goi-ca-rot-vang.png`, `9-da-mua.png` (giá giữ chỗ của cửa hàng giả).

### Chưa đạt (cần chủ project)
- Tài khoản Apple Developer và Google Play Console, ba sản phẩm consumable khai trên hai cửa hàng với đúng ba id trên và giá thật; khoá Google Play → tangle → bật `RECEIPT_VALIDATION`; chạy thử trên máy thật (sandbox). Nâng gói lên Unity IAP 5 (viết lại `UnityStorefront` theo `StoreController`) là việc sau, khi đã có tài khoản.

## Bàn giao (22/11 ~18:25, cuối ngày: bảng issue đã cạn việc của session)

### Trạng thái
- main = origin/main tới commit này (ngay sau `a3663d6`), cây làm việc sạch trừ phần Unity tự sinh (`Assets/AddressableAssetsData/*`, `LocalizationSettings.asset`, `Garden.unity`; `.agent/`, `.agents/`, `.github/`, `skills-lock.json`, `docs/danh-sach-duyet.md` để nguyên).
- `tools/verify.sh --build` xanh lúc 18:22 trên HEAD `a3663d6` (main sau #1 + #9 + các nhánh của B): seed, 124 ảnh preview, biên dịch, 709/710 EditMode (1 inconclusive có chủ ý trong SeasonTests), build Mac mở chạy, Player.log không exception. Mốc trước: 17:21 trên `1b1907e`.
- Tối 22/11 chủ project nói "mục tiêu là không dừng khi chưa hoàn thành issue": các issue cần chủ project được làm phần code theo mặc định của đề xuất, phần tài khoản/giá/khoá để lại comment rõ; điều phối đóng nhóm B theo mặc định.

### Việc tối 22/11 (sau mục Bàn giao 17:40)
- Session này: #1 Mùa trăng mới (`b53d5a4`, 676/677, Thỏ Trăng Non vẽ bằng Antigravity `kind_tho-trang-non.png`); #9 phần code (`cdde124`, 709/710: seam `IStorefront`, ba gói cà rốt vàng, thẻ mua với Khôi phục giao dịch, cộng một lần mỗi giao dịch, `UnityStorefront` cho iOS/Android trên Unity IAP 4.15, cửa hàng giả ở editor); comment đối chiếu + nhãn chờ-PO-duyệt trên #1 và #9; hai dòng trạng thái trong thân #16 đã làm mới.
- B (agent trong worktree, gộp qua nhánh tích hợp): #33 chữ mực trên nút cam (`09f8a94`, đã đóng); #39 Game Center + iCloud key-value (`59d075c`, `Game/Platform/*`, `Plugins/iOS/CloudKvs.mm`, chỉ biên dịch `#if UNITY_IOS`); #5 chụm trackpad Mac (`73d1611`, `Plugins/macOS/PinchZoom.bundle`); #40 Play Games + Saved Games (`6ae4d55` + `095dcdc` + `86ae0a2`; manifest thêm `com.google.play.games` 2.3.0 + EDM4U, define `VUONTRA_PLAY_GAMES`); `docs/build-ios.md`, `docs/build-android.md`.
- Điều phối: đóng theo mặc định #43, #46, #30, #49, #17, #38, #7, #15; bản nháp khai báo dữ liệu cửa hàng + nhóm tuổi `docs/khai-bao-du-lieu-cua-hang.md` (#42, hai ô cuối tick); làm mới `docs/danh-sach-duyet.md` cho chủ project.

### Còn mở
- Cần chủ project: #4 (chơi thử tay bản Mac), #41 (module iOS/Android trên máy build; làm theo hai file docs/build-*.md), #39/#40/#5 (tài khoản Apple/Google, thử máy thật), #9 (tài khoản hai cửa hàng, ba sản phẩm `vuontra.golden.50/150/500`, giá thật, tangle kiểm hoá đơn rồi bật define `RECEIPT_VALIDATION`), #42 (email hỗ trợ và địa chỉ chính sách thật trong `AppInfo`), #16 (hàng đợi).
- Chờ PO duyệt (chờ-PO-duyệt), tất cả đã có comment + ảnh: #1, #2, #3, #6, #8, #10, #11, #13, #19, #20, #21, #28, #29, #37, #44, #45 và phần code của #5, #39, #40, #41, #42. Chủ project đọc `docs/danh-sach-duyet.md`, đóng hoặc gắn `PO-trả-lại` kèm ý chưa đạt (nhãn tạo khi dùng); session kế sửa `PO-trả-lại` trước.
- Hình vẽ tay còn thiếu: huy hiệu mùa (#1), chớp mắt/vẫy tai (#28), mốc trạm theo bậc (#49, đã đóng theo mặc định bằng code).

### Lưu ý kỹ thuật mới
- Mùa trăng mới: mọi số trong `Core/NewMoon.cs` (`FirstLevel`, `LevelStep`, `MaxSeasons`, `BonusPerSeason`, quà); hệ số nằm trong `Modifiers.From` nên mọi nguồn thu hưởng; `RabbitKind.NewMoonOnly` loại khỏi `OfTier`; `RabbitKinds.Special()` = loài mùa + loài trăng non (album). Test `Every_Kind_Has_A_Picture…` đòi mỗi loài một hình thật `kind_<id>.png` trong `Art/Final` (meta chép từ loài cũ, đổi guid).
- Cửa hàng (#9): `GameRunner.Store` (trường `_storefront`; `_store` là `SaveStore`), `BuyGolden`, `RestorePurchases`; `Storefront.Grant` chỉ cộng khi mã giao dịch chưa có trong `PaidTransactions` (save `paid`, 64 mã); `VuonTra.Game.asmdef` tham chiếu `UnityEngine.Purchasing(.Stores/.Security)` với define `UNITY_PURCHASING` theo gói 4.x; `UnityStorefront` chỉ biên dịch `UNITY_PURCHASING && (UNITY_IOS || UNITY_ANDROID)`; nâng gói lên IAP 5 thì viết lại file này theo `StoreController`.
- Từ B: `Palette.OnCarrot` và `ChunkyButton.InkFor` trả màu mực cho nút cam; `ICloudSave.MaxBytes`, `SyncOutcome.TooLarge`, `CloudStatus.SignedOut`; `GameRunner.Init` gọi `PlatformServices.Install` + `Platform.AndroidServices.Install` trước `SyncNow(Open)`; `GardenScreen.SimulatePinch` cho test chụm.
- Vẽ loài thỏ: tấm 3×2 nền `#2E5E49` theo `docs/anh/VE-BANG-ANTIGRAVITY.md`, cắt bằng `/tmp/agy-art/cat-tam-tho-nocorner.py` (bản không xoá góc) ra khung 320×343; hình có vầng sáng (đèn, ánh trăng) để lại viền xanh của nền, chọn ứng viên không có vầng sáng.

## Rà soát tối 22/11 — các commit trong ngày (#1, #9, #39, #40)

- Rà bằng skill code-review mức medium trên b53d5a4 (#1), và đọc tay cdde124 (#9, phần UnityStorefront chỉ biên dịch iOS/Android), 59d075c (#39), 6ae4d55 (#40). Verify sau khi sửa: 710/711 EditMode (1 Inconclusive có chủ ý của #8).
- Sửa (commit `5b552c4`): ghép ba thỏ có thể nuốt Thỏ Trăng Non — `Rabbits.Merge` lấy thỏ đang nghỉ trước, không phân biệt loài, mà thỏ trăng non vừa đến thì đang nghỉ và chỉ đến đúng một lần. Nay `Rabbits.Mergeable` loại loài `NewMoonOnly`, `CountOfTier`/`CanMerge` không đếm nó; thêm test `A_Merge_Never_Uses_Up_The_Moon_Rabbit`.
- Sửa (commit `763c48a`): `UnityStorefront.OnPurchaseFailed` truy `product.definition.id` khi cửa hàng báo lỗi cho sản phẩm không biết (`product` null) → thêm chốt null, chỉ ghi warning. Không có test (file chỉ biên dịch trên điện thoại).
- Không sửa, ghi lại: (1) `GameRunner.BeginNewMoon` cuộn lại ngày nên bốn nhiệm vụ ngày đã nhận có thể nhận lần nữa cùng ngày (tối đa 7 lần, mỗi mùa một lần) — chờ PO; (2) chip "Mùa N" trên HUD chạm mép chip cà rốt vàng vài đơn vị — chỉnh vị trí khi có ảnh thật; (3) `UnityStorefront` từ chối biên lai → `Complete` (cửa hàng giữ tiền, không trả cà rốt) — hợp lý khi `RECEIPT_VALIDATION` tắt, cân nhắc `Pending` khi bật; (4) `CloudSync` không chặn hai lần đồng bộ chồng nhau trên Android (Pull bất đồng bộ) — chưa thấy hậu quả, theo dõi khi thử máy thật.
- Bốn điểm trên đã làm sau khi điều phối duyệt (717/718): `144f454` `Quests.RerollCarrotTarget` chỉ tính lại mục tiêu cà rốt ngày (và mốc đã thu) khi sang mùa, giữ ngày và các nhiệm vụ đã nhận (test lõi + test runner); `7796a61` chip cà rốt vàng dời sang phải (x 240), test bốn trường hợp vi/en × thường/chữ to hai chip không giao nhau; `fed56b3` biên lai bị từ chối → `Pending` thay vì `Complete`, cửa hàng đưa lại lần mở sau (ghi vào hai hướng dẫn build; chỉ soi bằng mắt); `f93374b` `CloudSync.InFlight` bỏ qua lời gọi mới khi đang chờ mây, `MemoryCloudSave.Hold/Release` giả lập mạng chậm trong test.
- Sạch, không sửa: lõi NewMoon (reset, save, cap 7 mùa), NewMoonView (giữ 1,5 s rồi bấm), PlatformServices/GameCenterAccount (nhớ đăng nhập, tên mặc định), ICloudKvsCloudSave + CloudKvs.mm (đo 1 MB trước khi gửi, free chuỗi), SavedGamesCloudSave (không ghi đè bản chưa đọc, trần 3 MB), AndroidServices (đổi tên vườn chỉ khi chưa đặt).

## #70 — Bug chặn: bản Mac không lưu được vườn

- Test: 723/724 EditMode qua (1 inconclusive có chủ ý của #8); `verify.sh --build` xanh, bản Mac chạy, Player.log sạch. Thêm 6 test (`SaveStoreTests.cs`).
- Commit `2578e4e`, đã push. Issue #70 đã comment (gốc lỗi + bảng 5 ý), nhãn chờ-PO-duyệt.
- Gốc lỗi: `Garden.unity` lưu `SaveDirectory` là chuỗi rỗng, `SaveStore` chỉ đổi `null` thành thư mục mặc định, nên đường dẫn rỗng làm mọi lần tự lưu ném lỗi và lần mở đọc `save.json` theo thư mục làm việc.
- Sửa: `SaveStore` coi `null`/rỗng/khoảng trắng là chưa đặt → `Application.persistentDataPath`; `SaveStore.DefaultDirectory` để test trỏ vào thư mục tạm (editor dùng chung thư mục với bản Mac); `SaveStore.Folder`, `GameRunner.SaveFolder`.
- Thử tay bản Mac: mở → chờ → thoát hai lần, `playSeconds` trong save tăng 5,0 → 18,6 → 32,2 giây; log không còn "Không lưu được game". Save nằm ở `~/Library/Application Support/VuonTraTho/Moon Rabbit Tea Garden/save.json`.

### Chưa đạt
- Save cũ nằm cạnh app không được tự chuyển sang thư mục dữ liệu. iOS/Android dùng cùng `SaveStore` nhưng chưa thử trên máy thật.

## #69 — Màn lúc đi vắng kể chuyện: trần 3 giờ, bốn dòng, nhận ×2 một lần mỗi ngày

- Test: 744/745 EditMode qua trên main (sau khi gộp cả #55). Thêm `AwayTests.cs` (6) và 2 test màn hình; ba test cũ ghim trần 2 giờ đổi sang 3 giờ.
- Commit `c09c1d1`, đã push. Issue #69 đã comment (bảng 8 ý), nhãn chờ-PO-duyệt. Save thêm `awayDouble` (không tăng phiên bản).
- Lõi `Core/Away.cs`: `Away.Claim` bọc `Simulation.ClaimAway` và trả `AwayReport` (cà rốt, số giây, chạm trần, món thêm vào khay + khay đầy, khách ghé mà không ai tiếp — một mỗi vòng đặt-chờ, tối đa 8 —, thỏ ngủ say nhất); `Away.Double` miễn phí lần đầu mỗi ngày (ngày kẹp theo `MaxDaySeen`), sau đó 5 cà rốt vàng; khoản ×2 là quà, không tính vào cà rốt đã thu. `Simulation.AwayCapSeconds` 7 200 → 10 800.
- Màn hình `AwayView` (tấm trượt `Canvas/Away`): bốn dòng, ảnh thỏ ngáp khi mở (tắt khi Giảm chuyển động), nút "Nhận" và "Nhận ×2 · miễn phí hôm nay / · 5 cà rốt vàng / Còn thiếu N / Đã nhận ×2". Vắng dưới 5 phút không mở tấm, chỉ có "+X" và cà rốt bung ở ví. `GameRunner.LastAway`, `DoubleAway`, `AwayDoubleCost`.
- `GardenPacingTests`: các mốc giữ nguyên từng giây (cấp 5 lúc 6:03, cấp 10 lúc 6 giờ 30).
- Ảnh: `docs/anh/po-69/69-sau-mung-quay-lai.png`.

### Chưa đạt
- ×2 qua quảng cáo thưởng (chờ seam quảng cáo). Chưa thử tay.

## #55 — Thỏ đi dạo ra chỗ cỏ trống (bản đầu, agent song song, nhánh agent-55)

- Test: 744/745 EditMode qua trên main sau khi gộp. Nhánh riêng 730/731: `StrollsTests.cs` (10) + 5 test màn hình; một test cũ (`A_Strolling_Rabbit_Leaves_From_Its_Seat_…`) nới điều kiện vị trí vì không còn làn dạo cố định.
- Commit `e4874dd`, gộp vào main qua `9249b39` + sửa tên `VuonTra.Core.Away` trong `GardenScreen` (hàm `Away(int)` của #55 che lớp lõi của #69), đã push. Issue #55 đã comment, nhãn chờ-PO-duyệt.
- Lõi `Core/Strolls.cs`: `PickSpot` theo seed trong bán kính, tránh trạm và đường tới trạm; `MaxAtOnce` 1 thỏ/3 chỗ; `ActivityFor` trà/đom đóm (đêm)/ngủ; ở 4–6 s. Màn hình: một walker cho mỗi ghế (6), mỗi 48 s ra một trong 11 điểm cỏ trống, uống trà ♪, đuổi đom đóm ♥, nằm lăn zZ, rồi về; Giảm chuyển động chỉ hiện ở chỗ rồi về; vuốt vẫn được.
- Ảnh: `docs/anh/po-55/55-sau-uong-tra.png`, `55-sau-duoi-dom-dom.png`.

### Chưa đạt
- Thỏ làm việc nghỉ giải lao; ghế thảm cạnh ao chưa có chỗ trong tầm. Chuyển sang ô trống khi #52 lên.

## #78 — Bug nặng: chỉnh giờ máy qua lại là nhận lại quà ngày

- Test: 747/748 EditMode qua (trước khi gộp #50). Thêm test lùi giờ cho ảnh (`PhotosTests`), nhiệm vụ ngày (`QuestsTests`, thay test cũ ghim hành vi sai), đơn khách (`OrdersTests`) và runner (`GardenScreenTests`); test vuốt thỏ của #19 đổi lại (lùi ngày không cho tim).
- Commit `4f283cb`, đã push. Issue #78 đã comment (gốc lỗi + bảng 7 ý), nhãn chờ-PO-duyệt.
- Sửa: `GameRunner` 18 chỗ đổi `Today` (giờ máy thô) sang `TodayNumber` (chặn lùi theo `MaxDaySeen`); luật lõi so `day > ngày đã lưu`: `Photos.Take/GiftWaiting`, `Quests.RollDaily` (ngày lùi không xoá nhiệm vụ đã nhận), `Orders` (không đếm lại khách), `Rabbits.Pet` (không cho tim và Trà lực lần nữa).

### Chưa đạt
- Người chỉnh giờ tới tương lai rồi chỉnh về phải chờ tới ngày đó mới có quà ngày mới (như Tiệm, trò chơi nhỏ, mùa). Chưa thử tay.

## #50 — Vườn thoáng hơn: lùi camera, dọn cây to khỏi bãi cỏ (agent song song, nhánh agent-50)

- Test: 752/753 EditMode qua trên main sau khi gộp. Nhánh riêng 722/723: 5 test màn hình mới (zoom mặc định/giới hạn và ba cột trạm vừa màn, ≥ 5 ô đất trong khung mở đầu, không cây lớn trên bãi, tán viền cách mọi trạm và đạo cụ có biển ≥ 200, save cũ nhớ zoom 1,0); sửa 2 test ghim số cũ.
- Commit `10dec38` (nhánh agent-50), gộp vào main qua `1e10542`, đã push. Issue #50 đã comment (bảng 4 ý), nhãn chờ-PO-duyệt.
- `GardenScreen`: `ZoomDefault` 1,0 → 0,83 (`ZoomNear` 1,5, `ZoomFar` 0,65 giữ nguyên; ngưỡng nhận mức là trung điểm); bãi cỏ vẫn 1 080, màn hình ở mặc định rộng 1 301 nên bãi nằm giữa, hai bên cỏ rồi rừng; rừng dời ra `RimLeft = −300`, `RimRight = 1390`; `StationReach` + `CanopyClear` giữ cây/bụi viền cách trạm, giỏ, lọ, cối ≥ 200; cây to trên ô còn sương thành bụi + nấm; bỏ cây cao rải trong vườn.
- Ảnh (docs/anh/po-50/): trước/sau khung mở đầu, tấm trạm, zoom xa.

### Chưa đạt
- Không mở rộng `WorldW`. Save cũ nhớ zoom 1,0 mở ở 1,0 tới khi đổi zoom. Hàng cây đỉnh trên lò bánh nằm dưới HUD ở mặc định khi cuộn lên đỉnh. Chưa thử tay.

## QA #71–#77 — Bảy lỗi giao diện nhỏ (agent song song, nhánh qa-nits, mỗi issue một commit)

- Test: 767/768 EditMode qua trên main sau khi gộp (1 inconclusive có chủ ý của #8). Nhánh riêng 759/760: thêm 15 ca test, mỗi test đỏ trên code cũ, xanh sau khi sửa.
- Commit: #71 `3dbe0c3` (+ `fddbbe9` sau khi gộp), #72 `eeb7390`, #73 `5e8b01e`, #74 `49e8156`, #75 `9492bfa`, #76 `be72b83`, #77 `21ea82d`; gộp vào main qua `4df9b72`, đã push. Bảy issue đã comment bảng đối chiếu, nhãn chờ-PO-duyệt.
- #71: viên "Khách!" không đè biển nào của vườn khi camera ở ấm trà. Agent dời xuống y −244; khi gộp chung với #50 (zoom và lọ đom đóm đổi chỗ) vị trí đó lại đè biển lọ, nên viên đưa về sát dưới hàng HUD (y −176). Test đo theo đơn vị màn 1080×1920.
- #72: tỷ lệ gọi tiếng Anh "60%" thay vì "60.0%" (`ShopView.Odds` định dạng `0.#` trước khi đổi dấu thập phân).
- #73: "Cấp 500 ✦✦✦" co về một dòng trong viên ở thẻ trạm và hàng trạm.
- #74: nút "Cấp tối đa" không còn icon cà rốt.
- #75: thanh nút chụp ảnh lên y 240, tên vườn và ngày ở chân khung hiện đủ.
- #76: tên mũ và đồ trang trí ở Tiệm một dòng ("Mushroom stools" co chữ), không chạm viên giá.
- #77: điểm và đồng hồ/số nhịp ở ba trò chơi nhỏ cách hai mép 24.
- Ảnh: `docs/anh/qa-71` … `qa-77` (`qa-NN-truoc.png`, `qa-NN-sau.png`; #73 thêm `-hang`, #77 thêm `-gia-banh`).
- Kỹ thuật: `AccessibilityTests` có thêm `RectIn`, `OnPhone` (đổi về đơn vị màn 1080×1920 khi so HUD với vườn) và `OneLine` (kiểm chữ best-fit nằm một dòng ở cỡ thật sự vẽ).

### Chưa đạt
- Viên "▲ N" ở mép trên có thể đè biển "Giã bánh" (nêu trong #71). Code chưa có vùng an toàn ngang. Chưa thử tay.

## #51 — Bãi cỏ luôn quang, phần chưa mở là vành rừng (agent song song, nhánh agent-51)

- Test: 776/777 EditMode qua trên main sau khi gộp. Nhánh riêng 761/762: 5 test màn hình mới (vành không đè ô ở 4/6/8/10/12 ô và cách ô đã mua ≥ 200, không sương ở mọi đợt, mua đợt thì vành phía đó lùi và mờ, Giảm chuyển động đặt ngay, camera tới mọi ô đã mở sau mỗi đợt); test sương cũ thay bằng test vòng nét đứt.
- Commit `a0e5e51` (nhánh agent-51), gộp vào main qua `8d53fb3`, đã push. Issue #51 đã comment (bảng 8 ý), nhãn chờ-PO-duyệt.
- `GardenScreen`: bỏ sương và bụi/nấm trên ô chưa mua; ô chưa mua là vòng nét đứt (`Ring i`), mua xong mọc cỏ + hoa (`Bloom i`). Vành rừng hai bên bãi, hai hàng mỗi bên, sáu mẫu theo hash cố định; mỗi mảnh sát mép bãi và lùi ra tới khi cách mọi ô đã mua và giỏ/lọ/cối ≥ 200, không đè ô nào. Mua đợt: mảnh phải lùi thì mờ rồi hiện lại xa hơn trong 0,6 s (`RimStep`). `RefreshFog` → `RefreshLand`. `ClampView` giữ nguyên.
- Ảnh (docs/anh/po-51/): trước/sau khung mở đầu và zoom xa, khung mở đầu vi/en/chữ to, mua đợt 1 trước/giữa/sau, năm mốc đợt đất (`PreviewShot.RenderLandWaves`).

### Chưa đạt
- Vành chỉ ở hai bên (ô chưa mở xen kẽ theo chiều dọc nên không đặt được mặt rừng ngang). Ảnh en/chữ to chỉ ở khung mở đầu. Chưa thử tay.

## #71 phần cuối — Viên "▲ N" không đè biển "Giã bánh"

- Commit `349e76a`, đã push; comment lên #71. Không đổi code: ảnh QA chụp từ bản trước #50, sau khi camera lùi biển cối nằm thấp hơn phao. Thêm test `The_Top_Edge_Pill_Covers_No_Garden_Plate_As_The_View_Rests_On_The_Teapot` ở zoom mặc định và zoom gần để lỗi không quay lại.

## #57 — Lịch quà 7 ngày ở Tiệm, ngày 7 là quà lớn, bỏ ngày chỉ lùi một (agent song song, nhánh agent-57)

- Test: 805/806 EditMode qua trên main sau khi gộp (cùng #56). Nhánh riêng 787/788: 5 test Core (`ShopTests`), 2 test màn hình, 1 test tràn chữ ×4; test cũ "bỏ một ngày về ngày 1" thay bằng luật mới. Thêm 1 test màn hình cho khoảnh khắc quà lớn.
- Commit `9c2fd85` (nhánh agent-57), gộp vào main qua `8ca0fee`, khoảnh khắc quà lớn `e16685b`, đã push. Issue #57 đã comment (bảng 9 ý), nhãn chờ-PO-duyệt. Save thêm `giftBig`, `giftBigGolden` (v14).
- `Shop`: `MissPenalty = 1`, streak sau khi bỏ k ngày = max(1, streak − k); `Week(s, day)` cho 7 ô; ngày 7 lần đầu tặng Nón lá + 1 cỏ (đã có mũ thì +20 cà rốt vàng). `ShopView`: mục Hôm nay cao 320 với hàng `Days/Day 1…7`. `GardenScreen.OnDailyGift`: quà lớn có `Feel.Moment` riêng nêu tên mũ và cỏ.
- Kinh tế: tuần thong thả cà rốt vàng không đổi (17 từ quà ngày, 50/50); thêm 1 cỏ → 118 thỏ (trước 117).
- Ảnh (docs/anh/po-57/): `57-ngay-1.png`, `57-ngay-4.png`, `57-ngay-7.png`.

### Chưa đạt
- Chưa thử tay.

## #56 — Thỏ xin chụp ảnh mỗi 4 phút (agent song song, nhánh agent-56)

- Test: 805/806 EditMode qua trên main sau khi gộp. Nhánh riêng 791/792: `PhotoAsksTests.cs` (11) + 4 test màn hình.
- Commit `6391173` (nhánh agent-56), gộp vào main qua `0b6c7c2` + seed lại bảng chuỗi (`d325663`), đã push. Issue #56 đã comment (bảng 11 ý), nhãn chờ-PO-duyệt. Save thêm khối `photoAsk` (v14).
- Lõi `Core/PhotoAsks.cs`: đồng hồ 240 giây chỉ chạy khi đang chơi, đứng yên khi có bong bóng hoặc đang chụp; bong bóng 90 giây; chỉ thỏ ở trạm đã mở hoặc chỗ nghỉ; thưởng 120 giây × hệ số; tối đa 10/ngày theo ngày đã chặn lùi; `QuestMetric.PhotoAsksAnswered` cuối enum.
- Màn hình: bong bóng máy ảnh vẽ bằng code trên đầu thỏ, chạm thì mở máy ảnh zoom gần, thỏ ở giữa; chụp xong thẻ ảnh ghi lời cảm ơn, bấm Xong thì thỏ nảy, tim bay, cà rốt về ví; đóng không chụp mất lượt đó. Nhiệm vụ ngày `ngay-anh` "Chụp 3 tấm thỏ xin ảnh" (+1 cỏ); giữ "Chạm bé thỏ 50 lần" nên có năm nhiệm vụ ngày.
- Ảnh (docs/anh/po-56/): `56-xin-chup.png`, `56-khung-thu.png`.

### Chưa đạt
- Mở máy ảnh bằng nút HUD lúc có lời mời thì không được thưởng. Chưa có tiếng riêng. Năm nhiệm vụ ngày (PO quyết có bỏ "Chạm bé thỏ 50 lần"). Chưa thử tay.

## #60 — Thỏ nghỉ góp sức: mỗi bé ở chỗ nghỉ cộng 2–10 % thu nhập vườn (agent song song, nhánh agent-60)

- Test: 816/817 EditMode qua trên main (gộp thẳng, cùng cây agent đã verify). 11 test mới (`RestSpotsTests` 7, `CatalogTests` 1, `GardenScreenTests` 3); hai test mùa trăng mới đổi kỳ vọng thành `1,25 × RestSpots.Multiplier`.
- Commit `d10d0e8`, đã push. Issue #60 đã comment (bảng 9 ý + nhịp trước/sau), nhãn chờ-PO-duyệt.
- `RestSpots`: `RestBonus = 0,02` × `TierScale` {1; 1,25; 1,5; 2; 3; 5} cho mỗi bé có ghế (`Seated`, cùng thứ tự màn hình vẽ); `Modifiers.From` nhân vào `IncomeMultiplier` nên mọi nguồn thu đều hưởng. `IncomeView` dòng "Thỏ đang nghỉ (N) +x %"; tấm thỏ "Đang nghỉ · +x % vườn"; tab Thỏ nhãn "+x % vườn". Thành tựu `nghi-6` (2 cỏ), `QuestMetric.RabbitsResting` cuối enum. Hai mô phỏng nhịp tính lại hệ số sau khi gọi thỏ hoặc mở rộng vườn.
- Nhịp: cấp 5, cấp 10, 12 ô đất không đổi ở cả ba mô phỏng nên giữ 2 %; thu nhập thong thả hết ngày 7 cao hơn khoảng 19 % so với cùng lượt chạy không có phần thưởng.
- Ảnh (docs/anh/po-60/): `60-thu-nhap.png`, `60-tho-nghi.png`, `60-tab-tho.png`.

### Chưa đạt
- Thẻ ở tab Thỏ chỉ có nhãn ngắn. Chưa thử tay.

## #59 — Thỏ tự nói chuyện khi rảnh (agent song song, nhánh agent-59)

- Test: 836/837 EditMode qua trên main sau khi gộp. Nhánh riêng 825/826: `ChatterTests.cs` (10), 6 test màn hình, 4 ca chữ vừa bong bóng trong `AccessibilityTests`.
- Commit `fe37a8d` (nhánh agent-59), gộp vào main qua `cf91d4a` (ghép hai hàm khung preview của #59 và #60) + seed lại bảng chuỗi, đã push. Issue #59 đã comment (bảng 11 ý), nhãn chờ-PO-duyệt. Save không đổi.
- Lõi `Core/Chatter.cs`: `KeysFor(nghề, đang nghỉ, pha ngày, mùa, được tiếp sức, diện đồ)`, `Pick`, `Wait` (40–70 giây), `BoosterOf`. 33 câu vi/en (4 × 4 nghề, 4 chung, 2 × 4 pha ngày, 3 mùa, tiếp sức, khoe đồ).
- Màn hình: vùng mới cuối `GardenScreen` + một lời gọi trong `Advance`; bong bóng `ChatLine` 2,5 giây đi theo thỏ; ưu tiên thân thiết > khách > rảnh; thỏ ngủ gật và thỏ đang xin chụp ảnh không nói.
- Ảnh (docs/anh/po-59/): `59-noi-ngay.png`, `59-noi-dem.png`.

### Chưa đạt
- Không có tiếng; thỏ ngủ gật không nói; câu rảnh có thể chờ tới 180 giây khi có khách; bong bóng chưa có đuôi. Chưa thử tay.

## #61 — Sổ thỏ có thưởng: đủ bộ nghề trong bậc +3 %, mốc 10/20/40/72 loài (agent song song, nhánh agent-61)

- Test: 852/853 EditMode qua trên main sau khi gộp. Nhánh riêng 832/833: `AlbumSetsTests` 9, `GardenScreenTests` 3, `AccessibilityTests` 4 ca; `GardenPacingTests` thêm kiểm tối đa 2 bộ trong ngày 1.
- Commit `ee9cd0f` (nhánh agent-61), gộp vào main qua `89e09d7` (ghép test, chuỗi và khung preview với #59) + seed lại bảng chuỗi, đã push. Issue #61 đã comment (bảng 10 ý + điểm cần PO chọn), nhãn chờ-PO-duyệt. Save thêm `albumMilestones` (v14).
- `Core/AlbumSets.cs`: 24 bộ (bậc × nghề, chỉ loài thường ngày); `TradeFactor` = 1 + 0,03 × số bộ đủ (tối đa 1,18), nhân trong `Economy.IncomePerCycle` theo nghề trạm (trạm nguyên liệu nhân vào phần tiếp sức). Mốc: nhiệm vụ `loai-10/20/40/72` (`QuestMetric.KindsMet`) trả 1, 2 + 1 cỏ, 10, 20 cà rốt vàng; `ClaimMilestones` tặng Mũ sao (40) và khung Đủ bộ (72). Sổ thỏ chia nhóm theo nghề, viền vàng khi đủ, băng rôn khi vừa đủ.
- Nhịp: mốc 10 và 20 hạ từ 3 và 5 vàng xuống 1 và 2 để giữ "12 ô đất chờ ngày 3"; sau khi hạ, 12 ô đất lúc 48 giờ 7 phút; cấp 10 thong thả lúc 4 giờ 29 (trước 6 giờ 30), cấp 5 không đổi.
- Ảnh (docs/anh/po-61/): `61-so-tho-bo.png`, `61-du-bo.png`.

### Chưa đạt
- Bảng thu nhập chưa có dòng sổ thỏ; chế độ chụp chưa chọn được khung Đủ bộ. Chưa thử tay.
- Chủ project quyết 23/11: giữ 1 và 2 cà rốt vàng ở mốc 10/20 loài (đã đúng trong code), giữ mốc 12 ô đất chờ ngày 3.

## #65 — Nhắc tôi: thông báo tại máy khi khay đầy, quà ngày, mùa về (agent song song, nhánh agent-65)

- Test: 868/869 EditMode qua trên main sau khi gộp. Nhánh riêng 852/853 + `verify --build` xanh: `RemindersTests` 13, `GardenScreenTests` 3.
- Commit `aeb1c42` (nhánh agent-65), gộp vào main qua `240b6d8` + seed lại bảng chuỗi, đã push. Issue #65 đã comment (bảng 9 ý), nhãn chờ-PO-duyệt. Save không đổi.
- Lõi `Core/Reminders.cs` (`Plan(state, now, offset, mods, on)`: tối đa 4 nhắc, ưu tiên mùa > quà ngày > khay đầy > nhớ bạn, cách nhau ≥ 4 giờ, 21h–9h dời tới 9h, ngày như `TodayNumber`). `Game/Platform/Notifications.cs`: `MobileNotifier` (gói `com.unity.mobile.notifications` 2.5.0, define `VUONTRA_NOTIFICATIONS`, chỉ trên máy thật) và `LogNotifier` (Mac, editor). `GameRunner` đăng ký khi vào nền/thoát, xoá khi quay lại. Bản iOS tắt xin quyền lúc mở app. Tấm Tạm nghỉ thêm dòng "Nhắc tôi" (cao 1500), mặc định Tắt.
- Ảnh (docs/anh/po-65/): `65-nhac-toi.png`, `65-nhac-toi-en.png`.

### Chưa đạt
- Thử trên máy thật chờ #41. Khởi động lại máy thì mất nhắc; giờ mùa hè có thể lệch một giờ; bấm thông báo chỉ mở game.

## #64 — Hũ trăng: 1 giờ thu nhập mở ở ví, Tiệm bán 12 cà rốt vàng (agent song song, nhánh agent-64)

- Test: 865/866 EditMode trên nhánh (main trước đó 852/853). Thêm `MoonJarsTests` 9, `GardenScreenTests` 3 (ví, kệ Tiệm, nhiệm vụ thu hoạch), `GardenPacingTests` 1 (tuần thong thả trước/sau). Test đom đóm trên màn hình đổi kỳ vọng từ cà rốt vàng sang hũ. `AccessibilityTests` đo thêm tên ô hũ.
- Commit `d4b2d24`, merge `b87eaa5`, bảng chuỗi `d109c58`. Main sau gộp: 881/882. Save thêm khối `moonJars` (giữ v14; save cũ 0 hũ).
- `Core/MoonJars.cs`: `Value` = thu/giây × 3600 × IncomeMultiplier × SpeedMultiplier lúc mở; trần 9; Tiệm 12 vàng, 3/ngày theo `TodayNumber`; một hũ miễn phí/ngày dùng chung cho mốc cấp chia hết cho 10, nhiệm vụ `ngay-thu` (`RewardJars` thay cỏ) và đêm đom đóm 3 sao (thay cà rốt vàng trong ngày). Hai nguồn sau chờ cấp vườn 15. Không có hũ thì nguồn trả quà cũ. Nút, ô HUD và ô Tiệm hiện từ cấp vườn 3.
- Màn hình: thanh hũ cuối `IncomeView` ("Mở hũ trăng (N)", cà rốt bay về ví như bán món), ô hũ cạnh ô cà rốt vàng trên HUD, ô "Hũ trăng" cuối hàng Trang trí vườn (chạm hai lần, "Còn N/3", "Kệ đầy", "Mai mua tiếp"), ô hũ trên dòng nhiệm vụ, dòng "+1 hũ trăng (3 sao)". Hũ vẽ bằng code. `GardenScreen` có vùng mới cuối file và một chỗ trong `OnClaim`.
- Nhịp (mở ngay, không mua): cấp 5, cấp 10, 12 ô đất và số lượt gọi vàng không đổi ở cả ba mô phỏng; thong thả 5–6 hũ/tuần, nhiều nhất 1/ngày; hũ 0,7 % thu nhập cuối ngày 1, 1,5 % cuối ngày 7; thu nhập/giây ngày 7 lên 8,04ac (trước 2,32ac). Trả hũ từ đầu hoặc từ cấp 10 đưa 12 ô đất về ngày 2, nên hai nguồn hằng ngày chờ cấp 15.
- Ảnh (docs/anh/po-64/): `64-vi-hu.png`, `64-ke-hu.png`.

### Chưa đạt / cần PO quyết
- Giữ mốc cấp 15 cho nhiệm vụ và đom đóm, hay nới mốc 12 ô đất. Chưa có thông báo "đã đầy"; hũ mốc cấp mất im lặng khi đã có hũ hôm nay hoặc kệ đầy. Hình hũ chờ vẽ tay. Chưa thử tay.

## #63 — Tiếng trạm khi zoom gần, thanh Tiếng trạm, chọn nhạc

- Test: 889/890 EditMode qua, 0 hỏng (1 test mùa vụ không kết luận, có từ trước, không liên quan). 22 test mới trong `StationSoundTests`. `tools/verify.sh --build` xanh, Player.log không có lỗi âm thanh.
- Commit: `7badf52`, merge `24b5fd9`, bảng chuỗi `6ee9679`. Main sau gộp: 902/903.
- Ảnh (docs/anh/po-63/): `63-tieng-tram.png` (thẻ Tạm nghỉ, nút Nhạc ở Rạng sáng), `10-pause.png`, `14-pause-en.png`.

### Đã làm
- Zoom gần thì trạm giữa màn (hoặc trạm đang mở thẻ) phát vòng tiếng riêng: lên trong 0,5 giây, đổi trạm thì chéo 0,5 giây, về mức mặc định thì tắt. Trà lực đang chạy thì nhanh hơn 10 %.
- 9 vòng tổng hợp bằng code, 5–6 giây, tạo một lần. Ấm trà sôi; nồi mứt sánh; hai lò lách tách; trà sữa lắc đá; bình ủ sao và đài ngắm sao leng keng; cối giã hoa thịch thịch; tổ mật nhỏ giọt có tiếng ong; suối hoa chảy; giỏ lá và giàn nho lá xào xạc.
- Thẻ Tạm nghỉ thêm thanh "Tiếng trạm" (mặc định 0,7) và nút "Nhạc" bấm vòng: Tự động / Đêm trăng / Rạng sáng / Mưa. Tự động: Rạng sáng 5–17 giờ, Đêm trăng các giờ còn lại.
- Thêm hai bài nhạc: Rạng sáng (nhanh hơn, sáng hơn) và Mưa (chậm, buồn nhẹ, có tiếng mưa).
- Sửa kèm: trước đây lần mở game đầu tiên, thanh Nhạc nền và Âm thanh hiện giá trị mặc định thay vì giá trị đã lưu. Nay hiện đúng.

### Chưa đạt
- Chưa ai nghe thật: chủ project cần mở bản Mac, zoom gần từng trạm và ghi tiếng nào chói.
- Chưa có file ghi âm; mới có chỗ gắn `Sfx.SupplyStationClip`, seeder chưa nạp `sfx_<id>.ogg`.
- Game chưa có mưa, nên bài Mưa chỉ phát khi người chơi chọn.

## #52 — Vườn đứng trên lưới lục giác (agent song song, nhánh agent-52)

- Test: 917/918 EditMode trên main sau khi gộp. Có 15 test mới: 12 trong `GardenGridTests`, 3 trong `GardenScreenTests`.
- Commit: code `00ef751`, merge `c587616`, sửa sau gộp `643faa1`, `bd10c7d`, `5b2ecfa`. Save thêm khối `grid`, vẫn giữ v14.
- Mỗi trạm, chỗ nghỉ, đồ trang trí, cối, tiệm và quầy khách có một ô lục giác riêng, tính từ ấm trà (các ô cách nhau 150 ngang, 200 dọc).
  - Bãi cỏ lúc đầu rộng 3 vòng; mỗi đợt mở đất thêm 1 vòng, tới 7 vòng khi đủ 12 trạm.
  - Save cũ tự xếp mọi thứ lên ô mặc định, không mất gì.
  - Người chơi gần như không thấy khác: vật xê dịch nhiều nhất 171 đơn vị, cối lên nằm trên ấm trà.
- Sửa thêm khi gộp với main:
  - Các bảng tĩnh (`Seats`, `DecorSpots`, `Spots`) mà viền rừng #51, bé thỏ đi dạo #55 và tiếng trạm #63 còn dùng nay đọc từ lưới.
  - Tính lại điểm dạo chơi trên bãi cỏ.
  - Biển "Giã bánh" dời sang trái để không nằm dưới ô "▲ N" ở mép trên.
- Ảnh (docs/anh/po-52/): `52-truoc-vuon.png`, `52-sau-vuon.png`, `52-truoc-12-tram.png`, `52-sau-12-tram.png`.

### Chưa đạt / cần PO quyết
- Bán kính bắt đầu là 3, không phải 2 như đề xuất. Ghế nghỉ, tiệm và hai chỗ trang trí cuối đứng ngoài bãi cỏ cho tới đợt mở đất cuối, coi như khung có sẵn của vườn.
- Chưa có #53 (vòng đất lộ dần) và #54 (người chơi tự xếp vườn). Chưa thử tay.

## #68 — Mã quà và hộp thư: nhập mã trong Hồ sơ, thư quà đền bù, không cần máy chủ (agent song song, nhánh agent-68)

- Test: 938/939 qua, 0 lỗi (1 test inconclusive là `SeasonTests.Season_Quests_Show_Only_In_Season_And_Read_The_Season_Rabbit`, không thuộc phần này); 36 test mới (18 GiftCodes, 11 Mailbox, 7 màn hình); `tools/verify.sh` GREEN.
- Commit: f6f1d4c, merge 5464614. Main sau gộp: 953/954.
- Đã làm:
- `Core/GiftCodes.cs`: bảng mã công khai `TRATHO-2026` (5 cà rốt vàng + 2 cỏ) và `CAMON-PLAYTEST` (mũ Nơ trăng), cả hai dùng được đến 31/3/2027.
- Mã ký HMAC-SHA256: 4 ký tự loại (quà + ngày cuối) và 4 ký tự chữ ký. Có 4 loại: 5 vàng, 10 vàng, 2 cỏ, 5 vàng + 2 cỏ.
- Mỗi vườn dùng một mã một lần; mã đã dùng lưu trong save (`codesUsed`). Không phân biệt hoa thường, bỏ qua dấu cách và gạch ngang.
- `tools/make-code.sh`: tạo khoá (`--new-key`) và tạo mã; khoá nằm ở `Assets/Resources/keys.json`, git bỏ qua.
- `Core/Mailbox.cs`: tối đa 10 thư, mỗi thư 30 ngày. Thư xin lỗi khi save hỏng (10 vàng + 3 cỏ). Thư quà mùa (5 vàng) cho mỗi mùa bị lỡ trọn vẹn trong vòng 1 năm. Số quà theo loại thư, sửa file save không tăng được.
- Hồ sơ: ô nhập mã + nút "Nhận quà" dưới mã người chơi. Mã đúng thì đóng thẻ, cà rốt vàng bay về ví; mã sai thì ô rung, `Sfx.Deny` và báo lý do.
- Thẻ Tạm nghỉ: nút "Thư" ở góc trên trái, có số đếm, mở thẻ Hộp thư.
- Ảnh: docs/anh/po-68/68-tam-nghi-thu.png, 68-hop-thu.png, 68-hop-thu-da-nhan.png, 68-ho-so-ma-qua.png, 68-ma-sai.png

### Chưa đạt / cần PO quyết
- Chưa chạy thử bằng tay trên bản Mac (mới có test màn hình).
- Chưa có khoá thật: cần chạy `tools/make-code.sh --new-key` trên máy build và cất khoá ở chỗ an toàn. Khoá nằm trong bản build (thư mục Resources), nên người chịu khó mổ bản build vẫn lấy được.
- PO duyệt các con số tự chọn: hạn 31/3/2027 cho hai mã công khai, thư xin lỗi 10 vàng + 3 cỏ, thư quà mùa 5 vàng, 4 loại mã ký.
- Nhập mã đúng sẽ đóng cả Hồ sơ lẫn thẻ Tạm nghỉ để thấy quà bay về ví; nhận thư thì ở lại hộp thư và chỉ hiện một dòng "Đã nhận …".
- Chơi lại từ đầu thì danh sách mã đã dùng cũng mất, nên nhập lại được mã cũ.

## #66 — Seam quảng cáo thưởng ở ba chỗ có trần ngày, và vé Bỏ quảng cáo (agent song song, nhánh agent-66)

**Test:** 940/941 (1 Inconclusive có sẵn: SeasonTests, phụ thuộc mùa), 25 test mới. verify.sh RESULT GREEN.
**Commit:** 6a544ac, merge 6e866b2. Main sau gộp: 976/977.

Đã làm:
- `Core/Ads.cs`: `IRewardedAds`, `FakeRewardedAds` (luôn sẵn sàng, trả kết quả sau 1 giây; chỉ dùng trong editor và test), `NoAds`; tên ba chỗ quảng cáo là hằng số; trần theo ngày đã kẹp, lưu thêm vào save (vẫn bản 14).
- Ba chỗ, mỗi chỗ một nút phụ, chỉ hiện khi có gì để nhận: ×2 ở màn chào trở lại khi lượt miễn phí đã dùng (1/ngày; ×2 miễn phí và ×2 bằng cà rốt vàng giữ nguyên); thêm 1 lượt ở thẻ kết quả khi trò đã hết lượt (1/trò/ngày); "Xem để nhận 1 cà rốt vàng" ở hàng cà rốt vàng của Tiệm (2/ngày).
- Vé "Bỏ quảng cáo · 30 ngày" (`vuontra.noads.30`) ở thẻ mua cà rốt vàng, giá lấy từ cửa hàng, chỉ bán khi quảng cáo bật; có vé thì mọi nút thành "Nhận ngay", cùng trần.
- Công tắc duy nhất: `GameRunner.AdsInPlayerBuilds` (đang tắt). Mọi bản build cho người chơi, kể cả bản Mac, không có nút quảng cáo.
- Mục 5 trong docs/khai-bao-du-lieu-cua-hang.md.
- Nhịp chơi khi xem mọi quảng cáo (Logs/economy-ads.txt): cấp 5 lúc 7 phút 15 giây, cấp 10 lúc 8 giờ 27 phút → 8 giờ 30 phút, 12 ô đất ngày 3 không đổi; +14 cà rốt vàng/tuần; lượt gọi vàng 5 → 6.

Ảnh: docs/anh/po-66/66-di-vang-xem.png, 66-tiem-xem.png, 66-ve-bo-quang-cao.png, 66-them-luot.png, 66-them-luot-nhan-ngay.png

### Chưa đạt / cần PO quyết
- Có quảng cáo hay không; ba chỗ và trần; giá vé và 30 ngày.
- Lượt gọi bằng cà rốt vàng của người chơi xem mọi quảng cáo chạm đúng mức trần 6/tuần; nếu muốn chừa khoảng trống thì giảm quà ở Tiệm còn 1/ngày.
- Mốc 12 ô đất chỉ cách vạch ngày 3 bảy phút ở cả ba cách chơi (có từ trước, không do quảng cáo).
- Chưa cài SDK LevelPlay, chưa có hộp ATT/GDPR; chạm vào cối/giỏ/hũ khi hết lượt chưa mời xem thêm lượt.

## #62 — Mưa và Thỏ Ếch (agent song song, nhánh agent-62)

**Test:** 919/920 (1 test mùa vụ Inconclusive như trước), thêm 17 test (9 `WeatherTests`, 7 `RainScreenTests`, 1 `GardenPacingTests`). `tools/verify.sh` → RESULT GREEN.
**Commit:** f213bce, merge 325e22d. Main sau gộp: 993/994; sau khi gộp cùng #64 và #66, các mốc nhịp không đổi.

**Đã làm:**
- `Core/Weather.cs`: mỗi ngày 2 cơn mưa 8 phút theo hạt giống ngày (cơn 1 trong 6:00–14:30, cơn 2 trong 14:30–23:00, không chồng); `FrogRabbit`: chỉ trong mưa, mỗi cơn một bé, ở 60 giây, chạm = 10 phút thu nhập (× thu nhập × tốc độ) + 1 cỏ xác suất 1/4, tối đa 2/ngày, lùi đồng hồ không thêm, chờ cấp vườn 15 và xong hướng dẫn.
- Save giữ bản 14, thêm khối `"frog"` (save cũ: chưa gặp bé nào).
- Màn vườn: màu mưa (ngày xám xanh, đêm xanh đậm), 90 vạch mưa xiên, giọt trên ao, đèn lồng sáng hơn 20 %. "Giảm chuyển động" tắt vạch và giọt, giữ màu. Thỏ nghỉ ở thảm và võng ngồi dưới gốc cây ven rừng (chỉ đổi chỗ vẽ). Thỏ Ếch = Thỏ Bông đội Mũ len nhuộm xanh có hai mắt, bước ra từ cổng trăng nhỏ cạnh ao.
- Âm thanh: tiếng mưa của bài "Mưa" (#63) thành hàm chung, mưa thì thay nền ambient, theo thanh Âm thanh.
- Thành tựu `ech-5` "Gặp Thỏ Ếch 5 lần" = 2 cỏ.

**Nhịp (trước → sau, gặp đủ 2 ếch mỗi ngày):** GardenPacing thong thả cấp 5 6p03 / cấp 10 4g29 / cấp 15 35g06 không đổi. EconomySimulation thong thả cấp 10 8g27, 12 ô đất ngày 3 (48g07), trò chơi + đơn khách ngày 1 19,5 %, không đổi; gặp 10 ếch, +5 cỏ. Không nghỉ tay: cấp 10 5p35, 12 ô đất 9p11, không đổi.

**Ảnh:** `docs/anh/po-62/62-mua-ngay.png`, `62-mua-dem.png`, `62-mua-giam-chuyen-dong.png`, `62-tho-tru-mua.png`, `62-tho-ech-den.png`, `62-tho-ech.png`.

### Chưa đạt / cần PO quyết
- PO quyết: Thỏ Ếch chỉ đến từ cấp vườn 15. Từ cấp 1 nó kéo phần trò chơi + đơn khách ngày 1 xuống 9,5 %; từ cấp 8 hoặc 10 nó mở đủ 12 ô đất ngay ngày 2; từ cấp 12 thì giữ nhịp.
- PO quyết: mưa chỉ rơi từ 6:00 tới 23:00. Theo giờ thật chỉ có 16 phút mưa mỗi ngày, nên người chơi thong thả gặp ếch thưa, thành tựu 5 lần sẽ đến chậm.
- Tiếng mưa theo thanh Âm thanh (như issue), còn nền ambient vẫn theo thanh Nhạc.
- Chưa có mũ ếch vẽ (Antigravity); chưa có ảnh Thỏ Ếch trong mưa đêm; bài "Mưa" chưa tự bật khi trời mưa.

## #53 — Mở rộng rừng theo vòng (agent song song, nhánh agent-53)

- **Test:** 946/947 qua (1 inconclusive, 0 lỗi), thêm 29 test (ForestRingTests 18, ForestRingScreenTests 10, GardenLevelTests +1). tools/verify.sh: RESULT GREEN.
- **Commit:** 3041061, merge 30f2ce1. Main sau gộp: 1022/1023.
- **Đã làm:** Đất không còn mở theo đợt mà theo vòng rừng: R3 → 4 → 5 → 6 → 7, mỗi vòng thêm 2 ô trạm, 1 chỗ nghỉ và 6R + 6 ô lưới. Khi đủ điều kiện, biển "Mở rộng rừng" hiện ở mép bãi cỏ cạnh đất sắp mở; bấm thì vành rừng (#51) lùi ra, cỏ mới mọc từng búi ở chỗ cây vừa đứng và trên ô trống của vòng mới; Giảm chuyển động thì hiện ngay. Bảng tên đất trống và thẻ trong tab Trạm đổi thành "Mở rộng rừng · vòng N". Save vẫn version 14; save cũ đổi số ô đất thành R. GardenGrid chỉ thêm `EmptyTiles` và `NearestEmptyApart`.
- **Nhịp:** mọi mốc giữ nguyên (thong thả: vòng 1 phút 4, vòng 2 phút 11, vòng 3 giờ 4–8, vòng 4 ngày 2–3; không nghỉ tay: vòng 4 phút 9).
- **Ảnh:** docs/anh/po-53/ — 53-vong-1-truoc/giua/sau, 53-vong-2-truoc/sau, 53-vong-1-xa-truoc, 53-vong-2-xa-sau.

### Chưa đạt / cần PO quyết
- Giá vòng: giữ 25 % giá hai trạm (như đợt cũ). Giá 25 % một trạm làm vòng 1 mở lúc 1:33 nhưng hỏng hai luật kinh tế (trò chơi + đơn khách 11,7 % < 15 %, R7 về ngày 2). Muốn rẻ hơn thì phải chỉnh lại kinh tế.
- Luật "R7 chờ ngày 3" mong manh: cấp 15 rơi lúc 32h35 hoặc 48h07 tùy seed. Cho R7 cần cấp 16 thì qua mọi seed, nhưng đổi bảng của issue.
- `ClampView` không đổi (vườn đã rộng hơn R7; vòng 6–7 phần lớn nằm ngoài bề ngang bãi cỏ dọc).
- Chỗ dạo của thỏ (#55) chưa lấy từ ô trống của lưới.
- Ba khóa chữ cũ (`land.expand`, `plot.title`, `moment.plot.title`) không còn dùng nhưng vẫn để trong L10nStrings.

## #54 — Chế độ Bố trí: kéo thả trạm sang ô trống, hoán đổi, cất vào kho (agent song song, nhánh agent-54)


- **Test:** 943/944 (0 hỏng; 26 test mới: 12 Core `LayoutTests`, 14 màn hình `LayoutModeTests`). `tools/verify.sh` GREEN, 157 khung xem trước.
- **Commit:** 3be8974, merge 582cd5b (sau #53). Main sau gộp: 1048/1049.

**Đã làm**
- Nút "Bố trí" (tổ ong 7 ô) ở đầu trái thanh tab, cạnh tab Trạm. Bật lên: HUD và thanh tab ẩn, mỗi ô trống có vòng nét đứt màu kem, dưới đáy có thanh với ô "Cất vào kho" và nút "Xong". Lần đầu hiện một dòng hướng dẫn.
- Giữ 0,3 giây trên trạm, chỗ nghỉ hoặc đồ trang trí để nhấc lên (to 1,06, có bóng); nó đi theo ngón và ô đích sáng vàng. Thả lên ô trống thì đặt, lên thứ khác thì đổi chỗ, ra ngoài thì trượt về. Giảm chuyển động thì đặt tức thì. Cối, tiệm và quầy không di chuyển.
- Cất vào kho: trạm giữ cấp, không sinh cà rốt, không làm hàng cho khay, khách không gọi món của nó, không tiếp sức trạm khác; thỏ ở đó về nghỉ. Đồ trang trí cất thì về kho trang trí cũ (#11). Lấy ra ở cuối tab Trạm, trạm đứng lên ô trống gần tâm nhất.
- Core: `GardenGrid.Move/Swap/Store/TakeOut`, `Place` từ chối trạm đang cất, `Settle` chừa trạm đang cất; `Core/Layout.cs` (Drop, Store, TakeOut, IsStored). Save vẫn v14, thêm danh sách "stored" trong khối "grid", chỉ ghi khi có. Mùa trăng mới thì kho trống lại.
- Bấm "Xong" sau khi có thay đổi thì dựng lại vườn để cảnh vật theo chỗ mới.

- **Ảnh:** docs/anh/po-54/54-bo-tri-bat.png (chế độ bật, vòng nét đứt), 54-dang-nhac.png (đang nhấc giỏ lá), 54-sau-hoan-doi.png (nồi mứt đổi chỗ với ấm trà), 54-sau-xong.png (vườn sau "Xong")

### Chưa đạt / cần PO quyết
- Ấm trà chỉ dời được, không cất được, để vườn không bao giờ về 0 thu nhập. PO có muốn cho cất không?
- Trạm mới mua vẫn về ô mặc định (#52), chỉ khi ô đó bị chiếm mới sang ô trống gần đó, chứ không phải "ô trống gần tâm nhất". Để sau khi gộp #53 mới quyết, vì #53 đổi cách mở đất.
- Cảnh vật cố định (đường đá, đèn lối đi) dựng theo chỗ trạm khi dựng vườn: sau khi dời, có đèn đứng trước trạm (ảnh 54-sau-xong). Lấy trạm ra từ tab Trạm thì cảnh vật chưa dựng lại ngay.
- Ô trống sát trạm có vòng đè lên mép đế trạm, vì ô cách nhau 150 đơn vị mà đế trạm rộng hơn.
- "+5 % khi trạm tiếp sức đứng kề trạm được tiếp sức" để làm issue sau.

## #58a — Tim thân thiết thành điểm, 10 tim cộng hệ số, luật ao câu (agent song song, nhánh agent-58a)


- **Test:** 1017/1018 qua, 0 lỗi (24 test mới), `tools/verify.sh` GREEN.
- **Commit:** `1ba7649`, merge `bd032e7`. Main sau gộp: 1072/1073.

**Đã làm**
- `Core/Fishing.cs`: 5 mồi, mỗi mồi hồi sau 24 phút; lùi đồng hồ không thêm mồi và không trả lại phút đã chờ. Một lần câu: phao nổi 2–6 giây, chìm 0,5 giây; chạm khi chìm được cá, chạm sớm phao nổi lại, không chạm mất mồi. 6 loại cá (1 / 3 / 5 / 8 / 12 / 30 điểm; 45 / 25 / 15 / 9 / 5 / 1 %), giỏ cá lưu trong save.
- Tim thành điểm, 10 tim (mốc 10 … 8.880 và 18.880). Tim cũ đổi đúng mốc. Vuốt +10 điểm/ngày. Câu thoại ở 3 tim, cỏ ở 5 tim. 10 tim tặng 3 cà rốt vàng một lần.
- Hệ số theo tim trong `Rabbits.Multiplier`, tới ×2,6.
- Thành tựu "Câu 50 con cá" (2 cỏ) và "Một bé thỏ đạt 10 tim" (3 cà rốt vàng).
- API `GameRunner` cho 58b; ô thỏ hiện "♥7" khi quá 5 tim; tấm thỏ hiện 10 tim vừa một dòng.

**Nhịp** (mỗi buổi thả hết mồi, chạm đúng 80 %, cho hết cá vào thỏ ở trạm thu nhiều nhất và vuốt nó mỗi ngày): cấp 5, cấp 10, cấp 15 và 12 ô đất gần như không đổi ở cả bốn người chơi. Tim cao nhất sau một tuần 3–4 (×1,2–1,3 ở một trạm).

### Chưa đạt / cần PO quyết
- Tim thứ 10 ở 18.880 điểm: đề xuất chỉ có 9 mốc cho 10 tim.
- Câu thoại 3 tim và cỏ 5 tim tới muộn hơn nếu chỉ vuốt (ngày 8 và ngày 38 thay vì ngày 3 và ngày 5).
- "Trò chơi + khách ngày 1 ≥ 15 %" giờ được đo bằng trung bình 17 hạt. Ở hạt cũ, chỉ một lần vuốt thỏ đầu tiên đã đẩy tỉ lệ từ 19,5 % xuống 7,2 %; ngay trong game hiện tại 6/17 hạt đã dưới 15 %.
- Đủ 12 ô đất ở ngày 2 trên 9/17 hạt, có từ trước #58: luật "chờ ngày 3" chỉ đúng ở hạt cũ.
- Bảng quảng cáo (#66) cho kết quả khác nhau giữa các lần chạy giống hệt (129 hoặc 132 thỏ).
- Phần 58b chưa làm.

## #67 — Bốn trạm mới sau Đài ngắm sao, đất là hai vòng rừng mới (67a + 67b, agent song song, nhánh agent-67a)

- Test: 1065/1066 qua (1 inconclusive, 0 lỗi), 17 test mới so với main (16 trong `FourStationsTests`, 1 trong `GardenPacingTests`); `tools/verify.sh` RESULT GREEN.
- Commit: 63091ac (67a), 69414ef (gộp main 1840323 cùng 67b), merge vào main 21748d8. Main sau gộp: 1089/1090.
- Đã làm:
  - Bốn trạm: Vườn hoa sen (`ho-sen`, tiếp sức, 3e14, tiếp sức Tiệm bánh trung thu và Đài ngắm sao), Lò kẹo mạch nha (`lo-keo`, pha chế, Làm bánh, 4,5e15, 60 giây, 1,2e10), Ruộng lúa nếp (`ruong-nep`, tiếp sức, 6,5e16, tiếp sức Lò kẹo và Trà sữa trăng), Thuyền trà đêm (`thuyen-tra`, pha chế, Pha trà, 1e18, 75 giây, 1,8e11). Growth 1,16–1,17. Mỗi trạm cần cấp vườn 16 / 18 / 20 / 23.
  - Đất: vòng rừng R 8 (cấp 16) cho hai trạm đầu, R 9 (cấp 20) cho hai trạm sau, giá ¼ giá hai trạm như #53. Bốn trạm đứng thành hàng phía trên tiệm trà và ao; vườn cao thêm 800 ở trên.
  - Bấm dòng trạm trong tab Trạm thì vườn cuộn tới trạm đó. Nhìn xa giờ thấy bãi cỏ tới tiệm trà; hàng trạm mới cần cuộn lên một chút.
  - Hình thật cho bốn trạm và hai món; món Lò kẹo và Thuyền trà hiện trong khay và trong đơn khách.
  - Nhiệm vụ "Cả 12 bé thỏ đều thức" và "Có 16 trạm", mỗi cái 5 cỏ.
  - Tiếng riêng, tên vi/en; save cũ nhận thêm 4 trạm cấp 0 mà không lệch dữ liệu nào; Mùa trăng mới đưa 4 trạm về 0.
- Nhịp (so với main): người thong thả không đổi ở cấp 5 / cấp 10 / R 7; mở trạm 13 ngày 5, trạm 14 ngày 6, trạm 15 ngày 7, trạm 16 ngày 9; cuối ngày 7 cấp 21 với 16 ô đất. Người chơi chăm hơn mở R 8 ngày 3, R 9 ngày 4, thu nhập cuối ngày 7 tăng khoảng ×94, gọi bằng cà rốt vàng 6 lần một tuần. Không mô phỏng nào tới cấp 30.
- Ảnh: `docs/anh/po-67/67-tram-moi-cho.png` (tab Trạm, bốn trạm chờ), `67-xa-truoc-vong-8.png` (nhìn xa, biển "Mở rộng rừng" trước R 8), `67-vong-moi.png` (hàng trạm mới ở đầu vườn), `67-xa-sau-vong-9.png` (nhìn xa sau R 9), `67-tram-moi-mo.png` (tab Trạm sau khi mở), `67-khay-keo.png` (món Lò kẹo trong khay).

### Chưa đạt / cần PO quyết
- Cấp vườn 16 / 18 / 20 / 23 thay cho 18 / 22 / 26 / 30 của issue; R 8 / R 9 hỏi cấp 16 / 20 thay vì 20 / 25 theo luật +5 của #53. Theo luật cũ người thong thả không mở được trạm 15–16 trước ngày 14 (đứng ở cấp 23 từ ngày 9).
- Hai nhiệm vụ mới chỉ thưởng 5 cỏ, bỏ 5 cà rốt vàng: có cà rốt vàng thì người chơi chăm hơn gọi bằng cà rốt vàng 7–8 lần một tuần, quá luật 6. Hiện là đúng 6.
- Thu nhập tăng mạnh vì hai trạm tiếp sức mới rẻ, được nâng cấp cao và nhân giá các trạm chúng tiếp sức (người chơi chăm: khoảng ×94 cuối ngày 7; người không nghỉ tay: khoảng ×600 sau 2 giờ). Nhịp đi theo cấp vườn nên các mốc gần như không đổi.
- Vườn hoa sen tiếp sức hai trạm một ở đầu vườn một ở cuối vườn, nên chỉ đứng gần Tiệm bánh. Luật "trạm tiếp sức đứng gần" được nới cho bốn trạm mới: gần ít nhất một trạm chúng tiếp sức.
- Nhìn xa không còn thấy cả vườn: thấy bãi cỏ tới tiệm trà, hàng trạm mới cần cuộn lên; nếu thấy cả vườn thì các trạm dưới cùng bị thanh tab che.
- Người thong thả không tới cấp 30 trong 14 ngày nên Mùa trăng mới đầu tiên không đến (đã như vậy trên main).

## #79–#82 — Bốn lỗi nhẹ QA hồi quy 23/11 (agent qa-nits-2)


- **Test:** verify GREEN, 1056/1057 (1 bỏ qua như trước), thêm 8 ca test.
- **Commit:** `1245a6b` (#79), `1d792c2` (#80), `e24be3c` (#81), `b5d8391` (#82) — nhánh `qa-nits-2`, merge `d1889cc`. Main sau gộp: 1098/1099.

- #79: tấm "Mừng bạn quay lại" vẽ trên màn tiêu đề, dòng "CHẠM ĐỂ BẮT ĐẦU" nằm dưới tấm; nút Nhận nhận chạm ngay.
- #80: số khách lỡ không vượt số khách còn lại hôm nay, bằng 0 khi vườn chưa có món để bán; qua ngày mới thì tính khách ngày mới, đồng hồ lùi không thêm khách.
- #81: viên loài · bậc trên tấm thỏ và viên bậc ở sổ thỏ co chữ để luôn một dòng, sao nằm trong viên (vi/en, chữ to).
- #82: nhãn cấp, nút "Xếp thỏ +" và mũi tên nâng cấp của trạm luôn vẽ trên đồ trang trí, ở mọi ô, kể cả sau khi bố trí lại.


- **Ảnh:** `docs/anh/qa-79/`, `qa-80/`, `qa-81/`, `qa-82/` (trước–sau, thêm ảnh cận cho #81, #82).

### Chưa đạt / cần PO quyết
- Lọ đom đóm (có thể cả giỏ lá, cối) không nằm trên lưới lục giác: chế độ bố trí cho đặt đồ trang trí ở ô 4,-3 cạnh lọ và đồ đó che nhãn "Đom đóm · còn N lượt". Ngoài phạm vi #82; cần quyết có giữ chỗ cho lọ trên lưới không.
- Nhãn tên thỏ trên trạm vẫn thuộc trạm; đèn đá cao đặt ô bên trái trạm có thể chạm mép nhãn (chưa đo).
- #80: thời gian vắng qua nửa đêm tính trọn cho ngày mới, không chia; khách đang chờ ở quầy lúc rời game không tính riêng.

## #58b — Tấm câu cá ở ao, nút Cho ăn, 10 tim (agent song song, nhánh agent-58b)


- **Test:** 1086/1087 xanh (tools/verify.sh RESULT GREEN; 194 khung ảnh). Mới: 9 test `PondScreenTests` (mở tấm, câu trúng rồi cá bay vào giỏ, chạm sớm, mất mồi, hết mồi và đếm ngược, giảm chuyển động, không mở khi Bố trí, ao nằm dưới Thỏ Ếch và hũ đom đóm, cho ăn tăng điểm và tim, 10 tim có băng rôn + 3 cà rốt vàng + huy hiệu, vuốt tới 10 tim) và 1 test chữ/nút ở `AccessibilityTests` (vi/en × chữ lớn). Tấm câu cá cũng được thêm vào các lần đo nút và chữ có sẵn.
- **Commit:** 16e0513, merge a901c01. Main sau gộp: 1112/1113.

Đã làm:
- Chạm mặt ao trăng mở tấm "Câu cá ở ao trăng" trượt từ dưới lên: 5 chấm mồi, số mồi, đếm ngược mồi tiếp theo, mặt nước có trăng, phao (vẽ bằng code) nhấp nhô 2–6 giây rồi chìm 0,5 giây. Chạm mặt nước lúc phao chìm thì được cá: hình cá thật hiện lên rồi bay vào giỏ, số cá trong giỏ chờ cá bay tới mới tăng. Chạm sớm thì phao nổi lại; không chạm thì mất mồi. Đóng tấm giữa chừng thì phao đứng yên, mồi còn nguyên.
- Bảng "Câu cá N/5" trên bờ ao. Ao nằm dưới mọi thứ đứng cạnh nó nên Thỏ Ếch và hũ đom đóm vẫn chạm được riêng; khi đang Bố trí thì ao không mở.
- Tấm thỏ có nút "Cho ăn" (300×100): cho con cá điểm cao nhất, ghi số điểm và tên cá ×số còn lại; cá bay tới hàng tim, "+N điểm thân thiết" bay lên, tim và thanh tới tim kế cập nhật ngay. Giỏ trống hoặc đủ 10 tim thì nút nằm phẳng và ghi lý do.
- Đủ 10 tim (do cho ăn hoặc vuốt): băng rôn có sẵn, 3 cà rốt vàng bay về ô của nó, ô thỏ trong tab Thỏ đeo huy hiệu "♥10".
- Giảm chuyển động: phao không nhấp nhô, khi chìm thì đổi màu vàng và nhỏ lại, luật thời gian giữ nguyên.
- Hình cá 1–6 và giỏ cá từ docs/anh/po-58 đã chép vào Art/Final và nối vào GameCatalog (thiếu hình thì hiện vòng tròn tô màu).
- Không đổi kinh tế hay phần thưởng.


- **Ảnh:** docs/anh/po-58/58-ao-vuon.png, 58-ao-cho.png, 58-phao-chim.png, 58-ca-bay.png, 58-ca-trong-gio.png, 58-cho-an.png, 58-cho-an-sau.png, 58-tim-10-bang.png, 58-tim-10-o.png

### Chưa đạt / cần PO quyết
- Cho ăn luôn dùng con cá điểm cao nhất; có cần bảng chọn từng loại cá không?
- Phao, mặt nước và trăng trong tấm đang vẽ bằng code; có cần vẽ tay không?
- Bảng "Câu cá N/5" nằm trên bờ trái ao, gần hũ đom đóm (chỗ trống duy nhất không đụng tiệm trà, cổng Thỏ Ếch và hàng cây); PO xem ảnh 58-ao-vuon.
- Đóng tấm khi phao còn ngoài nước thì không mất mồi; PO có muốn mất mồi không?
- 10 tim cần 18.880 điểm (mốc do 58a đặt), nên cảnh 10 tim rất hiếm khi chơi thật.
- Chưa có tiếng riêng cho câu cá; chưa chạy bản build trên máy thật.

## #54 (sửa tiếp) — Ô giữ chỗ cho lọ đom đóm, giỏ lá, cối, tiệm, quầy (agent-54r)


- **Test:** 1104/1105 (1 test mùa không kết luận do ngày chạy), verify GREEN; 6 test mới.
- **Commit:** 800bc82, merge a88c7f1. Main sau gộp: 1118/1119.
- **Đã làm:** Lưới vườn có thêm trạng thái ô "giữ chỗ" cho 10 ô dưới lọ đom đóm, giỏ hứng lá và bảng của cối. Chế độ bố trí không vẽ vòng chấm và không nhận đồ thả lên các ô này; việc đặt, dời, đổi chỗ, cất ra và mở rừng đều bỏ qua chúng. Save cũ có đồ trên ô giữ chỗ thì đồ được dời sang ô trống gần nhất khi tải. Giỏ hứng lá đứng cố định cạnh ô nhà của trạm Giỏ lá trà. Khi mở rừng, trạm nào có ô nhà trống được đặt trước, rồi mới tìm chỗ cho trạm bị chiếm ô, nên trạm mới vẫn cách các trạm khác. Tiệm và quầy đã có ô riêng, không có bảng. Không đổi kinh tế.
- **Ảnh:** docs/anh/po-54/54r-truoc.png (vòng chấm trên ô bảng lọ, đèn đá thả lên bảng lọ) · docs/anh/po-54/54r-sau.png (không còn vòng, đèn đá quay về chỗ cũ).

### Chưa đạt / cần PO quyết
- Giỏ hứng lá không còn đi theo trạm Giỏ lá trà khi trạm bị dời; nó ở yên dưới cây trà cạnh ô nhà (2,0). PO xem có ổn không.
- Mái tiệm trà chồm lên hai ô phía sau (2,-5) (3,-5); chưa giữ chỗ vì đồ ở đó đứng sau tiệm và không che gì.

## Bàn giao (22/11 ~17:40, mốc giữa chiều; mục mới hơn ở trên)

### Trạng thái
- main = origin/main (tới commit này, ngay sau `1b1907e`), cây làm việc sạch trừ phần Unity tự sinh (`Assets/AddressableAssetsData/*`, `LocalizationSettings.asset`, `Garden.unity`; `.agent/`, `.agents/`, `.github/`, `skills-lock.json`, `docs/danh-sach-duyet.md` để nguyên).
- `tools/verify.sh --build` xanh lúc 17:21 trên HEAD `1b1907e` (main sau khi gộp #6 + #11): seed, 113 ảnh preview, biên dịch, 662/663 EditMode (1 inconclusive có chủ ý trong SeasonTests), build Mac `Build/Mac/VuonTraTho.app` mở chạy 8 giây, Player.log không exception. Mốc trước: 16:10 trên `23f1a8a`.
- Chiều 22/11 (từ ~16:30) chủ project giao session này điều phối hoàn thiện mọi issue mở và trả lời ba điều: các session được đóng nhóm A trong `docs/danh-sach-duyet.md`; bảy issue `question` làm theo mặc định rồi duyệt sau; #2, #3, #8 làm bản code trước, hình vẽ sau. Phân việc: session này → #2, #3, #15, #42 phần code, #8 (kể cả băng rôn mùa + cổng trăng); B → #6, #11 (agent trong worktree riêng, gộp qua `agent-6` ff `6f2e1d3`); điều phối → đóng 15 issue nhóm A, mặc định + nhãn cần-chủ-project cho #7/#9/#39/#40, hình #8 và ba vòng hoa cổng trăng, đề xuất #1 (`docs/de-xuat-1.md`).
- Quy ước từ 22/11: không mở issue mới cho việc; dòng chưa có issue trong #16 thì comment đối chiếu vào #16, tick dòng, ghi mục ở file này. #49 mở trước khi có quy ước, để PO đóng hoặc gộp.
- Save v14 (`pounding`) cộng các khối thêm sau không tăng version: `leaves`, `fireflies`, `orders`, `playSeconds`, `visitorBought/Seen`, `dye`, `maxDay`, `seasonGreeted`, `photosSeason`, `decor`; save cũ mở lên được (test).

### Việc session này làm (sau mục 05:40)
- `tools/verify.sh` (`822a8e9`): một lệnh kiểm hết; `--build` thêm bản Mac và đọc Player.log; đỏ là thoát khác 0.
- Mở lại #43, #25, #27, #36 (session kia đã đóng) và gắn nhãn `chờ-PO-duyệt`: theo quy trình PO là bên đóng. Tạo hai nhãn `chờ-PO-duyệt`, `cần-chủ-project`.
- #17 phần làm được ngay (`691d3fa`): hồ sơ người chơi (tên vườn, thỏ đại diện, mã `THO-XXXXXX`), màn hồ sơ sau hàng đầu tấm Tạm nghỉ, lớp `IAccountService` + `LocalOnlyAccount`. Save v11.
- #46 Thang độ hiếm chung (`7bb0528`, issue mới): `Rarity` 6 bậc, mỗi bậc một số sao; tên bậc kèm sao ở mọi chỗ; dải sao trên mọi khung theo bậc.
- #47 Trần cấp trạm 500 (`283c820`, issue mới): mọi chế độ nâng và Nâng tất cả dừng ở 500; nút ghi "Cấp tối đa".
- #48 Sao của thỏ mỗi 10 cấp (`6b7b075`, issue mới): sao rỗng ☆ sau cấp ở ô lưới, tấm thỏ, nhãn trong vườn; băng rôn khi qua mốc.
- #26 phần còn lại (`a754d5f`): thẻ bùa ghi hiệu lực hiện tại → sau một cấp; thanh cấp bằng chấm.
- #32 hai ý đầu (`386be8e`): chạm ví mở bảng "Cà rốt đến từ đâu"; thanh Trà lực nói còn bao lâu và thở khi đủ.
- #31 hai ý (`ceca860`): viên ▲/▼ ở mép đếm trạm nâng được hoặc thỏ ngủ gật ngoài tầm nhìn, chạm bay tới; nút ấm trà về tâm vườn.
- #31 hai ý nữa (`57b1541`, commit gộp với quà liên kết #17 của session kia): chạm một lần ở zoom xa phóng vào; nhớ vị trí và zoom (save v12).
- #31 ý cuối (`3567671`): vuốt trái/phải trên tấm trạm sang trạm kế. #31 đủ năm ý.
- #22 hai ý còn lại (`1c59ccd`): vuốt xuống ở đâu trên tấm cũng kéo tấm khi danh sách ở đầu; vườn phía trên tấm chạm và kéo được. Còn các hộp giữa màn chưa thành tấm trượt.
- #23 ba ý còn lại (`9cff4c9`): tab không chọn rõ hơn; tấm trạm sát thanh tab; huy hiệu tab Trạm và tab Thỏ. Còn vùng an toàn trên máy thật.
- #32 hai ý còn lại (`40443d7`): chạm giữ ví xem số đầy đủ; cụm nút góc phải trên một tấm. Còn viền sáng khi tăng tốc.
- #24 hai ý còn lại (`3cebd43`): thang chữ cho màn vườn, khung ngoài, băng thông báo; khoảng cách thẻ dùng chung; biển "Widen the garden" hết tràn. Còn soi tràn toàn bộ (#33).
- #28 bốn ý còn lại (`33f9b5a`): thỏ nảy và ♥ khi chạm; cà rốt bay về ví, ví phồng khi nhận; ánh sáng loé khi nâng; vòng sao qua mốc. Còn chớp mắt/vẫy tai (cần hình), thỏ đi tới trạm, cảnh nền đung đưa.
- #22 ý cuối (`bdf2647`): tấm chi tiết thỏ, chọn đồ, chọn thỏ thành tấm trượt dùng chung (`BottomSheet`). #22 chỉ còn thử tay.
- #45 Giã bánh trăng (`7ce83c9`): trò chơi nhỏ đầu tiên theo đề xuất đã duyệt; save v14; hai thành tựu. Còn hình chày/bánh ba trạng thái.
- #28 phần code còn lại (`fcf3f64`): cây, đèn lồng, ao, đom đóm theo đàn chuyển động; thỏ đi dạo từ ghế ra lối và về; thỏ được xếp đi bộ tới trạm. Còn chớp mắt/vẫy tai (cần hình).
- #34 (`ab2aab2`): lớp đất (đất mòn, đá lát, thảm hoa, vệt trăng), bóng đổ dưới mọi đồ vật, tán cây viền trượt 0,9, trang trí có chủ đích quanh từng trạm với bồn hoa và đèn đá; cối giã dời lên trên ấm trà.
- #38 lõi lưu mây (`d88b60c`): phong bì có mã kiểm, `CloudSync` (khi nào đồng bộ, chọn bản, không tự ghi đè), sao lưu trên máy trước khi thay, tấm chọn bản, trạng thái + Lưu ngay + Khôi phục trong Hồ sơ, thời gian chơi trong save. Còn nhà cung cấp thật (#39/#40).
- Mốc trạm 25/100/300 (`f2ca812`, #16 + #49): dây đèn, đèn đá + biển vàng, vầng sáng; dấu ✦ theo bậc; băng rôn lên bậc.
- #41 phần chuẩn bị (`b9a7a62`): đích iOS/Android trong BuildScript + tools/build.sh, bundle id/version/số build, chỉ dọc, icon và màn hình chờ. Máy này không có module iOS/Android nên chưa build.
- #45 nối hình (`7ca9091`): cối trống, chày riêng, bột ba trạng thái, bánh méo. #37 nối hình (`a7897ba`): bảy món và đĩa gỗ trong khay.
- Chiều 22/11 chủ project giao điều phối hoàn thiện mọi issue: #2 + #3 bản code (`8438799`), #15 đổi tên app (`fb68262`); #42 phần code (`18c21d1`, chính sách bản nháp, xoá dữ liệu, liên hệ); #8 thỏ theo mùa bản code (`75f72fc`); #8 băng rôn mùa + trang trí cổng (`8062d34`); B làm #6 rồi #11; điều phối đóng nhóm A, mặc định cho #7/#9/#39/#40, #8 hình và đề xuất, #1 đề xuất.
- Hai chỗ chữ bị che (`ac9a9d0`, sau verify --build; verify thường xanh 618/618): thanh Trà lực ngắn lại (288, kết thúc trước tấm nút góc) nên "Tăng tốc!" đọc đủ; giỏ Hứng lá nhỏ lại (110×95), dời sang phải (StationSpot(giỏ lá) + (104, 130)) và biển của nó treo bên trái (240 rộng, một dòng), không còn đè "Mở rộng vườn" hay tên trạm; ảnh `docs/anh/po-37/37-khay-hinh.png` cập nhật.
- Nhãn tăng tốc đè số Trà lực (`994a396`, verify thường xanh 626/626): tên và đếm ngược mỗi bên một nửa hàng trên của thanh, cùng co chữ về một dòng (min 16/14); test 4 trường hợp vi/en × thường/chữ to đo hai ô không giao nhau và mỗi chữ vừa nửa của nó ở cỡ nhỏ nhất; ảnh `docs/anh/po-32/32-tang-toc-sau.png`.
- Biển "Đom đóm · còn N lượt" bị tán cây viền (lớp Canopy, vẽ trên vườn) che đuôi (`22fc554`, verify thường xanh 630/630): lọ đom đóm dời vào trong (PondSpot + (-5, -290)), biển kết thúc trước `GardenScreen.ReadableRight` = 1000; test 4 trường hợp vi/en × thường/chữ to đo biển nằm trọn trong màn (cách mép ≥ 24), không vượt 1000, chữ vừa một dòng ở cỡ nhỏ nhất; ảnh `docs/anh/po-32/32-lo-dom-dom-sau.png`.
- Mỗi issue có comment đối chiếu từng ý, nhãn `chờ-PO-duyệt`, mục riêng trong file này, ảnh trong `docs/anh/po-17`, `po-46`, `po-47`, `po-48`, `po-26`, `po-32`, `po-31`, `po-45`, `po-28`, `po-34`, `po-38`, `po-49`, `po-41`; #16 đã tick.

### Chưa đụng tới / còn mở
- Màn "lúc bạn đi vắng": tấm `Canvas/Away` đã có từ `ba1cd71`; PO chưa nói muốn thêm gì. Dòng #16 chưa tick.
- Còn mở vì cần chủ project: #4 (thử tay), #9 (tiền thật), #39, #40 (tài khoản Apple/Google, module và gói Play Games), #41 (module iOS/Android), #42 hai ô cửa hàng + địa chỉ chính sách và email hỗ trợ thật (`AppInfo`), #1 (đề xuất mùa trăng mới chờ duyệt), #7 (giữ luật ghép, phương án sao ghi trên issue), #5 (chụm trackpad Mac, ưu tiên thấp), #16 (hàng đợi).
- Chờ PO duyệt (chờ-PO-duyệt): #2, #3, #8, #10, #13, #15, #17, #19–#21, #29, #30, #33, #37, #38, #41–#46, #49, #6, #11, #44, #45 (xem `docs/danh-sach-duyet.md` cho nhóm B/C).

### Cần chủ project thử tay hoặc quyết
- Đọc bình luận và ảnh của các issue chờ-PO-duyệt, đóng hoặc gắn `PO-trả-lại` kèm ý chưa đạt (nhãn này chưa tồn tại; tạo khi dùng).
- Thử bản Mac mới nhất `Build/Mac/VuonTraTho.app` (tên cửa sổ giờ là Moon Rabbit Tea Garden): thỏ cầm dụng cụ nghề, thỏ nghỉ ngồi/nằm với mũ và tên, Hồ sơ mục Quyền riêng tư (Xoá dữ liệu của tôi), mùa (đặt đồng hồ máy vào tháng 9 hoặc 12 để thấy băng rôn, thỏ ghé tiệm, cổng trăng treo hình; đồng hồ không lùi được sau khi đã thấy ngày mới), kinh tế sau #6, trang trí vườn #11, cùng các mục đã liệt kê ở Bàn giao 16:00.
- Quyết: ứng viên 2 mỗi mùa (#8), kiểu mốc trạm (#49), #17 Unity Cloud hay theo #39/#40, #1 đề xuất, địa chỉ chính sách + email hỗ trợ (#42), nhóm tuổi và khai báo cửa hàng (#42), tên app giữ "Moon Rabbit Tea Garden" (#15).

### Lưu ý kỹ thuật mới
- Trước khi sửa file: xem `mcp list_sessions`/`git status` để chắc không có session khác đang viết cùng thư mục; không chạy hai Unity batch cùng lúc (khoá `Temp/UnityLockfile`).
- `UiKit.AddText` để chữ tràn dọc (`VerticalWrapMode.Overflow`); muốn best-fit co theo chiều cao thì đặt `verticalOverflow = Truncate` (xem `ItemPickerView` dòng bậc).
- Không gắn `Text` lên cùng GameObject với `Image`: Unity trả null; luôn tạo con "Text" (xem `UiKit.Stars`).
- Trong `GardenScreen` có thuộc tính `Profile` nên gọi lớp tĩnh phải viết `VuonTra.Core.Profile`.
- Kịch bản `PreviewShot` đổi về `vi` cuối phần tiếng Anh nhưng không dựng lại tấm, nên vài khung cuối trộn hai ngôn ngữ trong tấm trượt; không ảnh hưởng game.
- Lệnh test/seed/chụp/build như các mục Bàn giao trước; `tools/verify.sh` gom tất cả.
- Hai session code trên một checkout: luân phiên bằng tin nhắn; trước khi sửa xem `list_sessions` và `git status`; Unity ở worktree khác (projectPath khác) chỉ làm verify chậm, không chặn.
- `tools/verify.sh --build` cần Unity rảnh trên project này; `tools/build.sh mac|ios|android` cho bản chơi; thiếu module thì dừng với exit 2.
- `StationActorView`: bậc trạm là `SetRank` (đổi kiểu hình ở một chỗ); thỏ đi tới trạm là `Traveler` trong `GardenScreen` (ẩn thỏ ở trạm tới 2,4 giây, test nào xếp thỏ rồi soi ngay cần `Advance` hoặc `Motion.SetReduced(true)`).
- Mây: `GameRunner.Cloud` là seam (`ICloudSave`); `MemoryCloudSave` cho test/preview; không bao giờ thay save trên máy ngoài `ResolveSync`/`RestoreFromCloud`, và luôn `SaveStore.Backup()` trước.


## Đề xuất #1 — Mùa trăng mới (22/11, chưa code, chờ chủ project duyệt)

Bản đầy đủ: `docs/de-xuat-1.md`; đã đăng lên #1. Bốn điểm issue hỏi:

1. **Điều kiện mở:** cấp vườn 30 (không đợi mọi trạm cấp 500); mỗi mùa sau thêm 5 cấp, trần mùa 7.
2. **Giữ / mất:** giữ mọi thỏ, vật phẩm, cà rốt vàng, cỏ, thành tựu, album, hồ sơ; mất cà rốt, cấp trạm, ô đất (về 4), chỗ nghỉ (về 2), cấp vườn.
3. **Phần thưởng:** +25 % thu nhập mỗi mùa, cộng dồn tuyến tính, nằm trong `Modifiers.IncomeMultiplier`; quà mùa mới 3 cỏ, 5 cà rốt vàng, lần đầu một Thỏ Trăng Non bậc Tím.
4. **Save:** thêm `Season` (save cũ = 0), không tăng version; test đọc save cũ và mô phỏng mùa 1.

Cùng ngày: đề xuất #8 thỏ theo mùa ở `docs/de-xuat-8.md` với sáu hình ứng viên trong `docs/anh/po-8/`.
