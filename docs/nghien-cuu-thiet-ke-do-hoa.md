# Nghiên cứu thiết kế đồ hoạ, UI/UX và chuyển động: Vườn Trà Thỏ Trăng

Ngày 25/11. Người viết: phiên nghiên cứu thiết kế. Phạm vi: vì sao game trông "mờ mờ ảo ảo", chuẩn của game cozy/idle di động đã hoàn thiện, đối chiếu với game hiện tại và lộ trình sửa. Tài liệu chỉ rút ra nguyên tắc, không chép giao diện hay tài sản của game nào.

Ảnh đã xem: `docs/anh/po-114/114-be-ca-vuon.png` (vườn chính), `docs/anh/po-vuon-2d/91c-0-doan-bien.png`, `91c-6-doan-tren.png`, `docs/anh/po-110/110-tam-tram-vi.png`, `110b-so-cong-thuc-vi.png`.

Đã biết, không điều tra lại: sương trắng trên đất chưa mở (`GardenScreen.BuildMist`) là nguyên nhân chính và đang được đổi sang sương đêm xám xanh ở luồng khác; hậu kỳ đã tắt; texture nhập không nén, không mipmap.

---

## 1. Tóm tắt 5 dòng

1. Ngoài sương, còn ít nhất năm lớp trong suốt phủ cả màn hình hoặc mảng lớn (vân giấy màu kem, cột sáng trăng alpha 0,9, quầng trăng, vũng trăng, vignette), cộng với đá lát và lối đi bán trong suốt. Chúng nâng giá trị độ sáng của mọi thứ lên vùng xám giữa, nên cả cảnh "đục" dù từng sprite vẫn sắc.
2. Cảnh không có điểm nhìn chính. Vùng sáng nhất và tương phản nhất là các nhãn viên thuốc kem ("Cấp 27", "Giã bánh · còn 3 lượt", "Xếp thỏ +"). Trên một màn hình có tới 9–11 nhãn, nên mắt bị kéo khỏi thỏ và trạm.
3. Nền cỏ là texture placeholder 512 px chỉ có các mảng loang tần số thấp (`Art/Placeholder/bg_grass.png`). Mắt đọc nó như ảnh bị mất nét, và đây là phần duy nhất cần vẽ lại sớm.
4. UI khá đúng hướng (thẻ giấy kem có gờ, nút cà rốt) nhưng thiếu hệ thống: font hệ điều hành khác nhau trên iOS và Android, nhãn tab mảnh, tấm nền HUD góc phải bán trong suốt, chữ trong thế giới chỉ cao 13–16 px.
5. Chuyển động có nhiều nhưng lại làm bằng `Mathf.Sin` trong C# (117 chỗ đã tính trong baseline ở `GardenScreen.cs`). Thỏ chưa có Animator hay clip `.anim`, dự án chưa cài DOTween, và hạt là hạt UI tự viết, không phải Shuriken. Có thể sửa phần "mờ" chỉ bằng code và giá trị màu trong 1–2 ngày. Phần "sống động" cần bộ rig thỏ PSB.

---

## 2. Chuẩn thiết kế

### 2.1 Đồ hoạ và độ dễ đọc

**N1. Tương phản giá trị sáng tối quyết định mắt nhìn vào đâu, không phải màu hay độ bão hoà.** Điểm nhìn chính phải là vùng sáng nhất hoặc tương phản nhất. Nền và đạo cụ phụ nằm ở dải giá trị trung bình đến tối. Trong cảnh tối, cái quan trọng là tương phản tương đối. Kiểm tra bằng cách nheo mắt, chuyển ảnh sang xám, hoặc thu nhỏ ảnh.
Nguồn: https://80.lv/articles/studying-character-art-silhouette-and-contrast · https://nastyrodent.com/color-theory-for-game-art/ · https://seeindie.com/blog/steam-capsule-art/

**N2. Cảnh tối cần bóng dáng rõ, ánh sáng có chọn lọc chỉ đường.** Thứ tự đọc là: giá trị sáng tối, rồi bóng dáng, rồi ánh sáng chọn lọc. Một chi tiết thiết kế riêng lẻ có thể biến mất sau lớp ánh sáng cuối cùng hoặc giữa đám lộn xộn.
Nguồn: https://ohmypuns.com/blog/how-game-artists-can-guide-players-through-darkness-without-breaking-immersion/

**N3. Phong cách hoạ sĩ vẫn sắc nếu giữ "vân mềm trong bóng dáng sắc".** Vân màu nước nằm bên trong các mảng lớn, còn đường bao ngoài phải gọn. Lớp gameplay không bao giờ được mơ hồ, và không khí chỉ đặt ở lớp nền. Viền lấy màu tối nhất của chính vật thể sẽ trông hài hoà hơn viền đen.
Nguồn: https://www.creativebloq.com/how-to/how-to-create-stylised-game-artwork · https://sunstrikestudios.com/en/blog/game_art_visual_direction/ · https://pixor.art/learn/outlines/

