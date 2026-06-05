# Prompt genérico — Enxugar e otimizar memória do projeto (qualquer stack)

> **Uso:** colar na íntegra (ou adaptar os placeholders `{...}`) numa sessão de agente Cursor, Claude Code, Copilot ou similar.  
> **Origem:** metodologia validada em consolidação de memória de monólito ASP.NET MVC (2026-06-05).  
> **Objetivo:** reduzir duplicidade, tokens e erros de interpretação; manter só o que é **acionável** para intervenções futuras.

---

## Prompt (copiar a partir da linha abaixo)

---

### Missão

Audite e **enxugue a memória documental** deste repositório — ficheiros que orientam agentes e developers (regras Cursor, contextos, READMEs de IA, changelogs operacionais, backlogs, `CLAUDE.md`, `AGENTS.md`, `.cursor/rules/*.mdc`, `.cursor/context/*.md`, históricos em `docs/`, etc.).

**Não altere código de produção** (aplicação, infra deployada, schemas) salvo referências textuais que apontem para paths errados.

**Língua das respostas:** a mesma do projeto (se não definida, português brasileiro).

---

### Princípios inegociáveis

1. **Fonte de verdade:** em conflito entre documentação e código/manifestos do projeto (`package.json`, `.csproj`, `pom.xml`, `Cargo.toml`, `go.mod`, estrutura real de pastas), **prevalece o repositório** (código e manifestos).
2. **Cirurgia:** não apagar histórico valioso — **arquivar** ou **apontar**; remover só duplicatas óbvias e ruído.
3. **Uma informação, um lugar:** cada tipo de conhecimento tem **um** dono canónico; os restantes ficheiros só **ponteiros** (1–3 linhas).
4. **Mínimo útil:** memória ativa = o que um agente precisa **antes da primeira linha de código** + links para detalhe.
5. **Verificável:** toda consolidação termina com inventário (ficheiros tocados, linhas antes/depois, duplicatas mapeadas).

---

### Fase 1 — Inventário (só leitura)

Mapear **todos** os candidatos a «memória»:

| Categoria | Exemplos típicos |
|-----------|------------------|
| Regras do agente | `.cursor/rules/*.mdc`, `AGENTS.md`, `.github/copilot-instructions.md` |
| Contexto fixo | `AI-CONTEXT.md`, `CONTEXT.md`, `CLAUDE.md`, `GEMINI.md` |
| Estado operacional | `CHANGELOG*.md`, `BACKLOG*.md`, `TODO.md`, `ROADMAP.md` |
| Contexto por tema | `.cursor/context/`, `docs/architecture/`, `adr/`, `.claude/` |
| Redirecionamentos | ficheiros que só apontam para outro path |
| Gerados | `CHANGELOG-RECENT.md`, índices auto-gerados |

Para cada ficheiro, registar:

- **Linhas** (aproximado)
- **Função declarada** vs **conteúdo real**
- **Última utilidade** (estado atual vs histórico vs duplicata)

---

### Fase 2 — Classificar conteúdo

Etiquetar cada bloco de informação:

| Etiqueta | Critério | Destino após auditoria |
|----------|----------|------------------------|
| **P0 — Fixo** | Stack, restrições absolutas, áreas sensíveis, ordem de leitura | Ficheiro contexto fixo **enxuto** (alvo: ≤120 linhas) |
| **P1 — Operacional** | Últimas N alterações, decisões ativas, pendências | Changelog compacto + backlog (alvo changelog: ≤200 linhas) |
| **P2 — Tema** | Padrão profundo (auth, DB, UI, testes, deploy) | Um `.md` por tema + **índice** central |
| **P3 — Histórico** | Entradas antigas, fases concluídas, lotes de migração | Arquivo (`docs/history/`, `CHANGELOG-ARCHIVE`) — **não** reexpandir operacional |
| **P4 — Ruído** | Repetição literal, listas de fases já arquivadas, formato de resposta em 3 sítios | Eliminar ou substituir por ponteiro |
| **P5 — Órfão** | Duplicata sem prefixo/nome canónico, ficheiro abandonado | Remover após confirmar que o canónico existe |

---

### Fase 3 — Hierarquia alvo (adaptar ao projeto)

Definir e documentar esta árvore no **índice central** (criar se não existir):

```
Regras do agente (comportamento, git, deploy, formato resposta)
    ↓
Contexto fixo (identidade, stack, restrições, resumo arquitetura)
    ↓
Changelog operacional (estado + últimas ~10–20 entradas compactas)
Backlog / pendências (itens acionáveis com critério de aceite)
    ↓
ÍNDICE DE MEMÓRIA (catálogo: tema → ficheiro → 1 linha)
    ↓
Contextos por tema (detalhe técnico, ADRs, checklists)
    ↓
Histórico arquivo (somente consulta; não editar no dia a dia)
```

**Regra:** se um parágrafo aparece em ≥2 níveis, **só um nível** fica com o texto integral.

---

### Fase 4 — Mapa de centralização (template)

Preencher esta tabela para o projeto auditado:

| Tipo de informação | Dono canónico (1 lugar) | Deixar de duplicar em |
|--------------------|-------------------------|------------------------|
| Identidade / stack | `{CONTEXTO_FIXO}` | regras, CLAUDE, README IA |
| Formato resposta ao utilizador | `{REGRAS_AGENTE}` | CLAUDE, contexto fixo |
| Proibições (git push, deploy, secrets) | `{REGRAS_AGENTE}` | todos os outros |
| Arquitetura / padrões de código | `{CONTEXTO_TEMA_ARQUITETURA}` | contexto fixo, changelog |
| Pendências | `{BACKLOG}` | changelog (só resumo 3 linhas) |
| Alertas / armadilhas | `{CHANGELOG}` ou `{CONTEXTO_FIXO}` (escolher **um**) | CLAUDE (máx. 5 bullets ponteiro) |
| Scripts / comandos de verificação | `{ÍNDICE}` § Scripts | AI-CONTEXT, BACKLOG (lista curta OK) |
| Histórico de intervenções | `{ARQUIVO_HISTÓRICO}` | changelog operacional |

