"""Join Unity .splitN files from apk/assets/bin/Data into extracted/bin_Data (joined),
and copy the rest (hashed serialized files, .resource, bundles) unchanged."""
import os, re, shutil, sys
SRC = r"D:\ExtractApk\FishJamFever\apk\assets"
DST = r"D:\ExtractApk\FishJamFever\extracted"
os.makedirs(os.path.join(DST, "bin_Data"), exist_ok=True)
os.makedirs(os.path.join(DST, "aa"), exist_ok=True)

data_dir = os.path.join(SRC, "bin", "Data")
groups = {}
plain = []
for n in os.listdir(data_dir):
    p = os.path.join(data_dir, n)
    if os.path.isdir(p):
        continue
    m = re.match(r"^(.*)\.split(\d+)$", n)
    if m:
        groups.setdefault(m.group(1), []).append((int(m.group(2)), p))
    else:
        plain.append(p)

for base, parts in sorted(groups.items()):
    parts.sort()
    out = os.path.join(DST, "bin_Data", base)
    with open(out, "wb") as fo:
        for _, p in parts:
            with open(p, "rb") as fi:
                shutil.copyfileobj(fi, fo, 1 << 20)
    print("joined", base, len(parts), "parts", os.path.getsize(out))

for p in plain:
    shutil.copy2(p, os.path.join(DST, "bin_Data", os.path.basename(p)))
print("copied", len(plain), "plain files")

# Managed metadata
md = os.path.join(data_dir, "Managed", "Metadata", "global-metadata.dat")
os.makedirs(os.path.join(DST, "bin_Data", "Managed"), exist_ok=True)
shutil.copy2(md, os.path.join(DST, "bin_Data", "Managed", "global-metadata.dat"))

# Addressables
for n in os.listdir(os.path.join(SRC, "aa", "Android")):
    shutil.copy2(os.path.join(SRC, "aa", "Android", n), os.path.join(DST, "aa", n))
for n in ("catalog.json", "settings.json"):
    shutil.copy2(os.path.join(SRC, "aa", n), os.path.join(DST, "aa", n))
print("done")
