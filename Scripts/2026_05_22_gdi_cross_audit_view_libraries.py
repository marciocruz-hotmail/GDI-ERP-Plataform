# -*- coding: utf-8 -*-
"""
Auditoria cruzada views/modais: componentes visuais vs bibliotecas carregadas (G-PERF-20).
Gera JSON + resumo de gaps (layout flags vs necessidade real vs partials Tempus).

Uso: python Scripts/2026_05_22_gdi_cross_audit_view_libraries.py
"""
from __future__ import print_function

import json
import os
import re
from collections import defaultdict
from datetime import datetime

BASE = os.path.normpath(os.path.join(os.path.dirname(__file__), ".."))
VIEW_ROOTS = [os.path.join(BASE, "Areas"), os.path.join(BASE, "Views")]

# --- Detecção de componentes na view ---
COMPONENTS = {
    "datatables": re.compile(
        r"\.DataTable\s*\(|\.dataTable\s*\(|bServerSide\s*:\s*true|GdiDt|error\.dt|xhr\.dt",
        re.I,
    ),
    "select2": re.compile(
        r"\.select2\s*\(|data[-_]gdi[-_]lookup[-_]url|gdi-select2|GdiSelect2|GetClientesLookup|GetProdutosLookup",
        re.I,
    ),
    "jstree": re.compile(r"\.jstree\s*\(|jstree\s*\(|id\s*=\s*['\"]tree['\"]", re.I),
    "tempus": re.compile(
        r"jsDatepicker\w*\s*\(|jsInitDateTimepicker|tempus-dominus-input|data_td_target",
        re.I,
    ),
    "toggle": re.compile(r"bootstrapToggle|bootstrap4-toggle|data-toggle=[\"']toggle[\"']", re.I),
    "main_modal": re.compile(
        r'[#$]?\s*\(\s*["\']#mainModal["\']\s*\)\s*\.\s*load|GdiMainModalLoad',
        re.I,
    ),
    "swal_libmessage": re.compile(r"LibMessage\w+|GdiAjaxNotify|GdiDtNotify", re.I),
    "file_input": re.compile(r"jsFileInputChange|type\s*=\s*[\"']file[\"']|customFile", re.I),
    "chart": re.compile(r"Chart\.|chart\.js|apexcharts|morris", re.I),
}

LAYOUT_MODAL = re.compile(r"_Modal\.cshtml|Layout\s*=\s*null", re.I)
LAYOUT_BLANK = re.compile(r"_Blank\.cshtml", re.I)
LAYOUT_FULL = re.compile(r"~/Views/Shared/_Layout\.cshtml", re.I)
TEMPUS_HEAD = "_LayoutHeadTempus"
TEMPUS_SCRIPTS = "_LayoutScriptsTempus"

# Atributos [GdiPageScripts] conhecidos (grep manual — expandir se necessário)
ATTR_OVERRIDES = {
    ("g", "CentrosCustos", "Index"): "LayoutHubJstree",
    ("g", "ClassificacaoFinanceira", "Index"): "LayoutHubJstree",
    ("gc", "Parametros", "Index"): "LayoutLite",
    ("gc", "RelatoriosComerciais", "Index"): "LayoutHubReport",
    ("gc", "RelatoriosRegulamentacao", "Index"): "LayoutHubReport",
    ("gc", "RelatoriosCadastrais", "Index"): "LayoutHubReport",
    ("gc", "RelatoriosFinanceiros", "Index"): "LayoutHubReportSelect2",
    ("qa", "Treinamentos", "Index"): "LayoutLite",
    ("qa", "GedSGQ", "IndexPops"): "LayoutHubJstree",
    ("crm", "Pedidos", "Index"): "LayoutPortalCliente",
}

