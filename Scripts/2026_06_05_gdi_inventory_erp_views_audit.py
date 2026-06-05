# -*- coding: utf-8 -*-
"""Inventario read-only: padroes Ajax/DataTables em todas as views do ERP."""
import glob
import os
import re
from collections import defaultdict

ROOT = os.path.dirname(os.path.dirname(__file__))
VIEW_ROOTS = [
    os.path.join(ROOT, "Areas"),
    os.path.join(ROOT, "Views"),
]
SKIP_PARTS = {"obj", "bin", "packages", "node_modules", "LibUI_AdminLTE-4.0.0"}

RX = {
    "error_fn_result": re.compile(r"error:\s*function\s*\(\s*result\s*\)"),
    "errorThrown_only": re.compile(r"GdiAjaxNotifyInconsistencias\(errorThrown"),
    "unified_notify": re.compile(r"typeof result !== 'undefined'"),
    "gdi_ajax_notify": re.compile(r"GdiAjaxNotifyInconsistencias"),
    "has_datatable": re.compile(r"\.DataTable\(|\.dataTable\("),
    "dt_init": re.compile(r"\.[Dd]ata[Tt]able\s*\(\s*\{"),
    "has_xhr_dt": re.compile(r"xhr\.dt"),
    "has_error_dt": re.compile(r"error\.dt"),
    "alert_native": re.compile(r"(?<![\w.])alert\s*\("),
    "yes_filter": re.compile(r'id="yesFilterController"\s+value="([^"]+)"', re.I),
    "complete_sync": re.compile(
        r"complete:\s*function\s*\([^)]*\)\s*\{[^}]{0,250}(?:jsCreateTable|\.DataTable\s*\(|\.draw\s*\()"
    ),
}

COPIED_FILTER_VALUES = {
    "ModalViewNotasFiscais": "copia NF",
    "ModalPedidosLogs": "copia logs",
    "GcIndexFinanceiroLancamentos": "financeiro legado",
    "IndexGed": "copia GED",
    "ModalViewNotasFiscais": "copia NF",
}


def area_key(rel_path):
    p = rel_path.replace("\\", "/")
    if p.startswith("Areas/"):
        parts = p.split("/")
        return parts[1] if len(parts) > 1 else "Areas"
    if p.startswith("Views/"):
        return "Views"
    return "outros"


def controller_folder(rel_path):
    p = rel_path.replace("\\", "/")
    parts = p.split("/")
    if p.startswith("Areas/") and len(parts) >= 4 and parts[2] == "Views":
        return parts[3]
    if p.startswith("Views/"):
        return parts[1] if len(parts) > 2 else "Views"
    return "?"


def collect_cshtml():
    paths = []
    for base in VIEW_ROOTS:
        if not os.path.isdir(base):
            continue
        for path in glob.glob(os.path.join(base, "**", "*.cshtml"), recursive=True):
            parts = set(path.replace("\\", "/").split("/"))
            if parts & SKIP_PARTS:
                continue
            paths.append(path)
    return sorted(set(paths))


def yes_filter_suspect(rel, stem, val, folder):
    issues = []
    if val == "ModalViewNotasFiscais" and stem not in ("ModalViewNotasFiscais", "ModalInvoice"):
        issues.append("copia NF")
    if val == "ModalPedidosLogs" and stem not in ("ModalImportacoesLogs",) and "Historico" not in stem:
        issues.append("copia logs")
    if val == "GcIndexFinanceiroLancamentos" and folder not in (
        "FinanceiroLancamentos", "ComexFinanceiro", "Gerencial", "Parametros",
    ):
        issues.append("financeiro legado")
    if val == "IndexGed" and folder not in ("Ged", "GedSGQ", "Treinamentos") and "Ged" not in stem:
        issues.append("copia GED")
    return issues


