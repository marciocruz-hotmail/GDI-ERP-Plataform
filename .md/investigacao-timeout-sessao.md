# Investigação: timeout de sessão e erros associados a cache — ERP GDI Plataform

**Data do levantamento:** 2026-05-11  
**Âmbito:** apenas investigação e mapeamento (sem correções de código na fase de levantamento).  
**Stack:** ASP.NET MVC (.NET Framework 4.7.2), sessão InProc, identidade em `System.Runtime.Caching.MemoryCache`.

---

## 1. Resumo executivo

Os sintomas “**Tempo de conexão expirado, Efetue nova conexão**” e falhas que o utilizador descreve como “**cache**” estão, no código, fortemente ligados a **dois armazéns distintos** — **sessão ASP.NET** (`Session["TokenId"]`, etc.) e **`MemoryCache`** (`CachePersister.userIdentity` e derivados) — com **janelas de ~15 minutos** cada, mais **redirect MVC** em vez de **401 JSON** para pedidos AJAX/modais. A causa **mais provável** é **desincronização** entre sessão ainda com `TokenId` e entrada em cache já expirada/evictada ou perdida após **recycle** do worker; em segundo plano, **302 + HTML de login** em **`$("#mainModal").load(...)`** explica erros “estranhos” no cliente. O repositório **não** expõe logging estruturado (Serilog/NLog/Application Insights) nem `machineKey`/sessão distribuída — **multi-instância sem sticky session** continua a ser risco infraestrutural a validar à parte.

---

## 2. Inventário de configurações

### 2.1 Sessão

| Item | Valor / local |
|------|----------------|
| Timeout | **`Web.config`** `<sessionState timeout="15" />` (15 minutos de inatividade, renovação típica por pedido). |
| Modo | **`mode` não definido** → omissão ASP.NET = **InProc** (memória do processo). |
| Cookie explícito (SameSite, HttpOnly, Secure) | **Não** configurado no `Web.config` analisado; cookies usam predefinições do runtime/IIS. |
| `SessionStateBehavior` | Sem `[SessionState]` encontrado nos controllers analisados; omissão MVC. |
| Alinhamento cliente | `Scripts/sessionInactivity.js` — **900 s**; comentários em `_Layout.cshtml` alinham com 15 min. |

### 2.2 Cache

| Item | Valor / local |
|------|----------------|
| Cache de identidade / contexto | **`CachePersister`** (`Security/CachePersister.cs`) — `MemoryCache.Default`, chaves com sufixo `Session["TokenId"]`. |
| Política | **`SlidingExpiration = 15 minutos`** nas propriedades mapeadas. |
| Outro cache de app | `Lib/LibCache.cs` — apenas `GC.Collect` (não invalida sessão nem `MemoryCache` de negócio). |

### 2.3 Autenticação / autorização

| Item | Valor / local |
|------|----------------|
| Mecanismo | **`[CustomAuthorize]`** (`Security/CustomAuthorizeAttribute.cs`) sobre controllers; **não** há `<authentication mode="Forms">` no `Web.config` analisado. |
| Identificador de sessão lógica | `Session["TokenId"]` = `Session.SessionID` na página de login (`UserIdentityController`). |
| Cookie auth Core | **Não aplicável** da mesma forma; não há `ExpireTimeSpan` de Cookie Authentication. |
| Web API | `api/JobServer` com **`[AllowAnonymous]`** + chave em `appSettings` — **fora** do modelo de sessão MVC. |

### 2.4 Filtros e pipeline

| Item | Valor / local |
|------|----------------|
| Filtros globais | `HandleErrorAttribute` apenas (`App_Start/FilterConfig.cs`). |
| `Global.asax.cs` | Só `Application_Start`; **sem** `Application_Error` / `BeginRequest` custom. |
| Middleware tipo Core | **Inexistente** (`Program.cs` / `Startup.cs` não aplicáveis). |

---

## 3. Mapa da mensagem de erro

### 3.1 “Tempo de conexão expirado, Efetue nova conexão”

| Aspeto | Detalhe |
|--------|---------|
| **Onde** | `Security/CustomAuthorizeAttribute.cs` — `TempData["Info"]` no ramo `userIdentity == null` **e** `Session["TokenId"]` **não** vazio. |
| **Servidor vs cliente** | Gerada no **servidor**; exibida na **view** `Views/UserIdentity/Index.cshtml` via `TempData["Info"]` após **redirect**. |

### 3.2 Mensagens relacionadas (sessão / login)

| Mensagem | Onde | Disparo |
|----------|------|---------|
| “A sessão foi encerrada devido à inatividade…” | `UserIdentityController.Logout` | Query `?reason=inactivity` (ex.: `sessionInactivity.js`). |
| “Senha alterada com sucesso…” / erros de troca obrigatória | `UserIdentityController` | Fluxos de senha. |

