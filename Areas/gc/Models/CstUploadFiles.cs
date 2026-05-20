using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.gc.Models
{
    public class CstUploadFiles
    {
        public int IdMovimento { get; set; }
        public int IdMovimentoTipo { get; set; }
        public HttpPostedFileBase FilesourceXML { get; set; }
        public HttpPostedFileBase FilesourcePDF { get; set; }
        public HttpPostedFileBase FilesourceTXT { get; set; }
        public HttpPostedFileBase FilesourceXLXS { get; set; }

        public CstUploadFiles()
        {
            IdMovimento = 0;
            IdMovimentoTipo = 0;
        }
    }
}