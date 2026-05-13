using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.g.Models
{
    public class cstModalFiltroAvancado
    {
        public String Field_Text_01 { get; set; }
        public String Field_Text_02 { get; set; }
        public String Field_Text_03 { get; set; }
        public String Field_Text_04 { get; set; }
        public String Field_Text_05 { get; set; }
        public String Field_Text_06 { get; set; }
        public String Field_Text_07 { get; set; }
        public String Field_Text_08 { get; set; }
        public String Field_Text_09 { get; set; }
        public String Field_Text_10 { get; set; }

        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")] 
        public Nullable<System.DateTime> Field_Data_01 { get; set; }
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")] 
        public Nullable<System.DateTime> Field_Data_02 { get; set; }
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")] 
        public Nullable<System.DateTime> Field_Data_03 { get; set; }
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")] 
        public Nullable<System.DateTime> Field_Data_04 { get; set; }
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")] 
        public Nullable<System.DateTime> Field_Data_05 { get; set; }
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")] 
        public Nullable<System.DateTime> Field_Data_06 { get; set; }
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")] 
        public Nullable<System.DateTime> Field_Data_07 { get; set; }
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")] 
        public Nullable<System.DateTime> Field_Data_08 { get; set; }
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")] 
        public Nullable<System.DateTime> Field_Data_09 { get; set; }
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")] 
        public Nullable<System.DateTime> Field_Data_10 { get; set; }

        public cstModalFiltroAvancado()
        {
            Field_Text_01 = string.Empty;
            Field_Text_02 = string.Empty;
            Field_Text_03 = string.Empty;
            Field_Text_04 = string.Empty;
            Field_Text_05 = string.Empty;
            Field_Text_06 = string.Empty;
            Field_Text_07 = string.Empty;
            Field_Text_08 = string.Empty;
            Field_Text_09 = string.Empty;
            Field_Text_10 = string.Empty;
            Field_Data_01 = null;
            Field_Data_02 = null;
            Field_Data_03 = null;
            Field_Data_04 = null;
            Field_Data_05 = null;
            Field_Data_06 = null;
            Field_Data_07 = null;
            Field_Data_08 = null;
            Field_Data_09 = null;
            Field_Data_10 = null;
        }
    }
}