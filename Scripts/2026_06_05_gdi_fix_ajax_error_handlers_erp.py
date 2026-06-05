#!/usr/bin/env python3
"""C-2: corrige handlers GdiAjaxNotifyInconsistencias hibridos em Areas/**/Views.

- success/else: result.msg com fallback fixo
- error: rede: errorThrown || textStatus (sem referencia a result)

Uso:
  python Scripts/2026_06_05_gdi_fix_ajax_error_handlers_erp.py
  python Scripts/2026_06_05_gdi_fix_ajax_error_handlers_erp.py --dry-run
"""
from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
AREAS = ROOT / "Areas"

HYBRID = re.compile(
    r"GdiAjaxNotifyInconsistencias\(\(typeof result !== 'undefined' && result && result\.msg\) \? result\.msg : \(errorThrown \|\| textStatus \|\| '([^']*)'\)\);?"
)


def fix_file(path: Path, dry_run: bool) -> int:
    text = path.read_text(encoding="utf-8")
    if "typeof result !== 'undefined'" not in text:
        return 0

    lines = text.splitlines(keepends=True)
    new_lines: list[str] = []
    in_error_block = False
    changes = 0

    for line in lines:
        if re.search(r"\berror:\s*function\b", line):
            in_error_block = True
        elif in_error_block and re.search(r"\bsuccess:\s*function\b", line):
            in_error_block = False

        m = HYBRID.search(line)
        if m:
            fallback = m.group(1)
            if in_error_block:
                replacement = (
                    f"GdiAjaxNotifyInconsistencias(errorThrown || textStatus || '{fallback}')"
                )
            else:
                replacement = (
                    f"GdiAjaxNotifyInconsistencias((result && result.msg) ? result.msg : '{fallback}')"
                )
            new_line = HYBRID.sub(replacement, line)
            if new_line != line:
                changes += 1
            new_lines.append(new_line)
        else:
            new_lines.append(line)

    if changes and not dry_run:
        path.write_text("".join(new_lines), encoding="utf-8")
    return changes


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()

    total_files = 0
    total_changes = 0
    for path in sorted(AREAS.rglob("*.cshtml")):
        n = fix_file(path, args.dry_run)
        if n:
            total_files += 1
            total_changes += n
            rel = path.relative_to(ROOT)
            print(f"  {rel} ({n} linha(s))")

    mode = "dry-run" if args.dry_run else "aplicado"
    print(f"\n{mode}: {total_files} ficheiro(s), {total_changes} substituicao(oes)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
