# Financeiro `g` — mapeamento POSTs / views (2.5)

**Data:** 2026-05-20  
**Controller:** `Areas/g/Controllers/FinanceiroController.cs`

## Decisões (pós-remoção `FinanceiroFaturamentos` e `FinanceiroLancamentos` 2026-05-19)

| Endpoint | View / chamada UI | Decisão |
|----------|-------------------|---------|
| `AjaxCancelarTitulos` | `ModalCancelarTitulos.cshtml` | **Manter** |
| `AjaxBaixarTitulos` | `ModalBaixarTitulos.cshtml` | **Manter** |
| `AjaxEditarTitulo` | `ModalEditarTitulo.cshtml` | **Manter** |
| `AjaxSimularProrrogarVencimentoTitulos` / `AjaxProrrogarVencimentoTitulo` | `ModalProrrogarVencimentoTitulo.cshtml` | **Manter** |
| `AjaxGerarRemessaBoletosBancarios` | `ModalGerarRemessaBoletosBancarios.cshtml` | **Manter** |
| `ajaxEnviarBoletosEmail` | `Financeiro/Index.cshtml` | **Manter** |
| `AjaxFinanceiroCancelamento` | `gc/.../ModalGerarFinanceiroMovimentos.cshtml` | **Manter** (exclusão título no fluxo gc) |
| `GetDados` / `getValoresConsolidados` / `GetDadosGrafico` | Index / `DadosConsolidados` | **Manter** |
| `ModalTransferirContaCaixa` / `AjaxTransferirContaCaixa` | Index (dropdown Processos) | **Removido** — não usado em produção |
| `AjaxSaveFinanceiroAvulso` | *(ex-`FinanceiroLancamentos`)* | **Removido** — código morto |
| `AjaxDownloadBoletos` | *(ex-`FinanceiroFaturamentos`)* | **Removido** — código morto |
| `AjaxRelatorioBoletosPorStatus` | *(ex-Faturamentos)* | **Removido** — código morto |
| `AjaxRelatorioClientesInativoComLancamento` | *(ex-Faturamentos)* | **Removido** — código morto |
| `AjaxRelatorioClientesAtivoSemLancamento` | *(ex-Faturamentos)* | **Removido** — código morto |

## Smoke manual (2.5)

| # | Fluxo | Onde |
|---|--------|------|
| 1 | Exclusão faturamento / cancelamento título gc | `gc` → Gerar financeiro movimentos → cancelar título (`AjaxFinanceiroCancelamento`) |
| 2 | Cancelar título na grade `g` | Financeiro Index → Processos → Cancelar |
| 3 | Troca senha interno | `Usuarios` / navbar → `ModalUsuarioTrocarSenha` (`TokenAcesso` U/L) |
| 4 | Troca senha portal | `crm` / `UserIdentity` conforme perfil cliente |

**Não testar:** Transferir Conta Caixa (removido).
