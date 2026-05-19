using GdiPlataform.Controllers;
using GdiPlataform.Db;
using GdiPlataform.Lib;
using GdiPlataform.Security;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace GdiPlataform.Areas.gc.Controllers
{
    [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_ComexFinanceiro_*,gc_ComexFinanceiro_Default")]
    public class ComexFinanceiroController : Controller
    {
        private GdiPlataformEntities db;
        public ComexFinanceiroController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_ComexFinanceiro_*,gc_ComexFinanceiro_Actionread")]
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-sack-dollar", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Comex - Pagamentos Invoices";
            PreencherLookups(0);
            return View();
        }

        #region GetDadosPagamentos
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_ComexFinanceiro_*,gc_ComexFinanceiro_Actionread")]
        public ActionResult GetDadosPagamentos(jQueryDataTableParamModel param)
        {
            if (param == null) { param = new jQueryDataTableParamModel(); }
            string errorMessage = "";
            string stackTrace = "";
            string saldoContaImportacao = "0,00";

            try
            {
                // -----------------------------
                // Parse / paging
                // -----------------------------
                int start = param.iDisplayStart;
                int length = (param.iDisplayLength <= 0 ? 100 : param.iDisplayLength);

                int idImportacao = 0;
                int idInvoice = 0;
                int.TryParse(param.yesCustomField01.EmptyIfNull().ToString().Trim(), out idImportacao);
                int.TryParse(param.yesCustomField02.EmptyIfNull().ToString().Trim(), out idInvoice);

                // -----------------------------
                // Base query (LINQ + AsNoTracking)
                // -----------------------------
                IQueryable<Db.gc_comex_financeiro> query = db.gc_comex_financeiro
                    .AsNoTracking()
                    .Where(f => f.ativo == true);

                if (idImportacao > 0) query = query.Where(f => f.id_importacao == idImportacao);
                if (idInvoice > 0) query = query.Where(f => f.id_invoice == idInvoice);

                // Total para o DataTables (já com filtros)
                int totalRecords = query.Count();
                int totalDisplayRecords = totalRecords;

                // Ordenação e paginação (OrderBy antes do Skip evita erro LINQ to Entities)
                var page = query
                    .OrderByDescending(f => f.data_pagamento)
                    .Skip(start)
                    .Take(length)
                    .Select(f => new
                    {
                        f.id_financeiro,
                        f.tipo_pag_rec,
                        f.data_pagamento,
                        f.descricao,
                        f.id_importacao,
                        f.id_invoice,
                        f.valor_pago
                    })
                    .ToList();

                // -----------------------------
                // Lookups apenas dos IDs da página
                // -----------------------------
                var idsImportacoes = page.Select(x => x.id_importacao).Distinct().ToList();
                var idsInvoices = page.Select(x => x.id_invoice).Distinct().ToList();

                var importacoes = db.gc_comex_importacoes
                    .AsNoTracking()
                    .Where(i => idsImportacoes.Contains(i.id_importacao))
                    .Select(i => new { i.id_importacao, i.numero })
                    .ToList()
                    .ToDictionary(x => x.id_importacao, x => x.numero);

                var invoices = db.gc_comex_invoices
                    .AsNoTracking()
                    .Where(i => idsInvoices.Contains(i.id_invoice))
                    .Select(i => new { i.id_invoice, i.invoice })
                    .ToList()
                    .ToDictionary(x => x.id_invoice, x => x.invoice);

                // -----------------------------
                // Saldo conta importação (mantive seu SQL por DataTable, mas blindado)
                // -----------------------------
                try
                {
                    const string sqlCambioDebito =
                        "select (sum(i.cambio_debito) - sum(i.cambio_credito)) as saldo_devedor " +
                        "from gc_comex_importacoes i where id_importacao > 0 and ativo = 1";

                    DataTable tableSaldo = LibDB.GetDataTable(sqlCambioDebito, db);
                    if (tableSaldo.Rows.Count > 0)
                    {
                        decimal saldoDevedor = 0m;
                        decimal.TryParse(tableSaldo.Rows[0]["saldo_devedor"].EmptyIfNull().ToString().Trim(), out saldoDevedor);

                        // seu código multiplica por -1
                        saldoDevedor *= -1m;

                        saldoContaImportacao = string.Format(
                                CultureInfo.GetCultureInfo("pt-BR"),
                                "{0:C}",
                                saldoDevedor
                            )
                            .Replace("R$ ", "")
                            .Replace("R$", "")
                            .Replace("$", "");
                    }
                }
                catch
                {
                    saldoContaImportacao = "Erro";
                }

                // -----------------------------
                // Montagem aaData
                // -----------------------------
                var list = new List<string[]>(page.Count);

                foreach (var i in page)
                {
                    string numeroImportacao = importacoes.TryGetValue(i.id_importacao, out var nImp) ? (nImp ?? "") : "";
                    string numeroInvoice = invoices.TryGetValue(i.id_invoice, out var nInv) ? (nInv ?? "") : "";

                    string iconeTipoPagRec;
                    decimal valor = i.valor_pago;

                    if (i.tipo_pag_rec == 1)
                    {
                        iconeTipoPagRec = LibIcons.getIcon("fa-solid fa-sack-dollar", "Pagamento", "#008000", "");
                    }
                    else
                    {
                        iconeTipoPagRec = LibIcons.getIcon("fa-solid fa-file-invoice", "Débito/Fatura", "#cc0000", "");
                        valor = valor * -1m; // não altera entidade, só o valor exibido
                    }

                    list.Add(new[]
                    {
                "", // Coluna de Seleção
                i.id_financeiro.ToString(),
                iconeTipoPagRec,
                i.data_pagamento.ToString("dd/MM/yy"),
                i.descricao.EmptyIfNull().ToString(),
                numeroImportacao,
                numeroInvoice,
                valor.ToString("###,###,##0.00"),
                "" // Botão Editar
            });
                }

                return Json(new
                {
                    errorMessage = "",
                    stackTrace = "",
                    yesDisplayField01 = saldoContaImportacao,
                    sEcho = param.sEcho,
                    iTotalRecords = totalRecords,
                    iTotalDisplayRecords = totalDisplayRecords,
                    aaData = list
                }, JsonRequestBehavior.AllowGet);
            }
            catch (DbEntityValidationException ex)
            {
                errorMessage = LibExceptions.getDbEntityValidationException(ex);
                stackTrace = ex.ToString();
            }
            catch (WebException ex)
            {
                errorMessage = LibExceptions.getWebException(ex);
                stackTrace = ex.ToString();
            }
            catch (Exception ex)
            {
                errorMessage = LibExceptions.getExceptionShortMessage(ex);
                stackTrace = ex.ToString();
            }

            // ✅ retorna no padrão do DataTables, mas com erro real
            return Json(new
            {
                errorMessage = errorMessage,
                severity = "error",
                stackTrace = stackTrace, // se quiser ocultar em produção, devolva ""
                yesDisplayField01 = saldoContaImportacao,
                sEcho = param.sEcho,
                iTotalRecords = 0,
                iTotalDisplayRecords = 0,
                aaData = new List<string[]>()
            }, JsonRequestBehavior.AllowGet);
        }

        public void PreencherLookups(int? IdFinanceiro)
        {
            String DisplaySaldo = string.Empty;
            gc_comex_financeiro record_gc_comex_financeiro = new Db.gc_comex_financeiro();
            record_gc_comex_financeiro.id_importacao = 0;
            record_gc_comex_financeiro.id_invoice = 0;
            if (IdFinanceiro > 0) { record_gc_comex_financeiro = db.gc_comex_financeiro.Find(IdFinanceiro); }

            var comboComexImportacoes = new List<SelectListItem>();
            var comboComexImportacoesCrud = new List<SelectListItem>();
            try
            {
                IQueryable<gc_comex_importacoes> listaComexImportacoes = db.gc_comex_importacoes.Where(i => (i.id_importacao > 0) && (i.ativo == true)).OrderBy(i => i.numero);
                comboComexImportacoes.Add(new SelectListItem { Value = "0", Text = "[ TODAS ]" });
                foreach (gc_comex_importacoes item_gc_comex_importacoes in listaComexImportacoes)
                {
                    DisplaySaldo = "   ( " +  string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", item_gc_comex_importacoes.cambio_debito).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + 
                                   " | " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", (item_gc_comex_importacoes.cambio_debito - item_gc_comex_importacoes.cambio_credito)).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + " )";
                    comboComexImportacoes.Add(new SelectListItem { Value = item_gc_comex_importacoes.id_importacao.ToString(), Text = item_gc_comex_importacoes.numero.ToString() + DisplaySaldo });
                    if ((item_gc_comex_importacoes.cambio_credito < item_gc_comex_importacoes.cambio_debito) || (item_gc_comex_importacoes.id_importacao == record_gc_comex_financeiro.id_importacao))
                    {
                        comboComexImportacoesCrud.Add(new SelectListItem { Value = item_gc_comex_importacoes.id_importacao.ToString(), Text = item_gc_comex_importacoes.numero.ToString() + DisplaySaldo });
                    }
                }
            }
            finally { }
            ViewBag.comboComexImportacoes = comboComexImportacoes;
            ViewBag.comboComexImportacoesCrud = comboComexImportacoesCrud;

            var comboComexInvoices = new List<SelectListItem>();
            var comboComexInvoicesCrud = new List<SelectListItem>();
            try
            {
                IQueryable<gc_comex_invoices> listaComexInvoices = db.gc_comex_invoices.Where(i => (i.id_invoice > 0) && (i.ativo == true)).OrderBy(i => i.invoice);
                comboComexInvoices.Add(new SelectListItem { Value = "0", Text = "[ TODAS ]" });
                foreach (gc_comex_invoices item_gc_comex_invoices in listaComexInvoices)
                {
                    DisplaySaldo = "   ( " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", item_gc_comex_invoices.cambio_debito).Replace("R$ ", "").Replace("R$", "").Replace("$", "") +
                                   " | " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", (item_gc_comex_invoices.cambio_debito - item_gc_comex_invoices.cambio_credito)).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + " )";
                    comboComexInvoices.Add(new SelectListItem { Value = item_gc_comex_invoices.id_invoice.ToString(), Text = item_gc_comex_invoices.invoice.ToString() + DisplaySaldo });
                    if ((item_gc_comex_invoices.cambio_credito < item_gc_comex_invoices.cambio_debito) || (item_gc_comex_invoices.id_invoice == record_gc_comex_financeiro.id_invoice))
                    {
                        comboComexInvoicesCrud.Add(new SelectListItem { Value = item_gc_comex_invoices.id_invoice.ToString(), Text = item_gc_comex_invoices.invoice.ToString() + DisplaySaldo });
                    }
                }
            }
            finally { }
            ViewBag.comboComexInvoices = comboComexInvoices;
            ViewBag.comboComexInvoicesCrud = comboComexInvoicesCrud;

            var comboPagRec = new List<SelectListItem>();
            comboPagRec.Add(new SelectListItem { Value = "1", Text = "Pagamento" });
            comboPagRec.Add(new SelectListItem { Value = "2", Text = "Débito/Fatura" });
            ViewBag.comboPagRec = comboPagRec;
        }
        #endregion
        public ActionResult ModalCreateEditGcComexFinanceiro(int? IdFinanceiro)
        {
            gc_comex_financeiro record_gc_comex_financeiro;

            if (IdFinanceiro > 0)
            {
                record_gc_comex_financeiro = db.gc_comex_financeiro.Find(IdFinanceiro);
                ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Edição de Lançamento - " + record_gc_comex_financeiro.id_financeiro.EmptyIfNull().ToString() + "</b>";
            }
            else
            {
                DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
                record_gc_comex_financeiro = new Db.gc_comex_financeiro();
                record_gc_comex_financeiro.ativo = true;
                record_gc_comex_financeiro.tipo_pag_rec = 1;
                record_gc_comex_financeiro.data_pagamento = DataHoraAtual;
                //if (IdLancamento < 0) { record_gc_financeiro_lancamentos.id_conta_caixa = IdLancamento.GetValueOrDefault() * -1; };
                ViewBag.Title = LibIcons.getIcon("fa-solid fa-sack-dollar", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Novo Lançamento</b>";
            }
            PreencherLookups(IdFinanceiro);
            return View(record_gc_comex_financeiro);
        }
        public ActionResult AjaxModalCreateEditGcComexFinanceiro(gc_comex_financeiro view_gc_comex_financeiro)
        {
            bool sucesso = false;
            int qtdInconsistencias = 0;
            String msgRetorno = "";
            try
            {
                // Validações Gerais
                if (ModelState.IsValid)
                {
                    if (view_gc_comex_financeiro.descricao.EmptyIfNull().ToString().Length == 0)
                    {
                        qtdInconsistencias += 1;
                        msgRetorno += " - [Descrição] é de preenchimento obrigatório!<br/>";
                    }
                    if (view_gc_comex_financeiro.valor_pago <= 0)
                    {
                        qtdInconsistencias += 1;
                        msgRetorno += " - [R$ Valor] Deverá ser informado corretamente!<br/>";
                    }
                    if (view_gc_comex_financeiro.tipo_pag_rec == 1) // Pagamento
                    {
                        if (view_gc_comex_financeiro.agente_financeiro.EmptyIfNull().ToString().Length == 0)
                        {
                            qtdInconsistencias += 1;
                            msgRetorno += " - [Agente Financeiro] é de preenchimento obrigatório para lançamentos de Pagamentos!<br/>";
                        }
                        if (view_gc_comex_financeiro.id_importacao == 0)
                        {
                            qtdInconsistencias += 1;
                            msgRetorno += " - [Importacao] é de preenchimento obrigatório para lançamentos de Pagamentos!<br/>";
                        }
                        if (view_gc_comex_financeiro.id_invoice == 0)
                        {
                            qtdInconsistencias += 1;
                            msgRetorno += " - [Invoice] é de preenchimento obrigatório para lançamentos de Pagamentos!<br/>";
                        }
                    }
                }
                else
                {
                    qtdInconsistencias += 1;
                    msgRetorno = String.Join(Environment.NewLine, ModelState.Values.SelectMany(v => v.Errors).Select(v => v.ErrorMessage + " " + v.Exception + "<br/>"));
                }

                // Validações de Saldos (Invoices e Importações)

                if ((view_gc_comex_financeiro.tipo_pag_rec == 1) && (qtdInconsistencias == 0))
                {
                    Decimal CambioDebitoDI = 0;
                    Decimal SaldoPagoInvoices = 0;
                    gc_comex_importacoes record_gc_comex_importacoes = db.gc_comex_importacoes.Find(view_gc_comex_financeiro.id_importacao);
                    gc_comex_invoices record_gc_comex_invoices = db.gc_comex_invoices.Find(view_gc_comex_financeiro.id_invoice);
                    CambioDebitoDI = record_gc_comex_importacoes.cambio_debito;

                    if (view_gc_comex_financeiro.id_financeiro == 0) // NOVO LANÇAMENTO
                    {
                        // Validar Saldo Já Pago das Invoices
                        String SentencaSQL = " select sum(i.cambio_credito) as 'SaldoPago' " +
                                             " from gc_comex_invoices i " +
                                             " where i.ativo = 1 " +
                                             " and i.id_importacao = " + view_gc_comex_financeiro.id_importacao.ToString();
                        String _SaldoPagoInvoices = LibDB.dbQueryValue(SentencaSQL, db);
                        Decimal.TryParse(_SaldoPagoInvoices, out SaldoPagoInvoices);
                        if ((SaldoPagoInvoices + view_gc_comex_financeiro.valor_pago) > CambioDebitoDI)
                        {
                            qtdInconsistencias += 1;
                            msgRetorno += " - A Soma do valor informado (" + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", view_gc_comex_financeiro.valor_pago).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + "), ";
                            msgRetorno += " com os valores já pagos anteriormente em invoices da mesma DI (" + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", SaldoPagoInvoices).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + "), ";
                            msgRetorno += " é MAIOR do que o valor a pagar da DI (" + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", CambioDebitoDI).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + ")!";
                        }
                        else
                        {
                            if (view_gc_comex_financeiro.valor_pago > ((record_gc_comex_invoices.cambio_debito/100)*110)) // Limite de 10% a mais que o valor da DI
                            {
                                qtdInconsistencias += 1;
                                msgRetorno += " - O valor informado (" + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", view_gc_comex_financeiro.valor_pago).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + "), ";
                                msgRetorno += " é MAIOR do que o valor a pagar da Invoice (" + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", record_gc_comex_invoices.cambio_debito).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + ")!";
                            }
                        }
                    }
                    else
                    {
                        // Validar Saldo Já Pago das Invoices
                        String SentencaSQL = " select sum(i.cambio_credito) as 'SaldoPago' " +
                                             " from gc_comex_invoices i " +
                                             " where i.ativo = 1 " +
                                             " and i.id_importacao = " + view_gc_comex_financeiro.id_importacao.ToString() +
                                             " and i.id_financeiro != " + view_gc_comex_financeiro.id_financeiro.ToString();
                        String _SaldoPagoInvoices = LibDB.dbQueryValue(SentencaSQL, db);
                        Decimal.TryParse(_SaldoPagoInvoices, out SaldoPagoInvoices);
                        if ((SaldoPagoInvoices + view_gc_comex_financeiro.valor_pago) > CambioDebitoDI)
                        {
                            qtdInconsistencias += 1;
                            msgRetorno += " - A Soma do valor informado (" + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", view_gc_comex_financeiro.valor_pago).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + "), ";
                            msgRetorno += " com os valores já pagos anteriormente em invoices da mesma DI (" + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", SaldoPagoInvoices).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + "), ";
                            msgRetorno += " é MAIOR do que o valor a pagar da DI (" + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", CambioDebitoDI).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + ")!";
                        }
                        else
                        {
                            if (view_gc_comex_financeiro.valor_pago > ((record_gc_comex_invoices.cambio_debito / 100) * 110)) // Limite de 10% a mais que o valor da DI
                            {
                                qtdInconsistencias += 1;
                                msgRetorno += " - O valor informado (" + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", view_gc_comex_financeiro.valor_pago).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + "), ";
                                msgRetorno += " é MAIOR do que o valor a pagar da Invoice (" + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", record_gc_comex_invoices.cambio_debito).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + ")!";
                            }
                        }
                    }
                }

                if (qtdInconsistencias == 0)
                {
                    gc_comex_financeiro record_gc_comex_financeiro = new Db.gc_comex_financeiro();
                    DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();

                    if (view_gc_comex_financeiro.id_financeiro == 0) // NOVO LANÇAMENTO FINANCEIRO
                    {
                        record_gc_comex_financeiro = LibDB.CloneTObject(view_gc_comex_financeiro);
                        record_gc_comex_financeiro.ativo = true;
                        record_gc_comex_financeiro.descricao = record_gc_comex_financeiro.descricao;
                        record_gc_comex_financeiro.data_vencimento = record_gc_comex_financeiro.data_vencimento;
                        record_gc_comex_financeiro.valor_total = record_gc_comex_financeiro.valor_pago;
                        record_gc_comex_financeiro.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                        record_gc_comex_financeiro.datahora_cadastro = DataHoraAtual;
                        db.gc_comex_financeiro.Add(record_gc_comex_financeiro);
                        db.SaveChanges();
                        msgRetorno += "<b>Lançamento " + record_gc_comex_financeiro.id_financeiro.EmptyIfNull().ToLower() + " REGISTRADO com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>";
                    }
                    else if (view_gc_comex_financeiro.id_financeiro > 0) // EDIÇÃO DE LANÇAMENTO FINANCEIRO
                    {
                        record_gc_comex_financeiro = db.gc_comex_financeiro.Find(view_gc_comex_financeiro.id_financeiro);
                        record_gc_comex_financeiro.descricao = view_gc_comex_financeiro.descricao;
                        record_gc_comex_financeiro.data_vencimento = view_gc_comex_financeiro.data_vencimento;
                        record_gc_comex_financeiro.data_pagamento = view_gc_comex_financeiro.data_pagamento;
                        record_gc_comex_financeiro.agente_financeiro = view_gc_comex_financeiro.agente_financeiro;
                        record_gc_comex_financeiro.numero_documento = view_gc_comex_financeiro.numero_documento;
                        record_gc_comex_financeiro.observacao = view_gc_comex_financeiro.observacao;
                        record_gc_comex_financeiro.valor_pago = view_gc_comex_financeiro.valor_pago;
                        record_gc_comex_financeiro.valor_total = record_gc_comex_financeiro.valor_pago;
                        record_gc_comex_financeiro.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                        record_gc_comex_financeiro.datahora_alteracao = DataHoraAtual;
                        db.Entry(record_gc_comex_financeiro).State = EntityState.Modified;
                        db.SaveChanges();
                        msgRetorno += "<b>Lançamento " + record_gc_comex_financeiro.id_financeiro.EmptyIfNull().ToLower() + " ALTERADO com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>";
                    }
                    
                    if (view_gc_comex_financeiro.tipo_pag_rec == 1) // Pagamento
                    {
                        // Atualizar Invoice
                        Decimal TotalCambioCredito = 0;
                        var allPagamentos = db.gc_comex_financeiro.Where(f => f.ativo == true && f.id_invoice == view_gc_comex_financeiro.id_invoice).ToList();
                        foreach (var Pagamento in allPagamentos) { TotalCambioCredito += Pagamento.valor_pago; }
                        gc_comex_invoices record_gc_comex_invoices = db.gc_comex_invoices.Find(view_gc_comex_financeiro.id_invoice);
                        record_gc_comex_invoices.cambio_credito = TotalCambioCredito;
                        db.SaveChanges();

                        // Atualizar Importação
                        TotalCambioCredito = 0;
                        var allInvoices = db.gc_comex_invoices.Where(i => i.ativo == true && i.id_importacao == view_gc_comex_financeiro.id_importacao).ToList();
                        foreach (var Invoice in allInvoices) { TotalCambioCredito += Invoice.cambio_credito; }
                        gc_comex_importacoes record_gc_comex_importacoes = db.gc_comex_importacoes.Find(record_gc_comex_financeiro.id_importacao);
                        record_gc_comex_importacoes.cambio_credito = TotalCambioCredito;
                        if (record_gc_comex_importacoes.cambio_credito >= record_gc_comex_importacoes.cambio_debito) { record_gc_comex_importacoes.cambio_liquidado = true; } else { record_gc_comex_importacoes.cambio_liquidado = false; }
                        record_gc_comex_importacoes.datahora_alteracao = DataHoraAtual;
                        record_gc_comex_importacoes.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                        db.Entry(record_gc_comex_importacoes).State = EntityState.Modified;
                        db.SaveChanges();
                    }

                    sucesso = true;
                }
            }
            catch (DbEntityValidationException ex)
            {
                qtdInconsistencias = 1;
                sucesso = false;
                msgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                qtdInconsistencias = 1;
                sucesso = false;
                msgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            return Json(new { success = sucesso, msg = msgRetorno }, JsonRequestBehavior.AllowGet);
        }

        #region ModalCancelarComexFinanceiro
        public ActionResult ModalCancelarComexFinanceiro(String id)
        {
            String MsgAdvertencia = String.Empty;
            ViewBag.Title = ViewBag.Title = LibIcons.getIcon("fa-solid fa-ban", "", "", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Cancelar Pagamento Comex</b>";
            gc_comex_financeiro record_gc_comex_financeiro = db.gc_comex_financeiro.Find(int.Parse(id));
            if (record_gc_comex_financeiro.tipo_pag_rec == 2)
            {
                MsgAdvertencia += " - Somente Pagamentos podem ser cancelados!";
            }
            ViewBag.MsgAdvertencia = MsgAdvertencia;
            record_gc_comex_financeiro.motivo_cancelamento = "";
            return View(record_gc_comex_financeiro);
        }

        [HttpPost]
        public ActionResult AjaxModalCancelarComexFinanceiro(gc_comex_financeiro view_gc_comex_financeiro)
        {
            bool sucesso = false;
            int qtdInconsistencias = 0;
            String msgRetorno = String.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            try
            {
                gc_comex_financeiro record_gc_comex_financeiro = db.gc_comex_financeiro.Find(view_gc_comex_financeiro.id_financeiro);
                if (qtdInconsistencias == 0)
                {
                    record_gc_comex_financeiro.ativo = false;
                    record_gc_comex_financeiro.motivo_cancelamento = LibStringFormat.FormatarTextoCadastroNormal(view_gc_comex_financeiro.motivo_cancelamento);
                    record_gc_comex_financeiro.datahora_cancelamento = DataHoraAtual;
                    record_gc_comex_financeiro.id_usuario_cancelamento = CachePersister.userIdentity.IdUsuario;
                    record_gc_comex_financeiro.datahora_alteracao = DataHoraAtual;
                    record_gc_comex_financeiro.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                    db.Entry(record_gc_comex_financeiro).State = EntityState.Modified;
                    db.SaveChanges();
                    sucesso = true;
                    msgRetorno += "Pagamento Comex <b>Cancelado</b> com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg");
                    if (record_gc_comex_financeiro.tipo_pag_rec == 1) // Pagamento
                    {
                        // Atualizar Invoice
                        Decimal TotalCambioCredito = 0;
                        var allPagamentos = db.gc_comex_financeiro.Where(f => f.ativo == true && f.id_invoice == record_gc_comex_financeiro.id_invoice).ToList();
                        foreach (var Pagamento in allPagamentos) { TotalCambioCredito += Pagamento.valor_pago; }
                        gc_comex_invoices record_gc_comex_invoices = db.gc_comex_invoices.Find(record_gc_comex_financeiro.id_invoice);
                        record_gc_comex_invoices.cambio_credito = TotalCambioCredito;
                        db.SaveChanges();

                        // Atualizar Importação
                        TotalCambioCredito = 0;
                        var allInvoices = db.gc_comex_invoices.Where(i => i.ativo == true && i.id_importacao == record_gc_comex_financeiro.id_importacao).ToList();
                        foreach (var Invoice in allInvoices) { TotalCambioCredito += Invoice.cambio_credito; }
                        gc_comex_importacoes record_gc_comex_importacoes = db.gc_comex_importacoes.Find(record_gc_comex_financeiro.id_importacao);
                        record_gc_comex_importacoes.cambio_credito = TotalCambioCredito;
                        if (record_gc_comex_importacoes.cambio_credito >= record_gc_comex_importacoes.cambio_debito) { record_gc_comex_importacoes.cambio_liquidado = true; } else { record_gc_comex_importacoes.cambio_liquidado = false; }
                        record_gc_comex_importacoes.datahora_alteracao = DataHoraAtual;
                        record_gc_comex_importacoes.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                        db.Entry(record_gc_comex_importacoes).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }
            }
            catch (DbEntityValidationException ex)
            {
                sucesso = false;
                msgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                sucesso = false;
                msgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            return Json(new { success = sucesso, msg = msgRetorno }, JsonRequestBehavior.AllowGet);
        }
        #endregion


    }
}