---

### Fase 5 — Ações de enxugamento

Executar nesta ordem:

1. **Criar índice central** (`{INDICE_MEMORIA}`) com:
   - hierarquia de leitura;
   - tabela tema → path;
   - scripts/comandos úteis;
   - secção «duplicatas removidas nesta consolidação»;
   - «o que NÃO guardar em memória ativa».

2. **Reescrever contexto fixo** — manter só P0; remover listas de fases/lotes; ponteiros para índice e temas.

3. **Compactar changelog operacional:**
   - manter estado atual + decisões ativas;
   - «últimas alterações»: entradas **compactas** (1–5 bullets);
   - fundir entradas do mesmo dia/tema;
   - mover blocos longos `### [data]` com 10+ campos para arquivo;
   - adicionar nota: *detalhe antigo → histórico*.

4. **Enxugar ficheiros «assistant entry»** (`CLAUDE.md`, etc.) — identidade curta + `@changelog-recent` + tabela de ponteiros + 3–5 armadilhas; **sem** duplicar regras do agente.

5. **Remover órfãos** — ficheiros duplicados (mesmo sufixo, prefixo errado ou sem prefixo quando o projeto exige data).

6. **Atualizar referências cruzadas** em:
   - regras `.mdc` / `AGENTS.md`;
   - comandos `.claude/commands/`, skills;
   - scripts que leem paths de changelog;
   - README de `docs/history/`.

7. **Corrigir geradores** — se existir script que sincroniza changelog recente, apontar para o **path canónico** na raiz.

---

### Fase 6 — Limites quantitativos sugeridos

| Ficheiro | Alvo | Se exceder |
|----------|------|------------|
| Contexto fixo | ≤ 120 linhas | mover detalhe para tema ou índice |
| Changelog operacional | ≤ 200 linhas | arquivar entradas > 30 dias |
| Entrada «assistant» | ≤ 80 linhas | só ponteiros |
| Índice memória | ≤ 150 linhas | ok ser o catálogo mais longo |
| Regras agente | manter | evitar duplicar § longos noutros sítios |

---

### Fase 7 — Validação

Antes de concluir:

- [ ] Nenhum path referenciado aponta para ficheiro removido (grep por nomes antigos).
- [ ] Hierarquia documentada no índice e referenciada no contexto fixo + regras.
- [ ] Changelog operacional legível em < 3 minutos.
- [ ] Histórico arquivo **intocado** (ou append explícito, nunca truncate silencioso).
- [ ] Contagem linhas antes/depois reportada.
- [ ] Tabela «duplicatas removidas» preenchida.

Se o projeto tiver **scripts de inventário** (prefixos de data, csproj, lint), executá-los e reportar exit code.

---

### Formato obrigatório da resposta ao utilizador

Responder nesta ordem:

1. **Diagnóstico** — fragmentação, principais duplicatas, causa (crescimento orgânico, múltiplos agentes, changelog inflado).
2. **Arquivos afetados** — lista completa com ação (criado / enxugado / removido / ponteiro).
3. **Hierarquia final** — diagrama ou árvore.
4. **Resumo duplicatas** — tabela «estava em → agora em».
5. **O que foi preservado** — histórico, temas, backlog.
6. **O que NÃO foi alterado** — código, deploy, secrets.
7. **Registo** — linha compacta para changelog do projeto + backlog se aplicável.
8. **Linha de commit** — uma linha, se houve alteração de ficheiros.

---

### Placeholders para adaptar

| Placeholder | Exemplo neste repo |
|-------------|-------------------|
| `{PROJETO}` | GDI-ERP-Plataform |
| `{CONTEXTO_FIXO}` | `AI-CONTEXT.md` |
| `{REGRAS_AGENTE}` | `.cursor/rules/*_gdi-erp-plataform.mdc` |
| `{CHANGELOG}` | `CHANGELOG-DEV.md` |
| `{BACKLOG}` | `BACKLOG-DEV.md` |
| `{INDICE_MEMORIA}` | `.cursor/context/2026_06_05_indice-memoria-ia.md` |
| `{ARQUIVO_HISTÓRICO}` | `docs/dev-history/CHANGELOG-DEV-HISTORICO-INICIAL.md` |
| `{CONTEXTO_TEMA_ARQUITETURA}` | `.cursor/context/2026_06_05_arquitetura-centralizada-erp-gdi.md` |

---

### Anti-padrões (não fazer)

- Unificar tudo num único `README.md` gigante.
- Apagar histórico sem arquivo.
- Copiar 50 entradas de changelog para `CLAUDE.md`.
- Deixar duas regras `alwaysApply: true` com o mesmo conteúdo.
- Renomear em massa sem atualizar referências (grep obrigatório).
- «Otimizar» removendo restrições de git/deploy/segurança do projeto.

---

### Critério de sucesso

Um agente novo, com **só** regras + contexto fixo + índice, consegue:

1. Saber stack e restrições em < 2 min de leitura.
2. Encontrar o documento certo para qualquer tema via índice.
3. Registar uma intervenção sem inflar o changelog.
4. Não contradizer três ficheiros diferentes sobre o mesmo padrão.

---

*Fim do prompt — adaptar placeholders e executar.*
