#!/usr/bin/env python3
"""Audita <select> estáticos (sem data-gdi-lookup-url) sem pesquisa local Select2."""
import json
import os
import re

ROOT = os.path.join(os.path.dirname(__file__), "..")
AREAS = os.path.join(ROOT, "Areas")
VIEWS = os.path.join(ROOT, "Views")


def scan_file(path, rel):
    t = open(path, encoding="utf-8", errors="ignore").read()
    if "DropDownList" not in t and "<select" not in t.lower():
        return []

    hits = []
    # DropDownList / DropDownListFor blocks (rough: line contains DropDownList)
    for i, line in enumerate(t.splitlines(), 1):
        if "DropDownList" not in line:
            continue
        if "data-gdi-lookup-url" in line or "data_gdi_lookup_url" in line:
            continue
        if "data-gdi-no-select2" in line or "data_gdi_no_select2" in line:
            continue
        if 'data-gdi-select2-search="true"' in line or "data_gdi_select2_search = \"true\"" in line:
            continue
        if "@Html.DropDownList" not in line and "@Html.DropDownListFor" not in line:
            continue
        hits.append({
            "file": rel.replace("\\", "/"),
            "line": i,
            "snippet": line.strip()[:200],
        })
    return hits


def check_init(path, rel):
    t = open(path, encoding="utf-8", errors="ignore").read()
    if "jsInitModal()" not in t or "jsInitForm()" in t:
        return None
    if "DropDownList" not in t:
        return None
    return rel.replace("\\", "/")


def main():
    all_hits = []
    init_modal_only = []
    for base in (AREAS, VIEWS):
        if not os.path.isdir(base):
            continue
        for dirpath, _, files in os.walk(base):
            for fn in files:
                if not fn.endswith(".cshtml"):
                    continue
                path = os.path.join(dirpath, fn)
                rel = os.path.relpath(path, ROOT)
                all_hits.extend(scan_file(path, rel))
                r = check_init(path, rel)
                if r:
                    init_modal_only.append(r)

    out = {
        "static_select_without_local_search": len(all_hits),
        "hits": all_hits,
        "jsInitModal_only_no_jsInitForm": sorted(set(init_modal_only)),
    }
    out_path = os.path.join(os.path.dirname(__file__), "2026_06_01_select2_local_search_audit.json")
    with open(out_path, "w", encoding="utf-8") as f:
        json.dump(out, f, indent=2, ensure_ascii=False)
    print(f"Static selects sem pesquisa local: {len(all_hits)}")
    print(f"Views jsInitModal sem jsInitForm: {len(out['jsInitModal_only_no_jsInitForm'])}")
    print(f"JSON: {out_path}")


if __name__ == "__main__":
    main()
