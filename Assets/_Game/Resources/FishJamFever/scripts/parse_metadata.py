import struct, json, os, collections, sys
P   = r"D:\ExtractApk\FishJamFever\extracted\bin_Data\Managed\global-metadata.dat"
OUT = r"D:\ExtractApk\FishJamFever\code"
os.makedirs(OUT, exist_ok=True)
data = open(P,'rb').read()
HDR = ["stringLiteral","stringLiteralData","string","events","properties","methods","parameterDefaultValues",
 "fieldDefaultValues","fieldAndParameterDefaultValueData","fieldMarshaledSizes","parameters","fields",
 "genericParameters","genericParameterConstraints","genericContainers","nestedTypes","interfaces","vtableMethods",
 "interfaceOffsets","typeDefinitions","images","assemblies","fieldRefs","referencedAssemblies","attributeData",
 "attributeDataRange","unresolvedIndirectCallParameterTypes","unresolvedIndirectCallParameterRanges",
 "windowsRuntimeTypeNames","windowsRuntimeStrings","exportedTypeDefinitions"]
sanity, version = struct.unpack_from("<Ii", data, 0); assert sanity == 0xFAB11BAF, hex(sanity)
H = {}; off = 8
for n in HDR:
    H[n] = struct.unpack_from("<Ii", data, off); off += 8
print("metadata version", version, "header end", off, "stringLiteralOffset", H["stringLiteral"][0])
sOff, sSize = H["string"]
def S(idx):
    if idx < 0: return ""
    e = data.index(b'\0', sOff+idx); return data[sOff+idx:e].decode('utf-8','replace')

