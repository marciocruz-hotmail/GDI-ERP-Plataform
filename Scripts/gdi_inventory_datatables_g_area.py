# -*- coding: utf-8 -*-
"""
Inventário de actions GetDados* em Areas/g/Controllers (cadastros g).
Lista assinaturas e heurísticas: JsonDataTableException, param nulo, try em GetDados.

Uso: python Scripts/gdi_inventory_datatables_g_area.py
"""
import os
import re
import sys

BASE = os.path.normpath(os.path.join(os.path.dirname(__file__), ".."))
ROOT = os.path.join(BASE, "Areas", "g", "Controllers")

RX_METHOD = re.compile(
    r"public\s+ActionResult\s+(GetDados\w*)\s*\(\s*jQueryDataTableParamModel\s+param\s*\)",
    re.IGNORECASE,
)
RX_JSON_EX = re.compile(r"JsonDataTableException", re.IGNORECASE)
RX_PARAM_NULL = re.compile(
    r"if\s*\(\s*param\s*==\s*null\s*\)\s*param\s*=\s*new\s+jQueryDataTableParamModel\s*\(\s*\)\s*;",
    re.IGNORECASE,
)


def slice_after_getdados(body, start_pos):
    """Trecho do método GetDados até próximo '#region' ou 'public ' no nível de classe (heurístico)."""
    sub = body[start_pos:]
    m = re.search(r"\n\s*#region|\n\s*public\s+(?:ActionResult|void|JsonResult)", sub[1:])
    if m:
        return sub[: m.start() + 1]
    return sub


def main():
    if not os.path.isdir(ROOT):
        print("missing:", ROOT)
        sys.exit(2)

    rows = []
    for dirpath, _, files in os.walk(ROOT):
        for fn in sorted(files):
            if not fn.endswith(".cs"):
                continue
            path = os.path.join(dirpath, fn)
            rel = os.path.relpath(path, BASE)
            try:
                with open(path, "r", encoding="utf-8", errors="ignore") as f:
                    body = f.read()
            except OSError as e:
                print(rel, e)
                continue

            file_has_json_ex = bool(RX_JSON_EX.search(body))
            file_has_param_null = bool(RX_PARAM_NULL.search(body))

            for m in RX_METHOD.finditer(body):
                name = m.group(1)
                line_no = body[: m.start()].count("\n") + 1
                chunk = slice_after_getdados(body, m.start())
                has_try = "try" in chunk and "catch" in chunk
                rows.append(
                    (
                        rel.replace("/", os.sep),
                        line_no,
                        name,
                        "Y" if file_has_json_ex else "N",
                        "Y" if file_has_param_null else "N",
                        "Y" if has_try else "N",
                    )
                )

    print(
        "file\tline\tmethod\tJsonDataTableException(file)\tparam_null(file)\ttry_in_method(heuristic)"
    )
    for r in sorted(rows, key=lambda x: (x[0], x[1])):
        print("\t".join(str(x) for x in r))

    print("\nTotal GetDados* (jQueryDataTableParamModel):", len(rows))
    sys.exit(0)


if __name__ == "__main__":
    main()
