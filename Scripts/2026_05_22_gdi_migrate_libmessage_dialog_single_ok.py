#!/usr/bin/env python3
"""
Migra LibMessageDialog (1 botão OK, sem cancelar) → LibMessageSuccess, preservando callback.
Âmbito: Areas/** e Views/** (.cshtml), exclui _filestemp.
"""
from __future__ import annotations

import argparse
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
    """Lê expressão JS após start até vírgula de nível 0 antes de chave conhecida."""
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
    if not m:
        return None
    expr, _ = extract_js_expr_after(block, m.end())
    return expr


def extract_message(block: str) -> str | None:
    m = re.search(r"message\s*:\s*", block, re.I)
    if not m:
        return None
    expr, _ = extract_js_expr_after(block, m.end())
    return expr


def extract_callback_fn(block: str) -> str | None:
    m = re.search(r"callback\s*:\s*(function\s*\([^)]*\)\s*\{)", block, re.I)
    if not m:
        return None
    open_brace = block.find("{", m.start(1))
    body_block = brace_block(block, open_brace)
    if not body_block:
        return None
    return block[m.start(1) : open_brace + len(body_block)]


def is_single_ok_confirmation(block: str) -> bool:
    if re.search(r"\bcancelar\s*:", block, re.I):
        return False
    if re.search(r"\bcancel\s*:", block, re.I):
        return False
    if re.search(r"\bconfirm\s*:", block, re.I) and not re.search(
        r"\bconfirmar\s*:", block, re.I
    ):
        return False
    return bool(
        re.search(
            r"confirmar\s*:\s*\{[\s\S]*?label\s*:\s*['\"]OK['\"]",
            block,
            re.I,
        )
    )


def build_replacement(indent: str, title: str, message: str, callback_fn: str) -> str:
    lines = [
        f'{indent}LibMessageSuccess({title}, {message}, {{',
        f"{indent}    backdrop: true,",
        f"{indent}    callback: {callback_fn.strip()}",
        f"{indent}}});",
    ]
    return "\n".join(lines)


def migrate_file(path: Path, dry_run: bool) -> int:
    text = path.read_text(encoding="utf-8")
    if "LibMessageDialog" not in text:
        return 0

    replacements: list[tuple[int, int, str]] = []

    for m in re.finditer(r"LibMessageDialog\s*\(\s*\{", text):
        open_brace = m.end() - 1
        inner = brace_block(text, open_brace)
        if not inner:
            continue
        if not is_single_ok_confirmation(inner):
            continue

        title = extract_title(inner)
        message = extract_message(inner)
        callback_fn = extract_callback_fn(inner)
        if not title or not message or not callback_fn:
            print(f"  SKIP parse: {path.relative_to(ROOT)}:{text[:m.start()].count(chr(10))+1}")
            continue

        end = open_brace + len(inner)
        if end < len(text) and text[end] == ")":
            end += 1
        while end < len(text) and text[end] in " \t":
            end += 1
        if end < len(text) and text[end] == ";":
            end += 1

        line_start = text.rfind("\n", 0, m.start()) + 1
        indent = re.match(r"[ \t]*", text[line_start:m.start()]).group(0)

        new_text = build_replacement(indent, title, message, callback_fn)
        replacements.append((m.start(), end, new_text))

    if not replacements:
        return 0

    for start, end, new in reversed(replacements):
        text = text[:start] + new + text[end:]

    if not dry_run:
        path.write_text(text, encoding="utf-8", newline="\n")
    rel = path.relative_to(ROOT).as_posix()
    print(f"  {rel}: {len(replacements)} bloco(s)")
    return len(replacements)


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args()

    total = 0
    files = 0
    for path in sorted(ROOT.rglob("*.cshtml")):
        if any(p in SKIP for p in path.parts):
            continue
        if "Areas" not in path.parts and (not path.parts or path.parts[0] != "Views"):
            continue
        n = migrate_file(path, args.dry_run)
        if n:
            files += 1
            total += n
            if args.dry_run and not args.dry_run:
                pass

    mode = "DRY-RUN" if args.dry_run else "APLICADO"
    print(f"\n{mode}: {total} bloco(s) em {files} ficheiro(s)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
