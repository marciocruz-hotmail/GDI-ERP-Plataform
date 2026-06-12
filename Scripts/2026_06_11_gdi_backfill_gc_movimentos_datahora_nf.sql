-- Backfill gc_movimentos.datahora_nf com a primeira NF autorizada do pedido.
-- Executar uma vez no banco de produção/homologação após deploy da correção RoboEnotasNFE.
-- Seguro reexecutar: só atualiza registros com datahora_nf IS NULL.

UPDATE m
SET
    m.datahora_nf = x.primeira_nf_autorizacao
FROM gc_movimentos m
INNER JOIN (
    SELECT
        nf.id_movimento,
        MIN(nf.nf_data_autorizacao) AS primeira_nf_autorizacao
    FROM gc_movimentos_nf nf
    WHERE nf.id_nfe_status IN (8, 17, 22)
      AND nf.nf_data_autorizacao IS NOT NULL
    GROUP BY nf.id_movimento
) x ON x.id_movimento = m.id_movimento
WHERE m.datahora_nf IS NULL;
