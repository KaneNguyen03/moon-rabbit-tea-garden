# #28 — lớp phủ chớp mắt và vẫy tai

Mí mắt vẽ bằng Antigravity ngày 23/11/2026 (cách vẽ: [VE-BANG-ANTIGRAVITY.md](../VE-BANG-ANTIGRAVITY.md)); tai uốn bằng `uon-tai.py` từ chính hình loài. Chưa nhập vào `Assets`; code chép sang khi nối (Sprite, Bilinear, PPU 100, pivot giữa như `kind_*.png`).

## Mí mắt (mọi loài dùng chung)

Sáu kiểu, mỗi kiểu hai tấm 96×88, nền trong suốt:

| Kiểu | Dùng khi |
|---|---|
| `sleep` | chớp mắt thường, ngủ (cung úp ‿) |
| `happy` | vui, được vuốt (cung ngửa ⌒) |
| `lash` | chớp mắt có mi (mi ở mép phải tấm) |
| `half` | lim dim, buồn ngủ (mí che nửa trên con ngươi) |
| `flat` | nhắm thư thả |
| `squeeze` | nhắm tịt khi nhảy/giật mình (hình `<`) |

- `eyelid_<kiểu>.png`: mảng lông trắng xám (giữ đậm nhạt) + nét mắt mực tím. Tô bằng `SpriteRenderer.color = màu lông` (nhân màu): lông ra đúng màu loài, nét mực chỉ tối thêm chút.
- `eyelid_<kiểu>_line.png`: chỉ nét mực (và con ngươi của `half`), vẽ **không tô màu** ngay trên tấm kia (sortingOrder +1) nếu muốn nét đúng màu `Ink #3A2A4D` trên loài lông sẫm. Không bắt buộc.
- Mắt trái (mắt bên trái hình) lật ngang (`flipX`) với `lash` và `squeeze` để mi ở mép ngoài và thành `> <`.

### Vị trí: `eyes.json`

Toạ độ đo tự động trên `Art/Final/kind_<id>.png` (khung 320×343, gốc góc trên-trái, y xuống): tâm `x, y`, cỡ con ngươi `w, h`, và `fur` = màu lông ngay trên mỗi mắt (hai mắt có thể khác màu, ví dụ Thỏ Lùn, Thỏ Vá).

- Đặt mí làm con của sprite thỏ: `localPosition = ((x − 160) / 100, (171.5 − y − 1) / 100)` (PPU 100, pivot giữa; hạ 1 px cho mí phủ kín mép trên con ngươi).
- Rộng mí = `1.8 × w` px khung, tức `scale = 1.8 × w / 96` theo PPU 100 của tấm mí; cao theo tỉ lệ tấm.
- Tô `eyelid_<kiểu>.png` bằng `fur` của mắt đó.
- 56/75 loài có toạ độ. 19 loài là `null` (mắt không đo được tự động: nhìn nghiêng, mắt nhỏ, phát sáng, khác dáng): `tho-cup-kem, tho-dom, tho-kem-chay, tho-latte, tho-sim, tho-may, tho-reu, tho-gac, tho-dom-dom, tho-pha-le, tho-ba-duoi, tho-hang-nga, tho-mat-troi, tho-cuc-quang, tho-ngoc, tho-rong, tho-chom-sao, tho-den-ong-sao, tho-noel`. Với các loài này code bỏ qua chớp mắt (hoặc chỉ dùng hiệu ứng không cần hình) cho tới khi có người đo tay.

Gợi ý nhịp: mỗi 3–6 s ngẫu nhiên, hiện mí 0.12 s (`sleep` hoặc `lash`); vuốt thỏ thì `happy` 0.6 s; nhảy thì `squeeze`. Tắt khi giảm chuyển động.

`_thu-mi-mat.png`: thử `squeeze` và `lash` chồng lên Thỏ Bông, Thỏ Lùn, Thỏ Vá, Thỏ Cụp theo đúng cách trên. `_tam-mi-mat.png`: tấm gốc (thu nhỏ).

## Tai (Thỏ Bông, Thỏ Mè, Thỏ Cụp)

Ba loài mặc định của bậc 0–1 (`DefaultFor(0)` = `tho-bong`, `tho-me` cùng bậc 0, `DefaultFor(1)` = `tho-cup`). Mỗi loài thêm hai khung 320×343 cùng khung với `Art/Final/kind_<id>.png`; khung "tai bình thường" chính là hình loài sẵn có:

| File | Tai |
|---|---|
| `kind_<id>_ear_nua.png` | nghiêng nửa chừng |
| `kind_<id>_ear.png` | nghiêng hẳn |

- Thỏ Bông, Thỏ Mè: tai bên phải hình gập ra ngoài 30° (nửa chừng 15°). Thỏ Cụp: tai rủ bên trái hình khép vào má 11° (5.5°); kéo tai ra ngoài thì lộ má và vai vốn bị tai che, nên chỉ khép vào.
- Ngoài vùng tai, mọi điểm ảnh giống hệt hình gốc (đã so từng byte), nên chỉ việc đổi `sprite` của `SpriteRenderer`, không xê dịch gì; mũ (`HatX/HatY`) và mí mắt vẫn đúng chỗ.
- Nhịp gợi ý cho một lần vẫy: gốc → `_ear_nua` → `_ear` → `_ear_nua` → gốc, mỗi khung 0.07–0.09 s, cách nhau 5–10 s ngẫu nhiên, không trùng lúc chớp mắt. Tắt khi giảm chuyển động.
- Loài không có khung `_ear` thì bỏ qua vẫy tai (hoặc lắc cả hình 2–3° quanh chân như hiện có).
- Thêm loài: `python3 uon-tai.py <id> "<đa giác vùng tai>" <gốcX> <gốcY> <độ, dương = chiều kim đồng hồ> <dài tai> <ra.png> ["<đường viền má cần kẻ lại>"]`. Tai uốn dần từ 0° ở gốc tới đủ góc ở chóp nên chân tai không có đường cắt.

Tham số đã dùng (khung nửa chừng: nửa góc):

```bash
python3 uon-tai.py tho-bong "171,0 320,0 320,104 171,104" 200 100 30 95 kind_tho-bong_ear.png
python3 uon-tai.py tho-me "171,0 320,0 320,104 171,104" 200 100 30 95 kind_tho-me_ear.png
python3 uon-tai.py tho-cup "40,100 108,100 101,122 91,140 83,155 85,175 93,190 103,199 99,206 80,222 40,222" 100 118 -11 105 kind_tho-cup_ear.png
```

`_thu-tai.png`: ba loài × (gốc, nửa chừng, hẳn).

## Giới hạn

- Mí mắt: 19 loài chưa có toạ độ mắt (danh sách ở trên). Mí là một hình chung, nên loài mắt rất to hoặc nhìn nghiêng sẽ trông kém khớp hơn loài thân chung.
- Tai: mới 3/75 loài. Các loài tô lại màu từ thân chung không trùng khít đường viền với Thỏ Bông, nên mỗi loài cần chạy `uon-tai.py` riêng (cùng tham số thường đủ, nên kiểm bằng mắt).
- Antigravity báo hết chỗ (503) suốt buổi tối 23/11 cho tấm tai, nên tai không vẽ lại bằng AI; uốn từ hình gốc giữ đúng nét vẽ và thân hơn.
