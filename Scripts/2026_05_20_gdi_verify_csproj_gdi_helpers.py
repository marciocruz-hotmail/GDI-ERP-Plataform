# -*- coding: utf-8 -*-
"""
Lista .cshtml em Areas/ e Views/ que referenciam GdiAjax* ou GdiDt* e não estão em GDI-ERP-Plataform.csproj.
Uso: python Scripts/2026_05_20_gdi_verify_csproj_gdi_helpers.py
"""
import os
import re
import sys

BASE = os.path.normpath(os.path.join(os.path.dirname(__file__), ".."))
CSPROJ = os.path.join(BASE, "GDI-ERP-Plataform.csproj")

RX_GDI = re.compile(r"Gdi(Ajax|Dt)\w*", re.IGNORECASE)
RX_CONTENT = re.compile(
    r'<Content\s+Include="([^"]+\.cshtml)"\s*/>', re.IGNORECASE
)


def main():
    with open(CSPROJ, "r", encoding="utf-8") as f:
        txt = f.read()
    in_csproj = set()
    for m in RX_CONTENT.finditer(txt):
        rel = m.group(1).replace("/", os.sep)
        in_csproj.add(rel.lower())

    roots = [os.path.join(BASE, "Areas"), os.path.join(BASE, "Views")]
    missing = []
    used = 0
    for root in roots:
        if not os.path.isdir(root):
            continue
        for dirpath, _, files in os.walk(root):
            for fn in files:
                if not fn.endswith(".cshtml"):
                    continue
                path = os.path.join(dirpath, fn)
                rel = os.path.relpath(path, BASE)
                try:
                    with open(path, "r", encoding="utf-8", errors="ignore") as f:
                        body = f.read()
                except OSError:
                    continue
                if not RX_GDI.search(body):
                    continue
                used += 1
                key = rel.replace("/", os.sep).lower()
                if key not in in_csproj:
                    missing.append(rel.replace("/", os.sep))

    print("cshtml_with_GdiAjax_or_GdiDt:", used)
    print("missing_in_csproj:", len(missing))
    for m in sorted(missing):
        print(m)
    sys.exit(1 if missing else 0)


if __name__ == "__main__":
    main()
