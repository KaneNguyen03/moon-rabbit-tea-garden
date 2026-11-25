# #147 — đèn lồng, quầng, vũng sáng, bóng (ASSET-009 … 013)

Nguồn: `decor_lantern.png` vẽ bằng Antigravity (AI), 26/11/2026; các tệp còn lại dựng bằng script. Spec: `design/assets/specs/anh-sang-25d-assets.md`.

| Tệp | ASSET | Ghi chú |
|---|---|---|
| `decor_lantern.png` | 009 | cột gỗ cong, đèn giấy tắt; pivot chân |
| `decor_lantern_giay.png` | 009 | lớp giấy sáng hổ phách, tô từ chính vùng giấy của bản tắt nên chồng khít (không dùng bản "sáng" AI vẽ lại) |
| `vfx_quang-den.png` | 010 | quầng hướng tâm 256², tint Lantern trong shader cộng màu |
| `vfx_vung-sang-den.png` | 011 | elip 512×256 Lantern, alpha ≤ 0,45, nhiễu theo ô cỏ #129 |
| `shadow_elip.png` | 012 | elip 256×96 Ink, alpha tâm 0,3; thay `Art/Placeholder/shadow.png` |
| (có sẵn) `Art/Final/decor_den-da.png` | 013 | đèn đá đã có, không vẽ lại |

Bằng chứng: `preview_qa_sheet.png` (đèn tắt trên ca rô, tắt + lớp giấy trên nền đêm, quầng cộng màu, vũng sáng và bóng trên cỏ #129). Đo: 0 điểm màu nền cũ trên mép, 0 điểm đen thuần.

```
VERDICT: APPROVED (kèm ghi chú)
1. pass - lớp giấy sáng chồng khít lên đèn tắt, không lệch
2. pass - pivot chân cột đèn
3. pass - Lantern, Paper, Ink đúng art bible; đèn sáng hổ phách, không trắng xoá
4. pass - 0 điểm màu nền cũ trên mép; sạch viền trên ca rô
5. pass - không quầng vẽ chết vào đèn; quầng và vũng sáng là sprite riêng
6. ghi chú - vũng sáng hơi ngả vàng-xanh khi cộng lên cỏ; chỉnh cường độ và màu trong engine ở #147
7. n/a - không có UI
```