# View (.cshtml) → actions MVC que a servem (Resolve usa action da rota, não nome da view)
VIEW_TEMPUS_MVC_ACTIONS = {
    ("g", "Clientes", "CreateEdit"): ("Create", "Edit"),
    ("g", "ContratosAviacao", "CreateEdit"): ("Create", "Edit"),
    ("g", "Financeiro", "Index"): ("Index",),
    ("g", "Ged", "Index"): ("Index",),
    ("g", "Nfe", "CreateEdit"): ("Edit",),
    ("g", "Nfe", "Index"): ("Index",),
    ("gc", "ComexImportacoes", "CreateEdit"): ("Create", "Edit"),
    ("gc", "ComexFinanceiro", "Index"): ("Index",),
    ("gc", "EstoqueControle", "CreateEdit"): ("Create", "Edit"),
    ("gc", "FinanceiroLancamentos", "Index"): ("Index",),
    ("gc", "Fretes", "Index"): ("Index",),
    ("gc", "Movimentos", "FormPedidoCreate"): (
        "CreateCotacao", "CreatePedido", "CreateOS", "EditPedido",
    ),
    ("gc", "Movimentos", "IndexPedido"): ("IndexPedido",),
    ("gc", "Movimentos", "PainelPedidos"): ("PainelPedidos",),
    ("qa", "GedSGQ", "IndexAtasReunioes"): ("IndexAtasReunioes",),
    ("qa", "GedSGQ", "IndexComunicados"): ("IndexComunicados",),
    ("qa", "GedSGQ", "IndexDocsSGQ"): ("IndexDocsSGQ",),
}

# Espelha C# BuildTempusActionsByAreaController (actions MVC)
TEMPUS_ROUTE_FULL = set(
    (a, c, act)
    for (a, c, _view), acts in VIEW_TEMPUS_MVC_ACTIONS.items()
    for act in acts
)

# LayoutLite actions map (subset from GdiPageScriptsDefaults — mirrors C#)
LAYOUT_LITE_ACTIONS = {
    "GedSGQ": {"IndexPops"},
    "Treinamentos": {"IndexTreinamentoAviacao001"},
    "CentrosCustos": {"Create", "Edit"},
    "ClassificacaoFinanceira": {"Create", "Edit"},
    "Cidades": {"Create", "Edit"},
    "ContasCaixas": {"Create", "Edit"},
    "Filiais": {"Create", "Edit"},
    "PagRecCondicoes": {"Create", "Edit"},
    "PagRecTipos": {"Create", "Edit"},
    "Perfis": {"Create", "Edit"},
    "ProdutosNcm": {"Create", "Edit"},
    "UF": {"Create", "Edit"},
    "Usuarios": {"Create", "Edit"},
    "Cfop": {"Create", "Edit"},
    "FinanceiroParametroDifal": {"Create", "Edit"},
    "ComexProdutos": {"FormProcessarProdutosPreNovos", "FormProcessarProdutosPreAtualizar"},
    "MovimentosEntradas": {
        "FormProcessarNFCompraNacional",
        "FormProcessarNFDevolucao",
        "FormProcessarNFImportacao",
    },
}

NO_DT_CONTROLLERS = {
    "CentrosCustos", "ClassificacaoFinanceira", "Parametros",
    "RelatoriosCadastrais", "RelatoriosComerciais", "RelatoriosFinanceiros",
    "RelatoriosRegulamentacao", "Treinamentos",
}
JSTREE_CONTROLLERS = {"CentrosCustos", "ClassificacaoFinanceira"}

FLAG_BITS = {
    "Core": 1, "DataTables": 2, "Select2": 4, "Tempus": 8, "Jstree": 16, "Toggle": 32,
}
PRESETS = {
    "LayoutHubReport": {"Core", "Toggle", "Tempus"},
    "LayoutHubReportSelect2": {"Core", "Toggle", "Select2", "Tempus"},
    "LayoutHubJstree": {"Core", "Toggle", "Jstree"},
    "LayoutLite": {"Core", "Toggle"},
    "LayoutPortalCliente": {"Core", "Toggle"},
    "DefaultGcGArea": {"Core", "DataTables", "Select2", "Toggle"},
    "CoreOnly": {"Core"},
}


def needs_tempus(area, controller, action):
    return (area, controller, action) in TEMPUS_ROUTE_FULL


def layout_flags_for_host_view(area, controller, view_name):
    """União de flags para todas as actions MVC que renderizam esta view."""
    key = (area, controller, view_name)
    actions = VIEW_TEMPUS_MVC_ACTIONS.get(key, (view_name,))
    flags = set()
    for act in actions:
        f, _ = resolve_layout_flags(area, controller, act)
        flags |= f
    return flags


