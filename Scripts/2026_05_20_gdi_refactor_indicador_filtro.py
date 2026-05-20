# -*- coding: utf-8 -*-
"""Substitui jsAtualizarIndicadorFiltro* locais por wrapper em GdiAtualizarIndicadorFiltro (start.js)."""
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

# (caminho relativo, sufixo função, btnId, tituloInativo, tituloAtivo)
MODULOS = [
    ("Areas/g/Views/Cidades/Index.cshtml", "Cidades", "btnLimparFiltroCidades",
     "Limpar filtro e listar todos", "Filtro ativo — limpar e listar todas as cidades"),
    ("Areas/g/Views/Clientes/Index.cshtml", "Clientes", "btnLimparFiltroClientes",
     "Limpar filtro e listar todos", "Filtro ativo — limpar e listar todos os clientes"),
    ("Areas/g/Views/ContasCaixas/Index.cshtml", "ContasCaixas", "btnLimparFiltroContasCaixas",
     "Limpar filtro e listar todos", "Filtro ativo — limpar e listar todas as contas caixa"),
    ("Areas/g/Views/PagRecTipos/Index.cshtml", "PagRecTipos", "btnLimparFiltroPagRecTipos",
     "Limpar filtro e listar todos", "Filtro ativo — limpar e listar todos os tipos"),
    ("Areas/g/Views/PagRecCondicoes/Index.cshtml", "PagRecCondicoes", "btnLimparFiltroPagRecCondicoes",
     "Limpar filtro e listar todos", "Filtro ativo — limpar e listar todas as condições"),
    ("Areas/g/Views/Perfis/Index.cshtml", "Perfis", "btnLimparFiltroPerfis",
     "Limpar filtro e listar todos", "Filtro ativo — limpar e listar todos os perfis"),
    ("Areas/g/Views/ProdutosNcm/Index.cshtml", "ProdutosNcm", "btnLimparFiltroProdutosNcm",
     "Limpar filtro e listar todos", "Filtro ativo — limpar e listar todos os NCM"),
    ("Areas/g/Views/Produtos/Index.cshtml", "Produtos", "btnLimparFiltroProdutos",
     "Limpar filtro e listar todos", "Filtro ativo — limpar e listar todos os produtos"),
    ("Areas/g/Views/UF/Index.cshtml", "Uf", "btnLimparFiltroUf",
     "Limpar filtro e listar todos", "Filtro ativo — limpar e listar todas as UFs"),
    ("Areas/g/Views/Usuarios/Index.cshtml", "Usuarios", "btnLimparFiltroUsuarios",
     "Limpar filtro e listar todos", "Filtro ativo — limpar e listar todos os usuários"),
    ("Areas/g/Views/ContratosAviacao/Index.cshtml", "Contratos", "btnLimparFiltroContratos",
     "Limpar filtro e listar todos", "Filtro ativo — limpar e listar todos os contratos"),
    ("Areas/gc/Views/Cfop/Index.cshtml", "Cfop", "btnLimparFiltroCfop",
     "Limpar filtro e listar todos", "Filtro ativo — limpar e listar todos os CFOP"),
    ("Areas/gc/Views/CfopParametros/Index.cshtml", "CfopParametros", "btnLimparFiltroCfopParametros",
     "Limpar filtro e listar todos", "Filtro ativo — limpar e listar todas as parâmetros"),
    ("Areas/gc/Views/CfopOperacoes/Index.cshtml", "CfopOperacoes", "btnLimparFiltroCfopOperacoes",
     "Limpar filtro e listar todos", "Filtro ativo — limpar e listar todas as operações"),
    ("Areas/gc/Views/EstoqueControle/Index.cshtml", "EstoqueControle", "btnLimparFiltroEstoqueControle",
     "Limpar filtro e listar todos", "Filtro ativo — limpar e listar todos os controles"),
    ("Areas/gc/Views/FinanceiroParametroDifal/Index.cshtml", "Difal", "btnLimparFiltroDifal",
     "Limpar filtro e listar todos", "Filtro ativo — limpar e listar todas as parâmetros Difal"),
    ("Areas/g/Views/Vendedores/Index.cshtml", "Vendedores", "btnLimparFiltroVendedores",
     "Limpar filtro e listar todos", "Filtro ativo — limpar e listar todos os vendedores"),
]


def read_text(path):
    try:
        return path.read_text(encoding="utf-8")
    except UnicodeDecodeError:
        return path.read_text(encoding="cp1252")


def replace_function(text, suffix, btn_id, titulo_inativo, titulo_ativo):
    fn_name = f"jsAtualizarIndicadorFiltro{suffix}"
    start = text.find(f"function {fn_name}")
    if start < 0:
        return text, False
    brace = text.find("{", start)
    if brace < 0:
        return text, False
    depth = 0
    i = brace
    while i < len(text):
        if text[i] == "{":
            depth += 1
        elif text[i] == "}":
            depth -= 1
            if depth == 0:
                end = i + 1
                break
        i += 1
    else:
        return text, False

    new_fn = f"""function {fn_name}(yesFilterOnOff)
    {{
        GdiAtualizarIndicadorFiltro(yesFilterOnOff, {{
            btnId: '{btn_id}',
            tituloInativo: '{titulo_inativo}',
            tituloAtivo: '{titulo_ativo}'
        }});
    }}"""
    return text[:start] + new_fn + text[end:], True


def main():
    changed = []
    for rel, suffix, btn_id, ti, ta in MODULOS:
        path = ROOT / rel.replace("/", "\\") if "\\" in str(ROOT) else ROOT / rel
        path = ROOT / rel
        text = read_text(path)
        new_text, ok = replace_function(text, suffix, btn_id, ti, ta)
        if ok and new_text != text:
            path.write_text(new_text, encoding="utf-8", newline="\r\n")
            changed.append(rel)
        elif not ok:
            print("SKIP (função não encontrada):", rel)

    print("Atualizados:", len(changed))
    for c in changed:
        print(" ", c)


if __name__ == "__main__":
    main()
