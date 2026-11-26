# Giấy phép phông chữ

Game đóng gói phông của riêng mình trong `Assets/VuonTra/Fonts`, không lấy phông từ hệ điều hành (#142). Mọi phông dưới đây theo **SIL Open Font License 1.1** (OFL): được dùng, đóng gói và phát hành kèm phần mềm thương mại, miễn là giữ nguyên dòng bản quyền và bản giấy phép, và không bán riêng phông.

## Phông đang dùng

| File trong `Assets/VuonTra/Fonts` | Dùng cho | Nguồn | Bản quyền | Giấy phép kèm theo |
|---|---|---|---|---|
| `Nunito-Regular.ttf`, `Nunito-Bold.ttf` | chữ thân, nhãn, nút | `ofl/nunito/Nunito[wght].ttf` trong [google/fonts](https://github.com/google/fonts/tree/main/ofl/nunito) | Copyright 2014 The Nunito Project Authors (https://github.com/googlefonts/nunito) | `Nunito-OFL.txt` |
| `Baloo2-Regular.ttf`, `Baloo2-Bold.ttf` | tiêu đề và số lớn (cỡ từ `UiKit.TextTitle` = 36 trở lên) | `ofl/baloo2/Baloo2[wght].ttf` trong [google/fonts](https://github.com/google/fonts/tree/main/ofl/baloo2) | Copyright 2019 The Baloo 2 Project Authors (https://github.com/EkType/Baloo2) | `Baloo2-OFL.txt` |
| `NotoSansSymbols2-Subset.ttf` | ký hiệu ▲ ▶ ▼ ◀ ★ ♥ ✎ ✓ ✦ mà hai phông trên không có | `ofl/notosanssymbols2/NotoSansSymbols2-Regular.ttf` trong [google/fonts](https://github.com/google/fonts/tree/main/ofl/notosanssymbols2) | Copyright 2022 The Noto Project Authors (https://github.com/notofonts/symbols) | `NotoSansSymbols-OFL.txt` |
| `NotoSansSymbols-Subset.ttf` | ký hiệu → ♪ | `ofl/notosanssymbols/NotoSansSymbols[wght].ttf` trong [google/fonts](https://github.com/google/fonts/tree/main/ofl/notosanssymbols) | Copyright 2022 The Noto Project Authors (https://github.com/notofonts/symbols) | `NotoSansSymbols-OFL.txt` |

Không phông nào trong bốn họ khai "Reserved Font Name", nên bản đã cắt vẫn được giữ tên gốc.

## Các file này khác bản gốc ở đâu

Bản gốc của Nunito, Baloo 2 và Noto Sans Symbols là phông biến thiên (một file, trục `wght`). Văn bản uGUI của Unity không đọc trục biến thiên, nên `tools/fonts/build-fonts.py` cắt ra bản tĩnh:

- Nunito và Baloo 2: hai độ đậm 400 (Regular) và 700 (Bold), giữ nguyên toàn bộ ký tự và nét chữ.
- Baloo 2: đổi chiều cao dòng (`hhea` và `OS/2` typo ascender/descender) từ 1,078/−0,524 về 1,011/−0,353 cho bằng Nunito. Bản gốc chừa chỗ cho chữ Devanagari nên dòng cao 1,6 em; với số đó tiêu đề lệch khỏi tâm nút và chữ nhiều dòng giãn quá thẻ. Dấu tiếng Việt cao nhất của Baloo 2 Bold là 1,03 em, vẫn nằm trong dòng mới.
- Noto Sans Symbols và Noto Sans Symbols 2: chỉ giữ 11 ký hiệu game dùng, để file nhỏ (2 KB và 8 KB thay cho 373 KB và 1,2 MB); đặt `unitsPerEm` = 1250 để ký hiệu vẽ ở 80 % cỡ gốc (★ rộng 0,77 em thay cho 0,96 em, vừa các viên sao đã chừa 0,95 em mỗi sao), và dùng chung chiều cao dòng với Nunito.

Đây là "Modified Version" theo OFL; bản đã sửa vẫn theo OFL và giữ nguyên dòng bản quyền trong bảng `name` của phông.

## Tạo lại

Cần Python 3 và `fonttools` (đã thử với 4.63.0). Tải bốn file gốc ở cột "Nguồn" vào một thư mục rồi chạy:

```sh
python3 -I tools/fonts/build-fonts.py <thư mục chứa bốn file gốc> Assets/VuonTra/Fonts
```

Lệnh in SHA-256 của từng file. Với bản gốc tải ngày 26/11/2026, kết quả phải khớp bảng dưới (chạy hai lần cho cùng kết quả).

| File gốc | SHA-256 |
|---|---|
| `Nunito[wght].ttf` | `bb55a5ca5c2042335b3991af27c4d0705d0ef41cac6164ac737fd8f2a1e85207` |
| `Baloo2[wght].ttf` | `d47a6852548059b1db49a1319d06d499d546c3fa2237cf9eee9c43c8abb025c2` |
| `NotoSansSymbols2-Regular.ttf` | `7d5fb73b7ca67a6798101741f5d280a3d016a56a197afcd4199dbb57b4b82a21` |
| `NotoSansSymbols[wght].ttf` | `f7e7e04b4a24b6c78893d50cbfd2b2f6cae49617ab047bfef668d252adb128f7` |

| File đóng gói | SHA-256 |
|---|---|
| `Nunito-Regular.ttf` | `f77f63d124acdb5621049775de8a56fe8cca1dda264bb53926b4c3e95caf8bad` |
| `Nunito-Bold.ttf` | `9a580061c1993a96965a02550ba408092145b700ae8af4fa192e5c9ce6f28b76` |
| `Baloo2-Regular.ttf` | `4228515523dc8dc3cc7cf61f7ba9b6b0dc2ffcbee79788dce2b4154097f29ed1` |
| `Baloo2-Bold.ttf` | `af5e1bc40f8ab0592be327b8d9a619e73a04f68140bf7a92f569aa48a299d2e4` |
| `NotoSansSymbols2-Subset.ttf` | `2594aa68af80bc7801269b793ed7e5dc34ad5243d29740756a31952061760cd6` |
| `NotoSansSymbols-Subset.ttf` | `af32af64c83b8b952f766625ebf74ff26c264667148653ec9cf7af05d91c1de2` |

## Phông nối vào game thế nào

`ContentSeeder` gán `Nunito-Regular` vào `GameCatalog.BodyFont` và `Baloo2-Regular` vào `GameCatalog.TitleFont`. `GardenScreen.Build` đưa hai phông cho `UiKit.UseFonts`; `UiKit.AddText` chọn phông tiêu đề cho cỡ từ 36 trở lên, phông thân cho cỡ nhỏ hơn.

Bản Bold và hai phông ký hiệu được khai trong file `.meta` của mỗi phông (`fontNames` và `fallbackFontReferences`), để chữ đậm dùng nét vẽ thật và ký hiệu lấy từ phông đóng gói trước khi Unity tìm tới phông của máy. Các dòng này sửa tay trong `.meta`: gán qua `TrueTypeFontImporter.fontNames`/`fontReferences` rồi `SaveAndReimport` trên Unity 6000.6.10f1 không lưu lại.

`tools/fonts/check-fonts.py` (chạy trong CI, job `fonts`) đọc thẳng các file phông và báo lỗi nếu thiếu một chữ tiếng Việt nào trong 146 chữ có dấu, hoặc nếu bảng chữ `L10nStrings.cs` có ký tự mà không phông đóng gói nào vẽ được. Không kiểm bằng `Font.HasCharacter` của Unity vì hàm đó trả `true` cho mọi ký tự mà phông của máy vẽ được.

## Khi phát hành

Giữ ba file `*-OFL.txt` trong kho. Trang "Giới thiệu" hoặc mô tả cửa hàng nên ghi một dòng: "Phông chữ Nunito, Baloo 2 và Noto Sans Symbols, giấy phép SIL OFL 1.1". Game chưa có màn ghi công; việc này còn để ngỏ.
