# Gate #146 vòng 4, phần design: bóng và viền thỏ

Bằng chứng: `bong-truoc-sau.png` (bóng cũ và mới dưới thỏ, trên cỏ #3D8369 và nền tối #2B2420), `clip-vien-gan.png` (khung hình clip Cream chụp batch qua gate ngày 27/11 bằng `RabbitRigBuilder.BuildAndCapture`, có tay vung xa thân và góc ba phần tư).

```
VERDICT: APPROVED
1. pass - không hở ở cổ, gốc tai hay vai khi tay và đầu chuyển động; nét viền mờ dần ở chỗ nối nên không có khối mực
2. pass - bbox mọi lớp giữ nguyên, rect và bone trong .meta không đổi
3. pass - viền ngoài thỏ dày khoảng 7 px ở nguồn 512 (cũ khoảng 3,5 px), ra khoảng 2–3 px trên màn hình như trạm; màu Ink #3A2A4D; tay gần ở góc ba phần tư giữ nét gốc vì lớp đó vẽ mờ
4. pass - không viền thừa; dải hồng tím ở gốc tai là bóng có sẵn trong bản vẽ
5. pass - không có hiệu ứng vẽ chết
6. pass - shadow_elip tâm Ink 60% (cũ 30%), đọc rõ trên cỏ; code cần để tint bóng là trắng
7. không áp dụng
```
