#!/usr/bin/env python3
"""G-PUB/SMK: smoke dos inventarios de arquitetura (exit 0 = todos OK).

Uso:
  python Scripts/2026_06_05_gdi_smoke_architecture_inventories.py
"""
from __future__ import annotations

import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

CHECKS: list[tuple[str, list[str]]] = [
    ("verify_csproj_gdi_helpers", ["python", str(ROOT / "Scripts" / "2026_05_20_gdi_verify_csproj_gdi_helpers.py")]),
    ("inventory_gc_modal_get_guards", ["python", str(ROOT / "Scripts" / "2026_06_05_gdi_inventory_gc_modal_get_guards.py")]),
    ("inventory_erp_ajax_exceptions_residual", ["python", str(ROOT / "Scripts" / "2026_06_05_gdi_inventory_erp_ajax_exceptions_residual.py"), "--areas", "all"]),
    ("inventory_ajax_hybrid_handlers", ["python", str(ROOT / "Scripts" / "2026_06_05_gdi_inventory_ajax_hybrid_handlers.py")]),
    ("inventory_prefixed_file_dates_since_jun", ["python", str(ROOT / "Scripts" / "2026_06_05_gdi_inventory_prefixed_file_dates.py"), "--since", "2026-06-01"]),
]


def main() -> int:
    failed: list[str] = []
    for name, cmd in CHECKS:
        print(f"--- {name} ---")
        r = subprocess.run(cmd, cwd=str(ROOT), capture_output=True, text=True, encoding="utf-8", errors="replace")
        out = ((r.stdout or "") + (r.stderr or "")).strip()
        safe = out.encode("ascii", errors="replace").decode("ascii") if out else "(sem output)"
        print(safe)
        if r.returncode != 0:
            failed.append(name)
    if failed:
        print(f"\nFALHA: {len(failed)} inventario(s): {', '.join(failed)}")
        return 1
    print(f"\nOK - {len(CHECKS)} inventarios de arquitetura.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
