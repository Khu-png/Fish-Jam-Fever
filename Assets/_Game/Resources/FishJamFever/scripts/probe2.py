import os, csv, collections, json, UnityPy
ROOT = r"D:\ExtractApk\FishJamFever"
D = os.path.join(ROOT, "extracted", "bin_Data")
env = UnityPy.load(os.path.join(D, "globalgamemanagers"))
for obj in env.objects:
    print(obj.type.name)
    if obj.type.name == "ResourceManager":
        d = obj.read_typetree()
        cont = d.get("m_Container", [])
        print("ResourceManager containers:", len(cont))
        for k, v in cont[:15]:
            print("  ", k, v)
        # externals of globalgamemanagers
    if obj.type.name == "BuildSettings":
        d = obj.read_typetree()
        print("scenes:", d.get("scenes"), "version", d.get("m_Version"))
    if obj.type.name == "PlayerSettings":
        d = obj.read_typetree()
        print("product:", d.get("productName"), d.get("companyName"), d.get("bundleVersion"))
f = list(env.files.values())[0]
print("externals:", [(e.path) for e in f.externals][:10], len(f.externals))

# index.csv containers summary
c = collections.Counter()
with open(os.path.join(ROOT, "images", "index.csv"), encoding="utf-8") as fh:
    for r in csv.DictReader(fh):
        c[(r["kind"], r["source"][:12], r["container"].split("/")[0] if r["container"] else "-")] += 1
for k, v in c.most_common(30): print(k, v)

# hashed file: what's inside? root GameObjects names
names = sorted(n for n in os.listdir(D) if len(n) == 32)
for n in names[:6]:
    e = UnityPy.load(os.path.join(D, n))
    f = list(e.files.values())[0]
    gos = [o.read().m_Name for o in e.objects if o.type.name == "GameObject"]
    tex = [o.read().m_Name for o in e.objects if o.type.name in ("Texture2D", "Sprite", "TextAsset", "Font")]
    print(n, "externals", [x.path for x in f.externals], "GO", gos[:3], "assets", tex[:4])
