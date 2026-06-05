#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""PROD-000 — baseline CACHE-PROD (P0): inventário + contagens SQL + estimativas HTML."""

from __future__ import annotations

import argparse
import json
import re
import sys
from datetime import datetime, timezone
from pathlib import Path
from xml.etree import ElementTree

ROOT = Path(__file__).resolve().parents[1]
PATTERNS = (
    "GetComboGcProdutosServicosTodos",
    "GetComboGcProdutosServicosImportados",
    "GetDatasetGcProdutosServicos",
    "GetComboGcProdutosPosicaoEstoqueIndex",
)
EXCLUDE_PATH_PARTS = ("MovimentosCompras",)
REPORT_PATH = ROOT / ".cursor" / "context" / "2026_05_20_prod000-baseline-resultado.json"


def _grep_consumers() -> list[dict]:
    hits: list[dict] = []
    for ext in ("*.cs", "*.cshtml"):
        for path in ROOT.rglob(ext):
            rel = path.relative_to(ROOT).as_posix()
            if any(x in rel for x in EXCLUDE_PATH_PARTS):
                continue
            if (
                "obj" in path.parts
                or "bin" in path.parts
                or "packages" in path.parts
                or "_filestemp" in path.parts
                or "docs" in path.parts
                or "Tests" in path.parts
            ):
                continue
            try:
                text = path.read_text(encoding="utf-8", errors="replace")
            except OSError:
                continue
            for pat in PATTERNS:
                if pat in text:
                    for i, line in enumerate(text.splitlines(), 1):
                        if pat in line:
                            hits.append(
                                {
                                    "file": rel,
                                    "line": i,
                                    "method": pat,
                                    "snippet": line.strip()[:200],
                                }
                            )
    return hits


def _classify_consumer(file: str, method: str) -> str:
    if "MovimentosCompras" in file:
        return "excluido"
    p0_files = {
        "Areas/gc/Controllers/EstoqueController.Lookups.cs": {
            "GetComboGcProdutosPosicaoEstoqueIndex": "p0",
            "GetComboGcProdutosServicosImportados": "p1",
        },
        "Areas/gc/Controllers/EstoqueInventarioController.Lookups.cs": {
            "GetComboGcProdutosServicosImportados": "p0",
            "GetComboGcProdutosServicosTodos": "p1",
            "GetDatasetGcProdutosServicos": "p0",
        },
        "Areas/gc/Controllers/MovimentosController.cs": {"GetDatasetGcProdutosServicos": "p0"},
        "Areas/gc/Controllers/MovimentosController.Lookups.cs": {},
        "Areas/gc/Views/Movimentos/ModalPedidoInsertEditItem.cshtml": {},
        "Areas/gc/Views/Movimentos/ModalConsultaPedidos.cshtml": {},
    }
    if file in p0_files and method in p0_files[file]:
        return p0_files[file][method]
    if method == "GetComboGcProdutosPosicaoEstoqueIndex" and "EstoqueController" in file:
        return "p0"
    if method == "GetDatasetGcProdutosServicos" and (
        "EstoqueInventarioController" in file or "MovimentosController.cs" in file
    ):
        return "p0"
    if method == "GetComboGcProdutosServicosImportados" and "FormInventarioItens" in file:
        return "p0"
    if method == "GetComboGcProdutosServicosTodos" and "ModalCreateEditInventarioItem" in file:
        return "p1"
    if method in ("GetComboGcProdutosServicosTodos", "GetComboGcProdutosServicosImportados"):
        return "p1"
    if method == "GetDatasetGcProdutosServicos":
        return "p1"
    if "ILookupQueryService" in file or "LookupQueryService.cs" in file:
        return "definicao"
    return "p1"


