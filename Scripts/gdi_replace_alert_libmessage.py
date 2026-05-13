# -*- coding: utf-8 -*-
"""
Fase 7: substitui alert( nativo (catch handlers) por LibMessageError em .cshtml.
Não altera GdiSwal2.alert / window.alert (lookbehind (?<!\\.)).
Uso: python Scripts/gdi_replace_alert_libmessage.py [--dry-run]
"""
from __future__ import print_function

import os
import re
import sys

BASE = os.path.normpath(os.path.join(os.path.dirname(__file__), ".."))
ROOTS = [os.path.join(BASE, "Areas"), os.path.join(BASE, "Views")]
DRY = "--dry-run" in sys.argv

# alert( não precedido de . (evita GdiSwal2.alert, foo.alert)
ALERT_START = r"(?<!\.)alert\s*\("

REPLACEMENTS = [
    # alert("Erro [" + e + "]"); (catch com variável e)
    (
        re.compile(
            ALERT_START + r'"Erro \["\s*\+\s*e\s*\+\s*"\]"\s*\)\s*;',
            re.MULTILINE,
        ),
        r'LibMessageError("Atenção", "Erro [" + (e != null ? String(e) : "") + "]");',
    ),
    # Erro [tag] (msg)
    (
        re.compile(
            ALERT_START
            + r'"Erro \[([^\]]+)\]\s*\("\s*\+\s*err\.message\.toString\(\)\s*\+\s*"\)"\s*\)\s*;',
            re.MULTILINE,
        ),
        r'LibMessageError("Atenção", "Erro [\1] (" + (err && err.message ? err.message.toString() : String(err)) + ")");',
    ),
    # "literal" + err.message.toString()
    (
        re.compile(
            ALERT_START + r'"([^"]+)"\s*\+\s*err\.message\.toString\(\)\s*\)\s*;',
            re.MULTILINE,
        ),
        r'LibMessageError("Atenção", "\1" + (err && err.message ? err.message.toString() : String(err)));',
    ),
    # "literal" + err.message (sem .toString)
    (
        re.compile(
            ALERT_START + r'"([^"]+)"\s*\+\s*err\.message\s*\)\s*;',
            re.MULTILINE,
        ),
        r'LibMessageError("Atenção", "\1" + (err && err.message ? err.message.toString() : String(err)));',
    ),
    # "literal" + e.message.toString()
    (
        re.compile(
            ALERT_START + r'"([^"]+)"\s*\+\s*e\.message\.toString\(\)\s*\)\s*;',
            re.MULTILINE,
        ),
        r'LibMessageError("Atenção", "\1" + (e && e.message ? e.message.toString() : String(e)));',
    ),
    # alert(err.message);
    (
        re.compile(ALERT_START + r"err\.message\s*\)\s*;", re.MULTILINE),
        r'LibMessageError("Atenção", err && err.message ? String(err.message) : String(err));',
    ),
    # alert(err.toString());
    (
        re.compile(ALERT_START + r"err\.toString\(\)\s*\)\s*;", re.MULTILINE),
        r'LibMessageError("Atenção", err != null ? String(err) : "");',
    ),
]


def process_file(path):
    with open(path, "r", encoding="utf-8", errors="replace") as f:
        original = f.read()
    text = original
    for rx, sub in REPLACEMENTS:
        text = rx.sub(sub, text)
    if text != original:
        if not DRY:
            with open(path, "w", encoding="utf-8", newline="") as f:
                f.write(text)
        return True
    return False


def main():
    changed = []
    for root in ROOTS:
        if not os.path.isdir(root):
            continue
        for dirpath, _, files in os.walk(root):
            for fn in files:
                if not fn.endswith(".cshtml"):
                    continue
                path = os.path.join(dirpath, fn)
                if process_file(path):
                    changed.append(os.path.relpath(path, BASE))
    print("files_changed:", len(changed))
    for p in sorted(changed)[:80]:
        print(p)
    if len(changed) > 80:
        print("... and", len(changed) - 80, "more")
    if DRY:
        print("(dry-run: no files written)")
    sys.exit(0)


if __name__ == "__main__":
    main()
