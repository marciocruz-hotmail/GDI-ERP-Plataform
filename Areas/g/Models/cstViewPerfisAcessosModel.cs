using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GdiPlataform.Db;

namespace GdiPlataform.Areas.g.Models
{
    public class cstViewPerfisAcessosModel
    {
        public g_perfis g_perfis { get; set; }
        public List<cstPerfisAcessos> allCstPerfisAcessos { get; set; }
        public cstViewPerfisAcessosModel()
        {
            g_perfis = new Db.g_perfis();
            allCstPerfisAcessos = new List<cstPerfisAcessos>();
        }
    }
}