<!-- GERADO AUTOMATICAMENTE por .claude/scripts/sync_changelog_recent.py -->
<!-- Fonte: .cursor/CHANGELOG-DEV.md | Últimas 5 entradas -->

# CHANGELOG-DEV — Entradas Recentes

> Arquivo gerado automaticamente. Para o histórico completo, consulte `.cursor/CHANGELOG-DEV.md`.

---

## HISTÓRICO DE INTERVENÇÕES (últimas 5)

### [2026-05-14] — Fase 18: DataTables g e gc — audit e correção de controllers/views faltantes
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/gc/Controllers/CfopController.cs`
- `Areas/gc/Controllers/CfopOperacoesController.cs`
- `Areas/gc/Controllers/CfopParametrosController.cs`
- `Areas/gc/Controllers/EstoqueControleController.cs`
- `Areas/gc/Controllers/EstoqueLotesController.cs`
- `Areas/gc/Controllers/FinanceiroParametroDifalController.cs`
- `Areas/gc/Controllers/ComexFinanceiroController.cs`
- `Areas/gc/Views/EstoqueControle/Index.cshtml`

**Problema / Demanda:**
Audit das áreas g e gc revelou 7 controllers gc que não tinham o padrão DataTables completo (Fases 8–16 não os cobriram): 6 totalmente legados (sem param null, sem try/catch, sem errorMessage/stackTrace) e 1 com inline pattern mas sem null guard.

**O que foi feito:**
- `CfopController`, `CfopOperacoesController`, `CfopParametrosController`: param null guard + try/catch + `JsonDataTableException` privado + `errorMessage`/`stackTrace`/`yesFilterOnOff` no JSON de sucesso.
- `EstoqueControleController`: mesmo padrão + proteção NRE com `?.` nas chamadas `Find(...).descricao` (produto/status podem estar ausentes na lista).
- `EstoqueLotesController`: mesmo padrão + `yesFilterOnOff` computado pelos 4 filtros customizados (`yesCustomField01–04`).
- `FinanceiroParametroDifalController`: param null guard + try/catch + `JsonDataTableException` + `errorMessage`/`stackTrace` no sucesso (yesFilterOnOff já existia).
- `ComexFinanceiroController.GetDadosPagamentos`: apenas param null guard (inline pattern com errorMessage/stackTrace/severity já estava correto).
- `EstoqueControle/Index.cshtml`: adicionado `xhr.dt` → `GdiDtNotifyJsonErrorMessage` no `dtGcProdutosControle` (view estava sem, único gap de view nesta fase).

**Decisões técnicas relevantes:**
- `EstoqueLotes`: yesFilterOnOff calculado por `filtroCodigoLote || filtroSerialLote || filtroProduto || filtroImportacao` (a view não usa `btnFiltro`, mas o contrato fica consistente).
- Areas g: todas conformes após análise (ClientesController.GetDados usa método longo com catch no fim — 260 linhas; NfeController, AtendimentosController, FinanceiroController confirmados).

**O que foi evitado e por quê:**
- Não alteradas views Cfop/Index, CfopOperacoes/Index, CfopParametros/Index, EstoqueLotes/Index, FinanceiroParametroDifal/Index, ComexFinanceiro/Index — já tinham `xhr.dt` correto.
- Não alterado EstoqueControle/CreateEdit — `otableAfericoes` usa data source diferente de `GetDados`.
- Não alterado `GedSGQController` (qa) — já coberto como "baixa prioridade" na Fase 17.

**Impactos conhecidos:**
- `EstoqueControleController.GetDados`: o `?.` no `Find()` retorna `""` em vez de lançar NRE quando produto/status não está na lista — comportamento mais seguro em produção.
- Migração DataTables agora completa em todas as áreas: g, gc, crm, qa, a.

**Atenção para próximas intervenções:**
- `GetDadosPops` (qa/GedSGQController) — código morto confirmado; candidato a remoção.
- GedSGQController JSONs de sucesso sem `errorMessage`/`stackTrace` — inócuo funcionalmente, cosmético.

---

---

### [2026-05-14] — Fase 17: DataTables área `a` — AuditController + ParametrosController + 5 views
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/a/Controllers/AuditController.cs`
- `Areas/a/Controllers/ParametrosController.cs`
- `Areas/g/Views/Clientes/CreateEdit.cshtml`
- `Areas/g/Views/Produtos/CreateEdit.cshtml`
- `Areas/gc/Views/ComexImportacoes/CreateEdit.cshtml`
- `Areas/gc/Views/Movimentos/FormPedidoCreate.cshtml`
- `Areas/a/Views/Parametros/Index.cshtml`

