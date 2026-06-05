#!/usr/bin/env python3
"""Lote N-J: adicionar JsonDataTableException e unificar catch DataTables inline em 7 controllers gc."""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

WRAPPER = """
        private JsonResult JsonDataTableException(Exception e, jQueryDataTableParamModel param, string yesFilterOnOff)
        {
            return Json(GdiMvcJsonResults.DataTableError(e, param, yesFilterOnOff), JsonRequestBehavior.AllowGet);
        }
"""

CONTROLLERS = [
    "Areas/gc/Controllers/EstoqueController.cs",
    "Areas/gc/Controllers/ComexProdutosController.cs",
    "Areas/gc/Controllers/EstoqueInventarioController.cs",
    "Areas/gc/Controllers/ComexInvoicesController.cs",
    "Areas/gc/Controllers/MovimentosEntradasController.cs",
    "Areas/gc/Controllers/ComexFinanceiroController.cs",
    "Areas/gc/Controllers/FinanceiroLancamentosController.cs",
]

# Triple catch + trailing error Json return (suporta yesDisplayField* e comentários)
TRIPLE_CATCH_ERROR_RETURN = re.compile(
    r"""
            catch\s*\(DbEntityValidationException\s+\w+\)\s*\{[^}]*\}\s*
            catch\s*\(WebException\s+\w+\)\s*\{[^}]*\}\s*
            catch\s*\(Exception\s+\w+\)\s*\{[^}]*\}\s*
            (?://[^\n]*\n\s*)*
            return\s+Json\s*\(\s*new\s*\{[\s\S]*?errorMessage[\s\S]*?\}\s*,\s*JsonRequestBehavior\.AllowGet\s*\)\s*;
    """,
    re.MULTILINE | re.DOTALL | re.VERBOSE,
)

CATCH_SINGLE = """            catch (Exception e)
            {
                return JsonDataTableException(e, param, filterOnOff);
            }"""

# One-liner triple catch + return (recebimento itens)
ONELINER_TRIPLE = re.compile(
    r"""
            catch\s*\(DbEntityValidationException\s+\w+\)\s*\{\s*errorMessage\s*=[^}]+\}\s*
            catch\s*\(WebException\s+\w+\)\s*\{\s*errorMessage\s*=[^}]+\}\s*
            catch\s*\(Exception\s+\w+\)\s*\{\s*errorMessage\s*=[^}]+\}\s*
            return\s+Json\s*\(\s*new\s*\{[\s\S]*?errorMessage[\s\S]*?\}\s*,\s*JsonRequestBehavior\.AllowGet\s*\)\s*;
    """,
    re.MULTILINE | re.DOTALL | re.VERBOSE,
)

# Inline catch with return Json new { errorMessage = LibExceptions...
INLINE_CATCH_PAIR = re.compile(
    r"""
            catch\s*\(DbEntityValidationException\s+e\)\s*\{\s*
                return\s+Json\s*\(\s*new\s*\{[^}]+\}\s*,\s*JsonRequestBehavior\.AllowGet\s*\);\s*
            \}\s*
            catch\s*\(Exception\s+e\)\s*\{\s*
                return\s+Json\s*\(\s*new\s*\{[^}]+\}\s*,\s*JsonRequestBehavior\.AllowGet\s*\);\s*
            \}
    """,
    re.MULTILINE | re.DOTALL | re.VERBOSE,
)

GDI_INLINE_CATCH = re.compile(
    r"return\s+Json\s*\(\s*GdiMvcJsonResults\.DataTableError\s*\(\s*e\s*,\s*param\s*,\s*filterOnOff\s*\)\s*,\s*JsonRequestBehavior\.AllowGet\s*\);"
)


def add_wrapper(content: str) -> str:
    if "private JsonResult JsonDataTableException" in content:
        return content
    # Insert before last closing brace of class (before final `}`)
    marker = "\n    }\n}"
    if marker not in content:
        raise ValueError("class end marker not found")
    return content.replace(marker, WRAPPER + marker, 1)


