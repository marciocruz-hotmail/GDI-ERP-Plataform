using GdiPlataform.Areas.gc.Models;
using GdiPlataform.Db;
using GdiPlataform.Lib;
using GdiPlataform.Security;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Globalization;
using System.Linq;
using System.Text;

namespace GdiPlataform.Areas.gc.Services
{
    public class EstoqueInventarioService
    {
        private readonly StringBuilder _msgProcessamento = new StringBuilder();
        public string GetMsgProcessamento() => _msgProcessamento.ToString();
        public bool MovimentarEstoque(int idMovimento, int idTipo,
            GdiPlataformEntities db, bool saveDb)
        {
            _msgProcessamento.Clear();
            try
            {
                if (!CarregarContexto(idMovimento, idTipo, db,
                        out var movimento, out var tipo, out var idLocal, out var itens))
                    return false;

                if (!ValidarItens(itens, idLocal, movimento, db, true, out var produtos)) return false;
                if (!ValidarDuplicidade(tipo, movimento)) return false;
                if (tipo.is_saida && !ValidarEstoqueDisponivel(produtos, idLocal, movimento)) return false;

                ExecutarMovimentacao(itens, produtos, movimento, tipo, idLocal, db);

                if (saveDb) db.SaveChanges();
                return true;
            }
            catch (DbEntityValidationException ex)
            {
                _msgProcessamento.Append(LibExceptions.getDbEntityValidationException(ex));
                return false;
            }
            catch (Exception ex)
            {
                _msgProcessamento.Append(LibExceptions.getExceptionShortMessage(ex));
                return false;
            }
        }

        /// <summary>
        /// Apenas valida se o estoque pode ser movimentado, sem executar.
        /// Reutiliza exatamente as mesmas regras de MovimentarEstoque.
        /// </summary>
        public bool ValidarEstoque(int idMovimento, int idTipo, GdiPlataformEntities db)
        {
            _msgProcessamento.Clear();
            try
            {
                if (!CarregarContexto(idMovimento, idTipo, db,
                        out var movimento, out var tipo, out var idLocal, out var itens))
                    return false;

                if (!ValidarItens(itens, idLocal, movimento, db, false, out var produtos)) return false;
                if (!ValidarDuplicidade(tipo, movimento)) return false;
                if (tipo.is_saida && !ValidarEstoqueDisponivel(produtos, idLocal, movimento)) return false;

                return true;
            }
            catch (DbEntityValidationException ex)
            {
                _msgProcessamento.Append(LibExceptions.getDbEntityValidationException(ex));
                return false;
            }
            catch (Exception ex)
            {
                _msgProcessamento.Append(LibExceptions.getExceptionShortMessage(ex));
                return false;
            }
        }

        // ── Contexto ─────────────────────────────────────────────────────────

        /// <summary>
        /// Carrega e valida os registros base necessários para qualquer operação.
        /// </summary>
        private bool CarregarContexto(
            int idMovimento, int idTipo, GdiPlataformEntities db,
            out gc_movimentos movimento,
            out gc_estoque_movimento_tipo tipo,
            out int idLocal,
            out List<gc_movimentos_itens> itens)
        {
            // ✅ Variáveis locais — sem restrição de uso em lambdas
            var movimentoLocal = db.gc_movimentos.Find(idMovimento);
            var tipoLocal = db.gc_estoque_movimento_tipo.Find(idTipo);

            // Atribui aos out antes de qualquer return
            movimento = movimentoLocal;
            tipo = tipoLocal;
            idLocal = 1;
            itens = null;

            if (movimentoLocal == null)
            {
                _msgProcessamento.Append($" - Movimento nº {idMovimento} Não Localizado!<br/>");
                return false;
            }
            if (tipoLocal == null)
            {
                _msgProcessamento.Append($" - Tipo de Movimentação nº {idTipo} Não Localizado!<br/>");
                return false;
            }

            idLocal = movimentoLocal.id_local_estoque > 0 ? movimentoLocal.id_local_estoque : 1;

            // ✅ Usa a variável local no lambda — compila normalmente
            var itensLocal = db.gc_movimentos_itens
                .Where(i => i.id_movimento == movimentoLocal.id_movimento)
                .ToList();

            itens = itensLocal;

            if (!itensLocal.Any())
            {
                _msgProcessamento.Append($" - Não foram localizados itens para o movimento nº {idMovimento}<br/>");
                return false;
            }

            return true;
        }

