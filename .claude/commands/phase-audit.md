# /phase-audit — Auditoria de status das Fases DataTables

Audite o estado atual de implementação das fases de padronização DataTables/Ajax no GDI-ERP-Plataform.

## Contexto das fases

O projeto está numa migração progressiva (Fases 0–16+) padronizando:
- Servidor: `try/catch` + JSON `{errorMessage, severity, stackTrace, yesFilterOnOff, sEcho, aaData}`
- Cliente: eventos `error.dt` / `xhr.dt` com helpers `GdiDtNotifyLoadFailure` / `GdiDtNotifyJsonErrorMessage`
- Ajax fora do DataTables: `GdiAjaxNotifyInconsistencias`

## O que fazer

1. Execute `python Scripts/gdi_inventory_datatables_g_area.py` para obter o inventário atual da área `g`

2. Faça uma busca por controllers que ainda usam o padrão legado:
   - `return Json(new { error = "` (padrão legado — usar `errorMessage` no lugar)
   - `alert(` em arquivos `.cshtml` (substituir por `LibMessageError`)

3. Verifique as áreas em ordem:
   - `Areas/gc/Controllers/` — Fases 8–12 (devem estar completas)
   - `Areas/g/Controllers/` — Fases 13–16 (verificar pendências)
   - `Areas/crm/Controllers/` — verificar se há GetDados sem try/catch
   - `Areas/a/Controllers/` — verificar se há GetDados sem try/catch
   - `Areas/qa/Controllers/` — verificar se há GetDados sem try/catch

4. Para cada área, reporte:
   - Controllers com padrão CORRETO (try/catch + errorMessage)
   - Controllers com padrão LEGADO (sem try/catch, com `error` solto)
   - Controllers com `JsonDataTableException` privado
   - Views com `xhr.dt` / `GdiDtNotifyJsonErrorMessage` correto
   - Views com evento de erro ausente

5. Apresente um resumo por área:
   | Área | Controllers OK | Pendentes | Views OK | Views pendentes |
   |------|---------------|-----------|----------|-----------------|

6. Identifique a próxima fase a implementar e liste os 5 controllers mais prioritários (critério: mais acessados ou com mais DataTables).

**Referência de padrão correto (servidor):**
```csharp
private JsonResult JsonDataTableException(Exception ex, JQueryDataTableParamModel param) {
    return Json(new {
        sEcho = param?.sEcho ?? 0,
        iTotalRecords = 0,
        iTotalDisplayRecords = 0,
        aaData = new object[0],
        yesFilterOnOff = false,
        errorMessage = ex.Message,
        severity = "error",
        stackTrace = ex.StackTrace
    }, JsonRequestBehavior.AllowGet);
}
```
