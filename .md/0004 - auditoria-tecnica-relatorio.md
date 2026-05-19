# AUDITORIA TÉCNICA — ERP GDI PLATAFORMA
## FASE 1: Diagnóstico Sem Alteração de Código

**Data:** 2026-05-18  
**Stack auditada:** ASP.NET MVC .NET Framework 4.7.2 | SQL Server | Bootstrap 5 | AdminLTE 4 | IIS Windows  
**Status:** Leitura completa. Nenhum arquivo alterado.

---

## CONTEXTO

O ERP GDI é um sistema de gestão aeronáutica com múltiplas áreas funcionais (g, gc, a, crm, qa). O objetivo desta auditoria é mapear riscos reais, inconsistências e oportunidades de melhoria **dentro da stack atual**, sem reescrita, sem troca de framework e sem aumento de complexidade. A auditoria serve de base para um plano de ação graduado por risco.

---

## 1. MAPA TÉCNICO DO PROJETO

### Estrutura de Áreas e Controllers

| Área | Caminho | Controllers | Descrição |
|------|---------|-------------|-----------|
| root | Controllers/ | 11 | UserIdentity, Home, Error, JobServer, Navbar |
| g | Areas/g/Controllers/ | 27 | Clientes, Financeiro, NF-e, GED, Usuários |
| gc | Areas/gc/Controllers/ | 24 | COMEX, Estoque, Pedidos, CFOP, Movimentos |
| a | Areas/a/Controllers/ | 3 | Filtros, Parâmetros, Auditoria |
| crm | Areas/crm/Controllers/ | 2 | Portal Cliente externo |
| qa | Areas/qa/Controllers/ | 3 | Treinamentos LMS, GED qualidade |
| **Total** | | **70** | |

### Padrões de Acesso a Dados

| Padrão | Volume estimado | Observação |
|--------|----------------|------------|
| LINQ-to-Entities (EF 6) | ~50% | DbContext Database-First, 170+ DbSets |
| SqlQuery + ADO.NET | ~50% | Via LibDB.cs (helper centralizado) |
| sql_filtro bruto persistido | 10+ controllers | **Raiz da superfície de SQL Injection** |

### Autenticação e Sessão

- `CustomAuthorizeAttribute` + `CustomPrincipal` + `CachePersister` (MemoryCache, chave = "entidade_TokenId")
- Timeout de sessão: 15 minutos (SlidingExpiration)
- **Não usa ASP.NET Identity** — sistema próprio completo
- Multi-tenant via subdomínio (`SetTenants()` em `UserIdentityController.cs`)

### Security Headers (confirmados no Web.config)

- ✅ X-Frame-Options: SAMEORIGIN
- ✅ X-Content-Type-Options: nosniff
- ✅ Referrer-Policy: strict-origin-when-cross-origin
- ✅ CSP presente (com `unsafe-inline` + `unsafe-eval` — necessário para Razor inline)
- ✅ X-Powered-By removido
- ✅ HTTPS redirect configurado no IIS rewrite

---

## 2. ACHADOS — MATRIZ COMPLETA

### P0 — Crítico (antes do próximo deploy)

#### P0-01: SQL Injection — sql_filtro executado como SQL literal
- **Arquivo-raiz:** `Lib/LibDB.cs` → `setFilterByUser` (grava SQL literal de input do usuário no banco)
- **Controllers afetados (10+):**
  - `Areas/g/Controllers/AssistentesController.cs:65` — modo advanced: SQL completo injetado
  - `Areas/g/Controllers/NfeController.cs:204` — modo advanced: SQL completo injetado
  - `Areas/g/Controllers/CidadesController.cs:73` — fragmento após WHERE
  - `Areas/g/Controllers/ContratosAviacaoController.cs:73` — fragmento após WHERE
  - `Areas/gc/Controllers/ComexProdutosController.cs:95+115` — SQL completo + COUNT + paginação
  - + ClientesController, FinanceiroController, GedController, ProdutosController (fragmentos)
- **Código de risco confirmado (ComexProdutosController.cs:95):**
  ```csharp
  sentencaSql = "SELECT * FROM gc_comex_produtos WHERE ativo = 1 AND " + sentencaSql;
  // ... depois:
  string sqlPage = "SELECT * FROM (" + sentencaSql + ") T " + orderBy + " OFFSET " + start + " ROWS FETCH NEXT " + length + " ROWS ONLY";
  ```
- **Risco:** Exfiltração de dados, alteração de registros, possível RCE via stored procedures
- **Correção:** Validar `sql_filtro` no ponto de gravação (`setFilterByUser`): allowlist de tokens SQL seguros + blocklist de tokens perigosos (DROP, DELETE, UPDATE, UNION, EXEC, xp_, --)

