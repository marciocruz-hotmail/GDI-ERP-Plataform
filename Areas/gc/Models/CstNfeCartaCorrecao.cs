using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.gc.Models
{
    public class CstNfeCartaCorrecao
    {
        public bool CartaCorrecaoLiberada { get; set; }
        public int id_movimento_nf { get; set; }
        public string correcao { get; set; }
        public string nf_identificador { get; set; }
        public string msg { get; set; }
    }
}