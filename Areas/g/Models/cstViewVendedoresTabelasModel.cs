using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GdiPlataform.Db;

namespace GdiPlataform.Areas.g.Models
{
    public class cstViewVendedoresTabelasModel
    {
        public g_vendedores g_vendedores { get; set; }
        public List<cstVendedoresTabelasDetalhesModel> allcstVendedoresTabelasDetalhesModel { get; set; }
        public cstViewVendedoresTabelasModel()
        {
            g_vendedores = new Db.g_vendedores();
            allcstVendedoresTabelasDetalhesModel = new List<cstVendedoresTabelasDetalhesModel>();
        }
    }
}