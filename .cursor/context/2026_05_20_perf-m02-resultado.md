# G-PERF-M02 — Resultado da medição

**Data:** 2026-05-20  
**VersionERP:** 2026.51.12  
**Script:** `Scripts/2026_05_20_gdi_perf_m02_network_baseline.py`  
**JSON:** `2026_05_20_perf-m02-resultado.json`

---

## Metas (checklist §0)

| Métrica | Meta |
|---------|------|
| Transferred (full navigation) | **&lt; 800 kB** |
| Finish | **&lt; 1,5 s** |

---

## Baseline histórico (PERF-000.1, 2026-05-20, dev)

| Rota | Transferred | Finish |
|------|-------------|--------|
| `gc/Movimentos/IndexPedido` | 1,1 MB | 1,89 s |
| `gc/Movimentos/CreatePedido` | 960 kB | 2,14 s |
| `g/Financeiro/Index` | 1,2 MB | 1,84 s |

---

## Medição estática (executada — proxy assets layout)

Estimativa: soma dos ficheiros `.min.js` / `.css` do layout por `data-gdi-page-scripts` × **0,35** (proxy gzip). **Não** inclui HTML, navbar, imagens, 1.º Ajax `GetDados`.

| Rota | Flags | Est. transferred | Raw assets | vs PERF-000 | Meta &lt;800 kB |
|------|-------|------------------|------------|-------------|----------------|
| `gc/RelatoriosRegulamentacao/Index` | 33 | **333 KB** | 952 KB | n/a (hub novo) | **OK (est.)** |
| `gc/Parametros/Index` | 33 | **282 KB** | 806 KB | n/a | **OK (est.)** |
| `gc/RelatoriosFinanceiros/Index` | 37 (S2) | **379 KB** | 1084 KB | n/a | **OK (est.)** |
| `g/CentrosCustos/Index` | 49 (jstree) | **331 KB** | 944 KB | n/a | **OK (est.)** |
| `gc/Movimentos/IndexPedido` | 39 | **510 KB** | 1458 KB | **−590 KB** vs 1100 | **OK (est. layout)** |
| `gc/Movimentos/CreatePedido` | 39 | **510 KB** | 1458 KB | **−450 KB** vs 960 | **OK (est. layout)** |
| `g/Financeiro/Index` | 39 | **510 KB** | 1458 KB | **−690 KB** vs 1200 | **OK (est. layout)** |

**Economia layout (DT+S2 removidos do global):** ~**791 KB** brutos (~**277 KB** transferred proxy) por página lite.

> Valores exactos: correr `python Scripts/2026_05_20_gdi_perf_m02_network_baseline.py --static` e abrir o JSON gerado.

---

## Medição DevTools live (pendente operador)

O ambiente do agente **não** tinha IIS local (`localhost:44388` indisponível). Para fechar M02 com Finish/DCL reais:

```powershell
cd c:\Marcio\Projetos\GDI-ERP-Plataform
pip install playwright
playwright install chromium
$env:GDI_M02_USER = "SEU_USUARIO"
$env:GDI_M02_PASSWORD = "SUA_SENHA"
python Scripts/2026_05_20_gdi_perf_m02_network_baseline.py --live --base-url https://SEU_HOST_HOMOLOG
```

**Smoke manual (recomendado na mesma sessão):**

1. DevTools → Network → **Disable cache** → Hard reload.
2. `gc/RelatoriosRegulamentacao/Index` — sem `datatables.min.js` / `select2.min.js`; modal ANP + Tempus OK.
3. `gc/Movimentos/IndexPedido` — regressão DT+S2.
4. Anotar **Transferred** e **Finish** na barra inferior do Network.

---

## Conclusão provisória

| Critério | Estado |
|----------|--------|
| Hubs lite reduzem payload de layout (estático) | **Atendido (proxy)** |
| Meta &lt; 800 kB só layout | **Atendido (est.)** em hubs; página cheia depende de Ajax/HTML |
| Finish &lt; 1,5 s | **Validar live** (PERF-000 CreatePedido ainda 2,14 s) |
| G-PERF-20f lazy modal | Smoke manual ao abrir modal em hub lite |

---

## Referências

- `2026_05_20_checklist-performance-erp.md` § Etapa 0 e G-PERF-M02
- `2026_05_20_layout-scripts-fase5-lazy-modal.md`
