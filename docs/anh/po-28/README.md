# #28 — lớp phủ chớp mắt và vẫy tai

Vẽ bằng Antigravity ngày 23/11/2026 (cách vẽ: [VE-BANG-ANTIGRAVITY.md](../VE-BANG-ANTIGRAVITY.md)). Chưa nhập vào `Assets`; code chép sang khi nối.

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

## Tai

Đang vẽ (máy chủ ảnh báo hết chỗ); xem commit sau.
