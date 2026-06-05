# -*- coding: utf-8 -*-
"""Index.cshtml com _Layout e sem DataTables — candidatos opt-out Fase 4."""
from __future__ import print_function
import os, re

BASE = os.path.normpath(os.path.join(os.path.dirname(__file__), ".."))
DT = re.compile(r"\.DataTable\s*\(|\.dataTable\s*\(|bServerSide\s*:\s*true", re.I)
MODAL = re.compile(r"\(\s*['\"]#mainModal['\"]\s*\)\s*\.\s*load", re.I)
LAYOUT = re.compile(r"_Layout\.cshtml", re.I)

for area in ("g", "gc", "qa", "crm"):
    root = os.path.join(BASE, "Areas", area, "Views")
    if not os.path.isdir(root):
        continue
    for dp, _, fns in os.walk(root):
        for fn in fns:
            if fn != "Index.cshtml":
                continue
            path = os.path.join(dp, fn)
            rel = os.path.relpath(path, BASE).replace("\\", "/")
            text = open(path, encoding="utf-8", errors="ignore").read()
            if not LAYOUT.search(text) or DT.search(text):
                continue
            parts = rel.split("/")
            ctrl = parts[3] if len(parts) >= 5 and parts[2] == "Views" else "?"
            print("%s/%s/Index  mainModal=%s" % (area, ctrl, bool(MODAL.search(text))))