def parse_mvc(rel):
    parts = rel.replace("\\", "/").split("/")
    view = parts[-1].replace(".cshtml", "")
    if parts[0] == "Areas" and len(parts) >= 5 and parts[2] == "Views":
        return parts[1], parts[3], view
    if parts[0] == "Views" and len(parts) >= 2:
        return "", parts[1], view
    return "", "", view


def resolve_layout_flags(area, controller, action):
    key = (area, controller, action)
    if key in ATTR_OVERRIDES:
        return PRESETS[ATTR_OVERRIDES[key]], ATTR_OVERRIDES[key]

    if area in ("",) or area == "crm":
        if area == "crm":
            return PRESETS["LayoutPortalCliente"], "crm_default"
        return PRESETS["CoreOnly"], "root_default"

    flags = set(PRESETS["CoreOnly"])
    flags.add("Toggle")

    lite_actions = LAYOUT_LITE_ACTIONS.get(controller, set())
    if action in lite_actions:
        return PRESETS["LayoutLite"], "LayoutLiteActions"

    if controller in JSTREE_CONTROLLERS:
        flags.add("Jstree")

    if area in ("g", "gc", "qa"):
        if controller not in NO_DT_CONTROLLERS:
            flags.add("DataTables")
            flags.add("Select2")
    elif area == "a":
        flags.add("Toggle")
        if controller == "Parametros":
            flags.add("DataTables")

    if needs_tempus(area, controller, action):
        flags.add("Tempus")

    return flags, "Resolve_default"


def flags_to_bitset(flags):
    b = 0
    for f in flags:
        b |= FLAG_BITS.get(f, 0)
    return b


def iter_cshtml():
    for root in VIEW_ROOTS:
        if not os.path.isdir(root):
            continue
        for dp, _, fns in os.walk(root):
            for fn in fns:
                if fn.endswith(".cshtml"):
                    yield os.path.join(dp, fn)


def scan():
    rows = []
    for path in sorted(iter_cshtml()):
        rel = os.path.relpath(path, BASE).replace("\\", "/")
        if rel.startswith("Views/Shared/_Layout") and "_Modal" not in rel:
            if rel.endswith("_Layout.cshtml") or "Head" in rel or "Scripts" in rel:
                continue
        with open(path, encoding="utf-8", errors="ignore") as f:
            text = f.read()

        area, controller, action = parse_mvc(rel)
        is_modal = bool(LAYOUT_MODAL.search(text)) or "/Modal" in rel
        is_blank = bool(LAYOUT_BLANK.search(text))
        is_host = bool(LAYOUT_FULL.search(text)) and not is_modal

        comps = {k: bool(rx.search(text)) for k, rx in COMPONENTS.items()}
        has_tempus_partial = TEMPUS_HEAD in text and TEMPUS_SCRIPTS in text
        has_tempus_partial_any = TEMPUS_HEAD in text or TEMPUS_SCRIPTS in text

        if is_host:
            layout_provides = layout_flags_for_host_view(area, controller, action)
            layout_source = "Resolve_view_actions"
        else:
            layout_flags, layout_source = resolve_layout_flags(area, controller, action)
            layout_provides = layout_flags.copy()

        needs = set()
        if comps["datatables"]:
            needs.add("DataTables")
        if comps["select2"]:
            needs.add("Select2")
        if comps["jstree"]:
            needs.add("Jstree")
        if comps["tempus"]:
            needs.add("Tempus")
        if comps["toggle"]:
            needs.add("Toggle")
        needs.add("Core")

        missing_in_layout = needs - layout_provides
        gaps = []

        if is_modal:
            load_mode = "modal_ajax"
            # Modais: DT/S2/Tempus via GdiMainModalLoad se detectado no HTML
            if missing_in_layout:
                gaps.append(
                    "modal_fragment: depende de layout pai + GdiMainModalLoad para: "
                    + ", ".join(sorted(missing_in_layout - {"Core"}))
                )
        elif is_blank:
            load_mode = "blank_self"
            if comps["tempus"] and "tempus-dominus" not in text.lower():
                gaps.append("blank: verificar assets Tempus inline")
        elif is_host:
            load_mode = "layout_host"
            for lib in missing_in_layout:
                gaps.append("CRITICAL: %s necessário mas GdiPageScripts/layout não inclui" % lib)
        else:
            load_mode = "other"

        rows.append({
            "path": rel,
            "area": area,
            "controller": controller,
            "action": action,
            "layout": "modal" if is_modal else ("blank" if is_blank else ("host" if is_host else "other")),
            "load_mode": load_mode,
            "components": comps,
            "needs": sorted(needs, key=lambda x: list(FLAG_BITS).index(x) if x in FLAG_BITS else 99),
            "layout_provides": sorted(layout_provides),
            "layout_source": layout_source,
            "tempus_partial": has_tempus_partial,
            "gaps": gaps,
            "gap_severity": "ok" if not gaps else ("critical" if any("CRITICAL" in g for g in gaps) else "warn"),
        })
    return rows


