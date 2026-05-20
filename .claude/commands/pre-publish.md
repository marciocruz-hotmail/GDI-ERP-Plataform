# /pre-publish — Checklist de build e deploy (Visual Studio → IIS)

Execute o checklist completo de pré-publish para o GDI-ERP-Plataform.

## Passo 1 — Verificar views vs .csproj

Execute o script de verificação e reporte os resultados:

```
python Scripts/2026_05_20_gdi_verify_csproj_gdi_helpers.py
```

Se exit code 1: liste as views com GdiAjax*/GdiDt* ausentes do .csproj e pergunte se deve corrigir.

## Passo 2 — Verificar DataTables área g

```
python Scripts/2026_05_20_gdi_inventory_datatables_g_area.py
```

Reporte controllers em Areas/g que ainda não têm o padrão try/catch + errorMessage.

## Passo 3 — Checklist interativo

Pergunte ao usuário cada item abaixo e registre OK / PENDENTE / N/A:

**Compilação:**
- [ ] Projeto compila sem erros novos?
- [ ] Todos os `using` e namespaces corretos para .NET 4.7.2?
- [ ] Nenhum pacote NuGet novo introduzido?

**Arquivos para publicação:**
- [ ] Views .cshtml alteradas salvas e no .csproj?
- [ ] Arquivos estáticos (CSS/JS) salvos?
- [ ] BundleConfig.cs atualizado se novos assets foram adicionados?
- [ ] Web.config / connectionStrings.config considerados?

**Banco de dados:**
- [ ] Scripts SQL de schema prontos para execução manual?
- [ ] Nenhuma string de conexão hardcoded introduzida?

**IIS / Servidor Windows:**
- [ ] Correção não depende de permissões IIS diferentes entre dev e prod?
- [ ] Sessão, autenticação e autorização não afetadas?

## Passo 4 — Resumo final

Apresente um resumo com: itens OK, itens PENDENTES (bloqueadores), e recomendação: PODE PUBLICAR / RESOLVER PENDÊNCIAS ANTES.

**Armadilhas conhecidas para lembrar:**
- Pasta `obj\...\PackageTmp\...\bootbox-compat` pode ficar somente leitura → apagar `obj` antes de publicar
- Cache de `start.js`: incrementar versão em `?v=VersionERP` se start.js foi alterado
