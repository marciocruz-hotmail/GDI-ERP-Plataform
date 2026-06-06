<!-- GERADO AUTOMATICAMENTE por .claude/scripts/sync_changelog_recent.py -->
<!-- Fonte: CHANGELOG-DEV.md (raiz) | Últimas 5 entradas -->

# CHANGELOG-DEV — Entradas Recentes

> Gerado automaticamente. Histórico completo: `CHANGELOG-DEV.md` e `docs/dev-history/`.

---

## Últimas alterações (5)

### 2026-05-25 — NuGet Lote A: SkiaSharp + MathNet removidos; docs sem trilha ASP.NET Core
- Removidos `SkiaSharp`, `SkiaSharp.NativeAssets.Win32` e `MathNet.Numerics.Signed` (usings mortos); pastas `packages/` apagadas. Build Release + testes lookups OK. Decisão: **sem** migração ASP.NET Core; manifesto **78** pacotes.

---

### 2026-05-25 — System.Runtime.Caching: remoção pacote NuGet órfão (GAC mantido)
- Retirado `System.Runtime.Caching` 10.0.7 de `packages.config`; pasta `packages/System.Runtime.Caching.10.0.7` apagada. Referência de framework em `.csproj`/testes e uso de `MemoryCache` preservados (GAC .NET 4.7.2). Build Release OK.

---

### 2026-05-25 — ZString: remoção de pacote órfão (sem uso no código)
- Retirado `ZString` 2.6.0 de `packages.config` e `.csproj`; pasta `packages/ZString.2.6.0` apagada. Build Release OK; `ZString.dll` deixa de ir para o publish.

---

### 2026-05-25 — SkiaSharp: remoção NativeAssets Linux/macOS (IIS Windows)
- Retirados `SkiaSharp.NativeAssets.Linux.NoDependencies` e `SkiaSharp.NativeAssets.macOS` de `packages.config` e `.csproj` (Error/Import); pastas físicas apagadas em `packages/`. Mantidos `SkiaSharp` + `SkiaSharp.NativeAssets.Win32`. Publish ~141 MB mais leve; sem impacto em runtime IIS.

---

### 2026-05-25 — ReportEmailPedido: HTML de e-mail alinhado a Bootstrap 5.3.8
- `getEmailOrcamentoPedido` — CDN BS 4.3.1/FA 4.7 trocados por Bootstrap **5.3.8** e Font Awesome **7.2.0** (jsDelivr); markup `panel`/`borderless` → `card`/`table-borderless`/`table-bordered`; removidos scripts JS (inúteis em e-mail); correções `utf-8`, nº movimento dinâmico e data validade.

---
