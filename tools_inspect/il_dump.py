# -*- coding: utf-8 -*-
"""IL dump of selected methods from local RimWorld Assembly-CSharp.dll (read-only)."""
import sys
import dnfile
from dncil.cil.body import CilMethodBody
from dncil.cil.error import MethodBodyFormatError
from dncil.clr.token import Token, StringToken, InvalidToken
from dncil.cil.body.reader import CilMethodBodyReaderBase

DLL = r"D:\Steam\steamapps\common\RimWorld\Assembly-CSharp.dll"
pe = dnfile.dnPE(DLL)

def s(x):
    return x.value if hasattr(x, "value") else x

class DnfileMethodBodyReader(CilMethodBodyReaderBase):
    def __init__(self, pe, row):
        self.pe = pe
        self.row = row
        self.offset = self.pe.get_offset_from_rva(row.Rva)

    def read(self, n):
        data = self.pe.get_data(self.pe.get_rva_from_offset(self.offset), n)
        self.offset += n
        return data

    def tell(self):
        return self.offset

    def seek(self, off):
        self.offset = off
        return self.offset

TABLES = {
    0x01: "TypeRef", 0x02: "TypeDef", 0x04: "Field", 0x06: "MethodDef",
    0x0A: "MemberRef", 0x1B: "TypeSpec", 0x2B: "MethodSpec", 0x11: "StandAloneSig",
}

# cache method/field names by rid
_method_names = {}
for i, m in enumerate(pe.net.mdtables.MethodDef.rows):
    _method_names[i + 1] = s(m.Name)
_field_names = {}
for i, f in enumerate(pe.net.mdtables.Field.rows):
    _field_names[i + 1] = s(f.Name)
_typedef_names = {}
for i, t in enumerate(pe.net.mdtables.TypeDef.rows):
    _typedef_names[i + 1] = s(t.TypeName)
_typeref_names = {}
for i, t in enumerate(pe.net.mdtables.TypeRef.rows):
    _typeref_names[i + 1] = (s(t.TypeNamespace) + "." if t.TypeNamespace else "") + s(t.TypeName)
_memberref_names = {}
for i, m in enumerate(pe.net.mdtables.MemberRef.rows):
    cls = ""
    try:
        cr = m.Class
        if cr is not None and cr.row is not None:
            cls = (s(getattr(cr.row, "TypeName", "")) or "") + "::"
    except Exception:
        pass
    _memberref_names[i + 1] = cls + s(m.Name)

def resolve_token(tok):
    table = TABLES.get(tok.table)
    if table is None:
        if tok.table == 0x70:
            try:
                return '"' + pe.net.user_strings.get(tok.rid).value + '"'
            except Exception:
                return f'us(0x{tok.value:08x})'
        return f'tok(0x{tok.value:08x})'
    try:
        if table == "MethodDef":
            return _method_names.get(tok.rid, f"method#{tok.rid}")
        if table == "Field":
            return _field_names.get(tok.rid, f"field#{tok.rid}")
        if table == "TypeDef":
            return _typedef_names.get(tok.rid, f"typedef#{tok.rid}")
        if table == "TypeRef":
            return _typeref_names.get(tok.rid, f"typeref#{tok.rid}")
        if table == "MemberRef":
            return _memberref_names.get(tok.rid, f"memberref#{tok.rid}")
        return f"{table}#{tok.rid}"
    except Exception:
        return f'tok(0x{tok.value:08x})'

def dump_method(row, max_len=200000):
    if not row.Rva:
        print("    <no body>")
        return
    try:
        body = CilMethodBody(DnfileMethodBodyReader(pe, row))
    except Exception as e:
        print("    <error>", e)
        return
    out = []
    for insn in body.instructions:
        op = insn.opcode.name
        operand = ""
        if insn.operand is not None:
            if isinstance(insn.operand, Token):
                operand = resolve_token(insn.operand)
            else:
                operand = str(insn.operand)
        out.append(f"      {op:12s} {operand}")
    text = "\n".join(out)
    print(text[:max_len])
    if len(text) > max_len:
        print("      ...<truncated>")

def find_methods(type_name, method_names):
    for t in pe.net.mdtables.TypeDef:
        if s(t.TypeName) == type_name:
            for m in (t.MethodList or []):
                row = m.row if hasattr(m, "row") else m
                if s(row.Name) in method_names:
                    print(f"\n======== {type_name}.{s(row.Name)} ========")
                    dump_method(row)

# find consumers of expectationsOffset: scan all methods for field token "expectationsOffset"
print("### Methods referencing PreceptDef::expectationsOffset ###")
field_rows = []
for i, f in enumerate(pe.net.mdtables.Field.rows):
    if s(f.Name) == "expectationsOffset":
        field_rows.append(i + 1)
print("field rid:", field_rows)

targets = sys.argv[1:] if len(sys.argv) > 1 else []
if targets:
    for spec in targets:
        tname, _, mname = spec.partition(".")
        find_methods(tname, {mname} if mname else None)
