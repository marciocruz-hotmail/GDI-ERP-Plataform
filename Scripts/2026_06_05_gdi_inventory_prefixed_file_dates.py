#!/usr/bin/env python3
"""Inventario: prefixo AAAA_MM_DD_ vs CreationTime do ficheiro.

Pastas: .cursor/context, .cursor/rules, Scripts (ferramentas gdi_/fix_/...).

Exit 0 = conforme; exit 1 = prefixo != data de criacao (YYYY-MM-DD).

Uso:
  python Scripts/2026_06_05_gdi_inventory_prefixed_file_dates.py
  python Scripts/2026_06_05_gdi_inventory_prefixed_file_dates.py --since 2026-06-01
"""
from __future__ import annotations

import argparse
import re
import sys
from datetime import datetime
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PREFIX_RE = re.compile(r"^(\d{4})_(\d{2})_(\d{2})_")
SCAN_DIRS = (
    ROOT / ".cursor" / "context",
    ROOT / ".cursor" / "rules",
    ROOT / "Scripts",
)


def prefix_date(name: str) -> str | None:
    m = PREFIX_RE.match(name)
    if not m:
        return None
    return f"{m.group(1)}-{m.group(2)}-{m.group(3)}"


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--since",
        help="So reportar ficheiros criados apos AAAA-MM-DD (ex.: 2026-06-01)",
    )
    args = parser.parse_args()
    since: datetime | None = None
    if args.since:
        since = datetime.strptime(args.since, "%Y-%m-%d")

    mismatches: list[tuple[str, str, str]] = []
    for base in SCAN_DIRS:
        if not base.is_dir():
            continue
        for path in sorted(base.rglob("*")):
            if not path.is_file() or "__pycache__" in path.parts:
                continue
            pd = prefix_date(path.name)
            if pd is None:
                continue
            created = datetime.fromtimestamp(path.stat().st_ctime).strftime("%Y-%m-%d")
            if since and datetime.strptime(created, "%Y-%m-%d") < since:
                continue
            if pd != created:
                mismatches.append((str(path.relative_to(ROOT)), pd, created))

    if not mismatches:
        print("OK - todos os ficheiros com prefixo AAAA_MM_DD_ batem com CreationTime.")
        return 0

    print(f"PENDENTE - {len(mismatches)} ficheiro(s) com prefixo != CreationTime:")
    for rel, pd, created in mismatches[:80]:
        print(f"  {rel}  prefixo={pd}  criado={created}")
    if len(mismatches) > 80:
        print(f"  ... +{len(mismatches) - 80} mais")
    return 1


if __name__ == "__main__":
    sys.exit(main())