# ---- string literals
lo, ls = H["stringLiteral"]; ldo, lds = H["stringLiteralData"]
lits = []
for i in range(ls//8):
    ln, di = struct.unpack_from("<Ii", data, lo+i*8)
    lits.append(data[ldo+di:ldo+di+ln].decode('utf-8','replace'))
with open(os.path.join(OUT,"string_literals.txt"),"w",encoding="utf-8") as f:
    for i,s in enumerate(lits): f.write(f"{i}\t{s!r}\n")
print("string literals:", len(lits))

# ---- images / assemblies
io, isz = H["images"]; IMG_SZ = 40
images = []
for i in range(isz//IMG_SZ):
    v = struct.unpack_from("<iiiiiiiIii", data, io+i*IMG_SZ)
    images.append(dict(name=S(v[0]), assemblyIndex=v[1], typeStart=v[2], typeCount=v[3], exportedTypeStart=v[4], exportedTypeCount=v[5], entryPoint=v[6], token=v[7], caStart=v[8], caCount=v[9]))
print("images:", [(im["name"], im["typeCount"]) for im in images])

# ---- type definitions (v29+: 88 bytes)
to, tsz = H["typeDefinitions"]; TD_SZ = 88
assert tsz % TD_SZ == 0, tsz
ntypes = tsz//TD_SZ
types = []
for i in range(ntypes):
    v = struct.unpack_from("<iiiiiiiIiiiiiiiiHHHHHHHHII", data, to+i*TD_SZ)
    types.append(dict(idx=i, name=S(v[0]), ns=S(v[1]), byvalTypeIndex=v[2], declaringTypeIndex=v[3], parentIndex=v[4], elementTypeIndex=v[5],
        genericContainerIndex=v[6], flags=v[7], fieldStart=v[8], methodStart=v[9], eventStart=v[10], propertyStart=v[11], nestedTypesStart=v[12],
        interfacesStart=v[13], vtableStart=v[14], interfaceOffsetsStart=v[15], method_count=v[16], property_count=v[17], field_count=v[18],
        event_count=v[19], nested_type_count=v[20], vtable_count=v[21], interfaces_count=v[22], interface_offsets_count=v[23], bitfield=v[24], token=v[25]))
print("types:", ntypes)

# ---- methods: v31 = 36 bytes (returnParameterToken added), v29 = 32
mo, msz = H["methods"]
M_SZ = 36 if version >= 31 else 32
assert msz % M_SZ == 0, (msz, M_SZ)
methods = []
for i in range(msz//M_SZ):
    if M_SZ == 36:
        v = struct.unpack_from("<iiiIiiIHHHH", data, mo+i*M_SZ)
        methods.append(dict(name=S(v[0]), declaringType=v[1], returnType=v[2], parameterStart=v[4], genericContainerIndex=v[5], token=v[6], flags=v[7], iflags=v[8], slot=v[9], parameterCount=v[10]))
    else:
        v = struct.unpack_from("<iiiiiIHHHH", data, mo+i*M_SZ)
        methods.append(dict(name=S(v[0]), declaringType=v[1], returnType=v[2], parameterStart=v[3], genericContainerIndex=v[4], token=v[5], flags=v[6], iflags=v[7], slot=v[8], parameterCount=v[9]))
print("methods:", len(methods), "sample:", [m["name"] for m in methods[:5]])

# ---- params, fields, props, events
po, psz = H["parameters"]; params = [struct.unpack_from("<iIi", data, po+i*12) for i in range(psz//12)]
fo, fsz = H["fields"]; fields = [struct.unpack_from("<iiI", data, fo+i*12) for i in range(fsz//12)]
pro, prsz = H["properties"]; props = [struct.unpack_from("<iiiII", data, pro+i*20) for i in range(prsz//20)]
eo, esz = H["events"]; events = [struct.unpack_from("<iiiiiI", data, eo+i*24) for i in range(esz//24)]
print("params", len(params), "fields", len(fields), "props", len(props), "events", len(events))

# ---- nested types table (TypeDefinitionIndex) -> resolve nesting for names
nto, ntsz = H["nestedTypes"]; nested = [struct.unpack_from("<i", data, nto+i*4)[0] for i in range(ntsz//4)]
parent_of = {}
for t in types:
    for k in range(t["nestedTypesStart"], t["nestedTypesStart"]+t["nested_type_count"]) if t["nestedTypesStart"]>=0 else []:
        parent_of[nested[k]] = t["idx"]

# ---- field default values: (fieldIndex, typeIndex, dataIndex) 12 bytes
fdo, fdsz = H["fieldDefaultValues"]; ddo, ddsz = H["fieldAndParameterDefaultValueData"]
fdv = [struct.unpack_from("<iii", data, fdo+i*12) for i in range(fdsz//12)]
pdo, pdsz = H["parameterDefaultValues"]
pdv = [struct.unpack_from("<iii", data, pdo+i*12) for i in range(pdsz//12)]
all_di = sorted(set([x[2] for x in fdv if x[2] >= 0] + [x[2] for x in pdv if x[2] >= 0] + [ddsz]))
nxt = {a:b for a,b in zip(all_di, all_di[1:])}

# metadata v29+ stores int32/uint32 constants and string lengths as compressed zigzag varints
def read_cu32(b, pos):
    r = b[pos]
    if r & 0x80 == 0: return r, pos+1
    if r & 0xC0 == 0x80: return ((r & 0x7f) << 8) | b[pos+1], pos+2
    if r & 0xE0 == 0xC0: return ((r & 0x3f) << 24) | (b[pos+1] << 16) | (b[pos+2] << 8) | b[pos+3], pos+4
    if r == 0xF0: return struct.unpack_from("<I", b, pos+1)[0], pos+5
    if r == 0xFE: return 0xFFFFFFFE, pos+1
    if r == 0xFF: return 0xFFFFFFFF, pos+1
    raise ValueError("bad compressed int")
def read_ci32(b, pos):
    e, p = read_cu32(b, pos)
    if e == 0xFFFFFFFF: return -2**31, p
    neg = e & 1; e >>= 1
    return (-(e+1) if neg else e), p

# field index -> owning type; owning type is enum?
owner_of_field = {}
for t in types:
    if t["fieldStart"] >= 0:
        for fi in range(t["fieldStart"], t["fieldStart"]+t["field_count"]): owner_of_field[fi] = t["idx"]
field_name_idx = {}
for t in types:
    if t["fieldStart"] >= 0:
        for fi in range(t["fieldStart"], t["fieldStart"]+t["field_count"]): field_name_idx[(t["ns"], t["name"], S(fields[fi][0]))] = fi
fdv_by_field = {f: (ti, di) for (f, ti, di) in fdv}

# learn typeIndex -> primitive kind from well-known mscorlib constants
known = {}
LEARN = [("System","Int32","MaxValue","i4"),("System","UInt32","MaxValue","u4"),("System","Byte","MaxValue","u1"),("System","SByte","MaxValue","i1"),
         ("System","Boolean","True","bool"),("System","Char","MaxValue","char"),("System","Int16","MaxValue","i2"),("System","UInt16","MaxValue","u2"),
         ("System","Int64","MaxValue","i8"),("System","UInt64","MaxValue","u8"),("System","Single","MaxValue","r4"),("System","Double","MaxValue","r8"),
         ("System","Boolean","TrueLiteral","string"),("System","DateTime","MillisPerSecond","i4"),("System","TimeSpan","MillisecondsPerTick","r8"),
         ("System","Math","PI","r8"),("System","DateTime","MaxMillis","i8"),("System","Decimal","SignMask","i4"),("System","Decimal","ScaleShift","i4")]
for ns, tn, fn, kind in LEARN:
    fi = field_name_idx.get((ns, tn, fn))
    if fi is not None and fi in fdv_by_field: known[fdv_by_field[fi][0]] = kind
print("learned typeIndex kinds:", known)
# blob sizes per typeIndex (for fallback heuristics)
ti_sizes = collections.defaultdict(set)
for (f, ti, di) in fdv:
    if di >= 0: ti_sizes[ti].add(nxt.get(di, ddsz) - di)
for (p, ti, di) in pdv:
    if di >= 0: ti_sizes[ti].add(nxt.get(di, ddsz) - di)

def try_string(blob):
    try:
        ln, p = read_ci32(blob, 0)
        if ln >= 0 and ln == len(blob) - p:
            return blob[p:].decode('utf-8')
    except Exception: pass
    return None

def decode_default(ti, di, is_enum_owner):
    if di < 0: return None
    end = nxt.get(di, ddsz); sz = end - di; blob = data[ddo+di:ddo+end]
    kind = known.get(ti)
    if kind is None:
        if is_enum_owner: kind = "i4"
        else:
            sizes = ti_sizes.get(ti, {sz})
            if len(sizes) > 1: kind = "i4?"      # variable-size => compressed int or string
            else: kind = {8:"i8", 4:"i4?", 2:"i2", 1:"i4?"}.get(sz, "i4?")
    try:
        if kind == "string": return blob[read_ci32(blob,0)[1]:].decode('utf-8','replace')
        if kind in ("i4",): return read_ci32(blob, 0)[0]
        if kind == "u4": return read_cu32(blob, 0)[0]
        if kind == "i4?":
            s = try_string(blob) if sz > 1 else None
            if s is not None and all(31 < ord(c) < 0x10000 or c in "\r\n\t" for c in s): return s
            v, p = read_ci32(blob, 0)
            if p == sz: return v
            return blob.hex()
        if kind == "u1": return blob[0]
        if kind == "i1": return struct.unpack("<b", blob[:1])[0]
        if kind == "bool": return bool(blob[0])
        if kind == "char": return struct.unpack("<H", blob[:2])[0]
        if kind == "i2": return struct.unpack("<h", blob[:2])[0]
        if kind == "u2": return struct.unpack("<H", blob[:2])[0]
        if kind == "i8": return struct.unpack("<q", blob[:8])[0]
        if kind == "u8": return struct.unpack("<Q", blob[:8])[0]
        if kind == "r4": return struct.unpack("<f", blob[:4])[0]
        if kind == "r8": return struct.unpack("<d", blob[:8])[0]
    except Exception:
        pass
    return blob.hex()

field_default = {f: decode_default(ti, di, bool(types[owner_of_field.get(f, 0)]["bitfield"] & 2) if f in owner_of_field else False) for (f, ti, di) in fdv}
param_default = {p: decode_default(ti, di, False) for (p, ti, di) in pdv}

# ---- build model
img_of_type = {}
for im in images:
    for ti in range(im["typeStart"], im["typeStart"]+im["typeCount"]): img_of_type[ti] = im["name"]
def acc(flags):
    return {1:"private",2:"private",3:"internal",4:"protected",5:"protected internal",6:"public"}.get(flags & 7,"")
def qual_name(t):
    parts = [t["name"]]; cur = t["idx"]
    while cur in parent_of:
        cur = parent_of[cur]; parts.insert(0, types[cur]["name"])
    return ".".join(parts)

model = []
for t in types:
    entry = dict(image=img_of_type.get(t["idx"],"?"), namespace=t["ns"], name=qual_name(t), token=t["token"],
                 is_enum=bool(t["bitfield"]&2), is_valuetype=bool(t["bitfield"]&1),
                 is_interface=bool(t["flags"]&0x20), is_abstract=bool(t["flags"]&0x80), is_sealed=bool(t["flags"]&0x100),
                 fields=[], methods=[], properties=[], events=[])
    for fi in range(t["fieldStart"], t["fieldStart"]+t["field_count"]) if t["fieldStart"]>=0 else []:
        nm, ti, tok = fields[fi]
        fe = dict(name=S(nm))
        if fi in field_default: fe["default"] = field_default[fi]
        entry["fields"].append(fe)
    for mi in range(t["methodStart"], t["methodStart"]+t["method_count"]) if t["methodStart"]>=0 else []:
        m = methods[mi]
        ps = []
        for pi in range(m["parameterStart"], m["parameterStart"]+m["parameterCount"]):
            pn = S(params[pi][0]); pe = pn
            if pi in param_default: pe = f"{pn} = {param_default[pi]!r}"
            ps.append(pe)
        entry["methods"].append(dict(name=m["name"], access=acc(m["flags"]), static=bool(m["flags"]&0x10), virtual=bool(m["flags"]&0x40), abstract=bool(m["flags"]&0x400), params=ps, token=m["token"]))
    for pi in range(t["propertyStart"], t["propertyStart"]+t["property_count"]) if t["propertyStart"]>=0 else []:
        entry["properties"].append(S(props[pi][0]))
    for ei in range(t["eventStart"], t["eventStart"]+t["event_count"]) if t["eventStart"]>=0 else []:
        entry["events"].append(S(events[ei][0]))
    model.append(entry)

with open(os.path.join(OUT,"types.json"),"w",encoding="utf-8") as f: json.dump(model, f, ensure_ascii=False, indent=1)

# ---- dump.cs-like text, grouped by image
by_img = collections.defaultdict(list)
for e in model: by_img[e["image"]].append(e)
for img, ents in by_img.items():
    fn = os.path.join(OUT, "dump_"+img.replace(".dll","")+".cs")
    with open(fn,"w",encoding="utf-8") as f:
        f.write(f"// Image: {img}  types={len(ents)}\n// NOTE: field/param TYPES are not recoverable from global-metadata.dat alone (they live in libil2cpp.so). Names, enums and constants are exact.\n\n")
        for e in ents:
            kind = "enum" if e["is_enum"] else "interface" if e["is_interface"] else "struct" if e["is_valuetype"] else "class"
            mods = ("abstract " if e["is_abstract"] and not e["is_interface"] else "") + ("sealed " if e["is_sealed"] and not e["is_enum"] else "")
            ns = f"namespace {e['namespace']} " if e["namespace"] else ""
            f.write(f"// Token 0x{e['token']:08X}\n{ns}{mods}{kind} {e['name']}\n{{\n")
            if e["is_enum"]:
                for fe in e["fields"]:
                    if fe["name"] == "value__": continue
                    f.write(f"\t{fe['name']} = {fe.get('default','?')},\n")
            else:
                if e["fields"]:
                    f.write("\t// Fields\n")
                    for fe in e["fields"]:
                        if "default" in fe: f.write(f"\tconst {fe['name']} = {fe['default']!r};\n")
                        else: f.write(f"\t{fe['name']};\n")
                if e["properties"]:
                    f.write("\t// Properties\n")
                    for p in e["properties"]: f.write(f"\t{p} {{ get; set; }}\n")
                if e["events"]:
                    f.write("\t// Events\n")
                    for ev in e["events"]: f.write(f"\tevent {ev};\n")
                if e["methods"]:
                    f.write("\t// Methods\n")
                    for m in e["methods"]:
                        mm = (m["access"]+" " if m["access"] else "") + ("static " if m["static"] else "") + ("abstract " if m["abstract"] else "virtual " if m["virtual"] else "")
                        f.write(f"\t{mm}{m['name']}({', '.join(m['params'])});\n")
            f.write("}\n\n")
    print("wrote", os.path.basename(fn), len(ents))

with open(os.path.join(OUT,"enums.txt"),"w",encoding="utf-8") as f:
    for e in model:
        if e["is_enum"]:
            f.write(f"[{e['image']}] {e['namespace']+'.' if e['namespace'] else ''}{e['name']}: " + ", ".join(f"{fe['name']}={fe.get('default','?')}" for fe in e["fields"] if fe["name"]!="value__") + "\n")
print("OK")
