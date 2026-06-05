# -*- coding: utf-8 -*-
"""
G-PERF-M02 — Baseline Network (transferred / finish) pós G-PERF-20.

Modos:
  --static   Estima bytes dos assets do layout por flags (sem IIS; default).
  --live     Playwright + login (requer site a correr e credenciais).

Uso:
  python Scripts/2026_05_22_gdi_perf_m02_network_baseline.py --static
  python Scripts/2026_05_22_gdi_perf_m02_network_baseline.py --live --base-url https://localhost:44388 --user X --password Y

Saída: .cursor/context/2026_05_21_perf-m02-resultado.json
"""
from __future__ import print_function

import argparse
import json
import os
import re
import sys
from datetime import datetime, timezone

ROOT = os.path.normpath(os.path.join(os.path.dirname(__file__), ".."))
OUT_JSON = os.path.join(ROOT, ".cursor", "context", "2026_05_21_perf-m02-resultado.json")

# Espelho GdiPageScriptsFlags
CORE, DT, S2, TG, TM, JS = 1, 2, 4, 32, 8, 16

# Baseline PERF-000.1 (2026-05-20, dev) — checklist-performance-erp.md
BASELINE_PERF_000 = {
    "gc/Movimentos/IndexPedido": {"transferred_kb": 1100, "finish_ms": 1890},
    "gc/Movimentos/CreatePedido": {"transferred_kb": 960, "finish_ms": 2140},
    "g/Financeiro/Index": {"transferred_kb": 1200, "finish_ms": 1840},
}

META = {"transferred_kb_max": 800, "finish_ms_max": 1500}

# Rotas M02 + flags esperadas (resolve estático documentado)
M02_ROUTES = [
    ("gc/Movimentos/IndexPedido", CORE | DT | S2 | TG, "regressao_comercial"),
    ("gc/Movimentos/CreatePedido", CORE | DT | S2 | TG, "regressao_create"),
    ("g/Financeiro/Index", CORE | DT | S2 | TG, "regressao_financeiro"),
    ("gc/RelatoriosRegulamentacao/Index", CORE | TG, "hub_pos_20"),
    ("gc/RelatoriosFinanceiros/Index", CORE | S2 | TG, "hub_select2"),
    ("gc/Parametros/Index", CORE | TG, "hub_lite"),
    ("g/CentrosCustos/Index", CORE | TG | JS, "hub_jstree"),
]

# Assets por bundle (caminhos relativos à raiz do projeto)
BUNDLE_FILES = {
    "core_css": [
        "LibUI_AdminLTE-4.0.0/plugins/overlayscrollbars-2.11.0/overlayscrollbars.min.css",
        "LibUI_AdminLTE-4.0.0/plugins/bootstrap-icons-1.13.1/bootstrap-icons.min.css",
        "LibUI_AdminLTE-4.0.0/css/adminlte.min.css",
        "LibUI_AdminLTE-4.0.0/plugins/fontawesome-free-7.2.0/css/all.min.css",
        "LibUI_AdminLTE-4.0.0/plugins/sweetalert2/sweetalert2.min.css",
        "LibUI_AdminLTE-4.0.0/plugins/sweetalert2/bootstrap-5.min.css",
        "LibUI_AdminLTE-4.0.0/plugins/sweetalert2/gdi-swal2-overrides.css",
        "Content/gdi-section-cards.css",
        "Content/gdi-sidebar-nav.css",
        "LibUI_AdminLTE-4.0.0/plugins/startprime/css/start.css",
        "LibUI_AdminLTE-4.0.0/plugins/bootstrap4-toggle/bootstrap4-toggle.min.css",
    ],
    "core_js": [
        "LibUI_AdminLTE-4.0.0/plugins/jquery-3.6.0/jquery.min.js",
        "LibUI_AdminLTE-4.0.0/plugins/overlayscrollbars-2.11.0/overlayscrollbars.browser.es5.min.js",
        "LibUI_AdminLTE-4.0.0/plugins/sweetalert2/sweetalert2.min.js",
        "LibUI_AdminLTE-4.0.0/plugins/startprime/js/gdi-swal2-dialog-shim.js",
        "LibUI_AdminLTE-4.0.0/plugins/jquery.spin.fgnass-2.3.2/spin.min.js",
        "LibUI_AdminLTE-4.0.0/js/adminlte.min.js",
        "Scripts/jsFileInputChange.js",
        "LibUI_AdminLTE-4.0.0/plugins/startprime/js/start.js",
        "Scripts/sessionInactivity.js",
        "Scripts/gdi-session-handler.js",
        "LibUI_AdminLTE-4.0.0/plugins/bootstrap4-toggle/bootstrap4-toggle.min.js",
    ],
    "dataTables": [
        "LibUI_AdminLTE-4.0.0/plugins/datatablesnet-bs5-2.3.2/datatables.min.css",
        "Content/gdi-datatables-bs5.css",
        "LibUI_AdminLTE-4.0.0/plugins/datatablesnet-bs5-2.3.2/datatables.min.js",
    ],
    "select2": [
        "LibUI_AdminLTE-4.0.0/plugins/select2/select2.min.css",
        "LibUI_AdminLTE-4.0.0/plugins/select2/select2-bootstrap-5-theme.min.css",
        "LibUI_AdminLTE-4.0.0/plugins/select2/select2.min.js",
        "LibUI_AdminLTE-4.0.0/plugins/select2/i18n/pt-BR.js",
        "LibUI_AdminLTE-4.0.0/plugins/startprime/js/gdi-select2.js",
    ],
    "jstree": [
        "LibUI_AdminLTE-4.0.0/plugins/jstree-3.3.4/dist/themes/default/style.css",
        "LibUI_AdminLTE-4.0.0/plugins/jstree-3.3.4/dist/jstree.min.js",
    ],
    "tempus": [
        "LibUI_AdminLTE-4.0.0/plugins/tempus-dominus-6.9.4/css/tempus-dominus.min.css",
        "LibUI_AdminLTE-4.0.0/plugins/tempus-dominus-6.9.4/js/popper.min.js",
        "LibUI_AdminLTE-4.0.0/plugins/tempus-dominus-6.9.4/js/tempus-dominus.min.js",
        "LibUI_AdminLTE-4.0.0/plugins/tempus-dominus-6.9.4/js/jQuery-provider.min.js",
    ],
}

