# CHANGELOG-DEV.md

> Changelog **operacional** (~200 linhas).  
> **Histórico integral:** `docs/dev-history/CHANGELOG-DEV-HISTORICO-INICIAL.md` (187 entradas)  
> **Contexto fixo:** `AI-CONTEXT.md` | **Pendências:** `BACKLOG-DEV.md` | **Índice temas:** `.cursor/context/2026_06_05_indice-memoria-ia.md`

**Última atualização:** 2026-06-05 — consolidação memória IA + lote prefixos legado

---

## Estado atual do projeto

Monólito ASP.NET MVC **4.7.2** (COMEX, comercial, estoque, financeiro, qualidade). Portal cliente em `crm` + `UserIdentity`. Modernização 2026: **`GdiMvcJsonResults`** + guards modal GET + handlers Ajax/DT homogéneos; lookups via **`ILookupQueryService`**; módulos legados removidos (Filtros, PortalVendedor, Assistentes, FinanceiroFaturamentos/Lancamentos em `g`, etc.).

**Build** Release OK nas intervenções recentes. **VersionERP:** incrementar após `start.js` / `start.css` / `gdi-select2.js`.

---

## Decisões técnicas ativas

- **.NET Framework 4.7.2** permanente em IIS — **sem** migração para ASP.NET Core / .NET 6+ (decisão 2026-05-25). Bump opcional **4.8.1** (mesmo monólito) só se a equipa decidir — `2026_05_20_migracao-472-481.md`.
- Stack UI fixa (Bootstrap 5, AdminLTE 4, DataTables bs5, SweetAlert2, Tempus Dominus).
- **Dois tipos de tabela:** DataTables (Ajax) vs MVC (`@for`) — não misturar CSS/contratos.
- **Arquitetura centralizada obrigatória:** `2026_06_05_arquitetura-centralizada-erp-gdi.md` + `Lib/GdiMvcJsonResults.cs`.
- **Lookups:** Index = combo local; CreateEdit = `ILookupQueryService` + `*.Lookups.cs`.
- Portal externo = `crm`; `g/PortalCliente` = legado interno.
- Agente: sem `git push` / publish remoto; português BR; registo só linha compacta aqui + `BACKLOG-DEV.md`.

---

## Últimas alterações relevantes

### 2026-06-11 — datahora_nf vazio: relatórios zerados + correção gravação e fallback
- **Causa:** coluna `gc_movimentos.datahora_nf` existia no schema mas não era preenchida na autorização NFe (`RoboEnotasNFE`); filtros `BETWEEN datahora_nf` excluíam todos os pedidos (NULL).
- **Correção:** `RoboEnotasNFE` grava `datahora_nf`/`id_usuario_nf` na 1ª NF autorizada; relatórios (`GerencialController`, `RelatoriosComerciaisController`, `RoboWhatsAppGerencial`) usam `COALESCE(datahora_nf, MIN(nf_data_autorizacao))` para histórico.
- **Backfill:** `Scripts/2026_06_11_gdi_backfill_gc_movimentos_datahora_nf.sql` (executar uma vez no SQL Server).

### 2026-06-11 — RoboWhatsAppGerencial: ranking vendedores por volume (Hoje/Mês)
- `AppendSecaoPedidos`: listagem de vendedores em «Pedidos Hoje» e «Pedidos Mês» ordenada por `valor_total_bruto` decrescente (ranking isolado por seção); desempate alfabético. Removido `IdsVendedoresOrdemAlfabetica`.

### 2026-06-11 — Filtro período pedidos fechados (NF autorizada): datahora_nf
- `GerencialController`, `RelatoriosComerciaisController` (`AjaxModalRelatorioVendedoresPedidos`) e `RoboWhatsAppGerencial`: filtros de período em pedidos com NF autorizada passam a usar `gc_movimentos.datahora_nf` (primeira NF autorizada) em vez de `datahora_aprovacao`. Painel de pedidos em processamento (posições 1–5) mantém `datahora_aprovacao`.

### 2026-06-10 — Limpeza g_assistentes (tabela removida do banco)
- EDMX atualizado a partir do schema removeu a entidade `g_assistentes` (POCO `Db/g_assistentes.cs` e `DbSet` já não existem). Limpeza das referências órfãs restantes: removido `<Compile>` de `Db\g_assistentes.cs` e `Db\Metadata\g_assistentesMetadata.cs` do `.csproj`; apagado `Db/Metadata/g_assistentesMetadata.cs`; removido token `g_assistentes` de `Db/InserirMetadata.exe.config`. Sem Controller/View/Modal (já não existiam). `ddl-version.txt` intocado (refere `id_vendedor_assistente` de `gc_movimentos`, não relacionado).

### 2026-06-10 — gc/Movimentos: ícone do status Cancelado (3) → fa-circle-xmark
- Substituído `fa-thumbs-down` por `fa-circle-xmark` no status 3 (Cancelado) em `MovimentosController` (iconeStatus `fa-solid` em GetDadosPedidos; iconeTipo `fa-regular` na listagem por status), alinhando ao padrão já usado em AtendimentosController/Financeiro. Distinção visual: Cancelado = X em círculo, Devolvido = fa-reply-all.