**Problema / Demanda:**
Área `a` era a única com controllers DataTables sem try/catch, sem JsonDataTableException e sem errorMessage/stackTrace. `GetAuditTrail` é consumido por 4 views em áreas g e gc já na fase completa, mas sem xhr.dt — erro de servidor seria silencioso.

**O que foi feito:**
- `AuditController.GetAuditTrail`: param null guard + envolvimento em try/catch + JSON sucesso com `errorMessage: ""` / `stackTrace: ""` + método privado `JsonDataTableException`.
- `ParametrosController.GetDadosSistemas`: mesmo padrão + `yesFilterOnOff: "0"` no sucesso (ausente antes).
- 5 views: adicionado `.on('xhr.dt', function (e, settings, json, xhr) { GdiDtNotifyJsonErrorMessage(json); })` nas inicializações `otableGClientesAudit`, `otableGProdutosAudit`, `otableGcComexAudit`, `otableGcMovimentosAudit`, `otableAParametros`.

**Decisões técnicas relevantes:**
- `JsonDataTableException` em `AuditController` sem parâmetro `yesFilterOnOff` (método não tem filtro ativo — fixo em `"0"`).
- `try/catch` envolve todo o corpo do método; os `try/catch` internos para NRE de `NomeUsuario` foram preservados intactos.

**O que foi evitado e por quê:**
- Não alterado `GedSGQController` (qa) — já tem try/catch e JsonDataTableException corretos; ausência de errorMessage/stackTrace no sucesso é inócua funcionalmente.
- Não alterado `GetDadosPops` (qa) — método sem consumidor nas views (código morto); remover seria fora do escopo.
- Não alterado `LmsEvidenceController` — contrato `{ok, error}` próprio, não DataTables.

**Impactos conhecidos:**
- `GetAuditTrail` é usado em Clientes, Produtos, ComexImportacoes e FormPedidoCreate: comportamento de sucesso inalterado; erros agora chegam ao `GdiDtNotifyJsonErrorMessage` via `xhr.dt`.
- Área `crm`: sem GetDados DataTables — não é alvo de fase.

**Atenção para próximas intervenções:**
- `GetDadosPops` em `GedSGQController` é código morto — nenhuma view o chama. Candidato à remoção futura com validação.
- JSONs de sucesso em `GedSGQController` (qa) sem `errorMessage`/`stackTrace` — cosmético, baixa prioridade.
- Fases DataTables agora completas em todas as áreas mapeadas (g, gc, crm, qa, a).

---

---

### [2026-05-14] — Remoção de `GerarNFPImportacaoByMovimentoNF_OLD` (código morto)
**Tipo:** Refatoração
**Arquivos tocados:**
- `Robos/ENotas/RoboEnotasNFE.cs`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Método legado não referenciado em nenhum ponto do repositório.

**O que foi feito:**
- Removida a região e o método **`GerarNFPImportacaoByMovimentoNF_OLD`** de **`RoboEnotasNFE`**. O fluxo ativo continua a ser **`GerarNFPImportacaoByMovimentoNF`** (ex.: `MovimentosEntradasController`).

**O que foi evitado e por quê:**
- Sem alteração ao método V2 nem ao controller.

**Impactos conhecidos:**
- Nenhum consumidor no código; comportamento de produção inalterado.

**Atenção para próximas intervenções:**
- Se existir referência externa ao `_OLD` (scripts, outro repo), ajustar para o V2.

---

---

### [2026-05-14] — eNotas: falha de transmissão JSON → `gc_movimentos_nf.id_nfe_status = 14` + `g_nfe_logs`
**Tipo:** Correção
**Arquivos tocados:**
- `Robos/ENotas/RoboEnotasNFE.cs`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Em falhas de rede/IO ao enviar JSON à API eNotas (ex.: servidor remoto recusou conexão), o movimento NF e o log não refletiam o status de erro nos dados (**14**).

**O que foi feito:**
- Método **`PersistirFalhaTransmissaoJsonEnotasMovimentoNf`**: atualiza **`gc_movimentos_nf.id_nfe_status = 14`**, insere **`g_nfe_logs`** com mensagem truncada (via `LibExceptions` / `WebException`), campos obrigatórios do log preenchidos a partir do NF; erros secundários ao persistir são ignorados.
- **`GerarNFPVendaByMovimentoNF`** e **`GerarNFServicoByMovimentoNF`**: no `catch` antes de relançar, chamada ao helper quando **`id_movimento_nf > 0`** (já gravado antes do POST).
- **`GerarNFPImportacaoByMovimentoNF`** (V2): **`gc_movimentos_nf`** passa a ser gravado **antes** do HTTP (mesmos campos que no sucesso), para existir `id_movimento_nf` em falha de transmissão; `catch` chama o helper; ramo de sucesso deixa de duplicar `Add`/`SaveChanges` da NF.

