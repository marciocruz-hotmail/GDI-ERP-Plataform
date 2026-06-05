# AI-CONTEXT.md

Contexto técnico **fixo** do **GDI-ERP-Plataform** para IA e developers. Detalhe por tema → **`.cursor/context/2026_06_05_indice-memoria-ia.md`**.

| Ficheiro | Função |
|----------|--------|
| **`CHANGELOG-DEV.md`** | Estado atual + últimas alterações (compacto) |
| **`BACKLOG-DEV.md`** | Pendências ativas (IDs G-PUB, G-DT, …) |
| **Índice temas** | `.cursor/context/2026_06_05_indice-memoria-ia.md` |
| **Regras agente** | `.cursor/rules/2026_05_20_gdi-erp-plataform.mdc` (formato resposta, git, publish) |
| **Histórico** | `docs/dev-history/CHANGELOG-DEV-HISTORICO-INICIAL.md` |

---

## Projeto

Monólito **ASP.NET MVC .NET Framework 4.7.2** — COMEX, comercial, estoque, financeiro, qualidade (GDI Aviação, BH/SP). Portal cliente **integrado** (`crm`, `UserIdentity/AcessoPortal`); repositório **GDI-PortalCliente** descontinuado. Publish: VS → IIS; agente **sem** deploy remoto nem git salvo pedido explícito.

---

## Stack

| Camada | Tecnologia |
|--------|------------|
| Backend | ASP.NET MVC **4.7.2** |
| Dados | SQL Server — **EF6 ou ADO.NET por fluxo** (não misturar) |
| UI | Bootstrap 5, AdminLTE 4, Font Awesome 7.2 |
| JS | DataTables 2.3.2 bs5, SweetAlert2, Tempus Dominus 6.9.4 |
| UX global | `LibUI_.../start.js` — `LibMessage*`, `GdiDt*`, `GdiAjax*` |

---

## Ordem de leitura (antes de código)

1. Este ficheiro → `CHANGELOG-DEV.md` → `BACKLOG-DEV.md`
2. Tema específico → **índice memória** → ficheiro `.cursor/context/` correspondente
3. Formato da resposta ao utilizador → `.mdc` §6 (não duplicar noutros `.md`)

---

## Arquitetura centralizada (obrigatória)

**Detalhe:** `.cursor/context/2026_06_05_arquitetura-centralizada-erp-gdi.md`  
**Lib:** `Lib/GdiMvcJsonResults.cs` | **Referência ouro:** `MovimentosController`

| Situação | Regra resumida |
|----------|----------------|
| DataTables `GetDados*` | `param` nulo, `try/catch`, sucesso/erro via `GdiMvcJsonResults` |
| Modal GET | `int.TryParse`; `Find` null → `ViewBag.MsgBloqueio` + model mínimo; view oculta form/DT |
| Ajax POST | `{ success, msg }`; `catch` → `GdiMvcJsonResults.AjaxFailure*` |
| Views JS | DT: `error.dt`/`xhr.dt`; Ajax rede: `GdiAjaxNotifyInconsistencias` — **sem** `alert()` |
| Proibido | `GC.Collect()` em MVC; `int.Parse` em modal; side-effect em `GetDados*` |

**Smoke (exit 0):** `python Scripts/2026_06_05_gdi_smoke_architecture_inventories.py`

---

## Áreas sensíveis

| Área | Nota |
|------|------|
| `_Layout.cshtml`, `start.js` | Impacto global; incrementar `VersionERP` após mudança |
| `GDI-ERP-Plataform.csproj` | Views novas com `Gdi*` → `verify_csproj_gdi_helpers.py` |
| `MovimentosController`, `UserIdentityController` | Pedidos, portal, e-mail |
| `Web.config`, EF `.edmx` | Só com intenção documentada |
| `Robos/ENotas`, financeiro | Smoke homologação |

**Tabelas:** classificar **DataTables** vs **MVC** antes de CSS/JS — `2026_05_20_tabelas-datatables-vs-mvc.md`.

---

## Lookups (resumo)

- **Index/filtro:** combo local na action (sem cache global).
- **CreateEdit/modal:** `ILookupQueryService` + `*.Lookups.cs`.
- Convenção: `2026_05_20_lookups-convencao-index-vs-createedit.md`.

---

## Verificação pós-alteração

```powershell
python Scripts/2026_06_05_gdi_smoke_architecture_inventories.py
python Scripts/2026_05_20_gdi_verify_csproj_gdi_helpers.py
```

Lista completa de inventários → **índice memória** § Scripts.

---

## Registo pós-intervenção

1. Linha em `CHANGELOG-DEV.md` § «Últimas alterações»
2. `BACKLOG-DEV.md` — marcar pendência resolvida/aberta
3. Detalhe técnico longo → novo `.cursor/context/AAAA_MM_DD_*.md` (prefixo = data criação)

**Convenção nomes:** `AAAA_MM_DD_` em `.cursor/context/`, `.cursor/rules/`, `Scripts/` — ver `.mdc` §4.5; inventário `2026_06_05_gdi_inventory_prefixed_file_dates.py`.
