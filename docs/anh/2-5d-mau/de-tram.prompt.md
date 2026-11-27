# de-tram — đế trạm nằm đất (mẫu họ đế, #169, #138)

| | |
|---|---|
| Tệp ra | `de-tram_dat.png` (đất nện viền đá cuội), sau đó cùng họ: `_go`, `_da`, `_hoa` |
| Thay cho | `Final/pad_dat`, `pad_vong-da`, `pad_san-go`, `pad_hoa` (vẽ 3/4 nên bị dẹt hai lần trên mặt đất; độ sáng lệch nhau: vòng đá sáng chói, đất tối) |
| Cỡ | rộng 1,6 thỏ = 1,84 m → **552 px**, ảnh **576×576** (art bible mục 16, 300 px/m) |
| Pivot | Center, nằm trên mặt đất |
| Cắt | ảnh vẽ thẳng từ trên xuống trên nền `#2E5E49`: `python3 -I cong-cu/cat-theo-chieu-cao.py /tmp/agy-art/de-tram_dat.png de-tram_dat.png 552` (đế tròn nên cao = rộng) |

## Prompt

```text
{STYLE}
Override the view: this one is seen straight from above (top-down orthographic), a perfect circle, not an ellipse, no perspective.
Content: ONE round ground pad centered on a flat uniform dark green background (#2E5E49), nothing else, generous empty margin, image size 1024x1024. A circular patch of packed warm earth (#B98C66 centre to #8A6352 near the rim) ringed by small rounded lilac-grey pebbles (#A79FB0 / #6E6680) and a few tufts of grass creeping over the edge. Soft watercolor texture, the rim softly outlined in purple ink. Mid value: clearly darker than cream, clearly lighter than night.
```

## Kiểm
- Bốn đế cùng họ: đường kính ± 10 %, độ sáng trung bình ± 8 %, cùng nét mép (art bible mục 18).
- Trong cảnh, đế đọc như elip đúng phối cảnh mà không bị dẹt thêm.
