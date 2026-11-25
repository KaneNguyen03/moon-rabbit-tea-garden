# visual-critic-gate — spike 2.5D

Nguồn: nhánh `spike-3d` (`dfdc8b1`, không gộp), `Spike3DBuilder.BuildAndShoot`, 25/11/2026. Chủ project duyệt hướng 2.5D theo tấm này ("Approve", 26/11).

- `so-sanh-2d-25d.png`: trái là vườn 2D hiện tại trên main (sau #134/#135), phải là spike 2.5D cùng bộ hình vẽ.
- `spike-25d-vuon.png`, `spike-25d-tram.png`: khung 1080×1920 của spike.
- `spike-3d-lan-1-bo.png`: spike 3D hẳn lần 1 (khối cơ bản), hướng đã bỏ ("xấu xấu. 2.5D thôi").
- `tam-mau-vuon-ban-dau.png`, `tam-mau-tram-ban-dau.png`: tấm mẫu định hướng vẽ bằng Antigravity (AI) lúc còn tính 3D, giữ làm tham chiếu bố cục.

```
VERDICT: APPROVED (spike chọn hướng, kèm ghi chú)
1. ghi chú - lối đi là một cột thẳng, nhìn như thân cây dưới sàn; dùng mảnh lối cong #129
2. pass - hình 2D đứng thẳng trên đất, chân chạm đất, không trôi
3. pass - màu và viền mực của hình gốc giữ nguyên; bóng nhuộm tối, không đen
4. pass - không quầng, không sương
5. pass - không hiệu ứng vẽ chết
6. ghi chú - thỏ quá nhỏ so với bản 2D nên chưa là điểm nhìn chính; đèn lồng chưa hắt vũng sáng xuống cỏ
7. ghi chú - một phần ba trên khung là trời trống; spike chưa có HUD và nhãn
```

Các ghi chú được đưa vào tiêu chí nghiệm thu của các issue `2-5d` tương ứng.