def main():
    by_area = defaultdict(lambda: defaultdict(lambda: defaultdict(set)))
    totals = defaultdict(int)
    dt_init_no_xhr = []
    dt_init_no_error = []
    yes_issues = []
    error_result_files = []

    paths = collect_cshtml()
    totals["cshtml"] = len(paths)

    for path in paths:
        rel = os.path.relpath(path, ROOT).replace("\\", "/")
        area = area_key(rel)
        ctrl = controller_folder(rel)
        base = os.path.basename(path)
        stem = os.path.splitext(base)[0]

        with open(path, "r", encoding="utf-8", errors="replace") as f:
            content = f.read()

        for key, rx in RX.items():
            if key in ("yes_filter",):
                continue
            if rx.search(content):
                by_area[area][ctrl][key].add(rel)
                totals[key] += 1

        m = RX["yes_filter"].search(content)
        if m:
            val = m.group(1)
            for reason in yes_filter_suspect(rel, stem, val, ctrl):
                yes_issues.append((rel, val, reason))

        if RX["error_fn_result"].search(content):
            error_result_files.append(rel)

        if RX["dt_init"].search(content):
            if not RX["has_xhr_dt"].search(content):
                dt_init_no_xhr.append(rel)
            if not RX["has_error_dt"].search(content):
                dt_init_no_error.append(rel)

    print("=== ERP VIEWS — RESUMO GLOBAL ===")
    print("Total .cshtml:", totals["cshtml"])
    print("error_fn_result (ficheiros):", len(error_result_files))
    print("dt_init sem xhr.dt:", len(dt_init_no_xhr))
    print("dt_init sem error.dt:", len(dt_init_no_error))
    print("unified_notify (ficheiros):", totals.get("unified_notify", 0))
    print("GdiAjaxNotify (ficheiros):", totals.get("gdi_ajax_notify", 0))
    print("alert nativo (ficheiros):", totals.get("alert_native", 0))
    print()

    for area in sorted(by_area.keys()):
        ctrls = by_area[area]
        area_err = sum(len(c.get("error_fn_result", set())) for c in ctrls.values())
        area_dt = sum(len(c.get("has_datatable", set())) for c in ctrls.values())
        area_xhr = sum(len(c.get("has_xhr_dt", set())) for c in ctrls.values())
        area_unif = sum(len(c.get("unified_notify", set())) for c in ctrls.values())
        if not any([area_err, area_dt, area_unif]):
            continue
        print("## Area %s" % area)
        print("  controllers com gaps: %d | error_fn_result: %d | DataTables: %d | xhr.dt: %d | unified: %d" % (
            len([c for c in ctrls if ctrls[c].get("error_fn_result") or ctrls[c].get("has_datatable")]),
            area_err, area_dt, area_xhr, area_unif,
        ))
        for ctrl in sorted(ctrls.keys()):
            d = ctrls[ctrl]
            er = len(d.get("error_fn_result", set()))
            dt = len(d.get("has_datatable", set()))
            xhr = len(d.get("has_xhr_dt", set()))
            un = len(d.get("unified_notify", set()))
            if er or (dt and xhr < dt) or (dt and not un and d.get("gdi_ajax_notify")):
                flags = []
                if er:
                    flags.append("error_fn_result=%d" % er)
                if dt:
                    flags.append("DT=%d xhr=%d" % (dt, xhr))
                if un:
                    flags.append("unified=%d" % un)
                print("    %s: %s" % (ctrl, ", ".join(flags)))
        print()

    if error_result_files:
        print("=== error: function (result) — LISTA ===")
        for rel in sorted(error_result_files):
            print(" ", rel)
        print()

    if dt_init_no_xhr:
        print("=== .DataTable({ init SEM xhr.dt ===")
        for rel in sorted(dt_init_no_xhr):
            print(" ", rel)
        print()

    if yes_issues:
        print("=== yesFilterController suspeitos ===")
        for rel, val, reason in sorted(yes_issues):
            print(" ", rel, "->", val, "(%s)" % reason)


if __name__ == "__main__":
    main()
