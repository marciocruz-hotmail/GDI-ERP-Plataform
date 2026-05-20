using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using GdiPlataform.Db;

namespace GdiPlataform.Areas.g.Models
{
    public class CstUploadGed
    {
        public int id_arquivo { get; set; }
        public int versao { get; set; }
        public int id_arquivo_tipo { get; set; }
        public int id_gc_movimento { get; set; }
        public int id_comex_importacao { get; set; }
        public int id_comex_invoice { get; set; }
        public int id_comex_financeiro { get; set; }
        public int id_cliente { get; set; }
        public int id_contrato { get; set; }
        public int id_gc_financeiro { get; set; }
        public int id_estoque_lote { get; set; }
        public int id_atendimento { get; set; }
        public string descricao { get; set; }
        public string observacao { get; set; }
        public bool isContratoCliente { get; set; }
        public bool isComexInvoicePDF { get; set; }
        public bool isLancamentoFinanceiro { get; set; }
        public bool isEstoqueLote { get; set; }
        public bool isCotacaoPedido { get; set; }
        public bool isAtendimento { get; set; }
        public string comex_numero_invoice { get; set; }
        public string comex_sales_order { get; set; }
        public string file_name_new { get; set; }
        public string folder_index_registro { get; set; }
        public string tag1_string { get; set; }
        public HttpPostedFileBase filesource { get; set; }

        [Display(Name = "Dt. Referência")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> data_referencia { get; set; }
        public CstUploadGed()
        {
            id_arquivo = 0;
            versao = 1;
            id_arquivo_tipo = 0;
            id_gc_movimento = 0;
            id_gc_financeiro = 0;
            id_estoque_lote = 0;
            id_comex_importacao = 0;
            id_comex_invoice = 0;
            id_comex_financeiro = 0;
            id_cliente = 0;
            id_contrato = 0;
            id_atendimento = 0;
            descricao = string.Empty;
            observacao = string.Empty;
            folder_index_registro = string.Empty;
            isContratoCliente = false;
            isComexInvoicePDF = false;
            isCotacaoPedido = false;
            isAtendimento = false;
            isEstoqueLote = false;
            comex_numero_invoice = string.Empty;
            comex_sales_order = string.Empty;
            tag1_string = string.Empty;
            data_referencia = null;
        }
    }
}