# -*- coding: utf-8 -*-
"""
G-PERF-20 Fase 3 — Verifica rotas hub vs flags esperadas (espelha GdiPageScriptsDefaults + atributos 20e).

Uso: python Scripts/2026_05_20_gdi_verify_page_scripts_resolve.py
Exit 0 se OK; 1 se divergência documentada.
"""
from __future__ import print_function
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from importlib.machinery import SourceFileLoader

_lite_mod = SourceFileLoader(
    "layout_lite",
    os.path.join(os.path.dirname(__file__), "2026_05_20_gdi_layout_lite_actions.py"),
).load_module()
LAYOUT_LITE_ACTIONS = _lite_mod.LAYOUT_LITE_ACTIONS

# Espelho C# GdiPageScriptsFlags (valores)
CORE, DT, S2, TG, JS = 1, 2, 4, 32, 16
LAYOUT_HUB_REPORT = CORE | TG
LAYOUT_HUB_REPORT_S2 = CORE | TG | S2
LAYOUT_HUB_JSTREE = CORE | TG | JS
LAYOUT_LITE = CORE | TG
DEFAULT_GC_G_AREA = CORE | DT | S2 | TG

NO_DT = {
    "CentrosCustos", "ClassificacaoFinanceira", "Parametros",
    "RelatoriosCadastrais", "RelatoriosComerciais",
    "RelatoriosFinanceiros", "RelatoriosRegulamentacao",
    "Treinamentos",
}
JSTREE = {"CentrosCustos", "ClassificacaoFinanceira"}

# action Index com atributo explícito (G-PERF-20e)
LAYOUT_PORTAL = CORE | TG

ATTR_INDEX = {
    ("gc", "RelatoriosComerciais"): LAYOUT_HUB_REPORT,
    ("gc", "RelatoriosRegulamentacao"): LAYOUT_HUB_REPORT,
    ("gc", "RelatoriosCadastrais"): LAYOUT_HUB_REPORT,
    ("gc", "RelatoriosFinanceiros"): LAYOUT_HUB_REPORT_S2,
    ("gc", "Parametros"): LAYOUT_LITE,
    ("g", "CentrosCustos"): LAYOUT_HUB_JSTREE,
    ("g", "ClassificacaoFinanceira"): LAYOUT_HUB_JSTREE,
    ("qa", "Treinamentos"): LAYOUT_LITE,
    ("crm", "Pedidos"): LAYOUT_PORTAL,
}


def resolve(area, controller, action="Index"):
    key = (area, controller)
    if action == "Index" and key in ATTR_INDEX:
        return ATTR_INDEX[key]
    lite = LAYOUT_LITE_ACTIONS.get(controller)
    if lite and action in lite:
        return LAYOUT_LITE
    flags = CORE
    if not area:
        return flags | TG
    if area in ("g", "gc", "qa"):
        flags |= TG
        if controller not in NO_DT:
            flags |= DT | S2
    elif area == "crm":
        flags |= TG  # default; Pedidos Index usa atributo LayoutPortalCliente
    if controller in JSTREE:
        flags |= JS
    return flags


def flag_str(f):
    parts = []
    if f & CORE:
        parts.append("Core")
    if f & DT:
        parts.append("DataTables")
    if f & S2:
        parts.append("Select2")
    if f & TG:
        parts.append("Toggle")
    if f & JS:
        parts.append("Jstree")
    return ",".join(parts) if parts else "None"


def main():
    cases = [
        ("gc", "RelatoriosRegulamentacao", "Index", LAYOUT_HUB_REPORT),
        ("gc", "RelatoriosFinanceiros", "Index", LAYOUT_HUB_REPORT_S2),
        ("g", "CentrosCustos", "Index", LAYOUT_HUB_JSTREE),
        ("gc", "Movimentos", "IndexPedido", DEFAULT_GC_G_AREA),
        ("gc", "Parametros", "Index", LAYOUT_LITE),
        ("g", "Produtos", "Index", DEFAULT_GC_G_AREA),
        ("qa", "Treinamentos", "Index", LAYOUT_LITE),
        ("crm", "Pedidos", "Index", LAYOUT_PORTAL),
        ("qa", "GedSGQ", "IndexPops", LAYOUT_LITE),
        ("g", "CentrosCustos", "Create", LAYOUT_LITE),
        ("g", "Filiais", "Edit", LAYOUT_LITE),
        ("gc", "Cfop", "Create", LAYOUT_LITE),
        ("gc", "MovimentosEntradas", "FormProcessarNFImportacao", LAYOUT_LITE),
    ]
    ok = True
    for area, ctrl, action, expected in cases:
        got = resolve(area, ctrl, action)
        if got != expected:
            print("FAIL %s/%s/%s expected %s got %s" % (area, ctrl, action, flag_str(expected), flag_str(got)))
            ok = False
        else:
            print("OK   %s/%s/%s -> %s" % (area, ctrl, action, flag_str(got)))
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
