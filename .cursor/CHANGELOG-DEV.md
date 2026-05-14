# CHANGELOG-DEV — GDI-ERP-Plataform
# Projeto: GDI Aviação | ERP Plataform
# Stack: ASP.NET MVC .NET Framework 4.7.2 + SQL Server + Bootstrap 5 + AdminLTE 4
#
# INSTRUÇÕES:
# - Novas entradas sempre no TOPO da seção de histórico
# - Formato obrigatório definido no .cursor/rules
# - Este arquivo é lido pelo Cursor antes de cada intervenção
# - Mantenha registros objetivos — sem prolixidade
# - Não apague entradas antigas; elas são memória do projeto

---

## CONTEXTO GERAL DO PROJETO

**Descrição:** ERP customizado da GDI Importação e Comércio de Peças Aeronáuticas Ltda. (GDI Aviação), desenvolvido para gerenciar os processos de importação, comercialização e distribuição de peças e componentes aeronáuticos.

**Unidades atendidas:**
- Belo Horizonte, MG (matriz)
- São Paulo, SP (filial)

**Módulos principais do sistema:**
- COMEX — Gestão de Importações
- Comercial — Cotações e Pedidos de Venda
- Estoque — Cadastro, Recebimento e Separação
- Expedição — Separação, Embalagem e Rastreamento
- Financeiro — Monitoramento de Pedidos e Satisfação de Clientes
- Qualidade — Não Conformidades e Registros do SGQ

**Padrões estabelecidos no projeto:**
- (preencher conforme forem sendo definidos ao longo do desenvolvimento)

**Arquivos críticos / sensíveis identificados:**
- (preencher conforme forem sendo mapeados)

**Decisões de arquitetura já tomadas:**
- (preencher conforme forem sendo registradas)