**N4. Lớp trong suốt chồng lên nhau làm đục ảnh và tốn fill rate.** Mỗi lớp toàn màn hình (nền, lớp phủ, gradient, vignette) bắt GPU tô lại mọi điểm ảnh thêm một lần. Điểm ảnh alpha 0 vẫn tốn, và Image có alpha 0 nhưng vẫn bật cũng tốn. Cách khuyên dùng là tắt GameObject hẳn thay vì để alpha 0.
Nguồn: https://gamedev.net/blogs/entry/2298719-a-unity-performance-pass-for-mid-range-phones-an-eight-point-review · https://researchandprogram.blogspot.com/2026/01/fill-rate-overdraw-transparency-in-unity.html · https://github.com/ken48/unity-overdraw-monitor

**N5. Game cozy ít lộn xộn.** Thông tin phụ nên ẩn bớt, giao diện nhấn vào việc người chơi đang làm, menu đơn giản với ít màu, và dùng biểu tượng thay cho chữ khi được.
Nguồn: https://sdlccorp.com/post/the-art-of-designing-intuitive-user-interfaces-in-cozy-games/ · https://www.isadeu.com/cozy-mobile-game-wireframe · https://pixune.com/blog/casual-game-art/

### 2.2 UI và UX

**N6. Tương phản chữ.** Chữ thường cần tỉ lệ ≥ 4,5:1. Chữ lớn (≥ 24 px, hoặc ≥ 18,66 px khi đậm) cần ≥ 3:1. Thành phần giao diện và biểu tượng cần ≥ 3:1. Chữ đặt trên ảnh hay gradient vẫn phải đạt các mức này.
Nguồn: https://www.w3.org/WAI/WCAG20/Understanding/contrast-minimum.html · https://webaim.org/articles/contrast/

**N7. Cỡ chữ trên điện thoại.** Xbox Accessibility Guidelines đo theo chiều cao thân chữ: 1080p di động khoảng 400 DPI cần chừng 72 px vật lý, tương đương 16–18 sp. Chữ phụ không nhỏ hơn khoảng 12–14 sp. Màn 1080×1920 của game này là đơn vị canvas, nên chữ phụ nên ≥ 24 đơn vị và chữ chính 28–36.
Nguồn: https://devdocs.xbox.com/build/game-principles/accessibility/xag-deep-dives/xag-101-text-display · https://h-idris.com/blog/game-ui-typography.html

**N8. Vùng chạm ≥ 44 pt (iOS) hoặc 48 dp (Android), cách nhau ≥ 8.** Trên canvas 1080 rộng, mức này xấp xỉ 88 đơn vị, và `UiKit.MinTouch = 88` (`UiKit.cs:100`) đã đúng chuẩn.
Nguồn: https://www.appetiteui.com/blog/touch-target-size-ios-android · https://buildwithaccess.com/blog/touch-target-size-mobile-accessibility-wcag

**N9. Mỗi hành động đều có phản hồi: hình, tiếng, và nếu được thì hạt.** Có thể làm "bài thử một nút": nếu một lần bấm kèm hoạt hình và âm thanh đã thấy đã tay, thì nền móng cảm giác game ổn. Nút bấm co xuống 0,9–0,96 rồi bật 1,05–1,1 và về 1.
Nguồn: https://gamemaker8.com/design/game-feel-juice-what-makes-controls-feel-good-why-matters-more-than-graphics/ · https://tigerabrodi.blog/juice-is-the-difference-between-a-game-that-feels-alive-and-one-that-doesn-t · https://valdemird.com/blog/game-feel-on-the-web/

### 2.3 Chuyển động

**N10. Thời lượng và easing.** Vật vào màn hình dùng ease-out khoảng 250–400 ms. Vật rời màn hình dùng ease-in khoảng 200 ms. Di chuyển trên màn hình dùng ease-in-out khoảng 300 ms. Vùng chuyển động lớn cần lâu hơn (đến 500 ms), vật nhỏ thì nhanh hơn (150–200 ms). Không dùng tuyến tính cho chuyển động trong không gian; tuyến tính chỉ dành cho thanh tiến độ và đồng hồ.
Nguồn: https://m3.material.io/styles/motion/easing-and-duration · https://m1.material.io/motion/duration-easing.html

**N11. Các nguyên tắc Disney áp vào UI.** Chuẩn bị (anticipation) kéo dài 100–200 ms với biên độ 10–20% động tác chính. Phần đi theo (follow-through) trễ 50–150 ms sau phần chính. Danh sách xuất hiện so le khoảng 50 ms mỗi mục. Không làm chậm người chơi khi họ đang muốn xong việc.
Nguồn: https://github.com/LottieFiles/motion-design-skill/blob/main/skills/motion-design/director/disney-principles.md · https://medium.com/design-bootcamp/disneys-12-principles-of-animation-in-everyday-ui-design-71c6592064fe

**N12. Nhân vật dùng công cụ hoạt hình gốc của engine.** Trong dự án này nghĩa là SpriteLibrary, SpriteResolver, SortingGroup, Sprite Skin và Animator (theo `.claude/skills/unity-2d-native-rigging`). Package `com.unity.2d.animation` 16.0.1 đã có trong `Packages/manifest.json`.
Nguồn: https://docs.unity3d.com/Packages/com.unity.2d.animation@latest

