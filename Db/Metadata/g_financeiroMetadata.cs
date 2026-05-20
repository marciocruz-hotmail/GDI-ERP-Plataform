using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GdiPlataform.Db.Metadata
{
    public class g_financeiroMetadata
    {
        [Display(Name = "Id.")]
        public int id_financeiro { get; set; }

        [Display(Name = "Status")]
        public int id_financeiro_status { get; set; }

        [Display(Name = "Tipo")]
        public short tipo_pag_rec { get; set; }

        [Display(Name = "Cliente")]
        public int id_cliente { get; set; }

        [Display(Name = "Dt Proc")]
        public System.DateTime data_processamento { get; set; }

        [Display(Name = "Dt Venc")]
        public System.DateTime data_vencimento { get; set; }

        [Display(Name = "Total Liq (R$)")]
        [DataType(DataType.Currency)]
        public decimal valor_total_liquido { get; set; }

        [Display(Name = "Total (R$)")]
        [DataType(DataType.Currency)]
        public decimal valor_total_bruto { get; set; }

        [Display(Name = "Descontos (R$)")]
        [DataType(DataType.Currency)]
        public decimal valor_descontos { get; set; }

        [Display(Name = "Encargos (R$)")]
        [DataType(DataType.Currency)]
        public decimal valor_encargos { get; set; }

        [Display(Name = "Avulso")]
        public bool geracao_manual { get; set; }
    }
}