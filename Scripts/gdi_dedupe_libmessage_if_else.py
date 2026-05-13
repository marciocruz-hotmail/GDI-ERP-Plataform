# -*- coding: utf-8 -*-
"""Remove if (typeof LibMessageError...) { LibMessageError(...); } else { LibMessageError(...); } duplicado."""
from __future__ import print_function

import os
import re
import sys

BASE = os.path.normpath(os.path.join(os.path.dirname(__file__), ".."))
ROOTS = [os.path.join(BASE, "Areas"), os.path.join(BASE, "Views")]

RX = re.compile(
    r"if\s*\(\s*typeof\s+LibMessageError\s*===\s*[\"']function[\"']\s*\)\s*"
    r"\{\s*LibMessageError\s*\(\s*[\"']Atenção[\"']\s*,\s*([^;]+)\);\s*\}\s*"
    r"else\s*\{\s*LibMessageError\s*\(\s*[\"']Atenção[\"']\s*,\s*\1\s*\);\s*\}"
)


def main():
    total = 0
    files = 0
    for root in ROOTS:
        if not os.path.isdir(root):
            continue
        for dp, _, fns in os.walk(root):
            for fn in fns:
                if not fn.endswith(".cshtml"):
                    continue
                path = os.path.join(dp, fn)
                with open(path, "r", encoding="utf-8", errors="replace") as f:
                    s = f.read()
                def repl(m):
                    return 'LibMessageError("Atenção", ' + m.group(1).strip() + ");"

                ns, k = RX.subn(repl, s)
                if k:
                    total += k
                    files += 1
                    with open(path, "w", encoding="utf-8", newline="") as f:
                        f.write(ns)
                    print(os.path.relpath(path, BASE), k)
    print("replacements:", total, "files:", files)
    sys.exit(0)


if __name__ == "__main__":
    main()
