#!/usr/bin/env python3
"""Inventario: handlers Ajax hibridos (typeof result em error: ou success).

Exit 0 = nenhuma ocorrencia; exit 1 = pendentes.

Uso:
  python Scripts/2026_06_05_gdi_inventory_ajax_hybrid_handlers.py
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PATTERN = re.compile(r"typeof result !== 'undefined'")


def main() -> int:
    hits: list[tuple[str, int, str]] = []
    for path in sorted((ROOT / "Areas").rglob("*.cshtml")):
        for i, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
            if PATTERN.search(line):
                hits.append((str(path.relative_to(ROOT)), i, line.strip()[:120]))

    if not hits:
        print("OK - nenhum handler Ajax hibrido (typeof result) em Areas/**/Views.")
        return 0

    print(f"PENDENTE - {len(hits)} ocorrencia(s):")
    for rel, line_no, snippet in hits[:50]:
        print(f"  {rel}:{line_no}  {snippet}")
    if len(hits) > 50:
        print(f"  ... +{len(hits) - 50} mais")
    return 1


if __name__ == "__main__":
    sys.exit(main())
