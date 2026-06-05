#!/usr/bin/env python3
"""Atalho: inventario LibExceptions apenas em Areas/gc (delega ao script ERP)."""
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "Scripts"))

from importlib.util import module_from_spec, spec_from_file_location

_spec = spec_from_file_location(
    "erp_inv",
    ROOT / "Scripts" / "2026_06_05_gdi_inventory_erp_ajax_exceptions_residual.py",
)
_mod = module_from_spec(_spec)
assert _spec.loader is not None
_spec.loader.exec_module(_mod)

if __name__ == "__main__":
    sys.exit(_mod.main(["--areas", "gc"]))
