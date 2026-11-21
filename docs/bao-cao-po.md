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
