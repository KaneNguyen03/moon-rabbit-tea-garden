# Khai báo dữ liệu cho cửa hàng và nhóm tuổi (bản nháp cho #42)

Viết 22/11/2026 theo mặc định được phép; chủ project duyệt trước khi điền lên App Store Connect và Google Play Console. Phản ánh đúng bản hiện tại: game **không gọi mạng, không thu thập gì**, save chỉ nằm trên máy; cập nhật khi #39/#40 (lưu mây theo nền tảng) hoặc #9 (mua bằng tiền thật) bật.

## 1. App Store — App Privacy (Apple)

| Câu hỏi | Trả lời hiện tại | Khi bật #39 (Game Center + iCloud) | Khi bật #9 (IAP) |
|---|---|---|---|
| Do you or your third-party partners collect data from this app? | **No, we do not collect data** | Vẫn **No**: Game Center và iCloud do Apple vận hành trên tài khoản của người chơi; app không nhận dữ liệu về máy chủ của mình | Vẫn **No**: giao dịch do Apple xử lý; app chỉ nhận biên lai để mở khoá |
| Tracking (ATT) | Không theo dõi, không hiện hộp ATT | Không đổi | Không đổi |

Ghi chú: nếu sau này thêm phân tích (analytics) hay quảng cáo thì phải khai "Yes" với các loại Identifiers, Usage Data, Diagnostics và liên kết với người dùng hay không.

## 2. Google Play — Data safety

| Mục | Trả lời hiện tại | Khi bật #40 (Play Games + Saved Games) |
|---|---|---|
| Does your app collect or share any of the required user data types? | **No** | Vẫn **No** (Play Games chạy trên tài khoản Google của người chơi, không qua máy chủ riêng); nếu Google yêu cầu khai "Player ID" thì chọn: *App info and performance* không, *Personal info → User IDs*: collected, not shared, required, purpose: Account management |
| Is all of the user data collected by your app encrypted in transit? | Không áp dụng (không thu thập) | Yes |
| Do you provide a way for users to request that their data is deleted? | Yes: nút "Xoá dữ liệu của tôi" trong Hồ sơ (#42 phần code `18c21d1`) | Yes, xoá cả bản mây |
| Independent security review | No | No |

## 3. Nhóm tuổi mục tiêu (đề xuất)

- **App Store: 4+**. Không bạo lực, không nội dung người lớn, không cờ bạc mô phỏng bằng tiền thật. Gọi thỏ bằng cỏ bốn lá hay cà rốt vàng kiếm trong game không phải "simulated gambling" theo bảng của Apple vì không có tiền thật đổi vào (xem lại khi #9 bật: Apple vẫn thường xếp 4+ cho gacha dùng tiền ảo mua bằng tiền thật, nhưng cần công bố tỉ lệ, đã có ở màn gọi).
- **Google Play: Everyone (IARC)**, bảng câu hỏi IARC trả lời "không" cho mọi mục; mục "In-game purchases with randomized items" chọn **Yes** chỉ khi #9 bật (Google hiện yêu cầu nhãn "Includes random items" và công bố tỉ lệ).
- **Target audience Google Play:** chọn **Ages 13 and over** làm nhóm mục tiêu chính dù rating Everyone, để không rơi vào chính sách Families (bắt buộc ngặt hơn về quảng cáo, đăng nhập và dữ liệu); game không nhắm riêng trẻ em. Nếu chủ project muốn nhắm cả trẻ em thì phải vào chương trình Designed for Families và bỏ đăng nhập Google tự động cho người dưới 13.

## 4. Việc chủ project điền tay (không có trong code)

- Email hỗ trợ thật vào `AppInfo.SupportEmail` và đường dẫn công khai của chính sách (`docs/chinh-sach-quyen-rieng-tu.md` phải được đăng ở một trang mở, ví dụ GitHub Pages của một repo public) vào `AppInfo.PrivacyUrl`.
- Điền hai bảng trên vào console sau khi tạo app trên hai cửa hàng (#41).
