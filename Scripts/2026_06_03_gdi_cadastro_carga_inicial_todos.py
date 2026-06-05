#!/usr/bin/env python3
"""Grupo 6 — cadastros: carga inicial com yesFilterField='*' (exceto Produtos/Clientes)."""
import os
import re

ROOT = os.path.join(os.path.dirname(__file__), "..")
EXCLUDE = {
    "Areas/g/Views/Produtos/Index.cshtml",
    "Areas/g/Views/Clientes/Index.cshtml",
}
INIT_BLOCK = """            @if (ViewBag.RestoreFilterAutoSearch != true)
            {
                <text>document.getElementById("yesFilterField").value = "*";</text>
            }
"""

def norm(p):
    return p.replace("\\", "/")

def process(path):
    rel = norm(os.path.relpath(path, ROOT))
    if rel in EXCLUDE:
        return "skip_excluded", rel
    text = open(path, encoding="utf-8").read()
    if "deferLoading: true" not in text:
        return "skip_no_defer", rel
    if 'document.getElementById("yesFilterField").value = "*"' in text and "RestoreFilterAutoSearch != true" in text:
        # já tem init; só remover deferLoading
        new_text = re.sub(r"\n\s*deferLoading:\s*true,\s*", "\n", text, count=1)
        if new_text == text:
            return "skip_defer_only_fail", rel
        open(path, "w", encoding="utf-8", newline="\r\n").write(new_text)
        return "defer_only", rel

    # inserir init antes de jsCreateTable() no document.ready
    m = re.search(r"(jsInitForm\(\);\s*\r?\n)(\s*)(jsCreateTable\(\);)", text)
    if not m:
        return "fail_no_anchor", rel
    insert_at = m.start(3)
    new_text = text[: m.start(3)] + INIT_BLOCK + text[m.start(2) :]
    new_text = re.sub(r"\n\s*deferLoading:\s*true,\s*", "\n", new_text, count=1)
    open(path, "w", encoding="utf-8", newline="\r\n").write(new_text)
    return "ok", rel

changed = []
for dirpath, _, files in os.walk(os.path.join(ROOT, "Areas")):
    for f in files:
        if f != "Index.cshtml":
            continue
        path = os.path.join(dirpath, f)
        status, rel = process(path)
        if status in ("ok", "defer_only"):
            changed.append((status, rel))
        elif status.startswith("fail"):
            print(f"FAIL {status}: {rel}")

print(f"Updated {len(changed)} files:")
for s, r in sorted(changed):
    print(f"  [{s}] {r}")


def fix_indent():
    pat = re.compile(
        r"(jsInitForm\(\);\s*\r?\n)\s+(@if \(ViewBag\.RestoreFilterAutoSearch != true\)\s*\r?\n\s*\{\s*\r?\n\s*<text>document\.getElementById\(\"yesFilterField\"\)\.value = \"\*\";</text>\s*\r?\n\s*\})",
        re.MULTILINE,
    )
    repl = r"\1            \2"
    for dirpath, _, files in os.walk(os.path.join(ROOT, "Areas")):
        for f in files:
            if f != "Index.cshtml":
                continue
            path = os.path.join(dirpath, f)
            t = open(path, encoding="utf-8").read()
            new_t, n = pat.subn(repl, t, count=1)
            if not n:
                continue
            new_t = re.sub(r"(\}\)\.DataTable\(\{\r?\n)(processing:)", r"\1            processing:", new_t)
            open(path, "w", encoding="utf-8", newline="\r\n").write(new_t)
            print("fixed indent:", norm(os.path.relpath(path, ROOT)))


if __name__ == "__main__" and os.environ.get("GDI_FIX_INDENT") == "1":
    fix_indent()
