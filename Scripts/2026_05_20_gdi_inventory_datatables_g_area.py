# -*- coding: utf-8 -*-
"""
Inventário de actions DataTables em Areas/g/Controllers (cadastros g).
Inclui GetDados* (PascalCase) e getDados* (legado DT-1).

Uso: python Scripts/2026_05_20_gdi_inventory_datatables_g_area.py
"""
import os
import re
import sys

BASE = os.path.normpath(os.path.join(os.path.dirname(__file__), ".."))
ROOT = os.path.join(BASE, "Areas", "g", "Controllers")

RX_METHOD = re.compile(
    r"public\s+ActionResult\s+((?:Get|get)Dados\w*)\s*\(\s*jQueryDataTableParamModel\s+param\s*\)",
    re.IGNORECASE,
)
RX_JSON_EX = re.compile(r"JsonDataTableException", re.IGNORECASE)
RX_PARAM_NULL = re.compile(
    r"if\s*\(\s*param\s*==\s*null\s*\)\s*\{?\s*param\s*=\s*new\s+jQueryDataTableParamModel\s*\(\s*\)\s*;?\s*\}?",
    re.IGNORECASE,
)
RX_CATCH_JSON_EX = re.compile(
    r"catch\s*\([^)]*\)\s*\{[^}]*JsonDataTableException",
    re.IGNORECASE | re.DOTALL,
)
# Contrato gráfico / não-aaData — catch inline com errorMessage (doc DT-1)
ALLOW_NO_JSON_DT_EX = frozenset(
    {
        ("Areas/g/Controllers/FinanceiroController.cs", "GetDadosGrafico"),
    }
)


def slice_after_getdados(body, start_pos):
    """Trecho do método até próximo '#region' ou 'public ' (heurístico)."""
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
    legacy_names = []
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

            for m in RX_METHOD.finditer(body):
                name = m.group(1)
                line_no = body[: m.start()].count("\n") + 1
                chunk = slice_after_getdados(body, m.start())
                has_try = "try" in chunk and "catch" in chunk
                has_param_null = bool(RX_PARAM_NULL.search(chunk))
                catch_uses_json_ex = bool(RX_CATCH_JSON_EX.search(chunk))
                is_pascal = name.startswith("GetDados")
                if not is_pascal:
                    legacy_names.append((rel.replace("/", os.sep), line_no, name))

                rows.append(
                    (
                        rel.replace("/", os.sep),
                        line_no,
                        name,
                        "Y" if file_has_json_ex else "N",
                        "Y" if has_param_null else "N",
                        "Y" if has_try else "N",
                        "Y" if catch_uses_json_ex else "N",
                        "legacy" if not is_pascal else "std",
                    )
                )

    print(
        "file\tline\tmethod\tJsonEx(file)\tparam_null(method)\ttry\tcatch_JsonEx\tname_kind"
    )
    for r in sorted(rows, key=lambda x: (x[0], x[1])):
        print("\t".join(str(x) for x in r))

    print("\nTotal (Get|get)Dados* (jQueryDataTableParamModel):", len(rows))
    if legacy_names:
        print("\nNomes legados (getDados*, não renomear URLs nas views):")
        for rel, ln, nm in sorted(legacy_names):
            print("  %s:%s\t%s" % (rel, ln, nm))
    else:
        print("\nNenhum nome legado getDados* encontrado.")

    missing = []
    for r in rows:
        rel_norm = r[0].replace(os.sep, "/")
        key = (rel_norm, r[2])
        if key in ALLOW_NO_JSON_DT_EX:
            continue
        if r[5] == "N" or r[6] == "N" or r[3] == "N":
            missing.append(r)
    if missing:
        print("\nAtenção — revisar contrato DataTables:")
        for r in missing:
            print("  %s:%s %s try=%s catch_JsonEx=%s file_JsonEx=%s" % (r[0], r[1], r[2], r[5], r[6], r[3]))
        sys.exit(1)

    sys.exit(0)


if __name__ == "__main__":
    main()
