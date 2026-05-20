using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GdiPlataform.Db.Metadata
{
    public class gc_financeiro_lancamentosMetadata
    {
        [Display(Name = "Dt. Ocorr.")]
        [DataType(DataType.Date, ErrorMessage = "Campo [Data Ocorrência] contém uma data inválida")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public System.DateTime data_pagamento { get; set; }

        [Display(Name = "Dt. Venc.")]
        [DataType(DataType.Date, ErrorMessage = "Campo [Data Vencimento] contém uma data inválida")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public System.DateTime data_vencimento { get; set; }

        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public Nullable<System.DateTime> data_vencimento_original { get; set; }

        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public Nullable<System.DateTime> negociacao_data_limite { get; set; }



    }
}