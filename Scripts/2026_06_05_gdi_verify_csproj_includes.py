#!/usr/bin/env python3
"""Verifica Include paths no .csproj vs disco."""
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CSPROJ = ROOT / "GDI-ERP-Plataform.csproj"
INCLUDE_RE = re.compile(
    r'<(Compile|Content|None|EmbeddedResource|Folder)\s+Include="([^"]+)"'
)


def main() -> int:
    text = CSPROJ.read_text(encoding="utf-8")
    missing: list[tuple[str, str]] = []
    seen: set[tuple[str, str]] = set()
    dupes: list[tuple[str, str]] = []

    for kind, rel in INCLUDE_RE.findall(text):
        key = (kind, rel)
        if key in seen:
            dupes.append(key)
        seen.add(key)
        if "*" in rel or "?" in rel:
            continue
        path = ROOT / rel.replace("\\", "/")
        if not path.exists():
            missing.append((kind, rel))

    print(f"Includes: {len(seen)} | missing: {len(missing)} | duplicates: {len(dupes)}")
    if dupes:
        print("\nDUPLICATES:")
        for k, r in sorted(set(dupes))[:30]:
            print(f"  [{k}] {r}")
    if missing:
        print("\nMISSING ON DISK:")
        for k, r in sorted(missing):
            print(f"  [{k}] {r}")
        return 1
    print("OK - all explicit Include paths exist.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
