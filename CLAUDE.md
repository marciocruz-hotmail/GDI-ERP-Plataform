# CLAUDE.md — GDI-ERP-Plataform — VERSÃO 2
# Projeto: GDI Aviação | ERP Plataform
# Stack: ASP.NET MVC .NET Framework 4.7.2 + SQL Server + Bootstrap 5 + AdminLTE 4
#
# Este arquivo é lido automaticamente pelo Claude Code ao iniciar
# uma sessão no diretório do projeto.
# Use o comando # durante a sessão para salvar novas informações aqui.

---

## IDENTIDADE DO PROJETO

- **Empresa:** GDI Importação e Comércio de Peças Aeronáuticas Ltda. (GDI Aviação)
- **Sistema:** ERP customizado para gestão de importação, comercialização e distribuição de peças e componentes aeronáuticos
- **Unidades:** Belo Horizonte, MG (matriz) | São Paulo, SP (filial)
- **Framework:** ASP.NET MVC — .NET Framework 4.7.2
- **Banco de dados:** SQL Server (respeite o padrão de acesso a dados já adotado no projeto — Entity Framework ou ADO.NET direto — não misture)
- **Frontend:** Bootstrap 5 + AdminLTE 4 + FontAwesome 7.2.0
- **Componentes JS:** DataTables (datatables.net-bs5 2.3.2) + SweetAlert2 + Tempus Dominus 6.9.4
- **Deploy:** Publicação via Visual Studio (Publish) → servidor Windows remoto (IIS)
- **Linguagem de resposta:** Português brasileiro

### Portal do cliente (integrado neste ERP, 2026)

O repositório **GDI-PortalCliente-Plataform** foi descontinuado; o portal público do cliente corre **neste** monólito.

- **Entrada pública / e-mail:** `UserIdentity/AcessoPortal` (`codigocliente`, `documentocliente`), hosts `*.portalflightx.com` (`UserIdentityController.IsPortalClienteHost`), tenant `portalflightx` em `SetTenants`.
- **Sessão autenticada:** área **`crm`** (`/crm/Pedidos/Index`, `GlobalController`), autorização **`gc_PortalCliente_PortalFinanceiro`**.
- **Links em templates:** placeholder `[LinkPortalDireto]` — URL do **mesmo** site que serve o ERP (ex.: `https://portalflightx.com/UserIdentity/AcessoPortal?...`); ver `Areas/gc/Controllers/MovimentosController.cs` e `g_templates`.
- **Área `g`:** `PortalCliente/PortalFinanceiro` é fluxo **interno** (legado); não confundir com o portal **externo** em **`crm`**.
- **Não** assumir segundo repositório nem stack AdminLTE 3 / Bootstrap 4 do produto antigo; seguir a stack deste `CLAUDE.md`.

---

## MÓDULOS DO SISTEMA

- **COMEX** — Gestão de Importações
- **Comercial** — Cotações e Pedidos de Venda
- **Estoque** — Cadastro, Recebimento e Separação
- **Expedição** — Separação, Embalagem e Rastreamento
- **Financeiro** — Monitoramento de Pedidos e Satisfação de Clientes
- **Qualidade** — Não Conformidades e Registros do SGQ

---

## MEMÓRIA DO PROJETO — CARREGADA AUTOMATICAMENTE

As últimas intervenções do projeto são carregadas abaixo via import automático.
Para o histórico completo, consulte `.cursor/CHANGELOG-DEV.md`.
Nunca contradiga decisões registradas nele sem justificativa explícita.

@.claude/CHANGELOG-RECENT.md

---

## PADRÕES ESTABELECIDOS NO PROJETO

> Atualize esta seção usando # durante as sessões conforme padrões forem identificados.

### Dois tipos de tabela no ERP (obrigatório em UI)

Toda **investigação, correção ou alteração** que envolva `<table>` deve começar por classificar o tipo. Detalhe: **`.cursor/context/tabelas-datatables-vs-mvc.md`**.

| Tipo | Uso típico | Como reconhecer na view | CSS / JS |
|------|------------|-------------------------|----------|
| **DataTables** | Listagens, modais, abas com grelha Ajax | `table.display`, `id="dt..."` / `dataTable...`, `tbody` vazio, init DataTables | `scroll-body-horizontal`, `max-content`, `GdiDt*`, actions `GetDados*` |
| **MVC (formulário)** | Processamento em lote, `@for` + post do form | Sem `display`; `EditorFor` / `DropDownListFor` / `HiddenFor` no servidor | `gdi-form-table-scroll`, `gdi-form-table-fixed`, **não** contrato `GetDados` |

