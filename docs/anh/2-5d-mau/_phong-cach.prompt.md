# Khối phong cách chung (dán vào mọi prompt hình 2.5D)

Người thiết kế sửa khối này một lần; mọi tệp `<tên>.prompt.md` chép nguyên văn nó vào chỗ `{STYLE}`. Chuẩn: `docs/art-bible.md` mục 13–18.

```text
STYLE: hand-drawn watercolor and gouache storybook game art, matching the style reference exactly (the cream bunny is the master: same soft ink line, same paint texture). Chubby rounded shapes, no sharp corners. Soft painterly shading in 2-3 gentle value steps, slightly bleeding watercolor edges, very subtle paper grain inside the shapes. Outline: soft dark purple ink #3A2A4D with slight natural width variation, about 1.5% of the object height thick, thinner inner lines; never black, never brown-red. Light comes from the upper left: lit side upper-left, shade lower-right. Colours: muted warm, from this palette only: grass #3D8369, moss #6FBF8A, deep leaf #2E5E49, wood #A8714F / #6E4A44, stone lilac-grey #A79FB0 / #6E6680, earth #B98C66 / #8A6352, glaze teal #4FA79A / #2F6E6C, small accents only in carrot #FF9B4A and blush #F6A5B8. Nothing brighter than cream #FFF4E0 except lantern paper. View: three-quarter view from above at about 35 degrees, so the top surfaces are visible. Do NOT draw: a ground shadow, a ground patch (unless asked), steam, smoke, sparkles, glow halos, falling leaves, text, labels, flat vector fills, glossy plastic highlights, sticker gradients, thick uniform icon outlines.
```

Lệnh chung (Antigravity, `docs/anh/VE-BANG-ANTIGRAVITY.md`). `style-ref.png` là bản chép của `docs/anh/2-5d-mau/_style-ref.png` (thỏ Kem, quầy trà sữa và đèn lồng sau vòng 7 của #146, cùng mật độ điểm ảnh):

```bash
mkdir -p /tmp/agy-art && cp docs/anh/2-5d-mau/_style-ref.png /tmp/agy-art/style-ref.png
agy --effort medium --print-timeout 540s --add-dir /tmp/agy-art --dangerously-skip-permissions -p='Use the image-generator subagent to create ONE image and save it as /tmp/agy-art/<TÊN>.png. Style reference: the file /tmp/agy-art/style-ref.png. <PROMPT>. After saving, reply only with the saved file path.'
```

Hình đứng vẽ một vật một ảnh, trên nền phẳng `#2E5E49`, rồi cắt bằng `cong-cu/cat-theo-chieu-cao.py ảnh-gốc.png ra.png <cao nguồn>` (khử viền, xoá lỗ kín, thu về đúng cao nguồn của art bible mục 16). Hình nằm đất có cách cắt riêng ghi trong tệp của nó.
