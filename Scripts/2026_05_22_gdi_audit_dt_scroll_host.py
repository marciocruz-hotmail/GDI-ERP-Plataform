# -*- coding: utf-8 -*-
"""Auditoria: DataTables vs padrao gdi-dt-scroll-host + scroll-body-horizontal."""
import re
from pathlib import Path

BASE = Path(__file__).resolve().parent.parent
SCAN = ["Areas/g", "Areas/gc", "Areas/qa", "Areas/a", "Areas/crm", "Views"]

RX_DT_INIT = re.compile(r"\.DataTable\s*\(|\.dataTable\s*\(")
RX_DISPLAY = re.compile(r'class="[^"]*\bdisplay\b')
RX_ID_DT = re.compile(r'id="dt\w+"', re.I)


def classify(path: Path, text: str) -> dict:
    rel = str(path.relative_to(BASE)).replace("\\", "/")
    n_init = len(RX_DT_INIT.findall(text))
    n_display = len(RX_DISPLAY.findall(text))
    n_id_dt = len(RX_ID_DT.findall(text))
    has_scroll = "scroll-body-horizontal" in text
    has_host = "gdi-dt-scroll-host" in text
    has_form = "gdi-form-table-scroll" in text
    is_modal = "Modal" in path.name or "/Modal" in rel
    is_index = path.name == "Index.cshtml" or "/Index" in rel
    is_create = "CreateEdit" in path.name or "FormPedido" in path.name or "FormInventario" in path.name
    kind = "modal" if is_modal else ("index" if is_index else ("form_tab" if is_create else "other"))
    if n_init == 0 and n_display == 0 and n_id_dt == 0:
        return None
    status = "OK_HOST"
    if has_host:
        status = "OK_HOST"
    elif has_form:
        status = "MVC_FORM"  # not DataTables sizing pattern
    elif has_scroll:
        status = "GAP_SCROLL_ONLY"
    else:
        status = "GAP_NO_SCROLL"
    return {
        "path": rel,
        "n_init": n_init,
        "n_display": n_display,
        "has_scroll": has_scroll,
        "has_host": has_host,
        "has_form": has_form,
        "kind": kind,
        "status": status,
    }


def main():
    rows = []
    for area in SCAN:
        p = BASE / area
        if not p.exists():
            continue
        for f in sorted(p.rglob("*.cshtml")):
            try:
                text = f.read_text(encoding="utf-8", errors="replace")
            except OSError:
                continue
            r = classify(f, text)
            if r:
                rows.append(r)

    by_status = {}
    for r in rows:
        by_status.setdefault(r["status"], []).append(r)

    print("=== AUDITORIA DataTables / scroll-host ===\n")
    print(f"Ficheiros .cshtml com DataTable ou class display: {len(rows)}\n")

    for key in ["OK_HOST", "GAP_SCROLL_ONLY", "GAP_NO_SCROLL", "MVC_FORM"]:
        lst = sorted(by_status.get(key, []), key=lambda x: x["path"])
        print(f"--- {key}: {len(lst)} ---")
        for r in lst:
            extra = f" inits={r['n_init']} display={r['n_display']} kind={r['kind']}"
            print(f"  {r['path']}{extra}")
        print()

    gap = by_status.get("GAP_SCROLL_ONLY", []) + by_status.get("GAP_NO_SCROLL", [])
    indexes_gap = [r for r in gap if r["kind"] == "index"]
    modals_gap = [r for r in gap if r["kind"] == "modal"]
    forms_gap = [r for r in gap if r["kind"] == "form_tab"]
    print(f"RESUMO GAPS (sem gdi-dt-scroll-host): {len(gap)}")
    print(f"  Index/listagens: {len(indexes_gap)}")
    print(f"  Modais: {len(modals_gap)}")
    print(f"  CreateEdit/Form/tabs: {len(forms_gap)}")
    print(f"  Outros: {len(gap) - len(indexes_gap) - len(modals_gap) - len(forms_gap)}")


RX_FIRST_TABLE = re.compile(
    r'id="(dt[^"]+)"[^>]*>.*?<thead>.*?<tr>(.*?)</tr>',
    re.IGNORECASE | re.DOTALL,
)


def narrow_scroll_gaps():
    """Tabelas com poucas colunas + scroll-body-horizontal sem host (candidatas a migrar)."""
    out = []
    for area in SCAN:
        p = BASE / area
        if not p.exists():
            continue
        for f in sorted(p.rglob("*.cshtml")):
            text = f.read_text(encoding="utf-8", errors="replace")
            if "scroll-body-horizontal" not in text or "gdi-dt-scroll-host" in text:
                continue
            if not RX_DT_INIT.search(text) and "display" not in text:
                continue
            m = RX_FIRST_TABLE.search(text)
            if not m:
                continue
            n_th = len(re.findall(r"<th", m.group(2), re.IGNORECASE))
            if n_th and n_th <= 10:
                rel = str(f.relative_to(BASE)).replace("\\", "/")
                out.append((rel, n_th, m.group(1)))
    return sorted(out, key=lambda x: (x[1], x[0]))


if __name__ == "__main__":
    main()
    narrow = narrow_scroll_gaps()
    print(f"\n--- CANDIDATAS (<=10 colunas, scroll SEM host): {len(narrow)} ---")
    for rel, n, tid in narrow:
        print(f"  {n:2} cols  {tid:32}  {rel}")
