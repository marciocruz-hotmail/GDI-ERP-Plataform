#!/usr/bin/env python3
"""Substitui errorMessage/stackTrace literais vazios por GdiMvcJsonResults nos controllers."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
AREAS = ROOT / "Areas"

REPLACEMENTS = [
    ('errorMessage = ""', 'errorMessage = GdiMvcJsonResults.DataTableSuccessErrorMessage'),
    ("errorMessage = \"\"", 'errorMessage = GdiMvcJsonResults.DataTableSuccessErrorMessage'),
    ('stackTrace = ""', 'stackTrace = GdiMvcJsonResults.DataTableSuccessStackTrace'),
    ("stackTrace = \"\"", 'stackTrace = GdiMvcJsonResults.DataTableSuccessStackTrace'),
]

changed = []
for path in sorted(AREAS.rglob("*Controller*.cs")):
    text = path.read_text(encoding="utf-8")
    new = text
    for old, new_val in REPLACEMENTS:
        new = new.replace(old, new_val)
    if new != text:
        path.write_text(new, encoding="utf-8")
        changed.append(str(path.relative_to(ROOT)))

print(f"Arquivos alterados: {len(changed)}")
for p in changed:
    print(f"  {p}")
