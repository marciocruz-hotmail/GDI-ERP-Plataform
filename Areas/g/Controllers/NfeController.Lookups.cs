using System;
using System.Linq;
using System.Web.Mvc;
using GdiPlataform.Db;
using GdiPlataform.Lib;
using GdiPlataform.Lib.Lookups;

namespace GdiPlataform.Areas.g.Controllers
{
    public partial class NfeController
    {
        private ILookupQueryService NfeLookups => LookupQueryServiceAccessor.Current;

        private void PreencherLookupsCreateEdit(g_nfe record)
        {
            ViewBag.comboCidade = NfeLookups.GetComboGCidadesAtivas(db);
            ViewBag.comboUF = NfeLookups.GetComboGUf(db);

            g_nfe_status st = db.g_nfe_status.FirstOrDefault(s => s.id_nfe_status == record.id_nfe_status);
            ViewBag.NfeStatus = st != null ? st.descricao.EmptyIfNull().ToString() : String.Empty;
            ViewBag.NfeKey = record.nfe_key.EmptyIfNull().ToString();
            ViewBag.UrlPDF = record.url_pdf.EmptyIfNull().ToString();
            ViewBag.UrlXML = record.url_xml.EmptyIfNull().ToString();
        }
    }
}
