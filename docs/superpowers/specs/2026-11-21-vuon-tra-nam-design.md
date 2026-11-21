# Vườn Trà Thỏ Trăng — bản thiết kế

Ngày viết: 21/11/2026. Trạng thái: chờ bạn duyệt. Tên game là tên tạm.

## Mục đích

Làm một game idle thư giãn cùng thể loại Cats & Soup, làm cho ra game thật chứ không phải bản thử: có nhiều lớp tính năng, có sự kiện, hình ảnh tử tế. Chủ đề, nhân vật và hình vẽ là của riêng mình (các bé thỏ trăng pha trà trong rừng đêm), không sao chép game gốc.

Những gì bạn đã nói: gameplay giống Cats & Soup; đồ họa và nội dung để mình tự quyết; phải có sự kiện; tính năng phải xịn; dùng Unity. Mọi thứ còn lại bên dưới là đề xuất của mình, bạn sửa chỗ nào cũng được.

## Mục tiêu, và những gì không làm

**Mục tiêu**

- Chơi được trên điện thoại ở màn hình dọc, mỗi lần mở vài phút là thấy tiến triển.
- Có đủ lý do để quay lại mỗi ngày: thu cà rốt lúc vắng mặt, điểm danh, sự kiện.
- Thêm sự kiện mới, trạm mới, bé thỏ mới mà không phải sửa code (chỉ thêm file dữ liệu).

**Không làm ở bản đầu**

- Quảng cáo, mua bằng tiền thật, tài khoản online, bảng xếp hạng, lưu trên mây.
- Bản iOS (cần máy có Xcode và tài khoản Apple Developer). Làm Android và bản chạy trên máy tính trước.

**Coi như thất bại nếu**

- Nhìn giống một bảng tính có nút bấm, không có cảm giác ấm cúng.
- Người chơi phải bấm liên tục mới tiến được. Đây là game để ngắm, không phải game bấm.

## Game gồm những gì

### 1. Vòng chơi chính

Các trạm tự làm ra đồ uống, đồ uống tự bán lấy **cà rốt**, dùng cà rốt nâng cấp trạm và mở trạm mới. Có hai loại trạm:

- **Trạm pha chế** (ấm trà, nồi mứt, quầy trà sữa…): làm ra món để bán.
- **Trạm nguyên liệu** (hái lá, giã hoa, lấy mật…): không bán gì, nhưng làm tăng giá các món của trạm pha chế. Đây là lớp chiến thuật: nâng cái nào trước thì lời hơn.

Mốc cấp 10, 25, 50, 100… cho thưởng nhân đôi. Vắng mặt vẫn thu cà rốt, có giới hạn số giờ.

### 2. Các bé thỏ

Mỗi bé thỏ là một nhân vật để sưu tầm, có độ hiếm và một kỹ năng riêng (ví dụ: làm nhanh hơn 20% ở trạm pha chế, hoặc tăng giá mứt). Bạn xếp bé nào vào trạm nào. Bé mới tìm đến vườn nhờ **cỏ bốn lá**, kiếm được qua nhiệm vụ và sự kiện. Nơ, mũ và phụ kiện để thay cho vui, có cộng thưởng nhỏ.

### 3. Sự kiện

Ba tầng, từ nhỏ tới lớn:

- **Chuyện xảy ra trong ngày** (vài phút một lần): đom đóm vàng bay ngang, mưa sao băng (chạm để nhặt), đêm trăng rằm (mọi thứ ×2 trong 10 phút), khách lạ ghé đặt hàng (giao đủ số trà trong thời gian cho phép thì có quà).
- **Điểm danh 7 ngày:** mỗi ngày vào nhận một phần quà, ngày thứ 7 quà lớn.
- **Sự kiện theo mùa** (kéo dài khoảng 7 ngày): mở ra một khu vườn phụ có trạm riêng và loại tiền riêng của sự kiện, kèm một thang phần thưởng. Leo tới đâu nhận tới đó; phần thưởng cuối là bé thỏ hoặc trang phục chỉ có trong sự kiện. Sự kiện đầu tiên mình đề xuất: **Lễ hội Trăng Rằm**.

Mỗi sự kiện là một file dữ liệu có ngày bắt đầu và kết thúc, nên muốn thêm sự kiện mới chỉ cần thêm file.

### 4. Lý do chơi lâu dài

- **Nhiệm vụ hằng ngày và thành tựu**, thưởng ngọc trăng (tiền hiếm) và cỏ bốn lá.
- **Mùa trăng mới:** khi đã đi xa, bạn có thể bắt đầu lại từ đầu để đổi lấy hệ số thưởng vĩnh viễn, lần sau đi nhanh hơn và xa hơn.
- **Trang trí khu vườn:** đặt đèn lồng, ghế, hồ nước; mỗi món cộng một chút vào thu nhập.

## Hình ảnh

Bản web xấu vì hình được vẽ bằng code. Game thật cần hình vẽ thật, và mình không tự tạo ra ảnh được. Đề xuất của mình:

1. Dựng game bằng bộ hình miễn phí bản quyền (loại CC0) để mọi thứ chạy và nhìn gọn gàng ngay từ đầu.
2. Tổ chức sao cho thay hình chỉ là thay file, không đụng tới code.
3. Ở mốc cuối, thay bằng hình riêng của game. Hình đó phải đến từ bạn: công cụ tạo ảnh bạn có quyền dùng, hoặc người vẽ.

Nếu bạn đã có sẵn công cụ tạo ảnh thì nói mình, mình sẽ viết sẵn mô tả từng hình để bạn tạo, và dùng hình riêng ngay từ đầu.

## Cách làm (phần kỹ thuật, mình tự quyết)