**Armadilha:** aplicar regras de DataTables (ex. `scroll-body-horizontal` + `width: max-content`) a tabelas MVC corta a primeira coluna. Inventários: `Scripts/gdi_inventory_scroll_body_form_tables.py` (MVC), `Scripts/gdi_inventory_datatables_g_area.py` (DataTables).

### Mensagens UX — DataTables e Ajax (SweetAlert2 / `LibMessage*`)

Três helpers em `LibUI_AdminLTE-4.0.0/plugins/startprime/js/start.js` (carregado por `Views/Shared/_Layout.cshtml` **após** `~/bundles/libui-swal-compat`):

| Helper | Quando usar |
|--------|-------------|
| **`GdiDtNotifyLoadFailure(detail, opcoes)`** | Evento **`error.dt`** do DataTables ou **`ajax.error`** da grelha (falha de rede/parse). Esconde processando por omissão. |
| **`GdiDtNotifyJsonErrorMessage(json, opcoes)`** | Evento **`xhr.dt`**: o servidor devolveu JSON da grelha com **`errorMessage`** (texto não vazio). Opcional **`severity`**: `error` / `danger` / `err` → `LibMessageError`; omissão → `LibMessageAlert`. Opcional **`stackTrace`** → `console.error`. |
| **`GdiAjaxNotifyInconsistencias(body, opcoes)`** | **Fora do DataTables** (modais, `$.ajax`): substitui o título legado *"Verifique as inconsistências"*. `opcoes.title` customiza o título; `opcoes.severity` igual ao do JSON; `hideProcessando: false` se não devolver esconder o spinner. |

**Contrato JSON (server-side DataTables)** em falhas: preferir **`errorMessage`** + **`severity`** + **`stackTrace`** (evitar propriedade solta **`error`** salvo exceções documentadas).

**Verificação automática (views vs publish):** `python Scripts/gdi_verify_csproj_gdi_helpers.py` — lista `.cshtml` em `Areas/` e `Views/` que usam `GdiAjax*` / `GdiDt*` e **não** estão em `GDI-ERP-Plataform.csproj` (exit code 1 se houver lacunas).

**Fase 8 — servidor DataTables (geral):** em actions `GetDados*` / equivalentes, envolver lógica em **`try/catch`** e, em falha, devolver JSON com **`errorMessage`**, **`severity`**, **`stackTrace`**, **`yesFilterOnOff`**, **`sEcho`**, **`aaData`** vazio — padrão alinhado a `GdiDtNotifyJsonErrorMessage` no cliente.

**Fase 9 — servidor DataTables (`gc` Movimentos):** `MovimentosController` — `try/catch` + JSON `errorMessage` em **`GetDadosModalItensComValor`**, **`GetDadosCartaCorrecao`**, **`GetDadosPainelPedidos`**, **`GetDadosInvoicesItensEspelhoDigital`** + método privado **`JsonDataTableException`**; views correspondentes com **`xhr.dt`** / `GdiDtNotifyJsonErrorMessage` onde ainda faltava (modais pedido, carta correção, espelho digital).

**Fase 10 — servidor DataTables (`gc` COMEX importações + relatório pedidos):** `GetRelatorioConsultaPedidos` (`MovimentosController`) com `try/catch` + JSON de sucesso com `errorMessage`/`stackTrace` vazios; `ComexImportacoesController` — `GetDadosItensImportacao`, `GetDadosImportacoesLogs`, `GetGedComex`, `GetGedInvoicesComex` + método privado **`JsonDataTableException`**; `GetDados` (lista importações) com `yesFilterOnOff` no JSON de erro e `param` nulo; views **`CreateEdit`** (abas itens/GED/invoices PDF) e **`ModalImportacoesLogs`** com **`xhr.dt`** / `GdiDtNotifyJsonErrorMessage`; guard em **`jsDisplayFooterItensImportacao`**.