---

## 3. Đối chiếu game hiện tại

Ký hiệu: **[Ảnh]** là điều thấy trong ảnh chụp, **[Code]** là chỗ gây ra, **[N#]** là nguyên tắc bị vi phạm.

### 3.1 Màn mờ đục: các lớp phủ ngoài sương

**V1. Vân giấy toàn màn hình phủ một lớp kem lên mọi thứ.**
[Ảnh] 114-be-ca-vuon và 91c-6: cỏ, đá và cây đều như nhìn qua kính mờ. Màu tối nhất trên cỏ không bao giờ thật tối.
[Code] `PaperGrain.cs:54-55`: một nửa số điểm ảnh tô màu `Paper` (kem), nửa còn lại tô `Ink`, alpha tới `Strength = 0.14` (`PaperGrain.cs:9`). uGUI chỉ có hoà trộn alpha, không có Multiply. Vì vậy nửa kem nâng sáng mọi vùng tối, nghĩa là cả màn hình bị phủ một tấm kem 7–14%. Lớp này đặt cuối cùng trên vườn (`GardenScreen.cs:2329`).
Skill `cozy-watercolor-art` §2 yêu cầu Multiply hoặc Overlay, nhưng code thực tế là Normal blend. [N1, N4]

**V2. Cột sáng trăng gần như đặc.**
[Ảnh] 91c-6 và 114-be-ca-vuon có hai cột xanh trắng chạy dọc qua bãi cỏ, cắt ngang trạm. 110-tam-tram cũng có một cột trắng sau quầy.
[Code] `MoonbeamAlpha = 0.9f` (`GardenScreen.cs:189`), tạo ở `GardenScreen.cs:2648-2655` với kích thước 560×1500, nhấp nháy bằng `Mathf.Sin` ở `GardenScreen.cs:898`. Phía trên còn có `MoonPool` alpha 0,4 rộng 620×340 (`GardenScreen.cs:2646-2647`) và `MoonGlow` 1300×1300 alpha 0,26 ở góc trên phải (`GardenScreen.cs:2293`, `2319-2320`). Cả ba đều là màu xanh trắng, nên chúng kéo vùng quanh ấm trà về phía xám nhạt, đúng chỗ đáng lẽ phải tương phản nhất. [N1, N2]

**V3. Đá lát và lối đi bán trong suốt trông như lỗ sương.**
[Ảnh] Các hình bầu dục xám mờ quanh ấm trà và dọc lối đi (114-be-ca-vuon, 110-tam-tram) không đọc ra là đá mà như vệt sương rơi xuống cỏ.
[Code] Đá ở `GardenScreen.cs:1360` (Paper pha Grass, alpha 0,6) và `GardenScreen.cs:2590` (Paper pha Ink, alpha 0,5). Lối đất là các hình tròn alpha 0,32 chồng lên nhau ở `GardenScreen.cs:2576`. Vật đặc mà lại trong suốt thì không có bóng dáng. [N2, N3]

**V4. Ngân sách lớp phủ toàn màn hình.**
Hiện có các lớp: Daylight (`GardenScreen.cs:2299`, alpha 0 ban đêm nhưng vẫn bật, set màu ở `2359`), MoonGlow (`2319`), Vignette (`2321-2327`), PaperGrain (`2329`), Rain tint (`6796`, alpha 0,13–0,22 ở `6836`) và BoostEdge. Như vậy là 4–5 lần tô toàn màn hình, chưa tính sương và 25 chỗ dùng sprite `catalog.Glow`.
Chuẩn đề xuất: tối đa 2 lớp màu toàn màn hình bật cùng lúc, và lớp nào có alpha 0 thì tắt hẳn GameObject. [N4]

**V5. Nền cỏ là placeholder mất nét.**
[Ảnh] 91c-6: những mảng loang xanh lớn, mềm, không có chi tiết tần số trung bình. Bụi cỏ vẽ thêm lên trên thì sắc, nên càng làm nền trông như ảnh hỏng nét.
[Code] `Assets/VuonTra/Art/Placeholder/bg_grass.png` (512×512), lát ở `GardenScreen.cs:1314-1320` và `829`. Mức zoom mặc định 0,83 (`GardenScreen.cs:458`) không làm vỡ ảnh. Lỗi nằm ở nội dung texture. [N3]

### 3.2 Không có điểm nhìn chính: nhãn lấn át cảnh

**V6. Có 9–11 nhãn viên thuốc kem giống hệt nhau trên một màn hình.**
[Ảnh] 114-be-ca-vuon: "Mở lúc 19:00", "Giã bánh · còn 3 lượt", "Câu cá 4/5", "Đom đóm · còn 3 lượt", "Hứng lá · còn 3 lượt", "Bông", ba nhãn "Xếp thỏ +", và "Cấp 27/12/4/9". Chúng là những mảng sáng nhất cảnh (kem gần trắng trên nền xanh tối), cùng cỡ, cùng kiểu, cùng độ ưu tiên. Mắt không biết nhìn đâu trước, còn con thỏ (cũng màu kem) bị chìm giữa chúng.
[Code] Nhãn hoạt động: cối `GardenScreen.cs:1523`, giỏ `1554`, hũ `1585`, ao `8156`. Bảng cấp trạm `StationActorView.cs:221-235` (236×58, màu `Paper`). Thẻ "Xếp thỏ +" `StationActorView.cs:172-177`. [N1, N5]

**V7. Thanh tiến độ chu kỳ gần như không thấy.**
[Ảnh] Vạch cam dưới "Cấp 27" chỉ dày vài điểm ảnh, sáng hơn nền rất ít.
[Code] `StationActorView.cs:224` (cao 10, nền Ink alpha 0,28). Đây là phản hồi chính của vòng idle mà lại yếu nhất. [N9]

**V8. Ô trống viền nét đứt.**
[Ảnh] 110-tam-tram và 91c-0 có các vòng nét đứt trắng trên cỏ ở bên phải. Skill `cozy-watercolor-art` §3 cấm khung dây nét đứt. [N3]

**V9. Đạo cụ bị khoá trông như hỏng.**
[Ảnh] Hũ đom đóm (114, 110) trong mờ, nhợt, trông như lỗi tải ảnh hơn là "chưa mở". Cần một trạng thái khoá có chủ ý: bóng dáng tối với ổ khoá hoặc dấu "?", không dùng alpha. [N2]

### 3.3 UI: HUD, thanh tab, thẻ

**V10. Tấm nền 3 nút góc phải bán trong suốt.**
[Ảnh] Cả 5 ảnh: tấm đỡ máy ảnh, kính lúp và bánh răng có màu xám đục, khác hẳn hai thẻ kem đặc bên trái.
[Code] `GardenScreen.cs:3301` dùng `Palette.Alpha(Palette.Paper, 0.55f)`. [N1]

**V11. Nhãn tab mảnh và nhạt, không thấy tab nào đang chọn.**
[Ảnh] "Bố trí · Trạm · Bùa · Nhiệm vụ · Thỏ" là chữ thường, màu xám tím, cỡ 26. Ở màn vườn không có dấu hiệu tab nào đang chọn.
[Code] `GardenScreen.cs:3404` dùng `Palette.InkSoft`, không đậm, cỡ `TabLabelSize = 26` (`GardenScreen.cs:123`). Tương phản InkSoft trên Paper là 4,87:1, đạt tối thiểu nhưng trông yếu cạnh biểu tượng đậm. Cỡ ngang chữ thân (26) nhưng mảnh hơn. [N6, N7]

**V12. Chữ trong thế giới quá nhỏ.**
[Ảnh] "Xếp thỏ +" chỉ cao khoảng 13–15 px trên ảnh 1080, nhỏ hơn mức tối thiểu ở N7.
[Code] `StationActorView.cs:174-177`: `TextSmall = 22`, `resizeTextMinSize = 16`, rồi bị nhân thêm tỉ lệ trạm và zoom 0,83. [N7]

**V13. Font hệ điều hành.**
[Code] `UiKit.cs:22-23` dùng `Font.CreateDynamicFontFromOSFont` với "Avenir Next", rồi Helvetica, rồi Roboto. Hệ quả là iOS và Android hiện hai kiểu chữ khác nhau, không có font thương hiệu, và chữ tiêu đề không có độ tròn mềm hợp phong cách sách tranh. Text vẫn là uGUI Legacy, không phải TMP, nên không có viền hay đổ bóng SDF sắc. [N5]

**V14. Trạng thái khoá và trạng thái tắt.**
[Ảnh] 110b-so-cong-thuc: "Món chưa mở" và nút "Nâng · 180 lá" xám trên kem.
[Code] `Palette.cs:27-29`: `OffInk` trên `Off` chỉ đạt 1,96:1. WCAG miễn chuẩn cho nút tắt, nhưng chữ "Mở ở cấp trạm 100" là thông tin cần đọc (`RecipeBookView.cs:175`). Nên giữ chữ Ink và chỉ làm nhạt nền và biểu tượng. [N6]

**V15. Thẻ trạm có khoảng trống chết.**
[Ảnh] 110-tam-tram: nửa trái thẻ "Ấm trà sương" trống khoảng 200 px giữa dòng phụ và "Món: Trà sương · cấp 3". Chỗ này nên đặt hình trạm hoặc hình món ăn, theo quy tắc nhân vật trong thẻ UI dùng RenderTexture của skill `game-ui-ux`. [N5]

**V16. Những gì đã tốt, cần giữ.**
Thẻ kem có gờ, nút cà rốt nổi rõ (Ink trên Carrot 6,2:1), chấm đỏ đếm số trên tab, mức `MinTouch = 88`, sổ công thức xếp lưới 3 cột rõ ràng, nền mờ khi mở bảng (`Palette.Scrim`), và chế độ chữ lớn cùng giảm chuyển động (`UiKit.LargeText`, `Motion.Reduced`).

### 3.4 Chuyển động

**V17. Hoạt hình nằm trong code thay vì asset.**
`tools/conventions.baseline` ghi `procedural-animation` 117 chỗ ở `GardenScreen.cs` và 17 chỗ ở `StationActorView.cs`. Ví dụ:
- Dụng cụ trong tay thỏ lắc bằng `Mathf.Sin` (`StationActorView.cs:351-359`).
- Thỏ đi nhảy cóc (`GardenScreen.cs:5976`).
- Khách "thở" bằng scale Sin (`GardenScreen.cs:5581`).
- Ếch nhảy (`GardenScreen.cs:7030`).
- Đèn lồng lắc lư (`GardenScreen.cs:891`).

Kết quả là mọi thứ chuyển động cùng một kiểu sin đều đặn, không có chuẩn bị, không có squash/stretch, không có đi theo. Đó là cái cảm giác "máy móc". [N11, N12]

**V18. Thỏ chưa có Animator.**
Không có file `.anim` hay `.controller` nào trong `Assets/VuonTra`. Bản rig PSB nằm ở `../VuonTraNam-rig` (quyết định ngày 24/11) nhưng chưa vào game. [N12]

**V19. Chưa có DOTween.**
CLAUDE.md yêu cầu dùng DOTween, nhưng `Packages/manifest.json` và `Assets/Plugins` không có. Game đang dùng `Tweens` và `Ease` tự viết (`Ease.cs:7-110`). Bộ này đủ cho các easing cơ bản (`OutBack`, `OutElastic`, `InOutCubic`), nhưng `Ease.Pop` lại là `Mathf.Sin` (`Ease.cs:42`). Đây là quyết định chủ dự án phải chọn: cài DOTween, hoặc ghi rõ `Tweens` là công cụ chính thức.

**V20. Hạt là hạt UI tự viết.**
`FeelLayer.cs` có tối đa 90 hạt Image, và đom đóm cũng làm bằng code (`GardenScreen.cs:2397`). Quy tắc 3 của dự án đòi Shuriken cho hơi nước, bụi, lá rơi và lấp lánh.

**V21. Những khoảnh khắc thường thiếu ở game idle đã hoàn thiện** (kiểm tra theo ảnh; cần xác nhận trong game):
- Số cà rốt nhảy đếm lên khi nhận.
- Đồng xu bay từ trạm về HUD rồi HUD "nảy" khi đón.
- Lên cấp trạm có tia sáng và hạt.
- Mở món mới có màn lật thẻ.
- Thỏ nhàn rỗi có hành vi phụ như chớp mắt, gãi tai, ngáp.
- Bảng trượt lên với đi theo trễ 50 ms cho các hàng.

`FeelLayer.FlyTime = 0.55f` cho thấy đã có đồng bay. Phần còn lại cần kiểm tra bằng quay màn hình.

---

## 4. Định hướng thẩm mỹ (để soát mọi việc sau này)

**Câu định hướng:** *Một trang sách tranh màu nước dưới trăng: nền đêm sâu và yên, ánh đèn lồng ấm là nơi mắt dừng lại, mọi vật đáng chạm vào đều mũm mĩm, sắc nét và hơi nảy khi được chạm.*

**Năm tính từ:** ấm, yên, mũm mĩm, rõ ràng, sống động khẽ khàng.

**Nên:**
- Nền tối và lạnh (Night, Grass tối). Vật tương tác sáng và ấm hơn một bậc giá trị.
- Một điểm sáng nhất mỗi màn hình: ấm trà hoặc thỏ đang làm việc.
- Vân màu nước nằm *trong* hình. Bóng dáng ngoài sắc, viền Ink.
- Mỗi vật đứng trên đất có bóng tiếp đất Ink 25–30%.
- Chữ trên thế giới chỉ hiện khi cần (có việc, vừa thay đổi, hoặc được chạm). Còn lại dùng biểu tượng.
- Chuyển động: chuẩn bị, động tác, đi theo. Vào 250–400 ms ease-out, ra 150–200 ms ease-in.
- Mọi hành động có ba lớp phản hồi: hình, tiếng (lệch cao độ ngẫu nhiên) và hạt Shuriken.

**Không:**
- Không phủ lớp màu trong suốt toàn màn hình để "tạo không khí" (tối đa 2 lớp bật cùng lúc).
- Không dùng trắng hay kem làm sương, khói hay khoá. Sương đêm phải tối hơn cỏ.
- Không làm vật đặc bằng alpha (đá, lối đi, đạo cụ khoá).
- Không đặt nhãn cùng kiểu, cùng độ sáng cho mọi thứ.
- Không dùng sin đều đặn cho tay, chân hay dụng cụ trong C#.
- Không dùng font hệ điều hành.
- Không để nét đứt kiểu khung dây.

**Bài kiểm tra trước khi đóng một việc hình ảnh:**
1. Chuyển ảnh chụp sang xám rồi thu còn 25%. Điểm sáng nhất phải là thứ cần nhìn (N1).
2. Đếm số lớp toàn màn hình đang bật (N4).
3. Đếm số nhãn chữ trong thế giới. Từ 4 trở xuống là đạt.
4. Chạy cổng `visual-critic-gate` theo quy tắc 5.

**Mâu thuẫn tài liệu cần sửa:** `docs/art-bible.md` và skill `cozy-watercolor-art` §1 vẫn ghi "Fog / locked-area clouds: Paper #FFF4E0", trái với việc đổi sương sang xám xanh đêm. Cần sửa art bible trước (art bible thắng skill), rồi sửa skill. Mục §2 về vân giấy cũng cần ghi rõ cách làm trên uGUI (xem S1).

---

## 5. Danh sách sửa ưu tiên (top 10)

| # | Sửa gì | Hiệu quả mong đợi | Công | Cần vẽ? |
|---|---|---|---|---|
| 1 | **Vân giấy chỉ làm tối:** bỏ nhánh `paper` ở `PaperGrain.cs:54`, chỉ dùng `Ink`. Hạ `Strength` (`PaperGrain.cs:9`) từ 0,14 xuống 0,06–0,08. | Gỡ tấm kem phủ toàn màn hình. Vùng tối lại tối, cỏ và cây có chiều sâu. | S | Không |
| 2 | **Giảm ánh trăng:** `MoonbeamAlpha` 0,9 → 0,3, `MoonPoolAlpha` 0,4 → 0,18 (`GardenScreen.cs:189`). `MoonGlowColor.a` 0,26 → 0,12 (`GardenScreen.cs:2293`). Màu ngả ấm hơn về `Lantern` cho vũng sáng ở ấm trà. Bỏ nhấp nháy sin ở `GardenScreen.cs:898`. | Hết các cột trắng cắt ngang cảnh. Vùng quanh ấm trà trở thành điểm sáng ấm duy nhất. | S | Không |
| 3 | **Đá và lối đi đặc:** đá (`GardenScreen.cs:1360`, `2590`) dùng màu đặc tối hơn cỏ một bậc (ví dụ `Mix(Grass, Ink, 0.45)`, alpha 1), có thể thêm viền sáng 2 px phía trên. Lối đất (`GardenScreen.cs:2576`) gộp thành ít hình hơn, alpha ≥ 0,7. | Đá đọc ra là đá, hết vệt sương. Lối đi dẫn mắt về ấm trà. | S | Không (sau này nên có sprite đá) |
| 4 | **Dọn nhãn:** nhãn hoạt động (`GardenScreen.cs:1523`, `1554`, `1585`, `8156`) chuyển thành huy hiệu biểu tượng tròn 56 px kèm số ("3"). Chữ đầy đủ chỉ hiện khi chạm, hoặc khi lượt chơi vừa hồi. Bảng cấp (`StationActorView.cs:221`) thu về 160×46, nền `CardLip` hay `Ink` 85% với chữ kem, để kem chỉ còn ở thỏ và đèn. | Từ khoảng 11 nhãn còn khoảng 4. Thỏ và trạm thành chủ thể. | M | Không (biểu tượng đã có trong catalog) |
| 5 | **HUD đặc và thống nhất:** tấm góc phải `GardenScreen.cs:3301` dùng `Palette.Card` đặc có `CardLip`, giống hai thẻ bên trái. | Hàng HUD đọc thành một dải. Hết vệt xám đục ở góc. | S | Không |
| 6 | **Thanh tab:** nhãn `GardenScreen.cs:3404` dùng `FontStyle.Bold`, màu `Palette.Ink`. Tab đang mở thì chữ Carrot đậm. Cỡ giữ 26. | Thanh điều hướng rõ, có trạng thái. | S | Không |
| 7 | **Chữ trên thế giới đủ to:** "Xếp thỏ +" (`StationActorView.cs:172-177`) và chữ bảng cấp bù theo tỉ lệ zoom × trạm, để cỡ hiển thị ≥ 22 trên màn 1080 (min 20). Thay vòng nét đứt bằng chiếu cói hoặc vầng sáng đất như skill §3. | Đọc được không cần nheo mắt. Bỏ khung dây "chưa làm xong". | M | Vòng ô trống cần 1 sprite chiếu/vầng sáng |
| 8 | **Ngân sách lớp phủ:** tắt `_daylight` khi alpha 0 (`GardenScreen.cs:2359`) và `_rainTint` khi không mưa. Gộp MoonGlow vào Vignette thành một texture duy nhất. Tối đa 2 lớp toàn màn hình. Đo bằng overdraw monitor. | Ảnh trong hơn, đỡ tốn fill rate trên máy tầm trung. | S–M | Không (texture gộp có thể tạo bằng script) |
| 9 | **Thỏ dùng rig PSB và Animator:** đưa rig ở `../VuonTraNam-rig` vào game. Clip `Idle` (thở, chớp mắt), `Work_<nghề>`, `Hop`, `Doze`, `Cheer`. Gỡ dần `MoveTool` (`StationActorView.cs:340-360`) và hop sin (`GardenScreen.cs:5976`), hạ baseline `procedural-animation`. | Thỏ "sống" thật: có chuẩn bị, squash/stretch, đi theo. Đúng quy tắc 1. | L | Có (tách lớp PSB, đã có prototype) |
| 10 | **Nền cỏ mới:** vẽ lại `bg_grass.png` ở 1024 px liền mép. Chi tiết tần số trung bình (nét cỏ, đốm), giá trị tối hơn `Grass` một chút và biên độ sáng tối hẹp. Thêm bóng tiếp đất Ink 25–30% cho mọi trạm, cây và đèn. | Hết cảm giác "ảnh mờ nét". Đạo cụ đứng trên đất thay vì dán lên. | M | Có |

Ngoài top 10:
- (11) Font tròn bundle sẵn có đủ dấu tiếng Việt, thay `UiKit.cs:22-23`, rồi cân nhắc chuyển sang TMP. Công M, cần chọn font có giấy phép.
- (12) Trạng thái khoá dùng bóng dáng tối thay alpha (hũ đom đóm), và chữ khoá dùng Ink (`RecipeBookView.cs:175`). Công S.
- (13) Thẻ trạm đặt hình trạm qua RenderTexture vào khoảng trống. Công M.
- (14) Thanh chu kỳ dày 14–16, nền `Ink` 45% (`StationActorView.cs:224`). Công S.

---

## 6. UX: luồng và khoảnh khắc

**Mật độ thông tin trên màn vườn.**
Theo N5, màn vườn chỉ nên trả lời được ba câu hỏi: tiền bao nhiêu, cái gì cần chạm ngay, thỏ đang làm gì. Các bảng "còn 3 lượt" là thông tin phụ, nên chuyển sang huy hiệu số (sửa #4). Chấm "!" cam trên quầy (114) là mẫu tốt: một biểu tượng thay cả câu chữ.

**Điều hướng.**
Tab mở bảng dưới, bảng có nút đóng × to (110b) là đúng chuẩn. Cần thêm: tab đang mở có trạng thái rõ (sửa #6), và chạm ra ngoài bảng thì đóng (cần kiểm tra trong game).

**Phản hồi mỗi hành động** (bảng kiểm, kiểm theo bản quay):

| Hành động | Hình | Tiếng | Hạt |
|---|---|---|---|
| Chạm trạm | co 0,95 → 1,05 → 1 trong 180 ms | click, lệch cao độ ±5% | vài lá hoặc hơi |
| Nâng cấp | bảng cấp nảy và số đếm lên | chuông | tia sáng |
| Thu cà rốt | đồng bay 550 ms, HUD nảy | tiếng xu | lấp lánh ở HUD |
| Mở món hoặc đất mới | màn lật thẻ hoặc sương tan 600–800 ms | nhạc ngắn | cánh hoa |
| Thiếu tiền | lắc ngang 3 lần trong 250 ms, không phóng đại | tiếng trầm | không |

**Khoảnh khắc thưởng.**
Màn "Mới" ở bể cá (114) và sổ công thức đã có nhãn mới. Cần một "khoảnh khắc lộ diện" thống nhất cho mọi phần thưởng hiếm: nền tối 72% (`Palette.Scrim` đã có), vật phóng to theo `OutBack` 400 ms, vầng sáng xoay chậm, hạt Shuriken, rồi nút nhận. Dùng chung một view thay vì mỗi tính năng tự làm (`SummonMomentView`, `NewMoonMomentView`, `NewDishView` hiện tách riêng).

**Hướng dẫn ban đầu.**
Chưa có ảnh chụp `TutorialView` trong bộ ảnh đã xem, nên chưa đánh giá. Lần kiểm tới nên chụp 5 bước đầu.

---

## 7. Chuẩn chuyển động cho dự án (áp dụng theo quy tắc nhà)

| Loại | Công cụ | Thời lượng và easing |
|---|---|---|
| Thỏ, khách, ếch (thân, tay, tai) | Rig PSB, SpriteLibrary/Resolver, Animator, clip `.anim` | Idle 2–3 s lặp, chớp mắt ngẫu nhiên mỗi 3–6 s, làm việc 0,6–1 s mỗi nhịp |
| Đạo cụ trạm (cối giã, nắp nồi) | Clip `.anim` trên prop | Có chuẩn bị 100 ms, va 60 ms, đi theo 150 ms |
| Nút, thẻ, bảng, HUD | DOTween (đã cài từ #155, bản 1.3.045 ở `Assets/Plugins/Demigiant/DOTween`); code cũ còn dùng `Tweens`/`Ease` cho tới khi được chuyển | Nhấn 80–120 ms, nảy 180–250 ms `OutBack`. Bảng vào 300 ms ease-out, ra 200 ms ease-in. Hàng so le 50 ms |
| Số đếm | Tween giá trị | 400–700 ms `OutCubic`, chữ nảy 1,1 khi xong |
| Hơi, bụi, lá, lấp lánh, đom đóm | Shuriken (`ParticleSystem`) trên camera canvas | Đời hạt 0,6–2 s. Giới hạn số hạt theo cấp chất lượng |
| Đèn lồng lắc, cỏ đung đưa | Shader wind sway (skill `shader-programming`) hoặc clip `.anim` | Chu kỳ 3–5 s, lệch pha giữa các cái |

Mọi chuyển động phải tôn trọng `Motion.Reduced`: giữ màu và trạng thái cuối, bỏ chuyển động.

---

## 8. Lộ trình ba bậc

**Làm ngay** (code hoặc giá trị màu, công S, khoảng 1–2 ngày, không cần vẽ):
1. Vân giấy chỉ làm tối, 0,06–0,08 (`PaperGrain.cs:9`, `54-55`).
2. Giảm ánh trăng, quầng trăng, vũng trăng; bỏ nhấp nháy (`GardenScreen.cs:189`, `898`, `2293`).
3. Đá và lối đi đặc, tối hơn cỏ (`GardenScreen.cs:1360`, `2576`, `2590`).
4. Tấm HUD góc đặc (`GardenScreen.cs:3301`).
5. Nhãn tab đậm, màu Ink, có trạng thái chọn (`GardenScreen.cs:3404`).
6. Tắt lớp phủ khi alpha 0 (`GardenScreen.cs:2359`, `6836`).
7. Thanh chu kỳ dày và rõ hơn (`StationActorView.cs:224`).
8. Chữ khoá dùng Ink (`RecipeBookView.cs:175`).
9. Sửa art bible và skill: sương tối, vân giấy kiểu "chỉ Ink".

Sau đó chụp lại 114-be-ca-vuon và 91c-6, ghép ảnh trước/sau và chạy `visual-critic-gate`.

**Vòng tới** (công M):
- Dọn nhãn thành huy hiệu biểu tượng và chữ hiện khi chạm (#4).
- Chữ trên thế giới bù theo zoom (#7).
- Gộp vignette và quầng trăng, đo overdraw (#8).
- Font bundle (11).
- Thẻ trạm có hình (13).
- Một view "khoảnh khắc thưởng" dùng chung.
- DOTween đã cài (#155); còn lại là quyết định có chuyển code đang dùng `Tweens` sang DOTween hay không.
- Chuyển đom đóm và FeelLayer sang Shuriken.

**Cần vẽ thêm:**
- Nền cỏ 1024 liền mép (#10).
- Bóng tiếp đất và sprite đá lát.
- Chiếu cói hoặc vầng sáng cho ô trống (#7).
- Rig PSB thỏ vào game với bộ clip Idle/Work/Hop/Doze/Cheer (#9).
- Bóng dáng khoá cho các đạo cụ.
- Bộ biểu tượng huy hiệu cho cối, giỏ, hũ, ao, cùng nét viền Ink.

Mọi hình vẽ mới đi theo `docs/anh/VE-BANG-ANTIGRAVITY.md` và skill `cozy-watercolor-art`.

---

## 9. Nguồn

- 80.lv, Silhouette and Contrast: https://80.lv/articles/studying-character-art-silhouette-and-contrast
- Color Theory for Game Art: https://nastyrodent.com/color-theory-for-game-art/
- SeeIndie, thumbnail/squint check: https://seeindie.com/blog/steam-capsule-art/
- Guiding players through darkness: https://ohmypuns.com/blog/how-game-artists-can-guide-players-through-darkness-without-breaking-immersion/
- Creative Bloq, stylised game artwork: https://www.creativebloq.com/how-to/how-to-create-stylised-game-artwork
- Realism vs Stylization: https://sunstrikestudios.com/en/blog/game_art_visual_direction/
- Outline styles: https://pixor.art/learn/outlines/
- Unity performance pass, mid-range phones: https://gamedev.net/blogs/entry/2298719-a-unity-performance-pass-for-mid-range-phones-an-eight-point-review
- Fill rate, overdraw, transparency: https://researchandprogram.blogspot.com/2026/01/fill-rate-overdraw-transparency-in-unity.html
- Unity overdraw monitor: https://github.com/ken48/unity-overdraw-monitor
- Cozy game UI: https://sdlccorp.com/post/the-art-of-designing-intuitive-user-interfaces-in-cozy-games/
- Cozy mobile wireframe: https://www.isadeu.com/cozy-mobile-game-wireframe
- Casual game art: https://pixune.com/blog/casual-game-art/
- WCAG 1.4.3: https://www.w3.org/WAI/WCAG20/Understanding/contrast-minimum.html · https://webaim.org/articles/contrast/
- Xbox XAG 101 text display: https://devdocs.xbox.com/build/game-principles/accessibility/xag-deep-dives/xag-101-text-display
- Game UI typography: https://h-idris.com/blog/game-ui-typography.html
- Touch targets: https://www.appetiteui.com/blog/touch-target-size-ios-android · https://buildwithaccess.com/blog/touch-target-size-mobile-accessibility-wcag
- Game feel / juice: https://gamemaker8.com/design/game-feel-juice-what-makes-controls-feel-good-why-matters-more-than-graphics/ · https://tigerabrodi.blog/juice-is-the-difference-between-a-game-that-feels-alive-and-one-that-doesn-t · https://valdemird.com/blog/game-feel-on-the-web/
- Material motion: https://m3.material.io/styles/motion/easing-and-duration · https://m1.material.io/motion/duration-easing.html
- Disney principles in UI: https://github.com/LottieFiles/motion-design-skill/blob/main/skills/motion-design/director/disney-principles.md · https://medium.com/design-bootcamp/disneys-12-principles-of-animation-in-everyday-ui-design-71c6592064fe
- Unity 2D Animation: https://docs.unity3d.com/Packages/com.unity.2d.animation@latest
