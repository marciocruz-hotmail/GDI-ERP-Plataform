# -*- coding: utf-8 -*-
"""
G-PERF-20 Fase 0 — Inventário de dependências JS/CSS por view (.cshtml).

Detecta uso de: DataTables, Select2, jstree, Tempus Dominus, mainModal.load (Ajax),
bootstrap4-toggle. Sugere flags GdiScripts para carregamento condicional no _Layout.

Uso (raiz do repo):
  python Scripts/2026_05_22_gdi_inventory_page_scripts.py
  python Scripts/2026_05_22_gdi_inventory_page_scripts.py --json
  python Scripts/2026_05_22_gdi_inventory_page_scripts.py --markdown
  python Scripts/2026_05_22_gdi_inventory_page_scripts.py --controllers

Exit 0 sempre (inventário informativo).
"""
from __future__ import print_function

import argparse
import json
import os
import re
from collections import defaultdict
from datetime import datetime

BASE = os.path.normpath(os.path.join(os.path.dirname(__file__), ".."))
VIEW_ROOTS = [
    os.path.join(BASE, "Areas"),
    os.path.join(BASE, "Views"),
]
SKIP_PARTS = {".vs", "bin", "obj", "packages"}

PATTERNS = {
    "datatables": re.compile(
        r"\.DataTable\s*\(|\.dataTable\s*\(|bServerSide\s*:\s*true|datatables\.net",
        re.I,
    ),
    "select2": re.compile(
        r"\.select2\s*\(|select2\s*\(|data[-_]gdi[-_]lookup[-_]url|data[-_]gdi[-_]select2|"
        r"gdi-select2|GdiSelect2|GetClientesLookup|GetProdutosLookup",
        re.I,
    ),
    "jstree": re.compile(r"\.jstree\s*\(|jstree\s*\(", re.I),
    "tempus": re.compile(
        r"TempusDominus|tempusDominus|tempus-dominus|new\s+tempusDominus|jsDatepicker\s*\(",
        re.I,
    ),
    "main_modal_load": re.compile(
        r'[#$]?\s*\(\s*["\']#mainModal["\']\s*\)\s*\.\s*load\s*\(|getElementById\s*\(\s*["\']mainModal["\']\s*\)',
        re.I,
    ),
    "toggle": re.compile(r"bootstrapToggle|bootstrap4-toggle|data-toggle=[\"']toggle[\"']", re.I),
}

# Layouts que não usam _Layout autenticado completo
LAYOUT_LITE = re.compile(
    r'Layout\s*=\s*null|Layout\s*=\s*["\']~/Views/Shared/_Modal\.cshtml["\']|_Blank\.cshtml',
    re.I,
)

FLAG_ORDER = ("Core", "DataTables", "Select2", "Tempus", "Jstree", "Toggle")


def iter_cshtml():
    for root in VIEW_ROOTS:
        if not os.path.isdir(root):
            continue
        for dirpath, dirnames, filenames in os.walk(root):
            dirnames[:] = [d for d in dirnames if d not in SKIP_PARTS]
            for name in filenames:
                if name.endswith(".cshtml"):
                    yield os.path.join(dirpath, name)


def parse_area_controller(rel_path):
    """Areas/g/Views/Clientes/Index.cshtml -> area=g, controller=Clientes, view=Index"""
    parts = rel_path.replace("\\", "/").split("/")
    view_file = parts[-1].replace(".cshtml", "") if parts else ""
    if parts[0] == "Areas" and len(parts) >= 5 and parts[2] == "Views":
        return parts[1], parts[3], view_file
    if parts[0] == "Areas" and len(parts) == 4 and parts[2] == "Views":
        return parts[1], view_file, view_file
    if parts[0] == "Views":
        ctrl = parts[1] if len(parts) > 2 else "Shared"
        return "", ctrl, view_file
    return "", "", view_file


def suggest_flags(hits, is_modal_layout, is_index):
    flags = set()
    flags.add("Core")
    if hits.get("datatables"):
        flags.add("DataTables")
    if hits.get("select2"):
        flags.add("Select2")
    if hits.get("tempus"):
        flags.add("Tempus")
    if hits.get("jstree"):
        flags.add("Jstree")
    if hits.get("toggle"):
        flags.add("Toggle")
    # Modal Ajax na mesma página: herda scripts do layout pai — marcar NeedsModalScripts
    needs_modal = bool(hits.get("main_modal_load"))
    # Heurística Index g/gc: quase sempre DT ou abrirá modal com DT
    if is_index and not is_modal_layout:
        if not flags.intersection({"DataTables", "Select2"}):
            if needs_modal:
                flags.add("DataTables")
                flags.add("Select2")
    return sorted(flags, key=lambda f: FLAG_ORDER.index(f) if f in FLAG_ORDER else 99), needs_modal


