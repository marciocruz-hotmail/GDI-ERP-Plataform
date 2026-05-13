using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GdiPlataform.Db;

namespace GdiPlataform.Areas.g.Models
{
    public class cstViewRevendasTabelasModel
    {
        public g_revendas g_revendas { get; set; }
        public List<cstRevendasTabelasDetalhesModel> allcstRevendasTabelasDetalhesModel { get; set; }
        public cstViewRevendasTabelasModel()
        {
            g_revendas = new Db.g_revendas();
            allcstRevendasTabelasDetalhesModel = new List<cstRevendasTabelasDetalhesModel>();
        }
    }
}