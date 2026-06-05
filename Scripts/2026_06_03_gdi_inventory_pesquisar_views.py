#!/usr/bin/env python3
"""Inventário views ERP com botão Pesquisar — mecanismo consulta/filtro/DataTables."""
import json
import os
import re

ROOT = os.path.join(os.path.dirname(__file__), "..", "Areas")

PATTERNS = {
    "btn_pesquisar": r'id="btnPesquisar"|onclick="jsAjaxPesquisar|>Pesquisar<',
    "btn_limpar": r'btnLimparFiltro|jsLimparFiltro',
    "yes_filter_field": r'id="yesFilterField"',
    "indicador_fn": r'jsAtualizarIndicadorFiltro\w*|GdiAtualizarIndicadorFiltro',
    "indicador_init": r"jsAtualizarIndicadorFiltro\w*\(\s*['\"]0['\"]\s*\)",
    "xhr_yes_filter": r"xhr\.dt.*yesFilterOnOff|yesFilterOnOff.*xhr\.dt",
    "error_dt": r"error\.dt|\.on\('error\.dt'",
    "defer_loading": r"deferLoading\s*:\s*true",
    "b_server_side": r"bServerSide\s*:\s*true",
    "auto_pesquisa_change": r"jsOnChangeFiltro|SuprimirPesquisaAuto|onchange.*Pesquisar",
    "keypress_enter": r"keypressed|keyCode\s*==\s*13",
    "restore_filter": r"RestoreFilterAutoSearch",
    "select2_lookup": r"Get\w+Lookup|data-gdi-lookup-url|select2",
    "datepicker_filtro": r"jsDatepicker|tempus|ig_.*_idx",
}

def extract_first(pat, text, flags=0):
    m = re.search(pat, text, flags)
    return m.group(0) if m else None

def extract_all(pat, text, flags=0):
    return list(set(re.findall(pat, text, flags)))

def analyze_file(path, rel):
    t = open(path, encoding="utf-8", errors="ignore").read()
    # must have explicit Pesquisar button (not just word in title)
    if not re.search(r'btnPesquisar|jsAjaxPesquisar\w*\(|onclick="[^"]*Pesquisar', t, re.I):
        if not re.search(r'>Pesquisar<|title="Pesquisar', t):
            return None
    if 'DataTable(' not in t and '.dataTable(' not in t:
        return None

    dt_ids = extract_all(r"\$\('#(dt\w+)'\)|\$\(\"#(dt\w+)\"\)", t)
    dt_ids = [x for pair in dt_ids for x in pair if x]

    pesquisar_fns = extract_all(r"function\s+(jsAjaxPesquisar\w*)", t)
    limpar_fns = extract_all(r"function\s+(jsLimparFiltro\w*)", t)
    indicador_fns = extract_all(r"function\s+(jsAtualizarIndicadorFiltro\w*)", t)

    ajax_url = extract_all(r"url:\s*['\"]@Url\.Action\(\"(\w+)\"", t)
    ajax_ctrl = extract_all(r'@Url\.Action\("[^"]+",\s*"(\w+)"', t)

    yes_pesquisar = None
    m = re.search(r'jsAjaxPesquisar\w*\([^)]*\)[^{]*\{[^}]*yesFilterField["\']?\)\.value\s*=\s*["\']([^"\']*)["\']', t, re.S)
    if not m:
        m = re.search(r'yesFilterField["\']?\)\.value\s*=\s*["\']([^"\']*)["\'][^;]*;\s*[^}]*DataTable\(\)\.draw', t, re.S)
    if m:
        yes_pesquisar = m.group(1)

    yes_limpar = None
    if limpar_fns:
        fn = limpar_fns[0]
        m = re.search(rf'function\s+{fn}\(\)[^{{]*\{{[^}}]*yesFilterField["\']?\)\.value\s*=\s*["\']([^"\']*)["\']', t, re.S)
        if m:
            yes_limpar = m.group(1)

    flags = {k: bool(re.search(p, t, re.I | re.S)) for k, p in PATTERNS.items()}

    return {
        "view": rel.replace("\\", "/"),
        "datatable_ids": sorted(set(dt_ids)) or ["(não detectado)"],
        "ajax_actions": sorted(set(ajax_url)),
        "controllers": sorted(set(ajax_ctrl)),
        "fn_pesquisar": pesquisar_fns,
        "fn_limpar": limpar_fns,
        "fn_indicador": indicador_fns,
        "yesFilterField_pesquisar": yes_pesquisar,
        "yesFilterField_limpar": yes_limpar,
        **flags,
    }

results = []
for dirpath, _, files in os.walk(ROOT):
    for f in sorted(files):
        if not f.endswith(".cshtml"):
            continue
        p = os.path.join(dirpath, f)
        rel = os.path.relpath(p, os.path.join(ROOT, ".."))
        row = analyze_file(p, rel)
        if row:
            results.append(row)

results.sort(key=lambda r: r["view"])

# summary stats
all_keys = [k for k in PATTERNS if k not in ("btn_pesquisar",)]
common_all = [k for k in all_keys if all(r[k] for r in results)]
common_index_limpar = [r for r in results if r["btn_limpar"]]
common_limpar_keys = [k for k in all_keys if all(r[k] for r in common_index_limpar)] if common_index_limpar else []

out = {
    "total_views": len(results),
    "with_limpar": len(common_index_limpar),
    "common_all_views": common_all,
    "common_among_limpar_views": common_limpar_keys,
    "views": results,
}
out_path = os.path.join(os.path.dirname(__file__), "2026_06_03_pesquisar_inventory.json")
with open(out_path, "w", encoding="utf-8") as fp:
    json.dump(out, fp, indent=2, ensure_ascii=False)
print(f"Wrote {len(results)} views to {out_path}")
