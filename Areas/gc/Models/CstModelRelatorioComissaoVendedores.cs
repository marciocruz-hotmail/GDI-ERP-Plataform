using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.gc.Models
{
    public class CstModelRelatorioComissaoVendedores
    {
        public int id_vendedor { get; set; }
        public int vendas_qtd { get; set; }
        public decimal vendas_total_reais { get; set; }
        public int comissao_qtd { get; set; }
        public decimal comissao_total_reais { get; set; }
        public CstModelRelatorioComissaoVendedores(int IdVendedor)
        {
            id_vendedor = IdVendedor;
            vendas_qtd = 0;
            vendas_total_reais = 0;
            comissao_qtd = 0;
            comissao_total_reais = 0;
        }
    }
}