def scan_view(path):
    rel = os.path.relpath(path, BASE).replace("\\", "/")
    try:
        with open(path, "r", encoding="utf-8", errors="ignore") as f:
            text = f.read()
    except OSError as e:
        return None, str(e)

    hits = {k: bool(rx.search(text)) for k, rx in PATTERNS.items()}
    is_modal = bool(LAYOUT_LITE.search(text))
    area, controller, view_name = parse_area_controller(rel)
    is_index = view_name.lower() == "index" or view_name.lower().startswith("index")

    flags, needs_modal = suggest_flags(hits, is_modal, is_index)
    return {
        "path": rel,
        "area": area,
        "controller": controller,
        "view": view_name,
        "layout_lite": is_modal,
        "hits": hits,
        "flags": flags,
        "needs_modal_scripts": needs_modal,
    }, None


def aggregate_controllers(rows):
    by_ctrl = defaultdict(lambda: {
        "views": 0,
        "flags_union": set(),
        "modal_hosts": 0,
        "datatables_views": 0,
        "select2_views": 0,
        "jstree_views": 0,
        "tempus_views": 0,
        "paths": [],
    })
    for r in rows:
        key = (r["area"], r["controller"])
        agg = by_ctrl[key]
        agg["views"] += 1
        agg["flags_union"].update(r["flags"])
        agg["paths"].append(r["path"])
        if r["needs_modal_scripts"]:
            agg["modal_hosts"] += 1
        if r["hits"]["datatables"]:
            agg["datatables_views"] += 1
        if r["hits"]["select2"]:
            agg["select2_views"] += 1
        if r["hits"]["jstree"]:
            agg["jstree_views"] += 1
        if r["hits"]["tempus"]:
            agg["tempus_views"] += 1

    result = []
    for (area, controller), agg in sorted(by_ctrl.items()):
        flags = sorted(agg["flags_union"], key=lambda f: FLAG_ORDER.index(f) if f in FLAG_ORDER else 99)
        # Default sugerido para GdiPageScripts no controller (Fase 3+)
        default = list(flags)
        if area in ("g", "gc") and "DataTables" not in default and agg["modal_hosts"] > 0:
            if "DataTables" not in default:
                default.append("DataTables")
            if "Select2" not in default:
                default.append("Select2")
            default = sorted(set(default), key=lambda f: FLAG_ORDER.index(f) if f in FLAG_ORDER else 99)
        lite_candidate = (
            area in ("g", "gc")
            and agg["datatables_views"] == 0
            and agg["select2_views"] == 0
            and agg["jstree_views"] == 0
            and agg["tempus_views"] == 0
            and agg["modal_hosts"] == 0
        )
        result.append({
            "area": area,
            "controller": controller,
            "views": agg["views"],
            "modal_hosts": agg["modal_hosts"],
            "default_flags": default,
            "lite_layout_candidate": lite_candidate,
            "datatables_views": agg["datatables_views"],
            "select2_views": agg["select2_views"],
            "jstree_views": agg["jstree_views"],
            "tempus_views": agg["tempus_views"],
        })
    return result


