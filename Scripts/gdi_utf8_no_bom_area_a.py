# -*- coding: utf-8 -*-
"""Converte todos os ficheiros de Areas/a para UTF-8 sem BOM."""
from __future__ import print_function
import codecs
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
AREA = ROOT / "Areas" / "a"

SKIP_SUFFIXES = {".png", ".jpg", ".jpeg", ".gif", ".ico", ".dll", ".exe", ".pdf", ".zip"}


def read_text_bytes(raw):
    if raw.startswith(codecs.BOM_UTF8):
        return raw[3:].decode("utf-8"), "utf-8-sig"
    if raw.startswith(b"\xff\xfe") and len(raw) % 2 == 0:
        return raw[2:].decode("utf-16-le"), "utf-16-le"
    if raw.startswith(b"\xfe\xff"):
        return raw[2:].decode("utf-16-be"), "utf-16-be"
    try:
        return raw.decode("utf-8"), "utf-8"
    except UnicodeDecodeError:
        pass
    try:
        return raw.decode("cp1252"), "cp1252"
    except UnicodeDecodeError:
        return None, None


def main():
    changed = []
    skipped = []
    for path in sorted(AREA.rglob("*")):
        if not path.is_file():
            continue
        if path.suffix.lower() in SKIP_SUFFIXES:
            skipped.append(path)
            continue
        raw = path.read_bytes()
        if not raw:
            text, enc = "", "empty"
        else:
            text, enc = read_text_bytes(raw)
            if text is None:
                skipped.append(path)
                print("SKIP (binario?):", path.relative_to(ROOT))
                continue
        out = text.encode("utf-8")
        if raw != out:
            path.write_bytes(out)
            changed.append((path.relative_to(ROOT), enc))
    print("Convertidos: %d" % len(changed))
    for rel, enc in changed:
        print("  [%s] %s" % (enc, rel))
    if skipped:
        print("Ignorados: %d" % len(skipped))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
