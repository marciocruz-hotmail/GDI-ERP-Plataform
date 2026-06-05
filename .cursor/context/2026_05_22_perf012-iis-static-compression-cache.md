# PERF-012 — Compressão e cache de estáticos (IIS)

**Data:** 2026-05-20  
**Objetivo:** `Content-Encoding: gzip` (ou br se configurado) em JS/CSS; **304** em reload quando `?v=VersionERP` inalterado.

---

## 1. Política recomendada (produção)

| Camada | O quê |
|--------|--------|
| **IIS (site)** | Compressão estática + dinâmica (preferível para todo o pool) |
| **Aplicação** | `Web.Release.config` — `urlCompression` + `clientCache` em `LibUI_AdminLTE-4.0.0` e `Content` (aplicado no publish Release) |
| **Cache-bust** | Query `?v=@VersionERP` já usada em `_Layout.cshtml` / `_Blank.cshtml` para `start.css`, `start.js`, `Content/*` versionados |

**Dev local (Debug):** compressão/cache no `Web.config` transformado **não** substitui instalar **Dynamic Compression** no Windows/IIS se o módulo estiver ausente.

---

## 2. IIS Manager (servidor / site GDI-ERP)

### 2.1 Instalar compressão (uma vez no servidor)

1. **Server Manager** → **Add Roles and Features** → servidor → **Web Server (IIS)** → **Web Server** → **Performance**:
   - [x] **Static Content Compression**
   - [x] **Dynamic Content Compression**
2. Ou: **Programs and Features** → **Turn Windows features on** → IIS → World Wide Web Services → Performance → marcar os dois itens acima.

### 2.2 Habilitar no site

1. **IIS Manager** → site da aplicação (ex. `GDI-ERP-Plataform`).
2. **Compression** (duplo clique):
   - [x] **Enable dynamic content compression**
   - [x] **Enable static content compression**
3. **Configuration Editor** → secção `system.webServer/httpCompression`:
   - Confirmar `staticTypes` inclui `text/*`, `application/javascript`, `application/json`, `image/svg+xml`.
   - `dynamicTypes` inclui `text/*`, `application/x-javascript`, `application/javascript`.

### 2.3 Cache de ficheiros estáticos (opcional no IIS, além do Web.config Release)

1. **Configuration Editor** → `system.webServer/staticContent` → **clientCache**:
   - `cacheControlMode` = `UseMaxAge`
   - `cacheControlMaxAge` = `7.00:00:00` (7 dias; alinhar ao `Web.Release.config`)
2. Ou por pasta virtual: se existir VDIR para `LibUI_AdminLTE-4.0.0`, repetir `clientCache` só nessa VDIR.

**Nota:** ficheiros **sem** `?v=VersionERP` (ex. `adminlte.min.css` base) podem cachear até ao próximo deploy; ficheiros com `?v=` invalidam quando `ControlVersion` / `VersionERP` sobe.

---

## 3. Web.config (publish Release)

O transform `Web.Release.config` acrescenta:

- `urlCompression` (`doStaticCompression`, `doDynamicCompression`)
- `<location path="LibUI_AdminLTE-4.0.0">` e `<location path="Content">` com `clientCache` 7 dias

**Não** alterar `Web.config` base (Debug local) para não mudar comportamento de desenvolvimento.

---

## 4. Validação (aceite)

### 4.1 Gzip

1. Publicar em **Release** (ou IIS local com transform aplicado).
2. DevTools → **Network** → pedido a `start.js?v=...` ou `sweetalert2.min.js` (bundle).
3. Response headers: `Content-Encoding: gzip` (ou `br`).

**PowerShell (servidor):**

```powershell
$r = Invoke-WebRequest -Uri "https://SEU_HOST/LibUI_AdminLTE-4.0.0/plugins/startprime/js/start.js?v=2026.51.02" -Headers @{ "Accept-Encoding" = "gzip" } -UseBasicParsing
$r.Headers["Content-Encoding"]
```

### 4.2 304 em reload

1. Primeiro GET: status **200**, `Cache-Control` com `max-age` (se `clientCache` ativo).
2. **F5** ou recarregar mesma URL (mesmo `?v=`): esperado **304 Not Modified** (se `If-Modified-Since` / etag suportado pelo IIS para esse ficheiro).

### 4.3 Após bump `VersionERP`

1. Incrementar versão em `ControlVersion` / fluxo habitual.
2. URL com novo `?v=` → **200** (novo conteúdo ou revalidação).

---

## 5. Troubleshooting

| Sintoma | Causa provável |
|---------|----------------|
| Sem `Content-Encoding` | Módulo compressão não instalado ou desligado no site |
| Sempre 200, nunca 304 | `clientCache` ausente; ou ficheiro servido via MVC/bundle sem cache estático |
| Bundle não minificado | `BundleTable.EnableOptimizations` — ver **PERF-013** (`Global.asax.cs` + build Release) |

---

## 6. Relacionado

- **PERF-013** — minificação bundles (`~/bundles/libui-swal-compat`)
- Google Analytics removido do projeto (2026-05-20)
- `CHANGELOG-DEV.md` — incrementar `VersionERP` após alterar `start.js` / `start.css`
