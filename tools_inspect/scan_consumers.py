# -*- coding: utf-8 -*-
"""Scan all method bodies for references to a named field; dump members of health types."""
import sys
import dnfile
from dncil.cil.body import CilMethodBody
from dncil.clr.token import Token
from dncil.cil.body.reader import CilMethodBodyReaderBase

DLL = r"D:\Steam\steamapps\common\RimWorld\Assembly-CSharp.dll"
pe = dnfile.dnPE(DLL)

def s(x):
    return x.value if hasattr(x, "value") else x

# map: typedef -> methods
type_methods = {}
for t in pe.net.mdtables.TypeDef:
    ms = []
    for m in (t.MethodList or []):
        row = m.row if hasattr(m, "row") else m
        ms.append(row)
    type_methods[(s(t.TypeNamespace), s(t.TypeName))] = ms

# field name -> rid
field_rid = {}
for i, f in enumerate(pe.net.mdtables.Field.rows):
    field_rid.setdefault(s(f.Name), []).append(i + 1)

class R(CilMethodBodyReaderBase):
    def __init__(self, pe, row):
        self.pe = pe; self.offset = pe.get_offset_from_rva(row.Rva)
    def read(self, n):
        d = self.pe.get_data(self.pe.get_rva_from_offset(self.offset), n); self.offset += n; return d
    def tell(self): return self.offset
    def seek(self, o): self.offset = o; return o

def scan_field_consumers(fname):
    rids = set(field_rid.get(fname, []))
    if not rids:
        print(f"field {fname}: NOT FOUND"); return
    print(f"### consumers of field '{fname}' (rids {sorted(rids)}) ###")
    for (ns, tn), ms in type_methods.items():
        for row in ms:
            if not row.Rva: continue
            try:
                body = CilMethodBody(R(pe, row))
            except Exception:
                continue
            for insn in body.instructions:
                op = insn.operand
                if isinstance(op, Token) and op.table == 0x04 and op.rid in rids:
                    print(f"  {ns}.{tn}::{s(row.Name)}")
                    break

def list_members(tname, ns_hint=""):
    for (ns, tn), ms in type_methods.items():
        if tn == tname and (not ns_hint or ns == ns_hint):
            print(f"\n=== {ns}.{tname} ===")
            for f in (pe.net.mdtables.TypeDef.rows[i].FieldList for i in range(len(pe.net.mdtables.TypeDef.rows)) if False):
                pass
    # fields via typedef scan
    for t in pe.net.mdtables.TypeDef:
        if s(t.TypeName) == tname and (not ns_hint or s(t.TypeNamespace) == ns_hint):
            print(f"\n=== {s(t.TypeNamespace)}.{tname} methods ===")
            for m in (t.MethodList or []):
                row = m.row if hasattr(m, "row") else m
                print("  ", s(row.Name))
            print(f"=== {s(t.TypeNamespace)}.{tname} fields ===")
            for f in (t.FieldList or []):
                row = f.row if hasattr(f, "row") else f
                print("  ", s(row.Name))

if __name__ == "__main__":
    for arg in sys.argv[1:]:
        if arg.startswith("field:"):
            scan_field_consumers(arg[6:])
        elif arg.startswith("type:"):
            ns, _, tn = arg[5:].rpartition(".")
            list_members(tn, ns)
