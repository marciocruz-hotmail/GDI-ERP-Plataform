# -*- coding: utf-8 -*-
"""One-off: split LookupQueryService.Wave6a.cs into domain partials (1.8.2)."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
src_path = ROOT / "Lib" / "Lookups" / "LookupQueryService.Wave6a.cs"
lines = src_path.read_text(encoding="utf-8").splitlines(keepends=True)


def extract(start: int, end: int) -> str:
    return "".join(lines[start - 1 : end])


HEADER = """using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using GdiPlataform.Areas.gc.Models;
using GdiPlataform.Db;
using GdiPlataform.Lib;
using GdiPlataform.Security;

namespace GdiPlataform.Lib.Lookups
{
"""

FOOTER = "    }\n}\n"

SPLITS = {
    "LookupQueryService.Comercial.cs": (
        "    /// <summary>Comercial / movimentos / COMEX / CFOP / contatos pedido (ex-Onda 6a).</summary>\n"
        "    public sealed partial class LookupQueryService\n    {\n",
        [ (15, 34), (108, 183), (292, 378) ],
    ),
    "LookupQueryService.Financeiro.cs": (
        "    /// <summary>Financeiro / pagrec / contas caixas gerencial (ex-Onda 6a).</summary>\n"
        "    public sealed partial class LookupQueryService\n    {\n",
        [ (36, 106), (380, 403) ],
    ),
    "LookupQueryService.CadastrosG.cs": (
        "    /// <summary>Cadastros g + atendimentos + produtos fiscais (ex-Onda 6a).</summary>\n"
        "    public sealed partial class LookupQueryService\n    {\n",
        [ (185, 290), (405, 411) ],
    ),
}

out_dir = ROOT / "Lib" / "Lookups"
for fname, (mid, ranges) in SPLITS.items():
    body = mid + "".join(extract(a, b) for a, b in ranges) + FOOTER
    (out_dir / fname).write_text(HEADER + body, encoding="utf-8", newline="\r\n")
    print("wrote", fname)
