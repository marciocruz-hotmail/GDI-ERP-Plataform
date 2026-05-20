# -*- coding: utf-8 -*-
"""
Grupo 1.2 — audita nomes Get* em *.Lookups.cs vs ILookupQueryService.

Uso: python Scripts/2026_05_20_gdi_audit_lookup_get_names.py [--fail]
"""
from __future__ import print_function
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
IFACE = ROOT / "Lib/Lookups/ILookupQueryService.cs"
SKIP_DIRS = {"obj", "bin", "packages", ".git", "Tests"}


def contract_methods():
    text = IFACE.read_text(encoding="utf-8")
    return set(re.findall(r"\b(Get\w+)\s*\(", text))


def calls_in_lookups_partials():
    calls = {}
    pat = re.compile(r"\.(Get\w+)\s*\(")
    for p in ROOT.rglob("*.Lookups.cs"):
        if any(x in p.parts for x in SKIP_DIRS):
            continue
        rel = str(p.relative_to(ROOT))
        for i, line in enumerate(p.read_text(encoding="utf-8", errors="ignore").splitlines(), 1):
            for m in pat.finditer(line):
                name = m.group(1)
                calls.setdefault(name, []).append((rel, i))
    return calls


def main():
    contract = contract_methods()
    calls = calls_in_lookups_partials()
    called = set(calls)
    unknown = sorted(called - contract)
    casing = []
    for u in unknown:
        for c in contract:
            if u.lower() == c.lower():
                casing.append((u, c, calls[u][0]))
                break

    print("=== 1.2.1 ILookupQueryService ===")
    print("Metodos no contrato:", len(contract))
    print("\n=== 1.2.2 *.Lookups.cs ===")
    print("Metodos distintos:", len(called))
    print("Total chamadas:", sum(len(v) for v in calls.values()))

    print("\n=== Fora do contrato ===")
    if not unknown:
        print("(nenhuma)")
    else:
        for u in unknown:
            loc = calls[u][0]
            print("%s  %s:%d" % (u, loc[0], loc[1]))

    print("\n=== Casing (chamada vs contrato) ===")
    if not casing:
        print("(nenhuma)")
    else:
        for u, c, loc in casing:
            print("%s -> %s  (%s:%d)" % (u, c, loc[0], loc[1]))

    only_contract = sorted(contract - called)
    if only_contract:
        print("\n=== No contrato, sem chamada em *.Lookups.cs (OK se usado noutro .cs) ===")
        for m in only_contract:
            print(" ", m)

    failed = bool(unknown or casing)
    if "--fail" in sys.argv and failed:
        sys.exit(1)
    sys.exit(0 if not failed else 1)


if __name__ == "__main__":
    main()