### 3.3 Buscas por outras strings pedidas

- **“sessão expirada” / “Session expired”** (literal): **sem** ocorrências mapeadas no código da app.  
- **`Session.IsAvailable` / `Session.GetString`**: **não** usados (APIs ASP.NET Core).

---

## 4. Hipóteses rankeadas (síntese)

| # | Nome curto | Prob. | Evidência resumida |
|---|------------|-------|---------------------|
| 1 | Desincronização `Session` ↔ `MemoryCache` | **Alta** | `CustomAuthorize` + `CachePersister` sliding 15 min + getters com `catch` → `null`. |
| 2 | AJAX/modais + **302** + HTML login | **Alta** | Sem `IsAjaxRequest`; muitos `$("#mainModal").load(...)`; sem handler global AJAX. |
| 3 | Recycle / **farm** sem sessão partilhada | **Média–Alta*** | InProc + `MemoryCache` por processo; sem `machineKey` no repo. *Depende da infra. |
| 4 | Eviction / `MemoryCache.Add` / duplicados | **Média** | `_cache.Add`; pressão de memória. |
| 5 | `Session.Timeout = 480` sem restaurar | **Média** | `FinanceiroLancamentosController.ModalGerarFinanceiroMovimentos`. |
| 6 | Fallback `dataBase` → `"127.0.0.1"` | **Baixa–Média** | `CachePersister.dataBase` getter. |
| 7 | Código fora de `HttpContext` (robôs) | **Baixa** | `CachePersister` + `HttpContext.Current`; ex.: `RoboCotacaoDolar`. |

**Inconsistência documental:** `UserIdentity.DataHoraExpiracao = Now + 4h` no login com comentário “tempo máximo de sessão”, mas **nenhum** código lê esta propriedade — o comportamento real é **15 min** (sessão + cache).

---

## 5. Próximos passos recomendados

### 5.1 Correções imediatas (baixo risco, baixo esforço) — *após aprovação de alteração*

- Introduzir **logging mínimo** no ramo de `CustomAuthorize` quando `userIdentity == null` (SessionId, se `TokenId` vazio ou não, **sem** dados sensíveis).  
- Evitar **`catch (Exception) { }` vazio** em `CachePersister` / `Contexto` / `_Layout` ou substituir por log + rethrow quando seguro.  
- Rever **`CachePersister.dataBase`**: fallback `"127.0.0.1"` é perigoso; preferir `null` + tratamento explícito.  
- Em **`ModalGerarFinanceiroMovimentos`**: garantir **`Session.Timeout`** restaurado em `try/finally` se a intenção for só alargar durante a action.

### 5.2 Correções com teste e janela de deploy

- **Resposta diferenciada para AJAX** (401/403 JSON ou 401 com corpo JSON + header) vs redirect HTML, e ajustar **handlers** globais ou `$.ajaxSetup` / wrapper interno.  
- Avaliar **sessão State Server ou SQL Server** ou **sticky session** consistente se existir **mais do que uma instância**.  
- Publicar **`machineKey`** explícito no processo de deploy se vários servidores partilharem cookies/viewstate (mesmo com modelo actual de auth custom).  
- Substituir **`MemoryCache.Add`** por **`Set`** ou remover antes de `Add` para evitar excepção em re-login.

### 5.3 Investigações adicionais (antes de grandes refactors)

- Confirmar na **infra**: número de instâncias IIS, ARR/nginx, **affinidade de sessão**, políticas de **recycle**.  
- Correlacionar horários dos relatos com **recycle** / **deploy**.  
- Validar timeouts de **proxy** vs `httpRuntime executionTimeout="3600"`.  
- Revisão pontual de **`RoboCotacaoDolar`** (e outros robôs) quanto a uso de `CachePersister` sem `HttpContext`.

---

## 6. Referências de ficheiros principais

- `Web.config` — `sessionState`, `httpRuntime`, `customErrors`, rewrite HTTPS.  
- `Security/CustomAuthorizeAttribute.cs` — mensagem “Tempo de conexão expirado…”.  
- `Security/CachePersister.cs` — `MemoryCache`, chaves, `SlidingExpiration`, `logout`.  
- `Controllers/UserIdentityController.cs` — login, `TokenId`, `Logout`, `DataHoraExpiracao`.  
- `Scripts/sessionInactivity.js` — logout por inatividade.  
- `Views/Shared/_Layout.cshtml` — `GDI_SessionTimeout`, `RenderAction` Navbar com try/catch.  
- `Areas/gc/Controllers/FinanceiroLancamentosController.cs` — `Session.Timeout = 480` em `ModalGerarFinanceiroMovimentos`.

---

*Documento gerado no âmbito da investigação PASSO 1–7; alterações de código devem seguir revisão e testes próprios da equipa.*
