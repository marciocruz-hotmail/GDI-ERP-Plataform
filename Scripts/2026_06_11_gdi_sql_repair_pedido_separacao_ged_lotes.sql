/*
  Reparo GED — cópias de anexos de lote para pedidos separados (bug id_arquivo_origem na origem, desde 05/06/2026).

  Contexto:
    Na separação, o ERP deve duplicar ged_arquivos dos lotes (id_estoque_lote > 0) para o pedido
    (id_gc_movimento, id_estoque_lote = 0, id_arquivo_origem = 1), espelhando AjaxModalPedidoSeparacao.

  Uso:
    1) Executar com @Modo = 'AUDIT' (padrão) — apenas diagnóstico.
    2) Revisar os resultsets.
    3) Executar com @Modo = 'CORRIGIR' dentro de transação (BEGIN TRAN … COMMIT/ROLLBACK).

  Critério de “já existe no pedido”:
    ged_arquivos ativo no pedido (id_gc_movimento, id_arquivo_origem = 1) com o mesmo filebucket do anexo do lote.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @DataInicio       DATE         = '2026-06-05';  /* pedidos separados a partir desta data (bug GED desde 05/06/2026) */
DECLARE @Modo             VARCHAR(10)  = 'AUDIT';        /* AUDIT | CORRIGIR */
DECLARE @IdUsuarioReparo  INT          = NULL;           /* opcional: id_usuario_cadastro nas cópias novas */

/* -------------------------------------------------------------------------- */
/* Pedidos separados no período                                               */
/* -------------------------------------------------------------------------- */
IF OBJECT_ID('tempdb..#PedidosSeparados') IS NOT NULL DROP TABLE #PedidosSeparados;

SELECT
    m.id_movimento,
    m.id_movimento_tipo,
    m.datahora_separacao,
    m.id_movimento_posicao
INTO #PedidosSeparados
FROM gc_movimentos m
WHERE m.movimento_separado = 1
  AND m.datahora_separacao >= @DataInicio;

/* -------------------------------------------------------------------------- */
/* Lotes efetivamente separados nos itens (slots 01–50 com qtd > 0)           */
/* -------------------------------------------------------------------------- */
IF OBJECT_ID('tempdb..#LotesPedido') IS NOT NULL DROP TABLE #LotesPedido;

SELECT DISTINCT
    p.id_movimento,
    v.id_estoque_lote
INTO #LotesPedido
FROM #PedidosSeparados p
INNER JOIN gc_movimentos_itens mi ON mi.id_movimento = p.id_movimento
CROSS APPLY (VALUES
    (mi.id_estoque_lote_01, mi.lote01_qtd),
    (mi.id_estoque_lote_02, mi.lote02_qtd),
    (mi.id_estoque_lote_03, mi.lote03_qtd),
    (mi.id_estoque_lote_04, mi.lote04_qtd),
    (mi.id_estoque_lote_05, mi.lote05_qtd),
    (mi.id_estoque_lote_06, mi.lote06_qtd),
    (mi.id_estoque_lote_07, mi.lote07_qtd),
    (mi.id_estoque_lote_08, mi.lote08_qtd),
    (mi.id_estoque_lote_09, mi.lote09_qtd),
    (mi.id_estoque_lote_10, mi.lote10_qtd),
    (mi.id_estoque_lote_11, mi.lote11_qtd),
    (mi.id_estoque_lote_12, mi.lote12_qtd),
    (mi.id_estoque_lote_13, mi.lote13_qtd),
    (mi.id_estoque_lote_14, mi.lote14_qtd),
    (mi.id_estoque_lote_15, mi.lote15_qtd),
    (mi.id_estoque_lote_16, mi.lote16_qtd),
    (mi.id_estoque_lote_17, mi.lote17_qtd),
    (mi.id_estoque_lote_18, mi.lote18_qtd),
    (mi.id_estoque_lote_19, mi.lote19_qtd),
    (mi.id_estoque_lote_20, mi.lote20_qtd),
    (mi.id_estoque_lote_21, mi.lote21_qtd),
    (mi.id_estoque_lote_22, mi.lote22_qtd),
    (mi.id_estoque_lote_23, mi.lote23_qtd),
    (mi.id_estoque_lote_24, mi.lote24_qtd),
    (mi.id_estoque_lote_25, mi.lote25_qtd),
    (mi.id_estoque_lote_26, mi.lote26_qtd),
    (mi.id_estoque_lote_27, mi.lote27_qtd),
    (mi.id_estoque_lote_28, mi.lote28_qtd),
    (mi.id_estoque_lote_29, mi.lote29_qtd),
    (mi.id_estoque_lote_30, mi.lote30_qtd),
    (mi.id_estoque_lote_31, mi.lote31_qtd),
    (mi.id_estoque_lote_32, mi.lote32_qtd),
    (mi.id_estoque_lote_33, mi.lote33_qtd),
    (mi.id_estoque_lote_34, mi.lote34_qtd),
    (mi.id_estoque_lote_35, mi.lote35_qtd),
    (mi.id_estoque_lote_36, mi.lote36_qtd),
    (mi.id_estoque_lote_37, mi.lote37_qtd),
    (mi.id_estoque_lote_38, mi.lote38_qtd),
    (mi.id_estoque_lote_39, mi.lote39_qtd),
    (mi.id_estoque_lote_40, mi.lote40_qtd),
    (mi.id_estoque_lote_41, mi.lote41_qtd),
    (mi.id_estoque_lote_42, mi.lote42_qtd),
    (mi.id_estoque_lote_43, mi.lote43_qtd),
    (mi.id_estoque_lote_44, mi.lote44_qtd),
    (mi.id_estoque_lote_45, mi.lote45_qtd),
    (mi.id_estoque_lote_46, mi.lote46_qtd),
    (mi.id_estoque_lote_47, mi.lote47_qtd),
    (mi.id_estoque_lote_48, mi.lote48_qtd),
    (mi.id_estoque_lote_49, mi.lote49_qtd),
    (mi.id_estoque_lote_50, mi.lote50_qtd)
) v(id_estoque_lote, lote_qtd)
WHERE v.id_estoque_lote > 0
  AND ISNULL(v.lote_qtd, 0) > 0;

