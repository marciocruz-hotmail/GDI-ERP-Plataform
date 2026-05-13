using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.g.Models
{
    public class cstImportacoesBancarias
    {
        [Display(Name = "Conta Caixa")]
        public int id_conta_caixa { get; set; }
        public HttpPostedFileBase filesource { get; set; }
    }
}