#### P0-02: SQL Injection — concatenação de listaCampos[] em queries
- **Arquivos:**
  - `Areas/g/Controllers/CidadesController.cs:90+94`
  - `Areas/g/Controllers/FinanceiroFaturamentosController.cs:82-94`
  - `Areas/g/Controllers/ContratosAviacaoController.cs:90-102`
- **Código de risco confirmado (CidadesController.cs):**
  ```csharp
  SentencaSQL += " and c.id_cidade = " + listaCampos[0].ToString().Trim();
  SentencaSQL += " and c.nome like '%" + listaCampos[1].ToString().Trim() + "%'";
  ```
- **Correção:** Substituir concatenação por `SqlParameter` tipado para cada campo

#### P0-03: SQL Injection — SqlQuery concatenado em RoboEnotasNFE.cs
- **Arquivo:** `Robos/ENotas/RoboEnotasNFE.cs:619-621+1063-1067`
- **Código confirmado:**
  ```csharp
  db.g_produtos.SqlQuery("select p.* from g_produtos p join gc_movimentos_itens i on(p.id_produto = i.id_produto) where i.id_movimento = " + record_gc_movimento_nf.id_movimento.EmptyIfNull().ToString()).ToList();
  ```
  E também (linha ~1063):
  ```csharp
  string sqlPedidos = " select mov.* from gc_movimentos mov where id_cliente = " + recordCliente.id_cliente.ToString()
      + " and mov.datahora_aprovacao > '" + dataLimiteSql + "' "
  ```
- **Risco adicional:** `dataLimiteSql` entre aspas simples sem escape — SQL Injection em campo de data
- **Correção:** Substituir por `.Where(i => i.id_movimento == val)` (LINQ) ou `SqlQuery` com `@param`

---


### P2 — Médio (backlog próximo mês)
| ID | Achado | Arquivo | Ação |
|----|--------|---------|------|
| P2-02 | SELECT * em SqlQuery | RoboEnotasNFE.cs:619+621, ComexProdutosController.cs:95 | Especificar colunas após corrigir injection |
| P2-04 | Multi-tenant hardcoded no controller | UserIdentityController.cs:SetTenants() | Mover para appSettings.local.config |


---

### P3 — Baixo (backlog trimestral)
| ID | Achado | Ação |
|----|--------|------|
| P3-01 | CSP com unsafe-inline e unsafe-eval | Pre-requisito: mover scripts inline para .js externos |
| P3-02 | alert() nativo remanescente | Areas/gc/Views/MovimentosCompras/CreateCotacaoPedidoCompra.cshtml — substituir por LibMessageError |
| P3-03 | Mistura EF/ADO.NET sem separação de camadas | Documentar como dívida técnica; não migrar agora |
| P3-04 | GlobalFilters só registra HandleErrorAttribute | CustomAuthorize não é global; documentar obrigatoriedade por controller |

---

## 3. ARQUIVOS MAIS CRÍTICOS

1. `Lib/LibDB.cs` — raiz do sql_filtro; efeito multiplicador sobre 10+ controllers
2. `Robos/ENotas/RoboEnotasNFE.cs` — SQL injection em robô autônomo de emissão NF-e
3. `Controllers/UserIdentityController.cs` — Open Redirect + credencial exposta em Old_Web.config
4. `Areas/gc/Controllers/ComexProdutosController.cs` — SQL injection triplo (SELECT + COUNT + paginação)
5. `Areas/g/Controllers/FinanceiroFaturamentosController.cs` — 5 campos concatenados em SQL financeiro
6. `Areas/g/Controllers/UsuariosController.cs` — Roles="*" no nível de classe (gestão de usuários)
7. `Areas/g/Controllers/AssistentesController.cs` — sql_filtro em modo advanced (SQL livre completo)
8. `Areas/g/Views/Atendimentos/Edit.cshtml` — Html.Raw em campos de texto livre do usuário
9. `Old_Web.config` — credencial JobServer:Key exposta (deletar)

---

## 4. PLANO DE FASES SUBSEQUENTES

### Fase 2 — SQL Injection (1-2 semanas)
1. `LibDB.setFilterByUser`: implementar `ValidateSqlFiltro()` com allowlist de tokens + blocklist de tokens perigosos
2. `RoboEnotasNFE.cs:619-621`: substituir SqlQuery concatenado por `.Where()` LINQ
3. `CidadesController.cs`, `FinanceiroFaturamentosController.cs`, `ContratosAviacaoController.cs`: substituir listaCampos[] por SqlParameter tipado
4. Auditar todos os demais controllers com sql_filtro

### Fase 3 — Auth, XSS e CSRF (2-3 semanas)
2. `UsuariosController.cs` e `FiltrosController.cs`: substituir Roles="*" por roles específicas
3. Auditoria de `Html.Raw(Model.*)` em views de atendimentos e boleto (8 casos de alto risco)
4. CSRF Fase 3A: endpoints financeiros (adicionar token em form + action)

