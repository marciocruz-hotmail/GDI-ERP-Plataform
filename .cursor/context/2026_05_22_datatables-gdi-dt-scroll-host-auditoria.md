# Auditoria — DataTables e `gdi-dt-scroll-host`

**Data:** 2026-05-20  
**Script:** `Scripts/2026_05_22_gdi_audit_dt_scroll_host.py`  
**Motivação:** overflow horizontal na aba Anexos de `FormPedidoCreate` (corrigido só em `#dtGcMovimentosGed`).

---

## Padrão oficial (`start.css`)

| Camada | Classe | Efeito em DataTables (`.display`) |
|--------|--------|-----------------------------------|
| Wrapper scroll largo | `table-responsive scroll-body-horizontal` | `width: max-content` na tabela — **scroll horizontal intencional** quando há muitas colunas |
| Host de contenção | `gdi-dt-scroll-host min-w-0` no ancestral | **Só** listagens largas (11+ col., ex. IndexPedido). Abas/modais ≤10 col. → tabela direto no `card-body` (padrão Index) |
| Linha vazia DT | `tbody td.dt-empty` | Mensagem numa linha (`colspan`); com host: `start.css` usa `table-layout: auto` quando vazio |
| Colunas curtas (data, ícone, ação) | `dt-nowrap` em `aoColumnDefs` | Uma linha + ellipsis (opcional com host) |
| Colunas com texto longo | `dt-wrap` em `aoColumnDefs` | Quebra de linha (opcional com host) |

**Regra prática:** listagens **largas** (12+ colunas, Index) podem manter só `scroll-body-horizontal`. Abas, modais de anexos e grelhas **≤10 colunas** dentro de card/tab devem usar **`gdi-dt-scroll-host`**.

`jsInitForm()` em `start.js` normaliza largura do wrapper `scroll-body-horizontal` a 100%, mas **não** substitui o host para `max-content` da tabela.

---

## Números (Areas + Views, `.cshtml`)

| Estado | Quantidade | % |
|--------|------------|---|
| Ficheiros com DataTable / `display` | **138** | 100% |
| Com `gdi-dt-scroll-host` | **3** | **2,2%** |
| `scroll-body-horizontal` **sem** host | **57** | 41% |
| DataTable **sem** `scroll-body-horizontal` | **78** | 57% (maioria modais pequenos) |

### Já conformes (host)

- `Areas/gc/Views/Movimentos/IndexPedido.cshtml`
- `Areas/gc/Views/Movimentos/PainelPedidos.cshtml`
- `Areas/gc/Views/Movimentos/FormPedidoCreate.cshtml` — aba Anexos (`#dtGcMovimentosGed`)
- **Lote A:** modais anexos/GED (4 ficheiros)
- **Lote A+ (2026-05-20):** `FormPedidoCreate` (itens + audit + anexos), `ComexImportacoes/CreateEdit` (itens, GED, invoices PDF, audit)
- **Lote B (2026-05-20):** `Clientes/CreateEdit`, `Atendimentos/Edit`, `ModalConsultaPedidos`, `Financeiro/DadosConsolidados`, `Nfe/CreateEdit` (logs)
- **Exceção intencional:** `ComexImportacoes` aba **Invoices expandidas** (13 col.) — mantém só `scroll-body-horizontal` (grelha larga)

### `FormPedidoCreate` — parcial

| Tabela | Colunas | Wrapper atual |
|--------|---------|---------------|
| `dtGcMovimentosCreatePedido` | 10 | `scroll-body-horizontal` **sem** host |
| `dtGcMovimentosGed` | 8 | **OK** `gdi-dt-scroll-host` |
| `dtAudit` | 2 | sem scroll (risco layout BS5) |
| `dtGcTarefasPedido` | (se existir) | ver view |

---

## Candidatas prioritárias (≤10 colunas + scroll, sem host)

**38 ficheiros** — maior risco do mesmo sintoma da aba Anexos.

Exemplos:

- `Areas/gc/Views/Movimentos/ModalPedidoViewAnexos.cshtml` (8 cols)
- `Areas/gc/Views/FinanceiroLancamentos/ModalFinanceiroViewAnexos.cshtml` (8 cols)
- `Areas/g/Views/Clientes/CreateEdit.cshtml` — destinatários (2 cols)
- `Areas/gc/Views/ComexImportacoes/CreateEdit.cshtml` — 6 grelhas com scroll, sem host
- Índices estreitos: `Cidades`, `UF`, `Filiais`, `Perfis`, `Cfop*`, etc.

Listagem completa: correr o script (secção `CANDIDATAS`).

---

## O que **não** migrar às cegas

- **IndexPedido**, **PainelPedidos** — já têm host; muitas colunas.
- Grelhas com **12+ colunas** onde o scroll horizontal é desejado (ex.: alguns Index com 14–18 `<th>`) — manter `scroll-body-horizontal` sem host ou avaliar caso a caso.
- Tabelas **MVC** (`gdi-form-table-scroll`) — outro contrato (ver `2026_05_20_tabelas-datatables-vs-mvc.md`).

---

## Plano sugerido (lotes)

1. **Lote A — Modais anexos/GED** (mesmo layout que Anexos pedido): `ModalPedidoViewAnexos`, `ModalFinanceiroViewAnexos`, uploads GED.
2. **Lote B — Abas CreateEdit** (`FormPedidoCreate` itens/audit, `ComexImportacoes/CreateEdit`, `Clientes/CreateEdit`, `Atendimentos/Edit` sem wrapper).
3. **Lote C — Index ≤10 colunas** (38 candidatas): host + `min-w-0` no `card-body`.
4. **Lote D — Index largos**: só se QA reportar scroll indesejado; senão manter scroll nativo.

**Aceite por ficheiro:** abrir ecrã → sem barra horizontal com tabela vazia; com dados longos, ellipsis ou `dt-wrap`; paginação DataTables sem cortar o card.

---

## Reexecução

```powershell
python Scripts/2026_05_22_gdi_audit_dt_scroll_host.py
```
