#!/usr/bin/env python3
"""Repara callbacks truncados após migração LibMessageSuccess + reindenta blocos."""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SKIP = {"bin", "obj", "packages", ".git"}
MARKER = 'LibMessageSuccess("Confirmação", result.msg, {'
BROKEN_FOREACH = re.compile(
    r"document\.querySelectorAll\('\.modal'\)\.forEach\(function\(m\)\{var i=bootstrap\.Modal\.getInstance\(m\);if\(i\)i\.hide\(\);\}\s*\n(\s*)\}\);",
    re.MULTILINE,
)


def brace_block(text: str, open_pos: int) -> str | None:
    depth = 0
    j = open_pos
    while j < len(text):
        if text[j] == "{":
            depth += 1
        elif text[j] == "}":
            depth -= 1
            if depth == 0:
                return text[open_pos : j + 1]
        j += 1
    return None


def brace_block_end(text: str, open_pos: int) -> int:
    blk = brace_block(text, open_pos)
    return open_pos + len(blk) if blk else open_pos


def extract_callback_fn(block: str) -> str | None:
    m = re.search(r"callback\s*:\s*(function\s*\([^)]*\)\s*\{)", block, re.I)
    if not m:
        return None
    open_brace = block.find("{", m.start(1))
    end = brace_block_end(block, open_brace)
    return block[m.start(1) : end]


def reindent_block(text: str, idx: int, callback_fn: str) -> str:
    before = text[:idx]
    m_if = None
    for m in re.finditer(
        r"^([ \t]*)if\s*\(\s*result\.success\s*==\s*true\s*\)",
        before,
        re.M,
    ):
        m_if = m
    block_indent = (m_if.group(1) + "    ") if m_if else "                        "
    inner = block_indent + "    "
    cb_lines = callback_fn.split("\n")
    reindented_cb = [cb_lines[0]]
    for ln in cb_lines[1:]:
        stripped = ln.lstrip()
        reindented_cb.append((inner + stripped) if stripped else "")
    callback_block = "\n".join(reindented_cb)
    return (
        f'{block_indent}LibMessageSuccess("Confirmação", result.msg, {{\n'
        f"{inner}backdrop: true,\n"
        f"{inner}callback: {callback_block}\n"
        f"{block_indent}}});"
    )


def find_backup(rel: str) -> Path | None:
    for base in ROOT.glob("_filestemp/*"):
        if not base.is_dir():
            continue
        candidate = base / Path(rel)
        if candidate.is_file():
            return candidate
    return None


def extract_single_ok_callbacks(backup: str) -> list[str]:
    cbs: list[str] = []
    for m in re.finditer(r"LibMessageDialog\s*\(\s*\{", backup):
        inner = brace_block(backup, m.end() - 1)
        if not inner or re.search(r"\bcancelar\s*:", inner, re.I):
            continue
        if not re.search(
            r"confirmar\s*:\s*\{[\s\S]*?label\s*:\s*['\"]OK['\"]", inner, re.I
        ):
            continue
        cb = extract_callback_fn(inner)
        if cb:
            cbs.append(cb)
    return cbs


def block_is_broken(block: str) -> bool:
    if "hide();}" in block and "hide();});" not in block:
        return True
    cb = extract_callback_fn(block)
    if not cb:
        return True
    return cb.count("{") != cb.count("}")


def foreach_repl(m: re.Match[str]) -> str:
    ind = m.group(1)
    return (
        "document.querySelectorAll('.modal').forEach(function(m){var i=bootstrap.Modal.getInstance(m);if(i)i.hide();});\n"
        f"{ind}LibMessageHideAll();\n"
        f"{ind}}}\n"
        f"{ind}}});"
    )


def main() -> None:
    foreach_fixed = 0
    restored = 0
    reindented = 0

    for path in sorted(ROOT.rglob("*.cshtml")):
        if any(p in SKIP for p in path.parts) or "Areas" not in path.parts:
            continue
        text = path.read_text(encoding="utf-8")
        orig = text

        text, n = BROKEN_FOREACH.subn(foreach_repl, text)
        foreach_fixed += n

        rel = path.relative_to(ROOT).as_posix()
        bp = find_backup(rel)
        backup_cbs = extract_single_ok_callbacks(bp.read_text(encoding="utf-8")) if bp else []
        cb_i = 0

        pos = 0
        while True:
            idx = text.find(MARKER, pos)
            if idx < 0:
                break
            open_brace = text.find("{", idx)
            end = brace_block_end(text, open_brace)
            if end < len(text) and text[end : end + 2] == ");":
                end += 2
            elif end < len(text) and text[end] == ")":
                end += 1
                if end < len(text) and text[end] == ";":
                    end += 1

            block = text[idx:end]
            cb = extract_callback_fn(block)

            if block_is_broken(block) and cb_i < len(backup_cbs):
                cb = backup_cbs[cb_i]
                cb_i += 1
                text = text[:idx] + reindent_block(text, idx, cb) + text[end:]
                restored += 1
                pos = idx + 1
                continue

            if cb:
                new_block = reindent_block(text, idx, cb)
                if new_block != block:
                    text = text[:idx] + new_block + text[end:]
                    reindented += 1
                    pos = idx + len(new_block)
                    continue
            pos = idx + 1

        if text != orig:
            path.write_text(text, encoding="utf-8", newline="\n")
            print(path.relative_to(ROOT).as_posix())

    print(f"\nforEach reparados: {foreach_fixed}")
    print(f"callbacks restaurados do backup: {restored}")
    print(f"blocos reindentados: {reindented}")


if __name__ == "__main__":
    main()
