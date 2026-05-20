# AI-CONTEXT.md

Contexto técnico **fixo e atual** do **GDI-ERP-Plataform** para desenvolvimento assistido por IA (Cursor, Claude Code) e desenvolvedores humanos.

**Documentação complementar (não duplicar aqui em detalhe):**

| Ficheiro | Função |
|----------|--------|
| `CHANGELOG-DEV.md` | Estado atual, decisões ativas, últimas mudanças |
| `BACKLOG-DEV.md` | Pendências por prioridade |
| `CLAUDE.md` | Padrões longos, fases DataTables, listas de ficheiros críticos |
| `docs/dev-history/` | Histórico completo do changelog e arquivo |

---

## Projeto

- **Nome:** GDI-ERP-Plataform (GDI Aviação — ERP Plataform)
- **Natureza:** Monólito ASP.NET MVC customizado para importação, comercialização e distribuição de peças e componentes aeronáuticos (matriz BH-MG, filial SP).
- **Repositório único:** o portal público do cliente (**GDI-PortalCliente-Plataform**) foi descontinuado; fluxos vivem neste ERP (`UserIdentity`, área `crm`).
- **Publicação:** Visual Studio Publish → IIS (Windows Server). O agente **não** faz deploy remoto.

---

## Stack principal

| Camada | Tecnologia |
|--------|------------|
| Backend | ASP.NET MVC, **.NET Framework 4.7.2** |
| Dados | SQL Server — **um** padrão por fluxo (EF6 **ou** ADO.NET; não misturar) |
| UI | Bootstrap 5, AdminLTE 4, Font Awesome 7.2 |
| JS | DataTables 2.3.2 (bs5), SweetAlert2, Tempus Dominus 6.9.4 |
| Helpers UX | `LibUI_AdminLTE-4.0.0/plugins/startprime/js/start.js` (`LibMessage*`, `GdiDt*`, `GdiAjax*`) |

---

## Objetivo atual

Modernização e refatoração **incremental**, com foco em:

- Eliminação de débito (`LibDataSets`, filtros legados, POSTs órfãos, PascalCase paths).
- Padronização DataTables/Ajax e mensagens UX.
- Lookups centralizados (`ILookupQueryService`) sem regressão de RAM/UX.
- Documentação enxuta para IA (`AI-CONTEXT`, `CHANGELOG-DEV`, `BACKLOG-DEV`, `docs/dev-history/`).

---

## Estratégia de modernização

1. **Fases documentadas** — DataTables (Fases 0–17), lookups (Ondas 6a/6b), checklist em `.cursor/context/2026_05_20_checklist-pendencias-lookups-e-erp.md`.
2. **Cirurgia** — alterar só o necessário; uma PR de encoding separada de refactor funcional.
3. **Verificação automatizada** — scripts em `Scripts/2026_05_20_gdi_*` (inventários, verify csproj, UTF-8 BOM).
4. **Smoke manual** — após mudanças sensíveis (pedidos, NFe, financeiro, portal).
5. **Migração 4.8.1** — trilha isolada (ver `.cursor/context/2026_05_20_migracao-472-481.md`).

---

## Regras para alterações assistidas por IA

- **Língua:** português brasileiro nas respostas ao utilizador.
- **Leitura obrigatória antes de código:** `CHANGELOG-DEV.md` → `BACKLOG-DEV.md` → contexto em `.cursor/context/` se o tema estiver documentado.
- **Proibido (salvo pedido explícito):** `git push`, publish remoto, alterar schema SQL, remover métodos/rotas/ViewBags sem confirmação, misturar libs (Bootstrap/AdminLTE/DataTables) não alinhadas à stack.
- **Git:** não executar comandos git salvo pedido explícito do utilizador (regra do projeto).
- **Workspace:** apenas ficheiros dentro deste repositório.
- **Commits:** só quando o utilizador pedir.

---

## Áreas sensíveis

| Área | Risco | Referência |
|------|-------|------------|
| `Views/Shared/_Layout.cshtml`, `_Navbar.cshtml` | Impacto global, ordem de scripts | `BundleConfig`, `start.js` |
| `LibUI_.../start.js` | Todo o ERP (mensagens, DataTables, menu) | Incrementar `VersionERP` após mudança |
| `Web.config`, `connectionStrings.config` | IIS, segurança | Só com intenção documentada |
| `GDI-ERP-Plataform.csproj` | Publish — views novas com `Gdi*` devem estar em `Content Include` | `Scripts/2026_05_20_gdi_verify_csproj_gdi_helpers.py` |
| `Areas/gc/Controllers/MovimentosController.cs` | Pedidos, portal, e-mail templates | |
| `UserIdentityController` | Login, portal `*.portalflightx.com` | |
| `Robos/ENotas`, financeiro, boletos | Integrações externas | Smoke homologação |
| `Db/*.edmx`, metadata | EF — não alterar schema sem autorização | |

**Módulos:** COMEX (`gc`), Comercial, Estoque, Financeiro (`g`/`gc`), Qualidade (`qa`), Cadastros (`g`), Portal (`crm`).

---

## Padrão obrigatório antes de alterar código

1. Localizar ficheiro, action, view ou query exatos; mapear referências.
2. Identificar **causa raiz** (não corrigir só sintoma).
3. Classificar tabelas: **DataTables** vs **MVC** (`.cursor/context/2026_05_20_tabelas-datatables-vs-mvc.md`).
4. Verificar impacto: outras actions, partials, bundles, roles.
5. Build local (VS/MSBuild Release) quando houver alteração C#.
6. Scripts de verificação aplicáveis (`verify_csproj`, inventários).

---

## Padrão de registro no CHANGELOG-DEV.md

Após intervenção relevante:

1. Atualizar tabela **«Últimas alterações relevantes»** no topo de `CHANGELOG-DEV.md` (1 linha resumo + data).
2. Se necessário detalhe técnico: criar/atualizar ficheiro em `.cursor/context/` e referenciar na linha.
3. Mover pendências resolvidas/abertas em `BACKLOG-DEV.md`.
4. **Não** reexpandir o changelog com blocos longos — histórico extenso fica em `docs/dev-history/` ou context docs.

Formato legado (blocos `### [data]`) preservado apenas no arquivo histórico `CHANGELOG-DEV-HISTORICO-INICIAL.md`.

---

## Arquivos de referência

| Tipo | Caminho |
|------|---------|
| Changelog operacional | `CHANGELOG-DEV.md` |
| Backlog | `BACKLOG-DEV.md` |
| Histórico changelog | `docs/dev-history/CHANGELOG-DEV-HISTORICO-INICIAL.md` |
| Checklist ERP | `.cursor/context/2026_05_20_checklist-pendencias-lookups-e-erp.md` |
| Regras Cursor | `.cursor/rules/2026_05_20_gdi-erp-plataform.mdc` (ordem de leitura: este ficheiro → `CHANGELOG-DEV` → `BACKLOG-DEV`) |
| Padrões Claude | `CLAUDE.md` |
| Lookups | `.cursor/context/2026_05_20_lookups-libdatasets.md` |
| DataTables vs MVC | `.cursor/context/2026_05_20_tabelas-datatables-vs-mvc.md` |
| Migração 4.8.1 | `.cursor/context/2026_05_20_migracao-472-481.md` |

**Scripts úteis (raiz do repo):**

```powershell
python Scripts/2026_05_20_gdi_verify_csproj_gdi_helpers.py
python Scripts/2026_05_20_gdi_inventory_libdatasets_usage.py --fail
python Scripts/2026_05_20_gdi_inventory_utf8_bom.py --fail
```
