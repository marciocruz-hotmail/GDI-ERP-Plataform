#!/usr/bin/env python3
"""Inventário: LibMessageDialog com botão único OK (candidatos a LibMessageSuccess)."""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SKIP = {"bin", "obj", "packages", ".git", "_filestemp"}


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


def main() -> None:
    single_ok: list[tuple[str, int, str]] = []
    multi_btn: list[tuple[str, int]] = []
    total_dialog = 0

    for path in sorted(ROOT.rglob("*.cshtml")):
        if any(p in SKIP for p in path.parts):
            continue
        try:
            text = path.read_text(encoding="utf-8", errors="ignore")
        except OSError:
            continue
        if "LibMessageDialog" not in text:
            continue

        for m in re.finditer(r"LibMessageDialog\s*\(\s*\{", text):
            total_dialog += 1
            open_brace = text.find("{", m.start())
            block = brace_block(text, open_brace)
            if not block:
                continue
            line = text[: m.start()].count("\n") + 1
            rel = path.relative_to(ROOT).as_posix()
            has_ok = bool(
                re.search(
                    r"confirmar\s*:\s*\{[^}]*label\s*:\s*['\"]OK['\"]",
                    block,
                    re.I | re.DOTALL,
                )
            )
            has_cancel = bool(re.search(r"\bcancelar\s*:", block, re.I))
            if has_ok and not has_cancel:
                snippet = re.sub(r"\s+", " ", block[:120]).strip()
                single_ok.append((rel, line, snippet))
            elif has_cancel:
                multi_btn.append((rel, line))

    print(f"LibMessageDialog total blocos: {total_dialog}")
    print(f"Candidatos fase A (1 botao OK, sem cancelar): {len(single_ok)}")
    print()
    for rel, line, _ in single_ok:
        print(f"  {rel}:{line}")
    print()
    print(f"Com cancelar/confirmar (nao fase A): {len(multi_btn)}")
    for rel, line in multi_btn[:5]:
        print(f"  ... {rel}:{line}")
    if len(multi_btn) > 5:
        print(f"  ... (+{len(multi_btn) - 5} mais)")


if __name__ == "__main__":
    main()
