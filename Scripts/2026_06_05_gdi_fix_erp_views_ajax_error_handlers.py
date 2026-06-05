# -*- coding: utf-8 -*-
"""Lote L — handlers Ajax em Areas/g, qa, a (e opcional gc). Delega patch do script gc."""
import importlib.util
import os
import sys

_REPO = os.path.dirname(os.path.dirname(__file__))
_PATCH_MOD = os.path.join(_REPO, "Scripts", "2026_06_05_gdi_fix_gc_views_ajax_error_handlers.py")

DEFAULT_AREAS = ("g", "qa", "a")


def load_patch():
    spec = importlib.util.spec_from_file_location("gdi_ajax_patch", _PATCH_MOD)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod.patch


def views_root(area):
    return os.path.join(_REPO, "Areas", area, "Views")


def run_area(patch_fn, area):
    import glob

    base = views_root(area)
    if not os.path.isdir(base):
        print("Skip (missing):", base)
        return []
    changed = []
    for path in sorted(glob.glob(os.path.join(base, "**", "*.cshtml"), recursive=True)):
        with open(path, "r", encoding="utf-8") as f:
            original = f.read()
        updated = patch_fn(original)
        if updated != original:
            with open(path, "w", encoding="utf-8", newline="\r\n") as f:
                f.write(updated)
            changed.append(os.path.relpath(path, base).replace("\\", "/"))
    print("Area %s: changed %d file(s)" % (area, len(changed)))
    for name in changed:
        print(" -", name)
    return changed


def main():
    areas = sys.argv[1:] if len(sys.argv) > 1 else list(DEFAULT_AREAS)
    patch_fn = load_patch()
    total = 0
    for area in areas:
        total += len(run_area(patch_fn, area))
    print("Total changed:", total)


if __name__ == "__main__":
    main()
