# -*- coding: utf-8 -*-
"""Lista views com scroll-body-horizontal e tabela MVC (sem class display/dataTable)."""
import re
import os

ROOT = os.path.join(os.path.dirname(__file__), "..", "Areas")
form_views = []
mixed_views = []

for dirpath, _, files in os.walk(ROOT):
    for fname in files:
        if not fname.endswith(".cshtml"):
            continue
        path = os.path.join(dirpath, fname)
        rel = os.path.relpath(path, os.path.join(ROOT, "..")).replace("\\", "/")
        text = open(path, encoding="utf-8", errors="replace").read()
        if "scroll-body-horizontal" not in text:
            continue
        has_form_table = False
        has_dt_table = False
        for m in re.finditer(
            r"scroll-body-horizontal[^>]*>[\s\S]{0,1200}?<table([^>]*)>",
            text,
            re.IGNORECASE,
        ):
            attrs = m.group(1)
            is_dt = bool(
                re.search(r"\bdisplay\b", attrs, re.I)
                or re.search(r"\bdataTable\b", attrs, re.I)
                or re.search(r'\bid="dt', attrs, re.I)
                or re.search(r'\bid="dataTable', attrs, re.I)
            )
            if is_dt:
                has_dt_table = True
            else:
                has_form_table = True
        if has_form_table:
            form_views.append(rel)
        if has_form_table and has_dt_table:
            mixed_views.append(rel)

print("=== Tabelas MVC (sem display) em scroll-body-horizontal ===")
for v in sorted(set(form_views)):
    print(v)
print("total:", len(set(form_views)))
if mixed_views:
    print("\n=== Views mistas (MVC + DataTables) ===")
    for v in sorted(set(mixed_views)):
        print(v)
