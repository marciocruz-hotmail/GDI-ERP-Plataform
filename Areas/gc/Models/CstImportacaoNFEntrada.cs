using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.gc.Models
{
    public class CstImportacaoNFEntrada
    {
        [Display(Name = "Tipo Movimento")]
        public int id_movimento_tipo { get; set; }
        public int id_movimento_pedido { get; set; }
        public HttpPostedFileBase filesourceXML { get; set; }
        public HttpPostedFileBase filesourcePDF { get; set; }
    }
}