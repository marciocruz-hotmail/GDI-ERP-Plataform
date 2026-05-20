using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.gc.Models
{
    public class CstComexEntradaMateriais
    {
        public int id_importacao { get; set; }
        public List<CstComexEntradaMateriaisItens> allItens { get; set; }
        public CstComexEntradaMateriais()
        {
            allItens = new List<CstComexEntradaMateriaisItens>();
        }
    }
}