**Armadilhas conhecidas:**
- Publish Web (VS/MSBuild): pasta obsoleta `obj\...\PackageTmp\...\plugins\bootbox-compat` herdada de build antigo pode ficar **somente leitura** (`d-r---`) e gerar *Warning: Access to the path 'bootbox-compat' is denied* em `Microsoft.Web.Publishing.targets`. **Solução:** apagar `obj` (ou só `obj\Release\Package`) antes de publicar; o projeto fonte não inclui mais `bootbox-compat` (usa `sweetalert2\`).
- Mesmo ficheiro de targets: *Access to the path 'context' is denied* ao limpar `PackageTmp` — restos de `.cursor\context` ou exclusão incompleta de `.cursor`. O `.csproj` define `ExcludeFoldersFromDeployment` + `ExcludeFromPackageFolders` para `.cursor`; se o aviso persistir, apagar `obj\...\Package\PackageTmp` antes de publicar.

---

## HISTÓRICO DE INTERVENÇÕES

> As entradas mais recentes ficam sempre no TOPO desta seção.

---

### [2026-05-14] — `_Navbar`: menu utilizador portal só «Sair» + deteção por role
**Tipo:** Correção
**Arquivos tocados:**
- `Views/Shared/_Navbar.cshtml` — `isPortalClienteNav` via `CustomPrincipal.IsInRole("gc_PortalCliente_PortalFinanceiro")`; ramo portal com única opção «Sair do Sistema»

**Problema / Demanda:**
Perfil portal ainda via Ambiente/Empresa/Filial/Perfil/Versão no dropdown; deve ver **apenas** «Sair do Sistema».

**O que foi feito:**
- Deteção alinhada ao `[CustomAuthorize(Roles = "gc_PortalCliente_PortalFinanceiro")]` da área `crm`. Conteúdo do ramo portal reduzido a um único `<li>`.

---

### [2026-05-14] — `_Navbar`: portal (menu utilizador + sidebar) via `IdCliente`; menu lateral Pedidos
**Tipo:** Correção
**Arquivos tocados:**
- `Views/Shared/_Navbar.cshtml` — `isPortalClienteNav` (`Model.userIdentity.IdCliente > 0`); dropdown portal com Ambiente/Empresa/Filial/Perfil/Versão + Sair (sem Trocar Senha/Device)
- `Controllers/UserIdentityController.cs` — `PerfilNome` no login portal; após `allNavbarItemMenu.Clear()` itens sintéticos grupo «Portal do Cliente» + «Pedidos» (`crm`)

**Problema / Demanda:**
`ViewBag.PortalClienteLogin` não existe no `RenderAction` do Navbar → ramo portal do dropdown não aplicado como previsto; com `Clear()` do menu a sidebar ficava sem entradas (só «uma opção» ou vazio).

**O que foi feito:**
- Deteção de portal autenticado pela sessão (`IdCliente`), alinhada ao `CompletePortalClienteLogin`.
- Menu lateral mínimo para voltar a `Pedidos/Index` na área `crm`.

---

### [2026-05-14] — Portal cliente título: CS0103 em `_ViewStart` (`ViewBag` indisponível)
**Tipo:** Correção
**Arquivos tocados:**
- `Areas/crm/Views/_ViewStart.cshtml` — removido `ViewBag` (StartPage não expõe `ViewBag` em runtime/compilação)
- `Areas/crm/Views/Pedidos/Index.cshtml` — `ViewBag.PortalClienteBrowserTitle = true` no topo da view (WebViewPage)

**Problema / Demanda:**
Produção: `CS0103: The name 'ViewBag' does not exist in the current context` em `Areas/crm/Views/_ViewStart.cshtml`.

**O que foi feito:**
- `_ViewStart` só define `Layout`. O sinal para `<title>` do portal fica em `Pedidos/Index.cshtml` (executa antes do `_Layout` e tem `ViewBag`). Novas views `crm` com `_Layout` devem repetir a linha ou definir no respetivo controller.

---

### [2026-05-14] — Portal cliente: `<title>GDI - Portal do Cliente` (área `crm` + login portal)
**Tipo:** Implementação
**Arquivos tocados:**
- `Views/Shared/_Layout.cshtml` — `<title>` condicional com `ViewBag.PortalClienteBrowserTitle`
- `Areas/crm/Views/Pedidos/Index.cshtml` — `ViewBag.PortalClienteBrowserTitle = true` (após correção CS0103; não usar `ViewBag` em `_ViewStart`)
- `Views/UserIdentity/Index.cshtml` — título do documento quando `portalClienteLogin`
- `Areas/crm/Views/Pedidos/BoletoPdfFebraban.cshtml` — `<title>` (view `Layout = null`)

**Problema / Demanda:**
No perfil portal, o título do browser era o mesmo do staff (`aeroflightx.com - versão`); deve ser `GDI - Portal do Cliente` só nas páginas desse perfil, sem alteração global desnecessária.

**O que foi feito:**
- `_Layout` altera apenas a linha do `<title>` quando `ViewBag.PortalClienteBrowserTitle` está ativo na view que corre antes do layout.
- Login portal (`UserIdentity/Index` com `ViewBag.PortalClienteLogin`) e PDF boleto (`Layout = null`) com título explícito.

**O que foi evitado e por quê:**
- Duplicar `_Layout` inteiro só para o portal.

---

### [2026-05-14] — Publish Web: target `GdiStripPackageTmpDotCursorBeforeWppCopy` (aviso `context` / RemoveEmptyDirectories)
**Tipo:** Correção
**Arquivos tocados:**
- `GDI-ERP-Plataform.csproj` — target MSBuild `BeforeTargets="CopyAllFilesToSingleFolderForMsdeploy;CopyAllFilesToSingleFolderForPackage"` (Windows): remove `$(WPPAllFilesInSingleFolder)\.cursor` com `attrib` + `rmdir`

**Problema / Demanda:**
Aviso persistente `Microsoft.Web.Publishing.targets(2693,5)` — *Access to the path 'context' is denied* em `RemoveEmptyDirectories`, mesmo com `ExcludeFoldersFromDeployment` / `ExcludeFromPackageFolders` para `.cursor`.

**O que foi feito:**
- Restos em `obj\...\PackageTmp\.cursor` (ex. `context` só leitura ou bloqueado pelo IDE) impedem apagar pastas vazias. O target corre **antes** da cópia WPP e força remoção recursiva da pasta `.cursor` no `PackageTmp` em `Windows_NT`.

**O que foi evitado e por quê:**
- Não alterar targets Microsoft; extensão só no `.csproj` do projeto.

**Atenção para próximas intervenções:**
- Build/publish em não-Windows: o target não corre (`OS` ≠ `Windows_NT`); manter limpeza manual de `PackageTmp` se necessário.

---

### [2026-05-14] — Análise: timeout / logout → login portal vs staff (`UserIdentity/Index`)
**Tipo:** Análise
**Arquivos tocados:**
- (nenhum — apenas verificação de código)

**Problema / Demanda:**
Confirmar se, em timeout de sessão, utilizador no **contexto portal do cliente** volta ao **login de portal** e não ao login staff.

**O que foi verificado:**
- `CustomAuthorizeAttribute` (não-Ajax) e `UserIdentityController.Logout` (inatividade via `sessionInactivity.js`) redirecionam para **`UserIdentity/Index`** no **mesmo host** da requisição.
- O GET `UserIdentityController.Index()` define `ViewBag.PortalClienteLogin = IsPortalClienteHost(GetHostWithoutPort(Request))`, logo em FQDN reconhecido (ex.: `*.portalflightx.com`, regras `portalflightx`/`.local`) a view `Views/UserIdentity/Index.cshtml` já apresenta o fluxo de portal.
- AJAX 401: `gdi-session-handler.js` usa `window.GDI_LoginUrl` (`_Layout`: `Url.Action("Index","UserIdentity")`) — URL relativa ao origin atual; mesmo comportamento.
- **Limite:** se o portal for acedido por host **não** coberto por `IsPortalClienteHost` (ex.: `localhost` puro, conforme decisão já registada), o login continua o de staff — por desenho. `X-Forwarded-Host` não está tratado no código; atrás de proxy que altere `Host`, validar cabeçalhos no IIS.

---

### [2026-05-14] — CRM `Pedidos/Index`: callout verde + UI alinhada AdminLTE 4 / área `g`
**Tipo:** Correção + Refatoração (UI)
**Arquivos tocados:**
- `Areas/crm/Views/Pedidos/Index.cshtml`

**Problema / Demanda:**
`callout callout-success` com `h-100` pintava todo o painel com fundo verde (AdminLTE 4 aplica `--lte-callout-bg` em todo o bloco). Modernizar cabeçalhos, cartões e botões ao padrão já usado (ex.: `PortalFinanceiro`).

**O que foi feito:**
- Substituídos callouts por `card card-outline card-secondary` (dados) e `card card-outline card-primary` (documentos): destaque só na borda superior, corpo neutro.
- Removidos estilos inline `#EAEDED` / `#aeb6bf`; uso de `bg-body-secondary`, `bg-body-tertiary`, `text-secondary`, `text-muted`, `border`, `shadow-sm`, espaçamento `g-*` / `mb-3`.
- Botões: `btn-outline-secondary` + `d-inline-flex align-items-center gap-2` para NF/XML; `btn-outline-info` para boletos; `btn-outline-secondary` no Sair (antes `btn-info`).

**O que foi evitado e por quê:**
- Manter `callout-success` com overrides CSS locais — preferível usar componentes AdminLTE 4 já previstos (`card-outline`).

**Impactos conhecidos:**
- Apenas visual/semântica de classes; JS e rotas inalterados.

---

### [2026-05-14] — Publish Web: excluir pasta `.cursor` do pacote (aviso `context` / RemoveEmptyDirectories)
**Tipo:** Correção
**Arquivos tocados:**
- `GDI-ERP-Plataform.csproj` — `ExcludeFoldersFromDeployment` = `.cursor`; `ItemGroup` `ExcludeFromPackageFolders` para `.cursor`

**Problema / Demanda:**
Aviso em `Microsoft.Web.Publishing.targets(2693,5)`: *Access to the path 'context' is denied* (tarefa `RemoveEmptyDirectories` sobre `PackageTmp`, frequentemente ligado a `.cursor\context`).

**O que foi feito:**
- Exclusão explícita da pasta `.cursor` do pipeline de packaging Web (alinhado a `ProcessItemToExcludeFromDeployment` / `ExcludeFilesFromPackage` nos targets Microsoft), para não copiar nem deixar subpastas problemáticas no `PackageTmp`.

**Atenção para próximas intervenções:**
- Se o aviso continuar após um publish antigo, apagar `obj\<Config>\Package` (ou `PackageTmp`) e voltar a publicar.

---

### [2026-05-14] — Publish Web: `.cursor` em `Content` causava falha ao copiar `PackageTmp\.cursor\rules`
**Tipo:** Correção
**Arquivos tocados:**
- `GDI-ERP-Plataform.csproj` — `.cursor\CHANGELOG-DEV.md`, `.cursor\context\migracao-472-481.md` e regra Cursor passam de `Content` para `None`; `.cursor\rules` substituído por `.cursor\rules\gdi-erp-plataform.mdc`

**Problema / Demanda:**
`Copying file .cursor\rules to obj\Release\Package\PackageTmp\.cursor\rules failed. Access to the path '.cursor\rules' is denied.`

**O que foi feito:**
- Ficheiros só de desenvolvimento não devem ir como `Content` para o pacote IIS. `None` mantém-os no projeto no Visual Studio sem os copiar no publish.

**Atenção para próximas intervenções:**
- Não voltar a marcar `.cursor\**` como `Content` salvo requisito explícito de entrega no site publicado.

---

### [2026-05-14] — Antlr3: `Antlr3 (1).Runtime.dll` em `bin` (FileLoadException 0x80131040)
**Tipo:** Correção
**Arquivos tocados:**
- `GDI-ERP-Plataform.csproj` — target `RemoveMisnamedAntlrDuplicatesFromBin` após `CopyFilesToOutputDirectory`
- `bin\Antlr3 (1).Runtime.dll` / `.pdb` — removidos na cópia de trabalho (cópia duplicada do Explorer)

**Problema / Demanda:**
IIS Express / ASP.NET falha ao iniciar: não carrega `Antlr3 (1).Runtime` — manifesto incompatível com a referência (HRESULT 0x80131040).

**O que foi feito:**
- Causa raiz: ficheiros `Antlr3 (1).Runtime.dll` (e `.pdb`) em `bin` ao lado de `Antlr3.Runtime.dll` (nome típico de “cópia (1)” no Windows). O runtime tenta carregar todos os `.dll` de `bin`; o nome do ficheiro não corresponde ao assembly identity interno.
- Target MSBuild remove padrões `Antlr3.Runtime (*).dll` e `Antlr3 (*).Runtime*.dll` após cada build para evitar recorrência.

**Atenção para próximas intervenções:**
- Se o erro persistir, apagar também a pasta temporária do ASP.NET para este site em `%LOCALAPPDATA%\Temp\Temporary ASP.NET Files\` (subpasta do VS/IIS correspondente).

---

### [2026-05-14] — Unificação portal do cliente: área `crm`, `AcessoPortal`, tenant `portalflightx`
**Tipo:** Implementação
**Arquivos tocados:**
- `Controllers/UserIdentityController.cs` — `AcessoPortal` (GET), `CompletePortalClienteLogin`, `SetTenants` (+ `portalflightx` → `GdiPlataformEntities_gdi_producao`), deteção `IsPortalClienteHost` / `GetHostWithoutPort`, POST `Index` com ramo portal em hosts `*.portalflightx.com` (e `portalflightx`/`.local` para dev)
- `Views/UserIdentity/Index.cshtml` — formulário portal (código + CPF/CNPJ) vs staff; `LibMessage*` no portal
- `Models/UserIdentity.cs` — `IdCliente`, `ClienteIdentificador`, `ClienteCpfCnpj`
- `Security/CustomPrincipal.cs` — `Roles` nulo não quebra `IsInRole`
- `Areas/crm/**` — `crmAreaRegistration`, `PedidosController`, `GlobalController`, models `cstListaPedidosPortal` / `cstDadosPedidoPortal`, views `Pedidos/Index`, `Pedidos/BoletoPdfFebraban` (modelo `Areas.g.Models.cstFinanceiroBoletos`), `Views/web.config`, `_ViewStart`
- `GDI-ERP-Plataform.csproj` — `Compile` + `Content` da área `crm`

**Problema / Demanda:**
Unificar o portal do cliente no monólito ERP (URLs públicas `UserIdentity/AcessoPortal`, `/crm/Pedidos/Index`, AJAX boleto, download), modernizar UI/JS ao padrão AdminLTE 4 + BS5 + `start.js`, e mapear tenant `portalflightx` com as mesmas connection names usadas no repositório Portal.

**O que foi feito:**
- Área MVC `crm` com paridade de rotas (`crm/{controller}/{action}`), autorização `[CustomAuthorize(Roles = "gc_PortalCliente_PortalFinanceiro")]` em `Pedidos` e `Global`.
- `AjaxFinanceiroBoletoGCPDF` e PDF via Rotativa alinhados ao código do Portal; modelo de boleto reutiliza `GdiPlataform.Areas.g.Models.cstFinanceiroBoletos` e `LibFinanceiroBoletos` em `Areas.g.Lib`.
- `GlobalController.AjaxGetFileProcessamento` com guard de `db` nulo (padrão área `g`).
- Login portal: hosts cujo FQDN termina em `portalflightx.com`, ou contém `portalflightx` e termina em `.local` / host `portalflightx` (IIS Express / hosts file); **não** força login portal em `localhost` puro (mantém login staff local).

**Decisões técnicas relevantes:**
- `homologacao.portalflightx.com` continua a resolver primeiro segmento `homologacao` → `GdiPlataformEntities_gdi_homologacao` (já existente no ERP), alinhado ao DNS unificado staff/portal no mesmo IIS.
- Sessão portal: `IdPerfil = -900`, `TokenAcesso` = `C{idCliente}`, role `gc_PortalCliente_PortalFinanceiro`, `GoogleTag` / `GoogleTagURL` corretos (corrige dupla atribuição ao campo `GoogleTag` do Portal legado).

**O que foi evitado e por quê:**
- Não alterar `MovimentosController` (link `LinkPortalDireto` já aponta para `UserIdentity/AcessoPortal`); não duplicar `cstFinanceiroBoletos` na área `crm`.

**Impactos conhecidos:**
- Staff em `aeroflightx` / `localhost` inalterado; build Debug OK.

**Atenção para próximas intervenções:**
- **Segurança:** `AcessoPortal` e validações por CPF/CNPJ + pedidos continuam passíveis de enumeração; rate limit fora de escopo.
- Publicar: confirmar bindings IIS para `portalflightx.com` e `homologacao.portalflightx.com` na mesma app; incrementar versão se alterar `start.js` (não alterado nesta entrega).

---

### [2026-05-14] — Regras Cursor: relação ERP ↔ Portal + manutenção do `CLAUDE.md`
**Tipo:** Implementação
**Arquivos tocados:**
- `.cursor/rules/gdi-erp-plataform.mdc`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Alinhar o ficheiro de regras do ERP ao trabalho já feito no repositório **GDI-PortalCliente-Plataform** (`.cursor/rules`): caminhos absolutos dos dois produtos, separação estrita de stack por repo, e instruções de atualização do `CLAUDE.md` equivalentes às do Portal.

**O que foi feito:**
- Nova secção **§0** com tabela de pastas (`C:\Marcio\Projetos\GDI-ERP-Plataform` vs `GDI-PortalCliente-Plataform`), aviso de não misturar tecnologias, onde vive cada `.mdc`/`CLAUDE.md`, manutenção sem drift e tabela de aplicabilidade (o que nas regras do Portal **não** vale para o ERP).
- **§2** alargada com ponto **4** sobre manutenção do `CLAUDE.md` (memória longa, `#` no Claude Code, não copiar secções longas do Portal; CHANGELOG para intervenções pontuais).
- **§8** com remessa explícita à stack do Portal apenas quando o trabalho for na pasta do Portal.
- `description` do frontmatter atualizada para mencionar a relação com o Portal.

**Decisões técnicas relevantes:**
- Espelhar simetria conceitual ao ficheiro `gdi-erp-plataform.mdc` **dentro** do repo Portal (contexto cruzado), sem duplicar listas técnicas do Portal no ERP.

**O que foi evitado e por quê:**
- Não editar ficheiros no diretório do Portal a partir desta intervenção → fora do escopo e da política de workspace do ERP.

**Impactos conhecidos:**
- Agentes em workspace só-ERP ficam com caminho canónico do irmão e regra clara de não importar stack do Portal.

**Atenção para próximas intervenções:**
- Se a política comum (CHANGELOG / ambiente) mudar num produto, replicar manualmente no outro conforme §0.1.

---

### [2026-05-14] — Reestruturação de `.cursor/rules/gdi-erp-plataform.mdc` (frontmatter + deduplicação)
**Tipo:** Refatoração
**Arquivos tocados:**
- `.cursor/rules/gdi-erp-plataform.mdc`

**Problema / Demanda:**
O ficheiro de regras do Cursor não seguia o formato `.mdc` com frontmatter YAML; repetia instruções (CHANGELOG, metodologia); o nome do repositório na restrição de workspace estava incorreto (`GDI-PortalCliente-Plataform`).

**O que foi feito:**
- Adicionado frontmatter (`description`, `alwaysApply: true`).
- Reorganizado em secções numeradas: identidade, ordem de leitura, restrições, metodologia, registo CHANGELOG, formato de resposta, lembretes, limites da regra.
- Corrigido o nome do repositório nas restrições de workspace; tabela única para git/deploy/infra.
- Reduzida duplicação com `CLAUDE.md` (referência explícita em vez de repetir fases DataTables); checklist longo de publish fundido na metodologia §4.5.
- Secção 6 alinhada a `CLAUDE.md` (tabela de ordem das respostas).

**Decisões técnicas relevantes:**
- Manter `alwaysApply: true` para regra global do monorepo ERP.
- Não duplicar template completo do CHANGELOG no `.mdc` — remeter ao formato já documentado no próprio `CHANGELOG-DEV.md`.

**O que foi evitado e por quê:**
- Não copiar para o `.mdc` listas extensas de fases/controllers já em `CLAUDE.md` → tokens e drift entre ficheiros.

**Impactos conhecidos:**
- Agentes passam a carregar metadados da regra no picker do Cursor; conteúdo mais curto favorece aderência.

**Atenção para próximas intervenções:**
- Se `CLAUDE.md` e este `.mdc` divergirem, atualizar ambos ou concentrar detalhe só num deles e manter o outro como índice.

---

### [2026-05-13] — S3: atualização de credenciais em `aws-s3.local.json` (gitignored)
**Tipo:** Implementação
**Arquivos tocados:**
- `App_Data/Secrets/aws-s3.local.json` (gitignored; não versionado)

**Problema / Demanda:**
Atualizar access key / secret access key do IAM S3 no ficheiro local de credenciais.

**O que foi feito:**
- Preenchido `aws-s3.local.json` com as novas chaves; região e buckets mantidos (`sa-east-1`, `bucket-erp-gdi`, `bucket-gdi-public-files`).

**O que foi evitado e por quê:**
- Não alterar `aws-s3.local.json.example` (continua só com placeholders no repositório).

---

### [2026-05-13] — SES: um ficheiro de runtime + modelo `aws-ses-smtp.template.json` (remove `.local.json.example`)
**Tipo:** Refatoração
**Arquivos tocados:**
- `App_Data/Secrets/aws-ses-smtp.template.json` (novo, versionado, sem segredos)
- Removido `App_Data/Secrets/aws-ses-smtp.local.json.example` (confundia com o ficheiro gitignored)
- `GDI-ERP-Plataform.csproj`, `.gitignore`, `Robos/Aws/GdiAwsSesSmtpCredentials.cs`, `.cursor/CHANGELOG-DEV.md` (referências históricas alinhadas)

**Problema / Demanda:**
Dois nomes SES pareciam ambos “credenciais”; clarificar: só `aws-ses-smtp.local.json` tem segredos em runtime.

**O que foi feito:**
- Modelo renomeado para **`aws-ses-smtp.template.json`**; runtime continua **`aws-ses-smtp.local.json`** (gitignored).

---

### [2026-05-13] — SES SMTP: credenciais locais + região São Paulo (`sa-east-1`)
**Tipo:** Implementação
**Arquivos tocados:**
- `Robos/Aws/GdiAwsSesSmtpCredentials.cs` — constante `AwsSesSmtpRegionSaoPaulo` = `sa-east-1`; `ResolveSmtpHost`; env opcional `AWS_SES_SMTP_REGION`; campo JSON opcional `SmtpRegion`
- `App_Data/Secrets/aws-ses-smtp.local.json` (gitignored, runtime)

**Problema / Demanda:**
Credenciais SES e região São Paulo; endpoint omissão `email-smtp.sa-east-1.amazonaws.com`.

**O que foi feito:**
- Resolução via variáveis de ambiente ou JSON local; modelo versionado posteriormente renomeado para `aws-ses-smtp.template.json` (ver entrada acima).

**Atenção para próximas intervenções:**
- Se a conta SES estiver noutra região, definir `SmtpHost` ou `SmtpRegion` / `AWS_SES_SMTP_REGION` em conformidade.

---

### [2026-05-13] — S3: regras por bucket (ERP vs público) centralizadas e aplicadas no GED/BotAws
**Tipo:** Implementação
**Arquivos tocados:**
- `Robos/Aws/GdiAwsS3BucketRules.cs` (novo) — lista branca de buckets (`ResolveBucketErp` / `ResolveBucketPublicFiles`); GED privado não grava no bucket público; validação de `public_url`; consistência `ged_arquivos.bucket` vs `ged_arquivos_tipos.bucket_s3`
- `Robos/Aws/BotAwsS3.cs` — `ValidateGedUpload` no upload; `ThrowIfBucketNotAllowed` em `BuildPublicObjectUrl`
- `Areas/g/Controllers/GedController.cs` — downloads contrato/arquivo: validações antes de presign / devolução de URL pública
- `Areas/qa/Controllers/TreinamentosController.cs` — presign LMS só após validação do bucket ERP
- `GDI-ERP-Plataform.csproj`

**Problema / Demanda:**
Garantir regras explícitas de leitura/gravação por bucket (`bucket-erp-gdi` vs `bucket-gdi-public-files`).

**O que foi feito:**
- **Gravação:** `BotAwsS3.UploadStreamS3` chama `ValidateGedUpload` — anexo privado (`publicRead == false`) recusa bucket público.
- **Leitura GED:** presigned só com bucket na lista branca; `public_url` só aceita HTTPS virtual-hosted (e variante dualstack) para um dos dois buckets; deteta divergência tipo vs registo.
- **LMS:** bucket de presign validado contra a lista branca (usa `ResolveBucketErp()`).

**O que foi evitado e por quê:**
- Não alterar dezenas de URLs estáticas nas views (bucket público já fixo em HTML); escopo mantém-se no SDK/presign/GED.

**Atenção para próximas intervenções:**
- URLs `public_url` fora do padrão virtual-hosted (ex.: CloudFront) exigem alargar `TryValidateStoredPublicUrl`.

---

### [2026-05-13] — S3: buckets GDI no modelo local + `ResolveBucketErp` / `ResolveBucketPublicFiles`
**Tipo:** Implementação
**Arquivos tocados:**
- `Robos/Aws/GdiAwsS3Credentials.cs` — campos opcionais `BucketErp` / `BucketPublicFiles` no JSON; resolução por `AWS_S3_BUCKET_ERP` / `AWS_S3_BUCKET_PUBLIC`; omissões = `bucket-erp-gdi` / `bucket-gdi-public-files`
- `App_Data/Secrets/aws-s3.local.json.example` — alinhado ao IAM **GDI-User-S3-ERP-AppAndPublicFiles-Access** e buckets oficiais (sem segredos no repo)
- `Areas/qa/Controllers/TreinamentosController.cs` — bucket do presign via `ResolveBucketErp()`

**Problema / Demanda:**
Documentar e centralizar buckets do utilizador S3 dedicado ao ERP e ficheiros públicos, mantendo access/secret só em ficheiro local ou env.

**O que foi evitado e por quê:**
- Não gravar access key / secret no repositório nem no chat em ficheiros versionados.

---

### [2026-05-13] — AWS SES SMTP: credenciais fora do código (`aws-ses-smtp.local.json` + env)
**Tipo:** Implementação
**Arquivos tocados:**
- `Robos/Aws/GdiAwsSesSmtpCredentials.cs` (novo)
- `Robos/Aws/BotAwsEmail.cs` — lê credenciais via resolver; `UseDefaultCredentials = false` para autenticação explícita
- `App_Data/Secrets/aws-ses-smtp.template.json` (modelo versionado; nome final após refactor — antes `.example`)
- `.gitignore` — `App_Data/Secrets/aws-ses-smtp.local.json`
- `Lib/LibEmail.cs` — bloco comentado: removidos segredos em texto claro (referência ao resolver)
- `GDI-ERP-Plataform.csproj`

**Problema / Demanda:**
Credenciais SES SMTP não podem permanecer hardcoded; alinhar ao padrão já usado para S3.

**O que foi feito:**
- **`GdiAwsSesSmtpCredentials.Resolve()`**: `AWS_SES_SMTP_USERNAME` / `AWS_SES_SMTP_PASSWORD` (opcionais `AWS_SES_SMTP_HOST`, `AWS_SES_SMTP_PORT`, `AWS_SES_SMTP_REGION`) ou JSON local **`App_Data/Secrets/aws-ses-smtp.local.json`** com `SmtpHost`, `SmtpPort`, `SmtpUsername`, `SmtpPassword`, `SmtpRegion` opcional.

**Atenção para próximas intervenções:**
- S3 e SES podem ser IAM users distintos: ficheiros **`aws-s3.local.json`** e **`aws-ses-smtp.local.json`** separados.

---

### [2026-05-13] — AWS S3: credenciais fora do código (env + `aws-s3.local.json` gitignored)
**Tipo:** Implementação
**Arquivos tocados:**
- `Robos/Aws/GdiAwsS3Credentials.cs` (novo)
- `Robos/Aws/BotAwsS3.cs`
- `Areas/g/Controllers/GedController.cs`
- `Areas/qa/Controllers/TreinamentosController.cs` (mesmas chaves duplicadas no repositório)
- `App_Data/Secrets/aws-s3.local.json.example` (novo, versionado)
- `.gitignore` — `App_Data/Secrets/aws-s3.local.json`
- `GDI-ERP-Plataform.csproj` — compile + Content do `.example`

**Problema / Demanda:**
Chaves AWS hardcoded em ficheiros que não podem ir ao GitHub; solução alinhada ao mercado com ficheiro local.

**O que foi feito:**
- Classe **`GdiAwsS3Credentials`**: lê **`AWS_ACCESS_KEY_ID`** / **`AWS_SECRET_ACCESS_KEY`** (e opcionalmente **`AWS_REGION`** / **`AWS_DEFAULT_REGION`**); se faltarem, lê **`~/App_Data/Secrets/aws-s3.local.json`** (JSON com `AccessKeyId`, `SecretAccessKey`, `Region` opcional; omissão de região → `sa-east-1`).
- **`BotAwsS3`** e **`GedController`**: usam `CreateS3Client()` / `ResolveRegion()`; modelo `.example` no repositório; ficheiro real gitignored.
- **`TreinamentosController`**: removidas constantes duplicadas; mesmo resolvedor (evita secrets no `qa`).

**Decisões técnicas relevantes:**
- Prioridade env → ficheiro local (padrão AWS CLI + secrets locais comuns em ASP.NET).

**O que foi evitado e por quê:**
- Novas dependências NuGet; uso de `Newtonsoft.Json` já referenciado.

**Impactos conhecidos:**
- Dev/IIS: criar `aws-s3.local.json` a partir do `.example` **ou** definir variáveis de ambiente no pool IIS.
- Chaves que já estiveram no Git: **revogar/rodar no IAM** e substituir por novas.

**Atenção para próximas intervenções:**
- Em produção preferir env vars ou perfil IAM na máquina em vez de JSON em disco quando possível.

---

### [2026-05-13] — Select2 lotes em modais (conferência importação, separação pedido, abas + `start.js`)
**Tipo:** Correção
**Arquivos tocados:**
- `Areas/gc/Views/Estoque/ModalConferenciaImportacaoItem.cshtml`
- `Areas/gc/Views/Estoque/ModalConferenciaEstoqueItem.cshtml`
- `Areas/gc/Views/Movimentos/ModalPedidoSeparacaoLotes.cshtml`
- `LibUI_AdminLTE-4.0.0/plugins/startprime/js/start.js`

**Problema / Demanda:**
Mesmo contexto do fix já testado em `ModalConferenciaEstoqueItem`: `ComboEstoqueLotes` + Select2 + abas + modal; replicar e evitar Select2 em painéis ocultos.

**O que foi feito:**
- `ModalConferenciaImportacaoItem`: CSS `overflow-x` no form, mesmos atributos nos 5 `DropDownList`, `gdiConferenciaLotesTabsSelect2Init(..., '#mainModal')` após `jsInitModal`.
- `ModalConferenciaEstoqueItem`: chamada `gdiConferenciaLotesTabsSelect2Init('#ModalConferenciaEstoqueItem', '#mainModal')` após `jsInitModal`.
- `ModalPedidoSeparacaoLotes`: CSS no form, `data_select2_dropdown_parent = "#containerModalPedidoSeparacaoLotes"`, handlers e `gdiConferenciaLotesTabsSelect2Init` com esse parent.
- `start.js`: função `gdiConferenciaLotesTabsSelect2Init` — destrói Select2 nas `.tab-pane` inativas, reinicia no painel ativo; `shown.bs.tab` ligado às âncoras `pill`/`tab`.

**Decisões técnicas relevantes:**
- Evento de aba nas âncoras (não delegação no `<ul>`), pois `shown.bs.tab` pode não propagar.

**O que foi evitado e por quê:**
- Alteração em massa de outros modais com Select2 sem o mesmo combo de lotes/abas.

**Impactos conhecidos:**
- Depende de `gdiDestroySelect2OnCollection` / `gdiInitSelect2OnCollection` (`gdi-select2.js`), já carregados nas telas que usam estes partials.

**Atenção para próximas intervenções:**
- Novo modal com o mesmo padrão de abas de lotes: passar o seletor do form e o `dropdownParent` correto (`#mainModal` vs container dedicado).

---

### [2026-05-13] — Estoque: `ModalConferenciaEstoqueItem` — Select2/modal (dropdown lotes)
**Tipo:** Correção
**Arquivos tocados:**
- `Areas/gc/Views/Estoque/ModalConferenciaEstoqueItem.cshtml`

**Problema / Demanda:**
DropDownList de lotes no modal de conferência não permitia seleção/busca (Select2 + `modal-dialog-scrollable` / overflow), padrão já usado em `ModalPedidoInsertEditItem`.

**O que foi feito:**
- Nos 5 blocos de `DropDownList` de lote: `data-select2-dropdown-parent="#mainModal"`, `jsHandleSelectOpen` / `jsHandleSelectClose` / `onmousedown`, classe `select-modal-fix`, `data-container="body"`.
- CSS `#ModalConferenciaEstoqueItem .modal-body { overflow-x: visible; }`.

**O que foi evitado e por quê:**
- Extensão imediata a importação/separação — feita em entrada seguinte do histórico após validação do padrão.

**Impactos conhecidos:**
- Conteúdo continua a ser carregado em `#mainModal` (`FormRecebimentoItensEstoque`).

---

### [2026-05-13] — Fase 17: Ajax NFe + RoboEnotasNFE, `GetDadosGedAtendimento`, modais com `id_nfe`, export/import/processamento
**Tipo:** Implementação
**Arquivos tocados:**
- `Robos/ENotas/RoboEnotasNFE.cs` — `AtualizarStatusNFPbyMovimentoNFId`, `CancelarNFPbyMovimentoNFId`, `GerarNFServicoByMovimentoNFId`, `AtualizarStatusG_nfePorId`, `CancelarG_nfePorId` (gateway e-Notas para `g_nfe` com `nfe_key` ou vínculo via `gc_movimentos_nf`)
- `Areas/g/Controllers/NfeController.cs` — Ajax: clonar `g_nfe`, cancelar / enviar cancelamento, e-mail unitário (CSV + `g_processamento` alinhado a `FinanceiroFaturamentos`), export período, gerar serviço, atualizar status, sincronizar lote (máx. 200), import arquivo; helpers `ResolverMovimentoNfParaGNfe`, `SincronizarGNfeComMovimentoNf`
- `Areas/g/Views/Nfe/Index.cshtml`, `ModalGerarNfe.cshtml`, `ModalAtualizarStatusNfe.cshtml`, `ModalEnviarCancelamentoNfe.cshtml`, `modalCancelarNfe.cshtml` — seleção única na grade + `id_nfe` nos POSTs
- `Areas/g/Controllers/AtendimentosController.cs` — action **`GetDadosGedAtendimento`** (ex-`GetGedAtendimento`)
- `Areas/g/Views/Atendimentos/Edit.cshtml` — URL DataTables GED
- `CLAUDE.md`, `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Fase 17: acionar Ajax NFe com `RoboEnotasNFE` / padrão de processamento de `FinanceiroFaturamentos`; alinhar nome do endpoint GED em atendimentos ao contrato `GetDados*`; validar publish/deps e plano de migração.

**O que foi feito:**
- **Robô:** wrappers por `id_movimento_nf` (mesmo contexto EF do Robô) e métodos para **atualizar/cancelar** usando `g_nfe.nfe_key` quando não há `gc_movimentos_nf` resolvido pelo financeiro.
- **NfeController:** fluxos Ajax reais com `try/catch` e mensagens; e-mail unitário e export geram arquivo em `_filestemp` + `g_processamento` (tipos **49** export, **50** import lote); import grava cópia do upload e relatório texto (integração XML futura).
- **Atendimentos:** renomeação para **`GetDadosGedAtendimento`** mantendo JSON DataTables.
- **Modais NFe:** `ModalExportarDadosNfePDF` passa `cstExportacaoDadosNFEModel` (corrige modelo da view).

**Decisões técnicas relevantes:**
- Prioridade: `g_financeiro.id_financeiro_movimento` → último `gc_movimentos_nf` do movimento; senão API só com `g_nfe.nfe_key`.
- Lote sincroniza no máximo **200** `id_nfe` mais recentes com `nfe_key` preenchido.

**O que foi evitado e por quê:**
- Não implementar parser XML de import em lote sem especificação — apenas recepção e registro em processamento.

**Impactos conhecidos:**
- `python Scripts/gdi_verify_csproj_gdi_helpers.py` → **0** lacunas.
- `python Scripts/gdi_inventory_datatables_g_area.py` → **32** métodos `GetDados*` (inclui `GetDadosGedAtendimento`).

**Atenção para próximas intervenções:**
- Homologar em ambiente real: `GerarNFServico` exige item de serviço único no movimento; validar `id_processamento_tipo` 49/50 na tabela de tipos se houver FK/restrição.
- **Fase 18 (opcional):** testes manuais e-Notas + ajuste de mensagens/roles `g_PortalVendedor_*`; opcional `param` nulo explícito em `Nfe.GetDados` para heurística do script.

---

### [2026-05-13] — Fase 16: controllers ausentes `Nfe` + `PortalVendedor`, `xhr.dt` logs, `JsonDataTableException` em `Atendimentos`, correção `.csproj` Lib `g`
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/g/Controllers/NfeController.cs` — **novo**: `Index`, `Edit` (GET/POST), `GetDados`, `GetDadosNfeLogs`, `ModalFiltroAvancadoView`, modais NFe, Ajax com resposta JSON explícita (integração e-Notas pendente de gateway)
- `Areas/g/Controllers/PortalVendedorController.cs` — **novo**: `PortalFinanceiro`, `GetDados` (carteira vendedor `IdPerfil == -800`), `JsonDataTableException`
- `Areas/g/Controllers/AtendimentosController.cs` — `JsonDataTableException` privado; `catch` dos endpoints DataTables delegando ao método
- `Areas/g/Views/PortalVendedor/PortalFinanceiro.cshtml` — **`xhr.dt`** + `GdiDtNotifyJsonErrorMessage`
- `Areas/g/Views/Nfe/CreateEdit.cshtml` — **`xhr.dt`** na tabela de logs
- `GDI-ERP-Plataform.csproj` — `<Compile Include>` de **`NfeController`**, **`PortalVendedorController`**; caminhos **`Areas\g\Models\Lib\LibFinanceiro*.cs`** (ficheiros existentes; corrigem build quebrado por paths antigos `Areas\g\Lib\`)
- `CLAUDE.md`, `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Fase 16: eliminar rotas 404 das views **`Nfe`** e **`PortalVendedor`**; alinhar notificação JSON nas grids tocadas; desbloquear compilação MSBuild por referências erradas a `LibFinanceiro`.

**O que foi feito:**
- **`NfeController`**: listagem `g_nfe` com filtros (persistido `getFilterByUser`, avançado 8 campos, genérico), colunas compatíveis com `Index.cshtml` (incl. rótulos **Autorizada** / **Cancelada** e sanitização de vírgulas para `JsGetSelectedRows`); logs `g_nfe_logs`; modais devolvem views existentes; Ajax devolve `success: false` com mensagem até integração com Robô/gateway.
- **`PortalVendedorController`**: `g_financeiro` + `g_clientes` com filtro opcional por vendedor; filtro genérico com `setFilterByUser` alinhado a `g_Financeiro`.
- **`AtendimentosController`**: DRY no erro DataTables.
- **Publish:** `.csproj` atualizado para novos controllers e paths reais de `LibFinanceiro`.

**Decisões técnicas relevantes:**
- Ajax NFe não invoca `RoboEnotasNFE` sem revisão funcional — evita efeitos colaterais em produção; mensagem JSON orienta suporte/faturamento.

**O que foi evitado e por quê:**
- Não duplicar centenas de linhas de integração e-Notas nesta fase.

**Impactos conhecidos:**
- `python Scripts/gdi_verify_csproj_gdi_helpers.py` → **0** lacunas após alteração em `CreateEdit` NFe.
- `python Scripts/gdi_inventory_datatables_g_area.py` → **31** métodos `GetDados*` (inclui `Nfe` + `PortalVendedor` + `GetDadosNfeLogs`); `GetGedAtendimento` fora do padrão de nome.

**Atenção para próximas intervenções:**
- **Fase 17:** ligar Ajax NFe (`AjaxClonarNfe`, cancelamento, e-mail, etc.) a `RoboEnotasNFE` / regras já usadas em `FinanceiroFaturamentos`; revisar roles reais de `g_PortalVendedor_*` em produção; opcional `param` nulo explícito em `Nfe.GetDados` (heurística script).

---

### [2026-05-13] — Fase 15: servidor DataTables — `Produtos`, `CentrosCustos`, `ClassificacaoFinanceira`, `Atendimentos` (atividades/logs/GED)
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/g/Controllers/ProdutosController.cs` — `GetDados`: `param` nulo, `try/catch`, JSON sucesso com `errorMessage`/`stackTrace` vazios, **`JsonDataTableException`**
- `Areas/g/Controllers/CentrosCustosController.cs` — `getDados`: idem + `yesFilterOnOff` quando filtro genérico; nome do pai sem NRE (`Find` nulo)
- `Areas/g/Controllers/ClassificacaoFinanceiraController.cs` — `getDados`: idem + nome do pai sem NRE
- `Areas/g/Controllers/AtendimentosController.cs` — `getDadosAtendimentos` / **`getDadosAtividades`** / **`getDadosAtendimentosLogs`** / **`GetGedAtendimento`**: sucesso com `errorMessage`/`stackTrace` vazios; `yesFilterOnOff` em atividades; NRE evitada (`FirstOrDefault` usuário GED, `Find` operador atividade); logs com `filterOnOff` no catch (**`JsonDataTableException`** consolidado na **Fase 16**).
- `CLAUDE.md`, `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Fase 15 (planejada na Fase 14): fechar o lote `GetDados`/`getDados*` pendente em cadastros e atendimentos, alinhando contrato DataTables no servidor.

**O que foi feito:**
- Padrão Fases 13–14 aplicado onde faltava; **`JsonDataTableException`** em `Produtos`, `CentrosCustos`, `ClassificacaoFinanceira` (métodos `getDados` ainda usados por integrações/API; telas atuais são jsTree).
- `AtendimentosController` — contrato de sucesso alinhado + correções de NRE; método **`JsonDataTableException`** na **Fase 16**.

**Decisões técnicas relevantes:**
- Controllers **`Nfe`** / **`PortalVendedor`** ausentes nesta entrega — entregues na **Fase 16**.

**O que foi evitado e por quê:**
- Não criar **`NfeController`** / **`PortalVendedorController`** sem inventário completo de actions — Fase 16.

**Impactos conhecidos:**
- `python Scripts/gdi_verify_csproj_gdi_helpers.py` → **0** lacunas (sem `.cshtml` novo nesta fase).
- `python Scripts/gdi_inventory_datatables_g_area.py` → **28** métodos `GetDados*` na data da Fase 15.

**Atenção para próximas intervenções:**
- **Fase 16** — ver entrada no topo do histórico.

---

### [2026-05-13] — Fase 14: servidor DataTables — `Areas/g` (lote ampliado) + `Financeiro` consolidados + `Clientes` abas
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/g/Controllers/` — `AssistentesController`, `PagRecCondicoesController`, `PerfisController`, `ContasCaixasController`, `ProdutosTiposController`, `CidadesController`, `ProdutosNcmController`, `UsuariosController`, `RequisicoesController`, `VendedoresController`, `ContratosAviacaoController` (correção tipo contrato + cliente nulos), `GedController`, `FinanceiroLancamentosController`, `FinanceiroFaturamentosController`, `FinanceiroController` (`GetDados`, `getValoresConsolidados`, `GetDadosGrafico`), `ClientesController` (`GetDados` + `GetDadosContatos` + `GetDadosDestinatarios` + `JsonDataTableException`)
- `Areas/g/Views/` — `Perfis/Index`, `ContasCaixas/Index`, `PagRecCondicoes/Index`, `Requisicoes/Index`, `Usuarios/Index`, `FinanceiroFaturamentos/Index`, `Financeiro/DadosConsolidados`, `Clientes/CreateEdit` (destinatários)
- `CLAUDE.md`, `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Fase 14: continuar padronização `GetDados*` / financeiro lista + dados consolidados + abas de cliente.

**O que foi feito:**
- `param` nulo, `try/catch`, JSON sucesso com `errorMessage`/`stackTrace`/`yesFilterOnOff` onde aplicável; **`JsonDataTableException`** por controller (ou compartilhado em `Clientes`); correções de NRE (`Usuarios` perfil, `Requisicoes` tipo/vendedor, `Financeiro` status, `Ged` tipo arquivo + usuário, `Vendedores` revenda, `ContratosAviacao` tipo/cliente); `GetDadosGrafico` com JSON de erro e listas vazias + guard na view.
- `getValoresConsolidados` e `DadosConsolidados` com `xhr.dt` + contrato alinhado.

**Decisões técnicas relevantes:**
- `GetDadosGrafico` não usa shape DataTables; erro devolve as mesmas chaves de séries vazias + `errorMessage` para o `$.ajax` não quebrar o Flot após alerta.

**O que foi evitado e por quê:**
- `ProdutosController`, `AtendimentosController`, `CentrosCustosController`, `ClassificacaoFinanceiraController` — Fase **15**; controllers **`Nfe`** / **`PortalVendedor`** — Fase **16**.

**Impactos conhecidos:**
- `python Scripts/gdi_verify_csproj_gdi_helpers.py` → 0 lacunas; inventário `g` mostra **4** endpoints ainda sem `JsonDataTableException` ao nível de ficheiro (nomes fora do padrão `GetDados*` ou não cobertos).

**Atenção para próximas intervenções:**
- **Fase 15** concluída; **`Nfe`** / **`PortalVendedor`** resolvidos na **Fase 16** (ver topo do histórico).

---

### [2026-05-13] — Fase 13: servidor DataTables — cadastros `Areas/g` (Filiais, UF, PagRecTipos) + inventário `g`
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/g/Controllers/FiliaisController.cs` — `GetDados`: `param` nulo, `try/catch`, sucesso com `errorMessage`/`stackTrace`/`yesFilterOnOff`, **`JsonDataTableException`**; coligada inexistente sem NRE
- `Areas/g/Controllers/UFController.cs` — `GetDados`: idem + sucesso com `errorMessage`/`stackTrace` vazios
- `Areas/g/Controllers/PagRecTiposController.cs` — `GetDados`: idem (`yesFilterOnOff` fixo `"0"`)
- `Areas/g/Views/Filiais/Index.cshtml`, `Areas/g/Views/PagRecTipos/Index.cshtml` — **`xhr.dt`** + `GdiDtNotifyJsonErrorMessage`
- `Scripts/gdi_inventory_datatables_g_area.py` — inventário `GetDados*` em `Areas/g/Controllers`
- `GDI-ERP-Plataform.csproj` — `<None Include>` do script
- `CLAUDE.md`, `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Fase 13: alinhar cadastros `g` ao contrato DataTables (servidor + cliente) e ferramenta de inventário para o restante da área.

**O que foi feito:**
- Três controllers representativos padronizados; views com consumo de `errorMessage` no retorno JSON; script para mapear próximos `GetDados*`.

**Decisões técnicas relevantes:**
- `JsonDataTableException` privado por controller (paridade com `gc`).

**O que foi evitado e por quê:**
- Não varrer todos os `GetDados` de `Areas/g` num único PR (risco de regressão); lote inicial + inventário.

**Impactos conhecidos:**
- `UF/Index` já tinha `xhr.dt`; sem alteração nesta entrega.

**Atenção para próximas intervenções:**
- **Fase 14:** demais controllers em `Areas/g` conforme saída de `gdi_inventory_datatables_g_area.py` (priorizar `ClientesController`, `FinanceiroController`, etc.).

---

### [2026-05-13] — Fase 12: servidor DataTables — `ComexInvoicesController` + `MovimentosEntradasController` + `EstoqueInventarioController` + views
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/gc/Controllers/ComexInvoicesController.cs` — `GetDadosViewImportacao`, `GetDadosViewInvoicesItens`: `param` nulo; JSON com **`yesFilterOnOff`** em sucesso/erro (importação)
- `Areas/gc/Controllers/MovimentosEntradasController.cs` — `GetDadosMovimentosEntradas`: `param` nulo; sucesso e resposta sem linhas com `errorMessage`/`stackTrace` vazios
- `Areas/gc/Controllers/EstoqueInventarioController.cs` — `GetDadosInventario`, `GetDadosInventarioItem`: `param` nulo; inventário inválido com **`severity`**
- `Areas/gc/Views/ComexImportacoes/CreateEdit.cshtml` — aba invoices: `xhr.dt` com `GdiDtNotifyJsonErrorMessage` + atualização condicional dos totais
- `Areas/gc/Views/ComexInvoices/ModalInvoice.cshtml` — `xhr.dt` + `GdiDtNotifyJsonErrorMessage`
- `Areas/gc/Views/EstoqueInventario/FormInventarioItens.cshtml` — `error.dt` + `xhr.dt` + `GdiDtNotifyJsonErrorMessage` + `btnFiltro`
- `CLAUDE.md`, `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Fase 12: fechar lacunas em COMEX invoices (view importação + itens no modal), listagem de entradas NF e inventário — contrato JSON e consumo `xhr.dt`.

**O que foi feito:**
- `MovimentosEntradas/Index` e `EstoqueInventario/Index` já tinham `xhr.dt`; `FormInventarioItens` e `ModalInvoice` completados.

**Decisões técnicas relevantes:**
- Labels de totais na aba invoices do `CreateEdit`: só atualizar quando não há `errorMessage` (evita `undefined` nos elementos).

**O que foi evitado e por quê:**
- Não varrer `Areas/g` (cadastros) nesta fase (escopo Fase 13 ou inventário).

**Impactos conhecidos:**
Erros de servidor nas grelhas citadas passam a SweetAlert2 quando o JSON traz `errorMessage`.

**Atenção para próximas intervenções:**
Fase 13 sugerida: cadastros `Areas/g` (`GetDados` em `ClientesController`, `Produtos*`, etc.) ou script de inventário global.

---

### [2026-05-13] — Fase 11: servidor DataTables — compras (`GetDadosCompras`) + financeiro + estoque + views Gerencial / recebimento itens estoque
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/gc/Controllers/MovimentosComprasController.cs` — `GetDadosCompras`: `param` nulo, `try/catch`, sucesso com `errorMessage`/`stackTrace` vazios; **`JsonDataTableException`**
- `Areas/gc/Controllers/FinanceiroLancamentosController.cs` — `GetDadosLancamentos`, `GetDadosLancamentosByMovimento`: `param` nulo
- `Areas/gc/Controllers/EstoqueController.cs` — `param` nulo e JSON de sucesso alinhado (`errorMessage`/`stackTrace`) em `GetDadosEstoque`, `GetDadosRecebimentoImportacao`, `GetDadosRecebimentoItensImportacao`, `GetDadosRecebimentoEstoque`, `GetDadosRecebimentoItensEstoque`
- `Areas/gc/Views/Estoque/FormRecebimentoItensEstoque.cshtml` — `error.dt` + `xhr.dt` + `GdiDtNotifyJsonErrorMessage`
- `Areas/gc/Views/Gerencial/IndexPainelComercialGerencial.cshtml` — `xhr.dt`: `GdiDtNotifyJsonErrorMessage` antes de `jsUpdateDataView`
- `CLAUDE.md`, `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Fase 11: lacunas da Fase 10 (financeiro/compras/estoque) — `GetDadosCompras` sem contrato de exceção; `param` nulo em endpoints já com try/catch; respostas de sucesso COMEX/recebimento sem `stackTrace` explícito; painel gerencial e form de itens de recebimento sem consumo uniforme de `errorMessage` no `xhr.dt`.

**O que foi feito:**
- `IndexCompras`, `FinanceiroLancamentos/Index` e grelhas Estoque já tinham `xhr.dt` em vários casos — foco em servidor + lacunas de view.

**Decisões técnicas relevantes:**
- `JsonDataTableException` em `MovimentosComprasController` (paridade com Movimentos/COMEX).

**O que foi evitado e por quê:**
- Não duplicar `JsonDataTableException` em `EstoqueController`/`FinanceiroLancamentosController` (já devolvem JSON de erro estruturado).

**Impactos conhecidos:**
Falhas nas grelhas citadas mostram mensagem via SweetAlert2 quando o JSON traz `errorMessage`.

**Atenção para próximas intervenções:**
Fase 13 sugerida: cadastros `Areas/g` (`GetDados*`) ou inventário automatizado global.

---

### [2026-05-13] — Fase 10: servidor DataTables — `GetRelatorioConsultaPedidos` + `ComexImportacoesController` + views COMEX
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/gc/Controllers/MovimentosController.cs` — `GetRelatorioConsultaPedidos`: `param` nulo, `try/catch`, sucesso com `errorMessage`/`stackTrace` vazios
- `Areas/gc/Controllers/ComexImportacoesController.cs` — `GetDados`: `param` nulo; JSON de erro com `yesFilterOnOff`; `GetDadosItensImportacao`, `GetDadosImportacoesLogs`, `GetGedComex`, `GetGedInvoicesComex`: `try/catch`, sucesso com `errorMessage`/`stackTrace` vazios onde aplicável; método privado **`JsonDataTableException`**
- `Areas/gc/Views/ComexImportacoes/CreateEdit.cshtml` — `xhr.dt` + `GdiDtNotifyJsonErrorMessage` (itens, GED, invoices PDF); guard em **`jsDisplayFooterItensImportacao`**
- `Areas/gc/Views/ComexImportacoes/ModalImportacoesLogs.cshtml` — `xhr.dt` + `GdiDtNotifyJsonErrorMessage`
- `CLAUDE.md`, `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Fase 10: fechar lacunas do contrato DataTables em relatório de consulta de pedidos e em grelhas COMEX (importação) ainda sem `try/catch` padronizado ou sem consumo `xhr.dt`.

**O que foi feito:**
- `ModalConsultaPedidos.cshtml` já tinha `xhr.dt` — apenas servidor alinhado.
- `ComexImportacoes/Index` já tratava `GetDados` com `xhr.dt` — reforço no JSON de erro e `param` nulo.

**Decisões técnicas relevantes:**
- `JsonDataTableException` no `ComexImportacoesController` (ficheiro extenso; evita `catch` duplicados).
- Footer da aba itens: não atualizar labels quando `errorMessage` preenchido (evita `undefined` nos campos custom).

**O que foi evitado e por quê:**
- Não varrer todos os controllers `GetDados*` do repositório nesta fase (escopo COMEX + um endpoint Movimentos).

**Impactos conhecidos:**
Erros de SQL/EF nas grelhas citadas passam a SweetAlert2 via `GdiDtNotifyJsonErrorMessage` quando aplicável.

**Atenção para próximas intervenções:**
Fase 13 sugerida: cadastros `Areas/g` (`GetDados*`) ou inventário automatizado global.

---

### [2026-05-13] — Fase 9: servidor DataTables — `MovimentosController` (`GetDados*`) + `xhr.dt` nas views
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/gc/Controllers/MovimentosController.cs` — `GetDadosModalItensComValor`, `GetDadosCartaCorrecao`, `GetDadosPainelPedidos`, `GetDadosInvoicesItensEspelhoDigital`: `param` nulo, `try/catch`, JSON de erro (`errorMessage`, `severity`, `stackTrace`, `yesFilterOnOff`, `sEcho`, `aaData` vazio); sucesso alinhado com `errorMessage`/`stackTrace` vazios onde aplicável; método privado **`JsonDataTableException`**
- `Areas/gc/Views/Movimentos/ModalPedidoEntrega.cshtml`, `ModalPedidoExpedicao.cshtml`, `ModalPedidoAprovacao.cshtml`, `ModalViewCartaCorrecao.cshtml`, `ModalInvoicesItensEspelhoDigital.cshtml` — encadeamento **`xhr.dt`** + `GdiDtNotifyJsonErrorMessage`
- `CLAUDE.md`, `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Fase 9: alargar o contrato de falha DataTables ao módulo comercial **Movimentos** (`gc`), complementando a Fase 8.

**O que foi feito:**
- Quatro actions que ainda não tinham tratamento de exceção estruturado para a grelha.
- `PainelPedidos.cshtml` já tratava `xhr.dt` com `GdiDtNotifyJsonErrorMessage` + `btnFiltro` — sem alteração.

**Decisões técnicas relevantes:**
- Reutilizar o mesmo shape JSON da Fase 5c/8; helper privado no controller (ficheiro muito grande — evita duplicação de quatro blocos `catch` idênticos).

**O que foi evitado e por quê:**
- Não alterar `GetDadosPedidos` / `GetDadosItensPedido` / `GetDadosItensSeparacao` (já com `try/catch`).

**Impactos conhecidos:**
Erros de SQL/EF nas grelhas dos modais e painel passam a mensagem legível via SweetAlert2 quando a view consome `xhr.dt`.

**Atenção para próximas intervenções:**
Fase 13 sugerida: cadastros `Areas/g` (`GetDados*`) ou inventário automatizado global.

---

### [2026-05-13] — Fase 8: servidor DataTables — `try/catch` + JSON `errorMessage` (Atendimentos + GedSGQ)
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/g/Controllers/AtendimentosController.cs` — `getDadosAtividades`: `try/catch`, `param` nulo, JSON de erro com `errorMessage`/`severity`/`stackTrace`/`yesFilterOnOff`/`sEcho`/`aaData` vazio; `GetGedAtendimento`: idem (removidos `filterDb`/`filterAdvanced`/`SentencaSQL` não usados para evitar CS0219)
- `Areas/qa/Controllers/GedSGQController.cs` — `GetDadosDocsSGQ`, `GetDadosPops`, `GetDadosComunicados`, `GetDadosAtasReunioes`: `filterOnOff` + `param` nulo + `try/catch` + método privado `JsonDataTableException`
- `Areas/g/Views/Atendimentos/Edit.cshtml` — `errMode` + `xhr.dt`/`GdiDtNotifyJsonErrorMessage` em **Atividades** e **GED** (paridade com logs)
- `CLAUDE.md` — nota Fase 8 (contrato servidor)
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Fase 8: extensão do contrato JSON de falha aos endpoints DataTables ainda sem `try/catch`, para consumo por `GdiDtNotifyJsonErrorMessage` nas views já preparadas.

**O que foi feito:**
- Padrão alinhado ao `getDadosAtendimentos` / `getDadosAtendimentosLogs` (Fase 5c).
- Qualidade (`qa`): quatro listagens GED SGQ passam a devolver JSON estruturado em exceção (views já tinham `xhr.dt`).

**Decisões técnicas relevantes:**
- `JsonDataTableException` centralizado em `GedSGQController` para evitar quatro blocos `catch` idênticos.

**O que foi evitado e por quê:**
- Não varrer todos os `GetDados*` do `gc`/`g` num único commit (volume e regressão).

**Impactos conhecidos:**
Erros de servidor nas grelhas citadas mostram mensagem via SweetAlert2 em vez de falha genérica / HTML parse.

**Atenção para próximas intervenções:**
Fase 13 sugerida: cadastros `Areas/g` (`GetDados*`) ou inventário automatizado global.

---

### [2026-05-13] — Fase 7: `alert(` nativo em `.cshtml` → `LibMessageError`
**Tipo:** Implementação
**Arquivos tocados:**
- `Scripts/gdi_replace_alert_libmessage.py` — substituição mecânica (regex com `(?<!\.)alert` para não tocar em `GdiSwal2.alert`) dos padrões `+"…"+ err.message` / `e.message` / `err.message` / `err.toString` / `Erro [tag]` + `err.message`
- `Scripts/gdi_dedupe_libmessage_if_else.py` — remoção de `if (typeof LibMessageError === "function") { LibMessageError(...); } else { LibMessageError(...); }` quando ambos os ramos são equivalentes (472 ocorrências em 168 ficheiros)
- ~237 ficheiros `.cshtml` tocados pelo primeiro script; ajustes manuais: `ClassificacaoFinanceira/CreateEdit`, `UserIdentity/Index`, `ModalPedidoNotaFiscal`, `Atendimentos/Index`, `ComexProdutos/Index`, `ProdutosPre`, `Produtos/Index`; `FormPedidoCreate` + `FinanceiroLancamentos/Index` (ramos `else` não idênticos)
- `GDI-ERP-Plataform.csproj` — `<None Include>` para `gdi_replace_alert_libmessage.py` e `gdi_dedupe_libmessage_if_else.py`
- `CLAUDE.md` — nota Fase 7 + scripts
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Fase 7 do plano de modernização UX: eliminar `alert(` nativo nas views em favor de SweetAlert2 via `LibMessageError`, alinhado ao restante do ERP.

**O que foi feito:**
- Script executado uma vez; revisão grep: restam apenas `GdiSwal2.alert` e comentário `// alert(options)`.
- `gdi-session-handler.js` e fallbacks internos em `start.js` mantidos (alert como último recurso).
- Após o primeiro script, correção em massa de `if/else` duplicado com `gdi_dedupe_libmessage_if_else.py`.

**Decisões técnicas relevantes:**
- Não alterar `GdiSwal2.alert({...})` (confirmações com callback).
- `confirm(` nativo: inexistente em `.cshtml` no âmbito pesquisado; `LibMessageConfirm` já usado via `start.js` quando Swal indisponível.

**O que foi evitado e por quê:**
- Não reescrever dezenas de `if (typeof LibMessageError)… else alert` para uma linha onde o script já substituiu o ramo `alert` equivalente em outros ficheiros; casos remanescentes tratados à mão.

**Impactos conhecidos:**
Mensagens de exceção em `catch` passam pelo mesmo estilo visual que o resto do sistema.

**Atenção para próximas intervenções:**
Fase 10 sugerida: continuar `try/catch` + JSON `errorMessage` nos restantes `GetDados*` / COMEX / financeiro ou script de inventário.

---

### [2026-05-13] — Fase 6: documentação + verificação automática views `Gdi*` vs `.csproj`
**Tipo:** Implementação | Análise
**Arquivos tocados:**
- `Scripts/gdi_verify_csproj_gdi_helpers.py` — script Python (regex `Gdi(Ajax|Dt)\w*` em `Areas/` e `Views/` vs `<Content Include="…cshtml" />` no `GDI-ERP-Plataform.csproj`)
- `GDI-ERP-Plataform.csproj` — `<None Include="Scripts\gdi_verify_csproj_gdi_helpers.py" />` (ferramenta de repo; não publicável como recurso do site)
- `CLAUDE.md` — padrões helpers `GdiDt*` / `GdiAjax*`, contrato JSON, ficheiros críticos, armadilhas publish/cache
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Fechar risco de novos `.cshtml` com helpers `GdiAjax*` / `GdiDt*` fora do `<Content Include>` do `.csproj` (publish incompleto) e consolidar documentação da migração de mensagens.

**O que foi feito:**
- Script executável antes do publish; exit code 1 se existir lacuna.
- Execução na máquina de dev: `cshtml_with_GdiAjax_or_GdiDt: 200`, `missing_in_csproj: 0`.

**Decisões técnicas relevantes:**
- Critério de deteção alinhado aos nomes reais dos helpers (`GdiAjax*`, `GdiDt*`).

**O que foi evitado e por quê:**
- Não incluir o `.py` como `<Content>` — evita tratar como ficheiro estático do site; `None` mantém-o no projeto MSBuild.

**Impactos conhecidos:**
Documentação para agentes e humanos; gate manual ou CI possível com o mesmo comando.

**Atenção para próximas intervenções:**
Fase 7 sugerida: substituir `alert(` / `confirm(` nativos por `LibMessageError` / `LibMessageConfirm` (por módulo) ou alargar `try/catch` + `errorMessage` em outros `GetDados*` fora dos já cobertos.

---

### [2026-05-13] — Fase 5b (ampla) + 5c: `GdiAjaxNotifyInconsistencias` global + Atendimentos DataTables servidor
**Tipo:** Implementação
**Arquivos tocados:**
- `LibUI_AdminLTE-4.0.0/plugins/startprime/js/start.js` (`getValidationSummary` → `GdiAjaxNotifyInconsistencias(message)` em vez de `LibMessageAlert` duplicado)
- ~152 ficheiros `.cshtml` em `Areas/**` (+ `Views/**` se existir) com `LibMessageAlert("Verifique as inconsistências", …)` → `GdiAjaxNotifyInconsistencias(…)`; variantes upload/erro com `opcoes.title`; `ModalIncluirLancamentos` título com `!`
- `Areas/g/Controllers/AtendimentosController.cs` — `getDadosAtendimentos` e `getDadosAtendimentosLogs`: `try/catch`, JSON de falha com `errorMessage`, `severity`, `stackTrace`, `yesFilterOnOff`; sucesso de logs inclui `yesFilterOnOff = "0"` para simetria com outras grelhas
- `Areas/g/Views/Atendimentos/Edit.cshtml` — tabela de logs: `errMode` + `xhr.dt` com `GdiDtNotifyJsonErrorMessage(json)`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Extender 5b a todo o projeto e concluir 5c com contrato DataTables nos endpoints de listagem de atendimentos.

**O que foi feito:**
- Script de substituição em massa nos `.cshtml` (padrão + literais "Erro no Upload…" / "Error: Verifique…" + correção `result.msg,)` → `result.msg)`).
- Servidor: exceções passam a JSON consumível por `GdiDtNotifyJsonErrorMessage` na view de logs.

**Decisões técnicas relevantes:**
- Mantidos `LibMessageAlert("Atenção", "Verifique o período…")` e similares (outro título/corpo).

**O que foi evitado e por quê:**
- `LmsEvidenceController` e outros `Json { ok, error }` não-DataTables sem alteração.

**Impactos conhecidos:**
Feedback Ajax unificado para a frase legado "Verifique as inconsistências"; atendimentos lista/logs mostram erro de servidor na grelha quando aplicável.

**Atenção para próximas intervenções:**
Revisão visual em QA; opcional alargar `GdiAjaxNotifyInconsistencias` a títulos totalmente customizados; documentar no `CLAUDE.md` o trio de helpers (`GdiDt*`, `GdiAjax*`).

---

### [2026-05-13] — Fase 5b (piloto): Ajax/modais — `GdiAjaxNotifyInconsistencias` + pasta `g/Financeiro`
**Tipo:** Implementação
**Arquivos tocados:**
- `LibUI_AdminLTE-4.0.0/plugins/startprime/js/start.js` (`GdiAjaxNotifyInconsistencias` — título legado por omissão; `severity` opcional; `LibMessageProcessandoHide` por omissão)
- `Areas/g/Views/Financeiro/Index.cshtml`
- `Areas/g/Views/Financeiro/ModalBaixarTitulos.cshtml`, `ModalBoleto.cshtml`, `ModalCancelarTitulos.cshtml`, `ModalEditarTitulo.cshtml`, `ModalGerarRemessaBoletosBancarios.cshtml`, `ModalNotaDebito.cshtml`, `ModalProrrogarVencimentoTitulo.cshtml`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Iniciar padronização de feedback Ajax fora do DataTables (texto "Verifique as inconsistências" repetido em dezenas de views).

**O que foi feito:**
- Helper único que delega em `LibMessageAlert` ou `LibMessageError` conforme `opcoes.severity`.
- Piloto: substituição mecânica de `LibMessageAlert("Verifique as inconsistências", …)` por `GdiAjaxNotifyInconsistencias(…)` em toda a pasta `Areas/g/Views/Financeiro`.

**Decisões técnicas relevantes:**
- Sem novo pacote NuGet; mesmo bundle que já inclui `start.js`.

**O que foi evitado e por quê:**
- Não varrer todas as áreas num único commit (risco de revisão); restantes `gc`/`qa`/outros módulos `g` ficam para extensões da 5b.

**Impactos conhecidos:**
Telas financeiras `g` listadas: mesmo texto e ícone de aviso que antes; possível `LibMessageProcessandoHide` extra em caminhos que já escondiam processando (efeito colateral benigno).

**Atenção para próximas intervenções:**
Estender `GdiAjaxNotifyInconsistencias` às outras pastas (`gc`, `qa`, restantes `g`); opcional Fase 5c (`getDadosAtendimentos` com try/catch + JSON).

---

### [2026-05-13] — Fase 5a: `Atendimentos/Index` — `error.dt` + `errMode` (paridade Fase 2)
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/g/Views/Atendimentos/Index.cshtml`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
A listagem de atendimentos era a única em `g` com DataTables sem `error.dt` e sem `$.fn.dataTable.ext.errMode = 'none'`, apesar de já ter `ajax.error` com `GdiDtNotifyLoadFailure`.

**O que foi feito:**
- Antes do `.DataTable({`: `errMode = 'none'` e `.on('error.dt', … GdiDtNotifyLoadFailure(message))` encadeado antes do `xhr.dt` existente.

**Decisões técnicas relevantes:**
- Alinhamento ao padrão de `Assistentes/Index` e restantes índices `g`.

**O que foi evitado e por quê:**
- Não alterar `getDadosAtendimentos` (sem `try/catch` explícito; falhas seguem pelo pipeline Ajax / erro DataTables).

**Impactos conhecidos:**
Erros de parsing/rendering da grelha passam a notificar via o mesmo helper que as outras listagens.

**Atenção para próximas intervenções:**
Fase 5b: padronização de modais/Ajax (`LibMessageAlert` genérico); opcional endurecimento de `getDadosAtendimentos` com `try/catch` + JSON `errorMessage` alinhado à Fase 4.

---

### [2026-05-13] — Fase 4: JSON DataTables — `error` → `errorMessage` + `severity` (servidor)
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/gc/Controllers/EstoqueController.cs` (`GetDados` recebimento importação lista, `GetDadosRecebimentoItensImportacao`, `GetDadosRecebimentoItensEstoque` / índice associado: catches e validações com `aaData`)
- `Areas/gc/Controllers/MovimentosEntradasController.cs` (`GetDados` — catches)
- `Areas/g/Controllers/ClientesController.cs` (guard `db == null` em `GetDados`)
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Payloads DataTables ainda expunham a propriedade `error`, incompatível com `GdiDtNotifyJsonErrorMessage` (que lê `errorMessage`).

**O que foi feito:**
- Substituição por `errorMessage`, `severity = "error"` e `stackTrace` quando há exceção em `catch` (`e.ToString()`); validações sem exceção sem `stackTrace` ou com `stackTrace = ""` no ramo agregado pós-catch onde a exceção não está em scope.
- Retorno antecipado `db == null` em `ClientesController.GetDados` alinhado ao mesmo contrato.

**Decisões técnicas relevantes:**
- `Areas/qa/Controllers/LmsEvidenceController.cs` mantém `ok`/`error` — contrato Ajax não-DataTables, fora deste passo.

**O que foi evitado e por quê:**
- Não alterar todos os `Json` da solução: apenas actions identificadas com `aaData` + `error`.

**Impactos conhecidos:**
Grelhas de recebimento importação/estoque e entradas de NF: mensagens de falha passam pelo helper no cliente com ícone de erro.

**Atenção para próximas intervenções:**
Paridade Fase 2 em `g/Atendimentos/Index`; padronização de modais/Ajax (`LibMessageAlert` genérico); outros `Json` legados fora de DataTables.

---

### [2026-05-13] — Fase 3b: ordem `xhr.dt` — `GdiDtNotifyJsonErrorMessage` antes de `btnFiltro` (`gc`)
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/gc/Views/Estoque/FormRecebimentoItensImportacao.cshtml`
- `Areas/gc/Views/CfopOperacoes/Index.cshtml`
- `Areas/gc/Views/ComexProdutos/Index.cshtml`
- `Areas/gc/Views/MovimentosEntradas/Index.cshtml`
- `Areas/gc/Views/Movimentos/IndexEstoque.cshtml`
- `Areas/gc/Views/EstoqueInventario/Index.cshtml`
- `Areas/gc/Views/Estoque/IndexRecebimentoImportacao.cshtml`
- `Areas/gc/Views/Estoque/IndexRecebimentoEstoque.cshtml`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Alinhar à convenção do plano: no `xhr.dt`, notificar `errorMessage` antes de atualizar o botão de filtro.

**O que foi feito:**
- Troca mecânica `{ btnFiltro(...); GdiDtNotifyJsonErrorMessage(...) }` → `{ GdiDtNotifyJsonErrorMessage(...); btnFiltro(...) }` nos 8 ficheiros.

**Decisões técnicas relevantes:**
- `ComexFinanceiro/Index.cshtml` mantém `if (GdiDtNotifyJsonErrorMessage(json)) { } else if (...) { btnFiltro(...) }` — não alterado (padrão condicional distinto).

**O que foi evitado e por quê:**
- Escopo limitado a `Areas/gc/Views`; sem mudanças em controllers ou `start.js`.

**Impactos conhecidos:**
Recebimento, inventário, entradas, estoque em movimentos, CFOP operações e listagem COMEX produtos: ordem igual às restantes grelhas já padronizadas.

**Atenção para próximas intervenções:**
Fase 4 (servidor DataTables) concluída para os pontos mapeados — ver entrada acima. Seguinte: `Atendimentos/Index` (`error.dt`) e/ou modais Ajax.

---

### [2026-05-13] — Fase 3 (fecho): `xhr.dt` → `GdiDtNotifyJsonErrorMessage` nas lacunas `gc`
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/gc/Views/Fretes/Index.cshtml`
- `Areas/gc/Views/CfopParametros/Index.cshtml`
- `Areas/gc/Views/FinanceiroParametroDifal/Index.cshtml`
- `Areas/gc/Views/FinanceiroLancamentos/ModalViewFinanceiroMovimentos.cshtml`
- `Areas/gc/Views/MovimentosCompras/IndexCompras.cshtml`
- `Areas/gc/Views/Movimentos/ModalConsultaPedidos.cshtml`
- `Areas/gc/Views/ComexProdutos/ProdutosPre.cshtml`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Completar o padrão Fase 3 nas grelhas `gc` que ainda só chamavam `btnFiltro` no `xhr.dt`.

**O que foi feito:**
- Handler unificado: `GdiDtNotifyJsonErrorMessage(json);` antes de `btnFiltro(json.yesFilterOnOff)` (inclui `ProdutosPre` com cadeia `.css(...).DataTable`).

**Decisões técnicas relevantes:**
- Mesma assinatura de função já usada noutras views; sem alteração de bundles ou `start.js`.

**O que foi evitado e por quê:**
- Na mesma data, a reordenação `GdiDtNotify` → `btnFiltro` nos ficheiros que ainda invertiam a ordem ficou registada em **Fase 3b** (entrada imediatamente acima no histórico).

**Impactos conhecidos:**
Fretes, CFOP parâmetros, DIFAL, modal de lançamentos por movimento, compras, modal histórico de pedidos e pré-listagem COMEX produtos passam a exibir mensagem de `errorMessage` no payload DataTables quando existir.

**Atenção para próximas intervenções:**
Auditoria servidor `error` vs `errorMessage` em actions DataTables (Fase 4).

---

### [2026-05-13] — Fase 3 (extensão): `xhr.dt` → `GdiDtNotifyJsonErrorMessage` + contrato JSON (áreas `g` e `qa`)
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/g/Views/**` (16 `.cshtml`: todas as grelhas com `xhr.dt` + `btnFiltro` passam a chamar `GdiDtNotifyJsonErrorMessage(json)` antes do filtro)
- `Areas/qa/Views/GedSGQ/IndexAtasReunioes.cshtml`, `IndexComunicados.cshtml`, `IndexDocsSGQ.cshtml` (mesmo padrão)
- `Areas/g/Controllers/ClientesController.cs` (`GetDados` / catch: `error` → `errorMessage` + `severity` + `stackTrace`)
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Replicar a Fase 3 nas áreas `g` e `qa` onde só existia `btnFiltro` no `xhr.dt`, e alinhar o JSON de falha do DataTables em `ClientesController` ao helper (`errorMessage` em vez de `error`).

**O que foi feito:**
- Views `g` e `qa` (GED SGQ): `GdiDtNotifyJsonErrorMessage(json)` no início do handler `xhr.dt`, antes de `btnFiltro`, sem alterar `error.dt` / `ajax.error`.
- `ClientesController`: no `catch` de `GetDados`, resposta JSON com `errorMessage`, `severity = "error"`, `stackTrace = e.ToString()` e demais campos DataTables inalterados.

**Decisões técnicas relevantes:**
- Manter `stackTrace` no JSON como em outros controllers gc para `console.error` no helper; em produção pode ser endurecido depois com log só no servidor.

**O que foi evitado e por quê:**
- Não acrescentar `error.dt` em `Atendimentos/Index` (já tinha só `xhr.dt`); escopo limitado ao padrão Fase 3 acordado.

**Impactos conhecidos:**
Listagens g (clientes, produtos, NCM, financeiro, NFe, GED, etc.) e índices GED SGQ em `qa`: se o servidor devolver `errorMessage` preenchido no JSON da grelha, o utilizador vê SweetAlert coerente com `severity`.

**Atenção para próximas intervenções:**
Outros actions em `g` que ainda devolvam apenas `error` no JSON do DataTables não são cobertos pelo helper até alinharem o contrato.

---

### [2026-05-13] — Fase 3 (extensão): `xhr.dt` → `GdiDtNotifyJsonErrorMessage` + `severity` nos JSON de erro (gc)
**Tipo:** Implementação
**Arquivos tocados:**
- ~15 ficheiros `.cshtml` em `Areas/gc/Views` (substituição do bloco `json.errorMessage` + `LibMessageAlert`/`stackTrace` manual pelo helper; `EstoqueLotes/Index` e `Movimentos/PainelPedidos` multilinha à mão por CRLF)
- `Areas/gc/Controllers/ComexProdutosController.cs`, `ComexInvoicesController.cs`, `ComexImportacoesController.cs`, `ComexFinanceiroController.cs`, `EstoqueController.cs` (ramo catch com `stackTrace`), `EstoqueInventarioController.cs`, `FinanceiroLancamentosController.cs` (`GetDadosLancamentosByMovimento`), `MovimentosController.cs` (dois `return Json` com `errorMessage,` + `stackTrace` shorthand)
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Alargar o piloto Fase 3: mesmo tratamento de `errorMessage`/`severity` em todas as grelhas gc identificadas com o padrão legado no `xhr.dt`.

**O que foi feito:**
- Views: `GdiDtNotifyJsonErrorMessage(json)` preservando ordem (`btnFiltro`, `jsUpdateDataView`, labels) onde já existia.
- Controllers: propriedade opcional `severity = "error"` nos `Json` de exceção que já expunham `errorMessage` + `stackTrace` (ou shorthand `errorMessage,` / `stackTrace,`).

**Decisões técnicas relevantes:**
- Omissão de `severity` no servidor mantém ícone de aviso (`LibMessageAlert`) no helper — sem alterar payloads de sucesso.

**O que foi evitado e por quê:**
- Não alterar actions que devolvem `error` em vez de `errorMessage` (ex.: ramos catch em `EstoqueController` já existentes).

**Impactos conhecidos:**
Listagens COMEX, estoque (índices/recebimento), CFOP, faturas, financeiro COMEX, inventário e entradas: mensagem de exceção do servidor pode aparecer como erro (vermelho) quando o JSON inclui `severity`.

**Atenção para próximas intervenções:**
Área `g` (ex.: `ClientesController` com `errorMessage`) e restantes `catch` sem `stackTrace` podem seguir o mesmo critério sob revisão.

---

### [2026-05-13] — Fase 3 (piloto): `GdiDtNotifyJsonErrorMessage` + `severity` opcional no JSON
**Tipo:** Implementação
**Arquivos tocados:**
- `LibUI_AdminLTE-4.0.0/plugins/startprime/js/start.js` (função `GdiDtNotifyJsonErrorMessage` — campo opcional `severity`; omissão mantém `LibMessageAlert` como no legado)
- `Areas/gc/Views/Movimentos/IndexPedido.cshtml` (`xhr.dt` → helper)
- `Areas/gc/Views/FinanceiroLancamentos/Index.cshtml` (`xhr.dt` → helper)
- `Areas/gc/Controllers/MovimentosController.cs` (`GetDadosPedidos`: JSON de exceção com `severity = "error"`)
- `Areas/gc/Controllers/FinanceiroLancamentosController.cs` (`GetDadosLancamentos`: JSON de exceção com `severity = "error"`)
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Fase 3 mínima: mensagens de negócio via `errorMessage` no mesmo payload DataTables, com distinção opcional aviso vs erro, sem alterar campos obrigatórios do contrato (`aaData`, `sEcho`, totais, etc.).

**O que foi feito:**
- Helper `GdiDtNotifyJsonErrorMessage(json)`: se `errorMessage` vazio → no-op; `stackTrace` só em `console.error`; `severity` `error`/`danger`/`err` → `LibMessageError`; caso contrário → `LibMessageAlert` (comportamento legado quando `severity` ausente).
- Piloto em duas telas e respetivos endpoints de listagem: ramo `catch` passa a incluir propriedade opcional `severity = "error"` para falhas de servidor.

**Decisões técnicas relevantes:**
- Respostas de sucesso inalteradas; `severity` só no JSON de erro já existente.

**O que foi evitado e por quê:**
- Propagar a todas as views/controllers num único passo — outras grelhas podem adoptar o helper sem mudar servidor até ser desejado `LibMessageError` via `severity`.

**Impactos conhecidos:**
Em `IndexPedido` e índice de lançamentos financeiros (gc), mensagem de exceção do servidor passa a ícone de erro quando `severity` é `error`.

**Atenção para próximas intervenções:**
Alargar `GdiDtNotifyJsonErrorMessage` às outras `xhr.dt` com `errorMessage`; usar `severity = "warning"` no servidor para avisos não bloqueantes.

---

### [2026-05-13] — Fase 2: `GdiDtNotifyLoadFailure` em ~79 views (`error.dt` / `ajax.error` DataTables)
**Tipo:** Implementação
**Arquivos tocados:**
- ~79 ficheiros `.cshtml` em `Areas/**` (lista gerada por substituição mecânica; ex.: índices `g`/`gc`/`qa`, modais com grelha server-side)
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Alargar a Fase 1: mesma mensagem legada e `LibMessageError` centralizado via `GdiDtNotifyLoadFailure` em todos os handlers `error.dt` e `ajax.error` que usavam `LibMessageAlert`/`LibMessageError` com o texto «Falha ao processar os dados <b>…</b>».

**O que foi feito:**
- Substituição literal dos quatro padrões (`message` / `errorThrown` × Alert/Error) por `GdiDtNotifyLoadFailure(message)` ou `GdiDtNotifyLoadFailure(errorThrown)` sem alterar URLs, `ajax.data`, colunas, `xhr.dt` nem contrato JSON.

**Decisões técnicas relevantes:**
- Exceção intencional: `Areas/g/Views/Financeiro/DadosConsolidados.cshtml` mantém `LibMessageAlert` com variável `url` (padrão diferente; fora dos quatro literais).

**O que foi evitado e por quê:**
- Mudar payloads ou actions; não tocar em `json.errorMessage` nos `xhr.dt` (Fase 3 / confirmação explícita para evolução de API).

**Impactos conhecidos:**
Em falhas de grelha, `LibMessageProcessandoHide` pode ser invocado via helper onde antes só existia alerta (comportamento alinhado ao piloto `FormPedidoCreate`).

**Atenção para próximas intervenções:**
Fase 3: opcional `errorMessage`/`severity` no JSON + extensão do helper, com revisão por tela para avisos não bloqueantes.

---

### [2026-05-13] — Fase 0/1: helper `GdiDtNotifyLoadFailure` + piloto em `FormPedidoCreate`
**Tipo:** Implementação
**Arquivos tocados:**
- `LibUI_AdminLTE-4.0.0/plugins/startprime/js/start.js`
- `Areas/gc/Views/Movimentos/FormPedidoCreate.cshtml`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Centralizar feedback de falha em DataTables (`error.dt` / `ajax.error` da grelha) com `LibMessageError` + `LibMessageProcessandoHide` opcional, sem alterar contrato JSON/URLs/colunas nem avaliação global controller↔DataTables.

**O que foi feito:**
- Nova função global `GdiDtNotifyLoadFailure(detail, opcoes)` em `start.js`: mensagem legada `Falha ao processar os dados <b>…</b>`, título omissão `Atenção`, `hideProcessando` omissão `true`, fallback `alert` se `LibMessageError` ausente.
- Piloto só em `FormPedidoCreate.cshtml`: os quatro `error.dt` e quatro `ajax.error` das tabelas passam a chamar o helper; AJAX de destinatários que tinha `error` vazio passa a `GdiDtNotifyLoadFailure(..., { hideProcessando: false })` (não é grelha — evita `hide` desnecessário).

**Decisões técnicas relevantes:**
- Nenhuma alteração em `ajax.data`, `dataSrc`, actions MVC ou resposta esperada; apenas substituição do corpo dos callbacks de erro já existentes.

**O que foi evitado e por quê:**
- Migração em massa noutras views e mudança de `xhr.dt` / payload — exige confirmação explícita para evolução de API de comunicação.

**Impactos conhecidos:**
Em falha de rede na grelha, `LibMessageProcessandoHide()` passa a ser invocado também via helper (alinhado ao comportamento já documentado para esta página).

**Atenção para próximas intervenções:**
Replicar `GdiDtNotifyLoadFailure` noutros `.cshtml` com `error.dt`/`ajax.error` em PRs pequenos; opcional estender opções (ex. título) sem tocar no servidor.

---

### [2026-05-13] — Remoção do plugin toastr (não utilizado; padrão SweetAlert2)
**Tipo:** Refatoração / Limpeza
**Arquivos tocados:**
- `Views/Shared/_Layout.cshtml`
- `Views/Shared/_Blank.cshtml`
- `Views/UserIdentity/Index.cshtml`
- `Views/UserIdentity/TrocaObrigatoriaSenha.cshtml`
- `Views/UserIdentity/OldTrocaObrigatoriaSenha.cshtml`
- `Views/UserIdentity/BackupIndex.cshtml`
- `GDI-ERP-Plataform.csproj`
- Pasta `LibUI_AdminLTE-4.0.0/plugins/toastr-3/` (ficheiros apagados)
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
O ERP não chamava a API `toastr.*`; o plugin era apenas carregado. Remover referências e ficheiros para alinhar ao padrão SweetAlert2 e reduzir superfície de manutenção.

**O que foi feito:**
- Removidos `<link>` e `<script>` do toastr nos layouts e páginas de identidade.
- Removidas entradas `<Content Include>` do `.csproj`.
- Eliminados `toastr.css`, `toastr.min.css`, `toastr.min.js`, `toastr.js.map` e a pasta `toastr-3`.

**Decisões técnicas relevantes:**
- Nenhum substituto funcional necessário: não havia chamadas a toastr; mensagens continuam via `LibMessage*` / Swal2.

**O que foi evitado e por quê:**
- Introduzir helper novo tipo toast no Swal2 — fora do escopo (só remoção).

**Impactos conhecidos:**
Publish: se existir `PackageTmp` antigo com cópia de `toastr-3`, seguir prática já documentada (limpar `obj` antes de publicar se surgir aviso de ficheiro em falta).

**Atenção para próximas intervenções:**
Se no futuro forem desejadas notificações não modais, usar modo toast do SweetAlert2 ou helper dedicado.

---

### [2026-05-13] — Fase E convergência: `Views` raiz (Error, UserIdentity) — `alert` → `LibMessageError`
**Tipo:** Implementação
**Arquivos tocados:**
- `Views/Error/Index.cshtml`
- `Views/Error/ModalError.cshtml`
- `Views/UserIdentity/Index.cshtml`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Fase E: alinhar mensagens em `catch` ao padrão SweetAlert2 via `LibMessageError` + fallback `alert`, após validar ordem de scripts.

**O que foi feito:**
- `Error/Index` e `Error/ModalError`: mesmo padrão `[jsInitForm]` + `LibMessageError("Atenção", …)` com `else { alert(…) }` (páginas com `_Layout` ou modal carregado no ERP já têm `libui-swal-compat` + `start.js`).
- `UserIdentity/Index`: `catch` de `jsValidarAcesso` com `LibMessageError` para a mensagem de exceção; comentário Razor junto a `@Scripts.Render("~/bundles/libui-swal-compat")` documentando dependência antes de `start.js`.

**Decisões técnicas relevantes:**
- Não alterar `TrocaObrigatoriaSenha` / `BackupIndex` — sem `alert` encontrado.

**O que foi evitado e por quê:**
- Duplicar carregamento de scripts; apenas comentário documental no login.

**Impactos conhecidos:**
Login e ecrã de erro usam o mesmo critério visual que o resto do ERP quando `LibMessageError` está disponível.

**Atenção para próximas intervenções:**
Qualquer nova view “standalone” com `Layout = null` deve incluir jQuery → `libui-swal-compat` → `start.js` antes de `LibMessage*`.

---

### [2026-05-13] — Fase D convergência (hotspots): `alert` → `LibMessageError` + erros DataTables com `LibMessageError`
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/gc/Views/Movimentos/FormPedidoCreate.cshtml`
- `Areas/gc/Views/Movimentos/IndexPedido.cshtml`
- `Areas/gc/Views/FinanceiroLancamentos/Index.cshtml`
- `Areas/g/Views/Clientes/CreateEdit.cshtml`
- `Areas/g/Views/Nfe/Index.cshtml`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Fase D: ficheiros com muitas ocorrências de `alert`; padronizar mensagens de erro em criação/atualização DataTables (SweetAlert2) sem alterar opções/comportamento do DataTables.net.

**O que foi feito:**
- Todos os `alert("[contexto]" + err|e.message…)` e `alert(err.message)` / `alert(err.toString())` nestes cinco ficheiros passaram ao padrão `LibMessageError("Atenção", …)` com fallback `else { alert(…) }`.
- Onde já existia `LibMessageAlert("Atenção", "Falha ao processar os dados …")` em handlers `error.dt` ou `ajax.error` das tabelas, substituído por **`LibMessageError`** (mesmo texto, ícone de erro via helper central — sem mudar assinaturas `.DataTable()` / `.dataTable()` nem callbacks além da chamada de mensagem).

**Decisões técnicas relevantes:**
- Não introduzir `LibMessageSuccess` novo em fluxos que já usavam apenas `LibMessageAlert`/`LibMessageProcessando` em sucesso; escopo limitado a erros visíveis ao utilizador e falhas de rede/tabela.

**O que foi evitado e por quê:**
- Refatoração de `columns`, `ajax`, `drawCallback`, etc.; apenas troca da função de exibição da mensagem.

**Impactos conhecidos:**
Linhas longas nos `catch`; comportamento de dados inalterado.

**Atenção para próximas intervenções:**
Outros índices com >3 `alert` (ex.: `Financeiro/Index`, `MovimentosEntradas/Index`) podem seguir o mesmo padrão numa extensão da Fase D.

---

### [2026-05-13] — Fase B convergência: `alert` em catch → `LibMessageError` (views com 1–3 ocorrências nativas)
**Tipo:** Implementação
**Arquivos tocados:**
- ~161 ficheiros `.cshtml` em `Areas` (contagem nativa `(?<!\.)alert\(` entre 1 e 3 antes da alteração), mais correção pontual em `Areas/g/Views/ClassificacaoFinanceira/CreateEdit.cshtml` (caso `alert("Erro [jsSalvarDados] (" + e + ")")`).
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Fase B do plano: substituir feedback ao utilizador em `catch` por `LibMessageError("Atenção", …)` com fallback `else { alert(…) }` quando `LibMessageError` não existir.

**O que foi feito:**
- Substituição automática dos padrões `alert("[tag]" + err.message.toString());` e equivalente com `e.message`, por bloco `if (typeof LibMessageError === "function") { LibMessageError("Atenção", "[tag]" + (…)); } else { alert(…); }`.
- Regex com lookbehind `(?<!\.)` para **não** alterar `GdiSwal2.alert`.
- Caso excecional em `ClassificacaoFinanceira/CreateEdit.cshtml` convertido manualmente.

**Decisões técnicas relevantes:**
- Mantém-se `alert` apenas no ramo `else` (ambiente sem helper).
- Ficheiros com mais de 3 `alert` nativos por ficheiro ficam para fases seguintes (C/D).

**O que foi evitado e por quê:**
- Alterações em `start.js`, `Views` raiz fora de `Areas`, e views com >3 ocorrências.

**Impactos conhecidos:**
Algumas linhas ficaram longas (if/else numa linha); comportamento funcional inalterado salvo canal visual (Swal).

**Atenção para próximas intervenções:**
Quebrar linhas nos ficheiros mais lidos se a equipa preferir legibilidade; continuar Fase C por área funcional.

---

### [2026-05-13] — Fase A convergência: sessão expirada (AJAX 401) via `LibMessageError` + checklist ordem scripts
**Tipo:** Implementação
**Arquivos tocados:**
- `Scripts/gdi-session-handler.js`
- `Views/Shared/_Layout.cshtml` (comentário de ordem de scripts)
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Executar Fase A do plano de convergência: substituir `alert` de feedback ao utilizador em `gdi-session-handler.js` por `LibMessageError` (SweetAlert2), com fallback nativo se Swal não existir; documentar checklist de ordem de carregamento.

**O que foi feito:**
- `notifySessionExpiredThenRedirect`: usa `LibMessageError('Sessão', msg, { callback: redirectToLogin })` quando `LibMessageError` e `GdiSwalCompat.alert` existem; caso contrário `alert` + redirecionamento.
- Redirecionamento para login **após** fechar o Swal (callback do `GdiSwal2.alert`), evitando redirecionar antes do utilizador ver a mensagem (comportamento diferente do `alert` bloqueante, corrigido de forma explícita).
- Cabeçalho JSDoc no `.js` com checklist: jQuery → bundle `libui-swal-compat` → `start.js` → `gdi-session-handler.js`.
- Comentário Razor em `_Layout.cshtml` junto ao include do script.

**Decisões técnicas relevantes:**
- Não alterar outras views nem outros `alert` do ERP nesta fase.

**O que foi evitado e por quê:**
- Mudança de posição dos `<script>` no layout: a ordem já satisfaz o checklist; apenas documentação.

**Impactos conhecidos:**
Páginas que não carreguem `start.js` / Swal mas incluam este handler isoladamente caem no ramo `alert` (fallback).

**Atenção para próximas intervenções:**
Se `_Blank.cshtml` ou outro layout passar a usar `gdi-session-handler.js`, replicar a mesma ordem de scripts ou o fallback cobre.

---

### [2026-05-13] — Cancelamento NF: `jsCancelamentoNF_Step4` com `Swal.fire` + input (protocolo SIARE)
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/gc/Views/Movimentos/ModalViewNotasFiscais.cshtml`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Alinhar o pedido de protocolo SIARE ao mesmo modelo visual e de validação já usado em `jsCancelamentoNF_Step3` (`Swal.fire`, botões Bootstrap, `target` no modal).

**O que foi feito:**
`jsCancelamentoNF_Step4` deixou de usar `GdiSwal2.prompt` e passou a `Swal.fire` com `title` + `html` para reproduzir as duas linhas de texto originais, `inputPlaceholder`, `inputValidator` (valor obrigatório após trim — aceita `0`), e encadeamento a `jsCancelamentoNF_Step5` com `result.value.trim()` em `isConfirmed`.

**Decisões técnicas relevantes:**
- Validação apenas de não vazio; não restringir a dígitos para não bloquear formatos de protocolo eventualmente alfanuméricos.

**O que foi evitado e por quê:**
- Alterações noutros ficheiros ou no shim `GdiSwal2.prompt`.

**Impactos conhecidos:**
Comportamento equivalente ao `Step3`: cancelar não chama `Step5`; confirmar sem texto mostra mensagem de validação.

**Atenção para próximas intervenções:**
Fluxo de cancelamento NF neste modal fica todo em `Swal.fire` até ao AJAX do `Step5`.

---

### [2026-05-13] — Cancelamento NF: `jsCancelamentoNF_Step3` com `Swal.fire` + input (substitui `GdiSwal2.prompt`)
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/gc/Views/Movimentos/ModalViewNotasFiscais.cshtml`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Modernizar o pedido de justificativa (`GdiSwal2.prompt`) para `Swal.fire` nativo com `input`, botões Bootstrap (`buttonsStyling: false`, `customClass`), `reverseButtons`, `target` no modal visível, validação de campo vazio, mantendo o título original do ERP e o fluxo para `jsCancelamentoNF_Step4`.

**O que foi feito:**
`jsCancelamentoNF_Step3` passou a usar `Swal.fire` com `input: "text"`, `inputPlaceholder`, `inputValidator` (trim), `result.isConfirmed` e `result.value.trim()` antes de chamar `Step4`. `jsCancelamentoNF_Step4` permanece com `GdiSwal2.prompt` (fora do exemplo fornecido).

**Decisões técnicas relevantes:**
- Título mantido literalmente: `"Informe a Justificatica do Cancelamento"` (texto já usado no sistema).
- `target: document.querySelector(".modal.show") || document.body` para empilhar o Swal corretamente sobre o Bootstrap Modal.

**O que foi evitado e por quê:**
- Migração de outros `GdiSwal2.prompt` do projeto e de `Step4` — escopo limitado ao caso definido no pedido.

**Impactos conhecidos:**
Comportamento mais rigoroso que o `prompt` antigo sem `required`: não aceita confirmação com valor em branco (alinhado ao modelo com `inputValidator`).

**Atenção para próximas intervenções:**
`Step4` (protocolo SIARE) pode seguir o mesmo padrão com `title` em `html` se for desejado alinhar todo o fluxo.

---

### [2026-05-13] — Cancelamento NF: confirmações em `Swal.fire` (substitui `GdiSwal2.dialog` de 2 botões)
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/gc/Views/Movimentos/ModalViewNotasFiscais.cshtml`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Modernizar diálogos de confirmação com exatamente dois botões (confirmar / desistir) para o padrão nativo SweetAlert2 (`Swal.fire`), mantendo o texto e a ordem dos botões do modelo acordado, sem alterar o restante ERP.

**O que foi feito:**
`jsCancelamentoNF_Step1` e `jsCancelamentoNF_Step2` passaram a usar `Swal.fire` com `html`, `icon: "question"`, `showCancelButton`, `reverseButtons`, `buttonsStyling: false`, `customClass` com classes Bootstrap e `allowEscapeKey: false` (equivalente a `onEscape: false`). Mensagens equivalentes às anteriores; `</br>` normalizado para `<br>`.

**Decisões técnicas relevantes:**
- Apenas o par de confirmações do fluxo de cancelamento neste modal; `GdiSwal2.prompt` (passos seguintes) mantido.
- `Step2` alinhado ao mesmo modelo que `Step1` para UX consistente no fluxo.

**O que foi evitado e por quê:**
- Migração em massa de todos os `GdiSwal2.dialog` do projeto — fora do escopo “cirúrgico” definido.

**Impactos conhecidos:**
Requer `Swal` global (já carregado com o bundle SweetAlert2 nas páginas que abrem este modal).

**Atenção para próximas intervenções:**
Outros modais com o mesmo padrão de 2 botões podem ser migrados caso a caso com o mesmo template.

---

### [2026-05-13] — SweetAlert2: popup sempre claro (anular dark de `prefers-color-scheme`)
**Tipo:** Correção
**Arquivos tocados:**
- `LibUI_AdminLTE-4.0.0/plugins/sweetalert2/gdi-swal2-overrides.css`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Com OS/browser em modo escuro, o tema oficial `bootstrap-5.min.css` do SweetAlert2 aplicava fundo escuro ao popup; desejável manter apenas o aspeto claro do diálogo, sem mudar JS nem outras funções.

**O que foi feito:**
Em `gdi-swal2-overrides.css` (já carregado após o tema), adicionado `@media (prefers-color-scheme: dark)` que repõe as variáveis CSS alteradas por esse ficheiro para os valores do tema claro `bootstrap-5`.

**Decisões técnicas relevantes:**
- Ajuste só em CSS centralizado; não tocar em `bootstrap-5.min.css` (vendor) nem no shim JS.

**O que foi evitado e por quê:**
- Desativar `prefers-color-scheme` noutras partes do ERP ou alterar tema AdminLTE.

**Impactos conhecidos:**
Utilizadores em dark mode continuam com app escura, mas alertas/confirmações Swal2 com fundo claro.

**Atenção para próximas intervenções:**
Se no futuro se quiser popup escuro alinhado ao OS, remover este bloco ou condicionar a uma classe no `html`.

---

### [2026-05-13] — Padronização: `bootbox` → `GdiSwal2` + ficheiro `gdi-swal2-dialog-shim.js`
**Tipo:** Refatoração
**Arquivos tocados:**
- `LibUI_AdminLTE-4.0.0/plugins/startprime/js/gdi-swal2-dialog-shim.js` (novo; substitui `gdi-swal-bootbox-shim.js`)
- `LibUI_AdminLTE-4.0.0/plugins/startprime/js/start.js` (comentário)
- `App_Start/BundleConfig.cs`
- `GDI-ERP-Plataform.csproj`
- Todas as `.cshtml` / `.js` do repo que invocavam `bootbox.alert|confirm|dialog|hideAll|prompt` (substituídas por `GdiSwal2.*`)
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Identificar o componente como API GDI sobre SweetAlert2, removendo o nome legado `bootbox` em chamadas, globais e nome de ficheiro.

**O que foi feito:**
1. Global **`GdiSwal2`** (métodos `alert`, `confirm`, `prompt`, `dialog`, `hideAll`) + `window.GdiSwal2`; `GdiSwalCompat` delega para `GdiSwal2`.  
2. Renomeado o módulo para **`gdi-swal2-dialog-shim.js`** e atualizado bundle/`.csproj`.  
3. Substituição em massa de **`bootbox.`** por **`GdiSwal2.`** nas views e scripts (sem manter alias `bootbox`).

**Decisões técnicas relevantes:**
- Não usar o identificador global `SweetAlert2` (reservado à lib / confuso); **`GdiSwal2`** indica stack Swal2 + camada GDI sem colidir com `Swal`.

**O que foi evitado e por quê:**
- Alias `window.bootbox = GdiSwal2` — manter dois nomes dilui a padronização.

**Impactos conhecidos:**
Qualquer script externo ou bookmarklet que ainda chamasse `bootbox` deixa de funcionar até migrar para `GdiSwal2`.

**Atenção para próximas intervenções:**
Novos modais devem usar `GdiSwal2.dialog` / `LibMessageDialog` conforme o padrão da view.

---

### [2026-05-13] — Remover pasta `plugins/bootbox-compat` (obsoleta)
**Tipo:** Refatoração
**Arquivos tocados:**
- `LibUI_AdminLTE-4.0.0/plugins/bootbox-compat/` (pasta removida do repositório)
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Eliminar a pasta `LibUI_AdminLTE-4.0.0\plugins\bootbox-compat` após migração do shim para `startprime/js/gdi-swal-bootbox-shim.js`.

**O que foi feito:**
Remoção recursiva da pasta no disco de trabalho; o `.csproj` e o `BundleConfig` já não referenciam este caminho.

**Decisões técnicas relevantes:**
- Nenhum ficheiro fonte restante na pasta; evita confusão com artefactos antigos de publish.

**O que foi evitado e por quê:**
- Alterar histórico antigo do CHANGELOG (entradas de 2026-05-12) — mantidas como arquivo.

**Impactos conhecidos:**
Nenhum em runtime; o bundle continua a servir `gdi-swal-bootbox-shim.js`.

**Atenção para próximas intervenções:**
Se `PackageTmp` ainda contiver cópia read-only de `bootbox-compat`, limpar `obj` antes do publish (já documentado na secção «Armadilhas»).

---

### [2026-05-13] — Shim Bootbox/Swal: mover para `startprime/js/gdi-swal-bootbox-shim.js`
**Tipo:** Refatoração
**Arquivos tocados:**
- `LibUI_AdminLTE-4.0.0/plugins/startprime/js/gdi-swal-bootbox-shim.js` (novo; conteúdo equivalente ao antigo shim)
- `LibUI_AdminLTE-4.0.0/plugins/bootbox-compat/bootbox-compat.js` (removido)
- `App_Start/BundleConfig.cs` (`~/bundles/libui-swal-compat`)
- `GDI-ERP-Plataform.csproj` (`Content` do novo JS; removido `bootbox-compat`)
- `LibUI_AdminLTE-4.0.0/plugins/startprime/js/start.js` (comentário JSDoc)
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Centralizar no startprime as funções globais necessárias ao ERP (`bootbox` + `GdiSwalCompat` sobre SweetAlert2), referenciadas pelo bundle, em vez de `plugins/bootbox-compat/`.

**O que foi feito:**
1. Criado `gdi-swal-bootbox-shim.js` com a mesma API (`GdiSwal2.alert|confirm|prompt|dialog|hideAll` e `window.GdiSwalCompat`).  
2. Bundle `libui-swal-compat`: SweetAlert2 → novo ficheiro (ordem inalterada em relação ao `start.js` no layout).  
3. Removido o ficheiro antigo e entrada `Content` no `.csproj`.

**Decisões técnicas relevantes:**
- Manter o nome global `bootbox` para não alterar dezenas de `.cshtml` com `GdiSwal2.dialog` / `hideAll`.

**O que foi evitado e por quê:**
- Renomear chamadas nas views para `Swal.fire` — fora do escopo; shim preserva contrato.

**Impactos conhecidos:**
Publish antigo em `PackageTmp` pode ainda referenciar `plugins\bootbox-compat` read-only (histórico conhecido); apagar `obj\Release\Package` se o aviso MSBuild voltar.

**Atenção para próximas intervenções:**
Editar o shim apenas em `startprime/js/gdi-swal-bootbox-shim.js`.

---

### [2026-05-13] — `GdiSwalCompat` ausente: `LibMessageAlert` caía no `alert()` nativo
**Tipo:** Correção
**Arquivos tocados:**
- `LibUI_AdminLTE-4.0.0/plugins/bootbox-compat/bootbox-compat.js`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Após restauro de backup, chamadas como `LibMessageAlert("Verifique as inconsistências", result.msg)` pareciam «desconfiguradas»: título/corpo sem SweetAlert2, HTML de `result.msg` (`<br/>`, `<b>`) não renderizado.

**O que foi feito:**
Definido `window.GdiSwalCompat` no fim de `bootbox-compat.js`, delegando `alert`, `confirm`, `prompt`, `dialog` e `hideAll` ao shim `bootbox` já existente (que usa SweetAlert2). `start.js` passa a encontrar `GdiSwalCompat` e a usar o fluxo previsto.

**Decisões técnicas relevantes:**
- Implementação no mesmo ficheiro do bundle `~/bundles/libui-swal-compat` (ordem: Swal → bootbox-compat → `GdiSwalCompat` → start.js), sem novo ficheiro nem dependência.

**O que foi evitado e por quê:**
- Alterar centenas de views com `LibMessageAlert(...)` — a causa era global (objeto em falta).

**Impactos conhecidos:**
`LibMessageConfirm`, `LibMessageDialog`, `LibMessagePrompt` (quando usam o ramo Swal) também passam a usar o shim; `prompt` continua com as limitações já existentes em `GdiSwal2.prompt` (não mostra `message` como HTML — comportamento anterior ao ramo `GdiSwalCompat` era fallback incompleto).

**Atenção para próximas intervenções:**
Se existir outro script que defina `GdiSwalCompat`, carregar antes de `bootbox-compat.js` ou remover duplicado.

---

### [2026-05-13] — FormPedidoCreate: `LibMessageProcessando` preso após `draw` na grelha de itens
**Tipo:** Correção
**Arquivos tocados:**
- `Areas/gc/Views/Movimentos/FormPedidoCreate.cshtml`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
`jsPesquisarGcMovimentosCreatePedidoItensPedido()` chama `LibMessageProcessando("")` e `DataTable().draw(false)` mas nunca `LibMessageProcessandoHide()`; o `waitingDialog` (start.js) permanece aberto («processando») e bloqueia a UI. Evidente ao chamar só o redraw (ex.: botão de teste) sem modal.

**O que foi feito:**
1. `drawCallback` no DataTable de `#dtGcMovimentosCreatePedido` para `LibMessageProcessandoHide()` ao concluir qualquer desenho (inclui redraw após `draw(false)`).  
2. `error.dt` e `ajax.error` passam a chamar `LibMessageProcessandoHide()` antes do alerta.  
3. `xhr.dt`: atualização de `label_total_pedido` só se `json` e propriedade existirem (evita exceção JS que interrompe o fluxo).

**Decisões técnicas relevantes:**
- Centralizar o fecho do overlay no `drawCallback` em vez de duplicar em cada chamada a `draw`, cobrindo também fluxos existentes (ex.: modal item) que reabriam o processando sem fecho garantido após o redraw.

**O que foi evitado e por quê:**
- Remover o botão de teste `btnInserirItem2` — não solicitado; a causa era o overlay global, não o botão.

**Impactos conhecidos:**
Qualquer `draw` na tabela de itens passa a invocar `LibMessageProcessandoHide()`; chamadas a `hide` sem `show` prévio devem ser toleradas por `waitingDialog.hide()` (já encapsulado em try/catch em start.js).

**Atenção para próximas intervenções:**
Se no futuro for necessário manter o `waitingDialog` aberto durante um redraw desta tabela, usar flag ou não acionar `LibMessageProcessando` antes desse `draw`.

---

### [2026-05-12] — Importação SC: não apagar itens ao abrir modal + URL com `idMovimento`
**Tipo:** Correção
**Arquivos tocados:**
- `Areas/gc/Controllers/MovimentosController.cs` (`ModalImportarExcelSC`, `ModalImportarTxtSC`)
- `Areas/gc/Controllers/MovimentosComprasController.cs` (`ModalImportarExcelSC`)
- `Areas/gc/Views/Movimentos/FormPedidoCreate.cshtml`
- `Areas/gc/Views/MovimentosCompras/CreateCotacaoPedidoCompra.cshtml`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Ao abrir «Importar Lista Itens» / «Importar Excel SC», `DeleteItemTemporario()` apagava todos os `gc_movimentos_itens` do `id_movimento` temporário (negativo), invalidando itens já incluídos antes do modal. A URL usava `?id=0` (não ligava ao parâmetro `idMovimento` da action).

**O que foi feito:**
1. Removida a chamada a `DeleteItemTemporario()` nos GET dos modais de importação (vendas e compras); o arranque «limpo» do pedido continua em `CreateCotacaoPedidoOS` / `CreateCotacaoPedidoCompra` via `DeleteItemTemporario()` já existente.  
2. `ViewBag.idMovimento` nos modais: se `idMovimento` na query for não nulo e ≠ 0, usa-se esse valor; senão mantém-se o negativo do utilizador (comportamento anterior).  
3. `FormPedidoCreate` e `CreateCotacaoPedidoCompra`: `load` do modal com `@Url.Action(..., new { area = "gc", idMovimento = ViewBag.idMovimento })`.

**Decisões técnicas relevantes:**
- Alinhar compras a vendas no mesmo critério de URL e de não limpar ao abrir importação.

**O que foi evitado e por quê:**
- Remover `DeleteItemTemporario()` do create inicial do pedido — alteraria o contrato de «nova ficha vazia».

**Impactos conhecidos:**
Reabrir o modal de importação já não zera a lista temporária; utilizadores que dependiam desse efeito colateral perdem-no (substituído por comportamento explícito).

**Atenção para próximas intervenções:**
`AjaxModalImportarTxtSC` / Excel continuam a gravar itens com `id_movimento` negativo do utilizador na lógica atual — independentemente do `ViewBag` passado ao modal (só o cabeçalho do modal fica coerente com a URL).

---

### [2026-05-12] — Remover sourceMappingURL de `popper.min.js` (Tempus Dominus)
**Tipo:** Correção
**Arquivos tocados:**
- `LibUI_AdminLTE-4.0.0/plugins/tempus-dominus-6.9.4/js/popper.min.js`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Ferramentas de debug / browser pediam `popper.min.js.map` (404 IIS); mensagem confundida com «erro de compilação» — não é MSBuild.

**O que foi feito:**
Removida a linha final `//# sourceMappingURL=popper.min.js.map` do `popper.min.js` bundle do Tempus Dominus, pois o `.map` não está versionado no repositório.

**Decisões técnicas relevantes:**
- Não adicionar ficheiro `.map` volumoso só para debug; remoção da diretiva é padrão quando não se distribuem maps.

**O que foi evitado e por quê:**
- Alterar `_Layout` / `_Modal` — desnecessário.

**Impactos conhecidos:**
Stack traces de `popper.min.js` no DevTools deixam de resolver ao fonte original (aceitável em produção).

**Atenção para próximas intervenções:**
Se ao atualizar Tempus Dominus o ficheiro for substituído, a diretiva pode voltar — repetir ou incluir o `.map` no deploy.

---

### [2026-05-12] — DataTables: aviso coluna 1 após «Importar Lista Itens» (pedido gc)
**Tipo:** Correção
**Arquivos tocados:**
- `Areas/gc/Controllers/MovimentosController.cs` (`GetDadosItensPedido`)
- `Areas/gc/Views/Movimentos/FormPedidoCreate.cshtml`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Alerta DataTables `Requested unknown parameter '1' for row 0, column 1` na tabela `dtGcMovimentosCreatePedido` após processar o modal «Importar Lista Itens».

**O que foi feito:**
1. `GetDadosItensPedido`: **LEFT JOIN** em `g_produtos` para incluir itens com `id_produto = 0` (comum após import SC antes da vinculação ao ERP); `ProdutoNome` com `p == null ? null : p.nome`.  
2. Só anexar textos de «Cotação Compra» quando `RecordMovimento != null` (evita `NullReferenceException` com `id_movimento` temporário negativo sem cabeçalho em `gc_movimentos`).  
3. `FormPedidoCreate.cshtml`: `ajax.dataSrc: 'aaData'` no DataTable para alinhar à resposta legado do servidor.

**Decisões técnicas relevantes:**
- Manter formato `aaData` / `iTotalRecords` já usado pelo controlador.

**O que foi evitado e por quê:**
- Refatorar todo o DataTable para API 2.x pura — fora do escopo.

**Impactos conhecidos:**
Itens sem produto GDI passam a aparecer na grelha (coluna produto pode ficar vazia até vinculação).

**Atenção para próximas intervenções:**
Replicar padrão LEFT JOIN noutras queries de itens se o mesmo sintoma aparecer.

---

### [2026-05-12] — Excluir `.cursor`, `.md` e `CLAUDE.md` do Web Publish (manter no Git)
**Tipo:** Implementação
**Arquivos tocados:**
- `GDI-ERP-Plataform.csproj`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Pastas `.cursor\`, `.md\` e ficheiro `CLAUDE.md` devem permanecer no repositório mas **não** ser copiados na publicação IIS.

**O que foi feito:**
Substituídos os `Content Include` pontuais por `None Include`: glob `.cursor\**\*`, glob `.md\**\*`, e `CLAUDE.md`. Em projetos Web ASP.NET, `Content` é empacotado no publish; `None` não é implantado por omissão.

**Decisões técnicas relevantes:**
- Um único bloco com comentário no `.csproj` para futuros ficheiros sob `.cursor` / `.md` herdarem o mesmo comportamento.

**O que foi evitado e por quê:**
- `wpp.targets` / exclusões por perfil — desnecessário enquanto os itens não forem `Content`.

**Impactos conhecidos:**
Publish (File System / FTP / Web Deploy) deixa de incluir esses caminhos no `PackageTmp`/destino.

**Atenção para próximas intervenções:**
Se algo em `.md` ou `.cursor` for marcado como `Content` noutro `ItemGroup`, voltará a publicar — rever ao adicionar.

---

### [2026-05-12] — Remoção segura do pacote NuGet SixLabors.ImageSharp (P3)
**Tipo:** Refatoração
**Arquivos tocados:**
- `GDI-ERP-Plataform.csproj`
- `packages.config`
- `.md/relatorio-migracao-netframework-472-481.md` (tabela de dependências)
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Plano P3: o projeto só importava `SixLabors.ImageSharp.props` e validava existência com `Error`; não havia `Reference` a `SixLabors.ImageSharp.dll` nem uso no código.

**O que foi feito:**
Removidos o `<Import ... SixLabors.ImageSharp.props />` no início do `.csproj`, a linha `Error` correspondente em `EnsureNuGetPackageBuildImports` e o pacote em `packages.config`. Atualizada a tabela no relatório de migração. `MSBuild /t:Restore,Build /p:Configuration=Release` concluído com sucesso.

**Decisões técnicas relevantes:**
- Manter `SixLabors.Fonts` (referência explícita; cadeia ClosedXML).

**O que foi evitado e por quê:**
- Remover `SixLabors.Fonts` → ainda necessário.

**Impactos conhecidos:**
Pasta local `packages\SixLabors.ImageSharp.3.1.12` pode permanecer até limpeza manual.

**Atenção para próximas intervenções:**
Se for necessário processamento de imagem com ImageSharp, reinstalar o pacote e adicionar `Reference` explícita se o SDK props não bastar em net472.

---

### [2026-05-12] — Remoção segura do pacote NuGet SkiaSharp e NativeAssets (P2)
**Tipo:** Refatoração
**Arquivos tocados:**
- `GDI-ERP-Plataform.csproj`
- `packages.config`
- `.md/relatorio-migracao-netframework-472-481.md` (tabela de dependências)
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Plano P2: remover SkiaSharp e pacotes `SkiaSharp.NativeAssets.*` (sem uso no código; barcodes via `System.Drawing` / Zen.Barcode).

**O que foi feito:**
Removida referência `SkiaSharp`, três linhas `Error` e três `Import` de targets no `EnsureNuGetPackageBuildImports` / final do projeto; removidas quatro entradas em `packages.config`. Build Release com MSBuild concluído com sucesso.

**Decisões técnicas relevantes:**
- Utilizador confirmou ausência de DLLs de terceiros que carreguem SkiaSharp por reflexão.

**O que foi evitado e por quê:**
- Alterar `SixLabors.ImageSharp` / `SixLabors.Fonts` → fora do escopo P2.

**Impactos conhecidos:**
Pastas `packages\SkiaSharp*` podem permanecer localmente até limpeza; deploy IIS deixa de incluir `SkiaSharp.dll` e nativos.

**Atenção para próximas intervenções:**
Se no futuro se integrar renderização via Skia, reintroduzir pacote + targets.

---

### [2026-05-12] — Remoção segura do pacote NuGet ZString (P1)
**Tipo:** Refatoração
**Arquivos tocados:**
- `GDI-ERP-Plataform.csproj`
- `packages.config`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Executar plano P1: excluir dependência não utilizada (`ZString`), alinhando projeto e restore.

**O que foi feito:**
Removida a entrada `<Reference Include="ZString" ...>` do `.csproj` e a linha `<package id="ZString" ...>` do `packages.config`. Validação: `MSBuild /t:Restore,Build /p:Configuration=Release` concluído com sucesso.

**Decisões técnicas relevantes:**
- Nenhum `using` ou tipo `ZString` no código-fonte — remoção sem substituto.

**O que foi evitado e por quê:**
- Remoção de outros pacotes (SkiaSharp, ImageSharp, etc.) → fora do escopo P1; exigem análise transitiva.

**Impactos conhecidos:**
Nenhum em runtime; pasta local `packages\ZString.2.6.0` pode permanecer até limpeza manual ou `nuget locals clear`.

**Atenção para próximas intervenções:**
Em máquinas de dev: opcionalmente apagar `packages\ZString.2.6.0` e correr restore.

---

### [2026-05-12] — Remoção de `_filestemp` do disco e reforço no `.gitignore`
**Tipo:** Implementação
**Arquivos tocados:**
- `_filestemp\` (pasta apagada do working tree — não estava rastreada pelo Git)
- `.gitignore`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Eliminar fisicamente `_filestemp` e subpastas; garantir que conteúdo gerado em runtime não volte a ser commitado.

**O que foi feito:**
`Remove-Item -Recurse -Force` na raiz do projeto. Confirmado `git ls-files` sem entradas em `_filestemp` (pasta já era ignorada; artefactos eram locais). No `.gitignore`: comentário alinhado a `Server.MapPath("~/_filestemp")`, mantido `_filestemp/` e acrescentado `**/_filestemp/`; removida linha acidental `/Areas/g/Controllers/ProdutosController.cs` que seguia o bloco (ruído, sem efeito útil no padrão de paths do repo).

**Decisões técnicas relevantes:**
- Não criar `.gitkeep`: a aplicação recria ficheiros sob `_filestemp` quando necessário (vários controllers).

**O que foi evitado e por quê:**
- `git rm` em massa → não havia paths rastreados com prefixo `_filestemp`.

**Impactos conhecidos:**
Primeira execução após deploy em servidor limpo: pastas filhas são criadas sob demanda pelo código existente.

**Atenção para próximas intervenções:**
Em IIS, garantir permissão de escrita na raiz da app para `_filestemp` quando relatórios/uploads forem gerados.

---

### [2026-05-12] — Publish VS: aviso Access denied em `bootbox-compat` (PackageTmp)
**Tipo:** Análise
**Arquivos tocados:**
- `.cursor/CHANGELOG-DEV.md`
- Removido localmente: `obj\Release\Package\PackageTmp\LibUI_AdminLTE-4.0.0\plugins\bootbox-compat` (artefato de build, não versionado)

**Problema / Demanda:**
Na publicação, MSBuild emitiu: `Warning : Access to the path 'bootbox-compat' is denied` (`Microsoft.Web.Publishing.targets` ao copiar para `PackageTmp`).

**O que foi feito:**
Confirmado que **não** existe `LibUI_AdminLTE-4.0.0\plugins\bootbox-compat` no código fonte (substituído por `sweetalert2\`). Em `PackageTmp` existia pasta residual `...\plugins\bootbox-compat` com atributo **somente leitura**, impedindo o pipeline de empacotamento de atualizar/remover o caminho. Pasta removida com `Remove-Item -Recurse -Force` (e tentativa de `attrib -R`).

**Decisões técnicas relevantes:**
- Tratar como problema de **cache de build** (`obj`), não de referência no `.csproj`.

**O que foi evitado e por quê:**
- Reintroduzir `bootbox-compat` no repositório → desnecessário e contrário à stack atual (SweetAlert2).

**Impactos conhecidos:**
Outros devs/CI: se o aviso voltar, limpar `obj` antes do publish.

**Atenção para próximas intervenções:**
Fechar VS, apagar pasta `obj` inteira e republicar se permissões persistirem; checar antivírus/OneDrive sobre a pasta do projeto.

---

### [2026-05-12] — Verificação de referências a bootbox-compat
**Tipo:** Análise
**Arquivos tocados:**
- `.cursor/CHANGELOG-DEV.md` (apenas este registro)

**Problema / Demanda:**
Verificar e corrigir referências ao componente `bootbox-compat` no código.

**O que foi feito:**
Varredura em todo o workspace (`.cshtml`, `.js`, `.cs`, `.csproj`, `.md`, `.cursor`): **nenhuma** ocorrência de `bootbox-compat`, `bootbox`, `libui-bootbox` ou arquivo `*bootbox*`. O padrão atual é SweetAlert2 + `gdi-swal-compat.js` registrado em `BundleConfig.cs` como `~/bundles/libui-swal-compat`, referenciado em `_Layout.cshtml`, `_Modal.cshtml`, `_Blank.cshtml` e views de identidade. Nenhuma correção de código foi necessária.

**Decisões técnicas relevantes:**
- Manter `libui-swal-compat` como camada de diálogos (substitui Bootbox historicamente).

**O que foi evitado e por quê:**
- Criar shim `bootbox-compat` → desnecessário: não há referências quebradas no repositório.

**Impactos conhecidos:**
Nenhum — apenas confirmação de estado.

**Atenção para próximas intervenções:**
Se aparecer 404 a `bootbox-compat` no navegador, checar cache/CDN ou HTML gerado fora deste repo; no código fonte atual o bundle correto é `~/bundles/libui-swal-compat`.

---

### [YYYY-MM-DD] — Inicialização do CHANGELOG-DEV
**Tipo:** Configuração
**Arquivos tocados:**
- `.cursor/rules`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Configuração inicial do ambiente de desenvolvimento com Cursor AI, definindo regras de comportamento do assistente e estrutura de memória do projeto.

**O que foi feito:**
Criados os arquivos `.cursor/rules` e `.cursor/CHANGELOG-DEV.md` para padronizar o comportamento do Cursor em todas as intervenções futuras e manter histórico estruturado da evolução do projeto.

**Decisões técnicas relevantes:**
- O Cursor deve ler este arquivo antes de qualquer intervenção para manter contexto acumulado
- Registros devem ser mínimos e objetivos — foco em decisões e alertas, não em descrição de código
- Novas entradas sempre no topo da seção de histórico

**O que foi evitado e por quê:**
- Uso de ferramentas externas de changelog → mantido dentro do próprio repositório para garantir acesso imediato pelo Cursor

**Impactos conhecidos:**
Nenhum — arquivos de configuração apenas.

**Atenção para próximas intervenções:**
- Sempre atualizar este arquivo ao final de cada sessão de trabalho
- Registrar padrões de código descobertos na seção "Padrões estabelecidos no projeto" do contexto geral acima
- Registrar arquivos críticos ou sensíveis na seção correspondente acima conforme forem identificados

---

<!-- PRÓXIMAS ENTRADAS ACIMA DESTA LINHA -->
