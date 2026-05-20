# PascalCase em `Areas/` — Inventário e plano de lotes

**Data:** 2026-05-19  
**Escopo pedido:** views `modal*` (Nfe, Faturamentos, etc.) + models `cst*` em `Areas/**/Controllers`, `Models`, `Views`.  
**Já concluído:** B1–B2d — classes `Cst*` (2026-05-19); **paths Git alinhados ao disco/csproj** (2026-05-20): 3 views NFe + `Usuarios` + **61** ficheiros `cst*.cs` → `Cst*.cs`. **Zero** `public class cst*`; **zero** `modal*.cshtml` em `Areas`; **zero** paths `cst*.cs` no índice Git.

**Nota:** `FinanceiroFaturamentos` (view `ModalAtualizarFaturamentoGestorFranquia`) — módulo **removido** do repo (2026-05-19); item B1.1 **N/A**.

---

## 1. Resumo executivo

| Categoria | Total analisado | Fora do padrão | Já conforme |
|-----------|-----------------|----------------|-------------|
| Views `Modal*.cshtml` | 129 ficheiros | **0** (B1 concluído) | 129 |
| Models `cst*` / `Cst*` em `Areas` | 58 ficheiros | **52** a alinhar (`cst` → `Cst` em ficheiro + classe) | 6 ficheiros já `Cst*.cs` (classes a rever) |
| Controllers em `Areas` | 69 | **0** (todos `*Controller.cs`) | 69 |

**Conclusão:** o trabalho de views é **pequeno** (3 renomeações). O trabalho de models `cst*` é **transversal** (~66 `.csproj` entries, dezenas de controllers/views, `Lib/`, `Controllers/UserIdentity`, `Robos/`).

---

## 2. Lote B1 — Views `modal*` → `Modal*` (prioridade alta, baixo risco)

### 2.1 Inventário (únicos ficheiros com inicial minúscula)

| # | Caminho atual | Nome alvo | Action MVC |
|---|---------------|-----------|------------|
| 1 | `Areas/g/Views/FinanceiroFaturamentos/modalAtualizarFaturamentoGestorFranquia.cshtml` | `ModalAtualizarFaturamentoGestorFranquia.cshtml` | `ModalAtualizarFaturamentoGestorFranquia` — `return View(...)` implícito |
| 2 | `Areas/g/Views/Nfe/modalCancelarNfe.cshtml` | `ModalCancelarNfe.cshtml` | `ModalCancelarNfe` — `return View("modalCancelarNfe", m)` **explícito** |
| 3 | `Areas/g/Views/Nfe/modalExportarDadosNfePDF.cshtml` | `ModalExportarDadosNfePDF.cshtml` | `ModalExportarDadosNfePDF` — `return View("modalExportarDadosNfePDF", ...)` **explícito** |

### 2.2 Referências a atualizar (B1)

| Ficheiro | Alteração |
|----------|-----------|
| `GDI-ERP-Plataform.csproj` | 3 entradas `Content Include` |
| `Areas/g/Controllers/NfeController.cs` | 2× `return View("modal...")` → `Modal...` |
| `Scripts/2026_05_20_gdi_remove_remaining_modal_icons.ps1` | 3 nomes de ficheiro |
| `Scripts/2026_05_20_gdi_check_specific_modals.ps1` | action `modalAtualizarFaturamentoGestorFranquia` → `ModalAtualizar...` (só no comentário/array de verificação; action real no controller já é PascalCase) |

**Sem alteração necessária:** `Url.Action("Modal...")` em `Index.cshtml` (já PascalCase). Form ids internos (`formModal...`) podem permanecer — não são nomes de ficheiro.

### 2.3 Impacto e riscos (B1)

| Item | Avaliação |
|------|-----------|
| Build | Baixo — só views + 2 strings no controller |
| Publish IIS Windows | Baixo — FS case-insensitive; preferir rename em 2 passos no git |
| Publish Linux / case-sensitive | **Médio** — nome do ficheiro deve coincidir com `return View(...)` |
| Runtime | Nenhum se renome + controller alinhados |
| Testes manuais | Faturamentos → sincronizar gestor franquia; Nfe → cancelar NF-e; Nfe → exportar PDF |

**Esforço estimado:** 0,5–1 h (inclui build + smoke test).

---

## 3. Lote B2 — Models `cst*` → `Cst*` (prioridade média, alto impacto)

### 3.1 Convenção alvo

| Antes | Depois |
|-------|--------|
| Ficheiro `cstFinanceiroIndex.cs` | `CstFinanceiroIndex.cs` |
| Classe `public class cstFinanceiroIndex` | `public class CstFinanceiroIndex` |
| `@model GdiPlataform.Areas.g.Models.cstFinanceiroIndex` | `...CstFinanceiroIndex` |
| `using` / parâmetros / variáveis tipadas | Atualizar em todo o repositório |

