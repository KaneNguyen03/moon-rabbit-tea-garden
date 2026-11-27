# Bộ mẫu 2.5D (pha 1) — kiểm kê, chuẩn và prompt

28/11/2026, nhánh `KaneNguyen03/art-25d-samples`, Refs #146, #131, #137, #138, #163, #164, #168, #169.

Chủ dự án duyệt hướng 2.5D (đảo nổi, hình vẽ tay đứng thẳng có bóng) nhưng chê ảnh hiện tại: tối, đục, vignette nặng, lẫn phong cách (thỏ màu nước cạnh hình vector viền dày, đồ phóng to mờ), tỉ lệ lệch, đế trạm không ăn nhập. Thư mục này là pha 1: kiểm kê, chuẩn mới trong `docs/art-bible.md` mục 13–19, và prompt cho bộ mẫu để người thiết kế sửa và chạy lại.

## Tình trạng

| Phần | Trạng thái |
|---|---|
| Kiểm kê (bảng dưới, `truoc/hien-trang-ti-le.png`) | Xong |
| Art bible 2.5D: mục 8–12 từ backup `MoonRabbitTeaGarden-sua146`, mục 13–19 mới | Xong |
| Tấm "trước" `truoc/bang-truoc.png`, gate mốc `truoc/VERDICT.md` (REJECTED) | Xong |
| Prompt từng hình (`*.prompt.md`, khối chung `_phong-cach.prompt.md`, style ref `_style-ref.png`) | Xong, chờ người thiết kế duyệt |
| Vũng sáng đèn `vung-sang-den.png` (làm bằng script, không cần AI) | Xong |
| Công cụ cắt theo cao nguồn `cong-cu/cat-theo-chieu-cao.py`, làm ô lặp `cong-cu/lam-lap.py`, dựng tấm `cong-cu/dung-bang.sh` | Xong |
| **Hình mẫu vẽ bằng AI, cảnh 2.5D và tấm "sau"** | **Chặn**: Antigravity `image-generator` trả `RESOURCE_EXHAUSTED (429)`, hạn mức hồi sau khoảng 95 giờ tính từ 01:30 ngày 28/11 (khoảng 01:10 ngày 02/12). SpriteCook chưa nối MCP trên máy này. Không dùng dịch vụ trả phí mới (điều phối 28/11). |

Dựng lại mọi tấm: `PY=<python có Pillow, numpy> docs/anh/2-5d-mau/cong-cu/dung-bang.sh` (lấy hình từ hai nhánh backup, ảnh chủ dự án từ `OWNER_SHOT`).

## Kiểm kê hình vườn 2.5D

