#!/usr/bin/env python3
"""Inventário DataTables: inits com/sem bloco language e carregamento de gdi-datatables-defaults."""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
VIEWS = list(ROOT.glob("Areas/**/Views/**/*.cshtml")) + list(ROOT.glob("Views/**/*.cshtml"))

DT_INIT = re.compile(r"\.dataTable\s*\(|\.DataTable\s*\(", re.I)
LANG = re.compile(r"\blanguage\s*:", re.I)
OLANG = re.compile(r"\boLanguage\s*:", re.I)

files_with_dt = []
inits_total = 0
inits_with_lang = 0
inits_without = []

for f in sorted(VIEWS):
    text = f.read_text(encoding="utf-8", errors="replace")
    if not DT_INIT.search(text):
        continue
    rel = f.relative_to(ROOT).as_posix()
    files_with_dt.append(rel)
    for m in DT_INIT.finditer(text):
        inits_total += 1
        chunk = text[m.start() : m.start() + 5000]
        if LANG.search(chunk) or OLANG.search(chunk):
            inits_with_lang += 1
        else:
            inits_without.append(rel)

print("=== DataTables language audit ===")
print(f"Views com DataTable: {len(files_with_dt)}")
print(f"Inits totais: {inits_total}")
print(f"Com language/oLanguage no bloco: {inits_with_lang}")
print(f"Sem language (herdam gdi-datatables-defaults): {len(inits_without)}")
if inits_without:
    uniq = sorted(set(inits_without))
    print(f"  ({len(uniq)} ficheiros únicos, primeiros 25)")
    for p in uniq[:25]:
        print(f"    - {p}")
    if len(uniq) > 25:
        print(f"    ... +{len(uniq) - 25}")

registry = (ROOT / "Views/Shared/_LayoutPageScriptRegistry.cshtml").read_text(encoding="utf-8")
blank = (ROOT / "Views/Shared/_Blank.cshtml").read_text(encoding="utf-8")
layout_dt = (ROOT / "Views/Shared/_LayoutScriptsDataTables.cshtml").read_text(encoding="utf-8")
ok_registry = "gdi-datatables-defaults" in registry
ok_blank = "gdi-datatables-defaults" in blank
ok_layout = "gdi-datatables-defaults" in layout_dt
print()
print("Carregamento gdi-datatables-defaults.js:")
print(f"  _LayoutScriptsDataTables: {'OK' if ok_layout else 'FALTA'}")
print(f"  _LayoutPageScriptRegistry: {'OK' if ok_registry else 'FALTA'}")
print(f"  _Blank.cshtml: {'OK' if ok_blank else 'FALTA'}")
