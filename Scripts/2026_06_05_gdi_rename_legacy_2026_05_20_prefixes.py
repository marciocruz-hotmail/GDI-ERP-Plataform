#!/usr/bin/env python3
"""Renomeia ficheiros 2026_05_20_* com prefixo != CreationTime e atualiza referencias.

Pastas: .cursor/context, .cursor/rules, Scripts/

Uso:
  python Scripts/2026_06_05_gdi_rename_legacy_2026_05_20_prefixes.py --dry-run
  python Scripts/2026_06_05_gdi_rename_legacy_2026_05_20_prefixes.py
"""
from __future__ import annotations

import argparse
import datetime as dt
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SCAN_DIRS = (
    ROOT / ".cursor" / "context",
    ROOT / ".cursor" / "rules",
    ROOT / "Scripts",
)
OLD_PREFIX = "2026_05_20_"
PREFIX_RE = re.compile(r"^2026_05_20_")

TEXT_EXTENSIONS = {
    ".md", ".mdc", ".py", ".ps1", ".json", ".csproj", ".cs", ".txt", ".config",
    ".cshtml", ".xml", ".asax", ".html", ".js", ".css", ".sql", ".psm1",
}
SKIP_DIRS = {
    ".git", "node_modules", "bin", "obj", "packages", "__pycache__",
    "LibUI_AdminLTE-4.0.0", "Scripts\\bootstrap", "Scripts\\jquery",
}


def creation_prefix(path: Path) -> str:
    ts = path.stat().st_ctime
    d = dt.datetime.fromtimestamp(ts)
    return f"{d.year:04d}_{d.month:02d}_{d.day:02d}_"


def collect_renames() -> list[tuple[Path, Path]]:
    renames: list[tuple[Path, Path]] = []
    for base in SCAN_DIRS:
        if not base.is_dir():
            continue
        for path in sorted(base.glob(f"{OLD_PREFIX}*")):
            if not path.is_file():
                continue
            new_prefix = creation_prefix(path)
            if new_prefix == OLD_PREFIX:
                continue
            new_name = PREFIX_RE.sub(new_prefix, path.name, count=1)
            dest = path.with_name(new_name)
            if dest.exists():
                raise FileExistsError(f"Destino ja existe: {dest}")
            renames.append((path, dest))
    return renames


def should_scan(path: Path) -> bool:
    if not path.is_file():
        return False
    if any(part in SKIP_DIRS for part in path.parts):
        return False
    if path.suffix.lower() in TEXT_EXTENSIONS:
        return True
    if path.name in ("CLAUDE.md", "Web.config", "AI-CONTEXT.md", "CHANGELOG-DEV.md", "BACKLOG-DEV.md"):
        return True
    return False


def update_references(renames: list[tuple[Path, Path]], dry_run: bool) -> int:
    # Substituir do nome mais longo para o mais curto evita colisoes parciais.
    pairs = sorted(
        [(old.name, new.name) for old, new in renames],
        key=lambda x: len(x[0]),
        reverse=True,
    )
    if not pairs:
        return 0

    files_touched = 0
    for path in ROOT.rglob("*"):
        if not should_scan(path):
            continue
        try:
            text = path.read_text(encoding="utf-8")
        except (OSError, UnicodeDecodeError):
            continue
        new_text = text
        for old_name, new_name in pairs:
            new_text = new_text.replace(old_name, new_name)
        if new_text != text:
            files_touched += 1
            if not dry_run:
                path.write_text(new_text, encoding="utf-8")
    return files_touched


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()

    renames = collect_renames()
    if not renames:
        print("Nada a renomear (todos 2026_05_20_* batem com CreationTime).")
        return 0

    by_prefix: dict[str, int] = {}
    for old, new in renames:
        p = new.name.split("_", 3)[:3]
        key = "_".join(p) + "_"
        by_prefix[key] = by_prefix.get(key, 0) + 1
        print(f"  {old.relative_to(ROOT)} -> {new.name}")

    print(f"\nRenomear: {len(renames)} ficheiro(s) — {by_prefix}")

    if not args.dry_run:
        for old, new in renames:
            old.rename(new)

    ref_count = update_references(renames, args.dry_run)
    mode = "dry-run" if args.dry_run else "aplicado"
    print(f"Referencias {mode}: {ref_count} ficheiro(s)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
