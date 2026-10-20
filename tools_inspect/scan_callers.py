# -*- coding: utf-8 -*-
"""Scan method bodies for calls to a named method (any type)."""
import sys
import dnfile
from dncil.cil.body import CilMethodBody
from dncil.clr.token import Token
from dncil.cil.body.reader import CilMethodBodyReaderBase

DLL = r"D:\Steam\steamapps\common\RimWorld\Assembly-CSharp.dll"
pe = dnfile.dnPE(DLL)

def s(x):
    return x.value if hasattr(x, "value") else x

_method_names = {}
for i, m in enumerate(pe.net.mdtables.MethodDef.rows):
    _method_names[i + 1] = s(m.Name)
_memberref_names = {}
for i, m in enumerate(pe.net.mdtables.MemberRef.rows):
    _memberref_names[i + 1] = s(m.Name)

class R(CilMethodBodyReaderBase):
    def __init__(self, pe, row):
        self.pe = pe; self.offset = pe.get_offset_from_rva(row.Rva)
    def read(self, n):
        d = self.pe.get_data(self.pe.get_rva_from_offset(self.offset), n); self.offset += n; return d
    def tell(self): return self.offset
    def seek(self, o): self.offset = o; return o

def callers_of(target, limit=60):
    print(f"### callers of method '{target}' ###")
    n = 0
    for t in pe.net.mdtables.TypeDef:
        tn = s(t.TypeName); ns = s(t.TypeNamespace)
        for m in (t.MethodList or []):
            row = m.row if hasattr(m, "row") else m
            if not row.Rva: continue
            try:
                body = CilMethodBody(R(pe, row))
            except Exception:
                continue
            hit = False
            for insn in body.instructions:
                op = insn.operand
                if isinstance(op, Token) and op.table in (0x06, 0x0A):
                    nm = _method_names.get(op.rid) if op.table == 0x06 else _memberref_names.get(op.rid)
                    if nm == target:
                        hit = True; break
            if hit:
                print(f"  {ns}.{tn}::{s(row.Name)}")
                n += 1
                if n >= limit:
                    print("  ...(limit)")
                    return

if __name__ == "__main__":
    for arg in sys.argv[1:]:
        callers_of(arg)