def _parse_ef_sql_connection(config_path: Path, name: str = "GdiPlataformEntities_gdi_homologacao") -> str | None:
    if not config_path.is_file():
        return None
    tree = ElementTree.parse(config_path)
    for add in tree.findall(".//add"):
        if add.get("name") == name:
            cs = add.get("connectionString") or ""
            m = re.search(
                r'provider connection string="([^"]+)"',
                cs.replace("&quot;", '"'),
            )
            if not m:
                m = re.search(r"provider connection string=&quot;([^&]+)&quot;", cs)
            if m:
                inner = m.group(1).replace("&quot;", '"')
                return inner
    return None


def _parse_adonet(conn_str: str) -> dict:
    out = {}
    for part in conn_str.split(";"):
        if "=" not in part:
            continue
        k, v = part.split("=", 1)
        out[k.strip().lower()] = v.strip()
    return out


def _sql_counts_sqlcmd(conn_str: str) -> dict:
    import subprocess

    p = _parse_adonet(conn_str)
    server = p.get("data source")
    database = p.get("initial catalog")
    user = p.get("user id")
    password = p.get("password")
    if not all([server, database, user, password]):
        return {"error": "connection string incompleta para sqlcmd"}

    queries = {
        "produtos_ativos": "SET NOCOUNT ON; SELECT COUNT(*) FROM g_produtos WHERE ativo = 1;",
        "produtos_importados_ativos": (
            "SET NOCOUNT ON; SELECT COUNT(*) FROM g_produtos WHERE ativo = 1 AND importado = 1;"
        ),
        "produtos_ativos_avg_nome_len": (
            "SET NOCOUNT ON; SELECT CAST(AVG(LEN(LTRIM(RTRIM(ISNULL(nome,''))))) AS INT) "
            "FROM g_produtos WHERE ativo = 1;"
        ),
    }
    out: dict = {"catalog": database, "driver": "sqlcmd"}
    for key, sql in queries.items():
        cmd = [
            "sqlcmd",
            "-S",
            server,
            "-d",
            database,
            "-U",
            user,
            "-P",
            password,
            "-C",
            "-h",
            "-1",
            "-W",
            "-Q",
            sql,
        ]
        try:
            proc = subprocess.run(cmd, capture_output=True, text=True, timeout=30)
            if proc.returncode != 0:
                out["error"] = (proc.stderr or proc.stdout or "sqlcmd falhou")[:300]
                break
            line = (proc.stdout or "").strip().splitlines()
            val = line[-1].strip() if line else ""
            out[key] = int(val) if val.isdigit() else None
        except Exception as ex:
            out["error"] = str(ex)[:300]
            break
    return out


def _sql_counts(conn_str: str) -> dict:
    try:
        import pyodbc  # type: ignore
    except ImportError:
        return _sql_counts_sqlcmd(conn_str)

    queries = {
        "produtos_ativos": "SELECT COUNT(*) FROM g_produtos WHERE ativo = 1",
        "produtos_importados_ativos": "SELECT COUNT(*) FROM g_produtos WHERE ativo = 1 AND importado = 1",
        "produtos_ativos_avg_nome_len": (
            "SELECT AVG(LEN(LTRIM(RTRIM(ISNULL(nome,''))))) FROM g_produtos WHERE ativo = 1"
        ),
    }
    out: dict = {"catalog": None}
    m = re.search(r"initial catalog=([^;]+)", conn_str, re.I)
    if m:
        out["catalog"] = m.group(1)
    try:
        cn = pyodbc.connect(conn_str, timeout=15)
        cur = cn.cursor()
        for key, sql in queries.items():
            cur.execute(sql)
            row = cur.fetchone()
            out[key] = int(row[0]) if row and row[0] is not None else None
        cn.close()
    except Exception as ex:
        out["error"] = str(ex)[:500]
    return out


