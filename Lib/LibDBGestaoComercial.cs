using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using GdiPlataform.Db;
using GdiPlataform.Security;
using GdiPlataform.Lib;

namespace GdiPlataform.Areas.gc.Controllers
{
    public class StartDBGestaoComercial
    {
        private GdiPlataformEntities db;
        DateTime DataHoraAtual;
        public StartDBGestaoComercial()
        {
            DataHoraAtual = LibDateTime.getDataHoraBrasilia().Date;
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        public gc_movimentos TotalizarMovimentosItens(int IdEstoqueCD, gc_movimentos RecordMovimentoOrigem, List<gc_movimentos_itens> ListaItens)
        {
            try
            {
                gc_movimentos RecordMovimentoNovo = LibDB.CloneTObject(RecordMovimentoOrigem);
                Decimal ValorTotalProdutosGeral = RecordMovimentoNovo.valor_total_produtos;
                Decimal ValorTotalDespesasAcessoriasGeral = RecordMovimentoNovo.despesas_acessorias_valor;
                RecordMovimentoNovo.icms_vbc = 0;
                RecordMovimentoNovo.icms_vicms = 0;
                RecordMovimentoNovo.desconto_valor = 0;
                RecordMovimentoNovo.valor_total_liquido = 0;
                RecordMovimentoNovo.valor_total_produtos = 0;
                RecordMovimentoNovo.frete_valor = 0;
                RecordMovimentoNovo.seguro_valor = 0;
                foreach (var RecordItem in ListaItens)
                {
                    RecordMovimentoNovo.icms_vbc += RecordItem.icms_vbc;
                    RecordMovimentoNovo.icms_vicms += RecordItem.icms_vicms;
                    RecordMovimentoNovo.desconto_valor += RecordItem.valor_desconto;
                    RecordMovimentoNovo.valor_total_liquido += RecordItem.valor_total;
                    RecordMovimentoNovo.valor_total_produtos += RecordItem.valor_total;
                    RecordMovimentoNovo.frete_valor += RecordItem.valor_frete;
                    RecordMovimentoNovo.seguro_valor += RecordItem.valor_seguro;
                }
                if (ValorTotalDespesasAcessoriasGeral > 0) { RecordMovimentoNovo.despesas_acessorias_valor = (((RecordMovimentoNovo.valor_total_produtos * 100) / ValorTotalProdutosGeral) * (ValorTotalDespesasAcessoriasGeral / 100)); };
                RecordMovimentoNovo.documento_numero = null;
                RecordMovimentoNovo.nf_serie = "0";
                RecordMovimentoNovo.nf_numero = "0";
                RecordMovimentoNovo.nf_data_geracao = null;
                RecordMovimentoNovo.id_usuario_alteracao = 0;
                RecordMovimentoNovo.nf_chave = null;
                RecordMovimentoNovo.nf_data_recebimento = null;
                RecordMovimentoNovo.nf_s3_pdf = 0;
                RecordMovimentoNovo.nf_s3_xml = 0;
                RecordMovimentoNovo.id_estoque_cd = IdEstoqueCD;
                RecordMovimentoNovo.id_movimento_ref = RecordMovimentoOrigem.id_movimento;
                RecordMovimentoNovo.id_movimento_tipo = RecordMovimentoOrigem.id_movimento_tipo + 1;
                RecordMovimentoNovo.valor_total_bruto = RecordMovimentoNovo.valor_total_produtos + RecordMovimentoNovo.frete_valor + RecordMovimentoNovo.seguro_valor + RecordMovimentoNovo.despesas_acessorias_valor - RecordMovimentoNovo.desconto_valor;
                RecordMovimentoNovo.qtd_itens = ListaItens.Count();
                RecordMovimentoNovo.qtd_produtos = ListaItens.Count();
                RecordMovimentoNovo.entrada_nfe_processada = true;
                RecordMovimentoNovo.id_coligada = 1;
                if ((IdEstoqueCD == 1) || (IdEstoqueCD == 2)) 
                { 
                    RecordMovimentoNovo.id_filial = 1; 
                }
                else if ((IdEstoqueCD == 3) || (IdEstoqueCD == 4)) 
                { 
                    RecordMovimentoNovo.id_filial = 2; 
                }
                db.gc_movimentos.Add(RecordMovimentoNovo);
                db.SaveChanges();
                return RecordMovimentoNovo;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public bool DesdobrarMovimentosItens(int IdMovimento, int IdMovimentoOrigem, List<gc_movimentos_itens> ListaItens)
        {
            int Sequencia = 0;
            try
            {
                foreach (var RecordItem in ListaItens)
                {
                    Sequencia += 1;
                    RecordItem.id_movimento = IdMovimento;
                    RecordItem.id_movimento_ref = IdMovimentoOrigem;
                    RecordItem.sequencia = Sequencia;
                    db.gc_movimentos_itens.Add(RecordItem);
                }
                db.SaveChanges();
                return true;
            }
            catch (Exception)
            {
                throw;
            }
            
        }

    }
}