Nguồn hình: nhánh `backup/agent-146-wip` (`Assets/VuonTra/Art/Garden25D/Stand`, bản mới nhất của #146), `Art/Final`, `Art/Placeholder`, `Art/Ground`, rig Cream. Nét: viền ngoài đo trên màn 1080×1920 ở zoom mặc định (124 px/m), chuẩn 2–3 px (mục 9). Cao theo `GardenStands.asset`.

| Hình | Dùng cho | Nguồn | Cao | Nét màn | Sai so với art bible |
|---|---|---|---|---|---|
| Rig thỏ Kem (`Rabbit25D/Cream`) | thỏ | vẽ tay + AI, tách lớp, chuẩn | 1,15 m | 2,9 px | Chuẩn của bộ. Trên màn ~142 px, cần to hơn hoặc nền tối hơn để là điểm nhìn chính (VERDICT spike) |
| `prop_tra-sua` | quầy trà sữa | AI 26/11 (vòng 7) | 2,0 m | 3,9 px | Nét nâu `#5E3E45`, hơi dày; nguồn 512 cho 2 thỏ (thiếu mật độ: cần 896) |
| `prop_tiem` | tiệm | AI | 2,0 m | 3,4 px | Tấm lưng xám sáng chói (mục 12 tint 0,8), nguồn 512 |
| `prop_am-tra` (+ `_nap-mo`, `_ngam`) | trạm ấm trà | AI | 1,15 m | 4,0 px | Bếp gần đen, nét `#443B41` dày nhất nhóm AI |
| `prop_gio-la` (+ `_hai`) | luống chè | AI | 0,9 m | 1,9 px | Nét xanh-nâu `#4A503C`, mỏng; vẽ sẵn mảng đất dưới (lẫn với đế) |
| `prop_coi-gia-banh` | cối giã bánh | AI | 0,7 m | 1,9 px | Cục bánh trắng chói sáng hơn thỏ |
| `prop_noi-mut` | nồi hồng | vector placeholder | 1,15 m | 7,3 px | Phong cách vector, hồng bão hoà, nét dày nhất cảnh (#163) |
| `decor_tree` | cây viền đảo | vector placeholder | 3,0 m | 6,5 px | Vector 400×480 phóng lên 3 m: mờ, nét dày, cây kẹo mút lặp (#137, #163) |
| `decor_bush` | bụi | vector placeholder | 0,6 m | 3,4 px | Vector khối tròn chấm hồng (#137) |
| `decor_lantern` | đèn lồng cột | AI vòng 7 | 1,4 m | 2,4 px | Đạt nét; nguồn 280×512 thấp hơn mật độ (cần 672 cao); chưa có vũng sáng (#147) |
| `decor_den-da` | đèn đá | AI | 0,9 m | 2,2 px | Ánh xanh lá lạnh, trái mục 17 |
| `decor_mushrooms` | nấm | vector placeholder | 0,4 m | 2,2 px | Vector phẳng, đỏ bão hoà (#137) |
| `decor_stones` | đá | vector placeholder | 0,3 m | 3,2 px | Vector, tím xanh lạnh (#137) |
| `decor_bon-hoa` | bồn hoa | AI | ~0,5 m | — | Nét nâu `#62424C`; giữ tạm |
| `pad_dat`, `pad_vong-da`, `pad_san-go`, `pad_hoa` | đế trạm | AI, vẽ 3/4 | rộng ~1,7 m | 3,2–3,9 px | Vẽ 3/4 rồi nằm đất nên dẹt hai lần; độ sáng 140–177 lệch nhau, vòng đá chói (#169, #138) |
| `decor_pond` | ao | vector placeholder | — | 4,5 px | Vector, xanh dương bão hoà, nét xanh (#137) |
| `decor_fence` | hàng rào | vector placeholder | 0,6 m | 3,0 px | Vector (#137) |
| `bg_grass` | mặt đảo | AI + vá lặp (#129) | 3,4 m/ô | — | Chấm sáng dày đọc như nhiễu ở zoom mặc định (#168) |
| Vách đảo | mép đảo | màu đặc trong `GardenCameraSettings` | — | — | Không có texture; màu theo mục 8 thì đạt |
| `pathPiece` | lối đi | AI, dải thẳng | rộng 0,55 m | — | Thẳng như thân cây (VERDICT spike), cần mảnh cong |
| Rig giống khác (`kind_<id>.png`) | thỏ khác | 2D cũ | — | — | Viền nâu/xám, không rig (#164) |

## Chuẩn đã chọn (tóm tắt `docs/art-bible.md` 13–19)

- **Một phong cách:** màu nước + gouache sách tranh, nét mực mềm `#3A2A4D` như thỏ Kem, sáng trên-trái, 3/4 từ trên ~35°. Cấm vector phẳng, highlight nhựa, viền đen/nâu, bóng vẽ sẵn, hiệu ứng vẽ sẵn.
- **Nét:** 2–3 px trên màn, nhờ cùng mật độ 448 px nguồn cho mỗi thỏ chiều cao (viền 6–8 px nguồn cho mọi hình).
- **Màu:** bảng mục 2 + cặp sáng/tối cho gỗ, đá, đất, tán lá, gốm, giấy đèn, nước; chỉ thỏ, giấy đèn, thẻ UI được sáng tới kem; cà rốt/má hồng chỉ là điểm nhỏ.
- **Tỉ lệ (thỏ = 1 = 1,15 m):** trạm nhỏ 0,8–1,1; quầy 2,0; đèn cột 1,5; cây 2,8–3,2; bụi 0,7–0,9; đế rộng 1,6; lối rộng 0,5.
- **Hình nằm đất** (đế, lối, ao, vũng sáng, mặt đảo) vẽ thẳng từ trên, 300 px/m; mọi đế một họ cùng cỡ, cùng độ sáng.
- **Ánh sáng:** trăng trên-trái, bóng elip của engine, vũng sáng đèn `#FFC866` alpha tâm 45 %, vignette ≤ 0,2 màu Night.
- **Nhập:** Bilinear, không mipmap, cạnh bội số 4, tối đa 2048, khử viền.

## Bộ mẫu (pha 1)

| Hình | Prompt | Cao/cỡ nguồn | Trạng thái |
|---|---|---|---|
| Trạm ấm trà trên bếp | `tram-am-tra.prompt.md` | 448 | chờ vẽ |
| Quầy trà sữa | `quay-tra-sua.prompt.md` | 896 | chờ vẽ |
| Cây tán tròn | `cay-tron.prompt.md` | 1344 | chờ vẽ |
| Bụi hoa | `bui-hoa.prompt.md` | 360 | chờ vẽ |
| Đèn lồng cột + lớp sáng | `den-long-cot.prompt.md` | 672 | chờ vẽ |
| Vũng sáng đèn | (script) | 512×512 | **xong** `vung-sang-den.png` |
| Đế trạm (họ 4 kiểu) | `de-tram.prompt.md` | 552 tròn | chờ vẽ |
| Mặt đảo + vách đảo | `mat-dao.prompt.md` | 1024², 2048×256 | chờ vẽ |
| Lối đi cong | `loi-di-cong.prompt.md` | 768² | chờ vẽ |
| Thỏ Kem tỉ lệ mới | `tho-kem.prompt.md` | rig sẵn | không vẽ lại; tỉ lệ ở dữ liệu |

Khi có hình: cắt theo từng tệp prompt, dựng `preview_qa_sheet.png`, đặt vào cảnh 2.5D của #146 (`Stands25D` trong `PreviewShot`), chụp 1080×1920 qua `tools/shot.sh` bằng gate Unity, ghép tấm `sau/bang-truoc-sau.png`, chấm gate.

## Kế hoạch làm lại toàn bộ (sau khi mẫu được duyệt)

| Đợt | Issue | Hình | Số hình (ước) |
|---|---|---|---|
| 1 | #146 | Ba trạm mẫu theo mẫu mới: ấm trà (3 trạng thái), luống chè (2) + giỏ 3 mức + bó lá, quầy trà sữa, tiệm; đế 4 kiểu; lối cong + thẳng; mặt đảo + vách | ~18 |
| 2 | #131 | 11 trạm placeholder (`gio-la` đã có ở đợt 1 nên còn `noi-mut, coi-hoa, lo-banh, to-mat, binh-sao, gian-nho, tiem-banh, suoi-hoa, dai-sao` và bản 2.5D của `tra-sua`), 5 trạm `Art/Final` (`am-tra` đã ở đợt 1; `ho-sen, lo-keo, ruong-nep, thuyen-tra`) vẽ lại theo mật độ mới | ~14 |
| 3 | #137 + #163 | Cây ×3, bụi ×3, hàng rào thẳng + góc, nấm ×2, đá ×2, gốc cây, bàn trà gốc cây, ao có bờ (nằm đất), viền rừng 3 lớp, đồi xa; nồi hồng (#163) | ~18 |
| 4 | #138 | Ô đất xới, cọc dây (2 trạng thái), bao vật liệu, biển giá, đệm rơm (2 trạng thái), biểu tượng thỏ | ~8 |
| 5 | #164 | Lớp tách tai cụp, đốm xám, tím (trước + 3/4, mỗi giống ~10 lớp), đóng PSB | 3 giống (~30 lớp) |
| 6 | #147 | Đèn lồng cột (đợt mẫu), đèn đá ánh `#FFC866`, vũng sáng (xong), bóng elip (có) | ~3 |
| 7 | #148 | Trời ba lớp, dải rừng/đồi, vân giấy: soát lại theo mục 13–17, vẽ lại phần trái chuẩn | 0–4 |

Tổng khoảng 65–75 hình (chưa tính lớp thỏ). Với hạn mức Antigravity hiện tại (một tấm 6–8 hình mỗi phút khi còn hạn mức), các đợt 1–4 cần khoảng 10 tấm; giới hạn thật là hạn mức và vòng duyệt của người thiết kế, không phải thời gian vẽ.
