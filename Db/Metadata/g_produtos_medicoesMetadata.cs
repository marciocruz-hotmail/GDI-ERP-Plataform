using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GdiPlataform.Db.Metadata
{
    public class g_produtos_medicoesMetadata
    {
        [DataType(DataType.Date, ErrorMessage = "Campo [Data Medição] contém uma data inválida")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public System.DateTime data_medicao { get; set; }


        [DisplayFormat(DataFormatString = "{0:0.00000}", ApplyFormatInEditMode = true)]
        public decimal tensao { get; set; }


    }
}