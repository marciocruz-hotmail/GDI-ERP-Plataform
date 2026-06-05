# PERF-015 — IsTableUpdate sem MAX repetido (TTL)

**Data:** 2026-05-20  
**Ficheiros:** `Lib/LibDB.cs`, `Lib/Lookups/LookupQueryServiceCache.cs`, `Models/ModelControlTableUpdate.cs`

---

## Problema

Cada `GetOrLoadCombo` / `GetOrLoadDataset` chamava `LibDB.IsTableUpdate`, que executava:

```sql
SELECT MAX(datahora_cadastro), MAX(datahora_alteracao) FROM [tabela]
```

Em páginas com vários combos da mesma sessão, o **mesmo MAX** repetia na mesma request.

---

## Solução

### 1. Carimbo de verificação na sessão (`ModelControlTableUpdate`)

| Campo | Função |
|-------|--------|
| `DateTimeUpdate` | Último MAX observado (quando a tabela mudou) |
| `DateTimeLastVerified` | Última execução de IsTableUpdate (ou skip TTL) |

### 2. TTL absoluto (PERF-015)

| Tabela | TTL |
|--------|-----|
| `g_clientes`, `g_produtos` | **15 min** |
| Demais tabelas com `IsTableUpdate` | **5 min** |

Se `(agora - DateTimeLastVerified) < TTL` → `IsTableUpdate` retorna **false** **sem** `SELECT MAX`.

### 3. Ordem no `LookupQueryServiceCache`

1. Tentar **MemoryCache** primeiro.
2. **Cache hit:** só então `IsTableUpdate` (com TTL pode evitar MAX).
3. **Cache miss:** `IsTableUpdate` uma vez (propaga invalidação a outros combos da tabela) → `factory()`.

### 4. Invalidação explícita

`LookupCacheInvalidator.InvalidateForTable` chama `LibDB.ResetTableUpdateVerification(tableName)` para forçar MAX na próxima leitura.

---

## Aceite (validação)

1. Login → abrir ecrã com 2+ combos cacheados da mesma tabela (ex. `g_vendedores`, `g_moedas`).
2. SQL Profiler / Extended Events: na **mesma request**, após o primeiro MAX da tabela, os seguintes `IsTableUpdate` da mesma tabela+processo **não** disparam MAX dentro do TTL.
3. Alterar cliente → guardar → abrir combo clientes: após TTL ou após `OnTableUpdate`/`InvalidateForTable`, dados atualizados.

---

## Risco (médio)

Alteração na BD **sem** passar por `datahora_alteracao` ou dentro da janela TTL pode manter combo em MemoryCache desatualizado até expirar TTL ou sliding cache (15 min). Mitigação operacional: TTL conservador; testes em CreateEdit cliente.

---

## Relacionado

- `LookupQueryServiceCache.SlidingExpiration` = 15 min
- PERF-012–014 (assets); lookups Index usam typeahead sem este cache global
