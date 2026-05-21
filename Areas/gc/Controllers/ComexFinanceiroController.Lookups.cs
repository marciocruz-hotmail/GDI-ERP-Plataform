using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using GdiPlataform.Db;

namespace GdiPlataform.Areas.gc.Controllers
{
    public partial class ComexFinanceiroController
    {
        public void PreencherLookups(int? IdFinanceiro)
        {
            string displaySaldo = string.Empty;
            var recordGcComexFinanceiro = new gc_comex_financeiro { id_importacao = 0, id_invoice = 0 };
            if (IdFinanceiro > 0)
                recordGcComexFinanceiro = db.gc_comex_financeiro.Find(IdFinanceiro) ?? recordGcComexFinanceiro;

            var comboComexImportacoes = new List<SelectListItem>();
            var comboComexImportacoesCrud = new List<SelectListItem>();
            foreach (var item in db.gc_comex_importacoes.Where(i => i.id_importacao > 0 && i.ativo).OrderBy(i => i.numero))
            {
                displaySaldo = "   ( " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", item.cambio_debito).Replace("R$ ", "").Replace("R$", "").Replace("$", "") +
                    " | " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", item.cambio_debito - item.cambio_credito).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + " )";
                comboComexImportacoes.Add(new SelectListItem { Value = item.id_importacao.ToString(), Text = item.numero + displaySaldo });
                if (item.cambio_credito < item.cambio_debito || item.id_importacao == recordGcComexFinanceiro.id_importacao)
                    comboComexImportacoesCrud.Add(new SelectListItem { Value = item.id_importacao.ToString(), Text = item.numero + displaySaldo });
            }
            comboComexImportacoes.Insert(0, new SelectListItem { Value = "0", Text = "[ TODAS ]" });
            ViewBag.comboComexImportacoes = comboComexImportacoes;
            ViewBag.comboComexImportacoesCrud = comboComexImportacoesCrud;

            var comboComexInvoices = new List<SelectListItem>();
            var comboComexInvoicesCrud = new List<SelectListItem>();
            foreach (var item in db.gc_comex_invoices.Where(i => i.id_invoice > 0 && i.ativo).OrderBy(i => i.invoice))
            {
                displaySaldo = "   ( " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", item.cambio_debito).Replace("R$ ", "").Replace("R$", "").Replace("$", "") +
                    " | " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", item.cambio_debito - item.cambio_credito).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + " )";
                comboComexInvoices.Add(new SelectListItem { Value = item.id_invoice.ToString(), Text = item.invoice + displaySaldo });
                if (item.cambio_credito < item.cambio_debito || item.id_invoice == recordGcComexFinanceiro.id_invoice)
                    comboComexInvoicesCrud.Add(new SelectListItem { Value = item.id_invoice.ToString(), Text = item.invoice + displaySaldo });
            }
            comboComexInvoices.Insert(0, new SelectListItem { Value = "0", Text = "[ TODAS ]" });
            ViewBag.comboComexInvoices = comboComexInvoices;
            ViewBag.comboComexInvoicesCrud = comboComexInvoicesCrud;

            ViewBag.comboPagRec = new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "Pagamento" },
                new SelectListItem { Value = "2", Text = "Débito/Fatura" }
            };
        }
    }
}
