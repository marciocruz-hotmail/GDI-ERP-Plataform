#!/usr/bin/env python3
"""Insere LibMessageProcessandoHide() em callbacks error de $.ajax após LibMessageProcessando."""
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SKIP_PARTS = ("_filestemp", "obj", "bin", "PackageTmp")
LOOKBACK = 2000
HIDE_LINE = "                    LibMessageProcessandoHide();"


def should_skip(path: Path) -> bool:
    parts = path.parts
    return any(p in SKIP_PARTS for p in parts)


def collect_cshtml() -> list[Path]:
    files: list[Path] = []
    for base in (ROOT / "Areas", ROOT / "Views"):
        if not base.is_dir():
            continue
        files.extend(base.rglob("*.cshtml"))
    return sorted(files)


def patch_file(path: Path) -> int:
    text = path.read_text(encoding="utf-8", errors="replace")
    original = text
    changes = 0

    matches = list(re.finditer(r"error\s*:\s*function\s*\([^)]*\)\s*\{", text))
    for m in reversed(matches):
        start = m.start()
        ctx_before = text[max(0, start - LOOKBACK) : start]
        if "LibMessageProcessando(" not in ctx_before:
            continue

        i = m.end()
        depth = 1
        while i < len(text) and depth > 0:
            ch = text[i]
            if ch == "{":
                depth += 1
            elif ch == "}":
                depth -= 1
            i += 1
        block = text[m.end() : i - 1]

        if "LibMessageProcessandoHide" in block:
            continue

        indent = "                    "
        for line in block.splitlines():
            stripped = line.strip()
            if stripped and not stripped.startswith("//"):
                indent = line[: len(line) - len(line.lstrip())]
                break

        hide = f"{indent}LibMessageProcessandoHide();\n"
        new_block = hide + block
        text = text[: m.end()] + new_block + text[i - 1 :]
        changes += 1

    if changes and text != original:
        path.write_text(text, encoding="utf-8", newline="\r\n" if "\r\n" in original else "\n")
    return changes


def main() -> int:
    total_files = 0
    total_patches = 0
    touched: list[str] = []

    for path in collect_cshtml():
        if should_skip(path):
            continue
        n = patch_file(path)
        if n:
            total_files += 1
            total_patches += n
            touched.append(f"{path.relative_to(ROOT)} ({n})")

    print(f"Arquivos alterados: {total_files}")
    print(f"Callbacks corrigidos: {total_patches}")
    for line in touched:
        print(line)
    return 0


def format_and_fill_empty() -> int:
    """Normaliza indentação do Hide e preenche callbacks error vazios."""
    fmt_files = 0
    empty_fixed = 0
    for path in collect_cshtml():
        if should_skip(path):
            continue
        text = path.read_text(encoding="utf-8", errors="replace")
        original = text
        text = re.sub(
            r"(\berror\s*:\s*function\s*\([^)]*\)\s*)\{\s*LibMessageProcessandoHide\(\);\s*",
            r"\1{\n                    LibMessageProcessandoHide();\n",
            text,
        )

        def fix_empty(m: re.Match[str]) -> str:
            nonlocal empty_fixed
            block = m.group(2)
            if re.sub(r"\s|LibMessageProcessandoHide\(\);", "", block):
                return m.group(0)
            empty_fixed += 1
            return (
                m.group(1)
                + "{\n                    LibMessageProcessandoHide();\n"
                + "                    GdiAjaxNotifyInconsistencias(errorThrown || textStatus || "
                + "'Falha na comunicação com o servidor.');\n                }"
            )

        text = re.sub(
            r"(error\s*:\s*function\s*\(\s*xhr\s*,\s*textStatus\s*,\s*errorThrown\s*\)\s*)\{([^{}]*)\}",
            fix_empty,
            text,
            flags=re.DOTALL,
        )
        if text != original:
            fmt_files += 1
            path.write_text(
                text,
                encoding="utf-8",
                newline="\r\n" if "\r\n" in original else "\n",
            )
    print(f"Formatação/empty: arquivos {fmt_files}, vazios preenchidos {empty_fixed}")
    return 0


def fix_brace_same_line() -> int:
    """Separa `}` colado ao fim de GdiAjaxNotifyInconsistencias."""
    patterns = [
        (
            "GdiAjaxNotifyInconsistencias(errorThrown || textStatus || 'Verifique as inconsistências.')            }",
            "GdiAjaxNotifyInconsistencias(errorThrown || textStatus || 'Verifique as inconsistências.');\n                }",
        ),
        (
            "GdiAjaxNotifyInconsistencias(errorThrown || textStatus || 'Falha na comunicação com o servidor.');            }",
            "GdiAjaxNotifyInconsistencias(errorThrown || textStatus || 'Falha na comunicação com o servidor.');\n                }",
        ),
    ]
    fixed = 0
    for path in collect_cshtml():
        if should_skip(path):
            continue
        text = path.read_text(encoding="utf-8", errors="replace")
        original = text
        for old, new in patterns:
            text = text.replace(old, new)
        if text != original:
            fixed += 1
            path.write_text(
                text,
                encoding="utf-8",
                newline="\r\n" if "\r\n" in original else "\n",
            )
    print(f"Chaves separadas em {fixed} arquivos")
    return 0


def fix_indent_after_hide() -> int:
    """Corrige linhas sem indentação após LibMessageProcessandoHide inserido pelo patch."""
    fixed = 0
    for path in collect_cshtml():
        if should_skip(path):
            continue
        text = path.read_text(encoding="utf-8", errors="replace")
        original = text
        text = re.sub(
            r"(LibMessageProcessandoHide\(\);\r?\n)(document\.)",
            r"\1                    \2",
            text,
        )
        text = re.sub(
            r"(LibMessageProcessandoHide\(\);\r?\n)(GdiAjax)",
            r"\1                    \2",
            text,
        )
        text = re.sub(
            r"(LibMessageProcessandoHide\(\);\r?\n)(GdiDt)",
            r"\1                    \2",
            text,
        )
        text = re.sub(
            r"(LibMessageProcessandoHide\(\);\r?\n)(LibMessage)",
            r"\1                    \2",
            text,
        )
        text = re.sub(
            r"(LibMessageProcessandoHide\(\);\r?\n)(//)",
            r"\1                    \2",
            text,
        )
        text = re.sub(
            r"(GdiAjaxNotifyInconsistencias\([^;]+\);)\s+(\})",
            r"\1\n                \2",
            text,
        )
        text = re.sub(
            r"(GdiDtNotifyLoadFailure\([^;]+\);)\s+(\})",
            r"\1\n                \2",
            text,
        )
        if text != original:
            fixed += 1
            path.write_text(
                text,
                encoding="utf-8",
                newline="\r\n" if "\r\n" in original else "\n",
            )
    print(f"Indentação corrigida em {fixed} arquivos")
    return 0


if __name__ == "__main__":
    import sys as _sys

    if len(_sys.argv) > 1 and _sys.argv[1] == "--indent-only":
        fix_brace_same_line()
        fix_indent_after_hide()
        _sys.exit(0)
    rc = main()
    format_and_fill_empty()
    fix_brace_same_line()
    fix_indent_after_hide()
    _sys.exit(rc)
