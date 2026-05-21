# -*- coding: utf-8 -*-
"""
G-PERF-20e / G-PERF-M02 — Manifesto de flags esperadas por rota (smoke DevTools).

Uso: python Scripts/2026_05_20_gdi_page_scripts_smoke_manifest.py
     python Scripts/2026_05_20_gdi_page_scripts_smoke_manifest.py --markdown

Validar no browser (Network, Disable cache):
  - Hub relatório: sem datatables.min.js / select2 (exceto Financeiros + S2)
  - IndexPedido: com datatables + select2
  - Meta G-PERF-M02: transferred hub relatório < ~800 kB (medir após login)
"""
from __future__ import print_function

import argparse

# area, controller, action, flags esperadas, notas smoke
ROUTES = [
    ("gc", "RelatoriosRegulamentacao", "Index", "Core,Toggle", "sem DT/S2; Tempus na view; modal datas OK"),
    ("gc", "RelatoriosComerciais", "Index", "Core,Toggle", "idem"),
    ("gc", "RelatoriosFinanceiros", "Index", "Core,Toggle,Select2", "modal lookup cliente"),
    ("gc", "RelatoriosCadastrais", "Index", "Core,Toggle", "sem Tempus no modal cadastral"),
    ("gc", "Parametros", "Index", "Core,Toggle", "layout lite"),
    ("g", "CentrosCustos", "Index", "Core,Toggle,Jstree", "árvore OK"),
    ("qa", "Treinamentos", "Index", "Core,Toggle", "layout lite SGQ treinamentos"),
    ("crm", "Pedidos", "Index", "Core,Toggle", "portal sem DT/S2"),
    ("g", "ClassificacaoFinanceira", "Index", "Core,Toggle,Jstree", "árvore OK"),
    ("gc", "Movimentos", "IndexPedido", "Core,Toggle,DataTables,Select2", "regressão pedidos"),
    ("g", "Financeiro", "Index", "Core,Toggle,DataTables,Select2", "regressão financeiro"),
    ("g", "Produtos", "Index", "Core,Toggle,DataTables,Select2", "regressão cadastro"),
    ("qa", "GedSGQ", "IndexPops", "Core,Toggle", "POPs SGQ sem DT/S2"),
    ("g", "CentrosCustos", "Create", "Core,Toggle", "CreateEdit lite lote C"),
    ("gc", "Cfop", "Edit", "Core,Toggle", "CreateEdit lite lote C"),
]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--markdown", action="store_true")
    args = ap.parse_args()
    if args.markdown:
        print("| Área | Controller | Action | Flags | Smoke |")
        print("|------|------------|--------|-------|-------|")
        for area, ctrl, act, flags, note in ROUTES:
            print("| `%s` | `%s` | `%s` | %s | %s |" % (area or "(root)", ctrl, act, flags, note))
        return
    print("G-PERF-20e smoke manifest (%d rotas)\n" % len(ROUTES))
    for area, ctrl, act, flags, note in ROUTES:
        path = "/%s/%s/%s" % (area, ctrl, act) if area else "/%s/%s" % (ctrl, act)
        print("%-45s  %s  # %s" % (path, flags, note))
    print("\nG-PERF-M02: medir transferred (KB) em homologação nas rotas hub vs IndexPedido.")


if __name__ == "__main__":
    main()