- **Unity 6 LTS, 2D, C#.** Bản đầu xuất ra Android và bản chạy trên máy tính.
- **Dữ liệu tách khỏi code:** trạm, bé thỏ, sự kiện, nhiệm vụ đều là file dữ liệu trong Unity.
- **Số rất lớn:** dùng kiểu số riêng, vì game idle nhanh chóng vượt quá giới hạn số thường.
- **Lưu game:** file trên máy, có số phiên bản để bản cập nhật sau không làm mất tiến trình.
- **Kiểm thử tự động** cho phần dễ sai nhất: công thức kinh tế, tính thu nhập lúc vắng mặt, lịch sự kiện.
- **Git** để lưu lịch sử, làm hỏng thì quay lại được.

### Công cụ

- **Plugin Unity chính thức cho Claude Code:** để mình điều khiển Unity Editor trực tiếp. Cần bạn đồng ý trước khi cài.
- **Claude Code Game Studios:** mình không bê nguyên bộ này vào. Nó dựng cả một "studio" 49 vai, quá cồng kềnh cho một game một người làm. Mình chỉ mượn cách nó tổ chức tài liệu thiết kế.

## Ba hướng đã cân nhắc

| Hướng | Được | Mất | Kết quả |
|---|---|---|---|
| Unity | Chuẩn ngành cho game idle trên điện thoại, có plugin chính thức | Cài nặng, cần tài khoản | **Bạn chọn** |
| Godot | Nhẹ, không cần tài khoản | Công cụ hỗ trợ ít hơn | Mình đề xuất lúc đầu, bạn chọn Unity |
| Tiếp tục bản web | Chơi ngay, không cài gì | Trần thấp về hình ảnh, khó lên điện thoại | Bỏ, giữ lại làm bản tham khảo luật chơi |

## Các mốc

Mỗi mốc kết thúc bằng một bản bạn chơi thử được.

| Mốc | Làm gì | Bạn thấy gì khi xong |
|---|---|---|
| 0. Dựng nền | Tạo dự án Unity, cài plugin, Git, một màn hình trống chạy được | Mở Unity bấm Play thấy khu vườn trống |
| 1. Vòng chơi chính | Trạm pha chế và nguyên liệu, nâng cấp, mốc thưởng, thu lúc vắng mặt, lưu game | Chơi được như bản web, nhưng trong Unity và có lớp nguyên liệu |
| 2. Các bé thỏ | Sưu tầm, độ hiếm, kỹ năng, xếp vào trạm, cỏ bốn lá | Mở được bé mới và thấy khác biệt khi xếp |
| 3. Sự kiện | Chuyện trong ngày, điểm danh 7 ngày, Lễ hội Trăng Rằm | Chơi trọn một sự kiện từ đầu tới phần thưởng cuối |
| 4. Chơi lâu dài | Nhiệm vụ, thành tựu, Mùa trăng mới, trang trí | Có lý do để quay lại sau một tuần |
| 5. Hình và cảm giác | Hình riêng, chuyển động, âm thanh, bản Android | File cài lên điện thoại Android |

## Việc để lại cho lúc làm

- Con số cụ thể (giá, tốc độ, phần thưởng): chỉnh dần qua chơi thử ở từng mốc.
- Danh sách đầy đủ trạm, bé thỏ, món trang trí: mốc 1 và 2 sẽ chốt.
- Dự án nằm ở thư mục nào trên máy: mình sẽ hỏi bạn khi bắt đầu mốc 0.

## Thay đổi so với bản thiết kế gốc (cập nhật 22/11/2026)

Các quyết định bạn đưa ra trong lúc làm, ghi lại để bản thiết kế khớp với game:

- **Màn hình dọc** (đã thử màn ngang ngày 22/11 rồi quay lại dọc theo yêu cầu). Khu vườn là một bãi đất 2.5D nhìn chếch từ trên xuống, rộng hơn màn hình, kéo được mọi hướng; trạm xếp 3 cột. Chạm vào trạm mở thẻ nâng cấp; các danh sách nằm trong bảng nổi mở từ hàng nút tròn ở mép dưới.
- **Hai ngôn ngữ:** tiếng Việt và tiếng Anh, qua gói Localization của Unity.
- **Sự sống trong vườn:** nguyên liệu chạy từ trạm nguyên liệu sang trạm pha chế, thỏ đi dạo, thỏ ngủ gật (chạm để đánh thức, được trà lực), đom đóm vàng (chạm để nhận cà rốt).
- **Tiền tệ là cà rốt.** Cỏ bốn lá là tiền hiếm, dùng để gọi thỏ.
- **Thỏ sáu bậc:** bạc, xanh lá, xanh dương, tím, vàng, đỏ. Gọi ngẫu nhiên bằng cỏ bốn lá, ghép 3 bé cùng bậc lên bậc trên. Mỗi bé có cấp riêng (trần cấp theo bậc) và 2 ô đồ: mũ và phụ kiện.
- **Ô đất:** trạm thứ 5 trở đi cần mua đất. Hiện có 12 trạm.
- **Trà lực:** thanh năng lượng tự đầy; tiêu 50 để cả vườn nhanh gấp đôi trong 30 giây.
- **Bùa vườn, nhiệm vụ hằng ngày, thành tựu** đã có; nhiệm vụ hằng ngày và thành tựu lớn tặng đồ.
- **Chưa làm:** sự kiện theo mùa, điểm danh 7 ngày, chuyện xảy ra trong ngày, "Mùa trăng mới", trang trí vườn, đăng nhập và lưu trên mây.
- **Lên cửa hàng:** để sau. Dự định mua Apple Developer khoảng một năm nữa, CH Play sau đó.