### 2026-06-10 — Movimento status 4 (Devolvido): tratamento de exibição (espelho do status 3)
- Novo `id_movimento_status = 4` ('Devolvido') em `gc_movimentos_status` (todo `movimento_devolvido = true` ⇒ status 4). Adicionado branch de exibição para status 4 onde já se tratava status 3 (Cancelado): `MovimentosController.GetDadosPedidos` (iconeStatus "Devolvido") e listagem por status (iconeTipo "Pedido(Devolvido)"); `RelatoriosComerciaisController` (sufixo " (Devolvido)"); `ReportEmailPedido` (texto "Devolvido", 2 ocorrências). Filtros por status revisados: PainelPedidos usa `== 2` (devolvido já excluído); GetDadosPedidos lista todos os status.

### 2026-06-10 — gc/Fretes/GetDados: exclui movimentos devolvidos (espelho de cancelado)
- `FretesController.GetDados`: filtro base passa a excluir `movimento_devolvido` (`.Where(m => !m.movimento_devolvido)`), espelhando o `!m.movimento_cancelado` já existente. Levantamento projeto-wide de `movimento_cancelado`: demais usos são definição de campo (POCO/EDMX/DDL), setter de cancelamento ou já tratados no MovimentosController.

### 2026-06-10 — gc/Movimentos/AjaxSavePosVenda: bloqueio de Pós-Venda em pedido devolvido
- `MovimentosController.AjaxSavePosVenda`: espelha a validação de `movimento_cancelado` para `movimento_devolvido` — bloqueia registro de Pós-Venda em pedido devolvido ("Não é possível registrar Pós-Venda em pedido devolvido."). Demais usos de `movimento_cancelado` no controller já tratados (projeção/exibição em GetDadosPedidos, filtro do PainelPedidos) ou são o setter do próprio cancelamento (não validação).