/* -------------------------------------------------------------------------- */
/* Anexos ativos dos lotes vinculados aos pedidos                             */
/* -------------------------------------------------------------------------- */
IF OBJECT_ID('tempdb..#AnexosLote') IS NOT NULL DROP TABLE #AnexosLote;

SELECT
    lp.id_movimento,
    lp.id_estoque_lote,
    g.id_arquivo      AS id_arquivo_lote,
    g.id_arquivo_tipo,
    g.descricao,
    g.filename,
    g.filebucket,
    g.filetype,
    g.bucket,
    g.public_url,
    g.observacao,
    g.controla_data_referencia,
    g.controla_data_vencimento,
    g.data_referencia,
    g.data_vencimento,
    g.versao,
    g.size_bytes,
    g.size_kbytes,
    g.size_mbytes,
    g.size_gbytes,
    g.downloads,
    g.id_cliente_relacionado,
    g.id_contrato_relacionado,
    g.id_usuario_relacionado,
    g.id_gc_financeiro,
    g.id_comex_importacao,
    g.id_comex_invoice,
    g.id_comex_financeiro,
    g.id_atendimento,
    g.tag_string,
    g.datahora_cadastro,
    g.id_usuario_cadastro
INTO #AnexosLote
FROM #LotesPedido lp
INNER JOIN ged_arquivos g
    ON g.ativo = 1
   AND g.id_estoque_lote > 0
   AND g.id_estoque_lote = lp.id_estoque_lote;

/* -------------------------------------------------------------------------- */
/* Anexos que faltam no pedido (sem cópia ativa com mesmo filebucket)         */
/* -------------------------------------------------------------------------- */
IF OBJECT_ID('tempdb..#AnexosFaltantes') IS NOT NULL DROP TABLE #AnexosFaltantes;

SELECT
    al.*
INTO #AnexosFaltantes
FROM #AnexosLote al
WHERE NOT EXISTS (
    SELECT 1
    FROM ged_arquivos p
    WHERE p.id_gc_movimento = al.id_movimento
      AND p.id_arquivo_origem = 1
      AND p.ativo = 1
      AND p.filebucket = al.filebucket
);

/* ============================== AUDITORIA ================================= */

DECLARE @QtdPedidosSeparados      INT;
DECLARE @QtdLotesDistintos        INT;
DECLARE @QtdAnexosLoteEncontrados INT;
DECLARE @QtdAnexosFaltantesPedido INT;
DECLARE @QtdPedidosComFalta       INT;

SELECT @QtdPedidosSeparados = COUNT(*) FROM #PedidosSeparados;
SELECT @QtdLotesDistintos = COUNT(*) FROM #LotesPedido;
SELECT @QtdAnexosLoteEncontrados = COUNT(*) FROM #AnexosLote;
SELECT @QtdAnexosFaltantesPedido = COUNT(*) FROM #AnexosFaltantes;
SELECT @QtdPedidosComFalta = COUNT(DISTINCT id_movimento) FROM #AnexosFaltantes;

PRINT '--- Resumo ---';
SELECT
    PedidosSeparados      = @QtdPedidosSeparados,
    LotesDistintos        = @QtdLotesDistintos,
    AnexosLoteEncontrados = @QtdAnexosLoteEncontrados,
    AnexosFaltantesPedido = @QtdAnexosFaltantesPedido,
    PedidosComFalta       = @QtdPedidosComFalta;

