/*
  Perfil vendedor legado (id_perfil = -800) — sem cadastro Clientes no ERP (PortalVendedor removido).
  Executar manualmente no SQL Server de homologação/produção após deploy do código.
  O bloqueio HTTP 403 em ClientesController permanece mesmo sem este script.
*/
SET NOCOUNT ON;

DECLARE @idPerfilVendedor INT = -800;
DECLARE @ids TABLE (id_sistema_controller INT PRIMARY KEY);

INSERT INTO @ids (id_sistema_controller)
SELECT id_sistema_controller
FROM a_sistemas_controllers
WHERE ativo = 1
  AND LOWER(LTRIM(RTRIM(ISNULL(area, '')))) = 'g'
  AND LOWER(LTRIM(RTRIM(ISNULL(controller, '')))) = 'clientes';

IF NOT EXISTS (SELECT 1 FROM @ids)
BEGIN
    PRINT 'Nenhum controller ativo g/Clientes encontrado.';
END
ELSE
BEGIN
    UPDATE pa
    SET pa.ativo = 0
    FROM g_perfis_acessos pa
    INNER JOIN @ids i ON i.id_sistema_controller = pa.id_sistema_controller
    WHERE pa.id_perfil = @idPerfilVendedor
      AND pa.ativo = 1;

    PRINT CONCAT('g_perfis_acessos desativados (perfil -800, g/Clientes): ', @@ROWCOUNT);
END

SELECT pa.id_perfil, c.area, c.controller, pa.ativo
FROM g_perfis_acessos pa
INNER JOIN a_sistemas_controllers c ON c.id_sistema_controller = pa.id_sistema_controller
WHERE pa.id_perfil = @idPerfilVendedor
  AND LOWER(c.controller) IN ('clientes', 'portalvendedor')
ORDER BY c.controller;