def summarize(rows):
    by_gap = defaultdict(list)
    for r in rows:
        if r["gaps"]:
            for g in r["gaps"]:
                if g.startswith("CRITICAL"):
                    by_gap[g.split(":")[0]].append(r["path"])

    critical_hosts = [
        r for r in rows
        if r["layout"] == "host" and r["gap_severity"] == "critical"
    ]
    tempus_hosts_need = [r["path"] for r in rows if r["layout"] == "host" and "Tempus" in r["needs"]]
    tempus_missing_partial = [
        r["path"] for r in rows
        if r["layout"] == "host" and "Tempus" in r["needs"] and not r["tempus_partial"]
    ]
    modal_with_tempus = [r["path"] for r in rows if r["layout"] == "modal" and r["components"]["tempus"]]
    hub_no_dt_layout = [
        r for r in rows
        if r["layout"] == "host"
        and r["components"]["main_modal"]
        and "DataTables" not in r["layout_provides"]
        and r["components"]["datatables"]
    ]

    return {
        "views_total": len(rows),
        "critical_host_count": len(critical_hosts),
        "tempus_hosts_need": len(tempus_hosts_need),
        "tempus_hosts_missing_partial": len(tempus_missing_partial),
        "modal_with_tempus": len(modal_with_tempus),
        "hub_host_dt_mismatch": len(hub_no_dt_layout),
        "by_component": {
            k: sum(1 for r in rows if r["components"][k])
            for k in COMPONENTS
        },
        "by_layout": defaultdict(int),
    }, critical_hosts, tempus_missing_partial, modal_with_tempus


def main():
    import sys
    rows = scan()
    summary, critical, tempus_miss, modals_tempus = summarize(rows)

    for r in rows:
        summary["by_layout"][r["layout"]] += 1
    summary["by_layout"] = dict(summary["by_layout"])

    out = {
        "generated_at": datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
        "summary": summary,
        "critical_hosts": [
            {"path": r["path"], "needs": r["needs"], "layout_provides": r["layout_provides"], "gaps": r["gaps"]}
            for r in critical
        ],
        "tempus_hosts_missing_partial": tempus_miss,
        "views": rows,
    }

    out_path = os.path.join(
        BASE, ".cursor", "context", "2026_05_21_cross_audit_view_libraries.json"
    )
    with open(out_path, "w", encoding="utf-8") as f:
        json.dump(out, f, indent=2, ensure_ascii=False)

    exit_code = 1 if critical else 0
    if critical:
        print("FALHA: %d host(s) com gap CRITICAL (pre-publish)." % len(critical), file=sys.stderr)
        for r in critical:
            print("  %s: %s" % (r["path"], "; ".join(r["gaps"])), file=sys.stderr)

    print("=== RESUMO AUDITORIA CRUZADA ===")
    print("Views analisadas:", summary["views_total"])
    print("Por layout:", summary["by_layout"])
    print("Por componente:", summary["by_component"])
    print("Hosts CRITICAL:", summary["critical_host_count"])
    print("Hosts precisam Tempus:", summary["tempus_hosts_need"])
    print("Hosts Tempus SEM partial:", summary["tempus_hosts_missing_partial"])
    print("Modais com Tempus (lazy):", summary["modal_with_tempus"])
    print("JSON:", out_path)
    print("\n=== TOP GAPS (hosts Tempus) ===")
    for p in tempus_miss[:20]:
        print(" ", p)
    if len(tempus_miss) > 20:
        print(" ... +%d" % (len(tempus_miss) - 20))

    return exit_code


if __name__ == "__main__":
    raise SystemExit(main())
