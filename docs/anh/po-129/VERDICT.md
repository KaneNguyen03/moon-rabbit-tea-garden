# visual-critic-gate — #129 phần vẽ (ô cỏ, 6 sprite rải, bộ lối mòn)

Bằng chứng: `preview_qa_sheet.png`, `loi-ghep-thu.png`, `bg_grass-lap-2x2.png`, `bg_grass_clover-lap-2x2.png`, `bg_grass_flowers-lap-2x2.png`.

Vòng 1 (nội bộ trước khi chấm): mép còn màu nền cũ #2E5E49 và vài trăm điểm đen thuần; mối nối lối đi lộ vạch sáng và lệch viền. Đã khử viền, gọt đầu cắt, làm đoạn thẳng lặp liền và hoà các đầu cắt vào mặt cắt chuẩn.

```
VERDICT: APPROVED
1. pass - các mảnh lối đi nối liền ngang (thẳng, ngã ba, thẳng, đầu) và dọc (xoay +90 độ), không vạch sáng, không bước lệch
2. pass - mọi đầu cắt nằm trên cùng dải hàng 21–24 / 206–210, ghi trong loi/noi.json
3. pass - viền mực tím, 0 điểm đen thuần; đất #C9B48A–#A88B63, hoa hồng #F6A5B8 theo art bible
4. pass - 0 điểm màu nền cũ trên mép mọi hình; trên nền tối không có quầng xanh (ô 400 %)
5. pass - không hiệu ứng vẽ chết vào hình
6. pass - tỉ lệ giữ chung một hệ số cắt; vật rải thấp, bóng tiếp đất đặt trong game
7. n/a - không có UI
```

Ghi chú: ở vùng chuyển của `loi_thang` viền mực hơi lởm chởm do hai lớp viền chồng sau khi trộn; không thấy ở cỡ hiển thị trong game. Ô cỏ hoa lặp liền (mép 4,0 so với 3,7 giữa hai cột kề) nhưng có đốm tối nhẹ lặp theo chu kỳ 512, nên giữ làm biến thể.
