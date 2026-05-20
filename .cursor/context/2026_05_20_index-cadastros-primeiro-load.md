# Index cadastros `g` — primeiro load da grelha (2.8)

**Inventário:** `python Scripts/2026_05_20_gdi_inventory_index_first_load.py`

## Padrão (referência: Cidades / Perfis / Produtos)

| Camada | Comportamento |
|--------|----------------|
| **View** | `deferLoading: true`; filtro inline Id + texto; Pesquisar / Limpar; Enter → pesquisar; `yesFilterField=""` em Pesquisar, `"*"` só em Limpar |
| **GetDados** | Sem critério inline nem `yesFilterField='*'` → `aaData` vazio, `iTotalDisplayRecords=0`; Limpar lista paginada (máx. 100/página); persistência `id;nome` em `g_filtros` |

## Estado (2026-05-20)

| Index | deferLoading | Gate GetDados | Nota |
|-------|--------------|---------------|------|
| Cidades, Perfis, UF, Clientes, Usuarios, Vendedores, ContratosAviacao, ContasCaixas, PagRecTipos/Condicoes, ProdutosNcm, **Produtos** | Sim | Sim | Produtos corrigido 2026-05-19 fase 2 |
| **Filiais** | Sim (2026-05-20) | Sim | Era o único cadastro DataTable com carga total no 1.º load |
| CentrosCustos, ClassificacaoFinanceira | N/A | N/A | jstree, não DataTables Index |
| Financeiro, Nfe, Ged, Atendimentos | Não | Variado | Módulos operacionais — fora escopo 2.8 cadastros |

## Smoke manual

1. Abrir Index → grelha vazia + mensagem `sEmptyTable`.
2. Pesquisar com critério → linhas + paginação.
3. Enter no filtro → mesma pesquisa.
4. Limpar → lista paginada (todos).
5. Com filtro ativo, mudar página → mantém critério.
