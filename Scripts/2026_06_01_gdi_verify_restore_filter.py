#!/usr/bin/env python3
"""Verify RestoreFilterAutoSearch parity: controller + view."""
import os
import re

ROOT = os.path.join(os.path.dirname(__file__), "..")

controllers = []
for dirpath, _, files in os.walk(os.path.join(ROOT, "Areas")):
    for f in files:
        if not f.endswith("Controller.cs"):
            continue
        p = os.path.join(dirpath, f)
        t = open(p, encoding="utf-8", errors="ignore").read()
        if "RestoreFilterAutoSearch" in t and "MontarFiltro" in t or (
            "RestoreFilterAutoSearch" in t and "TryParseFiltro" in t
        ):
            if "RestoreFilterAutoSearch" in t:
                controllers.append(os.path.relpath(p, ROOT).replace("\\", "/"))

issues = []
for c in sorted(set(controllers)):
    # guess view path
    m = re.search(r"Areas/(\w+)/Controllers/(\w+)Controller", c.replace("\\", "/"))
    if not m:
        continue
    area, ctrl = m.group(1), m.group(2)
    view = os.path.join(ROOT, "Areas", area, "Views", ctrl, "Index.cshtml")
    if not os.path.isfile(view):
        issues.append(f"{c}: sem Index.cshtml")
        continue
    vt = open(view, encoding="utf-8", errors="ignore").read()
    if "RestoreFilterAutoSearch == true" not in vt and "RestoreFilterAutoSearch != true" not in vt:
        issues.append(f"{view}: sem bloco RestoreFilterAutoSearch na view")

print(f"Controllers com RestoreFilterAutoSearch: {len(set(controllers))}")
if issues:
    print("LACUNAS:")
    for i in issues:
        print(" ", i)
else:
    print("OK: par controller/view completo")
