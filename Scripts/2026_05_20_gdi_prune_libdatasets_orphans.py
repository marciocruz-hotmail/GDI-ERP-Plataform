# -*- coding: utf-8 -*-
"""Remove metodos Load* orfaos de Lib/LibDataSets.cs (Fase 4 P4 / Fase 5)."""
from __future__ import print_function
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
LDS = ROOT / "Lib/LibDataSets.cs"
SKIP_DIRS = {"obj", "bin", "packages", ".git", "Tests"}


def find_orphans():
    lds = LDS.read_text(encoding="utf-8")
    methods = re.findall(r"public static [\w<>,\s]+ (Load\w+)\s*\(", lds)
    orphans = []
    for name in sorted(set(methods)):
        count = 0
        for p in ROOT.rglob("*.cs"):
            if any(x in p.parts for x in SKIP_DIRS):
                continue
            if p.name == "LibDataSets.cs":
                continue
            count += p.read_text(encoding="utf-8", errors="ignore").count("LibDataSets." + name)
        if count == 0:
            orphans.append(name)
    return orphans


def prune(orphans):
    text = LDS.read_text(encoding="utf-8")
    orphan_set = set(orphans)

    split_re = r"(?=\r?\n        \[Obsolete\(ObsoleteLookupMsg\)\]\r?\n        public static )"
    parts = re.split(split_re, text)
    header = parts[0]
    kept = [header.rstrip()]
    removed = []

    for part in parts[1:]:
        m = re.search(r"public static [\w<>,\s]+? (Load\w+)\s*\(", part)
        if m and m.group(1) in orphan_set:
            removed.append(m.group(1))
            continue
        kept.append(part)

    out = "".join(kept)
    if not out.endswith("\n"):
        out += "\n"
    LDS.write_text(out, encoding="utf-8", newline="\r\n")
    return removed


if __name__ == "__main__":
    if not LDS.is_file():
        print("LibDataSets.cs removido (Onda 6b). Nada a podar.")
        sys.exit(0)
    orphans = find_orphans()
    if not orphans:
        print("Nenhum orfao para remover.")
        sys.exit(0)
    removed = prune(orphans)
    print("Removidos:", len(removed), removed)
