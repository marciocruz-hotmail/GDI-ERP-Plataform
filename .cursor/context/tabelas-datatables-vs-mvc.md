# Tabelas no ERP — DataTables vs MVC (formulário)

**Regra obrigatória:** em toda investigação, correção ou alteração de UI que envolva `<table>`, identificar primeiro o **tipo** antes de mudar CSS, JS, views ou controllers.

---

## 1. DataTables (listagens / modais Ajax)

| Aspeto | Padrão no projeto |
|--------|-------------------|
| **Onde** | `Index.cshtml`, modais, abas em `CreateEdit` com grelha dinâmica |
| **Markup** | `table` com classes `display`, `compact`, muitas vezes `id="dt..."` ou `dataTable...` |
| **Wrapper** | `div.table-responsive.scroll-body-horizontal` (scroll horizontal intencional quando há muitas colunas) |
| **Dados** | `tbody` vazio; preenchimento via Ajax → actions `GetDados*` / equivalentes |
| **Cliente** | Inicialização DataTables; eventos `error.dt` / `xhr.dt`; helpers `GdiDtNotifyLoadFailure`, `GdiDtNotifyJsonErrorMessage` |
| **Servidor** | JSON com `aaData`, `errorMessage`, `stackTrace`, `yesFilterOnOff` (fases documentadas em `CLAUDE.md`) |
| **CSS** | `start.css`: `width: max-content` em `table.display`, `table.dataTable`, `.dataTables_wrapper table` |
| **Inventário** | `Scripts/gdi_inventory_datatables_g_area.py`, `Scripts/gdi_verify_csproj_gdi_helpers.py` |

**Não fazer:** aplicar `gdi-form-table-fixed`, remover `scroll-body-horizontal` nem `max-content` em listagens DataTables sem validar impacto em todas as colunas.

---

## 2. Tabelas MVC (formulário server-side)

| Aspeto | Padrão no projeto |
|--------|-------------------|
| **Onde** | `FormProcessar*.cshtml`, ecrãs de processamento em lote, grelhas com `@for` + `EditorFor` / `DropDownListFor` |
| **Markup** | `table` **sem** classe `display`; linhas geradas no servidor |
| **Wrapper** | `div.table-responsive.gdi-form-table-scroll` — **evitar** `scroll-body-horizontal` sozinho (legado forçava `max-content` na tabela) |
| **Tabela** | Classe `gdi-form-table-fixed` + `table-layout: fixed`; colunas read-only em texto simples (não `LabelFor` só para exibir) |
| **Card** | `min-w-0` na `col` / `card-body` em layouts flex |
| **JS** | `jsInitForm()` em `start.js` faz `scrollLeft = 0` em wrappers com tabela MVC |
| **CSS** | `start.css` — regras `.gdi-form-table-scroll` / `.gdi-form-table-fixed` |
| **Inventário** | `Scripts/gdi_inventory_scroll_body_form_tables.py` (tabelas MVC ainda com `scroll-body-horizontal` incorreto) |

**Não fazer:** tratar como DataTables (`GetDados`, `GdiDt*`, classe `display`) nem copiar padrão de `Index` para formulários de processamento.

---

## Checklist rápido (agente / dev)

1. Abrir a view: existe `id="dt..."` ou classe `display` + Ajax DataTables? → **DataTables**
2. Existe `@for` com `EditorFor`/`HiddenFor` e post de formulário? → **MVC**
3. Alteração em `start.css` na regra `scroll-body-horizontal` / `max-content`? → validar **só** impacto em DataTables
4. Correção de coluna cortada / tabela a estourar o card? → verificar se é MVC e usar padrão `gdi-form-table-*`

---

## Referência (correção 2026-05-20)

Problema típico: `.scroll-body-horizontal > table { width: max-content }` aplicado a tabelas MVC → primeira coluna fora do viewport. Solução: separar regras CSS por tipo; migrar forms para `gdi-form-table-scroll`. Ver `.cursor/CHANGELOG-DEV.md` (entradas «Tabelas MVC» / «FormProcessarNFImportacao»).
