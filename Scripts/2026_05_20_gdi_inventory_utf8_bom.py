# -*- coding: utf-8 -*-
"""
Inventário read-only: ficheiros de texto com UTF-8 BOM ou UTF-16.
Uso:
  python Scripts/2026_05_20_gdi_inventory_utf8_bom.py
  python Scripts/2026_05_20_gdi_inventory_utf8_bom.py Areas/g Lib
  python Scripts/2026_05_20_gdi_inventory_utf8_bom.py --fail
"""
from __future__ import print_function
import codecs
import sys
from collections import defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
AREAS_ROOT = ROOT / "Areas"

SKIP_DIRS = {
    ".git", "node_modules", "bin", "obj", "packages", "PackageTmp",
    ".vs", "agent-transcripts",
}

SKIP_SUFFIXES = {
    ".png", ".jpg", ".jpeg", ".gif", ".webp", ".ico", ".bmp",
    ".dll", ".exe", ".pdf", ".zip", ".7z", ".rar", ".pfx", ".p12",
    ".woff", ".woff2", ".ttf", ".eot", ".otf",
    ".xls", ".xlsx", ".doc", ".docx",
}

TEXT_SUFFIXES = {
    ".cs", ".cshtml", ".js", ".css", ".less", ".json", ".xml", ".config",
    ".txt", ".md", ".mdc", ".py", ".ps1", ".sql", ".asax", ".tt", ".edmx",
    ".resx", ".svg", ".html", ".htm", ".browser", ".csproj", ".sln",
}


def should_skip_path(path):
    try:
        rel = path.relative_to(ROOT)
    except ValueError:
        return True
    if any(part in SKIP_DIRS for part in rel.parts):
        return True
    if path.suffix.lower() in SKIP_SUFFIXES:
        return True
    return False


def is_text_candidate(path):
    if path.suffix.lower() in TEXT_SUFFIXES:
        return True
    return path.name in ("Web.config", "Global.asax")


def group_key(path):
    rel = path.relative_to(ROOT)
    if len(rel.parts) >= 2 and rel.parts[0] == "Areas":
        return "Areas/" + rel.parts[1]
    return rel.parts[0] if rel.parts else "?"


def resolve_roots(argv):
    fail = False
    names = []
    for arg in argv:
        if arg == "--fail":
            fail = True
        else:
            names.append(arg)
    if not names:
        return [ROOT], fail
    roots = []
    for name in names:
        p = Path(name)
        if not p.is_absolute():
            p = ROOT / p
        roots.append(p.resolve())
    return roots, fail


def scan_root(base):
    bom = []
    utf16 = []
    scanned = 0
    if base.resolve() == ROOT.resolve():
        paths = sorted(base.rglob("*"))
    else:
        paths = sorted(base.rglob("*")) if base.is_dir() else []
    for path in paths:
        if not path.is_file() or should_skip_path(path):
            continue
        if not is_text_candidate(path):
            continue
        scanned += 1
        raw = path.read_bytes()
        if not raw:
            continue
        if raw.startswith(codecs.BOM_UTF8):
            bom.append(path)
        elif raw.startswith(b"\xff\xfe") or raw.startswith(b"\xfe\xff"):
            utf16.append(path)
    return scanned, bom, utf16


def main():
    roots, fail_on_hit = resolve_roots(sys.argv[1:])
    total_scanned = 0
    all_bom = []
    all_utf16 = []
    for base in roots:
        if not base.exists():
            print("Nao encontrado:", base)
            return 1
        scanned, bom, utf16 = scan_root(base)
        total_scanned += scanned
        all_bom.extend(bom)
        all_utf16.extend(utf16)

    by_group = defaultdict(int)
    for p in all_bom:
        by_group[group_key(p)] += 1

    print("text_files_scanned:", total_scanned)
    print("utf8_bom:", len(all_bom))
    print("utf16:", len(all_utf16))
    for grp in sorted(by_group):
        print("  %s: %d" % (grp, by_group[grp]))
    for p in sorted(all_bom):
        print("  BOM", p.relative_to(ROOT))
    for p in sorted(all_utf16):
        print("  UTF16", p.relative_to(ROOT))

    if fail_on_hit and (all_bom or all_utf16):
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
