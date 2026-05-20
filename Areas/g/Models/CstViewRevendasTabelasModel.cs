using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GdiPlataform.Db;

namespace GdiPlataform.Areas.g.Models
{
    public class CstViewRevendasTabelasModel
    {
        public g_revendas g_revendas { get; set; }
        public List<CstRevendasTabelasDetalhesModel> allcstRevendasTabelasDetalhesModel { get; set; }
        public CstViewRevendasTabelasModel()
        {
            g_revendas = new Db.g_revendas();
            allcstRevendasTabelasDetalhesModel = new List<CstRevendasTabelasDetalhesModel>();
        }
    }
}