# tram-am-tra — trạm ấm trà trên bếp than (mẫu trạm nhỏ)

| | |
|---|---|
| Tệp ra | `tram-am-tra.png` |
| Thay cho | `Garden25D/Stand/prop_am-tra.png` (bếp gần đen, nét 4,0 px trên màn) |
| Cao | 1,0 thỏ → **448 px** nguồn (art bible mục 15–16) |
| Pivot | Bottom Center |
| Công cụ | Antigravity `image-generator`, `_phong-cach.prompt.md` |
| Cắt | `python3 -I cong-cu/cat-theo-chieu-cao.py /tmp/agy-art/tram-am-tra.png tram-am-tra.png 448` |

## Prompt

```text
{STYLE}
Content: ONE object centered on a flat uniform dark green background (#2E5E49), nothing else, generous empty margin on all sides, image size 1024x1024. A cozy tea station: a round chubby teal-glazed ceramic teapot (#4FA79A, crescent moon motif in cream) sitting on a small round clay charcoal stove of warm lilac-grey stone (#A79FB0 / #6E6680), a few glowing carrot-orange coals (#FF9B4A) visible through the stove's front opening. The teapot lid is closed. The whole object is about as wide as it is tall. The stove is clearly lighter than black: mid-value stone.
```

## Kiểm
- Độ sáng trung bình thấp hơn lông thỏ ≥ 15 %, bếp không gần đen (mục 14).
- Viền ngoài 6–8 px ở nguồn 448 (đo bằng `dung-bang.py` hàm `outline_px`).
- Không hơi nước vẽ sẵn; hơi là Shuriken.
