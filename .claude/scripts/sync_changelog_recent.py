"""
Gera .claude/CHANGELOG-RECENT.md a partir das N entradas mais recentes de
.cursor/CHANGELOG-DEV.md.

Executado automaticamente pelo hook PostToolUse (settings.json) sempre que
qualquer arquivo é editado, e pelo hook UserPromptSubmit no início de cada
sessão. Idempotente e rápido (leitura sequencial).

Uso direto: python .claude/scripts/sync_changelog_recent.py
"""

import os
import re

CHANGELOG_SRC = os.path.join(".cursor", "CHANGELOG-DEV.md")
CHANGELOG_OUT = os.path.join(".claude", "CHANGELOG-RECENT.md")
MAX_ENTRIES = 5

def main():
    if not os.path.exists(CHANGELOG_SRC):
        return

    with open(CHANGELOG_SRC, encoding="utf-8") as f:
        content = f.read()

    # Separar cabeçalho (tudo antes de "## HISTÓRICO DE INTERVENÇÕES")
    historico_marker = "## HISTÓRICO DE INTERVENÇÕES"
    marker_pos = content.find(historico_marker)
    if marker_pos == -1:
        # Fallback: sem marcador, pegar primeiras 50 linhas
        header = "\n".join(content.splitlines()[:50])
        entries_block = ""
    else:
        header_block = content[:marker_pos].rstrip()
        # Resumir cabeçalho: só primeiras 3 linhas (título + stack) + linha em branco
        header_lines = header_block.splitlines()
        header = "\n".join(header_lines[:5])
        entries_block = content[marker_pos + len(historico_marker):]

    # Dividir entradas pelo padrão "### [YYYY-MM-DD]"
    entry_pattern = re.compile(r"(?=^### \[\d{4}-\d{2}-\d{2}\])", re.MULTILINE)
    entries = entry_pattern.split(entries_block)
    # Filtrar fragmentos vazios (separadores ---, linhas em branco)
    entries = [e.strip() for e in entries if e.strip() and e.strip().startswith("###")]

    recent = entries[:MAX_ENTRIES]

    out_lines = [
        "<!-- GERADO AUTOMATICAMENTE por .claude/scripts/sync_changelog_recent.py -->",
        "<!-- Fonte: .cursor/CHANGELOG-DEV.md | Últimas {} entradas -->".format(MAX_ENTRIES),
        "",
        "# CHANGELOG-DEV — Entradas Recentes",
        "",
        "> Arquivo gerado automaticamente. Para o histórico completo, consulte `.cursor/CHANGELOG-DEV.md`.",
        "",
        "---",
        "",
        "## HISTÓRICO DE INTERVENÇÕES (últimas {})".format(MAX_ENTRIES),
        "",
    ]

    for entry in recent:
        out_lines.append(entry)
        out_lines.append("")
        out_lines.append("---")
        out_lines.append("")

    out_content = "\n".join(out_lines)

    # Só escrever se o conteúdo mudou (evita toques desnecessários no arquivo)
    if os.path.exists(CHANGELOG_OUT):
        with open(CHANGELOG_OUT, encoding="utf-8") as f:
            existing = f.read()
        if existing == out_content:
            return

    with open(CHANGELOG_OUT, "w", encoding="utf-8") as f:
        f.write(out_content)

    print("[sync_changelog_recent] CHANGELOG-RECENT.md atualizado ({} entradas).".format(len(recent)))

if __name__ == "__main__":
    main()