### Fase 4 — Upload e Headers (1 semana)
1. Limite de tamanho em ServiceUploadFileGed
2. Confirmar validação de upload nos demais controllers (NfeController, ComexImportacoesController)
3. Documentar CSP atual e criar backlog para endurecimento

### Fase 5 — Logs, Configuração e Ambiente (1 semana)
1. Mover tenants hardcoded para appSettings.local.config
2. Confirmar que Web.Release.config é aplicado corretamente no publish
3. Remover console.log em produção
4. Deletar `Old_Web.config` e rotacionar chave JobServer se reaproveitada

### Fase 6 — Refatoração Estrutural Gradual (backlog contínuo)
1. Extrair lógica de negócio dos controllers maiores para Services (padrão já existe em gc/Services)
2. Criar `FiltroTyped<T>` para eliminar sql_filtro como SQL literal
3. Mover scripts inline para .js externos (pré-requisito para CSP sem unsafe-inline)

---

## 5. PADRÕES RECOMENDADOS (manter daqui para frente)

### Controllers — Acesso a Dados
```csharp
// CORRETO: LINQ (preferencial para CRUD simples)
var results = db.g_cidades.Where(c => c.id_cidade == id).ToList();

// CORRETO: SqlQuery com parâmetros (quando LINQ não atender)
var results = db.g_cidades.SqlQuery(
    "SELECT * FROM g_cidades WHERE id_cidade = @id AND nome LIKE @nome",
    new SqlParameter("@id", id),
    new SqlParameter("@nome", "%" + nome + "%")).ToList();

// PROIBIDO
SentencaSQL += " and c.id_cidade = " + listaCampos[0];  // SQL Injection
```

### Views — Html.Raw
```razor
@* CORRETO: texto puro — Razor encoda automaticamente *@
@Model.descricao

@* CORRETO: HTML intencional sanitizado *@
@Html.Raw(HtmlSanitizer.Sanitize(Model.descricaoHtml))

@* PROIBIDO: campo de texto livre sem sanitização *@
@Html.Raw(Model.solicitacao)
```

### Autorização
```csharp
// CORRETO
[CustomAuthorize(Roles = "SuperAdmin,Admin,g_Usuarios_Default")]

// PROIBIDO em controllers sensíveis
[CustomAuthorize(Roles = "*")]
```

---

## 6. O QUE NÃO FAZER

- **Não migrar para .NET Core** — fora de escopo desta auditoria
- **Não trocar EF 6 Database-First por Code-First** — 170+ entidades; regressão garantida
- **Não reescrever sql_filtro de uma vez** — 10+ controllers; corrigir incrementalmente
- **Não adicionar [ValidateAntiForgeryToken] sem adicionar envio do token no JS** — quebrará forms existentes
- **Não remover Html.Raw de campos Rich Text sem implementar sanitização** — HTML escapado visível ao usuário
- **Não alterar Web.config de produção diretamente** — usar pipeline Publish com Web.Release.config
- **Não alterar regras de negócio sem validação humana** — especialmente em filtros financeiros

---

## 7. DÚVIDAS QUE EXIGEM VALIDAÇÃO HUMANA

1. **sql_filtro em modo `advanced`:** Os controllers AssistentesController e NfeController executam o SQL completo do filtro sem nenhuma restrição de tabela. É necessário confirmar se usuários externos (portal CRM) têm acesso a esses filtros, pois o risco seria crítico.

2. **UrlSair no CachePersister:** Confirmar se `userIdentity.UrlSair` pode ser populado via input externo (formulário, query string) ou se é sempre atribuído internamente pelo sistema. Isso determina a exploitabilidade real do open redirect.

3. **Campos Html.Raw nos atendimentos:** Confirmar se `Model.solicitacao` e `Model.descricao` em `Atendimentos/Edit.cshtml` são campos de texto puro ou rich text (editor WYSIWYG). Isso determina se a correção é simples (remover Html.Raw) ou exige sanitizador.

4. **Roles="*" em UsuariosController:** Confirmar se usuários do portal CRM (id_perfil = -900) podem navegar para a área `g/Usuarios`. Se sim, é P0. Se a rota é protegida por área, é P1.

5. **Chave JobServer em Old_Web.config:** Confirmar se a chave `JOBSERVER_KEY_3F9K7P` ainda está em uso ou foi substituída. Se ainda ativa, precisa ser rotacionada antes de deletar o arquivo.

---

## VERIFICAÇÃO (sem código alterado)

Esta fase produz apenas diagnóstico. Verificação se dá por:
- Revisão humana deste documento
- Confirmação das dúvidas listadas na seção 7
- Aprovação explícita antes de iniciar Fase 2

**Próximo passo recomendado:** Responder as 5 dúvidas da seção 7, depois iniciar Fase 2 com correção em `LibDB.setFilterByUser` (efeito multiplicador) e `Old_Web.config` (deleção imediata).