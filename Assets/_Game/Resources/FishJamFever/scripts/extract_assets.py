"""Extract Texture2D / Sprite / TextAsset / Font / MonoBehaviour(typetree) / Mesh-less assets
from Fish Jam Fever (Unity 2022.3.67f2). Sources: extracted/bin_Data/* (joined) + extracted/aa/*.bundle."""
import os, glob, csv, time, sys, json, re, traceback
import UnityPy
from UnityPy.enums import ClassIDType

ROOT = r"D:\ExtractApk\FishJamFever\extracted"
OUT_IMG = r"D:\ExtractApk\FishJamFever\images"
OUT_DATA = r"D:\ExtractApk\FishJamFever\data"
for d in (OUT_IMG, OUT_DATA):
    os.makedirs(d, exist_ok=True)

SKIP = {"boot.config", "unity_app_guid", "RuntimeInitializeOnLoads.json", "ScriptingAssemblies.json"}
files = []
D = os.path.join(ROOT, "bin_Data")
for n in sorted(os.listdir(D)):
    p = os.path.join(D, n)
    if os.path.isdir(p) or n in SKIP or n.endswith(".resource"):
        continue
    files.append(p)
files += sorted(glob.glob(os.path.join(ROOT, "aa", "*.bundle")))

t0 = time.time()
env = UnityPy.Environment()
for f in files:
    try:
        env.load_file(f)
    except Exception as e:
        print("LOAD_ERROR", os.path.basename(f), str(e)[:150], flush=True)
print(f"loaded {len(files)} files in {time.time()-t0:.0f}s", flush=True)

# --- Resources/ mapping: globalgamemanagers.ResourceManager.m_Container -> (external file, path_id) -> resources path
resmap = {}    # (file basename, path_id) -> "Resources/<path>"
filedir = {}   # hashed file basename -> "Resources/<dir of path>"
ggm = env.files.get("globalgamemanagers") or next((v for k, v in env.files.items() if os.path.basename(str(k)) == "globalgamemanagers"), None)
if ggm is not None:
    ext = [os.path.basename(e.path) for e in ggm.externals]
    for obj in ggm.objects.values():
        if obj.type.name == "ResourceManager":
            for path, ref in obj.read_typetree()["m_Container"]:
                fid, pid = ref["m_FileID"], ref["m_PathID"]
                fname = "globalgamemanagers" if fid == 0 else ext[fid - 1]
                resmap[(fname, pid)] = "Resources/" + path
                if re.fullmatch(r"[0-9a-f]{32}", fname):
                    filedir[fname] = "Resources/" + os.path.dirname(path)
print("resource map entries:", len(resmap), "hashed files mapped:", len(filedir), flush=True)

SAFE = re.compile(r'[<>:"|?*\x00-\x1f]')
def clean(s):
    s = SAFE.sub("_", s or "")
    return s.strip(" .") or "unnamed"

def src_of(obj):
    try:
        af = obj.assets_file
        par = getattr(af, "parent", None)
        nm = getattr(par, "name", None) or getattr(af, "name", None) or "?"
        return os.path.basename(str(nm))
    except Exception:
        return "?"

def short_src(src):
    # bundle name without hash suffix, hashed serialized file -> "data_<hash8>"
    if src == "sharedassets0.assets":
        return "SceneGame"
    if src.endswith(".bundle"):
        return re.sub(r"_[0-9a-f]{32}\.bundle$", "", src)
    if re.fullmatch(r"[0-9a-f]{32}", src):
        return "resfile_" + src[:8]
    return src

def target(base, sub, container, name, path_id, ext, src):
    if container and container.lower().startswith("assets/"):
        rel = container[len("assets/"):]
        # Addressables/Resources give dependencies the address of their main asset, so keep only the
        # directory and use the object's own name for the file name
        nm = name or os.path.splitext(os.path.basename(rel))[0]
        if sub in ("textures", "sprites") and nm.lower().endswith((".png", ".jpg", ".jpeg")):
            nm = os.path.splitext(nm)[0]
        rel = os.path.dirname(rel) + "/" + nm
        p = os.path.join(base, sub, *[clean(x) for x in rel.strip("/").split("/")])
        if sub != "text" or not os.path.splitext(p)[1]:
            p += ext
    else:
        p = os.path.join(base, sub, "_by_source", short_src(src), f"{clean(name)}{ext}")
    if os.path.exists(p):
        stem, e = os.path.splitext(p); p = f"{stem}_{path_id}{e}"
    os.makedirs(os.path.dirname(p), exist_ok=True)
    return p

idx = open(os.path.join(OUT_IMG, "index.csv"), "w", newline="", encoding="utf-8")
w = csv.writer(idx); w.writerow(["kind", "file", "name", "size", "container", "source", "path_id"])
didx = open(os.path.join(OUT_DATA, "index.csv"), "w", newline="", encoding="utf-8")
dw = csv.writer(didx); dw.writerow(["kind", "file", "name", "size", "container", "source", "path_id", "script"])

