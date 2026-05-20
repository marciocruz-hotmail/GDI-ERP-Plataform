# /changelog — Gerar registo para CHANGELOG-DEV.md (raiz)

Gere um resumo para **`CHANGELOG-DEV.md`** na **raiz** do repositório (formato compacto), com base nas alterações desta sessão.

**Não** gerar blocos longos no estilo antigo (listas extensas de ficheiros). O histórico detalhado fica em `docs/dev-history/` ou `.cursor/context/`.

## Regras

- Data: `YYYY-MM-DD`
- Tipo: Correção | Implementação | Análise | Refatoração
- Uma ou duas frases de resumo técnico (módulo + efeito)
- Pendências novas → sugerir linha em `BACKLOG-DEV.md`
- Apresentar linha para a tabela «Últimas alterações relevantes» e, se necessário, bullet sob o mês correto

## Formato sugerido (colar no CHANGELOG operacional)

```markdown
| YYYY-MM-DD | **<área>** <resumo curto> |
```

## Formato opcional (context doc — detalhe)

Se a intervenção for grande, criar `.cursor/context/YYYY_MM_DD_<tema>.md` com:

- Problema / causa raiz
- O que foi feito
- Decisões e o que foi evitado
- Smoke / publish

## Referências

- Changelog operacional: `CHANGELOG-DEV.md`
- Backlog: `BACKLOG-DEV.md`
- Histórico integral: `docs/dev-history/CHANGELOG-DEV-HISTORICO-INICIAL.md`

Perguntar se deve atualizar `CHANGELOG-DEV.md` e `BACKLOG-DEV.md` automaticamente.
