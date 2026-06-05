using GdiPlataform.Areas.g.Controllers;
using GdiPlataform.Areas.g.Models;
using GdiPlataform.Areas.gc.Models;
using GdiPlataform.Controllers;
using GdiPlataform.Db;
using GdiPlataform.Lib;
using GdiPlataform.Security;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace GdiPlataform.Areas.gc.Controllers
{
    [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_ComexInvoices_*,gc_ComexInvoices_Default")]
    public class ComexInvoicesController : Controller
    {
        private GdiPlataformEntities db;

        public ComexInvoicesController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_ComexInvoices,gc_ComexInvoices_*,gc_ComexInvoices_Actionread")]
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-file-invoice-dollar", "", "", "") + LibStringFormat.GetTabHtml(1) + "Invoices";
            return View();
        }

        #region GetDadosViewImportacao
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_ComexInvoices,gc_ComexInvoices_*,gc_ComexInvoices_Actionread")]
        public ActionResult GetDadosViewImportacao(jQueryDataTableParamModel param)
        {
            if (param == null) param = new jQueryDataTableParamModel();
            string errorMessage = GdiMvcJsonResults.DataTableSuccessErrorMessage;
            string stackTrace = GdiMvcJsonResults.DataTableSuccessStackTrace;

            int idImportacao = -1;
            int.TryParse(param.yesCustomIdPK, out idImportacao);

            try
            {
                int start = param.iDisplayStart;
                int length = (param.iDisplayLength <= 0 ? 20 : param.iDisplayLength);

                // -----------------------------
                // Base query
                // -----------------------------
                var query = db.gc_comex_invoices
                    .AsNoTracking()
                    .Where(i => i.id_invoice > 0 && i.ativo == true && i.id_importacao == idImportacao);

                // Totais para DataTables
                int totalRecords = query.Count();
                int totalDisplayRecords = totalRecords;

                // -----------------------------
                // Totais do cabeçalho (SUM no banco, sem trazer tudo)
                // -----------------------------
                var totals = query
                    .GroupBy(x => 1)
                    .Select(g => new
                    {
                        TotalCambioDebito = (decimal?)g.Sum(x => x.cambio_debito),
                        TotalItensImportacao = (decimal?)g.Sum(x => x.itens_total_price)
                    })
                    .FirstOrDefault();

                decimal valorTotalCambioDebito = totals?.TotalCambioDebito ?? 0m;
                decimal valorTotalItensImportacao = totals?.TotalItensImportacao ?? 0m;

                // -----------------------------
                // Página (OrderBy antes do Skip)
                // -----------------------------
                var page = query
                    .OrderBy(i => i.id_invoice)
                    .Skip(start)
                    .Take(length)
                    .Select(i => new
                    {
                        i.id_invoice,
                        i.invoice,
                        i.itens1_rows,
                        i.itens1_price,
                        i.itens2_rows,
                        i.itens2_price,
                        i.itens_total_rows,
                        i.itens_total_price,
                        i.cambio_debito,
                        i.cambio_credito
                    })
                    .ToList();

                // -----------------------------
                // Montagem aaData
                // -----------------------------
                var list = new List<string[]>(page.Count);

                foreach (var i in page)
                {
                    list.Add(new[]
                    {
                "", // Botão seleção
                i.id_invoice.ToString(),
                i.invoice.EmptyIfNull().ToString(),
                i.itens1_rows.ToString().Replace(",00", ""),
                string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", i.itens1_price).Replace("R$ ", "").Replace("R$", "").Replace("$", ""),
                i.itens2_rows.ToString().Replace(",00", ""),
                string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", i.itens2_price).Replace("R$ ", "").Replace("R$", "").Replace("$", ""),
                i.itens_total_rows.ToString().Replace(",00", ""),
                string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", i.itens_total_price).Replace("R$ ", "").Replace("R$", "").Replace("$", ""),
                string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", i.cambio_debito).Replace("R$ ", "").Replace("R$", "").Replace("$", ""),
                string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", i.cambio_credito).Replace("R$ ", "").Replace("R$", "").Replace("$", ""),
                "", // Botão Câmbio
                ""  // Botão Editar
            });
                }

                return Json(new
                {
                    errorMessage = GdiMvcJsonResults.DataTableSuccessErrorMessage,
                    stackTrace = GdiMvcJsonResults.DataTableSuccessStackTrace,
                    yesFilterOnOff = "0",
                    yesDisplayField01 = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", valorTotalCambioDebito).Replace("R$ ", "").Replace("R$", "").Replace("$", ""),
                    yesDisplayField02 = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", valorTotalItensImportacao).Replace("R$ ", "").Replace("R$", "").Replace("$", ""),
                    sEcho = param.sEcho,
                    iTotalRecords = totalRecords,
                    iTotalDisplayRecords = totalDisplayRecords,
                    aaData = list
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return JsonDataTableException(e, param, "0");
            }
        }
        #endregion

        #region GetDadosViewInvoicesItens
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_ComexInvoices,gc_ComexInvoices_*,gc_ComexInvoices_Actionread")]
        public ActionResult GetDadosViewInvoicesItens(jQueryDataTableParamModel param)
        {
            if (param == null) param = new jQueryDataTableParamModel();
            string errorMessage = GdiMvcJsonResults.DataTableSuccessErrorMessage;
            string stackTrace = GdiMvcJsonResults.DataTableSuccessStackTrace;

            int idInvoice = -1;
            int.TryParse(param.yesCustomIdPK, out idInvoice);

            try
            {
                int start = param.iDisplayStart;
                int length = (param.iDisplayLength <= 0 ? 20 : param.iDisplayLength);

                // -----------------------------
                // Base query
                // -----------------------------
                IQueryable<Db.gc_comex_invoices_itens> query = db.gc_comex_invoices_itens
                    .AsNoTracking()
                    .Where(i => i.id_invoice_item > 0 && i.ativo == true && i.id_invoice == idInvoice);

                // Totais DataTables
                int totalRecords = query.Count();
                int totalDisplayRecords = totalRecords;

                // -----------------------------
                // Ordenação (DataTables)
                // colunas do seu aaData:
                // 0=item_row | 1=item_qty | 2=pn | 3=serial | 4=description | 5=unit | 6=total | 7=weigth | 8=note | 9=customer
                // Observação: seu código antigo tratava sortCol==1 como item_row (provável mismatch). Ajustei para sortCol==0.
                // Se no seu DataTables o índice do item_row for "1", altere o mapeamento abaixo.
                // -----------------------------
                bool asc = (param.sSortDir_0 ?? "asc").Equals("asc", StringComparison.OrdinalIgnoreCase);
                int sortCol = param.iSortCol_0;

                IOrderedQueryable<Db.gc_comex_invoices_itens> ordered;

                if (sortCol == 0)
                    ordered = asc ? query.OrderBy(i => i.item_row) : query.OrderByDescending(i => i.item_row);
                else if (sortCol == 2)
                    ordered = asc ? query.OrderBy(i => i.pn) : query.OrderByDescending(i => i.pn);
                else if (sortCol == 6)
                    ordered = asc ? query.OrderBy(i => i.item_total_price) : query.OrderByDescending(i => i.item_total_price);
                else
                    ordered = query.OrderBy(i => i.item_row); // padrão

                // -----------------------------
                // Página (OrderBy antes do Skip)
                // -----------------------------
                var page = ordered
                    .Skip(start)
                    .Take(length)
                    .Select(i => new
                    {
                        i.item_row,
                        i.item_qty,
                        i.pn,
                        i.serial_number,
                        i.description,
                        i.item_unit_price,
                        i.item_total_price,
                        i.item_total_weigth,
                        i.note,
                        i.customer
                    })
                    .ToList();

                // -----------------------------
                // Montagem aaData
                // -----------------------------
                var list = new List<string[]>(page.Count);

                foreach (var i in page)
                {
                    list.Add(new[]
                    {
                i.item_row.EmptyIfNull().ToString(),
                i.item_qty.EmptyIfNull().ToString().Replace(",00",""),
                i.pn.EmptyIfNull().ToString(),
                i.serial_number.EmptyIfNull().ToString().Replace(",", " | "),
                i.description.EmptyIfNull().ToString(),
                string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", i.item_unit_price).Replace("R$ ", "").Replace("R$", "").Replace("$", ""),
                string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", i.item_total_price).Replace("R$ ", "").Replace("R$", "").Replace("$", ""),
                string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", i.item_total_weigth).Replace("R$ ", "").Replace("R$", "").Replace("$", ""),
                i.note.EmptyIfNull().ToString(),
                i.customer.EmptyIfNull().ToString().Replace("GDI IMPORTACAO E COMERCIO DE PECAS AERONAUTICAS", "GDI")
            });
                }

                return Json(new
                {
                    errorMessage = GdiMvcJsonResults.DataTableSuccessErrorMessage,
                    stackTrace = GdiMvcJsonResults.DataTableSuccessStackTrace,
                    yesFilterOnOff = "0",
                    sEcho = param.sEcho,
                    iTotalRecords = totalRecords,
                    iTotalDisplayRecords = totalDisplayRecords,
                    aaData = list
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return JsonDataTableException(e, param, "0");
            }
        }
        #endregion
        public ActionResult ModalImportarInvoiceXLS()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-file-excel", "", "", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Importar Invoice (xls)</b>";
            return View();
        }

        [HttpPost]
        public ActionResult AjaxModalImportarInvoiceXLS(HttpPostedFileBase filesource)
        {
            int IndexItem = 0;
            int IndexQty = 1;
            int IndexUn = 2;
            int IndexPN = 3;
            int IndexDescription = 4;
            int IndexCD = 5;
            int IndexManufactured = 6;
            int IndexUnitPrice = 7;
            int IndexTotalPrice = 8;
            int IndexTotalWeigth = 9;
            int IndexBox = 10;
            int IndexNote = 11;
            int IndexCustomer = 12;
            int ItensSerializados = 0;
            bool Processado = false;
            bool ErroProcessamento = false;
            String Logs = string.Empty;
            string String_Item_Agrupador = string.Empty;
            string MsgRetorno = string.Empty;
            string IdentificadorInvoice = string.Empty;
            string ResultadoProcessamento = String.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            gc_comex_invoices record_gc_comex_invoices = null;
            gc_comex_importacoes record_gc_comex_importacoes = db.gc_comex_importacoes.Find(CachePersister.userIdentity.IdGcComexImportacaoAtiva);

            var fileExt = System.IO.Path.GetExtension(filesource.FileName.ToLower()).Substring(1);

            if (fileExt != "xls")
            {
                ErroProcessamento = true;
                MsgRetorno = "Arquivo de invoce deve ser do tipo Planilha Excel (.xls)";
            }
            if (filesource.ContentLength > 500000)
            {
                ErroProcessamento = true;
                MsgRetorno = "O Tamanho do arquivo não pode exceder 500 Kb!";
            }
            if (filesource.ContentLength == 0)
            {
                ErroProcessamento = true;
                MsgRetorno = "O Arquivo está vazio!";
            }

            if (ErroProcessamento == false)
            {
                try
                {
                    MsgRetorno = String.Empty;
                    var fileNameOrigem = Path.GetFileName(filesource.FileName);

                    String DirTempFiles = Server.MapPath("~/_filestemp");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, "uploads");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    LibFilesDisk.DeleteFilesInDirectory(DirTempFiles); // Apagar todos os arquivos que estiveremno diretório do usuario
                    var FileNameInvoice = Path.Combine(DirTempFiles, LibDateTime.getDataHoraBrasilia().ToString("yyyyMMddhhmmss") + "_Planilha-Invoices_" + fileNameOrigem);
                    filesource.SaveAs(FileNameInvoice);

                    List<CstModelComexItemInvoice> PreListaItens = new List<CstModelComexItemInvoice>();
                    List<CstModelComexItemInvoice> ListaItens = new List<CstModelComexItemInvoice>();

                    //Get the path of specified file
                    FileStream FileTemplate = new FileStream(FileNameInvoice, FileMode.Open, FileAccess.Read);
                    HSSFWorkbook _workbook = new HSSFWorkbook(FileTemplate);
                    ISheet sheet = _workbook.GetSheetAt(0);

                    // Linha Cabeçalho
                    try
                    {
                        IdentificadorInvoice = sheet.GetRow(0).GetCell(0).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                        record_gc_comex_invoices = db.gc_comex_invoices.Where(i => (i.invoice == IdentificadorInvoice) && (i.ativo == true)).FirstOrDefault();
                        if (record_gc_comex_invoices != null)
                        {
                            ErroProcessamento = true;
                            MsgRetorno += "Invoice [" + IdentificadorInvoice + "] já importada anteriormente" + "<br/>";
                        }
                    }
                    catch (Exception)
                    {
                        ErroProcessamento = true;
                        MsgRetorno += "Identificador da Invoice NÃO localizado" + "<br/>";
                    }


                    // Processar a PreLista de Itens
                    if (ErroProcessamento == false)
                    {
                        try
                        {
                            if (sheet.GetRow(1) != null)
                            {
                                CstModelComexItemInvoice ItemInvoice = new CstModelComexItemInvoice();
                                ItemInvoice.String_Item = LibStringFormat.RemoverEspacos(LibExcelReader.GetStringCellXls(sheet.GetRow(1).GetCell(IndexItem).EmptyIfNull().ToString().Trim().ToUpperInvariant()));
                                ItemInvoice.String_Qty = LibStringFormat.RemoverEspacos(LibExcelReader.GetStringCellXls(sheet.GetRow(1).GetCell(IndexQty).EmptyIfNull().ToString().Trim().ToUpperInvariant()));
                                ItemInvoice.String_Un = LibStringFormat.RemoverEspacos(LibExcelReader.GetStringCellXls(sheet.GetRow(1).GetCell(IndexUn).EmptyIfNull().ToString().Trim().ToUpperInvariant()));
                                ItemInvoice.String_PN = LibStringFormat.GDIFormatarCodigoProduto(LibExcelReader.GetStringCellXlsx(sheet.GetRow(1).GetCell(IndexPN).EmptyIfNull().ToString().Trim().ToUpperInvariant()));
                                ItemInvoice.String_PN_Auxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(ItemInvoice.String_PN);
                                ItemInvoice.String_PN_Variacao1 = ItemInvoice.String_PN_Auxiliar.Replace("0", "O");
                                ItemInvoice.String_PN_Variacao2 = ItemInvoice.String_PN_Auxiliar.Replace("O", "0");
                                ItemInvoice.String_Description = LibStringFormat.GDIFormatarDescricaoProduto(LibExcelReader.GetStringCellXls(sheet.GetRow(1).GetCell(IndexDescription).EmptyIfNull().ToString().Trim().ToUpperInvariant()));
                                ItemInvoice.String_CD = LibStringFormat.RemoverEspacos(LibExcelReader.GetStringCellXls(sheet.GetRow(1).GetCell(IndexCD).EmptyIfNull().ToString().Trim().ToUpperInvariant()));
                                ItemInvoice.String_Manufacturer = LibExcelReader.GetStringCellXls(sheet.GetRow(1).GetCell(IndexManufactured).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                                ItemInvoice.String_UnitPrice = LibExcelReader.GetStringCellXls(sheet.GetRow(1).GetCell(IndexUnitPrice).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                                ItemInvoice.String_TotalPrice = LibExcelReader.GetStringCellXls(sheet.GetRow(1).GetCell(IndexTotalPrice).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                                ItemInvoice.String_TotalWeigth = LibExcelReader.GetStringCellXls(sheet.GetRow(1).GetCell(IndexTotalWeigth).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                                ItemInvoice.String_Box = LibExcelReader.GetStringCellXls(sheet.GetRow(1).GetCell(IndexBox).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                                ItemInvoice.String_Note = LibExcelReader.GetStringCellXls(sheet.GetRow(1).GetCell(IndexNote).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                                ItemInvoice.String_Customer = LibExcelReader.GetStringCellXls(sheet.GetRow(1).GetCell(IndexCustomer).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                                if (ItemInvoice.String_Customer.EmptyIfNull().ToString().ToUpperInvariant().StartsWith("GDI") == true) { ItemInvoice.String_Customer = "GDI"; };

                                if (ItemInvoice.IsValidHeader() == false)
                                {
                                    ErroProcessamento = true;
                                    MsgRetorno += "Cabeçalho da planilha NÃO localizado!" + "<br/>";
                                }


                                if (ErroProcessamento == false)
                                {
                                    int IndexRowSheet = 1;
                                    for (int IndexRow = 2; IndexRow <= sheet.LastRowNum; IndexRow++)
                                    {
                                        if (sheet.GetRow(IndexRow) != null)
                                        {
                                            CstModelComexItemInvoice PreItemInvoice = new CstModelComexItemInvoice();
                                            PreItemInvoice.IndexRowSheet = IndexRowSheet;
                                            PreItemInvoice.String_Item = LibStringFormat.RemoverEspacos(LibExcelReader.GetNumericCellXls(sheet.GetRow(IndexRow).GetCell(IndexItem).EmptyIfNull().ToString().Trim().ToUpperInvariant()));
                                            PreItemInvoice.String_Qty = LibStringFormat.RemoverEspacos(LibExcelReader.GetNumericCellXls(sheet.GetRow(IndexRow).GetCell(IndexQty).EmptyIfNull().ToString().Trim().ToUpperInvariant()));
                                            PreItemInvoice.String_Un = LibStringFormat.RemoverEspacos(LibExcelReader.GetStringCellXls(sheet.GetRow(IndexRow).GetCell(IndexUn).EmptyIfNull().ToString().Trim().ToUpperInvariant()));
                                            PreItemInvoice.String_PN = LibStringFormat.GDIFormatarCodigoProduto(LibExcelReader.GetStringCellXls(sheet.GetRow(IndexRow).GetCell(IndexPN).EmptyIfNull().ToString().Trim().ToUpperInvariant()));
                                            PreItemInvoice.String_PN_Auxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(PreItemInvoice.String_PN);
                                            PreItemInvoice.String_PN_Variacao1 = PreItemInvoice.String_PN_Auxiliar.Replace("0", "O");
                                            PreItemInvoice.String_PN_Variacao2 = PreItemInvoice.String_PN_Auxiliar.Replace("O", "0");
                                            PreItemInvoice.String_Description = LibStringFormat.GDIFormatarDescricaoProduto(LibExcelReader.GetStringCellXls(sheet.GetRow(IndexRow).GetCell(IndexDescription).EmptyIfNull().ToString().Trim().ToUpperInvariant()));
                                            PreItemInvoice.String_CD = LibStringFormat.RemoverEspacos(LibExcelReader.GetStringCellXls(sheet.GetRow(IndexRow).GetCell(IndexCD).EmptyIfNull().ToString().Trim().ToUpperInvariant()));
                                            PreItemInvoice.String_Manufacturer = LibExcelReader.GetStringCellXls(sheet.GetRow(IndexRow).GetCell(IndexManufactured).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                                            PreItemInvoice.String_UnitPrice = LibExcelReader.GetDecimalCellXls(sheet.GetRow(IndexRow).GetCell(IndexUnitPrice).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                                            PreItemInvoice.String_TotalPrice = LibExcelReader.GetDecimalCellXls(sheet.GetRow(IndexRow).GetCell(IndexTotalPrice).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                                            PreItemInvoice.String_TotalWeigth = LibExcelReader.GetWeightCellXls(sheet.GetRow(IndexRow).GetCell(IndexTotalWeigth).EmptyIfNull().ToString().Trim().ToUpperInvariant()).ToUpperInvariant().Trim();
                                            PreItemInvoice.String_Box = LibExcelReader.GetStringCellXls(sheet.GetRow(IndexRow).GetCell(IndexBox).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                                            PreItemInvoice.String_Note = LibExcelReader.GetStringCellXls(sheet.GetRow(IndexRow).GetCell(IndexNote).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                                            PreItemInvoice.String_Customer = LibExcelReader.GetStringCellXls(LibStringFormat.DecodeHtmlString(sheet.GetRow(IndexRow).GetCell(IndexCustomer).EmptyIfNull().ToString().Trim().ToUpperInvariant()));
                                            if (PreItemInvoice.String_Customer.EmptyIfNull().ToString().ToUpperInvariant().StartsWith("GDI") == true) { PreItemInvoice.String_Customer = "GDI"; };
                                            if (PreItemInvoice.String_Item.Trim().Length > 0) // Item Principal
                                            {
                                                if (PreItemInvoice.IsValidItem())
                                                {
                                                    PreItemInvoice.String_PN = PreItemInvoice.String_PN.EmptyIfNull().Trim();
                                                    PreListaItens.Add(PreItemInvoice);
                                                }
                                                else
                                                {
                                                    ErroProcessamento = true;
                                                    MsgRetorno += " - Erro ao processar a linha [" + (IndexRow + 1).ToString() + "]!" + "<br/>";
                                                }
                                            }
                                            else if (PreItemInvoice.IsDetailItem()) // Item Detalhe
                                            {
                                                PreItemInvoice.String_Item = PreListaItens[PreListaItens.Count - 1].String_Item;
                                                if (PreItemInvoice.IsValidItem())
                                                {
                                                    PreItemInvoice.String_PN = PreItemInvoice.String_PN.EmptyIfNull().Trim();
                                                    PreListaItens.Add(PreItemInvoice);
                                                }
                                                else
                                                {
                                                    ErroProcessamento = true;
                                                    MsgRetorno += " - Erro ao processar a linha [" + (IndexRow + 1).ToString() + "]!" + "<br/>";
                                                }
                                            }
                                            else if (PreItemInvoice.IsRowEmpty()) 
                                            {
                                                ErroProcessamento = true;
                                                MsgRetorno += " - Linha [" + (IndexRow + 1).ToString() + "] não contém dados!" + "<br/>"; 
                                            }
                                            else // Complemento de linha (Número Serial)
                                            {
                                                CstModelComexItemInvoice UltimoPreItem = PreListaItens[PreListaItens.Count - 1];
                                                if (PreItemInvoice.String_PN.Trim().Length > 0) 
                                                {
                                                    String ItemSerialNumber = PreItemInvoice.String_PN.Replace("SN:", "").Replace("SN :", "").Replace(" ", "").Replace(",", "").Replace(";", "") + ",";
                                                    UltimoPreItem.String_SerialNumber += ItemSerialNumber;
                                                };
                                                PreListaItens[PreListaItens.Count - 1] = UltimoPreItem;
                                            }
                                            IndexRowSheet += 1;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                ErroProcessamento = true;
                                MsgRetorno += "Cabeçalho da planilha NÃO localizado!" + "<br/>";
                            }
                        }
                        catch (Exception)
                        {
                            ErroProcessamento = true;
                            MsgRetorno += "Cabeçalho da planilha NÃO localizado!" + "<br/>";
                        }
                    }


                    IndexItem = 1;
                    foreach (CstModelComexItemInvoice Item in PreListaItens)
                    {
                        if (Item.String_Note == "AKIT")
                        {

                        }
                        else
                        {
                            Item.ItemValido = true;
                            Item.ItemIndex = IndexItem;
                            ListaItens.Add(Item);
                            IndexItem += 1;
                        }
                    }


                    // Salvar Banco Dados
                    if (ErroProcessamento == false)
                    {
                        int _itens1_rows = 0;
                        int _itens2_rows = 0;
                        int _itens1_qty = 0;
                        int _itens2_qty = 0;
                        Decimal _itens1_price = 0;
                        Decimal _itens2_price = 0;
                        Decimal _itens1_weigth = 0;
                        Decimal _itens2_weigth = 0;

                        // Processar os itens
                        List<gc_comex_invoices_itens> ListaItensNovaInvoice = new List<gc_comex_invoices_itens>();
                        List<CstModelComexItemInvoice> ListaFinal = ListaItens.Where(l => l.ItemIndex >= 0).OrderBy(l => l.IndexRowSheet).ToList();
                        foreach (CstModelComexItemInvoice ItemImportacao in ListaFinal)
                        {
                            gc_comex_invoices_itens novo_item_invoice = new Db.gc_comex_invoices_itens();
                            if (ItemImportacao.ItemValido == true) // NOVO ITEM INVOICE - new gc_comex_invoices_itens
                            {
                                novo_item_invoice.id_importacao = CachePersister.userIdentity.IdGcComexImportacaoAtiva;
                                novo_item_invoice.ativo = true;
                                novo_item_invoice.recebido = false;
                                novo_item_invoice.item_row = ItemImportacao.Int_Item;
                                novo_item_invoice.item_qty = ItemImportacao.Int_Qty;
                                novo_item_invoice.un = ItemImportacao.String_Un;
                                novo_item_invoice.pn = ItemImportacao.String_PN.Trim();
                                novo_item_invoice.pn_auxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(novo_item_invoice.pn);
                                novo_item_invoice.pn_variacao1 = novo_item_invoice.pn_auxiliar.Replace("0", "O");
                                novo_item_invoice.pn_variacao2 = novo_item_invoice.pn_auxiliar.Replace("O", "0");
                                novo_item_invoice.description = ItemImportacao.String_Description;
                                novo_item_invoice.cd = ItemImportacao.String_CD;
                                novo_item_invoice.manufacturer = ItemImportacao.String_Manufacturer;
                                novo_item_invoice.item_unit_price = ItemImportacao.Decimal_UnitPrice;
                                novo_item_invoice.item_total_price = ItemImportacao.Decimal_TotalPrice;
                                novo_item_invoice.item_unit_weigth = (ItemImportacao.Decimal_TotalWeigth / ItemImportacao.Int_Qty);
                                novo_item_invoice.item_total_weigth = ItemImportacao.Decimal_TotalWeigth;
                                novo_item_invoice.box = ItemImportacao.String_Box;
                                novo_item_invoice.serial_number = ItemImportacao.String_SerialNumber;
                                novo_item_invoice.note = ItemImportacao.String_Note;
                                novo_item_invoice.customer = ItemImportacao.String_Customer;
                                novo_item_invoice.id_coligada = 1;
                                novo_item_invoice.id_filial = 1;
                                novo_item_invoice.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                                novo_item_invoice.datahora_cadastro = DataHoraAtual;
                                ListaItensNovaInvoice.Add(novo_item_invoice);

                                if (ItemImportacao.String_Customer.StartsWith("GDI") == true)
                                {
                                    _itens1_rows += 1;
                                    _itens1_qty += ItemImportacao.Int_Qty;
                                    _itens1_price += ItemImportacao.Decimal_TotalPrice;
                                    _itens1_weigth += ItemImportacao.Decimal_TotalWeigth;
                                }
                                else
                                {
                                    _itens2_rows += 1;
                                    _itens2_qty += ItemImportacao.Int_Qty;
                                    _itens2_price += ItemImportacao.Decimal_TotalPrice;
                                    _itens2_weigth += ItemImportacao.Decimal_TotalWeigth;
                                }
                            }
                        }

                        // Criar nova invoice
                        gc_comex_invoices nova_invoce = new Db.gc_comex_invoices();
                        nova_invoce.ativo = false;
                        nova_invoce.invoice = IdentificadorInvoice;
                        nova_invoce.id_importacao = CachePersister.userIdentity.IdGcComexImportacaoAtiva;
                        nova_invoce.itens1_rows = _itens1_rows;
                        nova_invoce.itens2_rows = _itens2_rows;
                        nova_invoce.itens_total_rows = _itens1_rows + _itens2_rows;
                        nova_invoce.itens1_qty = _itens1_qty;
                        nova_invoce.itens2_qty = _itens2_qty;
                        nova_invoce.itens_total_qty = _itens1_qty + _itens2_qty;
                        nova_invoce.itens1_price = _itens1_price;
                        nova_invoce.itens2_price = _itens2_price;
                        nova_invoce.itens_total_price = _itens1_price + _itens2_price;
                        nova_invoce.itens_total_debit = nova_invoce.itens1_price;
                        nova_invoce.itens_total_credit = 0;
                        nova_invoce.itens1_weigth = _itens1_weigth;
                        nova_invoce.itens2_weigth = _itens2_weigth;
                        nova_invoce.itens_total_weigth = _itens1_weigth + _itens2_weigth;
                        nova_invoce.cambio_debito = _itens1_price;
                        nova_invoce.cambio_credito = 0;
                        nova_invoce.cambio_liquidado = false;
                        nova_invoce.id_coligada = 1;
                        nova_invoce.id_filial = 1;
                        nova_invoce.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                        nova_invoce.datahora_cadastro = DataHoraAtual;
                        db.gc_comex_invoices.Add(nova_invoce);
                        db.SaveChanges();

                        // Validar Cadastro de Produtos (Comex e GDI)
                        List<gc_comex_produtos> ListaProdutosComex = db.gc_comex_produtos.Where(i => i.ativo == true).ToList();
                        List<g_produtos> ListaProdutosGDI = db.g_produtos.Where(p => p.ativo == true).ToList();
                        foreach (gc_comex_invoices_itens ItemNovaInvoice in ListaItensNovaInvoice)
                        {
                            // CADASTRO DE PRODUTOS - 5
                            String PNOficial = ItemNovaInvoice.pn.EmptyIfNull().ToString();
                            String PNAuxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(PNOficial);
                            String PNCuringaOH = PNAuxiliar.Replace("0", "O");
                            String PNCuringaZERO = PNAuxiliar.Replace("O", "0");

                            g_produtos ProdutoGDI = ListaProdutosGDI.Where(p => p.codigo == PNOficial).FirstOrDefault(); // Buscar pelo PN principal
                            try { if (ProdutoGDI == null) { ProdutoGDI = ListaProdutosGDI.Where(p => p.codigo_auxiliar == PNAuxiliar || p.codigo_variacao1 == PNCuringaOH || p.codigo_variacao2 == PNCuringaZERO).FirstOrDefault(); };  } catch (Exception) { }; // Buscar pelo PN Auxiliar

                            gc_comex_produtos ProdutoComex = ListaProdutosComex.Where(p => p.pn == PNOficial).FirstOrDefault();
                            try { if (ProdutoComex == null) { ProdutoComex = ListaProdutosComex.Where(p => p.pn_auxiliar == PNAuxiliar || p.pn_variacao1 == PNCuringaOH || p.pn_variacao2 == PNCuringaZERO).FirstOrDefault(); }; } catch (Exception) { };

                            if (ProdutoComex == null) // Novos Produtos Comex  - New gc_comex_produtos
                            {
                                ProdutoComex = new Db.gc_comex_produtos();
                                ProdutoComex.ativo = true;
                                if (ProdutoGDI != null) 
                                {
                                    ProdutoComex.id_produto = ProdutoGDI.id_produto;
                                    ProdutoComex.item_cadastro_novo = false;
                                }
                                else
                                {
                                    ProdutoComex.id_produto = 0;
                                    ProdutoComex.item_cadastro_novo = true;
                                }
                                ProdutoComex.pn = PNOficial;
                                ProdutoComex.pn_auxiliar = PNAuxiliar;
                                ProdutoComex.pn_variacao1 = ProdutoComex.pn_auxiliar.Replace("0", "O");
                                ProdutoComex.pn_variacao2 = ProdutoComex.pn_auxiliar.Replace("O", "0");
                                ProdutoComex.description = ItemNovaInvoice.description;
                                ProdutoComex.manufacturer = ItemNovaInvoice.manufacturer;
                                ProdutoComex.fob1_dollar = ItemNovaInvoice.item_unit_price;
                                ProdutoComex.fob1_id_importacao = ItemNovaInvoice.id_importacao;
                                ProdutoComex.id_coligada = 1;
                                ProdutoComex.id_filial = 1;
                                ProdutoComex.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                                ProdutoComex.datahora_cadastro = DataHoraAtual;
                                db.gc_comex_produtos.Add(ProdutoComex);
                                db.SaveChanges();
                                ListaProdutosComex.Add(ProdutoComex);

                                Logs = "Novo Produto Comex | pn: " + ProdutoComex.pn.EmptyIfNull().ToString() + " | description: " + ProdutoComex.description.EmptyIfNull().ToString() + " | manufacturer: " + ProdutoComex.manufacturer.EmptyIfNull().ToString() + " | Id Importação: " + CachePersister.userIdentity.IdGcComexImportacaoAtiva.EmptyIfNull().ToString();
                                LibAudit.SaveAudit(db, false,"gc_comex_produtos", ProdutoComex.id_comex_produto, Logs);

                                if (ProdutoComex.id_produto > 0) { LibAudit.SaveAudit(db, false, "gc_comex_produtos", ProdutoComex.id_comex_produto, "Vinculação ao Produto ERP id: " + ProdutoComex.id_produto.ToString()); };
                            }
                            else if (ProdutoComex != null)
                            {
                                Logs = string.Empty;
                                
                                // Atualização do Produto Comex
                                bool ProdutoComexAtualizado = false;
                                if ((ProdutoComex.description.EmptyIfNull().Trim().Length > 0) && (ItemNovaInvoice.description.EmptyIfNull().Trim() != ProdutoComex.description.EmptyIfNull().Trim()))
                                {
                                    ProdutoComex.description = ItemNovaInvoice.description.EmptyIfNull().Trim();
                                    Logs = "Atualização Dados | description: " + ItemNovaInvoice.description.EmptyIfNull().Trim() + " > " + ProdutoComex.description.EmptyIfNull().Trim() + " | Id Importação: " + CachePersister.userIdentity.IdGcComexImportacaoAtiva.EmptyIfNull().ToString();
                                    LibAudit.SaveAudit(db, false, "gc_comex_produtos", ProdutoComex.id_comex_produto, Logs);
                                    ProdutoComexAtualizado = true;
                                }
                                if ((ProdutoComex.manufacturer.EmptyIfNull().Trim().Length > 0) && (ItemNovaInvoice.manufacturer.EmptyIfNull().Trim() != ProdutoComex.manufacturer.EmptyIfNull().Trim()))
                                {
                                    ProdutoComex.manufacturer = ItemNovaInvoice.manufacturer.EmptyIfNull().Trim();
                                    Logs = "Atualização Dados | Manufacturer: " + ItemNovaInvoice.manufacturer.EmptyIfNull().Trim() + " > " + ProdutoComex.manufacturer.EmptyIfNull().Trim() + " | Id Importação: " + CachePersister.userIdentity.IdGcComexImportacaoAtiva.EmptyIfNull().ToString();
                                    LibAudit.SaveAudit(db, false, "gc_comex_produtos", ProdutoComex.id_comex_produto, Logs);
                                    ProdutoComexAtualizado = true;
                                }
                                if (ProdutoComex.fob1_dollar == 0)
                                {
                                    ProdutoComex.fob1_dollar = ItemNovaInvoice.item_unit_price;
                                    LibAudit.SaveAudit(db, false, "gc_comex_produtos", ProdutoComex.id_produto, "Atualização Fob US$: " + ProdutoComex.fob1_dollar.ToString("0.00000"));
                                    ProdutoComexAtualizado = true;
                                };
                                if ((ProdutoComex.id_produto == 0) && (ProdutoGDI != null))
                                {
                                    ProdutoComex.id_produto = ProdutoGDI.id_produto;
                                    LibAudit.SaveAudit(db, false, "gc_comex_produtos", ProdutoComex.id_comex_produto, "Vinculação ao Produto ERP id: " + ProdutoGDI.id_produto.ToString());
                                    ProdutoComexAtualizado = true;
                                }
                                if (ProdutoComexAtualizado == true)
                                {
                                    ProdutoComex.datahora_alteracao = DataHoraAtual;
                                    ProdutoComex.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                    db.Entry(ProdutoComex).State = EntityState.Modified;
                                    db.SaveChanges();
                                }

                                // Atualização do Produto GDI
                                if (ProdutoGDI != null)
                                {
                                    bool ProdutoGDIAtualizado = false;
                                    if (ProdutoGDI.fob1_dollar == 0)
                                    {
                                        ProdutoGDI.fob1_dollar = ItemNovaInvoice.item_unit_price;
                                        ProdutoGDI.fob1_id_importacao = ItemNovaInvoice.id_importacao;
                                        ProdutoGDI.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                        ProdutoGDI.datahora_alteracao = DataHoraAtual;
                                        LibAudit.SaveAudit(db, false, "gc_produtos", ProdutoGDI.id_produto, "Atualização Fob US$: " + ProdutoGDI.fob1_dollar.ToString("0.00000"));
                                        ProdutoGDIAtualizado = true;
                                    }
                                    if (ProdutoGDIAtualizado == true)
                                    {
                                        ProdutoGDI.datahora_alteracao = DataHoraAtual;
                                        ProdutoGDI.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                        db.Entry(ProdutoGDI).State = EntityState.Modified;
                                        db.SaveChanges();
                                    }
                                }
                            }

                            ItemNovaInvoice.id_comex_produto = ProdutoComex.id_comex_produto;
                            ItemNovaInvoice.id_produto = ProdutoComex.id_produto;
                            ItemNovaInvoice.id_invoice = nova_invoce.id_invoice;
                            db.gc_comex_invoices_itens.Add(ItemNovaInvoice);
                            db.SaveChanges();

                            // Tratamento dos itens seriais
                            if (ItemNovaInvoice.serial_number.EmptyIfNull().ToString().Length > 0)
                            {
                                String[] ListaSeriais = null;
                                try { ListaSeriais = ItemNovaInvoice.serial_number.EmptyIfNull().ToString().Split(','); } catch (Exception) { ListaSeriais = new string[1] { "" }; };
                                if (ListaSeriais.Count() > 0)
                                {
                                    foreach (String Serial in ListaSeriais)
                                    {
                                        if (Serial.EmptyIfNull().ToString().Length > 0)
                                        {
                                            gc_comex_invoices_itens_serials record_gc_comex_invoices_itens_serials = new Db.gc_comex_invoices_itens_serials();
                                            record_gc_comex_invoices_itens_serials.id_invoice_item = ItemNovaInvoice.id_invoice_item;
                                            record_gc_comex_invoices_itens_serials.ativo = true;
                                            record_gc_comex_invoices_itens_serials.recebido = false;
                                            record_gc_comex_invoices_itens_serials.disponivel = false;
                                            record_gc_comex_invoices_itens_serials.serial = Serial;
                                            db.gc_comex_invoices_itens_serials.Add(record_gc_comex_invoices_itens_serials);
                                            ItensSerializados += 1;
                                            db.SaveChanges();
                                        }
                                    }
                                }
                            }
                        }

                        // GED - XLS
                        // Verificar se há outro XML GED para a mesma Invoice
                        int VersaoGedXML = 0;
                        String DescricaoGedXML = String.Empty;
                        DescricaoGedXML = "Planilha Invoice [" + IdentificadorInvoice + "] Expandida (xls)";
                        IQueryable<ged_arquivos> listaGedXML = db.ged_arquivos.Where(g => (g.ativo == true) && (g.descricao == DescricaoGedXML));
                        if (listaGedXML.Count() > 0)
                        {
                            foreach (ged_arquivos itemGedXML in listaGedXML)
                            {
                                if (itemGedXML.versao > VersaoGedXML) { VersaoGedXML = itemGedXML.versao; };
                                itemGedXML.ativo = false;
                                db.Entry(itemGedXML).State = EntityState.Modified;
                            }
                        }
                        // Realizar o upload do XML para o GED
                        CstUploadGed record_cstUploadGedXML = new CstUploadGed();
                        record_cstUploadGedXML.id_arquivo = 0;
                        record_cstUploadGedXML.id_arquivo_tipo = 13; // Comex - Importações
                        record_cstUploadGedXML.filesource = filesource;
                        record_cstUploadGedXML.id_comex_importacao = CachePersister.userIdentity.IdGcComexImportacaoAtiva; ;
                        record_cstUploadGedXML.descricao = DescricaoGedXML;
                        record_cstUploadGedXML.observacao = DescricaoGedXML + ", processado em " + DataHoraAtual.ToString("dd/MM/yyyy HH:mm") + " por " + CachePersister.userIdentity.Username;
                        record_cstUploadGedXML.versao = VersaoGedXML + 1;
                        var ResultUploadFileXML = new GedController().ServiceUploadFileGed(record_cstUploadGedXML);
                        db.SaveChanges();


                        // Ativar a invoice
                        nova_invoce.ativo = true;
                        nova_invoce.datahora_alteracao = DataHoraAtual;
                        nova_invoce.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                        db.Entry(nova_invoce).State = EntityState.Modified;
                        db.SaveChanges();

                        AtualizarFinanceiroComex();

                        // Atualizar dados da importação
                        String textoSQLAtualizarImportacao = " update gc_comex_importacoes set " +
                                                             " cambio_debito = (select sum(cambio_debito) from gc_comex_invoices where ativo = 1 and id_importacao = " + CachePersister.userIdentity.IdGcComexImportacaoAtiva.ToString() + ") " +
                                                             " where id_importacao = " + CachePersister.userIdentity.IdGcComexImportacaoAtiva.ToString();
                        int qtdItensAtualizados = LibDB.dbQueryExec(textoSQLAtualizarImportacao, db);

                        MsgRetorno += "Invoice [ " + IdentificadorInvoice + " ] <b>PROCESSADA</b> com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>" +
                                        "---------- Itens GDI ----------" + "<br/>" +
                                        "<b>Itens</b> " + nova_invoce.itens1_rows.ToString() + LibStringFormat.GetTabHtml(2) + "<b>Quantidade</b>: " + nova_invoce.itens1_qty.ToString() + "<br/>" +
                                        "<b>Valor</b>: " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", nova_invoce.itens1_price).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + " US$" + LibStringFormat.GetTabHtml(2) + "<b>Peso </b>: " + nova_invoce.itens1_weigth.ToString("0.000") + " KG" + "<br/>" +
                                        "---------- Itens Terceiros ----------" + "<br/>" +
                                        "<b>Itens</b> " + nova_invoce.itens2_rows.ToString() + LibStringFormat.GetTabHtml(2) + "<b>Quantidade</b>: " + nova_invoce.itens2_qty.ToString() + "<br/>" +
                                        "<b>Valor</b>: " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", nova_invoce.itens2_price).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + " US$" + LibStringFormat.GetTabHtml(2) + "<b>Peso </b>: " + nova_invoce.itens2_weigth.ToString("0.000") + " KG" + "<br/>" +
                                        "---------- TOTAIS ----------" + "<br/>" +
                                        "<b>Itens</b> " + nova_invoce.itens_total_rows.ToString() + LibStringFormat.GetTabHtml(2) + "<b>Quantidade</b>: " + nova_invoce.itens_total_qty.ToString() + "<br/>" +
                                        "<b>Valor</b>: " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", nova_invoce.itens_total_price).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + " US$" + LibStringFormat.GetTabHtml(2) + "<b>Peso </b>: " + nova_invoce.itens_total_weigth.ToString("0.000") + " KG" + "<br/>" +
                                        "<b>Itens Serializados</b>: " + ItensSerializados.ToString();
                        Processado = true;
                    }
                }
                catch (DbEntityValidationException ex)
                {
                    return JsonAjaxErroValidacao(ex);
                }
                catch (Exception e)
                {
                    return JsonAjaxErro(e);
                }
            }
            return Json(new { success = Processado, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        public ActionResult AjaxModalReprocessarInvoiceXLS(HttpPostedFileBase filesource)
        {
            int IndexItem = 0;
            int IndexQty = 1;
            int IndexUn = 2;
            int IndexPN = 3;
            int IndexDescription = 4;
            int IndexCD = 5;
            int IndexManufactured = 6;
            int IndexUnitPrice = 7;
            int IndexTotalPrice = 8;
            int IndexTotalWeigth = 9;
            int IndexBox = 10;
            int IndexNote = 11;
            int QtdItensNaoLocalizados = 0;
            int IndexCustomer = 12;
            bool Processado = false;
            bool ErroProcessamento = false;
            String Logs = string.Empty;
            string String_Item_Agrupador = string.Empty;
            string MsgRetorno = string.Empty;
            string IdentificadorInvoice = string.Empty;
            string ResultadoProcessamento = String.Empty;
            string ListaItensNaoLocalizados = String.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            gc_comex_importacoes record_gc_comex_importacoes = db.gc_comex_importacoes.Find(CachePersister.userIdentity.IdGcComexImportacaoAtiva);
            gc_comex_invoices RecordComexInvoice = null;

            var fileExt = System.IO.Path.GetExtension(filesource.FileName.ToLower()).Substring(1);

            if (fileExt != "xls")
            {
                ErroProcessamento = true;
                MsgRetorno = "Arquivo de invoce deve ser do tipo Planilha Excel (.xls)";
            }
            if (filesource.ContentLength > 500000)
            {
                ErroProcessamento = true;
                MsgRetorno = "O Tamanho do arquivo não pode exceder 500 Kb!";
            }
            if (filesource.ContentLength == 0)
            {
                ErroProcessamento = true;
                MsgRetorno = "O Arquivo está vazio!";
            }

            if (ErroProcessamento == false)
            {
                try
                {
                    MsgRetorno = String.Empty;
                    var fileNameOrigem = Path.GetFileName(filesource.FileName);

                    String DirTempFiles = Server.MapPath("~/_filestemp");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, "uploads");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    LibFilesDisk.DeleteFilesInDirectory(DirTempFiles); // Apagar todos os arquivos que estiveremno diretório do usuario
                    var FileNameInvoice = Path.Combine(DirTempFiles, LibDateTime.getDataHoraBrasilia().ToString("yyyyMMddhhmmss") + "_Planilha-Invoices_" + fileNameOrigem);
                    filesource.SaveAs(FileNameInvoice);

                    List<CstModelComexItemInvoice> PreListaItens = new List<CstModelComexItemInvoice>();
                    List<CstModelComexItemInvoice> ListaItens = new List<CstModelComexItemInvoice>();

                    //Get the path of specified file
                    FileStream FileTemplate = new FileStream(FileNameInvoice, FileMode.Open, FileAccess.Read);
                    HSSFWorkbook _workbook = new HSSFWorkbook(FileTemplate);
                    ISheet sheet = _workbook.GetSheetAt(0);


                    // Linha Cabeçalho
                    try
                    {
                        IdentificadorInvoice = sheet.GetRow(0).GetCell(0).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                        RecordComexInvoice = db.gc_comex_invoices.Where(i => (i.invoice == IdentificadorInvoice) && (i.ativo == true)).FirstOrDefault();
                        if (RecordComexInvoice == null)
                        {
                            ErroProcessamento = true;
                            MsgRetorno += "Itens da Invoice [" + IdentificadorInvoice + "] NÃO localizados!" + "<br/>";
                        }
                    }
                    catch (Exception)
                    {
                        ErroProcessamento = true;
                        MsgRetorno += "Identificador da Invoice NÃO localizado" + "<br/>";
                    }


                    // Processar a PreLista de Itens
                    if (ErroProcessamento == false)
                    {
                        try
                        {
                            if (sheet.GetRow(1) != null)
                            {
                                CstModelComexItemInvoice ItemInvoice = new CstModelComexItemInvoice();
                                ItemInvoice.String_Item = LibStringFormat.RemoverEspacos(LibExcelReader.GetStringCellXls(sheet.GetRow(1).GetCell(IndexItem).EmptyIfNull().ToString().Trim().ToUpperInvariant()));
                                ItemInvoice.String_Qty = LibStringFormat.RemoverEspacos(LibExcelReader.GetStringCellXls(sheet.GetRow(1).GetCell(IndexQty).EmptyIfNull().ToString().Trim().ToUpperInvariant()));
                                ItemInvoice.String_Un = LibStringFormat.RemoverEspacos(LibExcelReader.GetStringCellXls(sheet.GetRow(1).GetCell(IndexUn).EmptyIfNull().ToString().Trim().ToUpperInvariant()));
                                ItemInvoice.String_PN = LibStringFormat.GDIFormatarCodigoProduto(LibExcelReader.GetStringCellXlsx(sheet.GetRow(1).GetCell(IndexPN).EmptyIfNull().ToString().Trim().ToUpperInvariant()));
                                ItemInvoice.String_PN_Auxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(ItemInvoice.String_PN);
                                ItemInvoice.String_PN_Variacao1 = ItemInvoice.String_PN_Auxiliar.Replace("0", "O");
                                ItemInvoice.String_PN_Variacao2 = ItemInvoice.String_PN_Auxiliar.Replace("O", "0");
                                ItemInvoice.String_Description = LibStringFormat.GDIFormatarDescricaoProduto(LibExcelReader.GetStringCellXls(sheet.GetRow(1).GetCell(IndexDescription).EmptyIfNull().ToString().Trim().ToUpperInvariant()));
                                ItemInvoice.String_CD = LibStringFormat.RemoverEspacos(LibExcelReader.GetStringCellXls(sheet.GetRow(1).GetCell(IndexCD).EmptyIfNull().ToString().Trim().ToUpperInvariant()));
                                ItemInvoice.String_Manufacturer = LibExcelReader.GetStringCellXls(sheet.GetRow(1).GetCell(IndexManufactured).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                                ItemInvoice.String_UnitPrice = LibExcelReader.GetStringCellXls(sheet.GetRow(1).GetCell(IndexUnitPrice).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                                ItemInvoice.String_TotalPrice = LibExcelReader.GetStringCellXls(sheet.GetRow(1).GetCell(IndexTotalPrice).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                                ItemInvoice.String_TotalWeigth = LibExcelReader.GetStringCellXls(sheet.GetRow(1).GetCell(IndexTotalWeigth).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                                ItemInvoice.String_Box = LibExcelReader.GetStringCellXls(sheet.GetRow(1).GetCell(IndexBox).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                                ItemInvoice.String_Note = LibExcelReader.GetStringCellXls(sheet.GetRow(1).GetCell(IndexNote).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                                ItemInvoice.String_Customer = LibExcelReader.GetStringCellXls(sheet.GetRow(1).GetCell(IndexCustomer).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                                if (ItemInvoice.String_Customer.EmptyIfNull().ToString().ToUpperInvariant().StartsWith("GDI") == true) { ItemInvoice.String_Customer = "GDI"; };
                                if (ItemInvoice.IsValidHeader() == false)
                                {
                                    ErroProcessamento = true;
                                    MsgRetorno += "Cabeçalho da planilha NÃO localizado!" + "<br/>";
                                }


                                if (ErroProcessamento == false)
                                {
                                    int IndexRowSheet = 1;
                                    for (int IndexRow = 2; IndexRow <= sheet.LastRowNum; IndexRow++)
                                    {
                                        if (sheet.GetRow(IndexRow) != null)
                                        {
                                            CstModelComexItemInvoice PreItemInvoice = new CstModelComexItemInvoice();
                                            PreItemInvoice.IndexRowSheet = IndexRowSheet;
                                            PreItemInvoice.String_Item = LibStringFormat.RemoverEspacos(LibExcelReader.GetNumericCellXls(sheet.GetRow(IndexRow).GetCell(IndexItem).EmptyIfNull().ToString().Trim().ToUpperInvariant()));
                                            PreItemInvoice.String_Qty = LibStringFormat.RemoverEspacos(LibExcelReader.GetNumericCellXls(sheet.GetRow(IndexRow).GetCell(IndexQty).EmptyIfNull().ToString().Trim().ToUpperInvariant()));
                                            PreItemInvoice.String_Un = LibStringFormat.RemoverEspacos(LibExcelReader.GetStringCellXls(sheet.GetRow(IndexRow).GetCell(IndexUn).EmptyIfNull().ToString().Trim().ToUpperInvariant()));
                                            PreItemInvoice.String_PN = LibStringFormat.GDIFormatarCodigoProduto(LibExcelReader.GetStringCellXls(sheet.GetRow(IndexRow).GetCell(IndexPN).EmptyIfNull().ToString().Trim().ToUpperInvariant()));
                                            PreItemInvoice.String_PN_Auxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(PreItemInvoice.String_PN);
                                            PreItemInvoice.String_PN_Variacao1 = PreItemInvoice.String_PN_Auxiliar.Replace("0", "O");
                                            PreItemInvoice.String_PN_Variacao2 = PreItemInvoice.String_PN_Auxiliar.Replace("O", "0");
                                            PreItemInvoice.String_Description = LibStringFormat.GDIFormatarDescricaoProduto(LibExcelReader.GetStringCellXls(sheet.GetRow(IndexRow).GetCell(IndexDescription).EmptyIfNull().ToString().Trim().ToUpperInvariant()));
                                            PreItemInvoice.String_CD = LibStringFormat.RemoverEspacos(LibExcelReader.GetStringCellXls(sheet.GetRow(IndexRow).GetCell(IndexCD).EmptyIfNull().ToString().Trim().ToUpperInvariant()));
                                            PreItemInvoice.String_Manufacturer = LibExcelReader.GetStringCellXls(sheet.GetRow(IndexRow).GetCell(IndexManufactured).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                                            PreItemInvoice.String_UnitPrice = LibExcelReader.GetDecimalCellXls(sheet.GetRow(IndexRow).GetCell(IndexUnitPrice).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                                            PreItemInvoice.String_TotalPrice = LibExcelReader.GetDecimalCellXls(sheet.GetRow(IndexRow).GetCell(IndexTotalPrice).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                                            PreItemInvoice.String_TotalWeigth = LibExcelReader.GetWeightCellXls(sheet.GetRow(IndexRow).GetCell(IndexTotalWeigth).EmptyIfNull().ToString().Trim().ToUpperInvariant()).ToUpperInvariant().Trim();
                                            PreItemInvoice.String_Box = LibExcelReader.GetStringCellXls(sheet.GetRow(IndexRow).GetCell(IndexBox).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                                            PreItemInvoice.String_Note = LibExcelReader.GetStringCellXls(sheet.GetRow(IndexRow).GetCell(IndexNote).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                                            PreItemInvoice.String_Customer = LibExcelReader.GetStringCellXls(LibStringFormat.DecodeHtmlString(sheet.GetRow(IndexRow).GetCell(IndexCustomer).EmptyIfNull().ToString().Trim().ToUpperInvariant()));
                                            if (PreItemInvoice.String_Customer.EmptyIfNull().ToString().ToUpperInvariant().StartsWith("GDI") == true) { PreItemInvoice.String_Customer = "GDI"; };
                                            if (PreItemInvoice.String_Item.Trim().Length > 0) // Item Principal
                                            {
                                                
                                                if (PreItemInvoice.IsValidItem())
                                                {
                                                    PreItemInvoice.String_PN = PreItemInvoice.String_PN.EmptyIfNull().Trim();
                                                    PreListaItens.Add(PreItemInvoice);
                                                }
                                                else
                                                {
                                                    ErroProcessamento = true;
                                                    MsgRetorno += " - Erro ao processar a linha [" + (IndexRow + 1).ToString() + "]!" + "<br/>";
                                                }
                                            }
                                            else if (PreItemInvoice.IsDetailItem()) // Item Detalhe
                                            {
                                                PreItemInvoice.String_Item = PreListaItens[PreListaItens.Count - 1].String_Item;
                                                if (PreItemInvoice.IsValidItem())
                                                {
                                                    PreItemInvoice.String_PN = PreItemInvoice.String_PN.EmptyIfNull().Trim();
                                                    PreListaItens.Add(PreItemInvoice);
                                                }
                                                else
                                                {
                                                    ErroProcessamento = true;
                                                    MsgRetorno += " - Erro ao processar a linha [" + (IndexRow + 1).ToString() + "]!" + "<br/>";
                                                }
                                            }
                                            else if (PreItemInvoice.IsRowEmpty())
                                            {
                                                ErroProcessamento = true;
                                                MsgRetorno += " - Linha [" + (IndexRow + 1).ToString() + "] não contém dados!" + "<br/>";
                                            }
                                            else // Complemento de linha (Número Serial)
                                            {
                                                CstModelComexItemInvoice UltimoPreItem = PreListaItens[PreListaItens.Count - 1];
                                                if (PreItemInvoice.String_PN.Trim().Length > 0)
                                                {
                                                    String ItemSerialNumber = PreItemInvoice.String_PN.Replace("SN:", "").Replace("SN :", "").Replace(" ", "").Replace(",", "").Replace(";", "") + ",";
                                                    UltimoPreItem.String_SerialNumber += ItemSerialNumber;
                                                };
                                                PreListaItens[PreListaItens.Count - 1] = UltimoPreItem;
                                            }
                                            IndexRowSheet += 1;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                ErroProcessamento = true;
                                MsgRetorno += "Cabeçalho da planilha NÃO localizado!" + "<br/>";
                            }
                        }
                        catch (Exception)
                        {
                            ErroProcessamento = true;
                            MsgRetorno += "Cabeçalho da planilha NÃO localizado!" + "<br/>";
                        }
                    }


                    IndexItem = 1;
                    foreach (CstModelComexItemInvoice Item in PreListaItens)
                    {
                        if (Item.String_Note == "AKIT")
                        {

                        }
                        else
                        {
                            Item.ItemValido = true;
                            Item.ItemIndex = IndexItem;
                            ListaItens.Add(Item);
                            IndexItem += 1;
                        }
                    }


                    // Salvar Banco Dados
                    if (ErroProcessamento == false)
                    {
                        
                        List<gc_comex_invoices_itens> ListaComexInvoicesItens = db.gc_comex_invoices_itens.Where(i => i.ativo == true && i.id_importacao == CachePersister.userIdentity.IdGcComexImportacaoAtiva && i.id_invoice == RecordComexInvoice.id_invoice).ToList();
                        List<gc_comex_invoices_itens> ListaComexInvoicesItensAtualizar = new List<gc_comex_invoices_itens>();

                        // Processar os itens
                        int _itens1_rows = 0;
                        int _itens2_rows = 0;
                        int _itens1_qty = 0;
                        int _itens2_qty = 0;
                        Decimal _itens1_price = 0;
                        Decimal _itens2_price = 0;
                        Decimal _itens1_weigth = 0;
                        Decimal _itens2_weigth = 0;

                        int QtdItensProcessados = 0;
                        int QtdItensAtualizados = 0;
                        List<CstModelComexItemInvoice> ListaFinal = ListaItens.Where(l => l.ItemIndex >= 0).OrderBy(l => l.IndexRowSheet).ToList();
                        String ListaIdsAtualizados = string.Empty;
                        foreach (CstModelComexItemInvoice ItemImportacao in ListaFinal)
                        {
                            QtdItensProcessados += 1;
                            if (ItemImportacao.ItemValido == true) // NOVO ITEM INVOICE - new gc_comex_invoices_itens
                            {
                                if ((ItemImportacao.String_Customer.EmptyIfNull().ToString().Length == 0) || (ItemImportacao.String_Customer.EmptyIfNull().ToString().StartsWith("GDI")))
                                {
                                    _itens1_rows += 1;
                                    _itens1_qty += ItemImportacao.Int_Qty;
                                    _itens1_price += ItemImportacao.Decimal_TotalPrice;
                                    _itens1_weigth += ItemImportacao.Decimal_TotalWeigth;
                                }
                                else
                                {
                                    _itens2_rows += 1;
                                    _itens2_qty += ItemImportacao.Int_Qty;
                                    _itens2_price += ItemImportacao.Decimal_TotalPrice;
                                    _itens2_weigth += ItemImportacao.Decimal_TotalWeigth;
                                }

                                String PNOficial = ItemImportacao.String_PN.EmptyIfNull().ToString();
                                String PNAuxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(PNOficial);
                                String PNCuringaOH = PNAuxiliar.Replace("0", "O");
                                String PNCuringaZERO = PNAuxiliar.Replace("O", "0");


                                List<gc_comex_invoices_itens> ListaItensEncontrados = ListaComexInvoicesItens.Where(p => p.pn == PNOficial).ToList(); // Buscar pelo PN principal
                                try { if (ListaItensEncontrados.Count == 0) { ListaItensEncontrados = ListaComexInvoicesItens.Where(p => (p.pn_auxiliar == PNAuxiliar || p.pn_variacao1 == PNCuringaOH || p.pn_variacao2 == PNCuringaZERO)).ToList(); }; } catch (Exception) { }; // Buscar pelo PN Auxiliar
                                if (ListaItensEncontrados.Count > 0)
                                {
                                    foreach (gc_comex_invoices_itens ItemInvoice in ListaItensEncontrados)
                                    {
                                        if (ListaIdsAtualizados.IndexOf(ItemInvoice.id_invoice_item.ToString() + ",") < 0 )
                                        {
                                            QtdItensAtualizados += 1;
                                            ItemInvoice.item_unit_price = ItemImportacao.Decimal_UnitPrice;
                                            ItemInvoice.item_total_price = ItemInvoice.item_qty * ItemInvoice.item_unit_price;
                                            ItemInvoice.item_unit_weigth = (ItemImportacao.Decimal_TotalWeigth / ItemInvoice.item_qty);
                                            ItemInvoice.item_total_weigth = ItemInvoice.item_qty * ItemInvoice.item_unit_weigth;
                                            ListaComexInvoicesItensAtualizar.Add(ItemInvoice);
                                            ListaIdsAtualizados += ItemInvoice.id_invoice_item.ToString() + ",";
                                        }
                                    }
                                }
                                else
                                {
                                    QtdItensNaoLocalizados += 1;
                                    ListaItensNaoLocalizados += ItemImportacao.String_PN.EmptyIfNull().ToString() + ", ";
                                }
                            }
                        }

                        //gc_comex_invoices RecordInvoice = db.gc_comex_invoices.Where(i => i.id_importacao == CachePersister.userIdentity.IdGcComexImportacaoAtiva && i.invoice == IdentificadorInvoice).FirstOrDefault();
                        if (RecordComexInvoice != null)
                        {
                            RecordComexInvoice.itens1_rows = _itens1_rows;
                            RecordComexInvoice.itens2_rows = _itens2_rows;
                            RecordComexInvoice.itens_total_rows = _itens1_rows + _itens2_rows;
                            RecordComexInvoice.itens1_qty = _itens1_qty;
                            RecordComexInvoice.itens2_qty = _itens2_qty;
                            RecordComexInvoice.itens_total_qty = _itens1_qty + _itens2_qty;
                            RecordComexInvoice.itens1_price = _itens1_price;
                            RecordComexInvoice.itens2_price = _itens2_price;
                            RecordComexInvoice.itens_total_price = _itens1_price + _itens2_price;
                            RecordComexInvoice.itens_total_debit = RecordComexInvoice.itens1_price;
                            RecordComexInvoice.itens1_weigth = _itens1_weigth;
                            RecordComexInvoice.itens2_weigth = _itens2_weigth;
                            RecordComexInvoice.itens_total_weigth = _itens1_weigth + _itens2_weigth;
                            RecordComexInvoice.datahora_alteracao = DataHoraAtual;
                            RecordComexInvoice.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                            db.Entry(RecordComexInvoice).State = EntityState.Modified;
                        }

                        if (ListaComexInvoicesItensAtualizar.Count() > 0)
                        {
                            foreach (gc_comex_invoices_itens ItemImportacao in ListaComexInvoicesItensAtualizar)
                            {
                                ItemImportacao.datahora_alteracao = DataHoraAtual;
                                ItemImportacao.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                db.Entry(ItemImportacao).State = EntityState.Modified;
                                db.SaveChanges();
                            }
                            db.SaveChanges();
                        }

                        MsgRetorno += "Planilha PROCESSADA</b> com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>";
                        MsgRetorno += "<b>Itens Processados</b> " + QtdItensProcessados.ToString() + "<br/>";
                        MsgRetorno += "<b>Itens Atualizados</b> " + QtdItensAtualizados.ToString() + "<br/>";


                        if (QtdItensNaoLocalizados > 0)
                        {
                            MsgRetorno += "<br/><br/>";
                            MsgRetorno += QtdItensNaoLocalizados.ToString() + " Itens Não Localizados: " + ListaItensNaoLocalizados.ToString() + "<br/>";
                        }

                        Processado = true;
                    }
                }
                catch (DbEntityValidationException ex)
                {
                    return JsonAjaxErroValidacao(ex);
                }
                catch (Exception e)
                {
                    return JsonAjaxErro(e);
                }
            }
            return Json(new { success = Processado, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        public ActionResult AjaxModalReprocessarFobXLS(HttpPostedFileBase filesource)
        {
            int QtdProcessados = 0;
            int QtdItensAtualizadosDB = 0;
            int IndexPN = 0;
            int IndexUnitPrice = 1;
            bool Processado = false;
            bool ErroProcessamento = false;
            String Logs = string.Empty;
            string String_Item_Agrupador = string.Empty;
            string MsgRetorno = string.Empty;
            string ResultadoProcessamento = String.Empty;
            string ListaItensNaoLocalizados = String.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            List<g_produtos> ListaProdutosFob = new List<g_produtos>();
            List<g_produtos> ListaProdutosGDI = db.g_produtos.Where(p => p.ativo == true && p.fob1_dollar == 0).ToList();

            var fileExt = System.IO.Path.GetExtension(filesource.FileName.ToLower()).Substring(1);

            if (ErroProcessamento == false)
            {
                try
                {
                    MsgRetorno = String.Empty;
                    var fileNameOrigem = Path.GetFileName(filesource.FileName);

                    String DirTempFiles = Server.MapPath("~/_filestemp");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, "uploads");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    LibFilesDisk.DeleteFilesInDirectory(DirTempFiles); // Apagar todos os arquivos que estiveremno diretório do usuario
                    var FileNameInvoice = Path.Combine(DirTempFiles, LibDateTime.getDataHoraBrasilia().ToString("yyyyMMddhhmmss") + "_Planilha-Fob_" + fileNameOrigem);
                    filesource.SaveAs(FileNameInvoice);

                    List<CstModelComexItemInvoice> PreListaItens = new List<CstModelComexItemInvoice>();
                    List<CstModelComexItemInvoice> ListaItens = new List<CstModelComexItemInvoice>();

                    //Get the path of specified file
                    FileStream FileTemplate = new FileStream(FileNameInvoice, FileMode.Open, FileAccess.Read);
                    HSSFWorkbook _workbook = new HSSFWorkbook(FileTemplate);
                    ISheet sheet = _workbook.GetSheetAt(0);

                    int IndexRowSheet = 1;
                    for (int IndexRow = 2; IndexRow <= sheet.LastRowNum; IndexRow++)
                    {
                        if (sheet.GetRow(IndexRow) != null)
                        {
                            CstModelComexItemInvoice PreItemInvoice = new CstModelComexItemInvoice();
                            PreItemInvoice.String_PN = LibStringFormat.GDIFormatarCodigoProduto(LibExcelReader.GetStringCellXls(sheet.GetRow(IndexRow).GetCell(IndexPN).EmptyIfNull().ToString().Trim().ToUpperInvariant()));
                            PreItemInvoice.String_PN_Auxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(PreItemInvoice.String_PN);
                            PreItemInvoice.String_PN_Variacao1 = PreItemInvoice.String_PN_Auxiliar.Replace("0", "O");
                            PreItemInvoice.String_PN_Variacao2 = PreItemInvoice.String_PN_Auxiliar.Replace("O", "0");
                            PreItemInvoice.String_UnitPrice = LibExcelReader.GetDecimalCellXls(sheet.GetRow(IndexRow).GetCell(IndexUnitPrice).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                            PreItemInvoice.Decimal_UnitPrice = 0;

                            if ((PreItemInvoice.String_PN.Trim().Length > 0) && (PreItemInvoice.String_PN.Trim().ToUpperInvariant().IndexOf("FRETE") == -1) && (PreItemInvoice.String_UnitPrice.Trim().Length > 0))
                            {
                                decimal UnitPrice = 0;
                                Decimal.TryParse(PreItemInvoice.String_UnitPrice, out UnitPrice);
                                PreItemInvoice.Decimal_UnitPrice = UnitPrice;

                                if (PreItemInvoice.Decimal_UnitPrice > 0)
                                {
                                    PreItemInvoice.Decimal_UnitPrice = PreItemInvoice.Decimal_UnitPrice / 100;

                                    String PNOficial = PreItemInvoice.String_PN.EmptyIfNull().ToString();
                                    String PNAuxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(PNOficial);
                                    String PNCuringaOH = PNAuxiliar.Replace("0", "O");
                                    String PNCuringaZERO = PNAuxiliar.Replace("O", "0");

                                    g_produtos ProdutoGDI = ListaProdutosGDI.Where(p => p.codigo == PNOficial).FirstOrDefault(); // Buscar pelo PN principal
                                    try { if (ProdutoGDI == null) { ProdutoGDI = ListaProdutosGDI.Where(p => p.codigo_auxiliar == PNAuxiliar).FirstOrDefault(); }; } catch (Exception) { }; // Buscar pelo PN Auxiliar
                                    try { if (ProdutoGDI == null) { ProdutoGDI = ListaProdutosGDI.Where(p => p.codigo_variacao1 == PNCuringaOH).FirstOrDefault(); }; } catch (Exception) { }; // Buscar pelo PN Auxiliar OH
                                    try { if (ProdutoGDI == null) { ProdutoGDI = ListaProdutosGDI.Where(p => p.codigo_variacao2 == PNCuringaZERO).FirstOrDefault(); }; } catch (Exception) { }; // Buscar pelo PN Auxiliar ZERO


                                    // Atualização do Produto GDI
                                    if (ProdutoGDI != null)
                                    {
                                        if (ProdutoGDI.fob1_dollar == 0)
                                        {
                                            ProdutoGDI.fob1_dollar = PreItemInvoice.Decimal_UnitPrice;
                                            ProdutoGDI.tag1 = true;
                                            ListaProdutosGDI[ListaProdutosGDI.IndexOf(ProdutoGDI)] = ProdutoGDI;
                                            QtdProcessados += 1;
                                        }
                                    }
                                }
                            }
                            IndexRowSheet += 1;
                        }
                    }

                    foreach (g_produtos RecordProdutoAtualizar in ListaProdutosGDI)
                    {
                        if (RecordProdutoAtualizar.tag1 == true)
                        {
                            RecordProdutoAtualizar.datahora_alteracao = DataHoraAtual;
                            RecordProdutoAtualizar.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                            db.Entry(RecordProdutoAtualizar).State = EntityState.Modified;
                            QtdItensAtualizadosDB += 1;
                        }
                    }

                    if (QtdItensAtualizadosDB > 0) { db.SaveChanges(); };

                    Processado = true;
                    MsgRetorno += QtdProcessados.ToString() + " Itens Processados" + "<br/>";
                    MsgRetorno += QtdItensAtualizadosDB.ToString() + " Itens Atualizados" + "<br/>";
                }
                catch (DbEntityValidationException ex)
                {
                    return JsonAjaxErroValidacao(ex);
                }
                catch (Exception e)
                {
                    return JsonAjaxErro(e);
                }
            }
            return Json(new { success = Processado, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }



        public ActionResult ModalInvoice(String viewIdInvoice)
        {
            int IdInvoice = -1;
            int.TryParse(viewIdInvoice, out IdInvoice);
            if (IdInvoice <= 0)
            {
                ViewBag.MsgBloqueio = GdiMvcJsonResults.EntidadeNaoEncontradaMensagem("Invoice COMEX", null);
                ViewBag.Title = "<b>Itens da Invoice — (não localizada)</b>";
                return View(new gc_comex_invoices());
            }
            gc_comex_invoices record_gc_comex_invoices = db.gc_comex_invoices.Find(IdInvoice);
            if (record_gc_comex_invoices == null)
            {
                ViewBag.MsgBloqueio = GdiMvcJsonResults.EntidadeNaoEncontradaMensagem("Invoice COMEX", IdInvoice);
                ViewBag.Title = "<b>Itens da Invoice — (não localizada)</b>";
                return View(new gc_comex_invoices { id_invoice = IdInvoice });
            }
            ViewBag.Title = "<b>Itens da Invoice </b>" + record_gc_comex_invoices.invoice.EmptyIfNull().ToString();
            return View(record_gc_comex_invoices);
        }

        public ActionResult ModalCambioInvoice(String viewIdInvoice)
        {
            int IdInvoice = -1;
            int.TryParse(viewIdInvoice, out IdInvoice);
            if (IdInvoice <= 0)
            {
                ViewBag.MsgBloqueio = GdiMvcJsonResults.EntidadeNaoEncontradaMensagem("Invoice COMEX", null);
                ViewBag.Title = "<b>Câmbio da Invoice — (não localizada)</b>";
                return View(new gc_comex_invoices());
            }
            gc_comex_invoices record_gc_comex_invoices = db.gc_comex_invoices.Find(IdInvoice);
            if (record_gc_comex_invoices == null)
            {
                ViewBag.MsgBloqueio = GdiMvcJsonResults.EntidadeNaoEncontradaMensagem("Invoice COMEX", IdInvoice);
                ViewBag.Title = "<b>Câmbio da Invoice — (não localizada)</b>";
                return View(new gc_comex_invoices { id_invoice = IdInvoice });
            }
            ViewBag.Title = "<b>Câmbio da Invoice </b>" + record_gc_comex_invoices.invoice.EmptyIfNull().ToString();
            return View(record_gc_comex_invoices);
        }

        public ActionResult ModalCancelarInvoice(String idInvoice)
        {
            ViewBag.Title = "Cancelar Invoice";
            String MsgAdvertencia = String.Empty;
            int IdInvoice = 0;
            if (!int.TryParse(idInvoice, out IdInvoice) || IdInvoice <= 0)
            {
                ViewBag.MsgBloqueio = GdiMvcJsonResults.EntidadeNaoEncontradaMensagem("Invoice COMEX", null);
                ViewBag.Title = "Cancelar Invoice — (não localizada)";
                return View(new gc_comex_invoices());
            }
            gc_comex_invoices record_gc_comex_invoices = db.gc_comex_invoices.Find(IdInvoice);
            if (record_gc_comex_invoices == null)
            {
                ViewBag.MsgBloqueio = GdiMvcJsonResults.EntidadeNaoEncontradaMensagem("Invoice COMEX", IdInvoice);
                ViewBag.Title = "Cancelar Invoice — (não localizada)";
                return View(new gc_comex_invoices { id_invoice = IdInvoice });
            }
            record_gc_comex_invoices.exclusao_motivo = "";
            gc_comex_importacoes RecordImportacao = db.gc_comex_importacoes.Find(record_gc_comex_invoices.id_importacao);
            if (RecordImportacao != null && RecordImportacao.invoices_finalizadas == true)
            {
                MsgAdvertencia += " - Não é possível cancelar as invoices, a planilha de itens já foi processada!";
            }
            ViewBag.MsgAdvertencia = MsgAdvertencia;
            return View(record_gc_comex_invoices);
        }

        [HttpPost]
        public ActionResult AjaxModalCancelarInvoice(gc_comex_invoices modal_gc_comex_invoices)
        {
            bool Sucesso = false;
            bool ErroProcessamento = false;
            String MsgRetorno = String.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            try
            {
                if (modal_gc_comex_invoices.exclusao_motivo.EmptyIfNull().ToString().Length == 0)
                {
                    ErroProcessamento = true;
                    MsgRetorno += "Campo [Motivo] é de preenchimento obrigatório!" + "</br>";
                }
                if (ErroProcessamento == false)
                {
                    gc_comex_invoices RecordComexInvoice = db.gc_comex_invoices.Find(modal_gc_comex_invoices.id_invoice);
                    RecordComexInvoice.ativo = false;
                    RecordComexInvoice.exclusao_datahora = DataHoraAtual;
                    RecordComexInvoice.exclusao_id_usuario = CachePersister.userIdentity.IdUsuario; ;
                    RecordComexInvoice.exclusao_motivo = modal_gc_comex_invoices.exclusao_motivo;
                    RecordComexInvoice.datahora_alteracao = DataHoraAtual;
                    RecordComexInvoice.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                    db.Entry(RecordComexInvoice).State = EntityState.Modified;

                    // Atualizar dados da importação
                    String SqlUpdateComexImportacao = " update gc_comex_importacoes set " +
                                                      " cambio_debito = (select sum(cambio_debito) from gc_comex_invoices where ativo = 1 and id_importacao = " + CachePersister.userIdentity.IdGcComexImportacaoAtiva.ToString() + ") " +
                                                      " where id_importacao = " + CachePersister.userIdentity.IdGcComexImportacaoAtiva.ToString();
                    LibDB.dbQueryExec(SqlUpdateComexImportacao, db);


                    // Atualizar itens da invoce
                    String SqlUpdateComexItensInvoices = "update gc_comex_invoices_itens set ativo = 0 where id_invoice_item > 0 and id_invoice = " + RecordComexInvoice.id_invoice.ToString();
                    LibDB.dbQueryExec(SqlUpdateComexItensInvoices, db);

                    db.SaveChanges();
                    AtualizarFinanceiroComex();
                    Sucesso = true;
                    MsgRetorno += "Invoice <b>Cancelada</b> com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg");
                }
            }
            catch (DbEntityValidationException ex)
            {
                return JsonAjaxErroValidacao(ex);
            }
            catch (Exception e)
            {
                return JsonAjaxErro(e);
            }
            return Json(new { success = Sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AjaxModalCambioInvoice(gc_comex_invoices modal_gc_comex_invoices)
        {
            bool Sucesso = false;
            bool ErroProcessamento = false;
            String MsgRetorno = String.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            try
            {
                if (modal_gc_comex_invoices.cambio_debito.EmptyIfNull().ToString().Length == 0)
                {
                    ErroProcessamento = true;
                    MsgRetorno += "Campo [Câmbio(Débito)] é de preenchimento obrigatório!" + "</br>";
                }
                if (ErroProcessamento == false)
                {
                    gc_comex_invoices record_gc_comex_invoices = db.gc_comex_invoices.Find(modal_gc_comex_invoices.id_invoice);
                    record_gc_comex_invoices.cambio_debito = modal_gc_comex_invoices.cambio_debito;
                    record_gc_comex_invoices.datahora_alteracao = DataHoraAtual;
                    record_gc_comex_invoices.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario; ;
                    db.Entry(record_gc_comex_invoices).State = EntityState.Modified;
                    db.SaveChanges();
                    AtualizarFinanceiroComex();
                    Sucesso = true;
                    MsgRetorno += "Câmbio <b>Atualizado</b> com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg");
                }
            }
            catch (DbEntityValidationException ex)
            {
                return JsonAjaxErroValidacao(ex);
            }
            catch (Exception e)
            {
                return JsonAjaxErro(e);
            }
            return Json(new { success = Sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }

        public Boolean AtualizarFinanceiroComex()
        {
            Boolean Concluido = false;
            try
            {
                // Atualização do Financeiro Comex
                int IdImportacaoAtiva = CachePersister.userIdentity.IdGcComexImportacaoAtiva;
                gc_comex_importacoes record_gc_comex_importacoes = db.gc_comex_importacoes.Find(IdImportacaoAtiva);
                DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
                Decimal SaldoPagoInvoices = 0;
                Decimal TotalCambioDebito = 0;
                Decimal TotalCambioPagoInvoice = 0;
                Decimal TotalCambioPagoImportacao = 0;
                List<Db.gc_comex_invoices> ListaInvoices = db.gc_comex_invoices.Where(i => (i.ativo == true) && (i.id_importacao == IdImportacaoAtiva)).ToList();
                List<Db.gc_comex_invoices> ListaInvoicesAtualizar = new List<Db.gc_comex_invoices>();
                foreach (gc_comex_invoices ItemInvoice in ListaInvoices) { TotalCambioDebito += ItemInvoice.cambio_debito; }
                gc_comex_financeiro record_gc_comex_financeiro = db.gc_comex_financeiro.Where(f => (f.ativo == true) && (f.tipo_pag_rec == 2) && (f.id_importacao == IdImportacaoAtiva)).FirstOrDefault();
                if (record_gc_comex_financeiro == null)
                {
                    String DescricaoFinanceiro = String.Empty;
                    String IdentificadorDI = String.Empty;
                    if (record_gc_comex_importacoes != null) { IdentificadorDI += record_gc_comex_importacoes.di_numero.EmptyIfNull().ToString(); };
                    DescricaoFinanceiro = "Câmbio - Importação (" + record_gc_comex_importacoes.numero.EmptyIfNull().ToString() + "), DI(" + IdentificadorDI + ")";
                    if (DescricaoFinanceiro.Length > 200) { DescricaoFinanceiro = DescricaoFinanceiro.Substring(0, 200); };
                    record_gc_comex_financeiro = new Db.gc_comex_financeiro();
                    record_gc_comex_financeiro.ativo = true;
                    record_gc_comex_financeiro.id_importacao = IdImportacaoAtiva;
                    record_gc_comex_financeiro.tipo_pag_rec = 2;
                    record_gc_comex_financeiro.descricao = DescricaoFinanceiro;
                    record_gc_comex_financeiro.data_vencimento = record_gc_comex_importacoes.data_registro;
                    record_gc_comex_financeiro.data_pagamento = record_gc_comex_importacoes.data_registro;
                    record_gc_comex_financeiro.numero_documento = IdentificadorDI;
                    record_gc_comex_financeiro.valor_total = TotalCambioDebito;
                    record_gc_comex_financeiro.valor_pago = TotalCambioDebito;
                    record_gc_comex_financeiro.datahora_cadastro = DataHoraAtual;
                    record_gc_comex_financeiro.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                    record_gc_comex_financeiro.id_coligada = 1;
                    record_gc_comex_financeiro.id_filial = 1;
                    db.gc_comex_financeiro.Add(record_gc_comex_financeiro);
                }
                else if (record_gc_comex_financeiro != null)
                {
                    TotalCambioPagoImportacao = 0;
                    List<gc_comex_financeiro> ListaPagamentos = db.gc_comex_financeiro.Where(f => f.ativo == true && f.tipo_pag_rec == 1 && f.id_importacao == IdImportacaoAtiva).ToList();
                    foreach (gc_comex_invoices RecordInvoice in ListaInvoices) 
                    {
                        TotalCambioPagoInvoice = 0;
                        foreach (gc_comex_financeiro ItemPagamento in ListaPagamentos)
                        {
                            if (ItemPagamento.id_invoice == RecordInvoice.id_invoice)
                            {
                                TotalCambioPagoInvoice += ItemPagamento.valor_pago;
                            }
                        }
                        RecordInvoice.cambio_credito = TotalCambioPagoInvoice;
                        ListaInvoicesAtualizar.Add(RecordInvoice);
                        TotalCambioPagoImportacao += TotalCambioPagoInvoice;
                    }

                    foreach (gc_comex_invoices RecordInvoiceAtualizar in ListaInvoicesAtualizar)
                    {
                        RecordInvoiceAtualizar.datahora_alteracao = DataHoraAtual;
                        RecordInvoiceAtualizar.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                        db.Entry(RecordInvoiceAtualizar).State = EntityState.Modified;
                    }
                    db.SaveChanges();

                    // Validar Saldo Já Pago das Invoices
                    SaldoPagoInvoices = 0;
                    String SentencaSQL = " select sum(i.cambio_credito) as 'SaldoPago' " +
                                            " from gc_comex_invoices i " +
                                            " where i.ativo = 1 " +
                                            " and i.id_importacao = " + IdImportacaoAtiva;
                    String _SaldoPagoInvoices = LibDB.dbQueryValue(SentencaSQL, db);
                    Decimal.TryParse(_SaldoPagoInvoices, out SaldoPagoInvoices);
                    record_gc_comex_financeiro.valor_total = TotalCambioDebito;
                    record_gc_comex_financeiro.valor_pago = TotalCambioDebito;
                    record_gc_comex_financeiro.datahora_alteracao = DataHoraAtual;
                    record_gc_comex_financeiro.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                    db.Entry(record_gc_comex_financeiro).State = EntityState.Modified;
                }
                if (record_gc_comex_importacoes != null)
                {
                    record_gc_comex_importacoes.cambio_debito = TotalCambioDebito;
                    record_gc_comex_importacoes.cambio_credito = SaldoPagoInvoices;
                    record_gc_comex_importacoes.datahora_alteracao = DataHoraAtual;
                    record_gc_comex_importacoes.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                    db.Entry(record_gc_comex_importacoes).State = EntityState.Modified;
                }
                Concluido = true;
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return Concluido;
        }

        private JsonResult JsonDataTableException(Exception e, jQueryDataTableParamModel param, string yesFilterOnOff)
        {
            return Json(GdiMvcJsonResults.DataTableError(e, param, yesFilterOnOff), JsonRequestBehavior.AllowGet);
        }

        private JsonResult JsonAjaxErro(Exception ex)
        {
            return Json(GdiMvcJsonResults.AjaxFailure(ex), JsonRequestBehavior.AllowGet);
        }

        private JsonResult JsonAjaxErroValidacao(DbEntityValidationException ex)
        {
            return Json(GdiMvcJsonResults.AjaxFailureValidation(ex), JsonRequestBehavior.AllowGet);
        }

    }
}