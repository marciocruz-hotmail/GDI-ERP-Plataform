#!/usr/bin/env python3
"""Corrige handlers error: em views Movimentos — GdiAjaxNotifyInconsistencias sem referencia a result."""
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
VIEWS = ROOT / "Areas" / "gc" / "Views" / "Movimentos"

HYBRID = re.compile(
    r"GdiAjaxNotifyInconsistencias\(\(typeof result !== 'undefined' && result && result\.msg\) \? result\.msg : \(errorThrown \|\| textStatus \|\| 'Verifique as inconsistências\.'\)\)"
)

SUCCESS_ELSE = re.compile(
    r"GdiAjaxNotifyInconsistencias\(\(typeof result !== 'undefined' && result && result\.msg\) \? result\.msg : \(errorThrown \|\| textStatus \|\| 'Verifique as inconsistências\.'\)\);?"
)

ERROR_FIX = "GdiAjaxNotifyInconsistencias(errorThrown || textStatus || 'Verifique as inconsistências.');"
SUCCESS_FIX = "GdiAjaxNotifyInconsistencias((result && result.msg) ? result.msg : 'Verifique as inconsistências.');"

changed = []
for path in sorted(VIEWS.glob("*.cshtml")):
    text = path.read_text(encoding="utf-8")
    if "GdiAjaxNotifyInconsistencias" not in text:
        continue
    lines = text.splitlines(keepends=True)
    new_lines = []
    in_error_block = False
    file_changed = False
    for line in lines:
        if re.search(r"\berror:\s*function\b", line):
            in_error_block = True
        elif in_error_block and re.search(r"\bsuccess:\s*function\b", line):
            in_error_block = False

        if HYBRID.search(line):
            if in_error_block:
                new_line = HYBRID.sub(ERROR_FIX.rstrip(";"), line)
            else:
                new_line = HYBRID.sub(SUCCESS_FIX.rstrip(";"), line)
            if new_line != line:
                file_changed = True
            new_lines.append(new_line)
        else:
            new_lines.append(line)

    if file_changed:
        path.write_text("".join(new_lines), encoding="utf-8")
        changed.append(path.name)

print(f"Views alteradas: {len(changed)}")
for n in changed:
    print(f"  {n}")
