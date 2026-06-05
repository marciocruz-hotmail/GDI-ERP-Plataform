/*
  NFE-1 — Portal Vendedor (módulo removido 2026-05-19)
  Executar manualmente no SQL Server de homologação/produção.
  Desativa menu e permissões de controller inexistente (evita 404 no navbar).
*/
SET NOCOUNT ON;

DECLARE @ids TABLE (id_sistema_controller INT PRIMARY KEY);

INSERT INTO @ids (id_sistema_controller)
SELECT id_sistema_controller
FROM a_sistemas_controllers
WHERE ativo = 1
  AND LOWER(LTRIM(RTRIM(ISNULL(area, '')))) = 'g'
  AND LOWER(LTRIM(RTRIM(ISNULL(controller, '')))) = 'portalvendedor';

IF NOT EXISTS (SELECT 1 FROM @ids)
BEGIN
    PRINT 'Nenhum controller ativo g/PortalVendedor encontrado (já desativado ou inexistente).';
END
ELSE
BEGIN
    UPDATE c
    SET c.ativo = 0
    FROM a_sistemas_controllers c
    INNER JOIN @ids i ON i.id_sistema_controller = c.id_sistema_controller;

    PRINT CONCAT('Desativados ', @@ROWCOUNT, ' registo(s) em a_sistemas_controllers (PortalVendedor).');
END

/* Opcional: remover acessos de perfil ao menu legado (não apaga roles de troca de senha em Usuarios) */
UPDATE pa
SET pa.ativo = 0
FROM g_perfis_acessos pa
INNER JOIN @ids i ON i.id_sistema_controller = pa.id_sistema_controller
WHERE pa.ativo = 1;

PRINT CONCAT('Perfis_acessos desativados: ', @@ROWCOUNT);

/* Conferência */
SELECT id_sistema_controller, area, controller, action, title_menu, ativo
FROM a_sistemas_controllers
WHERE LOWER(LTRIM(RTRIM(ISNULL(controller, '')))) LIKE '%portalvendedor%';
