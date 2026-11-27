# quay-tra-sua — quầy trà sữa (mẫu quầy, tiệm)

| | |
|---|---|
| Tệp ra | `quay-tra-sua.png` |
| Thay cho | `Garden25D/Stand/prop_tra-sua.png` (nét nâu `#5E3E45`, 3,9 px trên màn) và là khuôn cho `prop_tiem` |
| Cao | 2,0 thỏ (đỉnh mái) → **896 px** nguồn |
| Pivot | Bottom Center |
| Cắt | `python3 -I cong-cu/cat-theo-chieu-cao.py /tmp/agy-art/quay-tra-sua.png quay-tra-sua.png 896` |

## Prompt

```text
{STYLE}
Content: ONE object centered on a flat uniform dark green background (#2E5E49), nothing else, generous empty margin, image size 1024x1280. A small wooden milk-tea market stall: a chubby wooden counter (#A8714F lit, #6E4A44 shade) with a scalloped striped awning in muted blush pink (#F6A5B8 desaturated) and cream; on the counter a glass jar of tapioca pearls, a short stack of cups and a wooden tray. The back of the stall is wood, not a light grey panel. Rounded soft beams, no straight hard CAD lines. The outline is purple ink, not brown.
```

## Kiểm
- Mái không sáng hơn lông thỏ; sọc hồng là điểm nhấn, không bão hoà.
- Khe giữa mái và quầy trong suốt (lỗ kín bị xoá khi cắt).
