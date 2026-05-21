# Typeahead Ajax — pedidos (Grupo 1.6)

**Data:** 2026-05-20

## Contrato JSON (1.6.1)

**GET** `Movimentos/GetClientesLookup` | `Movimentos/GetProdutosLookup` (área `gc`)

| Parâmetro | Tipo | Descrição |
|-----------|------|-----------|
| `q` | string | Termo de pesquisa (mín. 2 caracteres; ignorado se `id` informado) |
| `id` | int? | Pré-seleção / item único (edição) |
| `limit` | int? | Máx. resultados (default 30, teto 50) |

**Resposta sucesso:**

```json
{
  "items": [
    { "id": "123", "text": "Nome cliente    [Id: 123] [ 00.000.000/0001-00 ]" }
  ]
}
```

**Resposta erro:**

```json
{
  "items": [],
  "errorMessage": "mensagem",
  "severity": "error"
}
```

Select2: `gdi-select2.js` → `processResults` mapeia `items` para `{ results: [{ id, text }] }`.

**Digitação contínua:** o Select2 cancela o GET do termo anterior (`abort`). O `transport` em `gdi-select2.js` **não** deve exibir modal nesse caso — só em HTTP/JSON de erro real (2026-05-21, `gdiSelect2IsLookupAjaxAbort`).

## UI (1.6.3)

Atributos no `<select>`:

- `data-gdi-select2-search="true"` — força Select2 mesmo com poucas `<option>`
- `data-gdi-lookup-url` — URL da action GET (**controllers na raiz**, ex. `ClientesLookup`, exigem `Url.Action(..., new { area = "" })` em views dentro de `Areas/g` ou `Areas/gc` — senão MVC gera `/g/ClientesLookup/...` → 404 e Select2 mostra *Os resultados não puderam ser carregados*)
- `data-gdi-lookup-min-length="2"` — opcional

**Views com typeahead (obrigatório `data-gdi-lookup-url` + `data-gdi-select2-search="true"`):**

| Contexto | View / campo | Endpoint GET |
|----------|--------------|--------------|
| Pedidos (`is_cliente`) | `FormPedidoCreate` `#id_cliente`, `IndexPedido` `#edit_cliente`, `PainelPedidos` `id_cliente`, `ModalConsultaPedidos` `Field_Int_01` | `gc/Movimentos/GetClientesLookup` |
| Cliente/fornecedor (`ativo`) | `g/Clientes/Index` `#edit_id_cliente_lookup`, `gc/FinanceiroLancamentos/Index` `Field_Text_07`, modal lançamento `id_cliente` (ComDoc), `g/Financeiro/Index`, `g/ContratosAviacao/CreateEdit`, `gc/RelatoriosFinanceiros` modal `Field_Int_04` | `ClientesLookup/GetClientesFornecedoresLookup` ou `GetClientesFornecedoresComDocLookup` |
| Atendimentos | `g/Atendimentos/Edit`, `ModalCreateNewAtendimento` | `g/Atendimentos/GetClientesLookup` |
| Produto (estoque) | `gc/Estoque/Index` `#id_produto_servico` | `gc/Estoque/GetProdutosLookup` |
| Produto (pedidos) | `ModalPedidoInsertEditItem` `#id_produto`, `ModalConsultaPedidos` `Field_Int_02` | `gc/Movimentos/GetProdutosLookup` |

**Verificação (2026-05-21):** todos os hosts acima com `data-gdi-select2-search="true"` + `data-gdi-lookup-url`; controllers na raiz (`ClientesLookup`) com `area = ""`; controllers em área com `area = "g"` / `"gc"`.

**Fora do typeahead Ajax (não exigem `data-gdi-lookup-url`):** `ModalInvoicesItensEspelhoDigital` (filtro por nome de cliente legado); combos dependentes (`ComboClientesContatos` após escolher cliente); tabelas MVC com `ViewBag.comboProdutos` completo (NF compra/devolução, Comex batch); NCM/tipos/condições produto (listas pequenas ou cache local).

Placeholders: `ComboFiltroClienteTodosAtivos` (Index/Painel, `-1` = todos), `ComboFiltroClienteSelecione` (modal consulta), `ComboFiltroClienteFornecedor*` / `ComboFiltroClienteCadastroSelecione` (cadastro/financeiro).

**Armadilha:** combo só com 1 `<option>` sem os atributos acima → `gdi-select2.js` mantém `<select>` nativo (sem pesquisa Ajax). Ver `gdiInitSelect2OnCollection` (`nOpts <= 5`).

## Primeiro paint (1.6.4)

| Antes | Depois |
|-------|--------|
| `GetComboSomenteGClientes` no form pedido | Placeholder + opção do cliente em edição |
| `GetComboGcProdutosServicosTodos` + dataset no modal item | Placeholder + produto em edição/duplicar |

**Endpoints centralizados (2026-05-20):** `Controllers/ClientesLookupController.cs` — `GetClientesFornecedoresLookup` / `GetClientesFornecedoresComDocLookup` (substituem combos globais `GetComboGClientesFornecedores*`, removidos em CACHE-2e). **Cliente pedido (`is_cliente`):** `Movimentos/GetClientesLookup`, `Atendimentos/GetClientesLookup`. **Produto:** `Movimentos/GetProdutosLookup` (modal item pedido, consulta pedidos); **`Estoque/GetProdutosLookup`** (Index posição estoque — PROD-002a).

## Filtro de clientes (pedido vs demais)

- **Typeahead pedido** (`SearchClientes` / `GetClientesLookup`): `g_clientes` com `ativo = true` e `is_cliente = true`; **sem** filtro por vendedor/perfil do utilizador.
- **Combos gerais** (`GetComboGClientesFornecedores`): todos os registros com `ativo = true` (clientes e fornecedores).
- **Combos de pedido** (`GetComboSomenteGClientes` — legado, cache global): `ativo = true` e `is_cliente = true`. **Uso restante:** `AtendimentosController.Lookups.cs` (Create/Edit). Typeahead `GetClientesLookup` replica o mesmo filtro `is_cliente` sem cache HTML.

## Ficheiros

- `Lib/Lookups/LookupAjaxContracts.cs`, `LookupSearchQueries.cs`
- `Areas/gc/Controllers/MovimentosController.LookupAjax.cs`
- `Areas/gc/Controllers/EstoqueController.LookupAjax.cs`
- `Areas/g/Controllers/AtendimentosController.LookupAjax.cs`
- `Controllers/ClientesLookupController.cs`
- `LibUI_AdminLTE-4.0.0/plugins/startprime/js/gdi-select2.js`
