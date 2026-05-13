using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.gc.Models
{
    public class cstReportHTML
    {
        public string BodyHTML { get; set; }

        public int Identificador { get; set; }
        public cstReportHTML()
        {
            BodyHTML = String.Empty;
            Identificador = 0;
        }
    }
}