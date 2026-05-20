using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.gc.Models
{
    public class CstUploadList
    {
        public String List1 { get; set; }
        public String List2 { get; set; }
        public String List3 { get; set; }
        public String List4 { get; set; }
        public String List5 { get; set; }

        public CstUploadList()
        {
            List1 = String.Empty;
            List2 = String.Empty;
            List3 = String.Empty;
            List4 = String.Empty;
            List5 = String.Empty;
        }
    }
}