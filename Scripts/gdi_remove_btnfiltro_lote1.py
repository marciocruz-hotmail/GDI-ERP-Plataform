# -*- coding: utf-8 -*-
"""Lote 1: remove btnFiltro de views sem #btnFiltroDefault (Fase migração start.js)."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
EXCLUDE = {
    ROOT / "Areas/g/Views/Nfe/Index.cshtml",
    ROOT / "Areas/g/Views/ProdutosTipos/Index.cshtml",
    ROOT / "Areas/gc/Views/ComexProdutos/Index.cshtml",
    ROOT / "Areas/gc/Views/ComexProdutos/ProdutosPre.cshtml",
}

MULTILINE_OLD = """                .on('xhr.dt', function (e, settings, json, xhr) {
                    if (GdiDtNotifyJsonErrorMessage(json)) { }
                    else if (json && json.yesFilterOnOff !== undefined) {
                        btnFiltro(json.yesFilterOnOff);
                    }
                })"""

MULTILINE_OLD2 = """                .on('xhr.dt', function (e, settings, json, xhr) {
                    if (GdiDtNotifyJsonErrorMessage(json)) { }
                    else if (json && json.yesFilterOnOff !== undefined) {
                        btnFiltro(json.yesFilterOnOff);
                    }
                }"""

MULTILINE_NEW = """                .on('xhr.dt', function (e, settings, json, xhr) {
                    if (GdiDtNotifyJsonErrorMessage(json)) { return; }
                })"""

REPLACEMENTS = [
    (
        "GdiDtNotifyJsonErrorMessage(json); btnFiltro(json.yesFilterOnOff)",
        "if (GdiDtNotifyJsonErrorMessage(json)) { return; }",
    ),
    (
        "GdiDtNotifyJsonErrorMessage(json); btnFiltro(json.yesFilterOnOff);",
        "if (GdiDtNotifyJsonErrorMessage(json)) { return; }",
    ),
    (
        "if (GdiDtNotifyJsonErrorMessage(json)) { } else if (json && json.yesFilterOnOff !== undefined) { btnFiltro(json.yesFilterOnOff); }",
        "if (GdiDtNotifyJsonErrorMessage(json)) { return; }",
    ),
    ("            btnFiltro(yesFilterOnOff);\n", ""),
]

changed = []
for path in ROOT.glob("Areas/**/*.cshtml"):
    if path.resolve() in {p.resolve() for p in EXCLUDE}:
        continue
    try:
        text = path.read_text(encoding="utf-8")
    except UnicodeDecodeError:
        text = path.read_text(encoding="cp1252")
    if "btnFiltro" not in text:
        continue
    orig = text
    if MULTILINE_OLD in text:
        text = text.replace(MULTILINE_OLD, MULTILINE_NEW)
    for old, new in REPLACEMENTS:
        text = text.replace(old, new)
    if text != orig:
        path.write_text(text, encoding="utf-8", newline="\r\n")
        changed.append(str(path.relative_to(ROOT)))

print("Changed", len(changed), "files:")
for f in sorted(changed):
    print(" ", f)

remaining = []
for path in ROOT.glob("Areas/**/*.cshtml"):
    if path.resolve() in {p.resolve() for p in EXCLUDE}:
        continue
    try:
        content = path.read_text(encoding="utf-8")
    except UnicodeDecodeError:
        content = path.read_text(encoding="cp1252")
    if "btnFiltro" in content:
        remaining.append(str(path.relative_to(ROOT)))
if remaining:
    print("Still have btnFiltro:", remaining)
else:
    print("No btnFiltro left outside batch2 files")
