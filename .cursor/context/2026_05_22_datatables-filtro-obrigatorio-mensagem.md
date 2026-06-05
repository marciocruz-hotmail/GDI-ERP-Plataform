# DataTables — filtro obrigatório antes do primeiro Ajax (2026-05-20)

## Sintoma

Telas Index com `deferLoading: true` (ex. `g/Clientes/Index`) mostram **«Carregando...»** na grelha vazia, embora **não haja** pedido Ajax até o utilizador pesquisar.

## Causa (núcleo DataTables 2)

Em `_emptyRow`, se a fonte é `ssp`/`ajax` e **`settings.json` ainda não existe**, a mensagem é **`oLanguage.sLoadingRecords`**, não `sEmptyTable`:

```javascript
if ((dataSrc === 'ssp' || dataSrc === 'ajax') && !settings.json) {
    zero = oLang.sLoadingRecords;
}
```

Com `deferLoading: true`, o primeiro draw não carrega JSON → permanece `sLoadingRecords` (global: «Carregando...»).

## Arquitetura GDI (global)

| Camada | Ficheiro | Comportamento |
|--------|----------|----------------|
| **Hook de init** | `gdi-datatables-defaults.js` | Se `deferLoading`: copia mensagem de ação para `sLoadingRecords`; após **primeiro** `xhr.dt` limpa `sEmptyTable` para pesquisas vazias usarem `sZeroRecords` |
| **Mensagem por tela** | `language.sEmptyTable` ou `language.gdiAwaitingFilter` | Texto específico (ex. «Informe Id., Cliente, CPF ou CNPJ…») |
| **Padrão** | `GdiDataTablesPtBr.sAwaitingFilter` | «Informe os dados do filtro e clique em Pesquisar.» |
| **Servidor** | `GetDados` + `yesFilterOnOff` | Sem alteração; continua a devolver `aaData` vazio até haver critério ou «Limpar» (`yesFilterField = *`) |
| **UX botão** | `GdiAtualizarIndicadorFiltro` + `LibMessageProcessando` no Pesquisar | Mantido nas views |

### Contrato recomendado em novas Index com filtro obrigatório

```javascript
$('#dtX').DataTable({
    deferLoading: true,
    processing: true,
    bServerSide: true,
    // ...
    language: {
        gdiAwaitingFilter: 'Informe … e clique em Pesquisar (ou Limpar para listar todos).',
        sZeroRecords: 'Nenhum registro encontrado'
        // sEmptyTable opcional (alias de gdiAwaitingFilter); após 1.º xhr é limpo pelo hook
    }
});
```

**Não** é necessário repetir `sLoadingRecords` nas views — o hook sincroniza a partir de `gdiAwaitingFilter` / `sEmptyTable`.

## Inventário (deferLoading)

~19 Index: Clientes, Produtos, Cidades, UF, Filiais, Perfis, Usuarios, Vendedores, ContasCaixas, PagRec*, ContratosAviacao, ProdutosNcm, Cfop*, EstoqueControle, FinanceiroParametroDifal.

## Publish

Alterou `gdi-datatables-defaults.js` → incrementar **VersionERP**.
