"""
Gera .claude/CHANGELOG-RECENT.md a partir das N entradas mais recentes de
CHANGELOG-DEV.md (raiz).

Uso: python .claude/scripts/sync_changelog_recent.py
"""

from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CHANGELOG_SRC = ROOT / "CHANGELOG-DEV.md"
CHANGELOG_OUT = ROOT / ".claude" / "CHANGELOG-RECENT.md"
MAX_ENTRIES = 5
SECTION_MARKER = "## Últimas alterações relevantes"


def main() -> None:
    if not CHANGELOG_SRC.is_file():
        return

    content = CHANGELOG_SRC.read_text(encoding="utf-8")
    marker_pos = content.find(SECTION_MARKER)
    if marker_pos == -1:
        return

    entries_block = content[marker_pos + len(SECTION_MARKER) :]
    # Parar na próxima secção ##
    next_section = re.search(r"\n## ", entries_block)
    if next_section:
        entries_block = entries_block[: next_section.start()]

    entry_pattern = re.compile(r"(?=^### \d{4}-\d{2}-\d{2})", re.MULTILINE)
    entries = [
        e.strip()
        for e in entry_pattern.split(entries_block)
        if e.strip().startswith("###")
    ]
    recent = entries[:MAX_ENTRIES]

    out_lines = [
        "<!-- GERADO AUTOMATICAMENTE por .claude/scripts/sync_changelog_recent.py -->",
        f"<!-- Fonte: CHANGELOG-DEV.md (raiz) | Últimas {MAX_ENTRIES} entradas -->",
        "",
        "# CHANGELOG-DEV — Entradas Recentes",
        "",
        "> Gerado automaticamente. Histórico completo: `CHANGELOG-DEV.md` e `docs/dev-history/`.",
        "",
        "---",
        "",
        f"## Últimas alterações ({MAX_ENTRIES})",
        "",
    ]

    for entry in recent:
        out_lines.append(entry)
        out_lines.append("")
        out_lines.append("---")
        out_lines.append("")

    out_content = "\n".join(out_lines)

    if CHANGELOG_OUT.is_file() and CHANGELOG_OUT.read_text(encoding="utf-8") == out_content:
        return

    CHANGELOG_OUT.write_text(out_content, encoding="utf-8")
    print(f"[sync_changelog_recent] atualizado ({len(recent)} entradas).")


if __name__ == "__main__":
    main()
