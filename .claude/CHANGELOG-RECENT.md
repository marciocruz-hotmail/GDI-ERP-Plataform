<!-- GERADO AUTOMATICAMENTE por .claude/scripts/sync_changelog_recent.py -->
<!-- Fonte: CHANGELOG-DEV.md (raiz) | Últimas 5 entradas -->

# CHANGELOG-DEV — Entradas Recentes

> Gerado automaticamente. Histórico completo: `CHANGELOG-DEV.md` e `docs/dev-history/`.

---

## Últimas alterações (5)

### 2026-06-05 — packages.config: auditoria e remoção de 26 pacotes mortos
- **Validado:** 112 → **86** pacotes; versões alinhadas ao `.csproj` e `targetFramework="net472"`.
- **Removidos:** bloco Microsoft.Graph/Kiota/Identity/Azure (zero uso em código); órfãos `jQuery`, `Modernizr`, `Microsoft.AspNet.WebApi` (metapacote), `SixLabors.ImageSharp` (só props).
- **Sincronizado:** `.csproj` (References/Import), `Web.config` (binding redirects), `using` mortos em `CustomPrincipal` e `FinanceiroLancamentosController`.
- **Build Release OK.** Candidatos futuros: `SkiaSharp.NativeAssets.Linux/macOS`, `ZString`, `bootstrap` NuGet (UI ativa = LibUI).

---

### 2026-06-05 — Web.config: auditoria e alinhamento de versões
- **7 ficheiros** revistos: raiz (`Web.config`, `Web.Debug.config`, `Web.Release.config`), `Views/Web.config`, áreas `a|g|gc|qa|crm`.
- **Correções:** `MvcWebRazorHostFactory` e assembly MVC em todas as Views → **5.3.0.0** (NuGet `Microsoft.AspNet.Mvc` 5.3.0); binding redirects IdentityModel (Tokens, Protocols, Jwt, JsonWebTokens, Logging) **8.15.0.0 → 8.18.0.0** alinhados a `packages.config`.
- **Validado:** `Scripts/2026_05_22_gdi_verify_web_release_transform.ps1` — Release sem `debug`, `customErrors On`, compressão PERF-012 OK; áreas `web.config` já no `.csproj`.

---

### 2026-06-05 — GDI-ERP-Plataform.csproj: auditoria e correções
- **Validação:** 1069 Includes, 0 paths em falta, 0 duplicados, 0 views `Gdi*` fora do csproj.
- **Correções:** SourceLink fallback 8.0.0 → 10.0.203; `ExcludeFoldersFromDeployment` + strip publish `.claude`; `None` para `AI-CONTEXT`/`CHANGELOG`/`BACKLOG`; `Content` templates `appSettings` + novo `sql-server.local.config.example`; script `2026_06_05_gdi_verify_csproj_includes.py`.

---

### 2026-06-05 — Remoção `nul)` acidental na raiz (F06)
- Artefacto cmd (redirecionamento `2>nul)` inválido); apagado com `Remove-Item -LiteralPath`; F06 resolvido em `docs/AVALIACAO-TECNICA-ERP-GDI.md`.

---

### 2026-06-05 — Remoção pasta `.md/` legada: csproj e refs
- Pasta **`.md/`** removida (duplicata de `docs/`); canónico: `docs/investigacao-timeout-sessao.md`, `docs/relatorio-migracao-netframework-472-481.md`.
- **`.csproj`:** removidos `Content` `.md\*`; `CLAUDE.md` e docs relevantes → `None Include` (versionam no Git, **não** vão ao IIS).
- **`roadmap.md`** / `0004 - auditoria-tecnica-relatorio.md` só existiam em `.md/` — recuperar do histórico Git se ainda forem necessários.

---