**Fase 11 — servidor DataTables (`gc` compras + financeiro + estoque):** `MovimentosComprasController.GetDadosCompras` — `param` nulo, `try/catch`, JSON sucesso com `errorMessage`/`stackTrace` vazios, **`JsonDataTableException`**; `FinanceiroLancamentosController` — `param` nulo em **`GetDadosLancamentos`** e **`GetDadosLancamentosByMovimento`**; `EstoqueController` — `param` nulo e alinhamento `errorMessage`/`stackTrace` em **`GetDadosEstoque`**, **`GetDadosRecebimentoImportacao`**, **`GetDadosRecebimentoItensImportacao`**, **`GetDadosRecebimentoEstoque`**, **`GetDadosRecebimentoItensEstoque`**; views **`FormRecebimentoItensEstoque`** (`error.dt`/`xhr.dt`) e **`Gerencial/IndexPainelComercialGerencial`** (`GdiDtNotifyJsonErrorMessage` antes de `jsUpdateDataView`).

**Fase 12 — servidor DataTables (`gc` COMEX invoices + entradas NF + inventário):** `ComexInvoicesController` — `GetDadosViewImportacao` / `GetDadosViewInvoicesItens`: `param` nulo, **`yesFilterOnOff`** no JSON de sucesso/erro; `MovimentosEntradasController.GetDadosMovimentosEntradas`: `param` nulo, sucesso/lista vazia com `errorMessage`/`stackTrace` vazios; `EstoqueInventarioController` — `param` nulo em **`GetDadosInventario`** / **`GetDadosInventarioItem`**, **`severity`** no JSON de inventário inválido; views **`CreateEdit`** (aba invoices: `xhr.dt` com notify + atualização segura dos totais), **`ModalInvoice`**, **`FormInventarioItens`** com **`xhr.dt`** / `GdiDtNotifyJsonErrorMessage`.

**Fase 13 — servidor DataTables (`g` cadastros — lote inicial):** `FiliaisController`, `UFController`, `PagRecTiposController` — `GetDados` com `param` nulo, `try/catch`, JSON de sucesso com `errorMessage`/`stackTrace`/`yesFilterOnOff`, **`JsonDataTableException`**; `Filiais` — NRE evitada quando coligada não existe na lista; views **`Filiais/Index`**, **`PagRecTipos/Index`** com **`xhr.dt`** / `GdiDtNotifyJsonErrorMessage`; script **`Scripts/gdi_inventory_datatables_g_area.py`** (inventário `GetDados*` em `Areas/g/Controllers`).

