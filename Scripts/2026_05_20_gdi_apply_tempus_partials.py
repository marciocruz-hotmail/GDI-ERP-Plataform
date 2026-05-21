# -*- coding: utf-8 -*-
"""G-PERF-20c-bis — Aplica partials Tempus nas views host (legado).

Preferir flag TempusDominus em GdiPageScriptsDefaults + partials opcionais no _Layout
(2026-05-21). Executar só se flags não estiverem activas.
"""
from __future__ import print_function

import os
import re

BASE = os.path.normpath(os.path.join(os.path.dirname(__file__), ".."))

HOSTS = [
    "Areas/g/Views/Clientes/CreateEdit.cshtml",
    "Areas/g/Views/ContratosAviacao/CreateEdit.cshtml",
    "Areas/g/Views/Financeiro/Index.cshtml",
    "Areas/g/Views/Ged/Index.cshtml",
    "Areas/g/Views/Nfe/CreateEdit.cshtml",
    "Areas/g/Views/Clientes/Index.cshtml",
    "Areas/g/Views/Nfe/Index.cshtml",
    "Areas/gc/Views/ComexImportacoes/CreateEdit.cshtml",
    "Areas/gc/Views/EstoqueControle/CreateEdit.cshtml",
    "Areas/gc/Views/FinanceiroLancamentos/Index.cshtml",
    "Areas/gc/Views/Fretes/Index.cshtml",
    "Areas/gc/Views/Gerencial/IndexPainelComercialGerencial.cshtml",
    "Areas/gc/Views/Movimentos/FormPedidoCreate.cshtml",
    "Areas/gc/Views/Movimentos/IndexPedido.cshtml",
    "Areas/gc/Views/Movimentos/PainelPedidos.cshtml",
    "Areas/gc/Views/ComexFinanceiro/Index.cshtml",
    "Areas/gc/Views/RelatoriosComerciais/Index.cshtml",
    "Areas/gc/Views/RelatoriosFinanceiros/Index.cshtml",
    "Areas/gc/Views/RelatoriosRegulamentacao/Index.cshtml",
    "Areas/qa/Views/GedSGQ/IndexAtasReunioes.cshtml",
    "Areas/qa/Views/GedSGQ/IndexComunicados.cshtml",
    "Areas/qa/Views/GedSGQ/IndexDocsSGQ.cshtml",
]

HEAD_PARTIAL = '@{ Html.RenderPartial("~/Views/Shared/_LayoutHeadTempus.cshtml"); }\n'
SCRIPTS_PARTIAL = '@{ Html.RenderPartial("~/Views/Shared/_LayoutScriptsTempus.cshtml"); }\n'
HEAD_MARK = "_LayoutHeadTempus"
SCRIPTS_MARK = "_LayoutScriptsTempus"

LAYOUT_LINE = '~/Views/Shared/_Layout.cshtml'
LAYOUT_INLINE_RE = re.compile(
    r'@\{\s*Layout\s*=\s*["\']~/Views/Shared/_Layout\.cshtml["\'];\s*\}',
    re.I,
)
LAYOUT_BLOCK_RE = re.compile(
    r'(Layout\s*=\s*["\']~/Views/Shared/_Layout\.cshtml["\'];?\s*\n)',
    re.I,
)


def insert_scripts_before(text):
    m = re.search(r'<script\s+type\s*=\s*["\']text/javascript["\']', text, re.I)
    if m:
        pos = m.start()
        return text[:pos] + SCRIPTS_PARTIAL + text[pos:]
    matches = list(re.finditer(r'<script\b', text, re.I))
    if not matches:
        return text + "\n" + SCRIPTS_PARTIAL
    pos = matches[-1].start()
    return text[:pos] + SCRIPTS_PARTIAL + text[pos:]


def patch_file(rel):
    path = os.path.join(BASE, rel.replace("/", os.sep))
    with open(path, encoding="utf-8") as f:
        text = f.read()
    changed = False
    if HEAD_MARK not in text:
        m_inline = LAYOUT_INLINE_RE.search(text)
        if m_inline:
            insert_at = m_inline.end()
            text = text[:insert_at] + "\n" + HEAD_PARTIAL + text[insert_at:]
            changed = True
        else:
            m = LAYOUT_BLOCK_RE.search(text)
            if m:
                text = text[: m.end()] + HEAD_PARTIAL + text[m.end() :]
                changed = True
            else:
                print("WARN no Layout line:", rel)
    if SCRIPTS_MARK not in text:
        new_text = insert_scripts_before(text)
        if new_text != text:
            text = new_text
            changed = True
    if changed:
        with open(path, "w", encoding="utf-8", newline="\r\n") as f:
            f.write(text)
        print("OK", rel)
    else:
        print("SKIP", rel)


def main():
    for rel in HOSTS:
        patch_file(rel)


if __name__ == "__main__":
    main()
