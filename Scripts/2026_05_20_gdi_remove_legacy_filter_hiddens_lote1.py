# -*- coding: utf-8 -*-
"""Lote 1: remove yesFilterOperador, yesFilterText, yesFilterAdvancedText de views e ajax DataTables."""
from __future__ import print_function
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
AREAS = ROOT / "Areas"

HIDDEN_PATTERNS = [
    re.compile(
        r"^\s*<input\s+type=\"hidden\"\s+id=\"yesFilterOperador\"[^>]*/>\s*$",
        re.IGNORECASE,
    ),
    re.compile(
        r"^\s*<input\s+type=\"hidden\"\s+id=\"yesFilterText\"[^>]*/>\s*$",
        re.IGNORECASE,
    ),
    re.compile(
        r"^\s*<input\s+type=\"hidden\"\s+id=\"yesFilterAdvancedText\"[^>]*/>\s*$",
        re.IGNORECASE,
    ),
]

AJAX_PATTERNS = [
    re.compile(
        r"^\s*yesFilterOperador:\s*\$\('#yesFilterOperador'\)\.val\(\)\.toString\(\),?\s*$"
    ),
    re.compile(
        r"^\s*yesFilterText:\s*\$\('#yesFilterText'\)\.val\(\)\.toString\(\),?\s*$"
    ),
    re.compile(
        r"^\s*yesFilterAdvancedText:\s*\$\('#yesFilterAdvancedText'\)\.val\(\)\.toString\(\),?\s*$"
    ),
]


def process_file(path):
    text = path.read_text(encoding="utf-8")
    original = text
    lines = text.splitlines(keepends=True)
    new_lines = []
    for line in lines:
        if any(p.match(line.rstrip("\r\n")) for p in HIDDEN_PATTERNS):
            continue
        if any(p.match(line.rstrip("\r\n")) for p in AJAX_PATTERNS):
            continue
        new_lines.append(line)
    text = "".join(new_lines)
    if text != original:
        path.write_text(text, encoding="utf-8")
        return True
    return False


def main():
    changed = []
    for cshtml in sorted(AREAS.rglob("*.cshtml")):
        if process_file(cshtml):
            changed.append(cshtml.relative_to(ROOT))
    print("Alterados: %d" % len(changed))
    for p in changed:
        print("  -", p)
    if changed:
        return 0
    print("Nenhum ficheiro alterado.")
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
