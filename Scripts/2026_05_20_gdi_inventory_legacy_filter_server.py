# -*- coding: utf-8 -*-
"""Lista referências servidor a yesFilterOperador/Text/AdvancedText e SentencaSQLFiltroGenerico."""
import os
import re

ROOT = os.path.join(os.path.dirname(__file__), "..")
patterns = [
    ("yesFilterAdvancedText", re.compile(r"yesFilterAdvancedText")),
    ("yesFilterOperador", re.compile(r"yesFilterOperador")),
    ("yesFilterText(?!OnOff)", re.compile(r"yesFilterText")),
    ("SentencaSQLFiltroGenerico", re.compile(r"SentencaSQLFiltroGenerico")),
]
skip_dirs = {".git", "bin", "obj", "packages", "node_modules"}

for label, rx in patterns:
    hits = []
    for dirpath, dirnames, files in os.walk(ROOT):
        dirnames[:] = [d for d in dirnames if d not in skip_dirs]
        for fname in files:
            if not fname.endswith((".cs", ".cshtml", ".js")):
                continue
            path = os.path.join(dirpath, fname)
            rel = os.path.relpath(path, ROOT).replace("\\", "/")
            try:
                text = open(path, encoding="utf-8", errors="replace").read()
            except OSError:
                continue
            if rx.search(text):
                hits.append(rel)
    print("=== %s (%d) ===" % (label, len(hits)))
    for h in sorted(hits):
        print(h)
    print()