# Views host com partial Tempus (não está no layout global)
TEMPUS_HOST_VIEWS = {
    "gc/RelatoriosRegulamentacao/Index",
    "gc/RelatoriosComerciais/Index",
    "gc/RelatoriosFinanceiros/Index",
}


def file_size(path):
    full = os.path.join(ROOT, path.replace("/", os.sep))
    if not os.path.isfile(full):
        return 0, "missing:" + path
    return os.path.getsize(full), None


def sum_bundle(keys):
    total = 0
    missing = []
    for key in keys:
        for rel in BUNDLE_FILES.get(key, []):
            n, err = file_size(rel)
            if err:
                missing.append(err)
            total += n
    return total, missing


def flags_to_bundles(flags):
    keys = ["core_css", "core_js"]
    if flags & DT:
        keys.append("dataTables")
    if flags & S2:
        keys.append("select2")
    if flags & JS:
        keys.append("jstree")
    if flags & TG:
        pass  # toggle já em core_js/core_css
    return keys


def run_static():
    rows = []
    for route, flags, note in M02_ROUTES:
        keys = flags_to_bundles(flags)
        raw, missing = sum_bundle(keys)
        tempus_extra = 0
        if route in TEMPUS_HOST_VIEWS:
            tempus_extra, _ = sum_bundle(["tempus"])
        total_raw = raw + tempus_extra
        # Heurística gzip ~35% do bruto em .min.js/.css (DevTools transferred)
        est_transferred_kb = int(round(total_raw * 0.35 / 1024))
        baseline = BASELINE_PERF_000.get(route)
        delta_kb = None
        if baseline:
            delta_kb = est_transferred_kb - baseline["transferred_kb"]
        rows.append({
            "route": route,
            "flags": flags,
            "data_gdi_page_scripts": flags,
            "note": note,
            "assets_raw_kb": int(round(total_raw / 1024)),
            "estimated_transferred_kb": est_transferred_kb,
            "baseline_perf000_transferred_kb": baseline["transferred_kb"] if baseline else None,
            "delta_vs_perf000_kb": delta_kb,
            "meta_transferred_ok": est_transferred_kb < META["transferred_kb_max"],
            "tempus_partial_kb": int(round(tempus_extra / 1024)),
            "missing_assets": missing[:5],
        })
    return {
        "mode": "static",
        "utc": datetime.now(timezone.utc).isoformat(),
        "versionerp": _read_version(),
        "meta": META,
        "disclaimer": (
            "Estimativa a partir do tamanho em disco dos assets do layout × 0,35 (proxy gzip). "
            "Não inclui HTML, navbar Ajax, imagens nem 1º GetDados. Finish/DCL exigem --live."
        ),
        "routes": rows,
    }


def _read_version():
    path = os.path.join(ROOT, "ControlVersion.cs")
    try:
        text = open(path, encoding="utf-8").read()
        m = re.search(r'getShortVersion\(\)[\s\S]*?return\s+"([^"]+)"', text)
        return m.group(1) if m else "?"
    except IOError:
        return "?"


