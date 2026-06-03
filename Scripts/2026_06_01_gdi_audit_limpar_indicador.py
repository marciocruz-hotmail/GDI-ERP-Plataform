#!/usr/bin/env python3
"""Audit views with btnLimparFiltro vs indicator wiring."""
import os
import re

ROOT = os.path.join(os.path.dirname(__file__), "..", "Areas")

for dirpath, _, files in os.walk(ROOT):
    for f in files:
        if not f.endswith(".cshtml"):
            continue
        p = os.path.join(dirpath, f)
        rel = os.path.relpath(p, os.path.join(ROOT, "..")).replace("\\", "/")
        t = open(p, encoding="utf-8", errors="ignore").read()
        if "btnLimparFiltro" not in t:
            continue
        has_fn = "GdiAtualizarIndicadorFiltro" in t or "jsAtualizarIndicadorFiltro" in t
        has_xhr = "xhr.dt" in t and "yesFilterOnOff" in t
        has_init = bool(re.search(r"jsAtualizarIndicadorFiltro\w*\(\s*['\"]0['\"]\s*\)", t))
        issues = []
        if not has_fn:
            issues.append("sem funcao indicador")
        if not has_xhr:
            issues.append("sem xhr.dt+yesFilterOnOff")
        if not has_init:
            issues.append("sem init('0')")
        if issues:
            print(f"{rel}: {', '.join(issues)}")