        // ── Validações ───────────────────────────────────────────────────────
        private bool ValidarItens(
            List<gc_movimentos_itens> itens,
            int idLocal,
            gc_movimentos movimento,
            GdiPlataformEntities db,
            bool ProcessarBaixaProduto,
            out Dictionary<int, g_produtos> produtos)
        {
            bool erro = false;
            produtos = new Dictionary<int, g_produtos>();

            foreach (var item in itens)
            {
                if (!produtos.TryGetValue(item.id_produto, out var produto))
                {
                    produto = db.g_produtos.Find(item.id_produto);
                    if (produto == null)
                    {
                        _msgProcessamento.Append($" - Produto ID [{item.id_produto}] Não Localizado!<br/>");
                        erro = true;
                        continue;
                    }

                    produto.saldo_01_separado = 0;
                    produto.saldo_03_separado = 0;
                    produtos[item.id_produto] = produto;
                }

                if (!produto.importado)
                {
                    _msgProcessamento.Append($" - Item [{produto.codigo.EmptyIfNull()}] Cadastro Temporário!<br/>");
                    erro = true;
                    continue;
                }

                switch (idLocal)
                {
                    case 1: produto.saldo_01_separado += item.quantidade; break;
                    case 3: produto.saldo_03_separado += item.quantidade; break;
                    default:
                        _msgProcessamento.Append($" - Local de Estoque [{movimento.id_local_estoque.EmptyIfNull()}] Não Identificado!<br/>");
                        erro = true;
                        break;
                }

                if (ProcessarBaixaProduto)
                {
                    if (item.id_estoque_lote_01 == 0)
                    {
                        _msgProcessamento.Append($" - Item [{produto.codigo.EmptyIfNull()}] Sem identificação do Lote!<br/>");
                        erro = true;
                    }
                }
            }

            return !erro;
        }

        private bool ValidarDuplicidade(gc_estoque_movimento_tipo tipo, gc_movimentos movimento)
        {
            if (tipo.is_saida && movimento.movimento_estoque_saida)
            {
                _msgProcessamento.Append($" - Baixa de estoque do Pedido [{movimento.id_movimento}] já processada anteriormente!<br/>");
                return false;
            }
            if (tipo.is_entrada && movimento.movimento_estoque_entrada)
            {
                _msgProcessamento.Append($" - Entrada de estoque do Pedido [{movimento.id_movimento}] já processada anteriormente!<br/>");
                return false;
            }
            return true;
        }

        private bool ValidarEstoqueDisponivel(
            Dictionary<int, g_produtos> produtos,
            int idLocal,
            gc_movimentos movimento)
        {
            bool erro = false;

            foreach (var produto in produtos.Values)
            {
                if (idLocal != 1 && idLocal != 3)
                {
                    _msgProcessamento.Append($" - Local de Estoque [{movimento.id_local_estoque.EmptyIfNull()}] Não Identificado!<br/>");
                    erro = true;
                    continue;
                }

                decimal separado = idLocal == 1 ? produto.saldo_01_separado : produto.saldo_03_separado;
                decimal disponivel = idLocal == 1 ? produto.saldo_01_disponivel : produto.saldo_03_disponivel;
                string local = idLocal == 1 ? "BH" : "SP";

                if (separado > disponivel)
                {
                    _msgProcessamento.Append(
                        $" - Item [{produto.codigo.EmptyIfNull()}] Estoque {local} insuficiente " +
                        $"[Qtd: {separado:0.###} | Disp: {disponivel:0.###}]<br/>");
                    erro = true;
                }
            }

            return !erro;
        }

        // ── Execução ─────────────────────────────────────────────────────────

        private void ExecutarMovimentacao(
            List<gc_movimentos_itens> itens,
            Dictionary<int, g_produtos> produtos,
            gc_movimentos movimento,
            gc_estoque_movimento_tipo tipo,
            int idLocal,
            GdiPlataformEntities db)
        {
            var dataHora = LibDateTime.getDataHoraBrasilia();
            var sqlProdutos = new StringBuilder();
            var sqlLotes = new StringBuilder();

            foreach (var item in itens)
            {
                var produto = produtos[item.id_produto];

                foreach (var lote in ObterLotes(item))
                {
                    var registro = CriarRegistro(item, lote, movimento, tipo, produto, idLocal, dataHora);
                    db.Entry(registro).State = EntityState.Added;
                    AcumularSQL(sqlProdutos, sqlLotes, lote, item.id_produto, idLocal, tipo);
                }
            }

            LibDB.dbQueryExec(sqlProdutos.ToString(), db);
            LibDB.dbQueryExec(sqlLotes.ToString(), db);
        }

