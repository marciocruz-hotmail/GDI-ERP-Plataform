#!/usr/bin/env python3
"""Corrige indentação de blocos LibMessageSuccess migrados (OK único)."""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SKIP = {"bin", "obj", "packages", ".git", "_filestemp"}
MARKER = 'LibMessageSuccess("Confirmação", result.msg, {'


def brace_block_end(text: str, open_pos: int) -> int:
    depth = 0
    j = open_pos
    while j < len(text):
        if text[j] == "{":
            depth += 1
        elif text[j] == "}":
            depth -= 1
            if depth == 0:
                return j + 1
        j += 1
    return open_pos


def main() -> None:
    fixed_files = 0
    for path in sorted(ROOT.rglob("*.cshtml")):
        if any(p in SKIP for p in path.parts) or "Areas" not in path.parts:
            continue
        text = path.read_text(encoding="utf-8")
        if MARKER not in text:
            continue
        changed = False
        pos = 0
        while True:
            idx = text.find(MARKER, pos)
            if idx < 0:
                break
            line_start = text.rfind("\n", 0, idx) + 1
            open_brace = text.find("{", idx)
            end = brace_block_end(text, open_brace)
            if end < len(text) and text[end : end + 2] == ");":
                end += 2
            elif end < len(text) and text[end] == ")":
                end += 1
                if end < len(text) and text[end] == ";":
                    end += 1

            before = text[:line_start]
            m_if = None
            for m in re.finditer(
                r"^([ \t]*)if\s*\(\s*result\.success\s*==\s*true\s*\)",
                before,
                re.M,
            ):
                m_if = m
            if not m_if:
                pos = idx + 1
                continue
            block_indent = m_if.group(1) + "    "
            inner = block_indent + "    "

            block = text[idx:end]
            cb_m = re.search(
                r"callback\s*:\s*(function\s*\([^)]*\)\s*\{[\s\S]*?\})",
                block,
            )
            if not cb_m:
                pos = idx + 1
                continue
            callback_fn = cb_m.group(1)
            # Reindent callback body lines relative to function open brace
            cb_lines = callback_fn.split("\n")
            reindented_cb = [cb_lines[0]]
            for ln in cb_lines[1:]:
                stripped = ln.lstrip()
                if stripped:
                    reindented_cb.append(inner + stripped)
                else:
                    reindented_cb.append("")
            callback_block = "\n".join(reindented_cb)

            new_block = (
                f'{block_indent}LibMessageSuccess("Confirmação", result.msg, {{\n'
                f"{inner}backdrop: true,\n"
                f"{inner}callback: {callback_block}\n"
                f"{block_indent}}});"
            )
            if new_block != block:
                text = text[:idx] + new_block + text[end:]
                changed = True
            pos = idx + len(new_block)

        if changed:
            path.write_text(text, encoding="utf-8", newline="\n")
            fixed_files += 1
            print(path.relative_to(ROOT).as_posix())

    print(f"\nIndent corrigido em {fixed_files} ficheiro(s)")


if __name__ == "__main__":
    main()
