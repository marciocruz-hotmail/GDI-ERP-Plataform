#!/usr/bin/env python3
"""Remove pastas em packages/ que não constam em packages.config."""
from __future__ import annotations

import os
import stat
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CFG = ROOT / "packages.config"
PKG_DIR = ROOT / "packages"


def main() -> int:
    if not CFG.is_file():
        print(f"ERRO: {CFG} não encontrado", file=sys.stderr)
        return 1
    if not PKG_DIR.is_dir():
        print(f"AVISO: {PKG_DIR} não existe — nada a remover")
        return 0

    expected = {
        f"{pkg.get('id')}.{pkg.get('version')}"
        for pkg in ET.parse(CFG).getroot()
    }

    orphans = sorted(
        d for d in PKG_DIR.iterdir() if d.is_dir() and d.name not in expected
    )

    print(f"Pacotes em packages.config: {len(expected)}")
    print(f"Pastas órfãs a remover: {len(orphans)}")

    def _on_rm_error(func, path, exc_info):
        try:
            os.chmod(path, stat.S_IWRITE)
            func(path)
        except OSError:
            raise exc_info[1]

    removed: list[str] = []
    errors: list[tuple[str, str]] = []
    for d in orphans:
        print(f"  - {d.name}")
        try:
            for root, dirs, files in os.walk(d, topdown=False):
                for name in files:
                    os.chmod(os.path.join(root, name), stat.S_IWRITE)
                for name in dirs:
                    os.chmod(os.path.join(root, name), stat.S_IWRITE)
            os.chmod(d, stat.S_IWRITE)
            import shutil

            shutil.rmtree(d, onerror=_on_rm_error)
            removed.append(d.name)
        except OSError as exc:
            errors.append((d.name, str(exc)))

    remaining = {d.name for d in PKG_DIR.iterdir() if d.is_dir()}
    unexpected = sorted(remaining - expected)

    print(f"Removidas com sucesso: {len(removed)}")
    if errors:
        print("Erros:", file=sys.stderr)
        for name, msg in errors:
            print(f"  {name}: {msg}", file=sys.stderr)
    if unexpected:
        print("Ainda inesperadas:", unexpected, file=sys.stderr)
        return 1

    print(f"Pastas restantes: {len(remaining)}")
    return 0 if not errors else 1


if __name__ == "__main__":
    raise SystemExit(main())
