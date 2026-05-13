using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.gc.Models
{
    public class cstModalRelatorio
    {
        public String Field_Text_01 { get; set; }
        public String Field_Text_02 { get; set; }
        public String Field_Text_03 { get; set; }
        public String Field_Text_04 { get; set; }
        public String Field_Text_05 { get; set; }
        public int Field_Int_01 { get; set; }
        public int Field_Int_02 { get; set; }
        public int Field_Int_03 { get; set; }
        public int Field_Int_04 { get; set; }
        public int Field_Int_05 { get; set; }
        public int Field_Int_06 { get; set; }
        public bool Field_Bool_01 { get; set; }
        public bool Field_Bool_02 { get; set; }
        public bool Field_Bool_03 { get; set; }
        public bool Field_Bool_04 { get; set; }
        public bool Field_Bool_05 { get; set; }

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

        public cstModalRelatorio()
        {
            Field_Text_01 = string.Empty;
            Field_Text_02 = string.Empty;
            Field_Text_03 = string.Empty;
            Field_Text_04 = string.Empty;
            Field_Text_05 = string.Empty;
            Field_Int_01 = -1;
            Field_Int_02 = -1;
            Field_Int_03 = -1;
            Field_Int_04 = -1;
            Field_Int_05 = -1;
            Field_Int_06 = -1;
            Field_Bool_01 = false;
            Field_Bool_02 = false;
            Field_Bool_03 = false;
            Field_Bool_04 = false;
            Field_Bool_05 = false;
            Field_Data_01 = null;
            Field_Data_02 = null;
            Field_Data_03 = null;
            Field_Data_04 = null;
            Field_Data_05 = null;
            Field_Bool_01 = false;
            Field_Bool_02 = false;
            Field_Bool_03 = false;
            Field_Bool_04 = false;
            Field_Bool_05 = false;
        }
    }
}