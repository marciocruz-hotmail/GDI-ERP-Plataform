using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.gc.Models
{
    public class CstEstoqueEntradaMateriais
    {
        public int id_movimento { get; set; }
        public List<CstEstoqueEntradaMateriaisItem> lista_itens { get; set; }
        public CstEstoqueEntradaMateriais()
        {
            lista_itens = new List<CstEstoqueEntradaMateriaisItem>();
        }

    }
}