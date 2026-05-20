using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.gc.Models
{
    public class CstMovimentoEntradaNFItem
    {
        public int id_movimento_item { get; set; }
        public Decimal sequencia { get; set; }
        public int id_produto { get; set; }
        public string nome_produto { get; set; }
        public decimal valor_unit { get; set; }
        public decimal valor_total { get; set; }
        public string valor_unit_formatado { get; set; }
        public string valor_total_formatado { get; set; }
        public decimal qtd_cdbh_01 { get; set; }
        public decimal qtd_cdbh_02 { get; set; }
        public decimal qtd_cdbh_03 { get; set; }
        public decimal qtd_cdsp_01 { get; set; }
        public decimal qtd_cdsp_02 { get; set; }
        public decimal qtd_cdsp_03 { get; set; }
        public decimal quantidade_geral { get; set; }
        public decimal quantidade_local01 { get; set; }
        public decimal quantidade_local02 { get; set; }
        public decimal quantidade_local03 { get; set; }
        public decimal quantidade_local04 { get; set; }
        public bool set_local01 { get; set; }
        public bool set_local02 { get; set; }
        public bool set_local03 { get; set; }
        public bool set_local04 { get; set; }
        public string produto_externo_codigo { get; set; }
        public string produto_externo_nome { get; set; }
    }
}