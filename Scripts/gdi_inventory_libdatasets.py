# -*- coding: utf-8 -*-
"""Gera .cursor/context/lookups-libdatasets.md a partir de Lib/LibDataSets.cs."""
from __future__ import print_function
import re
from pathlib import Path
from collections import defaultdict

ROOT = Path(__file__).resolve().parents[1]
text = (ROOT / "Lib/LibDataSets.cs").read_text(encoding="utf-8")
parts = re.split(r"(?=\n        public static )", text)


def usages(name):
    files = []
    count = 0
    for p in ROOT.rglob("*.cs"):
        if any(x in p.parts for x in ["obj", "bin", "packages", ".git"]):
            continue
        if p.name == "LibDataSets.cs":
            continue
        c = p.read_text(encoding="utf-8", errors="ignore").count("LibDataSets." + name)
        if c:
            count += c
            files.append(str(p.relative_to(ROOT)))
    return count, files


rows = []
for part in parts:
    m = re.search(r"public static ([\w<>,\s]+?)\s+(Load\w+)\s*\(([^)]*)\)", part, re.DOTALL)
    if not m:
        continue
    ret, name, params = m.group(1).strip(), m.group(2), m.group(3).strip()
    cache_props = list(dict.fromkeys(re.findall(r"CachePersister\.contextoModel\.(\w+)", part)))
    tables = re.findall(r'IsTableUpdate\("([^"]+)",\s*"([^"]+)"', part)
    json_clone = "JsonConvert.DeserializeObject" in part and "SerializeObject" in part
    returns_direct = "return CachePersister.contextoModel" in part and not json_clone
    params_list = [x.strip() for x in params.split(",") if x.strip()] if params else []
    parametric = any(x for x in params_list if x != "GdiPlataformEntities db")
    cnt, _ = usages(name)
    risk = []
    if parametric and cache_props:
        risk.append("CACHE_PARAMETRICO")
    if parametric and re.search(r"Count == 0", part):
        risk.append("CACHE_SO_COUNT0")
    rows.append(
        (
            name,
            ret,
            params or "-",
            "Sim" if parametric else "Nao",
            ", ".join(cache_props) or "-",
            "; ".join(["%s (%s)" % (t[0], t[1]) for t in tables]) or "-",
            "JSON clone" if json_clone else ("Direto cache" if returns_direct else "Lista local"),
            cnt,
            risk,
        )
    )

lines = [
    "# Inventario LibDataSets (Fase 0)",
    "",
    "Gerado por `Scripts/gdi_inventory_libdatasets.py` a partir de `Lib/LibDataSets.cs`.",
    "Stack: .NET Framework 4.7.2, EF6, cache em `CachePersister.contextoModel` (MemoryCache, chave por TokenId, sliding 15 min).",
    "",
    "## Resumo",
    "",
    "| Metrica | Valor |",
    "|--------|-------|",
    "| Metodos publicos Load* | %d |" % len(rows),
    "| Total chamadas em controllers | %d |" % sum(r[7] for r in rows),
    "| Com parametros alem de db | %d |" % sum(1 for r in rows if r[3] == "Sim"),
    "| Risco cache parametrico (Fase 1) | %d |"
    % sum(1 for r in rows if r[8]),
    "",
    "## Legenda",
    "",
    "| Coluna | Significado |",
    "|--------|-------------|",
    "| Parametrico | Recebe IdCliente, IdLocalEstoque, IdTipo, etc. |",
    "| Cache contextoModel | Propriedade em `Models/ContextoModel.cs` |",
    "| IsTableUpdate | Invalidacao via `LibDB.IsTableUpdate(tabela, processo, db)` |",
    "| Retorno | Clone JSON / direto do cache / lista local |",
    "| Risco Fase 1 | CACHE_PARAMETRICO = slot unico com filtro por parametro; CACHE_SO_COUNT0 = so recarrega se Count==0 |",
    "",
    "## Inventario metodo a metodo",
    "",
    "| # | Metodo | Retorno | Parametros | Param. | Cache contextoModel | IsTableUpdate | Retorno | Chamadas | Risco Fase 1 |",
    "|---|--------|---------|------------|--------|---------------------|---------------|---------|----------|--------------|",
]
for i, r in enumerate(sorted(rows, key=lambda x: x[0]), 1):
    risk = ", ".join(r[8]) if r[8] else "-"
    lines.append(
        "| %d | `%s` | `%s` | %s | %s | %s | %s | %s | %d | %s |"
        % (i, r[0], r[1], r[2], r[3], r[4], r[5], r[6], r[7], risk)
    )

lines.extend(["", "## Metodos com correcao Fase 1 (cache parametrico)", ""])
for r in sorted(rows, key=lambda x: x[0]):
    if r[8]:
        lines.append("- **%s** — %s — chamadas: %d" % (r[0], ", ".join(r[8]), r[7]))

lines.extend(["", "## Detalhe por metodo (consumo)", ""])
for r in sorted(rows, key=lambda x: x[0]):
    cnt, files = usages(r[0])
    if cnt == 0:
        continue
    lines.append("### `%s`" % r[0])
    lines.append("- Chamadas: %d" % cnt)
    for f in sorted(files)[:12]:
        lines.append("  - `%s`" % f)
    if len(files) > 12:
        lines.append("  - ... +%d ficheiros" % (len(files) - 12))
    lines.append("")

out = ROOT / ".cursor/context/lookups-libdatasets.md"
out.parent.mkdir(parents=True, exist_ok=True)
out.write_text("\n".join(lines) + "\n", encoding="utf-8")
print("OK", out)
