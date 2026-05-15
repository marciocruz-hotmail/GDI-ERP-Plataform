# /verify-csproj — Verificar views vs .csproj antes do publish

Execute a verificação de integridade entre views que usam helpers GDI e o arquivo .csproj.

## Execução

```
python Scripts/gdi_verify_csproj_gdi_helpers.py
```

## O que verificar

O script lista arquivos `.cshtml` em `Areas/` e `Views/` que:
- Usam `GdiAjax*` ou `GdiDt*` helpers
- **NÃO** estão declarados como `<Content Include="...">` no `GDI-ERP-Plataform.csproj`

Se exit code = 0: tudo OK, pode publicar.
Se exit code = 1: existem views fora do .csproj → elas **não serão publicadas** pelo Visual Studio Publish.

## Se houver lacunas

Para cada view listada pelo script, adicione ao `.csproj` a entrada:
```xml
<Content Include="Areas\NomeArea\Views\NomeController\NomeView.cshtml" />
```

Agrupe as adições na seção `<ItemGroup>` existente, próxima às outras views da mesma área.

Após corrigir, execute o script novamente para confirmar exit code = 0.

## Armadilha conhecida

Views novas criadas via "Add New Item" no Visual Studio são automaticamente adicionadas ao .csproj.
Views criadas pelo Claude Code ou editadas manualmente **não são** — este script é o guardrail.
