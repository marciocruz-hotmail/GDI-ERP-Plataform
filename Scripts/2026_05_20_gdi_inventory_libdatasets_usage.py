# -*- coding: utf-8 -*-
"""
Fase 4 P4 / Fase 5 / Onda 6b — inventario de uso de LibDataSets.

- Orfaos: metodos Load* sem referencia fora de Lib/LibDataSets.cs
- Nao migrados: LibDataSets.* em controllers fora de *.Lookups.cs

Onda 6b: LibDataSets.cs removido — verifica apenas ausencia de referencias no codigo.

Uso: python Scripts/2026_05_20_gdi_inventory_libdatasets_usage.py [--fail]
"""
from __future__ import print_function
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
LDS = ROOT / "Lib/LibDataSets.cs"
SKIP_DIRS = {"obj", "bin", "packages", ".git", "Tests"}


def iter_cs():
    for p in ROOT.rglob("*.cs"):
        if any(x in p.parts for x in SKIP_DIRS):
            continue
        yield p


def scan_libdatasets_refs():
    refs = []
    for p in iter_cs():
        text = p.read_text(encoding="utf-8", errors="ignore")
        if "LibDataSets." in text:
            hits = len(re.findall(r"LibDataSets\.\w+", text))
            refs.append((str(p.relative_to(ROOT)), hits))
    return refs


def main():
    if not LDS.is_file():
        print("=== LibDataSets.cs ===")
        print("(removido — Onda 6b)")
        refs = scan_libdatasets_refs()
        print("\n=== Referencias LibDataSets.* no codigo ===")
        if not refs:
            print("(nenhuma)")
        else:
            for path, hits in sorted(refs):
                print("%s (%d)" % (path, hits))
        if "--fail" in sys.argv and refs:
            sys.exit(1)
        return

    lds = LDS.read_text(encoding="utf-8")
    methods = re.findall(r"public static [\w<>,\s]+ (Load\w+)\s*\(", lds)
    orphans = []

    for name in sorted(set(methods)):
        count = 0
        for p in iter_cs():
            if p.name == "LibDataSets.cs":
                continue
            count += p.read_text(encoding="utf-8", errors="ignore").count("LibDataSets." + name)
        if count == 0:
            orphans.append(name)

    non_migrated = []
    for p in iter_cs():
        if p.name.endswith(".Lookups.cs"):
            continue
        text = p.read_text(encoding="utf-8", errors="ignore")
        if "LibDataSets." in text:
            hits = len(re.findall(r"LibDataSets\.Load\w+", text))
            non_migrated.append((str(p.relative_to(ROOT)), hits))

    print("=== Orfaos (0 refs fora LibDataSets.cs) ===")
    for n in orphans:
        print(n)
    print("\nTotal orfaos:", len(orphans), "/", len(methods))

    print("\n=== Referencias nao migradas (fora de *.Lookups.cs) ===")
    if not non_migrated:
        print("(nenhuma)")
    else:
        for path, hits in sorted(non_migrated):
            print("%s (%d)" % (path, hits))
    print("\nTotal ficheiros nao migrados:", len(non_migrated))

    if "--fail" in sys.argv and (orphans or non_migrated):
        sys.exit(1)


if __name__ == "__main__":
    main()
