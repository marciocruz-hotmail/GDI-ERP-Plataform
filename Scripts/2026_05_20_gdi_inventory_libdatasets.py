# -*- coding: utf-8 -*-
"""Gera .cursor/context/2026_05_20_lookups-libdatasets.md a partir de ILookupQueryService (Onda 6b)."""
from __future__ import print_function
import re
from pathlib import Path
from collections import defaultdict

ROOT = Path(__file__).resolve().parents[1]
IFACE = ROOT / "Lib/Lookups/ILookupQueryService.cs"
OUT = ROOT / ".cursor/context/2026_05_20_lookups-libdatasets.md"
SKIP_DIRS = {"obj", "bin", "packages", ".git", "Tests"}


def usages(name):
    files = []
    count = 0
    needle = "." + name + "("
    for p in ROOT.rglob("*.cs"):
        if any(x in p.parts for x in SKIP_DIRS):
            continue
        if p.name == "ILookupQueryService.cs":
            continue
        c = p.read_text(encoding="utf-8", errors="ignore").count(needle)
        if c:
            count += c
            files.append(str(p.relative_to(ROOT)))
    return count, files


def main():
    text = IFACE.read_text(encoding="utf-8")
    methods = re.findall(
        r"^\s+(?:List<[\w<>,\s]+>|List<\w+>)\s+(Get\w+)\s*\(([^)]*)\)\s*;",
        text,
        re.MULTILINE,
    )
    rows = []
    total_calls = 0
    for name, params in methods:
        params_list = [x.strip() for x in params.split(",") if x.strip()] if params else []
        parametric = any(
            x for x in params_list
            if "GdiPlataformEntities db" not in x or len(params_list) > 1
        )
        cnt, files = usages(name)
        total_calls += cnt
        rows.append((name, params, parametric, cnt, files))

    lines = [
        "# Inventario lookups (ILookupQueryService)",
        "",
        "**Estado (Onda 6b + 1.8):** `Lib/LibDataSets.cs` removido. Contratos em `ILookupQueryService` + `LookupQueryService` (+ partials Comercial/Financeiro/CadastrosG).",
        "",
        "Gerado por `Scripts/2026_05_20_gdi_inventory_libdatasets.py` a partir de `Lib/Lookups/ILookupQueryService.cs`.",
        "",
        "## Resumo",
        "",
        "| Metrica | Valor |",
        "|--------|-------|",
        "| Metodos Get* / GetDataset* | %d |" % len(rows),
        "| Total chamadas (grep `.GetX(`) | %d |" % total_calls,
        "",
        "## Metodos",
        "",
        "| Metodo | Parametros | Parametrico | Chamadas | Consumidores |",
        "|--------|------------|-------------|----------|--------------|",
    ]
    for name, params, parametric, cnt, files in rows:
        consumers = ", ".join(sorted(set(files))[:5])
        if len(files) > 5:
            consumers += " (+%d)" % (len(files) - 5)
        lines.append(
            "| `%s` | `%s` | %s | %d | %s |"
            % (name, params or "db", "Sim" if parametric else "Nao", cnt, consumers or "-")
        )
    lines.append("")
    OUT.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print("Escrito:", OUT, "—", len(rows), "metodos")


if __name__ == "__main__":
    main()
