#!/usr/bin/env python3
"""Restaura views de backup _filestemp e reaplica migração LibMessageDialog→LibMessageSuccess."""
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SKIP = {"bin", "obj", "packages", ".git", "_filestemp"}
NEXT_KEYS = (
    "icon",
    "centerVertical",
    "closeButton",
    "size",
    "onEscape",
    "backdrop",
    "buttons",
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


def extract_js_expr_after(text: str, start: int) -> tuple[str, int] | None:
    i = start
    n = len(text)
    while i < n and text[i] in " \t\r\n":
        i += 1
    if i >= n:
        return None
    depth_paren = depth_brack = 0
    in_str: str | None = None
    escape = False
    expr_start = i
    while i < n:
        ch = text[i]
        if in_str:
            if escape:
                escape = False
            elif ch == "\\":
                escape = True
            elif ch == in_str:
                in_str = None
            i += 1
            continue
        if ch in ("'", '"'):
            in_str = ch
            i += 1
            continue
        if ch == "(":
            depth_paren += 1
        elif ch == ")":
            depth_paren = max(0, depth_paren - 1)
        elif ch == "[":
            depth_brack += 1
        elif ch == "]":
            depth_brack = max(0, depth_brack - 1)
        elif ch == "," and depth_paren == 0 and depth_brack == 0:
            rest = text[i + 1 : i + 80]
            if re.search(
                r"^\s*(?:" + "|".join(re.escape(k) for k in NEXT_KEYS) + r")\s*:",
                rest,
                re.I,
            ):
                return text[expr_start:i].strip(), i
        i += 1
    return text[expr_start:i].strip(), i


def extract_title(block: str) -> str | None:
    m = re.search(r"title\s*:\s*", block, re.I)
    return extract_js_expr_after(block, m.end())[0] if m else None


def extract_message(block: str) -> str | None:
    m = re.search(r"message\s*:\s*", block, re.I)
    return extract_js_expr_after(block, m.end())[0] if m else None


def extract_callback_fn(block: str) -> str | None:
    m = re.search(r"callback\s*:\s*(function\s*\([^)]*\)\s*\{)", block, re.I)
    if not m:
        return None
    open_brace = block.find("{", m.start(1))
    body = brace_block(block, open_brace)
    return block[m.start(1) : open_brace + len(body)] if body else None


def is_single_ok_confirmation(block: str) -> bool:
    if re.search(r"\bcancelar\s*:", block, re.I) or re.search(r"\bcancel\s*:", block, re.I):
        return False
    return bool(
        re.search(
            r"confirmar\s*:\s*\{[\s\S]*?label\s*:\s*['\"]OK['\"]",
            block,
            re.I,
        )
    )


def reindent_success_block(text: str, idx: int, title: str, message: str, callback_fn: str) -> str:
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
        s = ln.lstrip()
        reindented_cb.append((inner + s) if s else "")
    callback_block = "\n".join(reindented_cb)
    return (
        f'{block_indent}LibMessageSuccess({title}, {message}, {{\n'
        f"{inner}backdrop: true,\n"
        f"{inner}callback: {callback_block}\n"
        f"{block_indent}}});"
    )


def migrate_text(text: str) -> tuple[str, int]:
    replacements: list[tuple[int, int, str]] = []
    for m in re.finditer(r"LibMessageDialog\s*\(\s*\{", text):
        open_brace = m.end() - 1
        inner = brace_block(text, open_brace)
        if not inner or not is_single_ok_confirmation(inner):
            continue
        title = extract_title(inner)
        message = extract_message(inner)
        callback_fn = extract_callback_fn(inner)
        if not title or not message or not callback_fn:
            continue
        end = open_brace + len(inner)
        if end < len(text) and text[end] == ")":
            end += 1
        while end < len(text) and text[end] in " \t":
            end += 1
        if end < len(text) and text[end] == ";":
            end += 1
        new = reindent_success_block(text, m.start(), title, message, callback_fn)
        replacements.append((m.start(), end, new))
    for start, end, new in reversed(replacements):
        text = text[:start] + new + text[end:]
    return text, len(replacements)


def find_backup(rel: str) -> Path | None:
    for base in sorted(ROOT.glob("_filestemp/*")):
        if not base.is_dir():
            continue
        candidate = base / Path(rel)
        if candidate.is_file():
            return candidate
    return None


def main() -> None:
    restored = 0
    migrated = 0
    total_blocks = 0

    rels: set[str] = set()
    for path in ROOT.rglob("*.cshtml"):
        if any(p in SKIP for p in path.parts) or "Areas" not in path.parts:
            continue
        rels.add(path.relative_to(ROOT).as_posix())

    for rel in sorted(rels):
        target = ROOT / rel
        backup = find_backup(rel)
        if backup:
            text = backup.read_text(encoding="utf-8")
            restored += 1
        else:
            text = target.read_text(encoding="utf-8")

        text, n = migrate_text(text)
        if backup or n:
            target.write_text(text, encoding="utf-8", newline="\n")
        if n:
            migrated += 1
            total_blocks += n
            print(f"  {rel}: {n}")
        elif backup:
            print(f"  {rel}: (restaurado backup, sem LibMessageDialog)")

    print(f"\nRestaurados de backup: {restored}")
    print(f"Ficheiros migrados: {migrated}, blocos: {total_blocks}")


if __name__ == "__main__":
    sys.exit(main())
