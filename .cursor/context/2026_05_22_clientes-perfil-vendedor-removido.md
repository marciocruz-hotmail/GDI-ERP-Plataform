# Clientes — remoção acesso perfil vendedor (-800)

**Data:** 2026-05-20

## Contexto

O perfil **`id_perfil = -800`** era usado para carteira de vendedor no ERP (título "Acesso Vendedor", filtro `id_vendedor` em `GetDados`, UI reduzida). O módulo **`g/PortalVendedor`** foi removido (NFE-1); o cadastro de clientes no ERP **não** deve substituir esse fluxo.

## Implementação (código)

| Local | Alteração |
|-------|-----------|
| `ClientesController` | `OnActionExecuting` → **403** se `IdPerfil == -800` (todas as actions) |
| `GetDados` | Removido ramo `id_vendedor` para -800 |
| `Create` | Removido `id_vendedor` automático para -800 |
| `Index.cshtml` | Removido `IdPerfil == -800` no menu Processos |
| `CreateEdit.cshtml` | Botão Salvar sem condição -800 |

## BD (opcional)

`Scripts/2026_05_22_gdi_sql_revoke_clientes_perfil_vendedor.sql` — desativa `g_perfis_acessos` do menu **g/Clientes** para perfil -800.

## Não alterado

- Portal cliente **`crm`** / `gc_PortalCliente_*`
- `UsuariosController` troca senha com `g_Vendedores_*`
- Perfis com roles `g_Clientes_*` **normais** (não -800)
