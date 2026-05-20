using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.gc.Models
{
    public class CstReportHTML
    {
        public string BodyHTML { get; set; }

        public int Identificador { get; set; }
        public CstReportHTML()
        {
            BodyHTML = String.Empty;
            Identificador = 0;
        }
    }
}