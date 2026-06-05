#!/usr/bin/env python3
"""Inventario: LibExceptions residual em Areas/*/Controllers (+ Services).

Classifica ocorrencias fora de JsonAjaxErro* / GdiMvcJsonResults por contrato especial ou pendencia documentada.

Uso:
  python Scripts/2026_06_05_gdi_inventory_erp_ajax_exceptions_residual.py
  python Scripts/2026_06_05_gdi_inventory_erp_ajax_exceptions_residual.py --areas gc,g,qa
  python Scripts/2026_06_05_gdi_inventory_erp_ajax_exceptions_residual.py --areas all

Exit 0 = todas classificadas; exit 1 = unclassified ou lookup_ajax com LibExceptions direto.
"""
from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
AREAS_ROOT = ROOT / "Areas"

LIBEX_RE = re.compile(
    r"LibExceptions\.(getDbEntityValidationException|getExceptionShortMessage|getWebException)\s*\("
)

# Ordem importa: regras mais especificas primeiro.
RULES: list[tuple[str, str, str]] = [
    ("Clipboard.SetText", "upload_clipboard", "upload xlsx + Clipboard no catch"),
    ("File.Delete", "upload_cleanup", "upload com apagar ficheiro temp no catch"),
    ("ListaIdsFinanceirosGerados", "cleanup_financeiro", "AjaxModalGerarFinanceiroMovimentos rollback"),
    ('msgRetorno += "Mensagem: "', "cleanup_financeiro", "AjaxModalGerarFinanceiroMovimentos rollback"),
    ("ErroArquivoXlxs", "upload_parse_loop", "parse celula/linha planilha xlsx"),
    ("HttpPostedFileBase", "upload_xlsx", "upload multipart xlsx"),
    ("Processado = false", "upload_xlsx", "upload/processamento xlsx"),
    ("LibFlashMessage.SetModalMessage(this, LibExceptions", "modal_flash", "flash message + redirect MVC"),
    ("LibFlashMessage.SetModalMessage", "modal_get", "modal GET - redirect ModalError"),
    ("RedirectToAction(\"ModalError\"", "modal_get", "modal GET - redirect ModalError"),
    ("_msgProcessamento.Append(LibExceptions", "service_msg_append", "service acumula msg (nao Ajax JSON)"),
    ("ModelState.AddModelError", "mvc_modelstate", "post MVC server-side (nao Ajax JSON)"),
    ("return Json(new { success = false, msg = LibExceptions", "ajax_json_return_inline", "Ajax return Json inline (candidato JsonAjaxErro)"),
    ("getWebException", "webexception", "WebException dedicada"),
    ("RetornoProcessamento.MsgProcessamento", "webexception_flow", "fluxo robô e-Notas"),
    ("MsgGeral +=", "loop_partial", "erro acumulado em loop"),
    ("LogAlteracoes +=", "loop_partial", "erro parcial em notificacao"),
    ("MsgRetorno += LibExceptions", "ged_partial", "GED erro acumulado em upload"),
    ("idProcessamento", "id_processamento", "export/lote com idProcessamento"),
    ("AjaxProcessarProdutosPre", "id_processamento", "processamento lote com idProcessamento"),
    ("JsonLookupError", "lookup_ajax", "typeahead LookupAjax"),
    ("AjaxFailureMessage", "lookup_ajax", "typeahead LookupAjax (alinhado Lib)"),
    ("errorMessage = LibExceptions", "datatables_inline", "DataTables catch inline (candidato DataTableError)"),
    ("return LibExceptions.getExceptionShortMessage", "helper_string_return", "helper devolve string de erro"),
    ("InnerException != null", "ajax_robo_enotas", "NFe/e-Notas com detalhe InnerException"),
    ("msgRetorno = LibExceptions", "ajax_json_catch_inline", "Ajax catch inline msgRetorno (candidato JsonAjaxErro)"),
    ("MsgRetorno = LibExceptions", "ajax_json_catch_inline", "Ajax catch inline MsgRetorno (candidato JsonAjaxErro)"),
    ("sucesso = false", "ajax_json_catch_inline", "Ajax catch inline (candidato JsonAjaxErro)"),
]

DEFAULT_AREAS = ("a", "crm", "g", "gc", "qa")


def classify(context: str) -> tuple[str, str]:
    for needle, cat, desc in RULES:
        if needle in context:
            return cat, desc
    return "unclassified", "revisar manualmente"