### 2026-06-10 — gc/Movimentos/GetDadosPedidos: tratamento visual de movimento_devolvido
- `MovimentosController.GetDadosPedidos`: espelha o tratamento de `movimento_cancelado` para `movimento_devolvido` — campo adicionado à projeção e novo `else if` com cor (#cc6600) e rótulo "Orçamento/Pedido Devolvido", "OS Devolvida", "Transferência Devolvida" (precedência: cancelado > devolvido).

### 2026-06-10 — gc/Movimentos/PainelPedidos: oculta movimentos cancelados/devolvidos
- `MovimentosController.GetDadosPainelPedidos`: filtro base passa a excluir `movimento_cancelado == true` e `movimento_devolvido == true` (antes do `Count`/`Skip`).

### 2026-06-10 — gc/Movimentos/PainelPedidos: filtro por atividade pendente da operação fiscal
- `MovimentosController.GetDadosPainelPedidos`: novo `.Where` estrutural (subconsulta `EXISTS` em `gc_cfop_operacoes`) antes do `Count`/`Skip`. Movimento só aparece se, a partir da posição (1–5), houver atividade pendente habilitada na operação (`has_separacao`/`has_financeiro`/`has_nfe`/`has_expedicao`/`has_entrega`). Movimentos sem atividade pendente (ou sem operação cadastrada) deixam de ser exibidos.
- Coluna "Próxima atividade" **dinâmica** (posições 1–5): calcula a primeira etapa habilitada na operação (flags `has_*`), descartando atividades não parametrizadas. Flags `has_*` adicionados à projeção; texto/ícone derivados em runtime (não altera o filtro de seleção). [G-UX-02]

### 2026-06-08 — Modais scrollable + DataTables: scroll horizontal residual (Lote A + start.css)
- `start.css` (`VersionERP` 2026.51.40): `.modal-dialog-scrollable .modal-body { overflow-x: clip; min-width: 0 }`; `.scroll-modal-horizontal` contido.
- **Lote A (≤10 col / GED/logs):** `ModalFinanceiroViewAnexos`, `ModalHistoricoMovimento`, `ModalViewCartaCorrecao`, `ModalImportacoesLogs`, `ModalConsultaPedidos`, `ModalInvoicesItensEspelhoDigital`, `EstoqueControle/ModalCreateEdit` (medições), `EstoqueLotes` (reforço CSS), `Produtos/ModalCreateEditProduto` (audit), `FinanceiroLancamentos/ModalCreateEditLancamento` (aba GED), `ModalViewFinanceiroMovimentos`, `ModalPedidoSeparacao`, `ModalPedidoExpedicao`, `ModalPedidoEntrega`; `ModalViewNotasFiscais` (14 col — host + scroll interno).
- Script inventário: `Scripts/2026_06_08_gdi_audit_modal_dt_scroll.py`.

### 2026-06-08 — gc/Movimentos/ModalPedidoViewAnexos: scroll horizontal residual (padrão EstoqueLotes)
- `ModalPedidoViewAnexos.cshtml`: CSS `#FormModalPedidoViewAnexos .modal-body { overflow-x: hidden }` + `#divDocsPedidoAnexos { min-width: 0 }`; remove wrappers extras; `.row` → `d-flex`; `modal-body p-1`; `drawCallback` zera `scrollLeft`.

### 2026-06-08 — gc/Movimentos/ModalPedidoViewAnexos: scroll horizontal desnecessário (gdi-dt-scroll-host)
- `ModalPedidoViewAnexos.cshtml`: padrão `EstoqueLotes/ModalCreateEdit` — `gdi-dt-scroll-host min-w-0` no host da tabela (8 col.); remove `scroll-body-horizontal` + `overflow-x` inline; botão upload fora do host; `drawCallback` + `columns.adjust()`.

### 2026-06-08 — gc/Movimentos/ModalPedidoViewAnexos: HTTP 500 — view + controller (arquitetura modal GET)
- `ModalPedidoViewAnexos.cshtml`: estrutura HTML alinhada a `ModalFinanceiroViewAnexos` (`_AlertMsg` + grelha só sem `MsgBloqueio`); corrige `@if`/`</div>` mal aninhados da modernização 2026-06-05; upload via `#id_movimento`.
- `MovimentosController.ModalPedidoViewAnexos`: guard `db`, `PedidoNaoEncontradoMensagem`, try/catch + `LibLogger`; `TryGetMovimentoModal` null-safe se `db` ausente.
- `_Modal.cshtml`: `VersionERP` null-safe (`ControlVersion` fallback).

### 2026-06-08 — gc/Movimentos/IndexPedido: anexos inline — confiar DT do host + erro HTTP detalhado
- `start.js` (`VersionERP` 2026.51.38): `gdiHostPageScriptFlags` / `gdiHasDataTablesRelaxed`; não recarrega DataTables/Select2 quando o host já declara o bundle (`data-gdi-page-scripts`); timeout do defer propaga erro; `GdiMainModalLoad` envia `X-Requested-With` e mensagem com HTTP + URL.
- `IndexPedido.cshtml`: botão anexo com `data-id-mov`; clique delegado namespaced; URL com `encodeURIComponent`; evita alerta duplicado (erro fica no `GdiMainModalLoad`).

### 2026-06-08 — gc/Movimentos/IndexPedido: botão inline Anexos (GdiMainModalLoad + DT defer)
- `start.js` (`VersionERP` 2026.51.37): `gdiEnsureScriptFlagsForModal` aguarda DataTables/Select2 já no host (defer) em vez de recarregar bundle; `gdiHasDataTables` reforçado; `gdiMainModalReleaseProcessando` em erros do `GdiMainModalLoad`.
- `IndexPedido.cshtml`: clique delegado `.btnGCIndexPedidoAnexos`; duplo `LibMessageProcessandoHide` no callback; mensagem se load falhar; `GdiMainModalShow`.

### 2026-06-08 — gc/Movimentos: AjaxModalPedidoEntrega — correção CS1513 (chave `}`)
- `MovimentosController.AjaxModalPedidoEntrega`: fecha bloco `if (Sucesso == true)` antes de `db.SaveChanges()` (padrão `AjaxModalPedidoExpedicao`).

### 2026-06-08 — gc/ComexProdutos: FormProcessarProdutosPreNovos — layout coluna PN
- `FormProcessarProdutosPreNovos.cshtml` + `start.css`: coluna PN mais larga (11rem) com quebra de linha; Description/Tradução e Produto dividem o espaço restante igualmente (`colgroup` + `table-layout: fixed`).

### 2026-06-08 — gc/Movimentos: AjaxModalPedidoSeparacao — validação de lotes na confirmação global
- `MovimentosController.AjaxModalPedidoSeparacao`: na confirmação da separação, replica regra do submodal — `qtd > 0` exige lote selecionado; `id_estoque_lote` deve ser ativo e do produto do item.

### 2026-06-08 — gc/Movimentos: AjaxModalPedidoSeparacaoLotes — validação qtd exige lote ativo do produto
- `MovimentosController.AjaxModalPedidoSeparacaoLotes`: bloqueia quantidade sem lote selecionado (`qtd > 0` ⇒ `id_estoque_lote > 0`) e rejeita `id_estoque_lote` inativo ou de outro produto (mesmo critério do GET do submodal).

### 2026-06-08 — gc/Movimentos: AjaxModalPedidoNotaFiscal — validações POST alinhadas ao GET
- `MovimentosController.AjaxModalPedidoNotaFiscal`: replica bloqueios do `ModalPedidoNotaFiscal` (`has_nfe`, aprovação, separação, faturado), null-check destinatário/cliente/UF, mensagem quando gateway NF-e ≠ e-Notas; gravação do movimento só com `IdGateway == 1`.

### 2026-06-08 — Views: LibMessageProcessandoHide explícito em callbacks error Ajax
- Lote em **124** `.cshtml` (`Areas/`, `Views/`): após `LibMessageProcessando`, handlers `error` de `$.ajax` passam a chamar `LibMessageProcessandoHide()` (padrão `ModalPedidoExpedicao`), incluindo modais de pedido/NF, financeiro, estoque, COMEX, relatórios e cadastros `g`.
- **2** handlers vazios (`IndexRecebimentoEstoque`, `IndexRecebimentoImportacao`) ganharam feedback `GdiAjaxNotifyInconsistencias`.
- Script reutilizável: `Scripts/2026_06_08_gdi_patch_ajax_error_processando_hide.py`.

### 2026-06-08 — gc/Movimentos: modal Entrega — LibMessageProcessandoHide no erro Ajax
- `ModalPedidoEntrega.cshtml`: handler `error` de `jsSendForm` chama `LibMessageProcessandoHide()` (overlay não fica preso em falha de rede).

### 2026-06-08 — gc/Movimentos: modal Entrega — data entrega na linha do status e obrigatória ao salvar
- `ModalPedidoEntrega.cshtml`: `datahora_entrega` com Tempus Dominus na mesma linha do switch entregue.
- `MovimentosController.ModalPedidoEntrega`: preenche data atual só quando pedido ainda não entregue.
- `MovimentosController.AjaxModalPedidoEntrega`: validação `qtdInconsistencias` obrigatória para `datahora_entrega` ao confirmar entrega.

### 2026-06-08 — gc/Movimentos: modal Expedição — datas editáveis e obrigatórias ao salvar
- `ModalPedidoExpedicao.cshtml`: `datahora_expedicao` e `datahora_entrega_previsao` com Tempus Dominus (`jsDatepicker`) na mesma linha do switch expedido.
- `MovimentosController.ModalPedidoExpedicao`: preenche data atual (+5 dias previsão entrega) só quando pedido ainda não expedido.
- `MovimentosController.AjaxModalPedidoExpedicao`: validação `qtdInconsistencias` obrigatória para ambas as datas ao confirmar expedição; grava valores do formulário na row (sem fallback silencioso).

### 2026-05-25 — gc/EstoqueControle: coluna Validade só com data (sem hora)
- `EstoqueControleController.GetDados`: `data_validade` formatada `dd/MM/yyyy` (alinhado a `EstoqueLotes`).

### 2026-05-25 — gc/EstoqueLotes: scroll horizontal desnecessário no DataTable Documentos
- `ModalCreateEdit.cshtml`: bloco GED com `gdi-dt-scroll-host min-w-0` (remove `overflow-x: auto` inline); botão «Incluir Anexos» fora do host; `drawCallback` com `columns.adjust()`; `#divDocsEstoqueLote` com `min-width: 0`.

### 2026-05-25 — gc/EstoqueControle: LibMessageProcessando ao abrir modal CreateEdit
- `Index.cshtml`: `LibMessageProcessandoHide` duplo no callback do `#mainModal.load` + erro de load; `drawCallback` da grelha com hide; `ModalCreateEditEstoqueControle`: `jsInitModal` antes dos datepickers + hide no `catch` (contador `waitingDialog`).

### 2026-05-25 — gc/EstoqueLotes: modal CreateEdit em `modal-full`
- `ModalCreateEdit.cshtml`: `modal-xl` → `modal-full modal-dialog-scrollable` (~90% largura, `start.css`); scroll no body do modal.

### 2026-05-25 — gc/EstoqueControle: CRUD modal CreateEdit
- `ModalCreateEditEstoqueControle` + Index (`jsModalCreateEditEstoqueControle`); GET `Create`/`Edit` → `Index`; reutiliza `AjaxSaveRecord`; `DataRowInUseSerialized` na edição; abas Dados/Aferições em `modal-full`.

### 2026-05-25 — CRUD modal: ContasCaixas, PagRecCondicoes, PagRecTipos, gc/Cfop
- Migração padrão Vendedores/UF: `ModalCreateEdit*` + `AjaxCreateEdit*`; GET `Create`/`Edit` → `Index`; Index com `jsModal*` + duplo clique; `CreateEdit.cshtml` legado mantido; ContasCaixas `modal-full` com abas.

### 2026-05-25 — g/Vendedores: modal CreateEdit sem erro quando GDC inativo
- `ModalCreateEditVendedor.cshtml` + `CreateEdit.cshtml`: `jsValidarRegraParametrizada` com guard quando abas Regras Comissão não estão no DOM (módulo GDC / `id_sistema == 3` inativo).

### 2026-05-25 — g/Produtos: modal com abas Parâmetros e Endereço Estoque
- `ModalCreateEditProduto.cshtml`: abas `Parâmetros` (card Parâmetros) e `Endereço Estoque` (cards BH/SP); aba Dados mantém Dados do Produto + FOB; abas só para `is_servico == false`.

### 2026-05-25 — g/ProdutosNcm: modal CreateEdit em `modal-full`
- `ModalCreateEditProdutosNcm.cshtml`: `modal-xl` → `modal-full modal-dialog-scrollable` (padrão maior do ERP em `start.css`, ~90% largura); scroll no diálogo; `overflow-x: visible` no body.

### 2026-05-25 — gc/ComexProdutos: modal CreateEdit — typeahead produto (G-PROD-04)
- `PreencherLookupsProdutoModal` (placeholder + item vinculado) substitui `GetComboGcProdutosServicosTodos` no GET do modal; `ComexProdutosController.LookupAjax.cs` (`GetProdutosLookup`); `ModalCreateEdit.cshtml` com Select2 Ajax (`data-gdi-select2-search`).

### 2026-05-25 — gc/ComexProdutos: modal CreateEdit alinhado ao padrão ERP
- `Index.cshtml`: duplo clique com `td:not(.dt-no-row-select)` + guard `data`; `jsGcComexProdutosCreateEdit` com `LibMessageProcessando`, validação de id e `emptyIfNull`; desativar com spinner; `ModalCreateEdit.cshtml`: redraw via `otableGcComexProdutos`, `LibMessageHideAll`/`LibMessageProcessandoHide` no Ajax.

### 2026-05-25 — Publish: remover `_Layout.cshtml` órfãos de área no `.csproj`
- `GDI-ERP-Plataform.csproj`: removidos `Content Include` de `Areas/gc`, `Areas/g` e `Areas/qa/Views/Shared/_Layout.cshtml` (ficheiros inexistentes); `_ViewStart` de cada área já aponta para `~/Views/Shared/_Layout.cshtml`.

### 2026-05-25 — g/Produtos: modal CreateEdit em `modal-full`
- `ModalCreateEditProduto.cshtml`: `modal-xl` → `modal-full modal-dialog-scrollable` (padrão maior do ERP em `start.css`, ~90% largura); scroll no diálogo; `overflow-x: visible` no body.

### 2026-05-25 — g/Vendedores: CreateEdit em modal Ajax (padrão Filiais)
- `ModalCreateEditVendedor` + `AjaxCreateEditVendedor`; view modal (`modal-xl`, abas Dados/Regras comissão); Index com `mainModal.load` + redraw `dtGVendedores`; validação nome duplicado preservada; GET `Create`/`Edit` → Index; POST legados mantidos; DataTable comissão com variável `otableGVendedoresServicoTipoMens` (evita colisão com Index).

### 2026-05-25 — g/ContratosAviacao: CreateEdit em modal Ajax (padrão Filiais)
- `ModalCreateEditContratoAviacao` + `AjaxCreateEditContratoAviacao`; view modal (`modal-xl`, Select2 cliente, datepicker); Index com `mainModal.load` + redraw `dtGContratos`; validação create (tipo/signatário/identificador) em `AplicarValidacaoContratoAviacao`; GET `Create`/`Edit` → Index; POST legados mantidos; `AjaxGetDadosClientes`, PDF e upload assinado inalterados.

### 2026-05-25 — g/UF: edição em modal Ajax (padrão Filiais)
- `ModalCreateEditUF` + `AjaxCreateEditUF`; view modal (`modal-lg`, parâmetros ICMS MG/SP); Index com `mainModal.load` + redraw `dtGUF`; GET `Create`/`Edit` → Index; POST legados mantidos; `CreateEdit.cshtml` mantido; botão Novo permanece desabilitado (só edição).

### 2026-05-25 — g/Produtos: edição em modal Ajax (padrão Filiais)
- `ModalCreateEditProduto`; view modal (`modal-xl`, abas Dados/Audit); Index com `mainModal.load` + redraw `dtGProdutos`; reutiliza `AjaxEditProduto`; `DataRowInUseSerialized`/`LibAudit` preservados; GET `Edit` → Index; `CreateEdit.cshtml` mantido; modais ficha estoque/desativar/atualizar cadastro inalterados; sem Create (só edição).

### 2026-05-25 — g/ProdutosNcm: CreateEdit em modal Ajax (padrão Filiais)
- `ModalCreateEditProdutosNcm` + `AjaxCreateEditProdutosNcm`; view modal (`modal-xl`); Index com `mainModal.load` + redraw `dtGProdutosNcm`; lookups `PreencherLookupsCreateEdit`; GET `Create`/`Edit` → Index; `ModalAtualizarTabelaIBPT` inalterado.

### 2026-05-25 — g/Cidades: CreateEdit em modal Ajax (padrão Filiais)
- `ModalCreateEditCidade` + `AjaxCreateEditCidade`; view modal; Index com `mainModal.load` + redraw `dtGCidades`; GET `Create`/`Edit` → Index; `ModalCadastrarNovaCidade`/`AjaxCadastrarNovaCidade` inalterados (fluxo Pefin).

### 2026-05-25 — gc/FinanceiroParametroDifal: edição em modal Ajax
- `ModalCreateEditParametroDifal` + `AjaxCreateEditParametroDifal`; view modal; Index com `mainModal.load` + redraw `dtGcFinanceiroParametrosDifal`; auditoria `DataRowInUseSerialized`/`LibAudit` preservada; GET `Edit` → Index; sem Create (só edição).

### 2026-05-25 — g/Usuarios: CreateEdit em modal Ajax (padrão Filiais)
- `ModalCreateEditUsuario` + `AjaxCreateEditUsuario`; view `ModalCreateEditUsuario.cshtml`; Index com `mainModal.load` + redraw `dtGUsuarios`; GET `Create`/`Edit` → Index; POST legados mantidos; `ModalUsuarioTrocarSenha` inalterado.

### 2026-05-25 — g/Filiais: CreateEdit em modal Ajax (padrão FinanceiroLancamentos)
- `ModalCreateEditFilial` + `AjaxCreateEditFilial`; view `ModalCreateEditFilial.cshtml` (`_Modal.cshtml`); Index com `$("#mainModal").load` + redraw `dtGFiliais`; `Create`/`Edit` GET redirecionam ao Index; POST legados mantidos.

### 2026-05-25 — Areas a/crm/gc/qa Views: normalização CRLF e linhas em branco
- `Scripts/2026_05_25_gdi_normalize_g_views_format.ps1` (multi-pasta) — **145** ficheiros em `Areas/a|crm|gc|qa/Views`; **128** alterados (LF→CRLF + linhas em branco excessivas); sem alteração de lógica.

### 2026-05-25 — Areas/g/Views: normalização CRLF e linhas em branco
- Script `Scripts/2026_05_25_gdi_normalize_g_views_format.ps1` — 84 ficheiros em `Areas/g/Views` com **CRLF** Windows; 69 ajustados (28 LF-only + linhas em branco duplicadas); sem alteração de lógica Razor/JS.

### 2026-05-25 — ClassificacaoFinanceira/CentrosCustos: Editar árvore (Wunderbaum)
- `GdiTreeGetSelectedKey` usa `activeNode` quando `getSelectedNodes()` vazio (clique ativa, não marca `selected` sem checkbox). Corrige botão **Editar** em `/g/ClassificacaoFinanceira` e `/g/CentrosCustos`. **VersionERP** 2026.51.36.

### 2026-05-25 — G-TREE-01 Lote 3: remoção jstree
- Pasta `jstree-3.3.4` e entradas `.csproj` removidas; flag **16** carrega só Wunderbaum. Ícone raiz em `startprime/images/icons8-genealogy-24.png`. CSS jstree retirado de 3 PDFs; `qa/GedSGQ/IndexPops` → `LayoutLite` (sem `#tree` morto). `start.js` sem compat jstree. **VersionERP** 2026.51.35.

### 2026-05-25 — G-TREE-01 Lote 2: CentrosCustos → Wunderbaum
- `CentrosCustosController`: `MontarArvoreCentrosCustos` recursivo (substitui 4 níveis fixos + Ajax); `ViewBag.CentrosCustosTreeJson` no `Index`; `GetTreeViewCentroCusto` reutiliza o builder. `CentrosCustos/Index` com `GdiTreeInit`. `start.js`: `GdiTreeNormalizeIcon` para classes Font Awesome no Wunderbaum. **VersionERP** 2026.51.34.

### 2026-05-25 — G-TREE-01 Lote 0+1: Wunderbaum + piloto ClassificacaoFinanceira
- Vendor `wunderbaum-0.14.1` (UMD+CSS) em `LibUI`; flag GdiPageScripts **16** carrega Wunderbaum + jstree (transição). `start.js`: `GdiTreeInit`, `GdiTreeNormalizeSource`, `GdiTreeGetSelectedKey`; `jsYesEditRecordJsTree` compatível Wunderbaum/jstree. `ClassificacaoFinanceira/Index` migra para Wunderbaum com dados inline (`JsTree3Node` inalterado). **VersionERP** 2026.51.33.

### 2026-05-25 — Assistentes (`g`): módulo MVC removido
- Apagado `AssistentesController.cs` e entrada no `.csproj`; sem views no repo. Entidade EF `g_assistentes` / tabela SQL mantidas (sem superfície MVC). Menu/roles em BD — limpar manualmente se ainda existirem.

### 2026-05-25 — ClassificacaoFinanceira (`g`): árvore presa em «Loading…»
- Árvore serializada no `Index` (`ViewBag.ClassificacaoFinanceiraTreeJson`) — jstree usa dados inline em vez de Ajax pendente; `MontarArvoreClassificacaoFinanceira` com proteção a ciclos pai/filho; `GetTreeViewClassificacaoFinanceira` reutiliza o mesmo builder.

### 2026-05-25 — Vendedores (`g`): remoção legado `gdc_consultas`
- `VendedoresController.Edit`: SQL `gdc_consultas` / `gdc_consultas_tabelas_vendedores` removido; `CreateEdit` sem aba «Tabela Preços Vendedor»; `Index` sem JS morto de exportar/copiar/alterar preços; models `CstVendedoresTabelasDetalhesModel` e `CstAlterarPrecosTabelasRevendaVendedor` apagados.

### 2026-05-25 — NuGet Lote A: SkiaSharp + MathNet removidos; docs sem trilha ASP.NET Core
- Removidos `SkiaSharp`, `SkiaSharp.NativeAssets.Win32` e `MathNet.Numerics.Signed` (usings mortos); pastas `packages/` apagadas. Build Release + testes lookups OK. Decisão: **sem** migração ASP.NET Core; manifesto **78** pacotes.

### 2026-05-25 — System.Runtime.Caching: remoção pacote NuGet órfão (GAC mantido)
- Retirado `System.Runtime.Caching` 10.0.7 de `packages.config`; pasta `packages/System.Runtime.Caching.10.0.7` apagada. Referência de framework em `.csproj`/testes e uso de `MemoryCache` preservados (GAC .NET 4.7.2). Build Release OK.

### 2026-05-25 — ZString: remoção de pacote órfão (sem uso no código)
- Retirado `ZString` 2.6.0 de `packages.config` e `.csproj`; pasta `packages/ZString.2.6.0` apagada. Build Release OK; `ZString.dll` deixa de ir para o publish.

### 2026-05-25 — SkiaSharp: remoção NativeAssets Linux/macOS (IIS Windows)
- Retirados `SkiaSharp.NativeAssets.Linux.NoDependencies` e `SkiaSharp.NativeAssets.macOS` de `packages.config` e `.csproj` (Error/Import); pastas físicas apagadas em `packages/`. Mantidos `SkiaSharp` + `SkiaSharp.NativeAssets.Win32`. Publish ~141 MB mais leve; sem impacto em runtime IIS.

### 2026-05-25 — ReportEmailPedido: HTML de e-mail alinhado a Bootstrap 5.3.8
- `getEmailOrcamentoPedido` — CDN BS 4.3.1/FA 4.7 trocados por Bootstrap **5.3.8** e Font Awesome **7.2.0** (jsDelivr); markup `panel`/`borderless` → `card`/`table-borderless`/`table-bordered`; removidos scripts JS (inúteis em e-mail); correções `utf-8`, nº movimento dinâmico e data validade.

### 2026-06-05 — Bootstrap 5 LibUI: remoção NuGet legado + bootstrap4-toggle (lotes 1–3)
- **Lote 1:** `bootstrap.bundle.min.js` vendorizado em `LibUI_AdminLTE-4.0.0/plugins/bootstrap-5.3.8/`; refs em `_LayoutScriptsOptional`, login; removido pacote NuGet `bootstrap` e ficheiros `Scripts/`/`Content/bootstrap*`.
- **Lote 2:** removido `bootstrap4-toggle` (BS4), flag `GdiPageScriptsFlags.BootstrapToggle`, partials e lazy-load em `start.js`/`GdiPageScriptRegistry`.
- **Lote 3:** apagados layouts mortos `Areas/g|gc|qa/Views/Shared/_Layout.cshtml`; `BundleConfig` só `jqueryval` + `libui-swal-compat`. Build Release OK. **Publish:** incrementar `VersionERP` após `start.js`.

### 2026-06-05 — packages/: limpeza de 26 pastas órfãs no disco
- Script `Scripts/2026_06_05_gdi_remove_orphan_packages_dirs.py` — remove pastas em `packages/` ausentes de `packages.config` (chmod + `rmtree`).
- **26 pastas** apagadas (Graph/Kiota/Identity/Azure, jQuery, Modernizr, ImageSharp, etc.); **86** restantes = manifesto atual; build Release OK.

### 2026-06-05 — packages.config: auditoria e remoção de 26 pacotes mortos
- **Validado:** 112 → **86** pacotes; versões alinhadas ao `.csproj` e `targetFramework="net472"`.
- **Removidos:** bloco Microsoft.Graph/Kiota/Identity/Azure (zero uso em código); órfãos `jQuery`, `Modernizr`, `Microsoft.AspNet.WebApi` (metapacote), `SixLabors.ImageSharp` (só props).
- **Sincronizado:** `.csproj` (References/Import), `Web.config` (binding redirects), `using` mortos em `CustomPrincipal` e `FinanceiroLancamentosController`.
- **Build Release OK.** Candidatos futuros: `SkiaSharp.NativeAssets.Linux/macOS`, `ZString`, `bootstrap` NuGet (UI ativa = LibUI).

### 2026-06-05 — Web.config: auditoria e alinhamento de versões
- **7 ficheiros** revistos: raiz (`Web.config`, `Web.Debug.config`, `Web.Release.config`), `Views/Web.config`, áreas `a|g|gc|qa|crm`.
- **Correções:** `MvcWebRazorHostFactory` e assembly MVC em todas as Views → **5.3.0.0** (NuGet `Microsoft.AspNet.Mvc` 5.3.0); binding redirects IdentityModel (Tokens, Protocols, Jwt, JsonWebTokens, Logging) **8.15.0.0 → 8.18.0.0** alinhados a `packages.config`.
- **Validado:** `Scripts/2026_05_22_gdi_verify_web_release_transform.ps1` — Release sem `debug`, `customErrors On`, compressão PERF-012 OK; áreas `web.config` já no `.csproj`.

### 2026-06-05 — GDI-ERP-Plataform.csproj: auditoria e correções
- **Validação:** 1069 Includes, 0 paths em falta, 0 duplicados, 0 views `Gdi*` fora do csproj.
- **Correções:** SourceLink fallback 8.0.0 → 10.0.203; `ExcludeFoldersFromDeployment` + strip publish `.claude`; `None` para `AI-CONTEXT`/`CHANGELOG`/`BACKLOG`; `Content` templates `appSettings` + novo `sql-server.local.config.example`; script `2026_06_05_gdi_verify_csproj_includes.py`.

### 2026-06-05 — Remoção `nul)` acidental na raiz (F06)
- Artefacto cmd (redirecionamento `2>nul)` inválido); apagado com `Remove-Item -LiteralPath`; F06 resolvido em `docs/AVALIACAO-TECNICA-ERP-GDI.md`.

### 2026-06-05 — Remoção pasta `.md/` legada: csproj e refs
- Pasta **`.md/`** removida (duplicata de `docs/`); canónico: `docs/investigacao-timeout-sessao.md`, `docs/relatorio-migracao-netframework-472-481.md`.
- **`.csproj`:** removidos `Content` `.md\*`; `CLAUDE.md` e docs relevantes → `None Include` (versionam no Git, **não** vão ao IIS).
- **`roadmap.md`** / `0004 - auditoria-tecnica-relatorio.md` só existiam em `.md/` — recuperar do histórico Git se ainda forem necessários.

### 2026-06-05 — .gitignore: consolidar Git × IIS + corrigir ProdutosController
- **Reescrita** `.gitignore` (~543 → ~200 linhas): removida duplicata VisualStudio; removidos `*.dll`/`*.exe` globais; removida linha acidental `/Areas/g/Controllers/ProdutosController.cs`.
- **GDI:** `**/_filestemp/`, `App_Data/Secrets/*Copia*`, `.claude/worktrees/`, `nul`/`nul)`; exceção `!Rotativa/*.exe` (PDF no IIS).
- **`.csproj`:** `Rotativa\wkhtmltopdf.exe` e `wkhtmltoimage.exe` como `Content` para publish IIS.

### 2026-06-05 — Prompt genérico enxugar memória (reutilizável)
- **Novo:** `.cursor/context/2026_06_05_prompt-enxugar-memoria-projeto-generico.md` — prompt completo para auditoria/consolidação de memória em **qualquer** projeto (7 fases, hierarquia, anti-padrões).

### 2026-06-05 — Auditoria memória IA: centralizar, enxugar duplicatas
- **Índice único:** `.cursor/context/2026_06_05_indice-memoria-ia.md` — navegação por tema + mapa de duplicatas removidas.
- **`AI-CONTEXT.md` / `CLAUDE.md`:** reduzidos a ponteiro + regras mínimas; detalhe arquitetura/lotes N-* só em contextos datados.
- **`CHANGELOG-DEV.md`:** compactado (~200 linhas); entradas antigas → histórico arquivado.
- **Órfão removido:** `pascalcase-areas-renomeacao-lotes.md` (duplicata sem prefixo).
- **`sync_changelog_recent.py`:** fonte corrigida para `CHANGELOG-DEV.md` na raiz.

### 2026-06-05 — Lote legado 2026_05_20_*: renomear por CreationTime + refs
- 62 renomeações (`2026_05_21_`/`2026_05_22_`/`2026_05_15_`); 85 refs; script `2026_06_05_gdi_rename_legacy_2026_05_20_prefixes.py`; inventário prefixos exit 0.

### 2026-06-05 — G-PUB smoke + I-1b + convenção prefixos
- Smoke arquitetura 5 inventários exit 0; 18× `int.Parse` → `LibNumbers.ConvertInt` em relatórios/g; 35 ficheiros renomeados `2026_05_25_`/`2026_06_01_` → datas corretas.

### 2026-05-29 — Painel Indicadores Qualidade Pós-Venda (ISO 9001) Fases 1–3
- `IndicadoresQualidade` hub + `IndicadoresQualidadePosVenda` (KPIs, Flot, DataTables, Excel ClosedXML); pedido entregue = `id_movimento_posicao >= 6`.

### 2026-05-28 — Ambiente tenant + WhatsApp gerencial
- `CstTenant.ambiente`; `SetTenants()` público; `JobServerController` sem mapeamento duplicado host→DB.
- `RoboWhatsAppGerencial` — resumo diário 18h via Z-API + Task Scheduler.

### 2026-05-25 — Arquitetura centralizada ERP + ciclo N (servidor/cliente)
- **`GdiMvcJsonResults`** Lib; wrappers `JsonDataTableException`/`JsonAjaxErro*` delegam (31+ controllers).
- Modais GET gc/g com guards; remoção `GC.Collect` gc; inventários LibExceptions → **0** em a/crm/g/qa.
- Cliente: C-1/C-2 handlers Ajax homogéneos (gc + 115 views a/g/gc/qa); DT `xhr.dt`/`error.dt`.
- **Detalhe lotes N-A…N-V:** `2026_06_05_arquitetura-centralizada-erp-gdi.md` + histórico — não reexpandir aqui.

### 2026-06-01 — UI cadastros/gc + API pública lote-documento
- Cadastros carga inicial (exceto Produtos/Clientes deferLoading); yesFilterOnOff/Limpar amarelo corrigido (35 views auditadas).
- Select2 pesquisa local; Fretes/Cfop/PagRecCondicoes/Estoque alinhamentos Index.
- `LoteDocumentoPublicoController` — API `GET /api/public/lote-documento` migrada do portal descontinuado.

### 2026-05-20 — Baseline modernização (Fases 0–17)
- DataTables/Ajax padronização; lookups Onda 6; remoção módulos legados; PascalCase B1–B2d; CSRF/XSS fases iniciais.
- **Detalhe:** `docs/dev-history/CHANGELOG-DEV-HISTORICO-INICIAL.md`.

*Entradas anteriores e blocos longos por ficheiro tocado:* arquivo histórico — **não** reabrir neste changelog operacional.

---

## Pendências abertas

Lista completa e critérios de aceite → **`BACKLOG-DEV.md`**.

| Prioridade | Resumo |
|------------|--------|
| Alta | Smoke pós-publish; NFe e-Notas homologação; Release/IIS/health |
| Média | 14 controllers híbridos `ViewBag.combo`; smoke Index cadastros |
| Baixa | Bump opcional 4.8.1 (sem Core); filtro legado residual; índices SQL (DBA) |

---

## Alertas técnicos

| Alerta | Ação |
|--------|------|
| DataTables vs MVC | `2026_05_20_tabelas-datatables-vs-mvc.md` antes de CSS em `<table>` |
| Views `Gdi*` fora do `.csproj` | `python Scripts/2026_05_20_gdi_verify_csproj_gdi_helpers.py` |
| Cache `start.js` | Incrementar `VersionERP` |
| Publish `obj`/PackageTmp | Limpar `obj` se Access denied |
| Filtro Index `id > 1` na base | Pode excluir id=1 — padrão Filiais/Clientes |
| Portal pós-deploy | Novo login se cache `contextoModel_*` legado |
| UTF-8 BOM | `python Scripts/2026_05_20_gdi_inventory_utf8_bom.py --fail` |

---

## Histórico completo

| Arquivo | Conteúdo |
|---------|----------|
| `docs/dev-history/CHANGELOG-DEV-HISTORICO-INICIAL.md` | Snapshot integral — **não editar** para registo diário |
| `.cursor/context/2026_06_05_indice-memoria-ia.md` | Índice temas + scripts |
| `.cursor/CHANGELOG-DEV.md` | Redirecionamento para este ficheiro |

**Registar intervenção:** uma entrada compacta em «Últimas alterações» + `BACKLOG-DEV.md`; bloco longo só em contexto datado ou histórico.
