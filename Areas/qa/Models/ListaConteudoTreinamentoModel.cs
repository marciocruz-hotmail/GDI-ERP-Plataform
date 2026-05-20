using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.qa.Models
{
    public class ListaConteudoTreinamentoModel
    {
        public int id_arquivo_video1 { get; set; }
        public string titulo_video1 { get; set; }
        public string presigned_url_video1 { get; set; }
        
        public int id_arquivo_pdf1 { get; set; }
        public string titulo_pdf1 { get; set; }
        public string presigned_url_pdf1 { get; set; }
        public int id_arquivo_pdf2 { get; set; }
        public string titulo_pdf2 { get; set; }
        public string presigned_url_pdf2 { get; set; }
        public int id_arquivo_pdf3 { get; set; }
        public string titulo_pdf3 { get; set; }
        public string presigned_url_pdf3 { get; set; }
    }
}