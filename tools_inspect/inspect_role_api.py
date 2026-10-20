# -*- coding: utf-8 -*-
"""Read-only inspection of local RimWorld Assembly-CSharp.dll metadata.
Enumerates RoleEffect subclasses and Precept_Role members for feature planning."""
import dnfile

DLL = r"D:\Steam\steamapps\common\RimWorld\Assembly-CSharp.dll"
pe = dnfile.dnPE(DLL)

def s(x):
    return x.value if hasattr(x, "value") else x

def dump_type(short):
    found = False
    for t in pe.net.mdtables.TypeDef:
        if s(t.TypeName) == short:
            found = True
            print(f"\n=== {s(t.TypeNamespace)}.{s(t.TypeName)} ===")
            print("  [fields]")
            for f in (t.FieldList or []):
                try:
                    print("   ", s(f.row.Name) if hasattr(f, "row") else s(f.Name))
                except Exception as e:
                    print("   ?", e)
            print("  [methods]")
            for m in (t.MethodList or []):
                try:
                    print("   ", s(m.row.Name) if hasattr(m, "row") else s(m.Name))
                except Exception as e:
                    print("   ?", e)
    if not found:
        print(f"\n=== {short}: NOT FOUND ===")

for tname in ["Precept_Role", "Precept_RoleSingle", "Precept_RoleMulti",
              "PawnRoleTracker", "RoleEffect_PawnStatModifier"]:
    dump_type(tname)

# PreceptDef fields that matter for our features (expectationsOffset, convertPowerFactor, roleEffects)
print("\n=== PreceptDef: grep role/expect/convert fields ===")
for t in pe.net.mdtables.TypeDef:
    if s(t.TypeName) == "PreceptDef":
        for f in (t.FieldList or []):
            n = s(f.row.Name) if hasattr(f, "row") else s(f.Name)
            if any(k.lower() in n.lower() for k in ["role", "expect", "convert", "certainty"]):
                print("  ", n)
        break
