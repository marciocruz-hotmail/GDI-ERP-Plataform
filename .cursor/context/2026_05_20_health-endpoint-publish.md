# Health check e publish Release (PUB-1 / PUB-2)

**Data:** 2026-05-20

## Endpoint

| URL | Método | Auth | Resposta |
|-----|--------|------|----------|
| `/health` | GET | Anónimo (`[AllowAnonymous]`) | JSON `{ ok, app, version, utc }` |

Implementação: `Controllers/HealthController.cs`, rota em `App_Start/RouteConfig.cs`.

**Nota:** `CustomAuthorizeAttribute` passou a respeitar `[AllowAnonymous]` (necessário para health e futuros endpoints públicos MVC).

## PUB-1 — Release / IIS

1. Publish com **Configuration = Release**.
2. Validar transform local:
   ```powershell
   .\Scripts\2026_05_20_gdi_verify_web_release_transform.ps1
   ```
3. No servidor publicado, confirmar `Web.config`:
   - `<compilation>` **sem** `debug="true"`
   - `<customErrors mode="On" />`
4. IIS: compressão estática/dinâmica (PERF-012) — ver `2026_05_20_perf012-iis-static-compression-cache.md`.

## PUB-2 — Pós-publish

1. Incrementar `ControlVersion` quando `start.js` / `start.css` ou lote publish relevante mudar (`2026.51.03` — PUB + health).
2. Smoke automatizado (site local a correr):
   ```powershell
   .\Scripts\2026_05_20_gdi_smoke_health_login.ps1 -BaseUrl https://SEU_HOST
   ```
3. Smoke manual: login → navbar → assets com `?v=` = `getShortVersion()`.

## Aceite

- [x] Transform Release sem debug, `customErrors On`
- [x] GET `/health` → 200 + `ok: true` + `version` alinhada a `ControlVersion`
- [ ] Smoke manual pós-deploy em homologação/produção (operador)
