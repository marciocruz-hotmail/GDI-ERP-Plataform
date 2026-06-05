# -*- coding: utf-8 -*-
"""G-PERF-20c-bis — Lista views _Layout que precisam partial Tempus (jsDatepicker ou hub de modal com datas)."""
from __future__ import print_function

import os
import re

BASE = os.path.normpath(os.path.join(os.path.dirname(__file__), ".."))
VIEW_ROOTS = [os.path.join(BASE, "Areas"), os.path.join(BASE, "Views")]

LAYOUT_LITE = re.compile(
    r"Layout\s*=\s*null|Layout\s*=\s*[\"']~/Views/Shared/_Modal|_Blank\.cshtml",
    re.I,
)
JS_DATE = re.compile(r"jsDatepicker|TempusDominus|tempus-dominus", re.I)
MAIN_MODAL = re.compile(r'[#$]?\s*\(\s*["\']#mainModal["\']\s*\)\s*\.\s*load', re.I)

# Modais com jsDatepicker → action name fragment
MODAL_ACTIONS = {}


def iter_cshtml():
    for root in VIEW_ROOTS:
        if not os.path.isdir(root):
            continue
        for dp, _, fns in os.walk(root):
            for fn in fns:
                if fn.endswith(".cshtml"):
                    yield os.path.join(dp, fn)


def scan():
    by_path = {}
    modal_with_date = []
    for path in sorted(iter_cshtml()):
        rel = os.path.relpath(path, BASE).replace("\\", "/")
        if rel.startswith("Views/Shared/_Layout") or "Shared/_LayoutHeadTempus" in rel:
            continue
        with open(path, encoding="utf-8", errors="ignore") as f:
            text = f.read()
        has_date = bool(JS_DATE.search(text))
        if not has_date:
            continue
        lite = bool(LAYOUT_LITE.search(text))
        is_modal = "/Modal" in rel or rel.endswith("Modal.cshtml")
        by_path[rel] = {"lite": lite, "modal": is_modal, "has_date": True}
        if is_modal:
            modal_with_date.append(rel)

    # hosts: full layout pages with jsDatepicker OR mainModal.load (hub)
    hosts_direct = []
    hosts_hub = []
    for path in sorted(iter_cshtml()):
        rel = os.path.relpath(path, BASE).replace("\\", "/")
        if rel.startswith("Views/Shared/_Layout"):
            continue
        with open(path, encoding="utf-8", errors="ignore") as f:
            text = f.read()
        if LAYOUT_LITE.search(text):
            continue
        is_modal = "/Modal" in rel
        if is_modal:
            continue
        has_date = bool(JS_DATE.search(text))
        has_modal_load = bool(MAIN_MODAL.search(text))
        if has_date:
            hosts_direct.append(rel)
        elif has_modal_load:
            # hub: check if any loaded modal has jsDatepicker
            for mrel in modal_with_date:
                mname = os.path.basename(mrel).replace(".cshtml", "")
                if mname in text:
                    hosts_hub.append(rel)
                    break

    return sorted(set(hosts_direct)), sorted(set(hosts_hub)), modal_with_date


if __name__ == "__main__":
    direct, hub, modals = scan()
    print("HOSTS_DIRECT (%d):" % len(direct))
    for p in direct:
        print(" ", p)
    print("HOSTS_HUB (%d):" % len(hub))
    for p in hub:
        print(" ", p)
    print("MODALS_WITH_DATE (%d):" % len(modals))
    print("TOTAL_HOSTS:", len(set(direct + hub)))