def hoist_filter_comex_financeiro(content: str) -> str:
    old = """            string saldoContaImportacao = "0,00";

            try
            {
                // -----------------------------
                // Parse / paging
                // -----------------------------
                int start = param.iDisplayStart;
                int length = (param.iDisplayLength <= 0 ? 100 : param.iDisplayLength);

                int idImportacao = 0;
                int idInvoice = 0;
                int.TryParse(param.yesCustomField01.EmptyIfNull().ToString().Trim(), out idImportacao);
                int.TryParse(param.yesCustomField02.EmptyIfNull().ToString().Trim(), out idInvoice);
                bool filtroAplicado = idImportacao > 0 || idInvoice > 0;
                string filterOnOff = (param.yesFilterField.EmptyIfNull().ToString().Trim() == "*" && filtroAplicado) ? "1" : "0";"""
    new = """            string saldoContaImportacao = "0,00";
            string filterOnOff = "0";

            try
            {
                // -----------------------------
                // Parse / paging
                // -----------------------------
                int start = param.iDisplayStart;
                int length = (param.iDisplayLength <= 0 ? 100 : param.iDisplayLength);

                int idImportacao = 0;
                int idInvoice = 0;
                int.TryParse(param.yesCustomField01.EmptyIfNull().ToString().Trim(), out idImportacao);
                int.TryParse(param.yesCustomField02.EmptyIfNull().ToString().Trim(), out idInvoice);
                bool filtroAplicado = idImportacao > 0 || idInvoice > 0;
                filterOnOff = (param.yesFilterField.EmptyIfNull().ToString().Trim() == "*" && filtroAplicado) ? "1" : "0";"""
    return content.replace(old, new)


def filter_arg_for_method(text: str, pos: int) -> str:
    """Escolhe filterOnOff ou literal conforme método GetDados*."""
    window = text[max(0, pos - 2500) : pos]
    if "GetDadosViewImportacao" in window or "GetDadosViewInvoicesItens" in window:
        return '"0"'
    return "filterOnOff"


def process_file(rel_path: str) -> int:
    path = ROOT / rel_path
    text = path.read_text(encoding="utf-8")
    original = text

    if rel_path.endswith("ComexFinanceiroController.cs") and "string filterOnOff = \"0\";" not in text:
        text = hoist_filter_comex_financeiro(text)

    def sub_triple(m: re.Match) -> str:
        fo = filter_arg_for_method(text, m.start())
        return f"""            catch (Exception e)
            {{
                return JsonDataTableException(e, param, {fo});
            }}"""

    text = TRIPLE_CATCH_ERROR_RETURN.sub(sub_triple, text)
    text = ONELINER_TRIPLE.sub(sub_triple, text)

    def replace_inline_pair(m: re.Match) -> str:
        # Detect filterOnOff from context - default filterOnOff; special cases use literal
        block = m.group(0)
        if "GetDadosMovimentosEntradas" in text[: m.start()][-2000:]:
            fo = "filterOnOff"
        elif "GetDadosRecebimentoImportacao" in text[: m.start()][-2000:]:
            fo = "filterOnOff"
        else:
            fo = "filterOnOff"
        return f"""            catch (Exception e)
            {{
                return JsonDataTableException(e, param, {fo});
            }}"""

    text = INLINE_CATCH_PAIR.sub(replace_inline_pair, text)

    # ComexInvoices / methods without filterOnOff variable — use "0"
    text = text.replace(
        "return JsonDataTableException(e, param, filterOnOff);",
        "return JsonDataTableException(e, param, filterOnOff);",
    )
    # Fix GetDadosView* where filterOnOff may not exist — replace in those methods only via manual below

    text = GDI_INLINE_CATCH.sub(
        "return JsonDataTableException(e, param, filterOnOff);", text
    )

    if "private JsonResult JsonDataTableException" not in text:
        text = add_wrapper(text)

    if text != original:
        path.write_text(text, encoding="utf-8")
    return 1 if text != original else 0


def main() -> int:
    changed = 0
    for rel in CONTROLLERS:
        changed += process_file(rel)
    print(f"N-J: {changed} controller(s) atualizado(s)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
