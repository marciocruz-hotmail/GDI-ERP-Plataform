<!-- GERADO AUTOMATICAMENTE por .claude/scripts/sync_changelog_recent.py -->
<!-- Fonte: CHANGELOG-DEV.md (raiz) | Últimas 5 entradas -->

# CHANGELOG-DEV — Entradas Recentes

> Gerado automaticamente. Histórico completo: `CHANGELOG-DEV.md` e `docs/dev-history/`.

---

## Últimas alterações (5)

### 2026-06-11 — datahora_nf vazio: relatórios zerados + correção gravação e fallback
- **Causa:** coluna `gc_movimentos.datahora_nf` existia no schema mas não era preenchida na autorização NFe (`RoboEnotasNFE`); filtros `BETWEEN datahora_nf` excluíam todos os pedidos (NULL).
- **Correção:** `RoboEnotasNFE` grava `datahora_nf`/`id_usuario_nf` na 1ª NF autorizada; relatórios (`GerencialController`, `RelatoriosComerciaisController`, `RoboWhatsAppGerencial`) usam `COALESCE(datahora_nf, MIN(nf_data_autorizacao))` para histórico.
- **Backfill:** `Scripts/2026_06_11_gdi_backfill_gc_movimentos_datahora_nf.sql` (executar uma vez no SQL Server).

---

### 2026-06-11 — RoboWhatsAppGerencial: ranking vendedores por volume (Hoje/Mês)
- `AppendSecaoPedidos`: listagem de vendedores em «Pedidos Hoje» e «Pedidos Mês» ordenada por `valor_total_bruto` decrescente (ranking isolado por seção); desempate alfabético. Removido `IdsVendedoresOrdemAlfabetica`.

---

### 2026-06-11 — Filtro período pedidos fechados (NF autorizada): datahora_nf
- `GerencialController`, `RelatoriosComerciaisController` (`AjaxModalRelatorioVendedoresPedidos`) e `RoboWhatsAppGerencial`: filtros de período em pedidos com NF autorizada passam a usar `gc_movimentos.datahora_nf` (primeira NF autorizada) em vez de `datahora_aprovacao`. Painel de pedidos em processamento (posições 1–5) mantém `datahora_aprovacao`.

---

### 2026-06-10 — Limpeza g_assistentes (tabela removida do banco)
- EDMX atualizado a partir do schema removeu a entidade `g_assistentes` (POCO `Db/g_assistentes.cs` e `DbSet` já não existem). Limpeza das referências órfãs restantes: removido `<Compile>` de `Db\g_assistentes.cs` e `Db\Metadata\g_assistentesMetadata.cs` do `.csproj`; apagado `Db/Metadata/g_assistentesMetadata.cs`; removido token `g_assistentes` de `Db/InserirMetadata.exe.config`. Sem Controller/View/Modal (já não existiam). `ddl-version.txt` intocado (refere `id_vendedor_assistente` de `gc_movimentos`, não relacionado).

---

### 2026-06-10 — gc/Movimentos: ícone do status Cancelado (3) → fa-circle-xmark
- Substituído `fa-thumbs-down` por `fa-circle-xmark` no status 3 (Cancelado) em `MovimentosController` (iconeStatus `fa-solid` em GetDadosPedidos; iconeTipo `fa-regular` na listagem por status), alinhando ao padrão já usado em AtendimentosController/Financeiro. Distinção visual: Cancelado = X em círculo, Devolvido = fa-reply-all.

---
