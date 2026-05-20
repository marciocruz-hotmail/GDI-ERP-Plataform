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

## UI (1.6.3)

Atributos no `<select>`:

- `data-gdi-select2-search="true"` — força Select2 mesmo com poucas `<option>`
- `data-gdi-lookup-url` — URL da action GET
- `data-gdi-lookup-min-length="2"` — opcional

**Views alteradas:** `FormPedidoCreate` (`#id_cliente`), `ModalPedidoInsertEditItem` (`#id_produto`).

## Primeiro paint (1.6.4)

| Antes | Depois |
|-------|--------|
| `GetComboSomenteGClientes` no form pedido | Placeholder + opção do cliente em edição |
| `GetComboGcProdutosServicosTodos` + dataset no modal item | Placeholder + produto em edição/duplicar |

**Mantidos:** Index/filtros, `ModalConsultaPedidos`, demais modais; `GetDatasetGcProdutosServicos` em actions server que já consultam por id.

## Filtro de clientes (pedido vs demais)

- **Typeahead pedido** (`SearchClientes` / `GetClientesLookup`): `g_clientes` com `ativo = true` e `is_cliente = true`; **sem** filtro por vendedor/perfil do utilizador.
- **Combos gerais** (`GetComboGClientesFornecedores`): todos os registros com `ativo = true` (clientes e fornecedores).
- **Combos de pedido** (`GetComboSomenteGClientes`): `ativo = true` e `is_cliente = true`.

## Ficheiros

- `Lib/Lookups/LookupAjaxContracts.cs`, `LookupSearchQueries.cs`
- `Areas/gc/Controllers/MovimentosController.LookupAjax.cs`
- `LibUI_AdminLTE-4.0.0/plugins/startprime/js/gdi-select2.js`
