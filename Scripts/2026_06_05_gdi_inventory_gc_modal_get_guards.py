#!/usr/bin/env python3
"""Inventário heurístico: modais GET em Areas/gc/Controllers sem guard MsgBloqueio/EntidadeNaoEncontrada.

Uso: python Scripts/2026_06_05_gdi_inventory_gc_modal_get_guards.py
Exit 0 = lista apenas; exit 1 = há candidatos pendentes (para CI opcional).
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CONTROLLERS = ROOT / "Areas" / "gc" / "Controllers"

GUARD_MARKERS = (
    "ViewBag.MsgBloqueio",
    "EntidadeNaoEncontradaMensagem",
    "PedidoNaoEncontradoMensagem",
    "TryGetMovimentoModal",
    "MsgBloqueio =",
)

MODAL_ACTION_RE = re.compile(
    r"public\s+ActionResult\s+(Modal\w+)\s*\([^)]*\)\s*\{",
    re.MULTILINE,
)


def extract_method_body(text: str, start: int) -> str:
    depth = 0
    i = start
    while i < len(text):
        c = text[i]
        if c == "{":
            depth += 1
        elif c == "}":
            depth -= 1
            if depth == 0:
                return text[start : i + 1]
        i += 1
    return text[start:]


def scan_file(path: Path) -> list[tuple[str, str, str]]:
    text = path.read_text(encoding="utf-8", errors="replace")
    pending: list[tuple[str, str, str]] = []
    for m in MODAL_ACTION_RE.finditer(text):
        name = m.group(1)
        body = extract_method_body(text, m.end() - 1)
        if any(marker in body for marker in GUARD_MARKERS):
            continue
        # ignora redirects puros para ModalError sem Find (raro)
        if "RedirectToAction" in body and ".Find(" not in body:
            continue
        if ".Find(" not in body and "int.Parse(" not in body:
            continue
        reason = "Find/Parse sem guard MsgBloqueio"
        pending.append((path.name, name, reason))
    return pending


def main() -> int:
    if not CONTROLLERS.is_dir():
        print(f"Diretório não encontrado: {CONTROLLERS}", file=sys.stderr)
        return 2

    all_pending: list[tuple[str, str, str]] = []
    for path in sorted(CONTROLLERS.glob("*.cs")):
        all_pending.extend(scan_file(path))

    if not all_pending:
        print("OK — nenhum modal GET gc pendente (heurística).")
        return 0

    print(f"Pendentes ({len(all_pending)}) — modais GET gc sem guard padrão N-F:\n")
    for ctrl, action, reason in all_pending:
        print(f"  {ctrl} :: {action}  ({reason})")
    return 1


if __name__ == "__main__":
    sys.exit(main())
