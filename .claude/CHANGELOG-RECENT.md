<!-- GERADO AUTOMATICAMENTE por .claude/scripts/sync_changelog_recent.py -->
<!-- Fonte: CHANGELOG-DEV.md (raiz) | Últimas 5 entradas -->

# CHANGELOG-DEV — Entradas Recentes

> Gerado automaticamente. Histórico completo: `CHANGELOG-DEV.md` e `docs/dev-history/`.

---

## Últimas alterações (5)

### 2026-06-12 — WhatsApp: novo robô de Inteligência Comercial por vendedor
- Novo serviço `Robos/WhatsApp/RoboWhatsAppInteligenciaComercial.cs` (mesma arquitetura/Z-API do `RoboWhatsAppGerencial`), acionável via JobServer `JobName=EnviarRelatorioInteligenciaComercialWhatsApp` (novo `case` em `JobServerController`). Adicionado `<Compile>` no `.csproj`.
- Destinatários: `g_vendedores.report_inteligencia_comercial = true`; envio individual ao `telefone_1`. `Parameters` opcional = telefone de teste (DDI 55) que sobrescreve o destino (homologação).
- Métricas por vendedor: comissões confirmadas no mês (lógica `ModalRelatorioVendedoresComissoes`/Projetado, status 1/4 por `data_vencimento`), total de parcelas atrasadas (`ModalRelatorioVendedoresAtrasados`: status 3, venc. ≤ hoje-3, soma `valor_total`), pedidos fechados no mês (`ModalRelatorioVendedoresPedidos`: NF autorizada + `COALESCE(datahora_nf, MIN(nf_data_autorizacao))`), e cotações em aberto (`id_movimento_tipo=3`, `status=1`, não cancelada/devolvida) por faixa de `datahora_alteracao` 0-30/30-60/60-90 dias. Comissões/atrasados/pedidos por `comissao1_vendedor`; cotações por `id_vendedor`.
- Agendamento (espelha o gerencial): `Scripts/2026_06_12_gdi_relatorio_inteligencia_comercial_whatsapp.ps1` (disparo) + `Scripts/2026_06_12_gdi_register_task_relatorio_inteligencia_comercial.ps1` (registro Task Scheduler, seg-sex 08h00).

---

### 2026-06-12 — gc/FinanceiroLancamentos: filtro de data do usuário passa a usar data_pagamento
- `GetDadosLancamentos` (listagem DataTables): o filtro de período informado pelo usuário (`data1`/`data2` de `yesCustomField09`/`yesCustomField10`) passou de `data_vencimento` para `data_pagamento` (linha do `Where`). Único filtro de data do usuário sobre `data_vencimento` no controller. Ordenações (`COALESCE(data_pagamento, data_vencimento)`), projeções/exibição e `AtualizarSaldoContaCaixa` (já em `data_pagamento`) preservados; `GetDadosLancamentosByMovimento` filtra por movimento (sem data).

---

### 2026-06-12 — gc/FinanceiroLancamentos: fallback de data_pagamento em parcelas sem vencimento
- **Causa:** `CreateFinanceiroMovimento` define `DataVencimentoParcela = data_vencimento_N.GetValueOrDefault()`; quando o vencimento da parcela vem nulo, retorna `DateTime.MinValue` (0001-01-01), gravado em `data_vencimento`, `data_vencimento_original` e `data_pagamento` (campo `DateTime` não-nullable → nunca null, mas aceita a sentinela). Evidência: tratamentos `Replace("01/01/01")`/`COALESCE` no próprio controller.
- **Correção cirúrgica:** guard único após a seleção da parcela — se `DataVencimentoParcela < 1900-01-01`, usa `DataHoraAtual`. Evita persistir 0001-01-01 (e risco de SqlDateTime overflow em coluna `datetime`). Sem alterar POCO/schema.

---

### 2026-06-12 — Menu lateral some na sessão: remontagem ao renderizar navbar
- **Causa raiz:** `contextoModel_{TokenId}` (que guarda `allNavbarItemMenu`) tem sliding de 15 min independente do `userIdentity_{TokenId}`. Em telas só com Ajax/lookups, o `userIdentity` é renovado mas o `contextoModel` expira; o próximo lookup chama `LookupQueryServiceCache.EnsureContextoModel()`, que recria um `ContextoModel` **vazio**. Na navegação full-page seguinte a sidebar renderiza sem `<li>` (menu some), com o usuário ainda autenticado.
- **Correção cirúrgica:** `NavbarController.Index` — quando `allNavbarItemMenu` está vazio/null e há `userIdentity` válido, remonta o menu pelo método oficial do login (`new Contexto().getNavbarItemsMenu()`) e re-persiste em `CachePersister.contextoModel`. Sem alterar `EnsureContextoModel`, `CachePersister`, `Web.config` nem a view `_Navbar`.

---

### 2026-06-11 — datahora_nf vazio: relatórios zerados + correção gravação e fallback
- **Causa:** coluna `gc_movimentos.datahora_nf` existia no schema mas não era preenchida na autorização NFe (`RoboEnotasNFE`); filtros `BETWEEN datahora_nf` excluíam todos os pedidos (NULL).
- **Correção:** `RoboEnotasNFE` grava `datahora_nf`/`id_usuario_nf` na 1ª NF autorizada; relatórios (`GerencialController`, `RelatoriosComerciaisController`, `RoboWhatsAppGerencial`) usam `COALESCE(datahora_nf, MIN(nf_data_autorizacao))` para histórico.
- **Backfill:** `Scripts/2026_06_11_gdi_backfill_gc_movimentos_datahora_nf.sql` (executar uma vez no SQL Server).

---
