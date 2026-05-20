using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GdiPlataform.Db;

namespace GdiPlataform.Areas.g.Models
{
    public class CstViewVendedoresTabelasModel
    {
        public g_vendedores g_vendedores { get; set; }
        public List<CstVendedoresTabelasDetalhesModel> allcstVendedoresTabelasDetalhesModel { get; set; }
        public CstViewVendedoresTabelasModel()
        {
            g_vendedores = new Db.g_vendedores();
            allcstVendedoresTabelasDetalhesModel = new List<CstVendedoresTabelasDetalhesModel>();
        }
    }
}