def _estimate_html_options(count_ativos: int, avg_nome: float, extra_options: int = 2) -> dict:
    avg_nome = avg_nome or 45.0
    options = count_ativos + extra_options
    bytes_per_option = 80 + int(avg_nome)  # value + truncated text markup
    html_kb = (options * bytes_per_option) / 1024
    return {
        "option_count_estimado": options,
        "html_select_kb_estimado": round(html_kb, 1),
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--sql", action="store_true", help="Tentar contagens SQL (homologação)")
    parser.add_argument("--out", type=Path, default=REPORT_PATH)
    args = parser.parse_args()

    hits = _grep_consumers()
    by_method: dict[str, list] = {p: [] for p in PATTERNS}
    for h in hits:
        tier = _classify_consumer(h["file"], h["method"])
        h["tier"] = tier
        by_method.setdefault(h["method"], []).append(h)

    result = {
        "generated_at": datetime.now(timezone.utc).isoformat(),
        "prod000_3": {
            "total_hits": len(hits),
            "excluded_module": "MovimentosCompras",
            "by_method": {k: len(v) for k, v in by_method.items()},
            "p0_consumers": [h for h in hits if h.get("tier") == "p0"],
            "p1_consumers": [h for h in hits if h.get("tier") == "p1"],
            "typeahead_ok": {
                "ModalPedidoInsertEditItem": "GetProdutosLookup em view; PreencherLookupsPedidoItemModal = placeholder",
                "ModalConsultaPedidos": "GetProdutosLookup; ComboFiltroProdutoConsultaPedidos",
            },
            "inventario_correcao": {
                "FormInventarioItens_filtro": "GetComboGcProdutosServicosImportados (não Todos)",
                "FormInventarioItens_grid": "GetDatasetGcProdutosServicos em GetDadosInventarioItem",
                "ModalCreateEditInventarioItem": "GetComboGcProdutosServicosTodos",
            },
        },
        "prod000_2": {},
        "prod000_1": {},
    }

    config = ROOT / "App_Data" / "Secrets" / "sql-server.local.config"
    if args.sql:
        conn = _parse_ef_sql_connection(config)
        if conn:
            counts = _sql_counts(conn)
            result["prod000_2"] = {
                "fonte": "SQL homologação (script)",
                "queries_equivalentes": {
                    "cenario_1_estoque_index": counts.get("produtos_ativos"),
                    "cenario_2_inventario_filtro": counts.get("produtos_importados_ativos"),
                    "cenario_3_dataset": counts.get("produtos_ativos"),
                },
                **{k: v for k, v in counts.items() if k != "error"},
            }
            if "error" not in counts and counts.get("produtos_ativos"):
                n = counts["produtos_ativos"]
                avg = float(counts.get("produtos_ativos_avg_nome_len") or 45)
                est_index = _estimate_html_options(n, avg, 2)
                est_import = _estimate_html_options(
                    counts.get("produtos_importados_ativos") or 0, avg, 1
                )
                result["prod000_1"] = {
                    "nota": "Estimativa estática (DevTools real = medir no browser)",
                    "estoque_index_abrir": est_index,
                    "inventario_form_filtro_importados": est_import,
                    "controle_pedido_modal": {"options_html_inicial": 1, "ajax_lookup": "GetProdutosLookup?q="},
                    "controle_consulta_pedidos": {"options_html_inicial": 1, "ajax_lookup": "GetProdutosLookup?q="},
                    "movimentos_ajax_dataset": {
                        "actions": ["AjaxDadosProduto", "AjaxGetPrecoVendaProduto"],
                        "dataset_rows_carregadas": n,
                        "memorycache_key": "GcProdutosServicosDataset",
                    },
                }
            else:
                result["prod000_2"]["error"] = counts.get("error", "sem contagens")
        else:
            result["prod000_2"]["error"] = "connection string não encontrada"

    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(result, indent=2, ensure_ascii=False), encoding="utf-8")
    print(f"OK: {args.out}")
    print(json.dumps({k: result[k] for k in ("prod000_2", "prod000_1") if result[k]}, indent=2, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    sys.exit(main())
