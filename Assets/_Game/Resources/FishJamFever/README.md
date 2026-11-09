# Fish Jam Fever – kết quả trích xuất APK

APK: `C:\Users\nguye\Documents\MuMuSharedFolder\Download\Share\Fish Jam Fever.apk` (249 MB), package `com.puzzle.fish.jam.fever`, versionName 1.0.7 (versionCode 11), minSdk 28. Trích xuất ngày 2026-11-06.

## Thông tin kỹ thuật

| Mục | Giá trị |
|---|---|
| Engine | Unity 2022.3.67f2, IL2CPP (global-metadata v31), 1 scene duy nhất `SceneGame` |
| Quản lý asset | Addressables (19 bundle trong `assets/aa/Android`, catalog `catalog.json`) + `Resources/` (248 entry, Unity tách mỗi asset thành một file tên hash trong `assets/bin/Data/`) |
| Bundle | UnityFS, **không mã hoá**, có type tree nên dump được MonoBehaviour |
| Obfuscation | Không. Tên class/method/field giữ nguyên (namespace `mygame.sdk`, `Sys.IAP`, `FishSort.LevelTest`, `Game.Gameplay.Core`…) |
| Thư viện native | APK là bản split (`requiredSplitTypes="base__abi"`), **không kèm** `lib/libil2cpp.so`, nên chỉ dump được tên class/method/field/hằng số, không có kiểu dữ liệu |
| Plugin | Spine 4.0 (spine-unity), DOTween Pro, UniTask, UniRx, Nice Vibrations, SRDebugger, Coffee UIParticle/Unmask, Newtonsoft.Json, SerializedCollections |
| SDK | AppLovin MAX, AppsFlyer, Firebase (Analytics, Crashlytics, Messaging, RemoteConfig), Unity IAP 4.x + Unity Services Core, Google Play Games |
| Server | `https://watersort.live/api` (`/api/savedata`), `https://logapi.backendxgame.xyz/api/log/...`, `https://adminlocalapi.backendxgame.xyz/api/v1`, `https://fbbidding.xgamedatagame.live/api/v1/bidding/...`, `https://android.iron.twistedtangle.store/api`, policy `https://thomnguyenstudio.online/policy` |

Game dùng chung khung code với "Fish Sort" (namespace `FishSort`, `FishSort.LevelTest`), nhiều tên asset tiếng Việt (`Cancau`, `ChauCa_Lock`, `chuyenman`), studio Việt Nam.

## Cấu trúc thư mục

```
FishJamFever/
├─ apk/                       APK giải nén nguyên bản (zip)
├─ extracted/
│  ├─ bin_Data/               file Unity đã ghép các mảnh .splitN (globalgamemanagers, level0, sharedassets0.assets,
│  │                          388 file Resources tên hash, 2 file .resource, Managed/global-metadata.dat)
│  └─ aa/                     19 bundle Addressables + catalog.json + settings.json
├─ images/
│  ├─ fish_contact_sheet.png  bảng 386 icon cá (id 1 … 399, có khoảng trống, vd. không có fish_218, fish_385–393) kèm id
│  ├─ sprites/
│  │  ├─ FishSort/Image/Fish/             386 icon cá (bundle fishicon_assets_fishicon_group_1..8)
│  │  ├─ _XGFrame/IAPSystem/.../Packages/ 68 sprite popup IAP (bundle iapsystem_assets_all)
│  │  ├─ Resources/                        background/map_N, mechanictutorial, loading, nativeads, com.unity.purchasing/brands
│  │  └─ _by_source/SceneGame/             352 sprite UI nằm trong scene (sharedassets0): nút, popup, booster, khung bể cá…
│  │     _by_source/resfiles/              sprite trong file hash không có đường dẫn Resources
│  ├─ textures/               cùng cấu trúc; thêm FishSort/Anim/Fish/fish_N/fish_N.png = atlas Spine của 386 con cá
│  └─ index.csv               file -> tên, kích thước, container, file nguồn, path_id
├─ data/
│  ├─ text/FishSort/TextAssets/Level/      Level_1.json … Level_300.json (bundle levelnormal_assets_all)
│  ├─ text/FishSort/Anim/Fish/fish_N/      fish_N.json + fish_N.atlas (Spine 4.0.64) cho 386 con cá
│  ├─ text/Resources/langs/                bản dịch 12 ngôn ngữ (CSV: key,default,vi,ar,it,de,fr,he,ru,pt,ja,ko,id)
│  ├─ text/Resources/configevent/          7 config sự kiện/pack (refill pack, happy pack, weekend pack, watch & win…)
│  ├─ text/Resources/refill_pack_tier_config.json  bậc gói nạp theo tổng chi tiêu
│  ├─ text/_by_source/SceneGame/           Spine UI (Cancau, Cannon, Chest_win, Home, LOGO, Lock_*, Win, daily_reward…)
│  ├─ monobehaviour/          1720 MonoBehaviour từ bundle dump ra JSON (SkeletonDataAsset, SpineAtlasAsset, prefab IAP)
│  ├─ fonts/                  Orbitron (4), SourceCodePro, LiberationSans, NotoSans-Bold, royal_kingdom_font, LegacyRuntime
│  ├─ AndroidManifest.xml     manifest đã giải mã
│  └─ index.csv
├─ sounds/ + sounds_index.csv 86 AudioClip (74 .ogg Vorbis, 12 .wav từ IMA ADPCM), xem README_sounds.md
├─ code/
│  ├─ dump_Assembly-CSharp.cs 2128 type code game
│  ├─ dump_*.cs               34 assembly plugin/SDK khác (MaxSdk, AppsFlyer, Firebase.*, spine-*, UniTask…) + UnityEngine/System
│  ├─ types.json              toàn bộ type dạng JSON (fields, methods, params, hằng số)
│  ├─ enums.txt               1384 enum với giá trị
│  └─ string_literals.txt     24458 chuỗi literal (key PlayerPrefs, URL, tên sound, key RemoteConfig, log tiếng Việt)
├─ addressables_catalog.csv   2536 key Addressables -> internal id, provider, kiểu, bundle
└─ scripts/                   pipeline Python (xem bên dưới)
```

