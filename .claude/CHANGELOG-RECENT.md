<!-- GERADO AUTOMATICAMENTE por .claude/scripts/sync_changelog_recent.py -->
<!-- Fonte: CHANGELOG-DEV.md (raiz) | Últimas 5 entradas -->

# CHANGELOG-DEV — Entradas Recentes

> Gerado automaticamente. Histórico completo: `CHANGELOG-DEV.md` e `docs/dev-history/`.

---

## Últimas alterações (5)

### 2026-06-10 — Limpeza g_assistentes (tabela removida do banco)
- EDMX atualizado a partir do schema removeu a entidade `g_assistentes` (POCO `Db/g_assistentes.cs` e `DbSet` já não existem). Limpeza das referências órfãs restantes: removido `<Compile>` de `Db\g_assistentes.cs` e `Db\Metadata\g_assistentesMetadata.cs` do `.csproj`; apagado `Db/Metadata/g_assistentesMetadata.cs`; removido token `g_assistentes` de `Db/InserirMetadata.exe.config`. Sem Controller/View/Modal (já não existiam). `ddl-version.txt` intocado (refere `id_vendedor_assistente` de `gc_movimentos`, não relacionado).

---

### 2026-06-10 — gc/Movimentos: ícone do status Cancelado (3) → fa-circle-xmark
- Substituído `fa-thumbs-down` por `fa-circle-xmark` no status 3 (Cancelado) em `MovimentosController` (iconeStatus `fa-solid` em GetDadosPedidos; iconeTipo `fa-regular` na listagem por status), alinhando ao padrão já usado em AtendimentosController/Financeiro. Distinção visual: Cancelado = X em círculo, Devolvido = fa-reply-all.

---

### 2026-06-10 — Movimento status 4 (Devolvido): tratamento de exibição (espelho do status 3)
- Novo `id_movimento_status = 4` ('Devolvido') em `gc_movimentos_status` (todo `movimento_devolvido = true` ⇒ status 4). Adicionado branch de exibição para status 4 onde já se tratava status 3 (Cancelado): `MovimentosController.GetDadosPedidos` (iconeStatus "Devolvido") e listagem por status (iconeTipo "Pedido(Devolvido)"); `RelatoriosComerciaisController` (sufixo " (Devolvido)"); `ReportEmailPedido` (texto "Devolvido", 2 ocorrências). Filtros por status revisados: PainelPedidos usa `== 2` (devolvido já excluído); GetDadosPedidos lista todos os status.

---

### 2026-06-10 — gc/Fretes/GetDados: exclui movimentos devolvidos (espelho de cancelado)
- `FretesController.GetDados`: filtro base passa a excluir `movimento_devolvido` (`.Where(m => !m.movimento_devolvido)`), espelhando o `!m.movimento_cancelado` já existente. Levantamento projeto-wide de `movimento_cancelado`: demais usos são definição de campo (POCO/EDMX/DDL), setter de cancelamento ou já tratados no MovimentosController.

---

### 2026-06-10 — gc/Movimentos/AjaxSavePosVenda: bloqueio de Pós-Venda em pedido devolvido
- `MovimentosController.AjaxSavePosVenda`: espelha a validação de `movimento_cancelado` para `movimento_devolvido` — bloqueia registro de Pós-Venda em pedido devolvido ("Não é possível registrar Pós-Venda em pedido devolvido."). Demais usos de `movimento_cancelado` no controller já tratados (projeção/exibição em GetDadosPedidos, filtro do PainelPedidos) ou são o setter do próprio cancelamento (não validação).

---
