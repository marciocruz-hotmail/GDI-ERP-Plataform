# -*- coding: utf-8 -*-
"""Inventário Index cadastros g: deferLoading na view e gate vazio no GetDados."""
import os
import re

ROOT = os.path.join(os.path.dirname(__file__), "..", "Areas", "g")
INDEX_DT = []
INDEX_OK = []
INDEX_NO_DT = []

for dirpath, _, files in os.walk(os.path.join(ROOT, "Views")):
    for fname in files:
        if fname != "Index.cshtml":
            continue
        path = os.path.join(dirpath, fname)
        rel = os.path.relpath(path, os.path.join(ROOT, "..")).replace("\\", "/")
        text = open(path, encoding="utf-8", errors="replace").read()
        ctrl = rel.split("/")[2] if "/Views/" in rel else "?"
        if 'class="display' not in text and "table.display" not in text and 'id="dt' not in text:
            INDEX_NO_DT.append(rel)
            continue
        has_defer = "deferLoading: true" in text or "deferLoading:true" in text
        if has_defer:
            INDEX_OK.append(rel)
        else:
            INDEX_DT.append(rel)

print("=== Index DataTables SEM deferLoading (1º load dispara GetDados) ===")
for v in sorted(INDEX_DT):
    print(v)
print("total:", len(INDEX_DT))

print("\n=== Index DataTables COM deferLoading (padrão 2.8) ===")
for v in sorted(INDEX_OK):
    print(v)
print("total:", len(INDEX_OK))

print("\n=== Index sem grelha DataTables (jstree/outro) ===")
for v in sorted(INDEX_NO_DT):
    print(v)
print("total:", len(INDEX_NO_DT))
