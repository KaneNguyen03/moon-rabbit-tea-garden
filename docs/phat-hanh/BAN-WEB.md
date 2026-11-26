# Bản chơi trên web (WebGL, GitHub Pages)

*Viết 26/11/2026 cho #150.* Đường dẫn chơi: **https://kanenguyen03.github.io/moon-rabbit-tea-garden/**

## Hai lệnh

```bash
tools/build-webgl.sh
tools/publish-pages.sh
```

- `tools/build-webgl.sh [ưu tiên]` dựng vào `Build/WebGL/` qua cổng Unity `/tmp/unity-gate.sh` (ưu tiên mặc định 4). Máy không có cổng thì chạy thẳng `tools/build.sh webgl`.
- `tools/publish-pages.sh [remote]` đẩy nội dung `Build/WebGL/` cùng `.nojekyll` lên nhánh `gh-pages` của origin bằng tài khoản KaneNguyen03.

Đóng Unity Editor trước khi build (batch mode không mở được project đang mở). Thư mục `Build/` không bao giờ commit.

## Bản build có gì

| Cài đặt | Giá trị | Đặt ở đâu |
|---|---|---|
| Nén | Brotli, **Decompression Fallback bật**: trình duyệt tự giải nén bằng JS nên GitHub Pages không cần header `Content-Encoding` | `BuildScript.ConfigureWebGL` |
| Exception | `ExplicitlyThrownExceptionsOnly` (mức tối thiểu còn bắt được lỗi do code ném) | như trên |
| Template | `Assets/WebGLTemplates/VuonTra`: trang nền tối, khung dọc 9:16 giữa màn trên máy tính, toàn màn trên điện thoại, thanh tải, không có viền/logo Unity | `PlayerSettings.WebGL.template = PROJECT:VuonTra` |
| Độ nét | canvas theo cửa sổ, `devicePixelRatio` tối đa 2 | `index.html` của template |
| Save | `persistentDataPath` trên web là `/idbfs/...` trong IndexedDB; template bật `autoSyncPersistentDataPath` nên mỗi lần `SaveStore` ghi tệp là Unity tự đẩy xuống IndexedDB, không cần `.jslib` | `index.html` của template |
| Tiếng | trình duyệt chặn âm thanh trước lần chạm đầu; Unity tự mở lại `AudioContext` khi người chơi chạm/nhấn lần đầu | sẵn trong Unity |
| Khởi động | Trên web, Localization/Addressables không được tải đồng bộ (`WaitForCompletion` ném lỗi). `GameRunner` giữ object `Game` tắt cho tới khi Localization khởi tạo xong và bảng chữ đã tải, rồi mới `Init` và bật lại; `L10n.T` trả về khoá thay vì chặn khi bảng chưa sẵn sàng. Máy tính, iOS, Android và Editor vẫn chạy đồng bộ như cũ (`L10n.LoadsAsync` chỉ đúng trên WebGL) | `GameRunner.Awake`, `L10n` |
| Chữ | Web không có font hệ điều hành, font dựng sẵn của Unity thiếu dấu tiếng Việt (ạ, ơ, ư, đ...). Bản web dùng `Assets/Resources/Fonts/DejaVuSans.ttf` (giấy phép Bitstream Vera/DejaVu cho phép phân phối lại, nằm sẵn trong tệp font), đủ mọi ký tự game dùng kể cả ✦ ☆ ♥ ▶ ✓. Các nền tảng khác vẫn dùng font hệ điều hành như trước | `UiKit.Font` |
| Seam iOS/Android | IAP, Game Center, iCloud, Play Games, thông báo, Firebase đều nằm sau `#if UNITY_IOS`/`UNITY_ANDROID`; trên WebGL các seam này là bản rỗng | `Assets/VuonTra/Game/Platform`, `Store` |
| Kiếm tiền | cờ `Features.Monetization` vẫn tắt (#144), bản web không có mua hàng/quảng cáo/giá | không đổi |

`BuildScript.BuildWebGL` chuyển active build target sang WebGL, build Addressables + player, rồi chuyển lại target cũ, để `tools/verify.sh` và `tools/test.sh` vẫn chạy trên target máy tính.

## Phát hành một bản mới

1. `git pull` trên nhánh cần phát hành; `tools/verify.sh` xanh.
2. `tools/build-webgl.sh` (lần đầu trên một Library mới mất lâu vì import lại toàn bộ asset cho WebGL; các lần sau nhanh hơn).
3. Thử tại máy: `cd Build/WebGL && python3 -m http.server 8150`, mở `http://localhost:8150/`. Thanh tải chạy hết, vào vườn, chạm vài lần, đóng tab, mở lại: vườn còn.
4. `tools/publish-pages.sh`. Script dựng một repo tạm chỉ chứa nội dung `Build/WebGL/` + `.nojekyll`, commit "Web build from <sha>", rồi **force push** một commit duy nhất lên `gh-pages` (nhánh không giữ lịch sử build cũ, repo không phình thêm vài chục MB mỗi lần).
5. Đợi 1–2 phút cho GitHub Pages dựng lại, mở đường dẫn ở đầu trang.

Token lấy bằng `env -u GITHUB_TOKEN -u GH_TOKEN gh auth token --user KaneNguyen03`, đưa vào git qua một credential helper chỉ sống trong lệnh `push` đó và đọc token từ biến môi trường, không nằm trong URL remote, không ghi xuống tệp nào. Đổi tài khoản bằng `PAGES_ACCOUNT=...`, đổi nhánh bằng `PAGES_BRANCH=...`. Thử script mà không đụng origin: `tools/publish-pages.sh /đường/dẫn/repo-trần.git`.

Script dừng nếu chưa có `Build/WebGL/index.html` (mã 2) hoặc có tệp lớn hơn 95 MB, vì GitHub từ chối tệp trên 100 MB (mã 3).

## Bật GitHub Pages (một lần, sau lần publish đầu)

Repo → **Settings** → **Pages** → *Build and deployment* → Source: **Deploy from a branch** → Branch: **gh-pages**, thư mục **/ (root)** → **Save**. Hoặc từ Terminal:

```bash
GH_TOKEN=$(env -u GITHUB_TOKEN -u GH_TOKEN gh auth token --user KaneNguyen03) \
  gh api -X POST repos/KaneNguyen03/moon-rabbit-tea-garden/pages -f 'source[branch]=gh-pages' -f 'source[path]=/'
```

Repo đang public nên Pages miễn phí. Nếu repo chuyển sang private thì Pages cần gói GitHub Pro.

## Lưu ý

- Save web gắn với địa chỉ trang (`/idbfs/<băm của đường dẫn>`): chơi ở `localhost` và ở github.io là hai save khác nhau. Xoá dữ liệu trang web trong trình duyệt là mất save; Safari có thể xoá dữ liệu của trang không mở trong 7 ngày.
- Không có iCloud/Game Center/Play Games trên web, nên save không đồng bộ với bản điện thoại.
- Build lần đầu tạo `Assets/AddressableAssetsData/WebGL/` (trạng thái nội dung Addressables); không commit thư mục này, như các tệp Addressables khác.
