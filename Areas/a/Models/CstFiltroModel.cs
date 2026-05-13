using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.a.Models
{
    public class cstFiltroModel
    {
        public string nome_campo { get; set; }

        [Display(Name = "Campo")]
        public string descricao_campo { get; set; }
    }
}