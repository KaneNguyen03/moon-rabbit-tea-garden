# Unity — Version Reference

| Field | Value |
|-------|-------|
| **Engine Version** | 6000.6.10f1 |
| **Project Pinned** | 2026-11-26 |
| **Installed at pin time** | 6000.6.10f1 (`/Applications/Unity/Hub/Editor/6000.6.10f1`, khớp `ProjectSettings/ProjectVersion.txt`, revision 40e81493c67d) |
| **LLM Knowledge Cutoff** | 2026-06 |
| **Risk Level** | MEDIUM — Unity 6.6 có thể ra sau mốc dữ liệu của model; chưa tra tài liệu chính thức trong lần ghim này |

## Gói đang dùng (từ `Packages/manifest.json`)

| Gói | Phiên bản |
|-----|-----------|
| com.unity.render-pipelines.universal | 17.6.0 |
| com.unity.2d.animation | 16.0.1 |
| com.unity.2d.psdimporter | 15.0.1 |
| com.unity.inputsystem | 1.20.1 |
| com.unity.purchasing | 4.15.0 |
| com.unity.mobile.notifications | 2.5.0 |
| com.unity.localization | 1.5.14 |
| com.unity.analytics | 3.8.2 |
| com.unity.test-framework | 1.8.0 |
| com.unity.timeline | 6.6.0 |
| com.unity.ugui | 2.6.0 |
| com.unity.ai.navigation | 2.0.16 |
| com.google.play.games | v2.3.0 (git) |

DOTween không nằm trong manifest; nó là plugin trong `Assets/`.

## Chưa tra nguồn

- NOT SOURCEABLE — breaking changes và API bỏ dùng của 6000.4 → 6000.6, chưa fetch release notes trong lần ghim này.
- NOT SOURCEABLE — hành vi `UniversalRendererData` Forward+ trên mobile cho cảnh 2.5D (cần cho KaneNguyen03/moon-rabbit-tea-garden#145).

Chạy `/setup-engine refresh` để tra và ghi `breaking-changes.md`, `deprecated-apis.md`, `current-best-practices.md` khi bắt đầu bước 1 của 2.5D.
