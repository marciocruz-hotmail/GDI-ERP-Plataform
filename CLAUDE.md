# CLAUDE.md — GDI-ERP-Plataform

ERP GDI Aviação | ASP.NET MVC 4.7.2 | Bootstrap 5 + AdminLTE 4 | SQL Server  
Respostas ao utilizador: **português brasileiro**.

@.claude/CHANGELOG-RECENT.md

**Memória:** `AI-CONTEXT.md` (fixo) → `CHANGELOG-DEV.md` → `BACKLOG-DEV.md` → `.cursor/context/2026_06_05_indice-memoria-ia.md` (temas).  
**Regras agente (git, publish, formato resposta):** `.cursor/rules/2026_05_20_gdi-erp-plataform.mdc` — **não duplicar aqui**.

### Portal cliente (neste monólito)

`UserIdentity/AcessoPortal` + área **`crm`** (externo). `Areas/g/PortalCliente` = legado interno. Ver `UserIdentityController`, `Areas/gc/Controllers/MovimentosController.cs` (templates).

---

## Padrões — ponteiro (detalhe no índice)

| Tema | Contexto |
|------|----------|
| **Arquitetura obrigatória** (modal, DT, Ajax) | `2026_06_05_arquitetura-centralizada-erp-gdi.md` |
| DataTables vs MVC | `2026_05_20_tabelas-datatables-vs-mvc.md` |
| Lookups Index vs CreateEdit | `2026_05_20_lookups-convencao-index-vs-createedit.md` |
| `GdiMvcJsonResults` | `2026_06_05_gdi-mvc-json-results-contrato.md` |
| Mensagens UX | `2026_05_22_libmessage-confirm-arquitetura.md` |
| NFe e-Notas | `2026_05_26_nfe-enotas-arquitetura.md` |

Helpers cliente: `GdiDtNotifyLoadFailure`, `GdiDtNotifyJsonErrorMessage`, `GdiAjaxNotifyInconsistencias` em `start.js`.

Fases DataTables 0–18 e lotes servidor N-*: **histórico** `docs/dev-history/CHANGELOG-DEV-HISTORICO-INICIAL.md` — não reexpandir neste ficheiro.

---

## Ficheiros críticos

- `LibUI_.../start.js` — mensagens e helpers Gdi (cache `VersionERP`)
- `Views/Shared/_Layout.cshtml` — ordem scripts
- `GDI-ERP-Plataform.csproj` — views `Gdi*` no publish
- `Lib/GdiMvcJsonResults.cs` — contrato JSON servidor

---

## Armadilhas

- Confundir tabela **DataTables** com **MVC** → corta colunas no layout
- Views `Gdi*` fora do `.csproj` → publish incompleto
- `alert()` nativo → usar `LibMessage*` / `GdiAjax*`
- APIs `{ ok, error }` (ex. LMS) → não forçar `errorMessage` sem revisão
- Publish: limpar `obj` se aviso `bootbox-compat` / Access denied

---

## Verificação rápida

```powershell
python Scripts/2026_06_05_gdi_smoke_architecture_inventories.py
python Scripts/2026_05_20_gdi_verify_csproj_gdi_helpers.py
```

Mais inventários → índice memória § Scripts.