def _guess_method(lines: list[str], line_no: int) -> str:
    for i in range(line_no - 1, max(0, line_no - 80), -1):
        m = re.search(
            r"public\s+(?:async\s+)?(?:ActionResult|JsonResult|bool|string)\s+(\w+)",
            lines[i],
        )
        if m:
            return m.group(1)
    return "?"


def scan_file(path: Path, area: str) -> list[dict]:
    text = path.read_text(encoding="utf-8", errors="replace")
    lines = text.splitlines()
    rel = path.relative_to(ROOT).as_posix()
    hits: list[dict] = []
    for m in LIBEX_RE.finditer(text):
        line_no = text[: m.start()].count("\n") + 1
        start = max(0, line_no - 25)
        end = min(len(lines), line_no + 25)
        context = "\n".join(lines[start:end])
        cat, desc = classify(context)
        hits.append(
            {
                "area": area,
                "file": path.name,
                "path": rel,
                "line": line_no,
                "method": _guess_method(lines, line_no),
                "category": cat,
                "desc": desc,
                "snippet": lines[line_no - 1].strip() if line_no <= len(lines) else "",
            }
        )
    return hits


def collect_hits(areas: tuple[str, ...]) -> list[dict]:
    all_hits: list[dict] = []
    for area in areas:
        base = AREAS_ROOT / area
        if not base.is_dir():
            print(f"AVISO: area ausente: {area}", file=sys.stderr)
            continue
        seen: set[str] = set()
        for sub in ("Controllers", "Services"):
            folder = base / sub
            if not folder.is_dir():
                continue
            for path in sorted(folder.rglob("*.cs")):
                key = str(path)
                if key in seen:
                    continue
                seen.add(key)
                all_hits.extend(scan_file(path, area))
    return all_hits


def parse_areas(raw: str) -> tuple[str, ...]:
    if raw.strip().lower() in ("all", "*"):
        return DEFAULT_AREAS
    return tuple(a.strip() for a in raw.split(",") if a.strip())


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Inventario LibExceptions residual ERP")
    parser.add_argument(
        "--areas",
        default="all",
        help="Lista separada por virgula (gc,g,qa,a,crm) ou 'all' (padrao)",
    )
    args = parser.parse_args(argv)
    areas = parse_areas(args.areas)

    all_hits = collect_hits(areas)
    if not all_hits:
        print(f"OK - nenhuma ocorrencia LibExceptions em Areas {areas}.")
        return 0

    by_area: dict[str, list[dict]] = {}
    for h in all_hits:
        by_area.setdefault(h["area"], []).append(h)

    unclassified: list[dict] = []
    lookup_stale: list[dict] = []

    print(f"LibExceptions residual — Areas {','.join(areas)}: {len(all_hits)} ocorrencia(s)\n")

    for area in sorted(by_area):
        hits = by_area[area]
        by_cat: dict[str, list[dict]] = {}
        for h in hits:
            by_cat.setdefault(h["category"], []).append(h)
            if h["category"] == "unclassified":
                unclassified.append(h)
            if h["category"] == "lookup_ajax" and "LibExceptions.getExceptionShortMessage" in h["snippet"]:
                lookup_stale.append(h)

        print(f"=== Area {area} ({len(hits)}) ===")
        for cat in sorted(by_cat):
            items = by_cat[cat]
            print(f"  ## {cat} ({len(items)}) - {items[0]['desc']}")
            for h in items:
                print(f"     {h['path']}:{h['line']}  {h['method']}()")
        print()

    pending = [h for h in all_hits if h["category"] in ("ajax_json_return_inline", "ajax_json_catch_inline", "datatables_inline")]
    if pending:
        print(f"CANDIDATOS MIGRACAO N-Q+ ({len(pending)}) - JsonAjaxErro* / DataTableError:\n")
        for h in pending:
            print(f"  [{h['area']}] {h['path']}:{h['line']}  {h['method']}()  ({h['category']})")
        print()

    if lookup_stale:
        print(f"PENDENTE lookup_ajax ({len(lookup_stale)}) - usar GdiMvcJsonResults.AjaxFailureMessage:\n")
        for h in lookup_stale:
            print(f"  [{h['area']}] {h['path']}:{h['line']}  {h['method']}()")
        print()

    if unclassified:
        print(f"NAO CLASSIFICADO ({len(unclassified)}):\n")
        for h in unclassified:
            print(f"  [{h['area']}] {h['path']}:{h['line']}  {h['method']}()  |  {h['snippet'][:90]}")
        print()

    if unclassified or lookup_stale:
        return 1
    print("OK - todas as ocorrencias estao classificadas.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