def main():
    ap = argparse.ArgumentParser(description="Inventário G-PERF-20 page scripts")
    ap.add_argument("--json", action="store_true", help="JSON completo em stdout")
    ap.add_argument("--markdown", action="store_true", help="Resumo Markdown")
    ap.add_argument("--controllers", action="store_true", help="Agregado por controller")
    ap.add_argument(
        "--out",
        default=os.path.join(BASE, ".cursor", "context", "2026_05_21_layout-scripts-inventario.json"),
        help="Gravar JSON (default: .cursor/context/...)",
    )
    args = ap.parse_args()

    rows = []
    errors = []
    for path in sorted(iter_cshtml()):
        rel = os.path.relpath(path, BASE).replace("\\", "/")
        if rel.startswith("Views/Shared/_Layout") or rel.startswith("Views/Shared/_Blank"):
            continue
        row, err = scan_view(path)
        if err:
            errors.append({"path": rel, "error": err})
        elif row:
            rows.append(row)

    controllers = aggregate_controllers(rows)
    lite = [c for c in controllers if c["lite_layout_candidate"]]
    jstree_only = [r for r in rows if r["hits"]["jstree"]]
    tempus_only = [r for r in rows if r["hits"]["tempus"]]
    modal_hosts = [r for r in rows if r["needs_modal_scripts"]]

    payload = {
        "generated_at": datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
        "script": "2026_05_22_gdi_inventory_page_scripts.py",
        "views_scanned": len(rows),
        "summary": {
            "datatables_views": sum(1 for r in rows if r["hits"]["datatables"]),
            "select2_views": sum(1 for r in rows if r["hits"]["select2"]),
            "jstree_views": len(jstree_only),
            "tempus_views": len(tempus_only),
            "main_modal_hosts": len(modal_hosts),
            "lite_layout_candidates": len(lite),
        },
        "controllers": controllers,
        "lite_layout_candidates": lite,
        "jstree_views": [{"path": r["path"], "flags": r["flags"]} for r in jstree_only],
        "tempus_views": [{"path": r["path"], "flags": r["flags"]} for r in tempus_only],
        "modal_hosts": [
            {"path": r["path"], "flags": r["flags"], "hits": r["hits"]} for r in modal_hosts
        ],
        "errors": errors,
    }

    out_path = args.out
    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    with open(out_path, "w", encoding="utf-8") as f:
        json.dump(payload, f, ensure_ascii=False, indent=2)

    if args.json:
        print(json.dumps(payload, ensure_ascii=False, indent=2))
        return 0

    if args.markdown:
        s = payload["summary"]
        print("# Inventário page scripts (G-PERF-20 Fase 0)\n")
        print("| Métrica | Valor |")
        print("|---------|-------|")
        print("| Views analisadas | {} |".format(payload["views_scanned"]))
        print("| Com DataTables | {} |".format(s["datatables_views"]))
        print("| Com Select2 | {} |".format(s["select2_views"]))
        print("| Com jstree | {} |".format(s["jstree_views"]))
        print("| Com Tempus | {} |".format(s["tempus_views"]))
        print("| Host `#mainModal.load` | {} |".format(s["main_modal_hosts"]))
        print("| Candidatos layout lite | {} |".format(s["lite_layout_candidates"]))
        print("\n## Candidatos layout lite (sem DT/Select2/jstree/tempus/modal)\n")
        print("| Área | Controller | Views |")
        print("|------|------------|-------|")
        for c in lite[:40]:
            print("| {} | {} | {} |".format(c["area"], c["controller"], c["views"]))
        if len(lite) > 40:
            print("| … | … | +{} |".format(len(lite) - 40))
        print("\n## Views com jstree\n")
        for item in payload["jstree_views"]:
            print("- `{}`".format(item["path"]))
        print("\nJSON: `{}`".format(os.path.relpath(out_path, BASE).replace("\\", "/")))
        return 0

    if args.controllers:
        print("area\tcontroller\tviews\tmodal_hosts\tDT\tS2\tjstree\ttempus\tdefault_flags\tlite?")
        for c in controllers:
            print(
                "{area}\t{controller}\t{views}\t{modal_hosts}\t{dt}\t{s2}\t{js}\t{tm}\t{flags}\t{lite}".format(
                    area=c["area"] or "-",
                    controller=c["controller"],
                    views=c["views"],
                    modal_hosts=c["modal_hosts"],
                    dt=c["datatables_views"],
                    s2=c["select2_views"],
                    js=c["jstree_views"],
                    tm=c["tempus_views"],
                    flags=",".join(c["default_flags"]),
                    lite="Y" if c["lite_layout_candidate"] else "",
                )
            )
        return 0

    s = payload["summary"]
    print("=== G-PERF-20 inventario page scripts ===")
    print("Views:", payload["views_scanned"])
    print("DataTables:", s["datatables_views"])
    print("Select2:", s["select2_views"])
    print("jstree:", s["jstree_views"])
    print("Tempus:", s["tempus_views"])
    print("mainModal hosts:", s["main_modal_hosts"])
    print("Lite layout candidates:", s["lite_layout_candidates"])
    print("JSON:", out_path)
    if errors:
        print("Errors:", len(errors))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
