using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.gc.Models
{
    public class cstEstoqueEntradaMateriais
    {
        public int id_movimento { get; set; }
        public List<cstEstoqueEntradaMateriaisItem> lista_itens { get; set; }
        public cstEstoqueEntradaMateriais()
        {
            lista_itens = new List<cstEstoqueEntradaMateriaisItem>();
        }

    }
}