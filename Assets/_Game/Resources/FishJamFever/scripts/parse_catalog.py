"""Decode Addressables catalog.json (binary-packed key/entry tables) -> addressables_catalog.csv
columns: key, internal_id, provider, dependency_key, bundle(file)"""
import json, base64, struct, csv, os, sys
ROOT = r"D:\ExtractApk\FishJamFever"
cat = json.load(open(os.path.join(ROOT, "extracted", "aa", "catalog.json"), encoding="utf-8"))

def b64(s): return base64.b64decode(s)
key_data = b64(cat["m_KeyDataString"])
bucket_data = b64(cat["m_BucketDataString"])
entry_data = b64(cat["m_EntryDataString"])
extra_data = b64(cat["m_ExtraDataString"])
internal_ids = cat["m_InternalIds"]
providers = cat["m_ProviderIds"]
res_types = cat["m_resourceTypes"]

def read_obj(data, off):
    t = data[off]; off += 1
    if t == 0:  # AsciiString
        n = struct.unpack_from("<i", data, off)[0]; off += 4
        return data[off:off+n].decode("ascii"), off+n
    if t == 1:  # UnicodeString
        n = struct.unpack_from("<i", data, off)[0]; off += 4
        return data[off:off+n].decode("utf-16-le"), off+n
    if t == 2: return struct.unpack_from("<H", data, off)[0], off+2
    if t == 3: return struct.unpack_from("<I", data, off)[0], off+4
    if t == 4: return struct.unpack_from("<i", data, off)[0], off+4
    if t == 7:  # JsonObject
        asm_n = data[off]; off += 1; asm = data[off:off+asm_n].decode("ascii"); off += asm_n
        cls_n = data[off]; off += 1; cls = data[off:off+cls_n].decode("ascii"); off += cls_n
        n = struct.unpack_from("<i", data, off)[0]; off += 4
        js = data[off:off+n].decode("utf-16-le"); off += n
        return {"cls": cls, "json": js}, off
    if t == 8: return data[off:off+16].hex(), off+16  # hash128
    if t == 9: return data[off:off+4].hex(), off+4
    raise ValueError(f"unknown type {t}")

# keys
nkeys = struct.unpack_from("<i", key_data, 0)[0]
keys = []; off = 4
for _ in range(nkeys):
    k, off = read_obj(key_data, off); keys.append(k)

# buckets: dataOffset + entries list
nb = struct.unpack_from("<i", bucket_data, 0)[0]; off = 4
buckets = []
for _ in range(nb):
    doff, ne = struct.unpack_from("<ii", bucket_data, off); off += 8
    ents = list(struct.unpack_from(f"<{ne}i", bucket_data, off)); off += 4*ne
    buckets.append((doff, ents))

# entries: 7 ints each
ne = struct.unpack_from("<i", entry_data, 0)[0]; off = 4
entries = []
for _ in range(ne):
    iid, prov, dep, depHash, extra, pk, rtype = struct.unpack_from("<iiiiiii", entry_data, off); off += 28
    entries.append(dict(internal_id=internal_ids[iid], provider=providers[prov], dep_key_index=dep, extra=extra, primary_key=pk, rtype=rtype))

def keystr(k):
    if isinstance(k, dict): return k["cls"]
    return str(k)

rows = []
for ki, (k, (doff, ents)) in enumerate(zip(keys, buckets)):
    for ei in ents:
        e = entries[ei]
        dep = keystr(keys[e["dep_key_index"]]) if e["dep_key_index"] >= 0 else ""
        rt = res_types[e["rtype"]]["m_ClassName"] if 0 <= e["rtype"] < len(res_types) else ""
        rows.append([keystr(k), e["internal_id"], e["provider"].split(".")[-1], rt.split(".")[-1], dep, keystr(keys[e["primary_key"]])])
out = os.path.join(ROOT, "addressables_catalog.csv")
with open(out, "w", newline="", encoding="utf-8") as fh:
    w = csv.writer(fh); w.writerow(["key", "internal_id", "provider", "resource_type", "dependency", "primary_key"])
    w.writerows(rows)
print("keys", nkeys, "entries", ne, "rows", len(rows), "->", out)
# summary: bundles
bund = [e for e in entries if e["provider"].endswith("AssetBundleProvider")]
print("bundles:", len(bund))
for e in bund: print("  ", e["internal_id"])
import collections
c = collections.Counter(r[3] for r in rows)
print("resource types:", c.most_common())
