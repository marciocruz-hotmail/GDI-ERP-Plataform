# G-PERF-20f — Lazy load de scripts no `#mainModal`

**Data:** 2026-05-20  
**VersionERP:** 2026.51.12

---

## Problema

Páginas **layout lite** (`data-gdi-page-scripts=33`) não carregam DataTables/Select2/Tempus no primeiro paint. Modais Ajax (`$("#mainModal").load(...)`) executam scripts inline que assumem essas libs — erro `$.fn.DataTable is not a function` ou `jsDatepicker` sem Tempus.

---

## Solução

| Peça | Ficheiro |
|------|----------|
| Registo de URLs | `Views/Shared/_LayoutPageScriptRegistry.cshtml` (após jQuery no `_LayoutScriptsAuthenticated`) |
| Helpers + patch | `start.js` — `GdiLoadScriptOnce`, `GdiLoadStylesOnce`, `GdiEnsureScriptFlags`, `GdiMainModalLoad` |
| Interceção global | `jQuery.fn.load` redireciona só `#mainModal` → `GdiMainModalLoad` |

### Fluxo

1. `GET` do HTML do modal (texto).  
2. `GdiDetectScriptFlagsFromHtml` — regex + opcional `data-gdi-require-scripts="39"` no fragmento.  
3. `GdiEnsureScriptFlags` — carrega CSS/JS em falta (uma vez por URL).  
4. `$('#mainModal').html(html)` — scripts inline correm com libs presentes.  
5. Callback original (ex.: `bootstrap.Modal.show()`).

### Deteção em runtime

Mesmo com flag no `<body>`, verifica `$.fn.DataTable`, `$.fn.select2`, `$.fn.tempusDominus`, etc., antes de carregar.

---

## Uso explícito (opcional)

```javascript
GdiMainModalLoad('@Url.Action("ModalX", "Controller", new { area = "gc" })', function () {
    var e = document.getElementById('mainModal');
    if (e) { bootstrap.Modal.getOrCreateInstance(e).show(); }
});
```

Não é obrigatório migrar views: o patch em `$.fn.load` cobre o padrão existente.

### Forçar libs no partial do modal

```html
<div class="modal-dialog" data-gdi-require-scripts="8">
```

Valores = bitmask `GdiPageScriptsFlags` (ex.: 8 = Tempus, 6 = DT+S2).

---

## Smoke manual

1. `gc/RelatoriosRegulamentacao/Index` (lite 33 + Tempus na view).  
2. Abrir modal ANP — datas Tempus OK.  
3. Remover Tempus da view (só teste local) — modal deve carregar Tempus via lazy.  
4. `gc/Parametros/Index` — abrir modal que use DataTables (se existir) — DT injetado sob demanda.  
5. Regressão: `gc/Movimentos/IndexPedido` — modal pedido com grid + Select2.

---

## Referências

- Matriz modais: `2026_05_22_layout-scripts-matriz-modais.md`  
- Fase 4 opt-out: `2026_05_22_layout-scripts-fase4-optout.md`
