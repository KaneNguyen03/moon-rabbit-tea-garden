# Bản chơi trên web (WebGL, GitHub Pages)

*Viết 26/11/2026 cho #150.* Đường dẫn chơi: **https://kanenguyen03.github.io/moon-rabbit-tea-garden/**

Bản cũ (chỉ WebGL 2, build từ `e7da078`) để dự phòng: **https://kanenguyen03.github.io/moon-rabbit-tea-garden/old/**. Xem mục [Bản cũ ở /old/](#bản-cũ-ở-old-2811).

## Hai lệnh

```bash
tools/build-webgl.sh
tools/publish-pages.sh
```

- `tools/build-webgl.sh [ưu tiên]` dựng vào `Build/WebGL/` qua `tools/build.sh webgl`, lệnh này tự xếp hàng ở cổng Unity của repo `tools/unity-gate.sh` (ưu tiên mặc định 4; xem `docs/save-va-test.md`).
- `tools/publish-pages.sh [remote]` đẩy nội dung `Build/WebGL/` cùng `.nojekyll` lên nhánh `gh-pages` của origin bằng tài khoản KaneNguyen03, giữ nguyên các thư mục trong danh sách giữ lại (mặc định `old/`).

Đóng Unity Editor trước khi build (batch mode không mở được project đang mở). Thư mục `Build/` không bao giờ commit.

## Bản build có gì

