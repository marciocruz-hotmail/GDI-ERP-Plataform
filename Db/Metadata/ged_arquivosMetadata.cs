using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GdiPlataform.Db.Metadata
{
    public class ged_arquivosMetadata
    {
        [DataType(DataType.Date, ErrorMessage = "Campo [Dt. Referência] contém uma data inválida")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        [Display(Name = "Dt. Referência")]
        public Nullable<System.DateTime> data_referencia { get; set; }

        [DataType(DataType.Date, ErrorMessage = "Campo [Dt. Vencimento] contém uma data inválida")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        [Display(Name = "Dt. Vencimento")]
        public Nullable<System.DateTime> data_vencimento { get; set; }
    }
}