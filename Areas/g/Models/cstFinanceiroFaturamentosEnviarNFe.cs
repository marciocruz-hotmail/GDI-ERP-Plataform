using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.g.Models
{
    public class cstFinanceiroFaturamentosEnviarNFe
    {
        public int idFinanceiroFaturamento { get; set; }
        public bool simulacao { get; set; }
        public cstFinanceiroFaturamentosEnviarNFe()
        {
            simulacao = false;
        }
    }
}