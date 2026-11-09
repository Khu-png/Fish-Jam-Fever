# Fish Jam Fever – âm thanh trích từ APK

*Trích xuất ngày 2026-11-06 từ `C:\Users\nguye\Documents\MuMuSharedFolder\Download\Share\Fish Jam Fever.apk` (package `com.puzzle.fish.jam.fever`, versionName 1.0.7, versionCode 11).*

## Nguồn

- **Engine:** Unity 2022.3.67f2 (IL2CPP, Addressables)
- **Vị trí trong APK:** 89 AudioClip trong `assets/bin/Data/sharedassets0.assets` (dữ liệu trong `sharedassets0.resource`) và 1 clip trong file asset đặt tên theo mã băm `5db43453fe160fb4192c261866e2d480` (kèm `.resource` cùng tên); FSB5 Vorbis + IMA ADPCM. Các file lớn được Unity cắt thành mảnh 1 MB `.split0`, `.split1`, …
- **Codec gốc:** Vorbis (74), IMA ADPCM->PCM16 (12)

## Cách trích xuất

Đọc AudioClip bằng UnityPy (tự ghép các mảnh `.splitN`), dựng lại Ogg Vorbis từ FSB5 bằng python-fsb5; 12 clip IMA ADPCM được giải mã sang WAV PCM16.

Định dạng đầu ra: `.ogg` là bitstream Vorbis gốc được đóng gói lại vào Ogg (không mất chất lượng); `.wav` là PCM 16-bit giải mã từ ADPCM/PCM gốc.

## Kết quả

- **86 file**, tổng thời lượng **4 ph 24 s**.
- Thư mục: `sounds/`; danh sách chi tiết trong `sounds_index.csv` (cột: game, folder, file, codec, channels, sample_rate, duration_sec, bytes, source).

| Thư mục | Số file | Thời lượng | Định dạng |
|---|---:|---:|---|
| `(gốc)` | 86 | 4 ph 24 s | .ogg ×74, .wav ×12 |

## Ghi chú

- 4 clip trùng nội dung 100 % chỉ giữ 1 bản.
- Nhạc nền: `BG_Music` (1 ph 17 s) và `BgMusic_01` (1 ph 25 s); còn lại là SFX gameplay/UI và giọng khen (`SFX_amazing`, `SFX_perfect`...).
- Các bundle Addressables (cá, icon cá, IAP, level) không chứa AudioClip.
- Phần ảnh/data/code của game nằm cùng thư mục, xem README.md.

Script dùng chung nằm ở `D:\ExtractApk\_sound_tools\` (xem README ở đó).
