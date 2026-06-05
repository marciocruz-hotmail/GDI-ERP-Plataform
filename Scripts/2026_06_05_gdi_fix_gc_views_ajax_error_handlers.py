# -*- coding: utf-8 -*-
"""Lotes I-A / I-B — handlers Ajax em Areas/gc/Views (recursivo, idempotente).

- error: function (result) -> error: function (xhr, textStatus, errorThrown)
- GdiAjaxNotifyInconsistencias(result.msg|result.toString()) -> padrão rede ou unified result.msg
"""
import glob
import os
import re
import sys

_REPO = os.path.dirname(os.path.dirname(__file__))
DEFAULT_ROOT = os.path.join(_REPO, "Areas", "gc", "Views")


def resolve_root(argv):
    if len(argv) > 1:
        arg = argv[1]
        if os.path.isdir(arg):
            return arg, arg
        return os.path.join(_REPO, "Areas", arg, "Views"), arg
    return DEFAULT_ROOT, "gc/Views"

GDI_NOTIFY_UNIFIED = (
    "GdiAjaxNotifyInconsistencias((typeof result !== 'undefined' && result && result.msg) "
    "? result.msg : (errorThrown || textStatus || 'Verifique as inconsistências.'))"
)


def patch_notify_inconsistencias(content):
    patterns = [
        (
            "GdiAjaxNotifyInconsistencias(errorThrown || textStatus || 'Falha ao confirmar expedição.')",
            "GdiAjaxNotifyInconsistencias((typeof result !== 'undefined' && result && result.msg) "
            "? result.msg : (errorThrown || textStatus || 'Falha ao confirmar expedição.'))",
        ),
        (
            "GdiAjaxNotifyInconsistencias(errorThrown || textStatus || 'Falha ao sincronizar notas fiscais.')",
            "GdiAjaxNotifyInconsistencias(errorThrown || textStatus || 'Falha ao sincronizar notas fiscais.')",
        ),
        (
            "GdiAjaxNotifyInconsistencias(errorThrown || textStatus || 'Falha ao sincronizar cartas de correção.')",
            "GdiAjaxNotifyInconsistencias(errorThrown || textStatus || 'Falha ao sincronizar cartas de correção.')",
        ),
        (
            "GdiAjaxNotifyInconsistencias(errorThrown || textStatus);",
            GDI_NOTIFY_UNIFIED + ";",
        ),
        (
            "GdiAjaxNotifyInconsistencias(errorThrown || textStatus)",
            GDI_NOTIFY_UNIFIED,
        ),
    ]
    for old, new in patterns:
        content = content.replace(old, new)
    return content


def patch(content):
    content = re.sub(
        r"error:\s*function\s*\(\s*result\s*\)",
        "error: function (xhr, textStatus, errorThrown)",
        content,
    )
    content = content.replace(
        "GdiAjaxNotifyInconsistencias(result.toString())",
        "GdiAjaxNotifyInconsistencias(errorThrown || textStatus)",
    )
    content = content.replace(
        "GdiAjaxNotifyInconsistencias(result.msg)",
        GDI_NOTIFY_UNIFIED,
    )
    content = re.sub(
        r'LibMessageError\("Atenção",\s*result\s*&&\s*result\.msg\s*!=\s*null\s*\?\s*String\(result\.msg\)\s*:\s*\(result\s*&&\s*result\.statusText\s*\?\s*String\(result\.statusText\)\s*:\s*"Erro na requisição\."\)\);',
        GDI_NOTIFY_UNIFIED + ";",
        content,
    )
    return patch_notify_inconsistencias(content)


def main():
    base, label = resolve_root(sys.argv)
    paths = sorted(glob.glob(os.path.join(base, "**", "*.cshtml"), recursive=True))
    changed = []
    for path in paths:
        with open(path, "r", encoding="utf-8") as f:
            original = f.read()
        updated = patch(original)
        if updated != original:
            with open(path, "w", encoding="utf-8", newline="\r\n") as f:
                f.write(updated)
            changed.append(os.path.relpath(path, base).replace("\\", "/"))
    print("Changed %d file(s) under Areas/%s" % (len(changed), label))
    for name in changed:
        print(" -", name)


if __name__ == "__main__":
    main()
