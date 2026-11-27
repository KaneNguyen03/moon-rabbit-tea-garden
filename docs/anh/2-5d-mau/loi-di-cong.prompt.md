# loi-di-cong — mảnh lối đi cong (#129)

| | |
|---|---|
| Tệp ra | `loi-di-cong.png` (cung 90°, bán kính tâm 2 m), sau đó `loi-di-thang.png` (2 m) |
| Thay cho | `pathPiece` của #146 (lối là một dải thẳng, VERDICT spike: "như thân cây dưới sàn") |
| Cỡ | rộng lối 0,5 thỏ = 0,58 m → 174 px; cung trong ô **768×768** (2,56 m, 300 px/m) |
| Pivot | Center, nằm trên mặt đất; hai đầu lối chạm đúng giữa hai cạnh ô để ghép |
| Cắt | nền `#2E5E49`, vẽ thẳng từ trên: `python3 -I cong-cu/cat-theo-chieu-cao.py /tmp/agy-art/loi-di-cong.png loi-di-cong.png 768` rồi kiểm hai đầu nằm giữa cạnh |

## Prompt

```text
{STYLE}
Override the view: seen straight from above (top-down orthographic), no perspective.
Content: image size 1024x1024, flat uniform dark green background (#2E5E49). ONE quarter-circle curve of a narrow garden footpath of packed warm earth (#B98C66 centre, #8A6352 edges): it enters at the exact middle of the bottom edge and leaves at the exact middle of the right edge, constant width (about 17% of the image), soft irregular edges with little grass tufts creeping in, two or three flat rounded stepping stones (#A79FB0) set into it. No outline around the whole path, only soft ink on the stepping stones.
```
