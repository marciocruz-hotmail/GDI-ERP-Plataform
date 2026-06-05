# EF6 — SqlQuery com colunas parciais vs entidade completa (2026-05-20)

## Sintoma

Ao salvar cotação/pedido (`AjaxSavePedido`):

> The data reader is incompatible with `g_produtos`. A member `id_produto_substituto` does not have a corresponding column in the data reader.

## Causa

`db.g_produtos.SqlQuery("select col1, col2, ...")` materializa a entidade **`g_produtos` completa**. O reader só traz as colunas do `SELECT` — qualquer propriedade nova no `.edmx` (ex. `id_produto_substituto`) exige coluna no result set.

**Anti-padrão (PERF-010 inicial):** `GProdutosPedidoItemSqlColumns` em `MovimentosController.LoadProdutosPedidoItens`.

## Correção aplicada

`LoadProdutosPedidoItens` → EF:

```csharp
db.g_produtos.AsNoTracking().Where(p => idsProduto.Contains(p.id_produto))
```

Carrega só produtos dos itens do movimento, com **todas** as colunas mapeadas.

## Regra para novos códigos

| Necessidade | Abordagem |
|-------------|-----------|
| Entidade EF (`g_produtos`, etc.) | `AsNoTracking()` + `Where` / `Find`, ou `SqlQuery("select * ...")` |
| Menos colunas / performance | Classe DTO dedicada + `SqlQuery<Dto>` ou projeção `.Select(x => new { ... })` — **não** `SqlQuery<Entidade>` com colunas parciais |

## Publish homologação

Se o erro for **Invalid column name 'id_produto_substituto'** no SQL (não reader mismatch), executar script de schema em homologação antes do deploy do binário.
