# G-PERF-20c-bis — Views com partial Tempus

**Data:** 2026-05-20  
**Partials:** `Views/Shared/_LayoutHeadTempus.cshtml`, `_LayoutScriptsTempus.cshtml`  
**Scripts:** `Scripts/2026_05_20_gdi_inventory_tempus_hosts.py`, `Scripts/2026_05_20_gdi_apply_tempus_partials.py`

## Regra

- Telas autenticadas com `_Layout` que usam `jsDatepicker()` **ou** abrem `#mainModal` com modal que usa `jsDatepicker` devem incluir os dois partials (head após `Layout`, scripts antes do primeiro `<script type="text/javascript">`).
- Modais Ajax **não** incluem partial — herdam do documento pai.
- Login (`UserIdentity` Layout=null), `_Blank.cshtml` mantêm assets Tempus próprios.

## 22 hosts (2026.51.06)

| View | Motivo |
|------|--------|
| `g/Clientes/CreateEdit` | jsDatepicker direto |
| `g/Clientes/Index` | hub → `ModalAtualizarLimiteCredito` |
| `g/ContratosAviacao/CreateEdit` | jsDatepicker |
| `g/Financeiro/Index` | jsDatepicker + `ModalProrrogarVencimentoTitulo` |
| `g/Ged/Index` | jsDatepicker + modais upload/edit |
| `g/Nfe/CreateEdit` | jsDatepicker |
| `g/Nfe/Index` | hub → `ModalExportarDadosNfePDF` |
| `gc/ComexImportacoes/CreateEdit` | jsDatepicker |
| `gc/ComexFinanceiro/Index` | hub → `ModalCreateEditGcComexFinanceiro` |
| `gc/EstoqueControle/CreateEdit` | jsDatepicker |
| `gc/FinanceiroLancamentos/Index` | jsDatepicker |
| `gc/Fretes/Index` | jsDatepicker |
| `gc/Gerencial/IndexPainelComercialGerencial` | jsDatepicker |
| `gc/Movimentos/FormPedidoCreate` | jsDatepicker |
| `gc/Movimentos/IndexPedido` | jsDatepicker + modais pedido |
| `gc/Movimentos/PainelPedidos` | modais expedição/entrega/financeiro |
| `gc/RelatoriosComerciais/Index` | modais relatório |
| `gc/RelatoriosFinanceiros/Index` | modais relatório |
| `gc/RelatoriosRegulamentacao/Index` | modais relatório |
| `qa/GedSGQ/IndexComunicados` | jsDatepicker |
| `qa/GedSGQ/IndexAtasReunioes` | jsDatepicker |
| `qa/GedSGQ/IndexDocsSGQ` | jsDatepicker |

## jstree (G-PERF-20c + 20d)

- `g/CentrosCustos/Index`, `g/ClassificacaoFinanceira/Index` — flag `Jstree` no filter (sem partial manual na view)

## Nova view com datepicker

1. Incluir partials na view host (não no modal).
2. Se for hub novo, validar com `python Scripts/2026_05_20_gdi_inventory_tempus_hosts.py`.
