using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GdiPlataform.Db.Metadata
{
    public class g_contratos_aviacaoMetadata
    {
        [DataType(DataType.Date, ErrorMessage = "Campo [Data Assinatura] contém uma data inválida")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public Nullable<System.DateTime> data_assinatura { get; set; }
    }
}