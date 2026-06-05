# -*- coding: utf-8 -*-
"""Lote G-FLT-05/06 + G-UX-01: getFilterByUser 3 args; LibFlashMessage.SetModalMessage."""
import os
import re

ROOT = os.path.normpath(os.path.join(os.path.dirname(__file__), ".."))
SKIP = {"bin", "obj", "_filestemp", "packages", ".git"}

RX_GET_FILTER = re.compile(
    r"LibDB\.getFilterByUser\(\s*([^,]+)\s*,\s*([^,]+)\s*,\s*(?:false|true|filterAdvanced)\s*,\s*([^)]+)\)",
    re.I,
)
RX_FLASH = re.compile(
    r'TempData\["message"\]\s*=\s*([^;]+);\s*\r?\n\s*TempData\.Keep\("message"\);',
    re.MULTILINE,
)
RX_FLASH_SINGLE = re.compile(
    r'TempData\["message"\]\s*=\s*([^;]+);',
)


def walk_cs():
    for dirpath, dirnames, files in os.walk(ROOT):
        dirnames[:] = [d for d in dirnames if d not in SKIP]
        for fn in files:
            if fn.endswith(".cs"):
                yield os.path.join(dirpath, fn)


def main():
    for path in walk_cs():
        rel = os.path.relpath(path, ROOT)
        with open(path, "r", encoding="utf-8", errors="ignore") as f:
            text = f.read()
        orig = text

        text = RX_GET_FILTER.sub(r"LibDB.getFilterByUser(\1, \2, \3)", text)
        text = RX_FLASH.sub(r"LibFlashMessage.SetModalMessage(this, \1);", text)

        if rel == os.path.join("Areas", "g", "Controllers", "NfeController.cs").replace("\\", os.sep):
            pass
        elif "Controllers" in rel and RX_FLASH_SINGLE.search(text) and "LibFlashMessage" not in text:
            # Nfe: single-line TempData message without Keep
            text = RX_FLASH_SINGLE.sub(r"LibFlashMessage.SetModalMessage(this, \1);", text)

        if text != orig:
            with open(path, "w", encoding="utf-8", newline="\r\n") as f:
                f.write(text)
            print("updated:", rel.replace("\\", "/"))


if __name__ == "__main__":
    main()
