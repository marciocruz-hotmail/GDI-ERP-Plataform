using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.g.Models
{
    public class CstUpload
    {
        public int id { get; set; }
        public HttpPostedFileBase filesource { get; set; }
    }
}