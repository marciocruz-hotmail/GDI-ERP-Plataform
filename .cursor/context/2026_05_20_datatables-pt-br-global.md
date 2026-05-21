# DataTables — PT-BR global (2026-05-20)

## Fonte única

| Ficheiro | Função |
|----------|--------|
| `LibUI_AdminLTE-4.0.0/plugins/startprime/js/gdi-datatables-defaults.js` | `$.extend(true, DataTable.defaults.oLanguage, …)` + `window.GdiDataTablesPtBr` |

## Onde carrega (ordem após `datatables.min.js`)

1. `Views/Shared/_LayoutScriptsDataTables.cshtml` — páginas com flag `GdiPageScriptsFlags.DataTables`
2. `Views/Shared/_LayoutPageScriptRegistry.cshtml` — bundle `dataTables` (lazy `#mainModal`, hubs sem DT no host)
3. `Views/Shared/_Blank.cshtml` — layout modal/legado síncrono

## Views

- **~170 inits** sem bloco `language:` → herdam defaults (auditoria: `Scripts/2026_05_20_gdi_audit_datatables_language.py`).
- Inits com `language: { … }` → DataTables 2 mapeia para `oLanguage`; use só para **mensagens por tela** (ex. `sEmptyTable` «Informe Id. e clique em Pesquisar»).
- Preferir **não** duplicar blocos inteiros de idioma em novas views.

## Publish

Incrementar **VersionERP** quando alterar `gdi-datatables-defaults.js` (cache-bust no layout e registry).

## Filtro obrigatório (`deferLoading`)

Ver **`.cursor/context/2026_05_20_datatables-filtro-obrigatorio-mensagem.md`** — hook global evita «Carregando...» antes da primeira pesquisa.

## Api vs jQuery (`$.fn.DataTable`)

| Chamada | Retorno |
|---------|---------|
| `$(sel).dataTable(opts)` | jQuery (cadeia) |
| `$(sel).DataTable(opts)` | **DataTables.Api** (`.draw`, `.row`, …) |

**Não** atribuir o mesmo wrapper aos dois. Helpers globais: `GdiDataTableApi(selector)`, `GdiDataTableDraw(selector, resetPaging)`.
