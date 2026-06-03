#!/usr/bin/env python3
"""Auditoria indicador Limpar (cinza=0 / amarelo=1) — views com Pesquisar + Limpar."""
import json
import os
import re

ROOT = os.path.join(os.path.dirname(__file__), "..", "Areas")

def has_pesquisar_btn(t):
    return bool(re.search(r'id="btnPesquisar|btnPesquisar\w*"|>Pesquisar<|jsAjaxPesquisar', t, re.I))

def analyze(path, rel):
    t = open(path, encoding="utf-8", errors="ignore").read()
    if not has_pesquisar_btn(t):
        return None
    if "btnLimparFiltro" not in t and "jsLimparFiltro" not in t:
        return None

    init_m = re.search(r"jsAtualizarIndicadorFiltro\w*\(\s*['\"]([01])['\"]\s*\)", t)
    init_val = init_m.group(1) if init_m else None
    has_init = init_m is not None
    has_fn = bool(re.search(r"function\s+jsAtualizarIndicadorFiltro\w*", t))
    has_gdi = "GdiAtualizarIndicadorFiltro" in t
    has_xhr = bool(re.search(r"xhr\.dt", t) and "yesFilterOnOff" in t)

    # yesFilterField no init (carga todos cadastros)
    init_star = "RestoreFilterAutoSearch != true" in t and 'yesFilterField").value = "*"' in t

    # deferLoading
    defer = "deferLoading: true" in t or "deferLoading: true" in t

    # Pesquisar / Limpar valores
    pesquisar_fn = re.findall(r"function\s+(jsAjaxPesquisar\w*|jsLimparFiltro\w*)", t)
    yes_p = yes_l = None
    for fn in re.findall(r"function\s+(jsAjaxPesquisar\w*)", t):
        m = re.search(rf"function\s+{fn}\(\)[^{{]*\{{[^}}]*yesFilterField[^=]*=\s*['\"]([^'\"]*)['\"]", t, re.S)
        if m:
            yes_p = m.group(1)
            break
    for fn in re.findall(r"function\s+(jsLimparFiltro\w*)", t):
        m = re.search(rf"function\s+{fn}\(\)[^{{]*\{{[^}}]*yesFilterField[^=]*=\s*['\"]([^'\"]*)['\"]", t, re.S)
        if m:
            yes_l = m.group(1)
            break

    padrao = "?"
    if yes_p == "" and yes_l == "*":
        padrao = "A_cadastro"
    elif yes_p == "*" and yes_l == "":
        padrao = "B_operacional"
    elif yes_p == "*" and yes_l == "*":
        padrao = "C_ambos_star"
    elif yes_p or yes_l:
        padrao = f"misto_p={yes_p}_l={yes_l}"

    issues = []
    if not has_fn and not has_gdi:
        issues.append("SEM_FUNCAO_INDICADOR")
    if not has_xhr:
        issues.append("SEM_XHR_YESFILTERONOFF")
    if not has_init:
        issues.append("SEM_INIT_INDICADOR")
    elif init_val != "0":
        issues.append(f"INIT_NAO_CINZA(val={init_val})")
    if padrao == "C_ambos_star":
        issues.append("PADRAO_C_AMBIGUO")
    if init_star and defer:
        issues.append("INIT_STAR_MAS_DEFER_LOADING")

    return {
        "view": rel.replace("\\", "/"),
        "padrao": padrao,
        "init": init_val,
        "init_star_abrir": init_star,
        "defer_loading": defer,
        "yes_pesquisar": yes_p,
        "yes_limpar": yes_l,
        "issues": issues,
    }

rows = []
for dirpath, _, files in os.walk(ROOT):
    for f in files:
        if not f.endswith(".cshtml"):
            continue
        p = os.path.join(dirpath, f)
        rel = os.path.relpath(p, os.path.join(ROOT, ".."))
        r = analyze(p, rel)
        if r:
            rows.append(r)

rows.sort(key=lambda x: x["view"])
with_issues = [r for r in rows if r["issues"]]
ok = [r for r in rows if not r["issues"]]

print(f"Total Pesquisar+Limpar: {len(rows)}")
print(f"OK: {len(ok)} | Com lacunas: {len(with_issues)}\n")

for r in with_issues:
    print(f"{r['view']}")
    print(f"  padrao={r['padrao']} init={r['init']} defer={r['defer_loading']} init*={r['init_star_abrir']}")
    print(f"  -> {', '.join(r['issues'])}\n")

out = os.path.join(os.path.dirname(__file__), "2026_06_01_indicador_limpar_audit.json")
with open(out, "w", encoding="utf-8") as fp:
    json.dump({"total": len(rows), "ok": len(ok), "with_issues": with_issues, "all": rows}, fp, indent=2, ensure_ascii=False)
print(f"JSON: {out}")