**Decisões técnicas relevantes:**
- **`id_nfe_status`** existe em **`gc_movimentos_nf`**, não na entidade **`g_nfe_logs`**; o log descreve a falha de transmissão.

**O que foi evitado e por quê:**
- Não alterar outros `catch` do robô (consulta status, etc.) fora do escopo de emissão POST produto/serviço.

**Impactos conhecidos:**
- Importação: registo **`gc_movimentos_nf`** criado antes da resposta OK da eNotas (antes só após sucesso); em sucesso o movimento pedido continua a ser atualizado como antes.

**Atenção para próximas intervenções:**
- Se a API devolver HTTP não OK mas com corpo (ex.: validação), o fluxo atual continua a lançar **`Exception(responseData)`** sem passar pelo helper de transmissão (conexão estabelecida).

---
**Tipo:** Correção
**Arquivos tocados:**
- `Robos/ENotas/RoboEnotasNFE.cs`

**Problema / Demanda:**
eNotas negou NFS-e (**GW3001**): série do RPS deve conter **apenas números** e estar entre **1 e 49.999**. O envio usava **`serieRps` = `"RPS"`** (letras).

**O que foi feito:**
- `NormalizarSerieRpsEnotas`: valida dígitos e intervalo; inválido ou vazio → **`"1"`**.
- `GerarNFServicoByMovimentoNF`: `serieRps` passa a usar **`g_nfe_gateway.key3`** da filial do gateway (quando preenchido com número válido); caso contrário **`"1"`**.

**O que foi evitado e por quê:**
- Sem alteração de schema SQL: `key3` já existia na entidade e não era usada no código do robô.

**Impactos conhecidos:**
- Filiais que precisem de série específica (ex.: 2, 10) devem gravar esse valor em **`g_nfe_gateway.key3`** para o registo do gateway da filial usado na NFS-e.

**Atenção para próximas intervenções:**
- Se `key3` for reutilizado noutro contexto no futuro, extrair coluna dedicada ou documentar convenção no `CLAUDE.md`.

---

---

### [2026-05-14] — `AtualizarStatusNFPbyMovimentoNF`: consulta NFS-e em `/v1/nfes/porIdExterno` (evita GEN002)
**Tipo:** Correção
**Arquivos tocados:**
- `Robos/ENotas/RoboEnotasNFE.cs`
- `Robos/ENotas/Nfe/NFSe.cs`

**Problema / Demanda:**
Ao atualizar status de **NF de serviços** (`GetNotaFiscalPedido` → `AtualizarStatusNFPbyMovimentoNF`), a API eNotas devolvia **GEN002** (erro genérico): o código fazia **GET** `.../v2/empresas/{id}/nf-e/{nf_identificador}` pensando em **NF-e produto** (identificador GUID da nota), mas em NFS-e o `nf_identificador` é o **`idExterno`** enviado na emissão, não a chave da rota `nf-e`.

**O que foi feito:**
- Deteção de payload NFS-e: `xml_erp` JSON com **`servico`** na raiz e sem **`itens`** (`IsJsonEnvioNfseServico`).
- Nesse caso: **GET** `https://api.enotasgw.com.br/v1/empresas/{Key2}/nfes/porIdExterno/{Uri.EscapeDataString(nf_identificador)}` (alinhado ao Yes-ERP-Cloud e ao download XML já existente no robô).
- Deserialização com **`DataNFSe`**: PDF via **`linkDownloadPDF`**, demais campos mapeados para as mesmas variáveis usadas na atualização do `gc_movimentos_nf` (fluxo unificado com NF-e produto).
- **`NFSe`**: propriedades opcionais para resposta da consulta (`numero`, `dataCompetenciaRps`, `chaveAcesso`, datas em string).
- **`catch (WebException)`** neste método: uso de **`LibExceptions.getWebException`** (evita NRE se `Response` for nulo).

**O que foi evitado e por quê:**
- Não alterar `AtualizarStatusG_nfePorId` (usa `nfe_key` / NF-e avulsa) nem cancelamento.

**Impactos conhecidos:**
- Notas de serviço emitidas pelo `GerarNFServicoByMovimentoNF` (com `xml_erp` típico) passam a consultar o endpoint correto; NF-e produto mantém **`/v2/.../nf-e/...`**.

**Atenção para próximas intervenções:**
- Registos antigos sem `xml_erp` ou com JSON atípico continuam no fluxo **NF-e** (v2); se existirem, avaliar outro critério (ex.: CFOP operação serviço).

---

---