**Prefixo `Cst`:** manter como marcador de *custom view model* (equivalente ao `cst` atual), só padronizar PascalCase .NET.

### 3.2 Inventário por área (58 ficheiros em `Areas/**/Models`)

#### Área `g` (28 ficheiros `cst*.cs`)

- `cstAlterarPrecosTabelasRevendaVendedor.cs`
- `cstClienteLimiteCredito.cs`
- `cstCopiarPrecosTabelas.cs`
- `cstDadosProdutoServicosFixo.cs`
- `cstDadosTituloFinanceiroEdicao.cs`
- `cstExportacaoDadosNFEModel.cs`
- `cstFinanceiroBoletos.cs`
- `cstFinanceiroBoletosLancamentos.cs`
- `cstFinanceiroFaturamentosEnviarNFe.cs`
- `cstFinanceiroImpostos.cs`
- `cstFinanceiroIndex.cs`
- `cstFinanceiroLancamentos.cs`
- `cstFinanceiroLancamentosIndex.cs`
- `cstFinanceiroProrrogarVencimentoTitulos.cs`
- `cstFinanceiroTransferirContaCaixa.cs`
- `cstImportacoesBancarias.cs`
- `cstImportacoesWithDate.cs`
- `cstModalFiltroAvancado.cs`
- `cstPerfisAcessos.cs`
- `cstPortalClienteFinanceiro.cs`
- `cstRevendasTabelasDetalhesModel.cs`
- `cstStringReplace.cs`
- `cstUpload.cs`
- `cstUploadGed.cs`
- `cstVendedoresTabelasDetalhesModel.cs`
- `cstViewPerfisAcessosModel.cs`
- `cstViewPortalClienteFinanceiro.cs`
- `cstViewRevendasTabelasModel.cs`
- `cstViewVendedoresTabelasModel.cs`

#### Área `gc` (27 ficheiros — 21 `cst*` + 6 já `Cst*`)

**Já com ficheiro `Cst*` (rever se a classe interna também está PascalCase):**

- `CstEstoqueLotesMovimentar.cs` → classe `CstEstoqueLotesMovimentar`
- `CstInvoiceItemValidacao.cs` → classe `CstInvoiceItemValidacao`
- `CstPedidoConferenciaEntradaLote.cs` → classe `CstPedidoConferenciaEntradaLote`
- `CstPedidoSeparacao.cs` → classe `CstPedidoSeparacao`
- `CstPedidoSeparacaoLote.cs` → classe `CstPedidoSeparacaoLote`

**Ainda `cst*` (ficheiro + classe):**

- `cstComexCadastrarAtualizarItens.cs`
- `cstComexEntradaMateriais.cs`
- `cstComexEntradaMateriaisItens.cs`
- `cstDadosFaturamentoNF.cs`
- `cstDatasetClientesContatos.cs`
- `cstDatasetProdutosServicos.cs`
- `cstEstoqueComexItemImportacao.cs`
- `cstEstoqueEntradaMateriais.cs`
- `cstEstoqueEntradaMateriaisItem.cs`
- `cstFinanceiroLancamentoBoletoFebraban.cs`
- `cstGerarNFCompraExterior.cs`
- `cstImportacaoNFEntrada.cs`
- `cstInvoice.cs`
- `cstInvoiceItem.cs`
- `cstModalRelatorio.cs`
- `cstModelComexItemImportacao.cs`
- `cstModelComexItemInvoice.cs`
- `cstModelRelatorioComissaoVendedores.cs`
- `cstModelSalesOrderSC.cs`
- `cstMovimentoEntradaNF.cs`
- `cstMovimentoEntradaNFItem.cs`
- `cstNfeAutorizacao.cs`
- `cstNfeCartaCorrecao.cs`
- `cstNfeEmitente.cs`
- `cstNfeICMSTotal.cs` (classe: `cstNfeIcmsTotal` — atenção ao acrónimo ICMSTotal)
- `cstPainelComercialGerencial.cs`
- `cstPedido.cs`
- `cstPosicaoFinanceiraCliente.cs`
- `cstReportHTML.cs`
- `cstRowExtratoContaCaixa.cs`
- `cstUploadFiles.cs`
- `cstUploadList.cs`

#### Área `crm` (2)

- `cstDadosPedidoPortal.cs`
- `cstListaPedidosPortal.cs`

#### Área `a` (1 — híbrido)

- `CstFiltroModel.cs` — ficheiro PascalCase, **classe ainda** `cstFiltroModel` → alinhar para `CstFiltroModel`

### 3.3 Referências fora de `Areas/Models` (obrigatório no B2)

