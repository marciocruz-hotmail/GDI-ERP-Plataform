using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.g.Models
{
    public class CstFinanceiroLancamentos
    {
        public string ids_lancamentos { get; set; }
        public int id_cliente { get; set; }
        public int id_conta_caixa { get; set; }
        public int id_financeiro_faturamento { get; set; }
        public string descricao { get; set; }
        public string numero_documento { get; set; }
        public decimal valor_despesas_cobranca { get; set; }
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public Nullable<System.DateTime> data_vencimento { get; set; }
        public string[][] registros { get; set; }
    }
}