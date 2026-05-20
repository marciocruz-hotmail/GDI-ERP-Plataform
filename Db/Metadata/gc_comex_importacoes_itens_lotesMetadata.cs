using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GdiPlataform.Db.Metadata
{
    public class gc_comex_importacoes_itens_lotesMetadata
    {
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")] 
        public Nullable<System.DateTime> lote1_data_validade { get; set; }
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public Nullable<System.DateTime> lote2_data_validade { get; set; }
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")] 
        public Nullable<System.DateTime> lote3_data_validade { get; set; }
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")] 
        public Nullable<System.DateTime> lote4_data_validade { get; set; }
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")] 
        public Nullable<System.DateTime> lote5_data_validade { get; set; }
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")] 
        public Nullable<System.DateTime> lote6_data_validade { get; set; }
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")] 
        public Nullable<System.DateTime> lote7_data_validade { get; set; }
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")] 
        public Nullable<System.DateTime> lote8_data_validade { get; set; }
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")] 
        public Nullable<System.DateTime> lote9_data_validade { get; set; }
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")] 
        public Nullable<System.DateTime> lote10_data_validade { get; set; }

    }
}