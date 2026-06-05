# -*- coding: utf-8 -*-
"""Lotes I-C / I-D — error.dt e xhr.dt em inits DataTables (Areas/*/Views)."""
import glob
import os
import re
import sys

_REPO = os.path.dirname(os.path.dirname(__file__))
DEFAULT_ROOT = os.path.join(_REPO, "Areas", "gc", "Views")


def resolve_root():
    if len(sys.argv) > 1:
        arg = sys.argv[1]
        if os.path.isdir(arg):
            return arg
        return os.path.join(_REPO, "Areas", arg, "Views")
    return DEFAULT_ROOT

ERROR_DT = (
    ".on('error.dt', function (e, settings, techNote, message) { GdiDtNotifyLoadFailure(message); })"
)
XHR_DT = (
    ".on('xhr.dt', function (e, settings, json, xhr) { GdiDtNotifyJsonErrorMessage(json); })"
)

HAS_INIT = re.compile(r"\.[Dd]ata[Tt]able\s*\(\s*\{")
HAS_XHR = re.compile(r"xhr\.dt")
HAS_ERROR = re.compile(r"error\.dt")

# error.dt presente, falta xhr.dt antes de .DataTable({
ERROR_ONLY = (
    ".on('error.dt', function (e, settings, techNote, message) { GdiDtNotifyLoadFailure(message); }).DataTable({"
)
ERROR_ONLY_LOWER = (
    ".on('error.dt', function (e, settings, techNote, message) { GdiDtNotifyLoadFailure(message); }).dataTable({"
)

# init direto $(...).DataTable({
DIRECT_INIT = re.compile(
    r"(\$\(\s*['\"][^'\"]+['\"]\s*\))\.DataTable\s*\(\s*\{"
)
DIRECT_INIT_LOWER = re.compile(
    r"(\$\(\s*['\"][^'\"]+['\"]\s*\))\.dataTable\s*\(\s*\{"
)


MULTILINE_ERROR_THEN_DT = re.compile(
    r"(\.on\('error\.dt',\s*function\s*\([^)]*\)\s*\{[^}]*GdiDtNotifyLoadFailure\([^)]*\);\s*\}\s*\))\s*(\.[Dd]ata[Tt]able\s*\(\s*\{)",
    re.MULTILINE,
)


def patch(content):
    if not HAS_INIT.search(content):
        return content

    if ERROR_ONLY in content:
        content = content.replace(
            ERROR_ONLY,
            ERROR_ONLY.replace(".DataTable({", XHR_DT + ".DataTable({"),
        )
    if ERROR_ONLY_LOWER in content:
        content = content.replace(
            ERROR_ONLY_LOWER,
            ERROR_ONLY_LOWER.replace(".dataTable({", XHR_DT + ".dataTable({"),
        )

    def add_xhr_multiline(m):
        between = content[m.start() : m.end()]
        if "xhr.dt" in between:
            return m.group(0)
        return m.group(1) + XHR_DT + m.group(2)

    content = MULTILINE_ERROR_THEN_DT.sub(
        lambda m: m.group(1) + XHR_DT + m.group(2)
        if "xhr.dt" not in m.group(0)
        else m.group(0),
        content,
    )

    def add_both(m):
        return m.group(1) + ERROR_DT + XHR_DT + ".DataTable({"

    def add_both_lower(m):
        return m.group(1) + ERROR_DT + XHR_DT + ".dataTable({"

    def repl_direct(rx, repl, text):
        out = []
        last = 0
        for m in rx.finditer(text):
            prefix = text[max(0, m.start() - 400) : m.start()]
            if "error.dt" in prefix[-400:] or "xhr.dt" in prefix[-400:]:
                continue
            out.append(text[last : m.start()])
            out.append(repl(m))
            last = m.end()
        out.append(text[last:])
        return "".join(out)

    content = repl_direct(DIRECT_INIT, add_both, content)
    content = repl_direct(DIRECT_INIT_LOWER, add_both_lower, content)
    return content


def main():
    root = resolve_root()
    changed = []
    for path in sorted(glob.glob(os.path.join(root, "**", "*.cshtml"), recursive=True)):
        with open(path, "r", encoding="utf-8") as f:
            original = f.read()
        updated = patch(original)
        if updated != original:
            with open(path, "w", encoding="utf-8", newline="\r\n") as f:
                f.write(updated)
            changed.append(os.path.relpath(path, root).replace("\\", "/"))
    print("Changed %d file(s) under %s:" % (len(changed), root))
    for name in changed:
        print(" -", name)


if __name__ == "__main__":
    main()
