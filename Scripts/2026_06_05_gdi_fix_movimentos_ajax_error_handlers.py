# -*- coding: utf-8 -*-
"""Alias — delega para script gc/Views completo (Movimentos = subpasta)."""
import subprocess
import sys
import os

_SCRIPT = os.path.join(os.path.dirname(__file__), "2026_06_05_gdi_fix_gc_views_ajax_error_handlers.py")

if __name__ == "__main__":
    args = [sys.executable, _SCRIPT, "Movimentos"] if len(sys.argv) <= 1 else [sys.executable, _SCRIPT] + sys.argv[1:]
    raise SystemExit(subprocess.call(args))