        private IEnumerable<CstEstoqueLotesMovimentar> ObterLotes(gc_movimentos_itens item)
        {
            return new (int Id, decimal Qtd)[]
            {
                (item.id_estoque_lote_01, item.lote01_qtd),
                (item.id_estoque_lote_02, item.lote02_qtd),
                (item.id_estoque_lote_03, item.lote03_qtd),
                (item.id_estoque_lote_04, item.lote04_qtd),
                (item.id_estoque_lote_05, item.lote05_qtd),
                (item.id_estoque_lote_06, item.lote06_qtd),
                (item.id_estoque_lote_07, item.lote07_qtd),
                (item.id_estoque_lote_08, item.lote08_qtd),
                (item.id_estoque_lote_09, item.lote09_qtd),
                (item.id_estoque_lote_10, item.lote10_qtd),
                (item.id_estoque_lote_11, item.lote11_qtd),
                (item.id_estoque_lote_12, item.lote12_qtd),
                (item.id_estoque_lote_13, item.lote13_qtd),
                (item.id_estoque_lote_14, item.lote14_qtd),
                (item.id_estoque_lote_15, item.lote15_qtd),
                (item.id_estoque_lote_16, item.lote16_qtd),
                (item.id_estoque_lote_17, item.lote17_qtd),
                (item.id_estoque_lote_18, item.lote18_qtd),
                (item.id_estoque_lote_19, item.lote19_qtd),
                (item.id_estoque_lote_20, item.lote20_qtd),
                (item.id_estoque_lote_21, item.lote21_qtd),
                (item.id_estoque_lote_22, item.lote22_qtd),
                (item.id_estoque_lote_23, item.lote23_qtd),
                (item.id_estoque_lote_24, item.lote24_qtd),
                (item.id_estoque_lote_25, item.lote25_qtd),
                (item.id_estoque_lote_26, item.lote26_qtd),
                (item.id_estoque_lote_27, item.lote27_qtd),
                (item.id_estoque_lote_28, item.lote28_qtd),
                (item.id_estoque_lote_29, item.lote29_qtd),
                (item.id_estoque_lote_30, item.lote30_qtd),
                (item.id_estoque_lote_31, item.lote31_qtd),
                (item.id_estoque_lote_32, item.lote32_qtd),
                (item.id_estoque_lote_33, item.lote33_qtd),
                (item.id_estoque_lote_34, item.lote34_qtd),
                (item.id_estoque_lote_35, item.lote35_qtd),
                (item.id_estoque_lote_36, item.lote36_qtd),
                (item.id_estoque_lote_37, item.lote37_qtd),
                (item.id_estoque_lote_38, item.lote38_qtd),
                (item.id_estoque_lote_39, item.lote39_qtd),
                (item.id_estoque_lote_40, item.lote40_qtd),
                (item.id_estoque_lote_41, item.lote41_qtd),
                (item.id_estoque_lote_42, item.lote42_qtd),
                (item.id_estoque_lote_43, item.lote43_qtd),
                (item.id_estoque_lote_44, item.lote44_qtd),
                (item.id_estoque_lote_45, item.lote45_qtd),
                (item.id_estoque_lote_46, item.lote46_qtd),
                (item.id_estoque_lote_47, item.lote47_qtd),
                (item.id_estoque_lote_48, item.lote48_qtd),
                (item.id_estoque_lote_49, item.lote49_qtd),
                (item.id_estoque_lote_50, item.lote50_qtd),
            }
            .Where(l => l.Id > 0)
            .Select(l => new CstEstoqueLotesMovimentar
            {
                id_estoque_lote = l.Id,
                saldo_movimentar = l.Qtd
            });
        }

        private gc_estoque_movimento CriarRegistro(
            gc_movimentos_itens item,
            CstEstoqueLotesMovimentar lote,
            gc_movimentos movimento,
            gc_estoque_movimento_tipo tipo,
            g_produtos produto,
            int idLocal,
            DateTime dataHora)
        {
            decimal saldoAtual = idLocal == 1 ? produto.saldo_01_disponivel : produto.saldo_03_disponivel;
            decimal qtdMovimento = tipo.is_entrada ? lote.saldo_movimentar : -lote.saldo_movimentar;

            return new gc_estoque_movimento
            {
                id_estoque_movimento_tipo = tipo.id_estoque_movimento_tipo,
                id_produto = item.id_produto,
                id_local_estoque = idLocal,
                id_estoque_lote = lote.id_estoque_lote,
                id_inventario = 0,
                id_inventario_item = 0,
                id_movimento = movimento.id_movimento,
                id_usuario_cadastro = CachePersister.userIdentity.IdUsuario,
                datahora_cadastro = dataHora,
                qtd_disponivel = qtdMovimento,
                saldo_disponivel = saldoAtual + qtdMovimento
            };
        }

        private void AcumularSQL(
            StringBuilder sqlProdutos,
            StringBuilder sqlLotes,
            CstEstoqueLotesMovimentar lote,
            int idProduto,
            int idLocal,
            gc_estoque_movimento_tipo tipo)
        {
            string coluna = idLocal == 1 ? "saldo_01_disponivel" : "saldo_03_disponivel";
            string operacao = tipo.is_entrada ? "+" : "-";
            string qtd = lote.saldo_movimentar.ToString("0.###", CultureInfo.InvariantCulture);

            sqlProdutos.Append(
                $" UPDATE g_produtos SET {coluna} = {coluna} {operacao} {qtd}" +
                $" WHERE id_produto = {idProduto};");

            sqlLotes.Append(
                $" UPDATE gc_estoque_lotes SET {coluna} = {coluna} {operacao} {qtd}" +
                $" WHERE id_estoque_lote = {lote.id_estoque_lote};");
        }
    }
}