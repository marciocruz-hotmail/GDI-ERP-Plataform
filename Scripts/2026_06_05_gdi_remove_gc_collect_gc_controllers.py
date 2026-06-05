#!/usr/bin/env python3
"""Remove GC.Collect / WaitForPendingFinalizers de Areas/gc/Controllers (lote N-E)."""
import glob
import os
import re

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CTRL = os.path.join(REPO, "Areas", "gc", "Controllers")

TRIPLE = re.compile(
    r"\r?\n[ \t]*GC\.Collect\(\);\r?\n[ \t]*GC\.WaitForPendingFinalizers\(\);\r?\n[ \t]*GC\.Collect\(\);\r?\n",
    re.MULTILINE,
)
SINGLE = re.compile(r"^[ \t]*GC\.Collect\(\);\r?\n", re.MULTILINE)


def clean(text):
    before = text
    text = TRIPLE.sub("\n", text)
    while True:
        n = SINGLE.sub("", text)
        if n == text:
            break
        text = n
    return text, text != before


def main():
    changed = []
    for path in sorted(glob.glob(os.path.join(CTRL, "*.cs"))):
        text = open(path, encoding="utf-8", errors="replace").read()
        if "GC.Collect" not in text:
            continue
        new_text, ok = clean(text)
        if ok:
            open(path, "w", encoding="utf-8", newline="\n").write(new_text)
            rel = os.path.relpath(path, REPO).replace("\\", "/")
            n = text.count("GC.Collect") - new_text.count("GC.Collect")
            changed.append((rel, n))
    print(f"Updated: {len(changed)}")
    for rel, n in changed:
        print(f"  {rel} (-{n} GC.Collect)")


if __name__ == "__main__":
    main()
