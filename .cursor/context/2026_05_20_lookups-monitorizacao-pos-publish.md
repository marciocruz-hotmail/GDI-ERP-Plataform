# Lookups — monitorização pós-publish (1.9.4)

**Data:** 2026-05-20  
**Objetivo:** validar RAM do app pool IIS e invalidação de cache após alterações cadastrais.

---

## O que monitorizar

| Sinal | Onde | Limiar / ação |
|-------|------|----------------|
| Memória do worker process (w3wp) | Performance Monitor → *Private Bytes* do app pool do ERP | Subida sustentada após navegação em muitas telas com combos grandes (clientes/produtos) — comparar antes/depois do publish |
| Entradas `lookup:*` em cache | Debug local: contagem aproximada via logs ou ferramenta de diagnóstico MemoryCache | Crescimento ilimitado por sessão/token — esperado com sliding 15 min; picos após primeiro login |
| Combos desatualizados | Cadastro alterado (ex. novo cliente) e combo em outra aba/sessão | Deve refrescar após `LibDB` marcar tabela atualizada **ou** novo request com `IsTableUpdate` |

---

## Procedimento de smoke — invalidação cadastral

1. Login com utilizador de teste; abrir **Clientes** ou **Movimentos** (combo clientes carregado).
2. Noutra sessão ou SQL: inserir/alterar registo em `g_clientes` e executar fluxo que chama `LibDB` / gravação que dispara atualização de tabela (processo habitual do ERP).
3. Voltar à primeira sessão: pesquisar o novo cliente no combo (ou reabrir CreateEdit).
4. **Esperado:** lista inclui alteração após refresh da página ou após invalidação (`LookupCacheInvalidator.InvalidateForTable("g_clientes")` quando integrado ao fluxo de gravação).

**Nota:** a invalidação automática depende de `LookupCacheRegistry.Register` na carga do combo e de `InvalidateForTable` / `IsTableUpdate` no request seguinte. Combos **sem** `tableName` no cache (ex. listas estáticas) não invalidam por tabela.

---

## Combos sem cache global (não medir como leak)

- `GetComboGcProdutosPosicaoEstoqueIndex` — sem MemoryCache (largura de ecrã).
- Index/filtros com LINQ local nos controllers híbridos (ver `2026_05_20_lookups-convencao-index-vs-createedit.md`).

---

## Testes automatizados locais (pré-publish)

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" "Tests\GDI-ERP-Plataform.Lookups.Tests\GDI-ERP-Plataform.Lookups.Tests.csproj" /p:Configuration=Debug
Tests\GDI-ERP-Plataform.Lookups.Tests\bin\Debug\GDI-ERP-Plataform.Lookups.Tests.exe
python Scripts\2026_05_20_gdi_inventory_libdatasets_usage.py --fail
```

---

## Registo no CHANGELOG

Após publish em produção, anotar data, versão `VersionERP` e resultado do smoke de invalidação (OK / falha + ecrã).