PRINT '--- Pedidos separados no período (amostra) ---';
SELECT TOP (100)
    p.id_movimento,
    p.id_movimento_tipo,
    p.datahora_separacao,
    LotesNoPedido = (SELECT COUNT(DISTINCT lp.id_estoque_lote) FROM #LotesPedido lp WHERE lp.id_movimento = p.id_movimento),
    AnexosLote    = (SELECT COUNT(*) FROM #AnexosLote al WHERE al.id_movimento = p.id_movimento),
    AnexosFaltam  = (SELECT COUNT(*) FROM #AnexosFaltantes af WHERE af.id_movimento = p.id_movimento)
FROM #PedidosSeparados p
ORDER BY p.datahora_separacao DESC, p.id_movimento DESC;

PRINT '--- Detalhe: anexos de lote sem cópia no pedido ---';
SELECT
    af.id_movimento,
    af.id_estoque_lote,
    af.id_arquivo_lote,
    af.id_arquivo_tipo,
    af.descricao,
    af.filename,
    af.filebucket
FROM #AnexosFaltantes af
ORDER BY af.id_movimento, af.id_estoque_lote, af.id_arquivo_lote;

/* ============================== CORREÇÃO ================================== */

IF UPPER(LTRIM(RTRIM(@Modo))) = 'CORRIGIR'
BEGIN
    IF NOT EXISTS (SELECT 1 FROM #AnexosFaltantes)
    BEGIN
        PRINT 'Nenhum anexo pendente de cópia. Nada a corrigir.';
    END
    ELSE
    BEGIN
        DECLARE @QtdInserir INT = @QtdAnexosFaltantesPedido;
        DECLARE @QtdInseridos INT;

        PRINT CONCAT('Inserindo ', @QtdInserir, ' cópia(s) em ged_arquivos...');

        INSERT INTO ged_arquivos (
            ativo,
            id_arquivo_tipo,
            id_cliente_relacionado,
            id_contrato_relacionado,
            id_usuario_relacionado,
            id_gc_financeiro,
            id_gc_movimento,
            id_comex_importacao,
            id_comex_invoice,
            id_comex_financeiro,
            id_atendimento,
            id_estoque_lote,
            id_arquivo_origem,
            controla_data_referencia,
            controla_data_vencimento,
            data_referencia,
            data_vencimento,
            versao,
            descricao,
            observacao,
            filename,
            filetype,
            bucket,
            filebucket,
            public_url,
            size_bytes,
            size_kbytes,
            size_mbytes,
            size_gbytes,
            downloads,
            datahora_cadastro,
            id_usuario_cadastro,
            datahora_alteracao,
            id_usuario_alteracao,
            datahora_desativacao,
            id_usuario_desativacao,
            motivo_desativacao,
            tag_string
        )
        SELECT
            1,                                              /* ativo */
            af.id_arquivo_tipo,
            af.id_cliente_relacionado,
            af.id_contrato_relacionado,
            af.id_usuario_relacionado,
            af.id_gc_financeiro,
            af.id_movimento,                                /* id_gc_movimento */
            af.id_comex_importacao,
            af.id_comex_invoice,
            af.id_comex_financeiro,
            af.id_atendimento,
            0,                                              /* id_estoque_lote */
            1,                                              /* id_arquivo_origem = cópia da separação */
            af.controla_data_referencia,
            af.controla_data_vencimento,
            af.data_referencia,
            af.data_vencimento,
            af.versao,
            af.descricao,
            af.observacao,
            af.filename,
            af.filetype,
            af.bucket,
            af.filebucket,
            af.public_url,
            af.size_bytes,
            af.size_kbytes,
            af.size_mbytes,
            af.size_gbytes,
            af.downloads,
            ISNULL(af.datahora_cadastro, SYSDATETIME()),
            ISNULL(@IdUsuarioReparo, af.id_usuario_cadastro),
            SYSDATETIME(),
            @IdUsuarioReparo,
            NULL,
            0,
            NULL,
            af.tag_string
        FROM #AnexosFaltantes af;

        SET @QtdInseridos = @@ROWCOUNT;
        PRINT CONCAT('Cópias inseridas: ', @QtdInseridos);

        PRINT '--- Pós-correção: anexos ainda faltantes ---';
        SELECT
            al.id_movimento,
            al.id_estoque_lote,
            al.id_arquivo_lote,
            al.filename,
            al.filebucket
        FROM #AnexosLote al
        WHERE NOT EXISTS (
            SELECT 1
            FROM ged_arquivos p
            WHERE p.id_gc_movimento = al.id_movimento
              AND p.id_arquivo_origem = 1
              AND p.ativo = 1
              AND p.filebucket = al.filebucket
        );
    END
END
ELSE
BEGIN
    PRINT 'Modo AUDIT — nenhuma alteração aplicada. Para corrigir, defina @Modo = ''CORRIGIR'' (recomendado dentro de BEGIN TRAN).';
END
