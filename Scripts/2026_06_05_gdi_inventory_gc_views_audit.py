# -*- coding: utf-8 -*-
"""Inventario read-only: padroes Ajax/DataTables nas views Areas/gc/Views."""
import glob
import os
import re
from collections import defaultdict

BASE = os.path.join(os.path.dirname(os.path.dirname(__file__)), "Areas", "gc", "Views")

RX = {
    "error_fn_result": re.compile(r"error:\s*function\s*\(\s*result\s*\)"),
    "errorThrown_notify": re.compile(r"GdiAjaxNotifyInconsistencias\(errorThrown"),
    "unified_notify": re.compile(r"typeof result !== 'undefined'"),
    "has_datatable": re.compile(r"\.DataTable\(|\.dataTable\("),
    "has_xhr_dt": re.compile(r"xhr\.dt"),
    "has_error_dt": re.compile(r"error\.dt"),
    "has_gdi_json_err": re.compile(r"GdiDtNotifyJsonErrorMessage"),
    "has_gdi_load_fail": re.compile(r"GdiDtNotifyLoadFailure"),
    "complete_then_dt": re.compile(
        r"complete:\s*function\s*\([^)]*\)\s*\{[^}]{0,200}(?:jsCreateTable|DataTable|\.draw)"
    ),
    "yes_filter": re.compile(
        r'id="yesFilterController"\s+value="([^"]+)"', re.I
    ),
}


def module_of(rel):
    return rel.replace("\\", "/").split("/")[0]


def main():
    by_mod = defaultdict(lambda: defaultdict(set))
    dt_no_xhr = []
    dt_no_error_dt = []
    yes_filter_issues = []
    files_total = 0

    for path in sorted(glob.glob(os.path.join(BASE, "**", "*.cshtml"), recursive=True)):
        rel = os.path.relpath(path, BASE)
        mod = module_of(rel)
        files_total += 1
        with open(path, "r", encoding="utf-8", errors="replace") as f:
            content = f.read()
        base = os.path.basename(path)

        for key, rx in RX.items():
            if key == "yes_filter":
                m = rx.search(content)
                if m:
                    val = m.group(1)
                    stem = os.path.splitext(base)[0]
                    folder = mod
                    # suspeito se copiou ModalViewNotasFiscais ou ModalPedidosLogs fora de contexto
                    if val in ("ModalViewNotasFiscais", "ModalPedidosLogs", "GcIndexFinanceiroLancamentos"):
                        if val == "ModalViewNotasFiscais" and stem not in (
                            "ModalViewNotasFiscais",
                            "ModalInvoice",
                        ):
                            yes_filter_issues.append((rel, val, "copia NF"))
                        if val == "ModalPedidosLogs" and stem != "ModalImportacoesLogs" and "Historico" not in stem:
                            yes_filter_issues.append((rel, val, "copia logs"))
                    if val == "GcIndexFinanceiroLancamentos" and folder not in (
                        "FinanceiroLancamentos",
                        "ComexFinanceiro",
                        "Gerencial",
                        "Parametros",
                    ):
                        yes_filter_issues.append((rel, val, "financeiro legado"))
            elif rx.search(content):
                by_mod[mod][key].add(base)

        if RX["has_datatable"].search(content):
            if not RX["has_xhr_dt"].search(content):
                dt_no_xhr.append(rel)
            if not RX["has_error_dt"].search(content):
                dt_no_error_dt.append(rel)

    print("=== RESUMO gc/Views ===")
    print("Total .cshtml:", files_total)
    print("Modulos:", len(by_mod))
    print()

    for mod in sorted(by_mod.keys()):
        if mod == "Movimentos":
            continue
        d = by_mod[mod]
        err_res = len(d.get("error_fn_result", set()))
        dt = len(d.get("has_datatable", set()))
        xhr = len(d.get("has_xhr_dt", set()))
        if not any([err_res, dt, len(d.get("errorThrown_notify", set()))]):
            continue
        print("## %s" % mod)
        if err_res:
            print("  error_fn_result (%d): %s" % (err_res, ", ".join(sorted(d["error_fn_result"])[:12])))
        if dt:
            print("  DataTables (%d), xhr.dt (%d), error.dt (%d)" % (
                dt, len(d.get("has_xhr_dt", set())), len(d.get("has_error_dt", set()))))
        unif = len(d.get("unified_notify", set()))
        if unif:
            print("  unified result.msg (%d)" % unif)
        print()

    print("=== DataTables SEM xhr.dt (fora Movimentos) ===")
    for rel in sorted(dt_no_xhr):
        if module_of(rel) != "Movimentos":
            print(" ", rel.replace("\\", "/"))
    print("Count:", sum(1 for r in dt_no_xhr if module_of(r) != "Movimentos"))
    print()

    print("=== DataTables SEM error.dt (fora Movimentos) ===")
    for rel in sorted(dt_no_error_dt):
        if module_of(rel) != "Movimentos":
            print(" ", rel.replace("\\", "/"))
    print("Count:", sum(1 for r in dt_no_error_dt if module_of(r) != "Movimentos"))
    print()

    print("=== yesFilterController suspeitos ===")
    for item in yes_filter_issues:
        print(" ", item[0].replace("\\", "/"), "->", item[1], "(%s)" % item[2])


if __name__ == "__main__":
    main()
