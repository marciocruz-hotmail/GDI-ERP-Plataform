using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.g.Models
{
    public class cstViewPortalClienteFinanceiro
    {
        public List<cstPortalClienteFinanceiro> allCstPortalClienteFinanceiro { get; set; }
        public cstViewPortalClienteFinanceiro()
        {
            allCstPortalClienteFinanceiro = new List<cstPortalClienteFinanceiro>();
        }
    }
}