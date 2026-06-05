# -*- coding: utf-8 -*-
"""Lista de actions LayoutLite (espelho de GdiPageScriptsDefaults — G-PERF-20 Fase 4 lote C)."""
from __future__ import print_function

# controller -> actions (case-insensitive no C#)
LAYOUT_LITE_ACTIONS = {
    "GedSGQ": ["IndexPops"],
    "Treinamentos": ["IndexTreinamentoAviacao001"],
    "CentrosCustos": ["Create", "Edit"],
    "ClassificacaoFinanceira": ["Create", "Edit"],
    "Cidades": ["Create", "Edit"],
    "ContasCaixas": ["Create", "Edit"],
    "Filiais": ["Create", "Edit"],
    "PagRecCondicoes": ["Create", "Edit"],
    "PagRecTipos": ["Create", "Edit"],
    "Perfis": ["Create", "Edit"],
    "ProdutosNcm": ["Create", "Edit"],
    "UF": ["Create", "Edit"],
    "Usuarios": ["Create", "Edit"],
    "Cfop": ["Create", "Edit"],
    "FinanceiroParametroDifal": ["Create", "Edit"],
    "ComexProdutos": ["FormProcessarProdutosPreNovos", "FormProcessarProdutosPreAtualizar"],
    "MovimentosEntradas": [
        "FormProcessarNFCompraNacional",
        "FormProcessarNFDevolucao",
        "FormProcessarNFImportacao",
    ],
}
