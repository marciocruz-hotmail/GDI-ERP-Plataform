# -*- coding: utf-8 -*-
"""
PERF-007 — Inventário de DataTables com paginação em memória.

Heurística: método GetDados* com
  iTotalRecords = allRecords.Count(...)
sem LibDataTableSqlPaging.SqlCount / query.Count() antes do Skip na mesma action.

Uso (raiz do repo):
  python Scripts/2026_05_22_gdi_inventory_datatables_memory_paging.py

Exit 0 = lote 2 fechado (nenhum PENDENTE nos controllers do lote 2).
Exit 1 = há candidatos PENDENTE (lote 3+); ver .cursor/context/2026_05_22_perf007-lote2-inventario.md
"""
from __future__ import print_function

import os
import re
import sys

BASE = os.path.normpath(os.path.join(os.path.dirname(__file__), ".."))
CONTROLLERS = os.path.join(BASE, "Areas")

RX_METHOD = re.compile(
    r"public\s+ActionResult\s+(GetDados\w*)\s*\(\s*jQueryDataTableParamModel\s+param\s*\)",
    re.IGNORECASE,
)
RX_ALLRECORDS_COUNT = re.compile(
    r"iTotalRecords\s*=\s*allRecords\.Count",
    re.IGNORECASE,
)
RX_SQL_PAGING = re.compile(r"LibDataTableSqlPaging\.(SqlCount|SqlPage)", re.IGNORECASE)
RX_QUERY_COUNT = re.compile(
    r"(?:int\s+)?totalRecords\s*=\s*query\.Count\s*\(\s*\)",
    re.IGNORECASE,
)

# Lote 2 corrigido (2026-05-20) — não reportar como pendente
LOTE2_OK = {
    ("Areas/g/Controllers/ClassificacaoFinanceiraController.cs", "getDados"),
    ("Areas/qa/Controllers/GedSGQController.cs", "GetDados"),
    ("Areas/qa/Controllers/GedSGQController.cs", "GetDadosArquivos"),
    ("Areas/qa/Controllers/GedSGQController.cs", "GetDadosArquivosVencidos"),
    ("Areas/qa/Controllers/GedSGQController.cs", "GetDadosArquivosVencendo"),
    ("Areas/g/Controllers/AtendimentosController.cs", "getDadosAtendimentos"),
    ("Areas/gc/Controllers/ComexImportacoesController.cs", "GetDadosItensImportacao"),
    ("Areas/gc/Controllers/ComexImportacoesController.cs", "GetGedComex"),
    ("Areas/gc/Controllers/MovimentosController.cs", "GetDadosModalItensComValor"),
    ("Areas/gc/Controllers/MovimentosController.cs", "GetRelatorioConsultaPedidos"),
    ("Areas/gc/Controllers/MovimentosController.cs", "GetNotaFiscalPedido"),
    ("Areas/gc/Controllers/MovimentosController.cs", "GetDadosCartaCorrecao"),
}

# Aceite documentado — refactor maior ou escopo fora do lote 2
ACEITE_DOC = {
    (
        "Areas/gc/Controllers/MovimentosController.cs",
        "GetDadosInvoicesItensEspelhoDigital",
    ): "DataTable/DataRow + dedupe por id_invoice_item; paginar SQL na fase seguinte",
}


def slice_method(body, start_pos):
    sub = body[start_pos:]
    m = re.search(
        r"\n\s*#region|\n\s*public\s+(?:ActionResult|void|JsonResult|bool|int|string)",
        sub[1:],
    )
    if m:
        return sub[: m.start() + 1]
    return sub[:8000]


def scan_file(path):
    rel = os.path.relpath(path, BASE).replace("\\", "/")
    try:
        with open(path, "r", encoding="utf-8", errors="ignore") as f:
            body = f.read()
    except OSError as e:
        return [], [("ERROR", rel, "", str(e))]

    hits = []
    for m in RX_METHOD.finditer(body):
        name = m.group(1)
        line_no = body[: m.start()].count("\n") + 1
        chunk = slice_method(body, m.start())
        if not RX_ALLRECORDS_COUNT.search(chunk):
            continue
        key = (rel, name)
        status = "PENDENTE"
        if key in LOTE2_OK:
            status = "LOTE2_OK"
        elif key in ACEITE_DOC:
            status = "ACEITE_DOC"
        elif RX_SQL_PAGING.search(chunk) or RX_QUERY_COUNT.search(chunk):
            status = "REVISAR"  # Count em memória mas pode ser só display name
        hits.append((status, rel, name, line_no, ACEITE_DOC.get(key, "")))
    return hits, []


def main():
    if not os.path.isdir(CONTROLLERS):
        print("missing:", CONTROLLERS)
        sys.exit(2)

    all_hits = []
    for dirpath, _, files in os.walk(CONTROLLERS):
        for fn in sorted(files):
            if not fn.endswith("Controller.cs"):
                continue
            path = os.path.join(dirpath, fn)
            hits, errs = scan_file(path)
            all_hits.extend(hits)
            for e in errs:
                print("\t".join(e))

    pendente = [h for h in all_hits if h[0] == "PENDENTE"]
    aceite = [h for h in all_hits if h[0] == "ACEITE_DOC"]
    revisar = [h for h in all_hits if h[0] == "REVISAR"]

    print("status\tfile\tmethod\tline\tnote")
    for h in sorted(all_hits, key=lambda x: (x[0], x[1], x[2])):
        print("\t".join(str(x) for x in h))

    print("\n--- resumo PERF-007 ---")
    print("allRecords.Count (memória):", len(all_hits))
    print("PENDENTE (lote 3+):", len(pendente))
    print("ACEITE_DOC:", len(aceite))
    print("REVISAR:", len(revisar))
    print("LOTE2_OK (corrigido, ainda com padrão legado no grep):", len([h for h in all_hits if h[0] == "LOTE2_OK"]))

    lote2_actions = {(k[0].lower(), k[1].lower()) for k in LOTE2_OK}
    lote2_pending = [
        h for h in pendente
        if (h[1].lower(), h[2].lower()) in lote2_actions
    ]
    if lote2_pending:
        print("LOTE2 FALHOU:")
        for h in lote2_pending:
            print(" ", h[1], h[2])
        sys.exit(1)
    if pendente:
        print("LOTE3+ PENDENTE:", len(pendente), "(exit 1 — ver context perf007)")
        sys.exit(1)
    sys.exit(0)


if __name__ == "__main__":
    main()
