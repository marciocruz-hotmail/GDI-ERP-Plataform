# -*- coding: utf-8 -*-
"""Inventário: modais modal-dialog-scrollable + DataTable — candidatos ao fix scroll horizontal."""
import re
import sys
from pathlib import Path

BASE = Path(__file__).resolve().parent.parent / "Areas"


def main():
    modals = sorted(BASE.rglob("Modal*.cshtml"))
    rows = []
    for p in modals:
        t = p.read_text(encoding="utf-8", errors="replace")
        if ".DataTable" not in t and "DataTable({" not in t:
            continue
        if "modal-dialog-scrollable" not in t:
            continue
        rel = p.as_posix()
        m = re.search(r'id\s*=\s*"(Form[^"]+)"', t)
        fid = m.group(1) if m else "?"
        has_fix = "overflow-x: hidden" in t or "overflow-x:hidden" in t
        rows.append(
            (
                rel,
                fid,
                has_fix,
                "gdi-dt-scroll-host" in t,
                "scroll-body-horizontal" in t,
                len(re.findall(r'id="dt[A-Za-z0-9_]+"', t)),
            )
        )

    needs = [r for r in rows if not r[2]]
    print(f"Total scrollable+DT: {len(rows)} | sem overflow-x hidden: {len(needs)}\n")
    for rel, fid, has_fix, host, sb, n_dt in rows:
        st = "OK" if has_fix else "FIX"
        print(f"{st} dt={n_dt} host={host} sb={sb} | {fid} | {rel}")
    return 1 if needs else 0


if __name__ == "__main__":
    sys.exit(main())
