# NFe (área `g`) — arquitetura Ajax + e-Notas

**Data:** 2026-05-26  
**Controller:** `Areas/g/Controllers/NfeController.cs`  
**Robô:** `Robos/ENotas/RoboEnotasNFE.cs`  
**Portal Vendedor:** módulo **removido** (2026-05-19); este documento **não** cobre `PortalVendedorController`.

---

## API eNotas (Nota Gateway)

| Item | Valor |
|------|--------|
| **URL base** | `https://api.notagateway.com.br` |
| **Constante C#** | `RoboEnotasNFE.EnotasApiBaseUrl` |
| **Autenticação** | Header `Authorization: Basic {key1}` (`g_nfe_gateway.key1` = API Key) |
| **Empresa** | `g_nfe_gateway.key2` = ID da empresa no gateway |

**Paths usados pelo robô:**

| Operação | Método | Path |
|----------|--------|------|
| NF-e produto — emissão | POST | `/v2/empresas/{empresaId}/nf-e` |
| NF-e produto — consulta status | GET | `/v2/empresas/{empresaId}/nf-e/{identificador}` |
| NF-e produto — cancelamento | DELETE | `/v2/empresas/{empresaId}/nf-e/{identificador}` |
| NF-e produto — carta correção | POST/GET | `/v2/empresas/{empresaId}/nf-e/cartaCorrecao/` … |
| NFS-e — emissão | POST | `/v1/empresas/{empresaId}/nfes` |
| NFS-e — consulta status | GET | `/v1/empresas/{empresaId}/nfes/porIdExterno/{idExterno}` |
| NFS-e — XML | GET | `/v1/empresas/{empresaId}/nfes/porIdExterno/{idExterno}/xml` |

> Domínio legado `api.enotasgw.com.br` **não** deve ser usado. Toda chamada HTTP deve partir de `EnotasApiBaseUrl`.

---

## Fluxo de resolução `g_nfe` ↔ `gc_movimentos_nf`

```mermaid
flowchart LR
  UI[Index / modais NFe] -->|POST Ajax| NC[NfeController]
  NC -->|ResolverMovimentoNfParaGNfe| FIN[g_financeiro.id_financeiro_movimento]
  FIN --> MOV[gc_movimentos_nf]
  NC -->|com movimento| ROBO[RoboEnotasNFE]
  NC -->|sem movimento, com nfe_key| ROBO
  ROBO --> API[e-Notas API]
  ROBO --> SYNC[SincronizarGNfeComMovimentoNf]
  SYNC --> GNFE[g_nfe status / url_pdf / url_xml / nfe_key]
```

1. **`g_nfe.id_financeiro`** → **`g_financeiro`** → **`id_financeiro_movimento`** → último **`gc_movimentos_nf`** do movimento.
2. Com **`gc_movimentos_nf`**: gerar/atualizar/cancelar via métodos `*byMovimentoNFId` e sincronizar campos em **`g_nfe`**.
3. Sem movimento, mas com **`g_nfe.nfe_key`**: atualizar/cancelar direto na API (`AtualizarStatusG_nfePorId`, `CancelarG_nfePorId`).

---

## Contratos

| Camada | Padrão |
|--------|--------|
| DataTables (`GetDados`, `GetDadosNfeLogs`) | `errorMessage`, `severity`, `stackTrace`, `yesFilterOnOff`, `aaData`; `JsonDataTableException` no `catch` |
| Ajax modais / Index | `{ success: bool, msg: string, idProcessamento?: string }` |
| Cliente Ajax | `GdiAjaxNotifyInconsistencias(result.msg)` se `success != true`; `LibMessage*` / `LibMessageProcessando` |

---

## Mapa actions Ajax ↔ robô

| Action | Robô / lógica | Notas |
|--------|----------------|-------|
| `AjaxClonarNfe` | Clone EF `g_nfe` (sem API) | JSON body `record_g_nfe.id_nfe` |
| `AjaxCancelarNfe` / `AjaxEnviarCancelamentoNfe` | `CancelarNFPbyMovimentoNFId` ou `CancelarG_nfePorId` | Motivo obrigatório |
| `AjaxNfeEnviarPorEmailUnitario` | Validação e-mail + log `g_nfe_envio_email_log` + CSV `g_processamento` tipo **45** | Não chama API e-Notas |
| `ajaxExportarDadosNfePDF` | CSV período + `g_processamento` tipo **49** | URLs PDF/XML no export |
| `ajaxGerarNfe` | `GerarNFServicoByMovimentoNFId` | Exige `gc_movimentos_nf` |
| `AjaxAtualizarStatusNfe` | `AtualizarStatusNFPbyMovimentoNFId` ou `AtualizarStatusG_nfePorId` | |
| `AjaxSincronizarLotesNfe` | Lote até **200** NFes com `nfe_key` | |
| `ajaxImportarNfeLote` | Upload + relatório texto; **sem** parser XML | Integração futura |

Autorização: classe `[CustomAuthorize(..., g_Nfe_*, g_Nfe_Default)]` cobre todas as actions.

---

## Smoke manual (2.2.4)

Executar em ambiente com gateway e-Notas configurado (`g_nfe_gateway`, `g_nfe_config`):

| # | Caso | Critério OK |
|---|------|-------------|
| 1 | **Index** — listar / filtro avançado | DataTables carrega; `xhr.dt` sem erro |
| 2 | **Export PDF** — modal período | `success: true`; processamento tipo 49; CSV com URLs |
| 3 | **Logs** — `CreateEdit` aba Logs | `GetDadosNfeLogs`; notify JSON em erro |
| 4 | **Clonar** — 1 registro | Novo `id_nfe`; mensagem sucesso |
| 5 | **Gerar NF** — 1 registro com movimento NF | Robô retorna sucesso ou mensagem clara de falta de vínculo |
| 6 | **Atualizar status** — nota com `nfe_key` | Status/PDF/XML atualizados em `g_nfe` |
| 7 | **Cancelar** — motivo preenchido | Modal; sucesso ou mensagem e-Notas |
| 8 | **E-mail unitário** — NFe autorizada com PDF | Processamento tipo 45 ou impedimento documentado |
| 9 | **Sincronizar lote** | Resumo OK/falhas no diálogo |
| 10 | **Download PDF/XML** — colunas da grade | `window.open(url)` quando URL preenchida |

---

## Portal Vendedor (2.2.2 — N/A)

- **Removido:** `PortalVendedorController`, view `PortalFinanceiro`.
- **Mantido (não é o módulo):** roles `g_PortalVendedor_*` em `UsuariosController.ModalUsuarioTrocarSenha` / `AjaxUsuarioTrocarSenha` para logons com `TokenAcesso` iniciando em **`V`** (troca de senha vendedor).
- **Produção:** executar `Scripts/2026_05_22_gdi_sql_deactivate_portal_vendedor_menu.sql` (desativa `a_sistemas_controllers` + `g_perfis_acessos` do controller removido).
- **Código (2026-05-20):** troca de senha vendedor em `UsuariosController` usa roles `g_Vendedores_*` (não `g_PortalVendedor_*`).