| Cài đặt | Giá trị | Đặt ở đâu |
|---|---|---|
| Nén | Brotli, **Decompression Fallback bật**: trình duyệt tự giải nén bằng JS nên GitHub Pages không cần header `Content-Encoding` | `BuildScript.ConfigureWebGL` |
| Exception | `ExplicitlyThrownExceptionsOnly` (mức tối thiểu còn bắt được lỗi do code ném) | như trên |
| Template | `Assets/WebGLTemplates/VuonTra`: trang nền tối, khung dọc 9:16 giữa màn trên cả máy tính và điện thoại, có khoảng nền tối ngoài khung nếu màn khác tỉ lệ, thanh tải, không có viền/logo Unity | `PlayerSettings.WebGL.template = PROJECT:VuonTra` |
| Độ nét | canvas theo cửa sổ, `devicePixelRatio` tối đa 2 | `index.html` của template |
| Save | `persistentDataPath` trên web là `/idbfs/...` trong IndexedDB; template bật `autoSyncPersistentDataPath` nên mỗi lần `SaveStore` ghi tệp là Unity tự đẩy xuống IndexedDB, không cần `.jslib` | `index.html` của template |
| Tiếng | trình duyệt chặn âm thanh trước lần chạm đầu; Unity tự mở lại `AudioContext` khi người chơi chạm/nhấn lần đầu | sẵn trong Unity |
| Khởi động | Trên web, Localization/Addressables không được tải đồng bộ (`WaitForCompletion` ném lỗi). `GameRunner` giữ object `Game` tắt cho tới khi Localization khởi tạo xong và bảng chữ đã tải, rồi mới `Init` và bật lại; Khi đổi ngôn ngữ, Localization nhả mọi bảng rồi tải lại; trong lúc đó `L10n.T` trả lại chữ đã hiện trước đó cho khoá ấy, và `L10n.Changed` chỉ báo khi bảng mới đã tải xong. Máy tính, iOS, Android và Editor vẫn chạy đồng bộ như cũ (`L10n.LoadsAsync` chỉ đúng trên WebGL) | `GameRunner.Awake`, `L10n` |
| Chữ | Web không có font hệ điều hành, font dựng sẵn của Unity thiếu dấu tiếng Việt (ạ, ơ, ư, đ...). Bản web dùng `Assets/Resources/Fonts/DejaVuSans.ttf` (giấy phép Bitstream Vera/DejaVu cho phép phân phối lại, nằm sẵn trong tệp font), đủ mọi ký tự game dùng kể cả ✦ ☆ ♥ ▶ ✓. Các nền tảng khác vẫn dùng font hệ điều hành như trước | `UiKit.Font` |
| Seam iOS/Android | IAP, Game Center, iCloud, Play Games, thông báo, Firebase đều nằm sau `#if UNITY_IOS`/`UNITY_ANDROID`; trên WebGL các seam này là bản rỗng | `Assets/VuonTra/Game/Platform`, `Store` |
| Kiếm tiền | cờ `Features.Monetization` vẫn tắt (#144), bản web không có mua hàng/quảng cáo/giá | không đổi |

`BuildScript.BuildWebGL` chuyển active build target sang WebGL, build Addressables + player, rồi chuyển lại target cũ, để `tools/verify.sh` và `tools/test.sh` vẫn chạy trên target máy tính.

## Đồ hoạ: WebGPU trước, WebGL 2 dự phòng (27/11, Refs #150)

Bản web vẫn là build target Web (WebAssembly). Danh sách Graphics API của Web tắt **Auto Graphics API** và xếp **WebGPU** trước, **WebGL 2** sau. Unity thử API đầu; trình duyệt không có WebGPU (hoặc không lấy được adapter) thì Unity tự chạy WebGL 2 trong **cùng một bản build**. `BuildScript.ConfigureWebGL` đặt lại danh sách này mỗi lần build (`BuildScript.WebGraphicsApis`), `WebGraphicsTests` hỏng nếu danh sách mất WebGPU hoặc WebGL 2.

Tài liệu Unity 6000.6 đã đọc:
- [Enable the WebGPU graphics API](https://docs.unity.com/en-us/engine/6000.6/manual/platform-specific/webgl/develop/graphics/web-gpu/enable): Unity không tự bật WebGPU; tắt Auto Graphics API, thêm WebGPU, kéo lên đầu, giữ WebGL 2 để trình duyệt cũ vẫn chạy. Không có define hay cờ nào khác. Trang này và các trang dưới không gọi WebGPU là experimental/preview.
- [Limitations of the WebGPU graphics API](https://docs.unity.com/en-us/engine/6000.6/manual/platform-specific/webgl/develop/graphics/web-gpu/limitations): không async compute, dynamic resolution, cubemap array; **không đọc ngược GPU→CPU đồng bộ** (`GetPixels`, `CaptureScreenshot`...), dùng `AsyncGPUReadback`; Safari/Firefox thiếu vài định dạng texture; máy yếu chạy "compatibility mode".
- [WebGPU Device Filter asset](https://docs.unity.com/en-us/engine/6000.6/manual/platform-specific/webgl/develop/graphics/web-gpu/webgpu-device-filter-asset/webgpu-intro-device-filter-asset): trình duyệt bị lọc dùng API dự phòng trong Player settings; không có API dự phòng thì không mở được game. Bản này không dùng filter.

**Xem game đang chạy renderer nào (cho tester):** mở trang với `?debug`, ví dụ `https://kanenguyen03.github.io/moon-rabbit-tea-garden/?debug` hoặc `http://localhost:8150/?debug`. Sau khi tải xong, góc trên trái hiện một dòng nhỏ: `Renderer: WebGPU`, `Renderer: WebGL 2 (fallback)` (có `navigator.gpu` nhưng không dùng được), hoặc `Renderer: WebGL 2 (fallback: no WebGPU in this browser)`. Không có `?debug` thì người chơi không thấy dòng này (chủ project 28/11: dòng chữ đè lên thanh tab là rác debug); dòng ấy vẫn luôn được ghi vào console của trình duyệt. Ảnh: `docs/anh/webgpu/trang-khong-debug.png` và `trang-co-debug.png`.

Kiểm tra bằng build thật qua cổng Unity, Edge 158.0.4370.45 trên Windows 11 (RTX 4070), `python3 -m http.server`:

| Lần chạy | Renderer trên trang | Ảnh |
|---|---|---|
| WebGPU bật | `WebGPU` (console: `Unity WebGPU: Version: WebGPU 1.0`) | `docs/anh/webgpu/webgpu-vuon.png`, `webgpu-the-tram.png`, `webgpu-anh-sau-khi-sua.png` |
| `--disable-features=WebGPUService,WebGPU` | `WebGL 2 (fallback)` | `webgl2-vuon.png`, `webgl2-bang-tram.png`, `webgl2-anh.png` |
| xoá `navigator.gpu` trước khi trang chạy | `WebGL 2 (fallback: no WebGPU in this browser)` | `webgl2-khong-co-navigator-gpu.png` |

Shader: build biên dịch 95 shader cho cả `gles3` và `webgpu`, không lỗi, không cảnh báo shader. Game không có shader riêng; vườn 2D (URP 2D renderer), post-processing, hạt Shuriken, UI, font DejaVu có dấu tiếng Việt và DOTween đều hiện giống WebGL 2 trên ảnh. Vườn 2.5D đang tắt trên main (`Features.Garden25DByDefault = false`) nên chỉ biên dịch shader của nó, chưa xem trên trình duyệt.

Lỗi WebGPU duy nhất tìm thấy: chế độ chụp ảnh dùng `ReadPixels` nên dưới WebGPU ảnh xám trắng (`webgpu-anh-truoc-khi-sua.png`, console: "Texture Readback is not supported by WebGPU"). `GardenScreen.Snap` dưới WebGPU chép ảnh bằng `Graphics.CopyTexture` cho khung xem trước, ghi PNG bằng `AsyncGPUReadback`, và chỉ khi tệp đã ghi xong mới lưu đường dẫn (`LastPhotoPath`, album, quà ảnh) và mở bảng xem ảnh, nên "Copy image", "Show file" và album không bao giờ trỏ tới tệp chưa có. Các nền tảng khác giữ `ReadPixels` như cũ. Album đọc lại được tệp PNG ghi bằng readback dưới WebGPU, đúng chiều (`webgpu-album.png`). Mỗi lần chụp giờ huỷ cả RenderTexture tạm (trước đây chỉ `Release`, để rò một object mỗi ảnh).

Dung lượng (Brotli, 27/11): `WebGL.data.unityweb` 32,93 MB (52,90 MB giải nén), `WebGL.wasm.unityweb` 9,75 MB (53,27 MB), `WebGL.framework.js.unityweb` 0,07 MB (0,43 MB), `WebGL.loader.js` 0,11 MB. Tổng tải 42,87 MB, giải nén 106,71 MB (MB = 2^20 byte). So với bản WebGL 2 ngày 26/11 (32,5 + 9,3 MB) thêm khoảng 0,9 MB vì có cả hai bộ shader và mã WebGPU.

**Nén texture (đề xuất, chưa làm):** 200 trong 271 ảnh PNG để nén = None trên Web (~110 MB nếu tính RGBA32 cho mọi ảnh trong `Assets`, gồm cả ảnh không vào build). Đặt override WebGL dùng DXT5/BC7 (1 byte/pixel, máy tính) cùng ASTC (điện thoại, Unity cho build thêm bản ASTC) sẽ giảm phần texture trong `.data` khoảng 4 lần trước Brotli và giảm RAM GPU tương ứng; ước tính `.data` tải về còn khoảng 12–18 MB thay vì 33 MB. Đổi lại là viền màu nước và alpha mềm có thể bị khối/nhoè, nên phải xem ảnh từng nhóm (nền, thỏ, UI) trước khi áp dụng. Chưa đo.

## Phát hành một bản mới

1. `git pull` trên nhánh cần phát hành; `tools/verify.sh` xanh.
2. `tools/build-webgl.sh` (lần đầu trên một Library mới mất lâu vì import lại toàn bộ asset cho WebGL; các lần sau nhanh hơn).
3. Thử tại máy: `cd Build/WebGL && python3 -m http.server 8150`, mở `http://localhost:8150/`. Thanh tải chạy hết, vào vườn, chạm vài lần, đóng tab, mở lại: vườn còn.
4. `tools/publish-pages.sh`. Script dựng một repo tạm chỉ chứa nội dung `Build/WebGL/` + `.nojekyll`, lấy `gh-pages` hiện có làm gốc (fetch nông một commit), chép lại nguyên các đường dẫn trong danh sách giữ lại từ `gh-pages`, commit "Web build from <sha>" lên trên rồi push thường, **không force push** (luật repo). Mọi tệp khác của bản cũ ở gốc bị thay bằng bản build mới. Mỗi lần publish thêm một commit vào `gh-pages`; build không đổi thì script dừng, không push.
5. Đợi 1–2 phút cho GitHub Pages dựng lại, mở đường dẫn ở đầu trang.

Token lấy bằng `env -u GITHUB_TOKEN -u GH_TOKEN gh auth token --user KaneNguyen03`, đưa vào git qua một credential helper chỉ sống trong lệnh `push` đó và đọc token từ biến môi trường, không nằm trong URL remote, không ghi xuống tệp nào. Đổi tài khoản bằng `PAGES_ACCOUNT=...`, đổi nhánh bằng `PAGES_BRANCH=...`. Thử script mà không đụng origin: `tools/publish-pages.sh /đường/dẫn/repo-trần.git`.

Script dừng nếu chưa có `Build/WebGL/index.html` (mã 2), có tệp lớn hơn 95 MB, vì GitHub từ chối tệp trên 100 MB (mã 3), hoặc bản build có sẵn một đường dẫn trùng với danh sách giữ lại (mã 4).

**Danh sách giữ lại:** `PAGES_KEEP`, các đường dẫn ở gốc `gh-pages` cách nhau bằng dấu cách, mặc định `old`. Mỗi đường dẫn có trên `gh-pages` được giữ byte nào cũng như cũ; đường dẫn chưa có thì bỏ qua. `PAGES_KEEP="old ban-thu"` giữ thêm thư mục; `PAGES_KEEP=""` không giữ gì, tức là xoá `/old/` (chỉ làm khi chủ project quyết bỏ bản cũ). `python3 -I tools/test_publish_pages.py` (dưới 1 s, không Unity, chạy trong CI) dựng một `gh-pages` giả trong thư mục tạm, publish vào đó và kiểm tra `old/` còn nguyên, tệp cũ ở gốc bị thay, publish lại bản giống hệt thì không push, và mã 4.

## Bản cũ ở /old/ (28/11)

Chủ project duyệt 28/11: "Thay, giữ bản cũ ở /old/". Commit `763409e` trên `gh-pages` (một commit thường trên `01e04a9`, không force push) chuyển nguyên bản đang chạy lúc ấy (`Web build from e7da078`, chỉ WebGL 2) vào thư mục `old/`, byte nào cũng như cũ, rồi đặt bản WebGPU build từ `43dcb74` (`b75be6a`, không khác `Assets`, `ProjectSettings`, `Packages`) ở gốc. Trang và Addressables của bản cũ chỉ dùng đường dẫn tương đối (`Build/...`, `StreamingAssets`, `{Addressables.RuntimePath}`) nên chạy được từ `/old/` không cần sửa. Lần dựng Pages: [46059574190](https://github.com/KaneNguyen03/moon-rabbit-tea-garden/actions/runs/46059574190).

Kiểm tra trên trang thật, Edge 158.0.4370.45 trên Windows 11:

| Địa chỉ | Kết quả | Ảnh |
|---|---|---|
| `/?debug` | vào vườn, `Renderer: WebGPU` | `docs/anh/webgpu/pages-webgpu.png` |
| `/?debug`, Edge mở với `--disable-features=WebGPUService,WebGPU` | vào vườn, `Renderer: WebGL 2 (fallback)` | `docs/anh/webgpu/pages-webgl2-fallback.png` |
| `/old/` | vào vườn bản cũ, console `Creating WebGL 2.0 context.` | `docs/anh/webgpu/pages-old.png` |

Dung lượng trên `gh-pages`: gốc 42,92 MB (MB = 2^20 byte; `WebGL.data.unityweb` 34 530 333 byte, `WebGL.wasm.unityweb` 10 206 207 byte), `old/` 42,06 MB (34 030 104 và 9 796 038 byte).

Save gắn với đường dẫn trang, nên `/old/` có save riêng, không thấy save của bản ở gốc và ngược lại.

`/old/` được tạo bằng một commit tay vì lúc ấy `tools/publish-pages.sh` còn xoá mọi thứ không có trong bản build. Từ nay script giữ `old/` theo danh sách giữ lại ở trên, nên lần phát hành sau chỉ cần `tools/publish-pages.sh`; bản ở `/old/` vẫn là bản `e7da078` cho tới khi có ai thay nó bằng tay. Bỏ `/old/` là quyết định của chủ project.

## Bật GitHub Pages (một lần, sau lần publish đầu)

Repo → **Settings** → **Pages** → *Build and deployment* → Source: **Deploy from a branch** → Branch: **gh-pages**, thư mục **/ (root)** → **Save**. Hoặc từ Terminal:

```bash
GH_TOKEN=$(env -u GITHUB_TOKEN -u GH_TOKEN gh auth token --user KaneNguyen03) \
  gh api -X POST repos/KaneNguyen03/moon-rabbit-tea-garden/pages -f 'source[branch]=gh-pages' -f 'source[path]=/'
```

Repo đang public nên Pages miễn phí. Nếu repo chuyển sang private thì Pages cần gói GitHub Pro.

## Số đo bản build 26/11

| Mục | Giá trị |
|---|---|
| Dung lượng tải | 43 MB trên đĩa, ~42 MB tải lần đầu: `WebGL.data.unityweb` 32,5 MB, `WebGL.wasm.unityweb` 9,3 MB, `framework.js` + `loader.js` 0,2 MB |
| Tải lần đầu | 5,5–9,5 s từ `localhost` (Chrome 158.0.8127.37 headless, `python3 -m http.server`, không header `Content-Encoding`); mạng thật ≤30 s: **NOT TESTED**, không suy ra từ localhost |
| Mở lại | ~6 s, tải 0 MB (Unity cache tệp build trong IndexedDB) |
| Console | không lỗi, không cảnh báo; chỉ có dòng gợi ý của Unity về header `Content-Encoding: br` |
| Save | đóng hẳn trình duyệt rồi mở lại: `playSeconds` 15,1 → 30,2, vườn còn nguyên |
| Đổi ngôn ngữ | Tiếng Việt ↔ English trong menu Tạm nghỉ đổi ngay, không hiện khoá thô |

Ảnh: `docs/anh/po-150/web-desktop-*.png` (1440×900) và `web-mobile-*.png` (viewport giả lập 390×844 trong Chrome desktop; không phải iPhone thật). **Safari desktop, Safari iOS, Chrome Android trên thiết bị thật: NOT TESTED.**

## Lưu ý

- Save web gắn với địa chỉ trang (`/idbfs/<băm của đường dẫn>`): chơi ở `localhost` và ở github.io là hai save khác nhau. Xoá dữ liệu trang web trong trình duyệt là mất save; Safari có thể xoá dữ liệu của trang không mở trong 7 ngày.
- Không có iCloud/Game Center/Play Games trên web, nên save không đồng bộ với bản điện thoại.
- Build lần đầu tạo `Assets/AddressableAssetsData/WebGL/` (trạng thái nội dung Addressables); không commit thư mục này, như các tệp Addressables khác.
