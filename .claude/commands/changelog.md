# /changelog — Gerar entrada CHANGELOG-DEV.md

Gere um bloco de entrada para o `.cursor/CHANGELOG-DEV.md` com base nas alterações feitas nesta sessão.

Regras:
- Use a data de hoje no formato `[YYYY-MM-DD]`
- O tipo deve ser: Correção | Implementação | Análise | Refatoração
- Liste apenas os arquivos realmente tocados nesta sessão
- "O que foi feito" deve ser objetivo, sem prolixidade
- "O que foi evitado" deve registrar decisões de escopo deliberadas
- Apresente o bloco pronto para colar no TOPO da seção de histórico

Formato obrigatório:

```
---
### [YYYY-MM-DD] — <Título curto da intervenção>
**Tipo:** Correção | Implementação | Análise | Refatoração
**Arquivos tocados:**
- `Controllers/XxxController.cs`

**Problema / Demanda:**
<descrição objetiva>

**O que foi feito:**
<solução aplicada e por quê>

**Decisões técnicas relevantes:**
- <decisão e justificativa>

**O que foi evitado e por quê:**
- <item> → <razão>

**Impactos conhecidos:**
<partes verificadas e resultado>

**Atenção para próximas intervenções:**
<alertas, dependências frágeis>
---
```

Após gerar o bloco, pergunte se deve inserí-lo automaticamente no topo da seção de histórico do arquivo `.cursor/CHANGELOG-DEV.md`.
