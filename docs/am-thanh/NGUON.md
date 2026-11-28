# Nguồn các tệp âm thanh

Mỗi tệp âm thanh thu sẵn trong `Assets` có một dòng ở đây: tệp trong game, tệp gốc, gói, trang tải, tác giả và giấy phép. Chỉ nhận nguồn CC0 hoặc public domain có ghi rõ trong gói tải về. Thêm tệp mới thì thêm dòng ở đây trong cùng PR.

## Sáu khoảnh khắc chính (#178)

`SoundBank` (`Assets/Resources/SoundBank.asset`, dựng lại bằng `VuonTra > Seed Sound Bank` hoặc `VuonTra > Seed Content`) ánh xạ mỗi khoảnh khắc tới một tệp. Thiếu tệp thì game phát tiếng tổng hợp cũ trong `Sfx.cs`.

| Tệp trong game | Khoảnh khắc | Tệp gốc | Gói | Trang tải | Tác giả | Giấy phép |
|---|---|---|---|---|---|---|
| `Assets/VuonTra/Art/Final/sfx_goi-tho.ogg` | Gọi thỏ (cao lên 1 nửa cung mỗi bậc) | `Audio/Pizzicato jingles/jingles_PIZZI00.ogg` | Music Jingles | https://kenney.nl/assets/music-jingles | Kenney Vleugels (Kenney.nl) | CC0 1.0, https://creativecommons.org/publicdomain/zero/1.0/ |
| `Assets/VuonTra/Art/Final/sfx_len-cap-vuon.ogg` | Lên cấp vườn | `Audio/Pizzicato jingles/jingles_PIZZI07.ogg` | Music Jingles | https://kenney.nl/assets/music-jingles | Kenney Vleugels (Kenney.nl) | CC0 1.0, https://creativecommons.org/publicdomain/zero/1.0/ |
| `Assets/VuonTra/Art/Final/sfx_mo-dat.ogg` | Mở đoạn đất | `Audio/Pizzicato jingles/jingles_PIZZI05.ogg` | Music Jingles | https://kenney.nl/assets/music-jingles | Kenney Vleugels (Kenney.nl) | CC0 1.0, https://creativecommons.org/publicdomain/zero/1.0/ |
| `Assets/VuonTra/Art/Final/sfx_qua.ogg` | Nhận quà hoặc rương | `Audio/Steel jingles/jingles_STEEL09.ogg` | Music Jingles | https://kenney.nl/assets/music-jingles | Kenney Vleugels (Kenney.nl) | CC0 1.0, https://creativecommons.org/publicdomain/zero/1.0/ |
| `Assets/VuonTra/Art/Final/sfx_vuot-tho.ogg` | Vuốt thỏ | `Audio/cloth3.ogg` | RPG Audio | https://kenney.nl/assets/rpg-audio | Kenney Vleugels (Kenney.nl) | CC0 1.0, https://creativecommons.org/publicdomain/zero/1.0/ |
| `Assets/VuonTra/Art/Final/sfx_thu-khay.ogg` | Thu khay (bán hàng trên khay, mở hũ trăng) | `Audio/handleCoins2.ogg` | RPG Audio | https://kenney.nl/assets/rpg-audio | Kenney Vleugels (Kenney.nl) | CC0 1.0, https://creativecommons.org/publicdomain/zero/1.0/ |

Tải ngày 28/11/2026. Tệp `License.txt` trong mỗi gói ghi "License (Creative Commons Zero, CC0)"; ghi công Kenney không bắt buộc, ghi ở đây cho rõ.

## Cách xử lý

Mỗi tệp gốc được đổi sang mono 44,1 kHz, cắt khoảng lặng ở đầu và cuối, cắt tối đa 1,45 giây (mờ dần 80 ms nếu bị cắt), đưa RMS cả tệp về −20 dBFS và giới hạn đỉnh ở −4 dBFS bằng một bộ giới hạn nhìn trước 4 ms, Riêng tiếng đồng xu của thu khay được lọc thấp ở 4 kHz trước đó cho bớt chói. Sau cùng mã hoá lại Ogg Vorbis chất lượng 4 (7–13 KB mỗi tệp).

Mức sau khi mã hoá (đo bằng ffmpeg):

| Tệp | Dài | RMS | Đỉnh |
|---|---|---|---|
| `sfx_goi-tho.ogg` | 0,489 s | −19,9 dBFS | −8,4 dBFS |
| `sfx_len-cap-vuon.ogg` | 1,324 s | −20,0 dBFS | −8,2 dBFS |
| `sfx_mo-dat.ogg` | 0,526 s | −19,9 dBFS | −9,6 dBFS |
| `sfx_qua.ogg` | 0,555 s | −19,9 dBFS | −5,1 dBFS |
| `sfx_vuot-tho.ogg` | 0,460 s | −20,1 dBFS | −4,0 dBFS |
| `sfx_thu-khay.ogg` | 0,344 s | −21,8 dBFS | −3,8 dBFS |

Một tiếng chuông thuỷ tinh (`interface-sounds/glass_004.ogg`) từng được chọn cho quà, nhưng trọng tâm phổ của nó ở 7,3 kHz, gần hết năng lượng trên 4 kHz, nên được thay bằng đoạn trống thép trên trước khi gộp; `SoundBankTests` giữ mọi tệp dưới 4,5 kHz như các tiếng trạm.

Đề xuất trong #178 nói −16 LUFS. Đo LUFS cần khối 400 ms nên không đúng cho tiếng ngắn hơn thế (tiếng thu khay đo ra −70 LUFS vì bị cổng cắt hết), và đưa tiếng nhiều xung như đồng xu lên −16 LUFS sẽ vượt đỉnh −3 dBFS. Vì vậy mức chung là RMS cả tệp −20 dBFS, đúng phép đo `SoundMeter` mà `SoundBankTests` dùng. Game phát các tệp này ở `SoundBank.Volume` (0,45) nhân thanh Âm thanh.
