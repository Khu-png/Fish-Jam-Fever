import os, sys, collections, UnityPy
ROOT = r"D:\ExtractApk\FishJamFever\extracted"
D = os.path.join(ROOT, "bin_Data")
env = UnityPy.load(os.path.join(D, "globalgamemanagers"))
for f in env.files.values():
    print("unity version:", getattr(f, "unity_version", None), "fmt", getattr(f, "version", None))
    break
# overall type counts per file group
tot = collections.Counter()
per = {}
names = sorted(os.listdir(D))
for n in names:
    p = os.path.join(D, n)
    if os.path.isdir(p) or n.endswith(".resource") or n in ("boot.config", "unity_app_guid") or n.endswith(".json"):
        continue
    try:
        e = UnityPy.load(p)
    except Exception as ex:
        print("LOADERR", n, str(ex)[:100]); continue
    c = collections.Counter(o.type.name for o in e.objects)
    per[n] = c
    tot.update(c)
hashed = [n for n in per if len(n) == 32]
print("hashed files:", len(hashed))
hc = collections.Counter()
for n in hashed: hc.update(per[n])
print("hashed types:", hc.most_common(25))
for n in per:
    if len(n) != 32:
        print(n, sum(per[n].values()), per[n].most_common(12))
print("TOTAL", tot.most_common(30))
# aa bundles
A = os.path.join(ROOT, "aa")
ac = collections.Counter()
for n in sorted(os.listdir(A)):
    if not n.endswith(".bundle"): continue
    e = UnityPy.load(os.path.join(A, n))
    c = collections.Counter(o.type.name for o in e.objects)
    ac.update(c)
    print("aa", n[:40], c.most_common(8))
print("AA TOTAL", ac.most_common(20))
