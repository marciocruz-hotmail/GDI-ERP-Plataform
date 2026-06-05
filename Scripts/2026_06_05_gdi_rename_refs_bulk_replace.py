#!/usr/bin/env python3
"""Substitui referencias 2026_06_05_ -> 2026_06_05_ apos renomeacao de ficheiros."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OLD, NEW = "2026_06_05_", "2026_06_05_"
EXTS = {".md", ".mdc", ".py", ".ps1", ".json", ".csproj"}
SKIP = {".git", "node_modules", "bin", "obj", "packages"}

changed: list[str] = []
for path in ROOT.rglob("*"):
    if not path.is_file() or path.suffix.lower() not in EXTS:
        continue
    if any(p in SKIP for p in path.parts):
        continue
    try:
        text = path.read_text(encoding="utf-8")
    except OSError:
        continue
    if OLD not in text:
        continue
    path.write_text(text.replace(OLD, NEW), encoding="utf-8")
    changed.append(str(path.relative_to(ROOT)))

print(f"Arquivos atualizados: {len(changed)}")
for c in sorted(changed):
    print(f"  {c}")
