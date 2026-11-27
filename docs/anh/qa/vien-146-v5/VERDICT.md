# Gate #146 vòng 5, phần design: tai, tay, nét

Bằng chứng: `clip-khung.png` (bốn khung của mọi clip Cream, chụp batch qua gate ngày 27/11), `clip-gan.png` (tay vung xa thân và tay trước bụng ở cỡ lớn, kèm góc nghiêng camera ở cỡ game), `den-va-quay.png` (cột đèn và quầy cùng cỡ, trên cỏ).

```
VERDICT: APPROVED
1. pass - đỉnh đầu chỗ tai nối là một nét mực liền, hết dải xám; tay trước bụng chỉ có nét trong mảnh, đầu vai không có nét nên tay mọc ra từ thân
2. pass - bbox mọi lớp giữ nguyên, khung hình clip không giật
3. pass - viền ngoài thỏ khoảng 7 px ở nguồn 512, nét trong giữ mảnh theo art bible mục 9; cột đèn viền mực 7 px như quầy
4. pass - không viền thừa; dải hồng tím gốc tai đã thay bằng màu tai
5. pass - không hiệu ứng vẽ chết
6. pass - bóng theo art bible mục 10 là phần code
7. không áp dụng
```
