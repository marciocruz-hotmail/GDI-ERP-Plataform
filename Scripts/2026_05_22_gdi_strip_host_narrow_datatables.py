# -*- coding: utf-8 -*-
"""
Remove gdi-dt-scroll-host + scroll-body-horizontal de DataTables com <=10 colunas
(padrão Index — colspan «Nenhum registro encontrado» em linha única).
"""
import re
import sys
from pathlib import Path

BASE = Path(__file__).resolve().parent.parent

# (caminho relativo, id da tabela)
UNWRAP = [
    ("Areas/g/Views/Clientes/CreateEdit.cshtml", "dataTables-Destinatarios"),
    ("Areas/g/Views/Clientes/CreateEdit.cshtml", "dtAudit"),
    ("Areas/g/Views/Atendimentos/Edit.cshtml", "dtGAtendimentoAtividades"),
    ("Areas/g/Views/Atendimentos/Edit.cshtml", "dtGAtendimentosLogs"),
    ("Areas/g/Views/Nfe/CreateEdit.cshtml", "dataTables-NfeLogs"),
    ("Areas/gc/Views/ComexImportacoes/CreateEdit.cshtml", "dtGcComexItens"),
    ("Areas/gc/Views/ComexImportacoes/CreateEdit.cshtml", "dtGedComexInvoicesPDF"),
    ("Areas/gc/Views/ComexImportacoes/CreateEdit.cshtml", "dtGedComexArquivos"),
    ("Areas/gc/Views/ComexImportacoes/CreateEdit.cshtml", "dtAudit"),
    ("Areas/gc/Views/Movimentos/FormPedidoCreate.cshtml", "dtGcMovimentosCreatePedido"),
    ("Areas/gc/Views/Movimentos/FormPedidoCreate.cshtml", "dtGcMovimentosGed"),
    ("Areas/gc/Views/Movimentos/FormPedidoCreate.cshtml", "dtAudit"),
    ("Areas/gc/Views/Movimentos/ModalConsultaPedidos.cshtml", "dtGcModalPedidosHistorico"),
    ("Areas/gc/Views/Movimentos/ModalPedidoViewAnexos.cshtml", "dtModalPedidoViewAnexos"),
    ("Areas/gc/Views/FinanceiroLancamentos/ModalFinanceiroViewAnexos.cshtml", "dtModalFinanceiroViewAnexos"),
    ("Areas/gc/Views/FinanceiroLancamentos/ModalCreateEditLancamento.cshtml", "dtGedGcLancamentos"),
    ("Areas/gc/Views/EstoqueLotes/ModalCreateEdit.cshtml", "dtGcEstoqueLotesGed"),
]


def unwrap_table(text: str, table_id: str) -> tuple[str, bool]:
    tid = re.escape(table_id)
    # Bloco: host > scroll > table ... + conteúdo opcional (ex. botão) até fechar host
    pat = re.compile(
        r'<div class="card-body m-0 p-1 gdi-dt-scroll-host min-w-0">\s*'
        r'<div class="table-responsive scroll-body-horizontal">\s*'
        r'(<table[^>]*\bid="' + tid + r'"[^>]*>[\s\S]*?</table>)\s*'
        r'</div>\s*'
        r'([\s\S]*?)'
        r'</div>',
        re.I,
    )

    def repl(m):
        table = m.group(1)
        extra = m.group(2)
        return (
            '<div class="card-body m-0 p-1" style="height: 100%">\n'
            + table + "\n"
            + extra
            + "</div>"
        )

    new_text, n = pat.subn(repl, text, count=1)
    return new_text, n > 0


def main():
    by_file: dict[str, list[str]] = {}
    for rel, tid in UNWRAP:
        by_file.setdefault(rel, []).append(tid)

    changed_files = []
    for rel, tids in sorted(by_file.items()):
        path = BASE / rel.replace("/", "\\")
        text = path.read_text(encoding="utf-8")
        orig = text
        for tid in tids:
            text, ok = unwrap_table(text, tid)
            if not ok:
                print(f"WARN: não encontrado {tid} em {rel}", file=sys.stderr)
        if text != orig:
            path.write_text(text, encoding="utf-8", newline="\r\n")
            changed_files.append(rel)
            print(f"OK {rel} ({len(tids)} tabela(s))")

    print(f"\nAlterados: {len(changed_files)} ficheiro(s)")


if __name__ == "__main__":
    main()
