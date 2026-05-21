# -*- coding: utf-8 -*-
"""Inventário: DataTables com gdi-dt-scroll-host e contagem de colunas."""
import re
from pathlib import Path

BASE = Path(__file__).resolve().parent.parent
RX_BLOCK = re.compile(
    r"gdi-dt-scroll-host[\s\S]{0,3000}?<table[^>]*\bid=[\"']([^\"']+)[\"']"
    r"[\s\S]{0,2000}?<thead>([\s\S]{0,3000}?)</thead>",
    re.I,
)
RX_TH = re.compile(r"<th\b", re.I)

rows = []
for f in sorted(BASE.glob("Areas/**/*.cshtml")):
    text = f.read_text(encoding="utf-8", errors="replace")
    if "gdi-dt-scroll-host" not in text:
        continue
    rel = str(f.relative_to(BASE)).replace("\\", "/")
    for m in RX_BLOCK.finditer(text):
        tid, thead = m.group(1), m.group(2)
        cols = len(RX_TH.findall(thead))
        rows.append({"path": rel, "id": tid, "cols": cols})

print("=== DataTables com gdi-dt-scroll-host ===\n")
for r in sorted(rows, key=lambda x: (x["cols"], x["path"])):
    flag = "REMOVE_HOST?" if r["cols"] <= 10 else "CSS_ONLY"
    print(f"{r['cols']:2d} cols [{flag:11s}] {r['id']:32s} {r['path']}")
print(f"\nTotal: {len(rows)}")
