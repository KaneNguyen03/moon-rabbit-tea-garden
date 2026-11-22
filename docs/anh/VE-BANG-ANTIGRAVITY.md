# Vẽ hình bằng Antigravity (Gemini) từ dòng lệnh

Máy đã đăng nhập Antigravity; lệnh `agy` (Homebrew) có subagent `image-generator`. Một tấm 6–8 hình mất khoảng một phút.

```bash
agy --effort medium --print-timeout 540s --dangerously-skip-permissions -p='Use the image-generator subagent to create ONE image and save it as /tmp/agy-art/sheet.png. Style reference: the file /tmp/agy-art/style-ref.png (soft storybook game props, chubby rounded shapes, thick dark purple-ink outlines, gentle painterly shading, no text). Content: a sheet of exactly 6 separate props arranged in a 3 columns x 2 rows grid with wide empty gaps between them, on a flat uniform dark green background (#2E5E49), nothing else. Each prop centered in its cell, roughly the same size, no shadows on the ground, no labels. Row 1: (1) ..., (2) ..., (3) .... Row 2: (4) ..., (5) ..., (6) .... Image size 1800x1200. After saving, reply only with the saved file path.'
```

- `style-ref.png` là bản chép của `docs/anh/po-do-vat/_tong-hop.png`.
- Cắt tấm thành từng PNG trong suốt (cạnh dài 512): `python3 docs/anh/po-do-vat/cat-luoi.py sheet.png thư-mục-ra CỘT HÀNG tên1,tên2,...` (tên theo thứ tự trái sang phải, trên xuống dưới). Lỗ kín trong hình (quai cốc, giữa vòng hoa) được xoá.
- Viết `-p='...'` liền với dấu bằng; `-p` tách rời sẽ nuốt cờ đứng sau làm prompt.

Đã vẽ 22/11/2026 theo cách này: `po-45/` (chày, bột, bột đang giã, bánh xong, bánh méo, cối trống), `po-37/` (7 món theo id trạm `product_<id>` và khay gỗ), `po-44/` (6 mũ `hat_*`).
