# #148 — trời, trăng, sao, rừng xa, vân giấy, dải HUD (ASSET-014, 015, 017, 018)

Nguồn: `bg_trang.png` và dải `bg_rung-xa.png` vẽ bằng Antigravity (AI), 26/11/2026; `bg_troi.png`, `bg_sao.png`, `fx_van-giay.png`, `ui_dai-hud.png` dựng bằng script. Spec: `design/assets/specs/troi-hud-25d-assets.md`.

| Tệp | ASSET | Ghi chú |
|---|---|---|
| `bg_troi.png` | 014 | 1080×1920 gradient Night, chân trời pha tím nhẹ |
| `bg_trang.png` | 014 | trăng màu nước kem ngà, không mặt, không quầng ngoài mép |
| `bg_sao.png` | 014 | 512² lặp liền, sao chấm và vài sao 4 cánh, alpha trên nền trong suốt |
| `bg_rung-xa.png` | 015 | 2048 rộng, lặp ngang liền (trộn cuộn nửa chiều dài, mép lệch 0,5), đáy kéo dài một màu |
| `fx_van-giay.png` | 017 | 512² xám, lặp liền, chỉ làm tối 0–7 % (Multiply) |
| `ui_dai-hud.png` | 018 | 1080×260 gradient Night alpha 0,35 → 0 |
| — | 016 | cây và bụi viền đảo **chưa vẽ**: hạn mức ảnh Gemini hết (429) lúc 03:24 26/11, mở lại sau khoảng 4 giờ 45 phút |

Bằng chứng: `preview_qa_sheet.png` (ghép trời + sao lặp + trăng + rừng xa + dải HUD; rừng lặp 2 lần; vân giấy lặp 2×2 trên kem; trăng trên ca rô).

```
VERDICT: APPROVED (kèm ghi chú)
1. pass - sao, rừng xa và vân giấy lặp liền mép, không lộ đường nối
2. pass - các lớp nền tách riêng, không dính nhau
3. pass - Night, Paper, xanh rừng tối hơn đảo; không đen thuần
4. pass - trăng sạch viền trên ca rô
5. pass - không hiệu ứng vẽ chết
6. ghi chú - ASSET-016 (cây, bụi) còn thiếu, vẽ khi có hạn mức
7. pass - dải tối HUD chỉ ở mép trên, đủ để thẻ kem nổi trên trời
```
