"""Add FishJamFever to the shared sound pipeline configs in D:\\ExtractApk\\_sound_tools."""
import os
T = r"D:\ExtractApk\_sound_tools"

p = os.path.join(T, "extract_sounds.py")
s = open(p, encoding="utf-8").read()
anchor = 'kind="unity", prefixes=["assets/bin/Data/", "assets/aa/"], title="Blocked In (Tripledot)"),\n}'
add = ('kind="unity", prefixes=["assets/bin/Data/", "assets/aa/"], title="Blocked In (Tripledot)"),\n'
       '    # full APK with 1 MB .splitN chunks; AudioClips in sharedassets0 + hashed resfiles, sample data in .resource (FSB5)\n'
       '    "FishJamFever":    dict(apks=[r"C:\\Users\\nguye\\Documents\\MuMuSharedFolder\\Download\\Share\\Fish Jam Fever.apk"],\n'
       '                            kind="unity", prefixes=["assets/bin/Data/", "assets/aa/"], title="Fish Jam Fever", bundle_folders=True),\n}')
if "FishJamFever" not in s:
    assert anchor in s
    s = s.replace(anchor, add)
    open(p, "w", encoding="utf-8").write(s)

p = os.path.join(T, "gen_player.py")
s = open(p, encoding="utf-8").read()
anchor = '    "FishSort":         ("Fish Sort Puzzle", "Unity · YooAsset"),'
if "FishJamFever" not in s:
    assert anchor in s
    s = s.replace(anchor, '    "FishJamFever":     ("Fish Jam Fever", "Unity 2022.3 · Addressables"),\n' + anchor)
    open(p, "w", encoding="utf-8").write(s)
print("registered")