def run_live(base_url, user, password, headless=True):
    try:
        from playwright.sync_api import sync_playwright
    except ImportError:
        print("FAIL: pip install playwright && playwright install chromium", file=sys.stderr)
        return None

    base = base_url.rstrip("/")
    results = []

    with sync_playwright() as p:
        browser = p.chromium.launch(headless=headless)
        context = browser.new_context(ignore_https_errors=True)
        page = context.new_page()

        login_url = base + "/UserIdentity/Index"
        page.goto(login_url, wait_until="networkidle", timeout=120000)
        page.fill("#userIdentity_Acesso", user)
        page.fill("#userIdentity_Password", password)
        with page.expect_navigation(timeout=120000):
            page.evaluate("document.getElementById('UserIdentity').submit()")

        for route, flags, note in M02_ROUTES:
            url = base + "/" + route.replace("\\", "/")
            page.goto(url, wait_until="networkidle", timeout=120000)
            metrics = page.evaluate(
                """() => {
                const nav = performance.getEntriesByType('navigation')[0];
                const res = performance.getEntriesByType('resource');
                let transferred = 0;
                for (const e of res) {
                    if (e.transferSize > 0) transferred += e.transferSize;
                }
                if (nav && nav.transferSize > 0) transferred += nav.transferSize;
                const body = document.body;
                const attr = body ? body.getAttribute('data-gdi-page-scripts') : null;
                return {
                    transferred,
                    finish: nav ? nav.loadEventEnd : 0,
                    dcl: nav ? nav.domContentLoadedEventEnd : 0,
                    requests: res.length + 1,
                    data_gdi_page_scripts: attr
                };
            }"""
            )
            t_kb = int(round(metrics["transferred"] / 1024))
            finish_ms = int(round(metrics["finish"]))
            results.append({
                "route": route,
                "flags_expected": flags,
                "note": note,
                "transferred_kb": t_kb,
                "finish_ms": finish_ms,
                "dom_content_loaded_ms": int(round(metrics["dcl"])),
                "requests": metrics["requests"],
                "data_gdi_page_scripts": metrics["data_gdi_page_scripts"],
                "meta_transferred_ok": t_kb < META["transferred_kb_max"],
                "meta_finish_ok": finish_ms < META["finish_ms_max"],
            })

        browser.close()

    return {
        "mode": "live",
        "utc": datetime.now(timezone.utc).isoformat(),
        "base_url": base_url,
        "versionerp": _read_version(),
        "meta": META,
        "routes": results,
    }


def print_report(data):
    print("=== G-PERF-M02 (%s) VersionERP %s ===" % (data["mode"], data.get("versionerp", "?")))
    if data.get("disclaimer"):
        print(data["disclaimer"])
    print("Meta: transferred < %s KB | finish < %s ms\n" % (META["transferred_kb_max"], META["finish_ms_max"]))
    for r in data["routes"]:
        if data["mode"] == "static":
            line = "%-42s est.%4d KB (raw assets %4d KB) scripts=%s meta_OK=%s" % (
                r["route"],
                r["estimated_transferred_kb"],
                r["assets_raw_kb"],
                r["data_gdi_page_scripts"],
                r["meta_transferred_ok"],
            )
            if r.get("delta_vs_perf000_kb") is not None:
                line += " delta_vs_PERF000=%+d KB" % r["delta_vs_perf000_kb"]
        else:
            line = "%-42s %4d KB  finish %5d ms  scripts=%s  OK_t=%s OK_f=%s" % (
                r["route"],
                r["transferred_kb"],
                r["finish_ms"],
                r["data_gdi_page_scripts"],
                r["meta_transferred_ok"],
                r["meta_finish_ok"],
            )
        print(line)
    print("\nJSON: " + OUT_JSON)


def main():
    ap = argparse.ArgumentParser(description="G-PERF-M02 network baseline")
    ap.add_argument("--static", action="store_true", help="Estimativa local (default)")
    ap.add_argument("--live", action="store_true", help="Playwright contra site autenticado")
    ap.add_argument("--base-url", default=os.environ.get("GDI_M02_BASE_URL", "https://localhost:44388"))
    ap.add_argument("--user", default=os.environ.get("GDI_M02_USER", ""))
    ap.add_argument("--password", default=os.environ.get("GDI_M02_PASSWORD", ""))
    ap.add_argument("--headed", action="store_true", help="Browser visível (--live)")
    args = ap.parse_args()

    if args.live:
        if not args.user or not args.password:
            print("FAIL --live requer --user e --password (ou GDI_M02_USER / GDI_M02_PASSWORD)", file=sys.stderr)
            return 2
        data = run_live(args.base_url, args.user, args.password, headless=not args.headed)
        if data is None:
            return 1
    else:
        data = run_static()

    os.makedirs(os.path.dirname(OUT_JSON), exist_ok=True)
    with open(OUT_JSON, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2, ensure_ascii=False)

    print_report(data)
    return 0


if __name__ == "__main__":
    sys.exit(main() or 0)