| Local | Motivo |
|-------|--------|
| `GDI-ERP-Plataform.csproj` | ~66 `Compile Include="Areas\...\Models\cst*.cs"` |
| `Areas/**/Controllers/*.cs` | Tipos em actions, parâmetros, `new cst...()` |
| `Areas/**/Views/**/*.cshtml` | `@model`, casts, instanciação em Razor |
| `Areas/g/Models/Lib/LibFinanceiro.cs` | Tipos financeiros |
| `Lib/LibDataSets.cs` | Datasets |
| `Controllers/UserIdentityController.cs` | Portal / tenant |
| `Models/ContextoModel.cs`, `Models/cstTenant.cs` | Raiz do projeto (fora de `Areas` — **decisão:** incluir no mesmo lote ou fase C separada) |
| `Robos/SintegraWS/Models/cstRetorno*.cs` | Robôs (fora de `Areas` — fase C) |

**Ordem de grandeza:** ~70 ficheiros `.cs` + ~68 `.cshtml` com `@model` contendo `cst` (grep parcial no repo).

### 3.4 Sub-lotes recomendados (B2)

Executar **um sub-lote por PR/sessão**, com build completo após cada um:

| Sub-lote | Conteúdo | Ficheiros model | Risco |
|----------|----------|-----------------|-------|
| **B2a** | `Areas/crm` + `Areas/a` | 3 | **Concluído** 2026-05-19 |
| **B2b** | `Areas/g/Models` | 29 | **Concluído** 2026-05-19 |
| **B2c** | `Areas/gc/Models` | 32 | **Concluído** 2026-05-19 |
| **B2d** | `Models/CstTenant`, `Robos/SintegraWS`, `UserIdentity` | 3 | **Concluído** 2026-05-19 |

### 3.5 Impacto e riscos (B2)

| Item | Avaliação |
|------|-----------|
| Compilação | **Obrigatório** build Release após cada sub-lote |
| Razor `@model` | Erro imediato se tipo não renomeado |
| Refactor manual | Propenso a falhas — **recomendado:** Rename Symbol (Visual Studio) ou `dotnet format` / Roslyn refactor por classe |
| Serialização JSON | Verificar se algum `TypeNameHandling` ou nome de tipo em JSON usa `cst*` (busca pontual antes do lote) |
| Git em Windows | Rename `cst`→`Cst` exige two-step (`git mv` intermediário) |
| Publish | Atualizar apenas DLL — sem migração SQL |
| Controllers | Nomes de classe **não** mudam (`UsuariosController` etc.) |

**Esforço estimado:** 2–4 dias (B2 completo), conforme ferramenta de rename e testes de regressão por módulo.

---

## 4. Lote B3 — Controllers/Views órfãos relacionados (opcional, pós B1/B2)

Itens **não** são `modal*` nem `cst*`, mas apareceram na análise de padronização:

| Item | Nota |
|------|------|
| `FinanceiroController.ModalTransferirContaCaixa` | **Concluído** 2026-05-19 — view `ModalTransferirContaCaixa.cshtml` + `AjaxTransferirContaCaixa` com filtro `LibDB.getFilterByUser` |
| `FinanceiroFaturamentosController` — script lista action `modalAtualizarFaturamentoGestorFranquia` | Coberto no B1 |

---

## 5. O que não entra nestes lotes

- **126 views** já nomeadas `Modal*.cshtml` — sem ação.
- **`web.config`** em `Areas/*/Views` — não renomear.
- **Entidades EF** `g_usuarios`, `db.*` — convenção de base de dados, fora do escopo.
- **Filtro global / CSRF / roles** — independente.

---

## 6. Checklist de verificação pós-cada lote

- [ ] `MSBuild` Release sem erros
- [ ] `GDI-ERP-Plataform.csproj` — todos os `Content`/`Compile` batem com o disco
- [ ] `python Scripts/2026_05_20_gdi_verify_csproj_gdi_helpers.py` (se views com helpers Gdi tocadas)
- [ ] Smoke test dos fluxos listados no lote
- [ ] Entrada no `.cursor/CHANGELOG-DEV.md`
- [ ] Em servidor case-sensitive: validar caminho exato da view no disco

---

## 7. Ordem de execução sugerida

1. **B1** — 3 views `modal*` (entrega rápida, desbloqueia Nfe/Faturamentos em Linux).
2. **B2a** — crm + a (aquecimento).
3. **B2b** — `Areas/g` models.
4. **B2c** — `Areas/gc` models.
5. **B2d** — referências na raiz (`Lib`, `UserIdentity`, `Robos`, `Models`).
6. **B3** — views órfãs / limpeza.

---

## 8. Comandos úteis (inventário reproduzível)

```powershell
# Views com inicial minúscula "modal"
Get-ChildItem -Path ".\Areas" -Recurse -Filter "*.cshtml" |
  Where-Object { $_.Name -cmatch '^modal' } |
  Select-Object FullName

# Models cst* em Areas
Get-ChildItem -Path ".\Areas" -Recurse -Filter "cst*.cs" |
  Select-Object FullName

# return View explícito com nome modal minúsculo
Select-String -Path ".\Areas" -Pattern 'return View\("modal' -Recurse
```

---

*Documento gerado para planeamento; implementação sob pedido por lote (B1 → B2a…).*
