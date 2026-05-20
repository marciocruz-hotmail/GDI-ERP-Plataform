# -*- coding: utf-8 -*-
"""
Inventário 1.7.2 — controllers Areas/g e Areas/gc com ViewBag.combo* sem *.Lookups.cs.

Uso:
  python Scripts/2026_05_20_gdi_inventory_combo_hybrid_controllers.py
  python Scripts/2026_05_20_gdi_inventory_combo_hybrid_controllers.py --markdown
"""
from __future__ import print_function
import argparse
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
COMBO_RE = re.compile(r"ViewBag\.(combo\w*|Combo\w*)", re.I)


def scan_area(area: str):
    ctrl_dir = ROOT / "Areas" / area / "Controllers"
    if not ctrl_dir.is_dir():
        return []
    rows = []
    for cs in sorted(ctrl_dir.glob("*Controller.cs")):
        if ".Lookups." in cs.name:
            continue
        lookups_path = ctrl_dir / (cs.stem + ".Lookups.cs")
        text = cs.read_text(encoding="utf-8", errors="replace")
        combos = sorted(set(m.group(1) for m in COMBO_RE.finditer(text)))
        if not combos:
            continue
        rows.append({
            "area": area,
            "controller": cs.stem,
            "has_lookups": lookups_path.exists(),
            "combo_count": len(combos),
            "combos": combos,
        })
    return rows


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--markdown", action="store_true", help="Markdown table to stdout")
    args = ap.parse_args()
    rows = scan_area("g") + scan_area("gc")
    hybrid = [r for r in rows if not r["has_lookups"]]
    if args.markdown:
        print("| Área | Controller | ViewBags |")
        print("|------|------------|----------|")
        for r in hybrid:
            vb = ", ".join(r["combos"][:4])
            if len(r["combos"]) > 4:
                vb += ", …"
            print("| {} | {} | {} |".format(r["area"], r["controller"], vb))
        print("\nTotal híbridos (sem Lookups.cs):", len(hybrid))
        return 0
    print("=== Controllers com ViewBag.combo* ===")
    for r in rows:
        flag = "HYBRID" if not r["has_lookups"] else "OK"
        print("[{}] {} / {} — {} combo(s)".format(flag, r["area"], r["controller"], r["combo_count"]))
    print("\nHíbridos (sem *.Lookups.cs):", len(hybrid))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
