# -*- coding: utf-8 -*-
"""Auditoria: views com jsDatepicker* vs partials Tempus (_LayoutHead/ScriptsTempus)."""
from __future__ import print_function

import os
import re

BASE = os.path.normpath(os.path.join(os.path.dirname(__file__), ".."))
JS_DATE = re.compile(
    r"jsDatepicker\w*|jsInitDateTimepicker", re.I
)
LAYOUT_FULL = re.compile(r"~/Views/Shared/_Layout\.cshtml", re.I)
LAYOUT_MODAL = re.compile(r"_Modal\.cshtml|Layout\s*=\s*null", re.I)
HEAD_MARK = "_LayoutHeadTempus"
SCRIPTS_MARK = "_LayoutScriptsTempus"

hosts_missing = []
modals_with_date = []
indexes_first_day = []


def iter_areas_cshtml():
    root = os.path.join(BASE, "Areas")
    for dp, _, fns in os.walk(root):
        for fn in fns:
            if fn.endswith(".cshtml"):
                yield os.path.join(dp, fn)


for path in sorted(iter_areas_cshtml()):
    rel = os.path.relpath(path, BASE).replace("\\", "/")
    with open(path, encoding="utf-8", errors="ignore") as f:
        text = f.read()
    if not JS_DATE.search(text):
        continue
    is_modal = "/Modal" in rel or LAYOUT_MODAL.search(text)
    is_host = LAYOUT_FULL.search(text) and not is_modal
    has_head = HEAD_MARK in text
    has_scripts = SCRIPTS_MARK in text
    calls = sorted(set(m.group(0) for m in JS_DATE.finditer(text)))

    if "jsDatepickerFirstDayMonth" in " ".join(calls) or "jsDatepickerLastDayMonth" in " ".join(calls):
        indexes_first_day.append(rel)

    if is_modal:
        modals_with_date.append((rel, calls))
    elif is_host:
        if not (has_head and has_scripts):
            hosts_missing.append((rel, has_head, has_scripts, calls))
    else:
        print("OTHER", rel, calls)

print("=== INDEXES com FirstDayMonth/LastDayMonth (alerta reportado) ===")
for r in indexes_first_day:
    print(" ", r)

print("\n=== HOSTS _Layout com datepicker SEM partials Tempus (%d) ===" % len(hosts_missing))
for rel, h, s, calls in hosts_missing:
    print("  %s  head=%s scripts=%s  calls=%s" % (rel, h, s, ", ".join(calls[:4])))

print("\n=== MODAIS com datepicker (%d) — dependem do host ou GdiMainModalLoad ===" % len(modals_with_date))
for rel, calls in modals_with_date[:15]:
    print("  %s  %s" % (rel, ", ".join(calls[:3])))
if len(modals_with_date) > 15:
    print("  ... +%d modais" % (len(modals_with_date) - 15))
