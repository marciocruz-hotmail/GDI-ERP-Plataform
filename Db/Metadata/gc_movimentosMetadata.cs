using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GdiPlataform.Db.Metadata
{
    public class gc_movimentosMetadata
    {
        [Display(Name = "Id.")]
        public int id_movimento { get; set; }

        [Display(Name = "Dt. Venc.")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> data_vencimento { get; set; }

        [Display(Name = "Dt. Expedição")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> datahora_expedicao { get; set; }

        [Display(Name = "Dt. Previsão Entrega")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> datahora_entrega_previsao { get; set; }

        [Display(Name = "Dt. Entrega")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> datahora_entrega { get; set; }

        [Display(Name = "Frete")]
        [RegularExpression(@"^\d+.?\d{0,2}$", ErrorMessage = "Campo [Frete] contém um valor inválido!")]
        public decimal frete_valor { get; set; }

        [DisplayFormat(DataFormatString = "{0:0.0000}", ApplyFormatInEditMode = true)]
        [RegularExpression(@"^\d+.?\d{0,4}$", ErrorMessage = "Campo [Cotação Dolar Oficial Venda] contém um valor inválido!")]
        public decimal cotacao_dolar_oficial_venda { get; set; }

        [DisplayFormat(DataFormatString = "{0:0.0000}", ApplyFormatInEditMode = true)]
        [RegularExpression(@"^\d+.?\d{0,4}$", ErrorMessage = "Campo [Cotação Dolar] contém um valor inválido!")]
        public decimal cotacao_dolar_venda { get; set; }

        [DisplayFormat(DataFormatString = "{0:0.0000}", ApplyFormatInEditMode = true)]
        [RegularExpression(@"^\d+.?\d{0,4}$", ErrorMessage = "Campo [FOB SCross (US$)] contém um valor inválido!")]
        public decimal valor_fob_scross { get; set; }

        [DisplayFormat(DataFormatString = "{0:0.0000}", ApplyFormatInEditMode = true)]
        [RegularExpression(@"^\d+.?\d{0,4}$", ErrorMessage = "Campo [FOB Brasil (US$)] contém um valor inválido!")]
        public decimal valor_fob_brasil { get; set; }

        [DisplayFormat(DataFormatString = "{0:0.0000}", ApplyFormatInEditMode = true)]
        [RegularExpression(@"^\d+.?\d{0,4}$", ErrorMessage = "Campo [FOB Brasil (US$)] contém um valor inválido!")]
        public decimal cotacao_dolar_compra { get; set; }

        [Display(Name = "Dt. Pós-Venda")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> posvenda_datahora_contato { get; set; }
    }
}
