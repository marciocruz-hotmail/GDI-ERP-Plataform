using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GdiPlataform.Db;

namespace GdiPlataform.Areas.g.Models
{
    public class CstViewPerfisAcessosModel
    {
        public g_perfis g_perfis { get; set; }
        public List<CstPerfisAcessos> allCstPerfisAcessos { get; set; }
        public CstViewPerfisAcessosModel()
        {
            g_perfis = new Db.g_perfis();
            allCstPerfisAcessos = new List<CstPerfisAcessos>();
        }
    }
}