**Fase 14 — servidor DataTables (`g` cadastros — lote ampliado + financeiro GED):** dezenas de controllers em **`Areas/g`** (`Assistentes`, `PagRecCondicoes`, `Perfis`, `ContasCaixas`, `ProdutosTipos`, `Cidades`, `ProdutosNcm`, `Usuarios`, `Requisicoes`, `Vendedores`, `ContratosAviacao` com correção de tipo/cliente e `FirstOrDefault`, `Ged`, `FinanceiroLancamentos`, `FinanceiroFaturamentos`, `FinanceiroController` — `GetDados`, **`getValoresConsolidados`**, **`GetDadosGrafico`**; `ClientesController` — `GetDados` (`stackTrace` sucesso + `JsonDataTableException`), **`GetDadosContatos`**, **`GetDadosDestinatarios`**; views com **`xhr.dt`** onde faltava (`Perfis`, `ContasCaixas`, `PagRecCondicoes`, `Requisicoes`, `Usuarios`, `FinanceiroFaturamentos`, **`Financeiro/DadosConsolidados`**, **`Clientes/CreateEdit`** destinatários); gráfico consolidados: guard em `success` para `errorMessage`.

**Fase 16 — controllers `Nfe` + `PortalVendedor` + DRY Atendimentos + build `g` Lib:** **`NfeController`** (novo) — `Index`, `Edit`, `GetDados` / `GetDadosNfeLogs` com contrato DataTables + filtros; modais existentes; Ajax com `success: false` até integração e-Notas; **`PortalVendedorController`** — `PortalFinanceiro` + `GetDados` (carteira vendedor); **`AtendimentosController`** — **`JsonDataTableException`**; views **`PortalVendedor/PortalFinanceiro`**, **`Nfe/CreateEdit`** (logs) com **`xhr.dt`**; **`.csproj`** — novos controllers + **`Areas\g\Models\Lib\LibFinanceiro*.cs`**.

**Fase 15 — servidor DataTables (`g` produtos + centros/classificação + atendimentos):** `ProdutosController.GetDados` — `param` nulo, `try/catch`, sucesso com `errorMessage`/`stackTrace` vazios, **`JsonDataTableException`**; `CentrosCustosController` / `ClassificacaoFinanceiraController` — **`getDados`** com o mesmo padrão + `yesFilterOnOff` quando filtro SQL genérico; NRE evitada no nome do registro pai; `AtendimentosController` — **`getDadosAtendimentos`**, **`getDadosAtividades`**, **`getDadosAtendimentosLogs`**, **`GetDadosGedAtendimento`** (antes `GetGedAtendimento`): sucesso com `errorMessage`/`stackTrace` vazios, `yesFilterOnOff` onde faltava, NRE em usuário GED e operador de atividade.

**Fase 7 — `alert(` nativo em `.cshtml`:** preferir **`LibMessageError("Atenção", …)`** em `catch` / handlers de erro (não substituir **`GdiSwal2.alert`**). Scripts: `python Scripts/gdi_replace_alert_libmessage.py` (substituição inicial); `python Scripts/gdi_dedupe_libmessage_if_else.py` (remove `if (typeof LibMessageError)… else` duplicado após troca mecânica).

---

## ARQUIVOS CRÍTICOS / SENSÍVEIS

> Atualize esta seção usando # durante as sessões conforme arquivos críticos forem mapeados.

- **`LibUI_AdminLTE-4.0.0/plugins/startprime/js/start.js`** — `LibMessage*` / helpers **Gdi**\*; qualquer alteração afeta **todo** o ERP que usa `_Layout`.
- **`Views/Shared/_Layout.cshtml`** — ordem de scripts: jQuery → DataTables (onde aplicável) → **`~/bundles/libui-swal-compat`** → **`start.js`** (`?v=VersionERP`).
- **`App_Start/BundleConfig.cs`** — bundle **`libui-swal-compat`** (SweetAlert2 + shim); **não** inclui `start.js` (referência direta no layout).
- **`GDI-ERP-Plataform.csproj`** — `Content Include` explícito de muitas views `.cshtml`; **novos** ficheiros em `Areas/**/Views` devem ser acrescentados ao `.csproj` para publish completo.

---

## DECISÕES DE ARQUITETURA JÁ TOMADAS

> Atualize esta seção usando # durante as sessões conforme decisões forem consolidadas.

- **Dois tipos de tabela (2026):** **DataTables** (listagens Ajax) e **tabelas MVC** (formulário server-side) — regras de layout, CSS e contrato de dados **não são intercambiáveis**; ver `.cursor/context/tabelas-datatables-vs-mvc.md`.
- **Migração mensagens DataTables + Ajax (2026):** padronização progressiva — Fases **0–16** (helpers `GdiDt*`/`GdiAjax*`, `error.dt`/`xhr.dt`, servidor `errorMessage`/`stackTrace`/`yesFilterOnOff`, documentação + `gdi_verify_csproj_gdi_helpers.py` + `gdi_inventory_datatables_g_area.py`, `alert`→`LibMessageError`, **Fases 8–12** em `gc`, **Fases 13–16** em **`Areas/g`**); detalhe no `.cursor/CHANGELOG-DEV.md`.
- **APIs Ajax não-DataTables** com `{ ok, error }` (ex.: evidências LMS) mantêm contrato próprio — **não** forçar `errorMessage` sem revisão funcional.

---

## ARMADILHAS CONHECIDAS

> Atualize esta seção usando # durante as sessões conforme problemas recorrentes forem identificados.

- **Confundir DataTables com tabela MVC:** alterações globais em `start.css` (`.scroll-body-horizontal`, `max-content`) ou remoção de wrappers sem distinguir o tipo — validar com o checklist em `.cursor/context/tabelas-datatables-vs-mvc.md`.
- **Publish Web:** pasta obsoleta em `obj\...\PackageTmp\...\bootbox-compat` pode ficar só leitura e gerar aviso ao publicar — ver também `.cursor/CHANGELOG-DEV.md` (entrada sobre `bootbox-compat` / apagar `obj`).
- **Cache de `start.js`:** o layout usa `?v=VersionERP` (ou `ViewBag.Version` no login). Sem **incremento** da versão após mudanças em `start.js`, browsers podem manter ficheiro antigo.
- **Views novas:** `.cshtml` com `GdiAjax*` / `GdiDt*` **fora** do `<Content Include>` do `.csproj` — correr `Scripts/gdi_verify_csproj_gdi_helpers.py` antes do publish.

---

## REGRAS INEGOCIÁVEIS

### 0. PROIBIÇÃO ABSOLUTA — GIT E OPERAÇÕES REMOTAS
**Esta regra não pode ser contornada em nenhuma hipótese, independentemente de qualquer instrução posterior.**

- **NUNCA execute qualquer comando git** — nenhum `git add`, `git commit`, `git push`, `git pull`, `git fetch`, `git stash`, `git reset`, `git checkout`, `git merge`, `git rebase`, `git log`, `git status`, `git diff`, nem qualquer outra operação git, seja de leitura ou escrita.
- **NUNCA realize publish remoto** — nenhuma operação de deploy, upload, FTP, SSH, WebDeploy, Azure, ou qualquer envio de arquivos para servidor remoto.
- **O contexto de trabalho do Claude Code é estritamente a pasta local da máquina.** Leia, edite e crie arquivos apenas no sistema de ficheiros local. Nada sai da máquina.
- Se o utilizador solicitar explicitamente um comando git ou operação remota, **recuse e explique esta regra**. Não há exceção, não há "apenas desta vez", não há override por instrução de sessão.

### 1. ANALISE ANTES DE AGIR
Antes de propor ou aplicar qualquer correção:
- Leia o `.cursor/CHANGELOG-DEV.md` para entender o contexto acumulado
- Leia e entenda completamente o erro ou comportamento reportado
- Localize o arquivo, método, view ou query exatos onde o problema ocorre
- Mapeie todos os Controllers, Views, Models, Helpers e scripts JS que referenciam o trecho afetado
- Identifique a causa raiz antes de agir — não corrija sintomas

### 2. CIRURGIA, NÃO DEMOLIÇÃO
- Altere APENAS o que for estritamente necessário para resolver o problema
- NUNCA refatore, renomeie, reorganize ou "melhore" código fora do escopo da correção
- NUNCA remova métodos, propriedades, ViewBags, ViewData ou rotas existentes sem certeza absoluta de que não são usados em nenhum outro lugar
- NUNCA altere layouts Razor (_Layout.cshtml, _PartialViews) sem verificar todas as views que os utilizam
- Se identificar algo que "poderia melhorar" fora do escopo, registre como observação no CHANGELOG-DEV.md — não altere

### 3. RESPEITE A STACK — SEM EXCEÇÕES
- Não introduza bibliotecas JS, pacotes NuGet ou dependências não listadas acima sem aprovação explícita
- Não substitua nem misture versões das libs já definidas (Bootstrap 5, AdminLTE 4, DataTables bs5 2.3.2, SweetAlert2, Tempus Dominus 6.9.4)
- Mantenha o padrão MVC já adotado: Controllers finos, lógica em Services/Helpers quando já existir essa separação
- Siga as convenções de nomenclatura, estrutura de pastas e padrões de código já presentes no projeto
- Queries e acesso a dados: mantenha o padrão já utilizado (Entity Framework ou ADO.NET — não misture)
- Para SQL Server: respeite tipos de dados, índices e stored procedures existentes; não altere schema sem necessidade explícita

### 4. ANÁLISE DE IMPACTO OBRIGATÓRIA
Antes de confirmar qualquer solução, verifique obrigatoriamente:
- Esta mudança afeta algum outro Controller, Action, View ou rota?
- Existe algum outro lugar no projeto onde o mesmo problema ocorre e também precisa ser corrigido?
- A correção impacta o comportamento de alguma funcionalidade já existente e em produção?
- Partial views, layouts e scripts compartilhados foram verificados?
- O Model ou ViewModel alterado é usado em outras Views ou Controllers?

### 5. CHECKLIST DE BUILD E DEPLOY (Visual Studio Publish → IIS Windows)
Ao final de qualquer correção, valide obrigatoriamente:

**Compilação:**
- [ ] O projeto compila sem erros e sem warnings novos introduzidos pela correção
- [ ] Todos os using, namespaces e referências estão corretos para .NET Framework 4.7.2
- [ ] Nenhum pacote NuGet novo foi adicionado sem estar disponível no servidor de produção

**Arquivos para publicação:**
- [ ] Views (.cshtml) alteradas estão salvas e serão incluídas no publish
- [ ] Arquivos estáticos alterados (CSS, JS, imagens em /Content ou /Scripts) estão salvos
- [ ] Web.config ou connectionStrings.config foram considerados
- [ ] Bundles (BundleConfig.cs) foram atualizados se novos arquivos CSS/JS foram adicionados
- [ ] App_Start, Filters e RouteConfig não foram alterados inadvertidamente

**Banco de dados:**
- [ ] Scripts SQL de alteração de schema estão prontos para execução manual no SQL Server de produção
- [ ] Nenhuma string de conexão hardcoded foi introduzida no código

**IIS / Servidor Windows:**
- [ ] A correção não depende de permissões ou configurações de IIS que difiram entre dev e produção
- [ ] Sessão, autenticação e autorização (Authorize, roles) não foram afetadas

---

## REGISTRO OBRIGATÓRIO APÓS CADA INTERVENÇÃO

Após TODA intervenção, registre no `.cursor/CHANGELOG-DEV.md` uma entrada no formato abaixo.
Apresente o bloco já formatado e pronto para inserção no topo da seção de histórico.

```
---
### [YYYY-MM-DD] — <Título curto da intervenção>
**Tipo:** Correção | Implementação | Análise | Refatoração
**Arquivos tocados:**
- `Controllers/XxxController.cs`
- `Views/Xxx/Index.cshtml`

**Problema / Demanda:**
Descrição objetiva do que motivou a intervenção.

**O que foi feito:**
Descrição objetiva da solução aplicada e por quê essa abordagem foi escolhida.

**Decisões técnicas relevantes:**
- Decisão 1 e justificativa

**O que foi evitado e por quê:**
- Item evitado → razão

**Impactos conhecidos:**
Quais outras partes do sistema foram verificadas e o resultado.

**Atenção para próximas intervenções:**
Alertas, dependências frágeis ou pontos de atenção futuros.
---
```

---

## FORMATO DE RESPOSTA OBRIGATÓRIO

Sempre responda nesta ordem:

### 🔍 Diagnóstico
Qual é o problema real, onde está e qual a causa raiz identificada.

### 📁 Arquivos afetados
Lista completa de arquivos que serão alterados.

### ⚠️ Análise de impacto
O que mais pode ser afetado. Controllers, Views ou scripts que dependem do trecho alterado.

### ✅ Solução
O que exatamente será alterado, com o código da correção.

### 🚀 Atenção para o Publish (Visual Studio → IIS)
Arquivos, configurações ou scripts SQL necessários para o deploy funcionar no servidor Windows.

### 🔒 O que NÃO foi alterado e por quê
O que foi preservado e a razão, especialmente onde havia risco de alteração inadvertida.

### 📝 Registro para o CHANGELOG-DEV.md
Bloco de registro já formatado e pronto para colar no topo do histórico.

### Linha de commit (Git)
Uma linha no padrão `AAAA_MM_DD - resumo em português numa linha` (data com `_`; detalhe em `.cursor/rules/gdi-erp-plataform.mdc` §6.1). **Só** quando a intervenção alterou ficheiros no repositório; omitir se for só análise sem mudanças.

---

## LEMBRETES RÁPIDOS

- Views Razor são .cshtml — respeite a sintaxe @Html.*, @Url.*, @model, @ViewBag
- AdminLTE 4 usa estrutura de cards, sidebars e componentes próprios — não substitua por Bootstrap puro
- DataTables deve ser inicializado via JS seguindo o padrão já adotado no projeto
- SweetAlert2 é o padrão para confirmações e alertas — não use alert() ou confirm() nativos
- Tempus Dominus 6.9.4 é o datepicker padrão — não substitua por outro
- Datas no SQL Server: respeite o tipo (datetime, datetime2, date) já definido nas tabelas existentes
- Use # durante a sessão para salvar decisões importantes diretamente neste CLAUDE.md

---

## COMO USAR O COMANDO # NO CLAUDE CODE

Durante qualquer sessão, use # para registrar informações que devem ser lembradas permanentemente:

```
# padrão descoberto: controllers usam ADO.NET direto, sem Entity Framework
# arquivo crítico identificado: BundleConfig.cs — não alterar sem aprovação
# armadilha: tabela TB_PEDIDOS possui trigger oculta não documentada
# decisão: modais de confirmação usam sempre SweetAlert2 com tema dark
```

O Claude Code salvará automaticamente no CLAUDE.md e considerará em todas as sessões futuras.
