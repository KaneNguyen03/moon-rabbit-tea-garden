# #168 — vườn đêm 2D sáng, ấm, trong

Phản hồi chủ project 28/11 "Đồ hoạ xấu quá". Chỉ code, vật liệu và cài đặt; không vẽ thêm hình.

## Đã đổi
| Chỗ | Trước | Sau |
|---|---|---|
| Vignette `bg_vignette` (`StreetArt.cs`) | `#0b0f2a`, alpha tới 0,62, ảnh ngang 256×144 kéo dọc | Night `#1B1E45`, alpha tới **0,22** (`SceneOverlays.VignetteStrength`), ảnh dọc 144×256, giữa màn trong hẳn; quầng trăng góc trên phải vẽ luôn vào ảnh này (0,10) |
| Lớp phủ toàn màn hình trên vườn | đêm: MoonGlow, Vignette, PaperGrain (3); ngày/mưa: thêm Daylight, Rain tint (4–5) | **chỉ Vignette + PaperGrain** mọi giờ, cả khi mưa. Màu theo giờ và màu mưa nằm ngay trên cỏ, dưới mọi thứ đứng trên vườn, nên thỏ và trạm không bị phủ |
| Cỏ `Art/Ground/bg_grass.png` | ảnh 1024 thu nhỏ ~0,4 lần không mipmap: chấm lấp lánh | mipmap + Trilinear; ô cỏ vẽ phủ 0,40 lên nền đặc `#3D8369` (`SceneOverlays.GrassTileStrength`) |
| Đèn lồng giấy và đèn đá | chỉ có quầng ở bóng đèn | thêm vũng sáng `#FFC866` alpha 0,45 nằm phẳng trên cỏ dưới chân đèn (sprite `glow`, cùng dáng tròn mềm với `vung-sang-den.png` của PR #171), đổi màu theo màu đèn người chơi treo |
| Trang web ngoài khung 9:16 | `#16120e` (hai dải đen) | Night `#1B1E45` |

`SceneOverlays` giữ ngân sách 2 lớp và các độ mạnh, để vườn 2.5D (#148: vignette ~0,2, ≤ 2 lớp phủ) dùng cùng số.

## Số đo (ảnh `tools/shot.sh` 1080×1920, đêm 22:00, trung vị từng ô, trung bình các ô cỏ đã mở)
| Vùng | Trước | Sau | Chuẩn |
|---|---|---|---|
| Cỏ đã mở, `115-phut-15-en` | `#42775C` HSL L36,4 S28,5 | `#3F7F64` HSL L37,2 S33,6 · HSV V49,7 S50,3 | `#3D8369` HSL L37,6 S36,5 · HSV V51,4 S53,4 |
| Lệch so với chuẩn | L −3 %, S −22 % | **L −1 %, S −8 %** (HSL); V −3 %, S −6 % (HSV) | ±10 % |
| Tâm vũng sáng đèn đá, `115-phut-15-en` (208, 1505) | `#3E6F59` | `#979262` | tint `#FFC866` |
| Cỏ trên web, Edge 158, ô mở | — | WebGPU `#3F7C61`/`#3F7D62`; WebGL 2 giống hệt | Editor `#3F7F64`: lệch < 2 điểm bão hoà |

Bản web: `tools/build-webgl.sh`, chạy trong Edge headless 390×844 @2×, múi giờ UTC (23:00, đêm). Lần WebGL 2 ẩn `navigator.gpu` và `getContext("webgpu")` trước khi trang tải; dòng `?debug` ghi `Renderer: WebGL 2 (fallback: no WebGPU in this browser)`. Ảnh `web-edge-webgpu-webgl2.png` (khung vườn có lớp tối của hướng dẫn bước 1/6, nên số đo lấy ở khung tiêu đề).

## Ảnh
- `115-phut-15-vi|en-truoc-sau.png` — đầu game.
- `2-garden-truoc-sau.png` (vi), `11-garden-en-truoc-sau.png` — vườn cấp 5.
- `91c-6-doan-tren|duoi-truoc-sau.png` — vườn đủ 6 đoạn.
- `200-co-en.png`, `200-den-da-en.png`, `200-den-giay-vi.png` — cắt 200 %.
- `web-edge-webgpu-webgl2.png`.

## visual-critic-gate
Chấm trên các ảnh sau và ảnh cắt 200 %; theo luật của việc này người làm không mở agent phụ, nên tự chấm bằng mắt theo đúng bảng kiểm.

```
VERDICT: APPROVED
1. pass - không có khớp nối mới; vũng sáng và vignette không có mép cứng
2. pass - vũng sáng nằm giữa chân mỗi đèn lồng giấy và đèn đá, cùng chỗ ở vi/en và mọi khung
3. pass - cỏ #3F7F64 trong ±10 % của #3D8369; vignette màu Night #1B1E45; vũng sáng #FFC866; không có đen thuần
4. pass - không quầng trắng hay xám; vignette và vũng sáng mờ dần mềm; cỏ hết chấm sáng lấp lánh ở zoom mặc định
5. pass - không vẽ hiệu ứng vào sprite; vũng sáng là một Image riêng
6. pass - vũng sáng làm điểm ấm dưới chân đèn; bóng đổ của đèn vẫn nằm trên vũng
7. pass - HUD, thanh tab, chữ không đổi; không gì bị cắt
```

Ghi chú, ngoài phạm vi:
- Dải đất chưa mở dưới trạm vẫn tối: đó là lớp tối của vùng khoá theo art bible §5, bố cục ở #169.
- Cột trăng giữ nguyên (cao hơn cỏ khoảng 5 điểm sáng).
- Đèn đá ở viền rừng (hiện ra khi mở đất) chưa có vũng sáng.
