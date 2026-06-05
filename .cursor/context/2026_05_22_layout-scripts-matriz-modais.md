# G-PERF-20 — Matriz páginas hub → modais Ajax → scripts

**Data:** 2026-05-20  
**Fonte:** grep `mainModal.load` + inventário `2026_05_21_layout-scripts-inventario.json`  
**Regra:** o HTML do modal executa no **mesmo documento** que a página pai; libs devem estar carregadas **antes** do `.load()`.

---

## Legenda de libs mínimas por modal

| Sigla | Biblioteca |
|-------|------------|
| DT | DataTables |
| S2 | Select2 / `gdi-select2` / typeahead |
| TM | Tempus Dominus |
| JS | jstree |
| TG | bootstrap4-toggle |
| C | Core (sempre) |

---

## Hubs prioritários (smoke Fase 2–4)

### gc — RelatóriosRegulamentacao (validado G-PERF-20e)

| Página | Modais (Ajax) | Libs na página pai |
|--------|---------------|-------------------|
| `RelatoriosRegulamentacao/Index` | ANP, IBAMA, PF, Jogue Limpo | **C + TG + TM** (partial view); sem DT/S2 |

### gc — Relatórios (comercial / financeiro / cadastrais) — validado 20e

| Página | Libs na página pai | Atributo |
|--------|-------------------|----------|
| `RelatoriosComerciais/Index` | C + TG + TM | `LayoutHubReport` |
| `RelatoriosFinanceiros/Index` | C + TG + **S2** + TM | `LayoutHubReportSelect2` |
| `RelatoriosCadastrais/Index` | C + TG | `LayoutHubReport` |

### gc — Movimentos (comercial)

| Página | Modais / notas | Libs sugeridas |
|--------|----------------|----------------|
| `Movimentos/IndexPedido` | Consulta pedidos, diversos | C + DT + **S2** (typeahead cliente) |
| `Movimentos/PainelPedidos` | Vários | C + DT + S2 |
| `Movimentos/FormPedidoCreate` | Upload, itens, NF, etc. | **FullAuthenticated** ou C+DT+S2+TM+TG |
| `MovimentosEntradas/Index` | NF entrada | C + DT |

### g — Financeiro

| Página | Modais | Libs sugeridas |
|--------|--------|----------------|
| `Financeiro/Index` | Boletos, prorrogar, remessa | C + DT (+ S2 se filtro typeahead) |

### g — Cadastros Index

| Página | Modais típicos | Libs sugeridas |
|--------|----------------|----------------|
| `Clientes/Index` | Contato, destinatário, Sintegra | C + DT + S2 |
| `Produtos/Index` | Ficha estoque, desativar | C + DT |
| `Nfe/Index` | Gerar, cancelar, lote | C + DT |
| `Ged/Index` | Upload, editar | C + DT |
| `Atendimentos/Index` | Novo atendimento | C + DT |
| `Vendedores/Index` | Copiar preços | C + DT |

### g — Hierárquicos (jstree) — validado 20e

| Página | Libs | Atributo |
|--------|------|----------|
| `CentrosCustos/Index` | C + TG + **JS** | `LayoutHubJstree` |
| `ClassificacaoFinanceira/Index` | C + TG + **JS** | `LayoutHubJstree` |

### g — Datas (Tempus)

| Página | Lib extra |
|--------|-----------|
| `Clientes/CreateEdit` | **TM** |

### gc — COMEX / Estoque

| Página | Notas | Libs |
|--------|-------|------|
| `ComexImportacoes/Index` / `CreateEdit` | Múltiplos modais GED/itens | C + DT |
| `Estoque/Index` | Typeahead produto | C + DT + S2 |
| `EstoqueInventario/FormInventarioItens` | Item inventário | C + DT |
| `FinanceiroLancamentos/Index` | Lançamento, anexos | C + DT + S2 |

### qa — GedSGQ

| Página | Libs |
|--------|------|
| `IndexDocsSGQ`, `IndexComunicados`, `IndexAtasReunioes` | C + DT (modais) |

---

## Contagem inventário (47 hosts `mainModal.load`)

Distribuição aproximada:

- **gc/Movimentos** e satélites: maior volume de modais  
- **g/Nfe**, **g/Financeiro**, **g/Clientes**: alto uso  
- **Relatórios** (5 Index): UI simples, **dependência via modais**  

**Implicação:** não usar “layout lite” (só Core) em Index de área `g`/`gc` sem validar **todos** os `Url.Action` em `mainModal.load` da view.

---

## Procedimento de validação (por hub, antes de opt-out)

1. Abrir Index no browser.  
2. DevTools → Network: anotar JS carregados no documento.  
3. Abrir **cada** botão que chama `$("#mainModal").load(...)`.  
4. No modal: testar grid, combo, datepicker, upload.  
5. Se falhar `$.fn.DataTable is not a function` → página pai precisa flag **DataTables**.  
6. Registrar exceção no `[GdiPageScripts(...)]` do controller.

---

## Referências

- Contrato: `2026_05_22_layout-scripts-contrato-flags.md`  
- JSON completo: `2026_05_21_layout-scripts-inventario.json`  
- Checklist: `2026_05_22_checklist-performance-erp.md` (G-PERF-20)
