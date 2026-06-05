# Grupo 2.10 — Ficheiros órfãos no disco (ProdutosTipos)

**Data:** 2026-05-20  
**Checklist:** `.cursor/context/2026_05_22_checklist-pendencias-lookups-e-erp.md` §2.10

## Arquitetura de verificação

| Camada | O que verificar | Ferramenta / critério |
|--------|-----------------|------------------------|
| **Módulo MVC órfão** | Pasta `Areas/g/Views/ProdutosTipos/` + `ProdutosTiposController.cs` | Existência no disco; entradas `Compile` / `Content` no `GDI-ERP-Plataform.csproj` |
| **Funcionalidade de negócio** | Cadastro de tipos de produto | Tabela `g_produtos_tipos` via **combo** em `Produtos/CreateEdit` (`GetComboGProdutosTipos`), não CRUD dedicado |
| **Publish DataTables/Ajax** | Views com `GdiAjax*` / `GdiDt*` fora do `.csproj` | `python Scripts/2026_05_20_gdi_verify_csproj_gdi_helpers.py` |
| **Órfãos gerais em Areas** | Qualquer `.cs` em Controllers ou `.cshtml` em Areas sem entrada no csproj | Scan PowerShell (regex `Compile` / `Content` vs disco) — lote 2026-05-20 |

## Decisão — ProdutosTipos

| Item | Estado no disco (2026-05-20) | Ação |
|------|------------------------------|------|
| `Areas/g/Views/ProdutosTipos/*` | **Inexistente** | Nenhuma — já removido (histórico: remoção modal filtro / commit `f8b68b9`) |
| `Areas/g/Controllers/ProdutosTiposController.cs` | **Inexistente** | Nenhuma |
| Entradas `ProdutosTipos` no `.csproj` | **Zero** | Coerente com disco |
| §2.1 checklist (`ProdutosTipos/Index`) | **N/A** | Mantido |

**Conclusão:** não há ficheiros a apagar nem a incluir no `.csproj`. O cadastro de tipos permanece como lookup/combo (`Lib/Lookups`, `ProdutosController.Lookups.cs`, `Produtos/CreateEdit.cshtml`).

## Verificações executadas

```
python Scripts/2026_05_20_gdi_verify_csproj_gdi_helpers.py
→ cshtml_with_GdiAjax_or_GdiDt: 183
→ missing_in_csproj: 0
→ exit 0
```

Scan Areas (controllers `.cs` + views `.cshtml` vs `.csproj`):

- Controllers fora do csproj: **0**
- Views fora do csproj: **0**

## Referências úteis (não órfãos)

- `Lib/Lookups/LookupQueryService.CadastrosG.cs` — `GetComboGProdutosTipos`
- `Areas/g/Controllers/ProdutosController.Lookups.cs` — `ViewBag.comboProdutosTipos`
- `Db/Metadata/g_produtos_tiposMetadata.cs` — entidade EF (dados, não UI MVC)

## Próximos órfãos (fora do escopo 2.10)

- Revisar outros módulos removidos no mesmo lote PascalCase/modal filtro apenas se o checklist ou inventário apontar pasta residual — não aplicável a ProdutosTipos.