stats = {"Texture2D": 0, "Sprite": 0, "TextAsset": 0, "Font": 0, "MonoBehaviour": 0, "MB_notree": 0, "err": 0}
n = 0
for obj in env.objects:
    t = obj.type.name
    if t not in ("Texture2D", "Sprite", "TextAsset", "Font", "MonoBehaviour"):
        continue
    n += 1
    src = src_of(obj)
    cont = obj.container or ""
    if not cont:
        rp = resmap.get((src, obj.path_id))
        if rp:
            cont = "Assets/" + rp
        elif src in filedir:
            try:
                nm = getattr(obj.read(), "m_Name", "") or ""
            except Exception:
                nm = ""
            cont = "Assets/" + filedir[src] + "/" + (nm or "unnamed")
    name = ""
    try:
        if t == "MonoBehaviour":
            sname = ""
            try:
                d0 = obj.read()
                name = getattr(d0, "m_Name", "") or ""
                sc = d0.m_Script.read() if getattr(d0, "m_Script", None) else None
                if sc is not None:
                    sname = f"{sc.m_Namespace}.{sc.m_ClassName}".strip(".")
            except Exception:
                pass
            if not obj.serialized_type or not getattr(obj.serialized_type, "node", None) and not getattr(obj.serialized_type, "nodes", None):
                stats["MB_notree"] += 1
                dw.writerow(["monobehaviour_notree", "", name, obj.byte_size, cont, src, obj.path_id, sname])
                continue
            tree = obj.read_typetree()
            if not name:
                name = tree.get("m_Name", "") or ""
            fname = clean(name) or clean(sname) or "mb"
            p = target(OUT_DATA, "monobehaviour", cont, fname + (f"__{clean(sname)}" if sname else ""), obj.path_id, ".json", src)
            with open(p, "w", encoding="utf-8") as fh:
                json.dump(tree, fh, ensure_ascii=False, indent=1, default=lambda o: str(o))
            dw.writerow(["monobehaviour", os.path.relpath(p, OUT_DATA), name, os.path.getsize(p), cont, src, obj.path_id, sname])
            stats["MonoBehaviour"] += 1
            continue
        d = obj.read()
        name = getattr(d, "m_Name", "") or ""
        if t == "Texture2D":
            if d.m_Width == 0 or d.m_Height == 0:
                continue
            img = d.image
            p = target(OUT_IMG, "textures", cont, name, obj.path_id, ".png", src)
            img.save(p)
            w.writerow(["texture", os.path.relpath(p, OUT_IMG), name, f"{img.width}x{img.height}", cont, src, obj.path_id])
            stats["Texture2D"] += 1
        elif t == "Sprite":
            img = d.image
            p = target(OUT_IMG, "sprites", cont, name, obj.path_id, ".png", src)
            img.save(p)
            w.writerow(["sprite", os.path.relpath(p, OUT_IMG), name, f"{img.width}x{img.height}", cont, src, obj.path_id])
            stats["Sprite"] += 1
        elif t == "TextAsset":
            s = d.m_Script
            b = s.encode("utf-8", "surrogateescape") if isinstance(s, str) else bytes(s)
            ext = ".bytes"
            bs = b.lstrip()[:1]
            if bs in (b"{", b"["):
                ext = ".json"
            elif b[:4] == b"\x89PNG":
                ext = ".png"
            elif b[:2] == b"\xff\xd8":
                ext = ".jpg"
            elif name.lower().endswith((".atlas", ".txt")):
                ext = ""
            p = target(OUT_DATA, "text", cont, name, obj.path_id, ext, src)
            open(p, "wb").write(b)
            dw.writerow(["text", os.path.relpath(p, OUT_DATA), name, len(b), cont, src, obj.path_id, ""])
            stats["TextAsset"] += 1
        elif t == "Font":
            fd = getattr(d, "m_FontData", None)
            if fd:
                b = bytes(fd)
                ext = ".otf" if b[:4] == b"OTTO" else ".ttf"
                p = target(OUT_DATA, "fonts", cont, name, obj.path_id, ext, src)
                open(p, "wb").write(b)
                dw.writerow(["font", os.path.relpath(p, OUT_DATA), name, len(b), cont, src, obj.path_id, ""])
                stats["Font"] += 1
    except Exception as e:
        stats["err"] += 1
        w.writerow(["ERROR", t, name, "", cont, src, obj.path_id])
        print("ERR", t, obj.path_id, src, repr(name)[:40], str(e)[:120].replace("\n", " "), flush=True)
    if n % 1000 == 0:
        print(n, stats, f"{time.time()-t0:.0f}s", flush=True)
idx.close(); didx.close()
print("DONE", stats, f"{time.time()-t0:.0f}s")
