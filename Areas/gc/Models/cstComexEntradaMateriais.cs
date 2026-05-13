using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.gc.Models
{
    public class cstComexEntradaMateriais
    {
        public int id_importacao { get; set; }
        public List<cstComexEntradaMateriaisItens> allItens { get; set; }
        public cstComexEntradaMateriais()
        {
            allItens = new List<cstComexEntradaMateriaisItens>();
        }
    }
}