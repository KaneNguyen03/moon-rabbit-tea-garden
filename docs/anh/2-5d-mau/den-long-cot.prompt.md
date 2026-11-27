# den-long-cot — đèn lồng cột (mẫu đèn, ASSET-009) và vũng sáng

| | |
|---|---|
| Tệp ra | `den-long-cot.png`, `den-long-cot_sang.png` (chỉ lớp giấy sáng, cùng khung) |
| Thay cho | `Garden25D/Stand/decor_lantern.png` (nét đã đạt 2,4 px; vẽ lại cho cùng tỉ lệ 1,5 thỏ và cùng mật độ) |
| Cao | 1,5 thỏ → **672 px** nguồn |
| Pivot | Bottom Center |
| Cắt | `python3 -I cong-cu/cat-theo-chieu-cao.py /tmp/agy-art/den-long-cot.png den-long-cot.png 672`; lớp `_sang` cắt cùng khung bằng mặt nạ vùng giấy |
| Vũng sáng | `vung-sang-den.png` 512×512 nằm đất, **đã làm bằng script** `cong-cu/dung-bang.py` (kind `light-pool`): tâm kem ra `#FFC866`, alpha tâm 45 %, bán kính 1,5 thỏ khi đặt (art bible mục 17). Không cần AI. |

## Prompt

```text
{STYLE}
Content: ONE object centered on a flat uniform dark green background (#2E5E49), nothing else, generous empty margin, image size 768x1280. A garden lantern post: a slender gently curved wooden post (#A8714F / #6E4A44) whose top curls into a hook, a round cream paper lantern (#FFF4E0 with soft #F2D8A8 rib lines and a small wooden cap) hanging from the hook at about two thirds of the post height. The lantern is drawn unlit: no glow, no halo, no light rays.
```

## Kiểm
- Đèn treo ngang tầm tai thỏ khi đặt cạnh thỏ Kem.
- Ở cảnh đêm: point light + `vung-sang-den` thấy rõ trên cỏ quanh chân (#147).