## Ảnh đáng chú ý

- `images/sprites/FishSort/Image/Fish/fish_N.png`: icon 386 loại cá, N là id dùng trong `FishStr` của level. Xem nhanh bằng `images/fish_contact_sheet.png`.
- `images/textures/FishSort/Anim/Fish/fish_N/fish_N.png` + `data/text/FishSort/Anim/Fish/fish_N/fish_N.{json,atlas}`: skeleton Spine 4.0 của từng con cá (bơi, idle).
- `images/sprites/Resources/background/map_N.png`: nền các map; `Resources/mechanictutorial/`: ảnh hướng dẫn cơ chế.
- `images/sprites/_by_source/SceneGame/`: toàn bộ UI trong scene (BG_*_popup, Booster_*, Btn_*, Char_Hard/Crazy, Board_Fish…).
- Ảnh ASTC/ETC đã giải mã sang PNG; tên trùng được thêm hậu tố `_<path_id>`.

## Dữ liệu level

`Level_N.json` (N = 1…300):

```json
{ "levelId": 1, "countdownTargetProgress": "",
  "bubbles": [ { "BubbleID": 1, "BubbleType": 3, "FishStr": "31,31,31",
                 "IsFrozen": false, "IsUnknown": false, "LockCount": 0, "IsKey": false } ] }
```

| Trường | Ý nghĩa |
|---|---|
| `BubbleType` | 0–5, loại/kích cỡ bong bóng (thống kê 300 level: type 5 ×3116, 3 ×3061, 4 ×2997, 2 ×1524, 1 ×94, 0 ×1) |
| `FishStr` | danh sách id cá trong bong bóng, phân tách bằng dấu phẩy (id tra `fish_N.png`) |
| `IsFrozen` | bong bóng đóng băng (276 bong bóng) |
| `IsUnknown` | cá ẩn, chưa lộ (152) |
| `LockCount` / `IsKey` | bong bóng khoá cần N chìa / bong bóng chứa chìa (83 mỗi loại) |
| `countdownTargetProgress` | cấu hình mục tiêu đếm ngược (đa số rỗng) |

Mỗi level có 3–51 bong bóng. Độ khó động do `Game.Gameplay.Core.DynamicDifficultyService` và A/B test `FishSort.LevelTest` (Firebase RemoteConfig, key `abTest`) điều khiển; game cũng có thể tải level khác từ server (`watersort.live/api`).

## Giới hạn

- `extracted/bin_Data`: 10706 MonoBehaviour trong scene/Resources **không** dump được (release build strip type tree, và không có `libil2cpp.so` để dựng lại).
- Không tìm thấy level ngoài 300 level trong bundle; level cao hơn có thể do server cấp hoặc sinh tự động.

## Scripts (`scripts/`)

| Script | Việc |
|---|---|
| `join_splits.py` | ghép `.splitN` trong `apk/assets/bin/Data` → `extracted/bin_Data`, copy bundle Addressables |
| `probe.py`, `probe2.py` | thống kê object theo file, đọc `ResourceManager`/`BuildSettings` |
| `extract_assets.py` | UnityPy: Texture2D/Sprite → PNG, TextAsset, Font, MonoBehaviour (type tree) → JSON; đặt tên theo container Addressables hoặc bảng `ResourceManager` |
| `parse_catalog.py` | giải mã `catalog.json` Addressables → `addressables_catalog.csv` |
| `parse_metadata.py` | dump `global-metadata.dat` → `code/` |
| `contact_sheet.py` | tạo `images/fish_contact_sheet.png` |
| `register_sound_tools.py`, `add_sound_readme_info.py` | đăng ký game vào pipeline âm thanh chung `D:\ExtractApk\_sound_tools` |

Âm thanh chạy bằng `python D:\ExtractApk\_sound_tools\extract_sounds.py FishJamFever` (rồi `validate.py`, `gen_readme.py`, `gen_player.py`).
