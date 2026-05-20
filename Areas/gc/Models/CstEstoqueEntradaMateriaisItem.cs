using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.gc.Models
{
    public class CstEstoqueEntradaMateriaisItem
    {
        public int id_movimento { get; set; }
        public int id_movimento_item { get; set; }
        public int produto_id { get; set; }
        public string produto_descricao { get; set; }
        public int produto_quantidade_nf { get; set; }
        public int id_invoice_item { get; set; }
        public string invoice_cliente { get; set; }
        public string invoice_os { get; set; }
        public string item_obs { get; set; }
        public int quantidade_recebido { get; set; }
        public int quantidade_disponivel { get; set; }
        public int quantidade_quarentena { get; set; }
        public CstEstoqueEntradaMateriaisItem()
        {
            id_movimento = 0;
            id_movimento_item = 0;
            produto_id = 0;
            produto_descricao = string.Empty;
            produto_quantidade_nf = 0;
            id_invoice_item = 0;
            invoice_cliente = string.Empty;
            invoice_os = string.Empty;
            item_obs = string.Empty;
            quantidade_recebido = 0;
            quantidade_disponivel = 0;
            quantidade_quarentena = 0;
        }
    }
}