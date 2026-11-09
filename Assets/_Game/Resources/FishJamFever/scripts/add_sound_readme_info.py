"""Add the FishJamFever prose entry to _sound_tools/gen_readme.py INFO."""
p = r"D:\ExtractApk\_sound_tools\gen_readme.py"
s = open(p, encoding="utf-8").read()
anchor = '''                            "nhiều khả năng thuộc SDK quảng cáo, nên đây có lẽ là toàn bộ âm thanh của bản 1064."]),
}'''
entry = '''                            "nhiều khả năng thuộc SDK quảng cáo, nên đây có lẽ là toàn bộ âm thanh của bản 1064."]),
    "FishJamFever": dict(title="Fish Jam Fever", date="2026-11-06", engine="Unity 2022.3.67f2 (IL2CPP, Addressables)",
                     source="`C:\\\\Users\\\\nguye\\\\Documents\\\\MuMuSharedFolder\\\\Download\\\\Share\\\\Fish Jam Fever.apk` "
                            "(package `com.puzzle.fish.jam.fever`, versionName 1.0.7, versionCode 11)",
                     where="89 AudioClip trong `assets/bin/Data/sharedassets0.assets` (dữ liệu trong `sharedassets0.resource`) và 1 clip "
                           "trong file asset đặt tên theo mã băm `5db43453fe160fb4192c261866e2d480` (kèm `.resource` cùng tên); "
                           "FSB5 Vorbis + IMA ADPCM. Các file lớn được Unity cắt thành mảnh 1 MB `.split0`, `.split1`, …",
                     method="Đọc AudioClip bằng UnityPy (tự ghép các mảnh `.splitN`), dựng lại Ogg Vorbis từ FSB5 bằng python-fsb5; "
                            "12 clip IMA ADPCM được giải mã sang WAV PCM16.",
                     notes=["4 clip trùng nội dung 100 % chỉ giữ 1 bản.",
                            "Nhạc nền: `BG_Music` (1 ph 17 s) và `BgMusic_01` (1 ph 25 s); còn lại là SFX gameplay/UI và giọng khen (`SFX_amazing`, `SFX_perfect`...).",
                            "Các bundle Addressables (cá, icon cá, IAP, level) không chứa AudioClip.",
                            "Phần ảnh/data/code của game nằm cùng thư mục, xem README.md."]),
}'''
if "\"FishJamFever\": dict(" not in s:
    assert anchor in s
    s = s.replace(anchor, entry)
    open(p, "w", encoding="utf-8").write(s)
print("ok")
