// Migrado em 2020_07_15


using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using GdiPlataform.Controllers;
using GdiPlataform.Db;
using GdiPlataform.Security;
using GdiPlataform.Lib;
using GdiPlataform.Areas.g.Models;

namespace GdiPlataform.Areas.g.Controllers
{
    [CustomAuthorize(Roles = "SuperAdmin,Admin,g_FinanceiroFaturamentos_*,g_FinanceiroFaturamentos_Default")]
    public class FinanceiroFaturamentosController : Controller
    {
        private GdiPlataformEntities db;
        private readonly String controllerName = "g_FinanceiroFaturamentos";

        public FinanceiroFaturamentosController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }
        
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_FinanceiroFaturamentos_*,g_FinanceiroFaturamentos_Actionread,g_FinanceiroFaturamentosActionrun")]
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Financeiro - Faturamentos";
            return View();
        }

        #region GetDados
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_FinanceiroFaturamentos_*,g_FinanceiroFaturamentos_Actionread")]
        public ActionResult GetDados(jQueryDataTableParamModel param)
        {
            if (param == null) param = new jQueryDataTableParamModel();
            string filterOnOff = "0";
            try
            {
            var allRecords = new List<Db.g_financeiro_faturamentos>();

            if ((param.yesFilterField.EmptyIfNull().ToString().Equals(String.Empty)) && (param.yesFilterAdvancedText.EmptyIfNull().ToString().Equals(String.Empty)))
            {
                // Perfil Adm visualiza todos os registros independente de Coligada e Filial
                if (CachePersister.userIdentity.IdPerfil == 1)
                { allRecords = allRecords = db.g_financeiro_faturamentos.ToList(); }
                else
                { allRecords = db.g_financeiro_faturamentos.Where(p => p.id_financeiro_faturamento > 0).OrderByDescending(p => p.id_financeiro_faturamento).ToList(); }
            }
            else if (!param.yesFilterField.EmptyIfNull().ToString().Equals(String.Empty)) // Filtro Simples
            {
                filterOnOff = "1";
                String SentencaSQL = String.Empty;
                SentencaSQL = "select * from g_financeiro_faturamentos where id_financeiro_faturamento > 0 and ";
                SentencaSQL += LibStringFormat.SentencaSQLFiltroGenerico(param.yesFilterField, param.yesFilterOperador, param.yesFilterText);
                allRecords = db.g_financeiro_faturamentos.SqlQuery(SentencaSQL.ToString()).ToList();
            }
            else if (!param.yesFilterAdvancedText.EmptyIfNull().ToString().Equals(String.Empty)) // Filtro Avançado
            {
                filterOnOff = "1";
                String[] listaCampos = null;
                String SentencaSQL = string.Empty;
                try { listaCampos = param.yesFilterAdvancedText.EmptyIfNull().ToString().Split(';'); } catch (Exception) { listaCampos = new string[1] { "" }; };

                if (listaCampos.Count() == 5)
                {
                    SentencaSQL = "select f.* from g_financeiro_faturamentos f where id_financeiro_faturamento > 0 ";
                    if (!listaCampos[0].ToString().Trim().Equals(String.Empty))
                    {
                        SentencaSQL += " and f.id_financeiro = " + listaCampos[0].ToString().Trim();
                    }
                    if (!listaCampos[1].ToString().Trim().Equals(String.Empty))
                    {
                        SentencaSQL += " and f.id_cliente = " + listaCampos[1].ToString().Trim();
                    }
                    if ((!listaCampos[2].ToString().Trim().Equals(String.Empty)) && (!listaCampos[2].ToString().Trim().Equals("0")))
                    {
                        SentencaSQL += " and f.id_financeiro_status = " + listaCampos[2].ToString().Trim();
                    }
                    if ((!listaCampos[3].ToString().Trim().Equals(String.Empty)) && (!listaCampos[4].ToString().Trim().Equals(String.Empty)))
                    {
                        SentencaSQL += " and f.data_vencimento between '" + DateTime.Parse(listaCampos[3].ToString().Trim()).ToString("yyyy-MM-dd") + " 00:00:00" + "' and '" + DateTime.Parse(listaCampos[4].ToString().Trim()).ToString("yyyy-MM-dd") + " 23:59:59'";
                    }
                    LibDB.setFilterByUser(SentencaSQL, controllerName, true, db);
                    allRecords = db.g_financeiro_faturamentos.SqlQuery(SentencaSQL.ToString()).ToList();
                }
                else
                {
                    allRecords = db.g_financeiro_faturamentos.ToList();
                }
            }

            var displayedRecords = allRecords.Skip(param.iDisplayStart).Take(param.iDisplayLength);
            Func<Db.g_financeiro_faturamentos, string> orderingFunction = (c =>
                                     param.iSortCol_0 == 1 && param.iSortingCols > 0 ? Convert.ToString(c.id_financeiro_faturamento) :
                                     "");

            if (param.sSortDir_0 == "asc") displayedRecords = displayedRecords.OrderBy(orderingFunction);
            else displayedRecords = displayedRecords.OrderByDescending(orderingFunction);

            if (param.iSortingCols > 0)
            {
                if (param.sSortDir_0 == "asc")
                {
                    if (param.iSortCol_0 == 1) { displayedRecords = displayedRecords.OrderByDescending(c => c.id_financeiro_faturamento); }
                }
                else
                {
                    if (param.iSortCol_0 == 1) { displayedRecords = displayedRecords.OrderByDescending(c => c.id_financeiro_faturamento); }
                }
            }

            // Totalizador de Títulos por faturamento
            String sqlTotalizador = "select id_financeiro_faturamento, count(*) from g_financeiro group by id_financeiro_faturamento";
            DataTable tableTotalizador = LibDB.GetDataTable(sqlTotalizador, db);
            //List<DataRow> rowsTableTotalizador = tableTotalizador.AsEnumerable().ToList();

            List<string[]> list = new List<string[]>();
            foreach (var c in displayedRecords)
            {
                DataRow rowTotalizador = tableTotalizador.Select("id_financeiro_faturamento = " + c.id_financeiro_faturamento.EmptyIfNull().ToString()).FirstOrDefault();
                string qtdTitulos = String.Empty;
                if (rowTotalizador != null) { qtdTitulos = rowTotalizador[1].EmptyIfNull().ToString(); };

                list.Add(new[] {
                                    "", // Coluna de Seleção
                                    c.id_financeiro_faturamento.ToString(),
                                    c.descricao.EmptyIfNull().ToString().Trim(),
                                    c.concluido == true ? LibIcons.getIcon("fa-regular fa-thumbs-up", "Concluído", "#008000", "fa-lg") : LibIcons.getIcon("fa-regular fa-thumbs-down", "NÃO Concluído", "cc0000", ""),
                                    c.clientes_notificados == true ? LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") : LibIcons.getIcon("fa-regular fa-thumbs-down", "Cliente NÃO notificados", "cc0000", ""),
                                    c.vendedores_notificados == true ? LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") : LibIcons.getIcon("fa-regular fa-thumbs-down", "Vendedores NÃO notificados", "cc0000", ""),
                                    qtdTitulos,
                                    c.datahora_cadastro.ToString("dd/MM/yy")
                                });
            }

            return Json(new
            {
                errorMessage = "",
                stackTrace = "",
                yesFilterOnOff = filterOnOff,
                sEcho = param.sEcho,
                iTotalRecords = allRecords.Count(),
                iTotalDisplayRecords = allRecords.Count(),
                aaData = list
            },
            JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return JsonDataTableException(e, param, filterOnOff);
            }
        }
        #endregion

        #region ImportarArquivoFaturamentoGestorFranquia
        public ActionResult ModalImportarArquivoFaturamentoGestorFranquia(int? idCliente)
        {
            ViewBag.Title = "Importação Faturamento - Gestor Franquia";
            ViewBag.idCliente = idCliente;
            return View();
        }

        [HttpPost]
        public ActionResult AjaxImportarArquivoFaturamentoGestorFranquia(HttpPostedFileBase filesource)
        {
            bool processado = false;
            bool erroProcessamento = false;            
            String msgRetorno = String.Empty;
            String resultadoProcessamento = String.Empty;
            String idProcessamentoGravado = "0";            
            DateTime dataInicioProcesso = LibDateTime.getDataHoraBrasilia();

            var fileExt = System.IO.Path.GetExtension(filesource.FileName).Substring(1);

            /*if (filesource.ContentLength > 600000)
            {
                erroProcessamento = true;
                msgRetorno = "O Tamanho do arquivo não pode exceder 600 Kb!";
            }*/

            if ((filesource.ContentLength > 0) && (erroProcessamento == false))
            {
                try
                {
                    
                    var fileNameOrigem = Path.GetFileName(filesource.FileName);

                    String DirTempFiles = Server.MapPath("~/_filestemp");
                    DirTempFiles = Path.Combine(DirTempFiles, "uploads");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    LibFilesDisk.DeleteFilesInDirectory(DirTempFiles); // Apagar todos os arquivos que estiveremno diretório do usuario

                    var fileNameDestino = Path.Combine(DirTempFiles, fileNameOrigem);                    
                    filesource.SaveAs(fileNameDestino);
                                       
                    if (!filesource.Equals(""))
                    {
                        resultadoProcessamento = "Arquivo ( " + fileNameOrigem + " ) importador com Sucesso!";

                        DirTempFiles = Server.MapPath("~/_filestemp");
                        DirTempFiles = Path.Combine(DirTempFiles, "uploads");
                        if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                        DirTempFiles = Path.Combine(DirTempFiles, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
                        if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                        LibFilesDisk.DeleteFilesInDirectory(DirTempFiles); // Apagar todos os arquivos que estiveremno diretório do usuario

                        String NomeArquivoSaida = fileNameOrigem.Replace(".csv","");

                        var fileNameProcessamento = Path.Combine(DirTempFiles, LibDateTime.getDataHoraBrasilia().ToString("yyyyMMddhhmmss") + "_Processamento_" + NomeArquivoSaida + ".txt");
                        using (StreamWriter w = new StreamWriter(fileNameProcessamento, true, Encoding.UTF8))
                        {
                            w.Write(resultadoProcessamento); // Write the text
                        }

                        // Atualizar o registro do processamento
                        g_processamento record_g_processamento = new g_processamento();
                        record_g_processamento.id_processamento_tipo = 44; // Upload Arquivo Fatura. GestorFranquia
                        record_g_processamento.id_processamento_modulo = 2; // Relatório Financeiros/Gerenciais
                        record_g_processamento.id_usuario = CachePersister.userIdentity.IdUsuario;
                        record_g_processamento.datahora_inicio = dataInicioProcesso;
                        record_g_processamento.datahora_final = LibDateTime.getDataHoraBrasilia();
                        record_g_processamento.qtd_registros = 0;
                        record_g_processamento.qtd_reg_ok = 0;
                        record_g_processamento.qtd_reg_erro = 0;
                        record_g_processamento.processando = false;
                        record_g_processamento.concluido = true;
                        record_g_processamento.pathfile = fileNameProcessamento;
                        record_g_processamento.id_coligada = 1;
                        record_g_processamento.id_filial = 1;
                        db.g_processamento.Add(record_g_processamento);
                        db.SaveChanges();
                        idProcessamentoGravado = record_g_processamento.id_processamento.ToString();
                    }

                    if (erroProcessamento == false)
                    {
                        processado = true;
                        msgRetorno = "Arquivo importado com sucesso!";                        
                    }
                    else
                    {
                        processado = false;                        
                        msgRetorno = "Não foi possivel importar arquivo!";
                    }
                }
                catch (DbEntityValidationException ex)
                {
                    processado = false;
                    msgRetorno = LibExceptions.getDbEntityValidationException(ex);
                }
                catch (Exception e)
                {
                    processado = false;
                    msgRetorno = LibExceptions.getExceptionShortMessage(e);
                }
            }
            return Json(new { success = processado, msg = msgRetorno, idProcessamento = idProcessamentoGravado }, JsonRequestBehavior.AllowGet);
        }
        #endregion Importar Arquivo - Pefin Credor

        #region ModalAtualizarFaturamentoGestorFranquia - Atualizar Cadastro GestorFranquia
        public ActionResult ModalAtualizarFaturamentoGestorFranquia(String id)
        {
            ViewBag.Title = "Sincronizar Faturamento - Gestor Franquia";
            TempData.Remove("IdCliente");
            TempData["IdCliente"] = id;
            TempData.Keep("IdCliente");
            g_financeiro record_g_financeiro = new g_financeiro();
            return View(record_g_financeiro);
        }

        [HttpPost]
        public ActionResult AjaxAtualizarFaturamentoGestorFranquia(g_financeiro view_record_g_financeiro)
        {

            return null;
        }
        #endregion

        #region ModalEnviarEmailsClientes
        public ActionResult ModalEnviarEmailsClientes(int id)
        {
            ViewBag.Title = "Comunicado de Faturamento - Email Clientes";
            g_financeiro_faturamentos record_g_financeiro_faturamentos = db.g_financeiro_faturamentos.Find(id);
            ViewBag.clientesNotificados = record_g_financeiro_faturamentos.clientes_notificados == true ? "Sim" : "Não";
            ViewBag.vendedoresNotificados = record_g_financeiro_faturamentos.vendedores_notificados == true ? "Sim" : "Não";
            String sqlTotalizador = "select id_financeiro_faturamento, count(*) from g_financeiro where id_financeiro_faturamento = " + record_g_financeiro_faturamentos.id_financeiro_faturamento.EmptyIfNull().ToString();
            DataTable tableTotalizador = LibDB.GetDataTable(sqlTotalizador, db);
            DataRow rowTotalizador = tableTotalizador.Select("id_financeiro_faturamento = " + record_g_financeiro_faturamentos.id_financeiro_faturamento.EmptyIfNull().ToString()).FirstOrDefault();
            string qtdTitulos = String.Empty;
            if (rowTotalizador != null) { qtdTitulos = rowTotalizador[1].EmptyIfNull().ToString(); };
            ViewBag.qtdTitulos = qtdTitulos;
            return View(record_g_financeiro_faturamentos);
        }

        [HttpPost]
        public ActionResult AjaxEnviarEmailsClientes(g_financeiro_faturamentos view_record_g_financeiro_faturamentos)
        {
            bool sucesso = false;
            bool erroProcessamento = false;
            String resultadoProcessamento = String.Empty;
            String msgRetorno = String.Empty;
            String msgErroProcessamento = String.Empty;
            int qtdEmailsEnviados = 0;

            try
            {
                g_filiais record_g_filiais = db.g_filiais.Where(f => f.id_filial == 1).FirstOrDefault();

                var allRecords = (from _f in db.g_financeiro
                                  join _c in db.g_clientes on _f.id_cliente equals _c.id_cliente
                                  where _f.id_financeiro_faturamento == view_record_g_financeiro_faturamentos.id_financeiro_faturamento
                                  select new { tableFinanceiro = _f, tableClientes = _c }).ToList();

                if (allRecords.Count > 0)
                {
                    foreach (var record in allRecords)
                    {
                        String MensagemEmail = string.Empty;
                        //String AssuntoEmail = "ERP Web - Faturamento de Informações Cadastrais";
                        MensagemEmail += "<b><u>Faturamento de Informações Cadastrais</u></b><br/><br/>";
                        MensagemEmail += "<br/>";
                        MensagemEmail += "Prezado Cliente " + record.tableClientes.nome.EmptyIfNull().ToString() + "<br/><br/>";
                        MensagemEmail += "Já está disponível no portal do cliente a Fatura referente aos serviços de informações cadastrais" + "<br/>";
                        MensagemEmail += "Acesse o " + "<a href=\"http://global.gestortech.com.br/UserIdentity\">Portal do Cliente</a>" + " ter acesso aos documentos" + "<br/>";
                        MensagemEmail += "Preencha no campo [Usuário] o número do seu CPF ou CNPJ" + "<br/>";
                        MensagemEmail += "Preencha no campo [Senha] os 6(seis) primeiros dígitos de seu CPF ou CNPJ" + "<br/>";
                        MensagemEmail += "Após o primeiro acesso, recomendamos a troca da senha, no botão superior direito do portal do cliente" + "<br/><br/>";
                        MensagemEmail += "<b>Valor da Fatura: " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", record.tableFinanceiro.valor_total_liquido) + "</b><br/>";
                        MensagemEmail += "<b>Vencimento da Fatura: " + record.tableFinanceiro.data_vencimento.ToString("dd/MM/yy") + "</b><br/>";

                        String EmailDestinatario = record.tableClientes.email_principal.EmptyIfNull().ToString();
                        if (EmailDestinatario.IndexOf(";") >= 0) { EmailDestinatario = EmailDestinatario.Substring(0, EmailDestinatario.IndexOf(";")); }
                        if (EmailDestinatario.Trim().Length > 0)
                        {
                            //LibEmail.EnviarEmailAWS(record_g_filiais.email_contato.EmptyIfNull().ToString(), record_g_filiais.nome.EmptyIfNull().ToString(), EmailDestinatario, "Financeiro - Faturamento", AssuntoEmail, MensagemEmail, "");
                            qtdEmailsEnviados += 1;
                        }
                    }
                }

                g_financeiro_faturamentos record_g_financeiro_faturamentos = db.g_financeiro_faturamentos.Find(view_record_g_financeiro_faturamentos.id_financeiro_faturamento);
                record_g_financeiro_faturamentos.clientes_notificados = true;
                record_g_financeiro_faturamentos.id_usuario_notificacao_clientes = CachePersister.userIdentity.IdUsuario;
                record_g_financeiro_faturamentos.datahora_notificacao_clientes = LibDateTime.getDataHoraBrasilia();
                db.Entry(record_g_financeiro_faturamentos).State = EntityState.Modified;
                db.SaveChanges();
            }
            catch (Exception e)
            {
                erroProcessamento = true;
                msgErroProcessamento = e.Message.ToString();
                if (e.InnerException.Message.ToString() != String.Empty)
                {
                    msgErroProcessamento += "<br/><br/>" + e.InnerException.Message.ToString();
                }
            }


            if (erroProcessamento == false)
            {
                sucesso = true;
                msgRetorno = qtdEmailsEnviados.ToString() + " Cliente(s) notificado(s) com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg");
            }
            else
            {
                sucesso = false;
                msgRetorno = "Erro no processo de Notificação de Cliente " + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-solid fa-circle-xmark", "Erro", "red", "") + "<br/><br/>" + msgErroProcessamento;
            }
            return Json(new { success = sucesso, msg = msgRetorno, idProcessamento = 0 }, JsonRequestBehavior.AllowGet);
        }
        #endregion    

        #region ModalEnviarNFEmailCliente
        public ActionResult ModalEnviarNFEmailCliente(int id)
        {
            
            ViewBag.Title = "Envio - Nota Fiscal Eletrônica Para Email Cliente";
            g_financeiro_faturamentos record_g_financeiro_faturamentos = db.g_financeiro_faturamentos.Find(id);            
            ViewBag.clientesNotificados = record_g_financeiro_faturamentos.clientes_notificados == true ? "Sim" : "Não";
            ViewBag.vendedoresNotificados = record_g_financeiro_faturamentos.vendedores_notificados == true ? "Sim" : "Não";            
            String sqlTotalizador = "SELECT F.id_financeiro_faturamento, count(*) FROM g_nfe N inner join g_financeiro F on(N.id_financeiro = F.id_financeiro) where url_pdf is not null and F.id_financeiro_faturamento = " + record_g_financeiro_faturamentos.id_financeiro_faturamento.EmptyIfNull().ToString();
            DataTable tableTotalizador = LibDB.GetDataTable(sqlTotalizador, db);
            DataRow rowTotalizador = tableTotalizador.Select("id_financeiro_faturamento = " + record_g_financeiro_faturamentos.id_financeiro_faturamento.EmptyIfNull().ToString()).FirstOrDefault();
            string qtdNfe = String.Empty;
            
            if (rowTotalizador != null) 
            { 
                qtdNfe = rowTotalizador[1].EmptyIfNull().ToString(); 
            }
            ViewBag.qtdNfe = qtdNfe;
            return View(record_g_financeiro_faturamentos);
        }
        public ActionResult ajaxSimularEnviarNFEmailCliente(g_financeiro_faturamentos view_record_g_financeiro_faturamentos)
        {
            cstFinanceiroFaturamentosEnviarNFe record_cstFinanceiroFaturamentosEnviarNFe = new cstFinanceiroFaturamentosEnviarNFe();
            record_cstFinanceiroFaturamentosEnviarNFe.idFinanceiroFaturamento = view_record_g_financeiro_faturamentos.id_financeiro_faturamento;
            record_cstFinanceiroFaturamentosEnviarNFe.simulacao = true;
            return AjaxEnviarNFEmailCliente(record_cstFinanceiroFaturamentosEnviarNFe);
        }
        public ActionResult ajaxProcessarEnviarNFEmailCliente(g_financeiro_faturamentos view_record_g_financeiro_faturamentos)
        {
            cstFinanceiroFaturamentosEnviarNFe record_cstFinanceiroFaturamentosEnviarNFe = new cstFinanceiroFaturamentosEnviarNFe();
            record_cstFinanceiroFaturamentosEnviarNFe.idFinanceiroFaturamento = view_record_g_financeiro_faturamentos.id_financeiro_faturamento;
            record_cstFinanceiroFaturamentosEnviarNFe.simulacao = false;
            return AjaxEnviarNFEmailCliente(record_cstFinanceiroFaturamentosEnviarNFe);
        }

        [HttpPost]
        public ActionResult AjaxEnviarNFEmailCliente(cstFinanceiroFaturamentosEnviarNFe record_cstFinanceiroFaturamentosEnviarNFe)
        {
            bool sucesso = false;
            bool erroProcessamento = false;
            String resultadoProcessamento = String.Empty;
            String msgRetorno = String.Empty;
            String msgErroProcessamento = String.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            String arquivoSaida = String.Empty;
            String status = String.Empty;
            bool enviarEmail = false;
            int qtdEmailsEnviados = 0;
            int qtdEmailsVerificados = 0;
            int qtdEmailsNaoPodeSerEnviados = 0;

            String fileNameDestino = String.Empty;
            String fileNameExportacao = String.Empty;
            String idProcessamentoGravado = "0";

            try
            {
                g_filiais record_g_filiais = db.g_filiais.Where(f => f.id_filial == 1).FirstOrDefault();

                // 2023-05-24 - SQL buscar apenas nota fiscal com pdf e xml
                String sqlRegistro = " SELECT N.id_nfe, N.descricao, N.id_cliente, N.nome, N.razao_social, " +
                                     " N.email, N.valor_total_liquido, N.url_pdf, N.url_xml " +
                                     " FROM g_nfe N " +
                                     " inner join g_financeiro F on(N.id_financeiro = F.id_financeiro) " +
                                     //" where N.id_nfe in (79) ";
                                     " where url_pdf is not null and F.id_financeiro_faturamento = " + record_cstFinanceiroFaturamentosEnviarNFe.idFinanceiroFaturamento;

                DataTable tableRegistro = LibDB.GetDataTable(sqlRegistro, db);
                List<DataRow> rowsTableRegistro = tableRegistro.AsEnumerable().ToList();

                if (rowsTableRegistro.Count > 0)
                {
                    //2023-06-05 - Cabeçalho geração arquivo
                    arquivoSaida += "Id. Cliente; Nome; Id. NFe; E-mail; R$ Valor; Status" + "\r\n";

                    foreach (var linhaRegistro in rowsTableRegistro)
                    {
                        qtdEmailsVerificados += 1;

                        String Email = String.Empty;
                        String EmailDestinatario = String.Empty;
                        String EmailDestinatarioValidado = String.Empty;
                        String DescricaoServico = String.Empty;
                        String AssuntoEmail = "ERP Web - Nota Fiscal Eletrônica - " + record_g_filiais.nome;                        

                        DescricaoServico = linhaRegistro["descricao"].EmptyIfNull().ToString().Trim();
                        DescricaoServico = DescricaoServico.Replace("SERVICOS NO PERIODO DE ", "");
                        DescricaoServico = DescricaoServico.Replace(" A ", "");
                        DescricaoServico = DescricaoServico.Substring(0, 2) + "/" + DescricaoServico.Substring(2, 2) + "/" + DescricaoServico.Substring(4, 4) + " a " + DescricaoServico.Substring(8, 2) + "/" + DescricaoServico.Substring(10, 2) + "/" + DescricaoServico.Substring(12, 4);

                        Email = "<!DOCTYPE html> " +
                                "<html lang='pt-br'> " +
                                "<head> " +
                                "    <meta charset='utf - 8'> " +
                                "    <meta http-equiv='X-UA-Compatible' content='IE=edge'> " +
                                "    <meta name='viewport' content='width=device-width, initial-scale=1'> " +
                                "    <meta name='description' content=''> " +
                                "    <meta name='author' content=''> " +
                                "    <title>Nota Fiscal Eletrônica</title> " +
                                "</head> " +
                                "<body> " +                                
                                "<br /> Prezado(a) Cliente(a): " + linhaRegistro["nome"].EmptyIfNull().ToString().Trim() + ", " +
                                "<br /><br /> A NF-e do serviço do período de "+ DescricaoServico + " no valor de R$ " + linhaRegistro["valor_total_liquido"].EmptyIfNull().ToString().Trim() + " já está disponível. <br /> " +
                                "<br /> <a href="+ linhaRegistro["url_pdf"].EmptyIfNull().ToString().Trim() + "><img src=\"https://bucket-gdi-public-files.s3.sa-east-1.amazonaws.com/FilesImages/icons_pdf.png\" class=\"media-object  img-responsive img-thumbnail\"></a>    <a href=" + linhaRegistro["url_xml"].EmptyIfNull().ToString().Trim() + "><img src=\"https://bucket-gdi-public-files.s3.sa-east-1.amazonaws.com/FilesImages/icons_xml.png\" class=\"media-object  img-responsive img-thumbnail\"></a> " +                                
                                "<br /><br /><br /> Para quaisquer dúvidas, entre em contato com a Central de Atendimento." +
                                "<br />----- Email automático -----" +
                                "</body> " +
                                "</html> ";

                        EmailDestinatario = linhaRegistro["email"].EmptyIfNull().ToString().Trim();

                        if ((EmailDestinatario.EmptyIfNull().ToString().Trim().Length > 0) && (EmailDestinatario != "NULL"))
                        {
                            EmailDestinatarioValidado = LibStringFormat.RemoverAcentos(EmailDestinatario);

                            if (EmailDestinatario.Equals(EmailDestinatarioValidado))
                            {
                                enviarEmail = true;

                                if (record_cstFinanceiroFaturamentosEnviarNFe.simulacao == true)
                                {
                                    status = "Simulação - Será possivel enviar NFe com Sucesso!";
                                }
                                else
                                {
                                    status = "NFe enviada com Sucesso!";
                                }
                            }
                            else
                            {
                                enviarEmail = false;
                                qtdEmailsNaoPodeSerEnviados += 1;

                                if (record_cstFinanceiroFaturamentosEnviarNFe.simulacao == true)
                                {
                                    status = "Simulação - NÃO SERÁ POSSIVEL ENVIAR O E-MAIL, ERRO DE CADASTRO, VALIDE O E-MAIL!";
                                }
                                else
                                {
                                    status = "NÃO SERÁ POSSIVEL ENVIAR O E-MAIL, ERRO DE CADASTRO, VALIDE O E-MAIL!";
                                }
                            }                             
                        }
                        else
                        {                         
                            enviarEmail = false;
                            status = "NÃO SERÁ POSSIVEL ENVIAR O E-MAIL, E-MAIL NÃO CADASTRADO!";
                        }

                        if (EmailDestinatario.EmptyIfNull().ToString().Trim().Length > 0)
                        {
                            string email = EmailDestinatario;
                            if ((email.IndexOf("@") == -1) || (email.IndexOf(".") == -1))
                            {                                
                                enviarEmail = false;
                                status = "NÃO SERÁ POSSIVEL ENVIAR O E-MAIL, ERRO DE CADASTRO, VALIDE E-MAIL!";
                            }
                        }

                        if (EmailDestinatario.EmptyIfNull().ToString().Trim().Length > 0)
                        {
                            string email = EmailDestinatario;
                            if (email.IndexOf(",") > 0)
                            {                                
                                enviarEmail = false;
                                status = "NÃO SERÁ POSSIVEL ENVIAR O E-MAIL, ERRO DE CADASTRO, E-MAIL CADASTRADO COM VIRGULA!";
                            }
                        }

                        if ((enviarEmail == true) && (record_cstFinanceiroFaturamentosEnviarNFe.simulacao == false))
                        {

                            g_nfe_envio_email_log record_g_nfe_envio_email_log = new g_nfe_envio_email_log();
                            record_g_nfe_envio_email_log.id_financeiro_faturamento = record_cstFinanceiroFaturamentosEnviarNFe.idFinanceiroFaturamento;
                            record_g_nfe_envio_email_log.id_cliente = Convert.ToInt32(linhaRegistro["id_cliente"].EmptyIfNull().ToString().Trim());
                            record_g_nfe_envio_email_log.id_nfe = Convert.ToInt32(linhaRegistro["id_nfe"].EmptyIfNull().ToString().Trim());
                            record_g_nfe_envio_email_log.email_enviado = EmailDestinatario;
                            record_g_nfe_envio_email_log.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                            record_g_nfe_envio_email_log.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                            db.g_nfe_envio_email_log.Add(record_g_nfe_envio_email_log);
                            db.SaveChanges();

                            //LibEmail.EnviarEmailAWS(record_g_filiais.email_contato.EmptyIfNull().ToString(), record_g_filiais.nome.EmptyIfNull().ToString(), EmailDestinatario, "Financeiro - NFe", AssuntoEmail, Email, "");                            
                            qtdEmailsEnviados += 1;
                        }

                        arquivoSaida += linhaRegistro["id_cliente"].EmptyIfNull().ToString().Trim() + ";";
                        arquivoSaida += linhaRegistro["nome"].EmptyIfNull().ToString().Trim() + ";";
                        arquivoSaida += linhaRegistro["id_nfe"].EmptyIfNull().ToString().Trim() + ";";
                        arquivoSaida += EmailDestinatario + ";";
                        arquivoSaida += linhaRegistro["valor_total_liquido"].EmptyIfNull().ToString().Trim() + ";";
                        arquivoSaida += status + ";";
                        arquivoSaida += "\r\n";
                    }
                }

                g_financeiro_faturamentos record_g_financeiro_faturamentos = db.g_financeiro_faturamentos.Find(record_cstFinanceiroFaturamentosEnviarNFe.idFinanceiroFaturamento);
                record_g_financeiro_faturamentos.clientes_notificados = true;
                record_g_financeiro_faturamentos.id_usuario_notificacao_clientes = CachePersister.userIdentity.IdUsuario;
                record_g_financeiro_faturamentos.datahora_notificacao_clientes = LibDateTime.getDataHoraBrasilia();
                db.Entry(record_g_financeiro_faturamentos).State = EntityState.Modified;
                db.SaveChanges();
            }
            catch (Exception e)
            {
                erroProcessamento = true;
                msgErroProcessamento = e.Message.ToString();
                if (e.InnerException.Message.ToString() != String.Empty)
                {
                    msgErroProcessamento += "<br/><br/>" + e.InnerException.Message.ToString();
                }
            }

            if (erroProcessamento == false)
            {
                // Salvar o arquivo em disco

                if (record_cstFinanceiroFaturamentosEnviarNFe.simulacao == true)
                {
                    fileNameExportacao = "Simulação_Envio_NFe" + LibDateTime.getDataHoraBrasilia().ToString("yyyyMMddhhmmss") + ".csv";
                }
                else
                {
                    fileNameExportacao = "Envio_NFe" + LibDateTime.getDataHoraBrasilia().ToString("yyyyMMddhhmmss") + ".csv";
                }

                String DirTempFiles = Server.MapPath("~/_filestemp");
                if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                DirTempFiles = Path.Combine(DirTempFiles, "reports");
                if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                DirTempFiles = Path.Combine(DirTempFiles, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
                if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                LibFilesDisk.DeleteFilesInDirectory(DirTempFiles); // Apagar todos os arquivos que estiveremno diretório do usuario
                fileNameDestino = Path.Combine(DirTempFiles, LibDateTime.getDataHoraBrasilia().ToString("yyyyMMddhhmmss") + "_" + fileNameExportacao);
                using (StreamWriter w = new StreamWriter(fileNameDestino, true, Encoding.UTF8))
                {
                    w.Write(arquivoSaida); // Write the text)
                }

                // Atualizar o registro do processamento
                g_processamento record_g_processamento = new g_processamento();
                record_g_processamento.id_processamento_tipo = 45; // Relatório envio de E-mail NFe
                record_g_processamento.id_processamento_modulo = 2; // Relatório Financeiros/Gerenciais
                record_g_processamento.detalhamento = "Relatório Envio NFe Por Email";
                record_g_processamento.id_usuario = CachePersister.userIdentity.IdUsuario;
                record_g_processamento.datahora_inicio = DataHoraAtual;
                record_g_processamento.datahora_final = LibDateTime.getDataHoraBrasilia();
                record_g_processamento.qtd_registros = qtdEmailsVerificados;
                record_g_processamento.qtd_reg_ok = qtdEmailsEnviados;
                record_g_processamento.qtd_reg_erro = 0;
                record_g_processamento.processando = false;
                record_g_processamento.concluido = true;
                record_g_processamento.pathfile = fileNameDestino;
                record_g_processamento.id_coligada = 1;
                record_g_processamento.id_filial = 1;                
                db.g_processamento.Add(record_g_processamento);
                db.SaveChanges();

                sucesso = true;
                idProcessamentoGravado = record_g_processamento.id_processamento.ToString();

                if (record_cstFinanceiroFaturamentosEnviarNFe.simulacao == true)
                {
                    msgRetorno = "Processo Simulado com Sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>" +
                                 "Total E-mail Verificados.: " + qtdEmailsVerificados.ToString() + "<br/>" +
                                 "Total E-mail Enviados.: " + qtdEmailsEnviados.ToString() + "<br/>" +
                                 "Total E-mail Que NÃO Será Enviado(s).: " + qtdEmailsNaoPodeSerEnviados.ToString() + "<br/>";
                }
                else
                {
                    msgRetorno = "Processo Gerado com Sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>" +
                                 "Total E-mail Verificados.: " + qtdEmailsVerificados.ToString() + "<br/>" +
                                 "Total E-mail Enviados.: " + qtdEmailsEnviados.ToString() + "<br/>" +
                                 "Total E-mail Que NÃO Será Enviado(s).: " + qtdEmailsNaoPodeSerEnviados.ToString() + "<br/>";
                }   
            }
            else
            {
                sucesso = false;
                msgRetorno = "Erro no processo de Notificação de Cliente " + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-solid fa-circle-xmark", "Erro", "red", "") + "<br/><br/>" + msgErroProcessamento;
            }
            return Json(new { success = sucesso, msg = msgRetorno, idProcessamento = idProcessamentoGravado }, JsonRequestBehavior.AllowGet);
        }
        #endregion    

        #region AjaxRelComissaoRegra2TabelaPorVendedor
        [HttpPost]
        public ActionResult AjaxRelComissaoRegra2Tabela(g_financeiro_faturamentos view_record_g_financeiro_faturamentos)
        {
            int idVendedor = 0;
            bool sucesso = false;
            bool erroProcessamento = false;
            String idProcessamentoGravado = "0";
            String arquivoSaida = String.Empty;
            String fileNameDestino = String.Empty;
            String fileNameExportacao = String.Empty;
            String resultadoProcessamento = String.Empty;
            String msgRetorno = String.Empty;
            String msgErroProcessamento = String.Empty;
            String SentencaSQLFinanceiro = String.Empty;
            String SentencaSQLLancamentos = String.Empty;
            String quebraLinha = "\r\n";
            DateTime datahora = LibDateTime.getDataHoraBrasilia();
            DataTable tableFinanceiro = null;
            DataTable tableLancamentos = null;
            List<DataRow> allFinanceiro = null;
            List<DataRow> allLancamentos = null;
            List<DataRow> allLancamentosTitulo = null;
            int qtdRegistros = 0;

            try
            {
                idVendedor = view_record_g_financeiro_faturamentos.id_usuario_cadastro;

                SentencaSQLFinanceiro = "select f.id_financeiro, f.id_financeiro_status, f.valor_total_bruto, f.valor_encargos, " +
                                        "f.cnab_nosso_numero, f.data_vencimento, " +
                                        "(f.iss_valor + f.ir_valor + f.pis_valor + f.cofins_valor + f.csll_valor + f.pcc_valor + f.inss_valor) as \"valor_total_impostos\", " +
                                        "c.cpf, c.cnpj, c.id_cliente, c.valor_consumo_minimo, c.nome, c.nf_tipo, c.nf_percentual, " +
                                        "s.nome as \"status\", v.regra2_perce_total_boleto_sem_consulta, v.nome as \"vendedor\" " +
                                        "from g_financeiro f " +
                                        "left join g_clientes c on (c.id_cliente = f.id_cliente) " +
                                        "left join g_financeiro_status s on (s.id_financeiro_status = f.id_financeiro_status) " +
                                        "left join g_vendedores v on (v.id_vendedor = c.id_vendedor) " +
                                        "where f.id_financeiro_faturamento = " + view_record_g_financeiro_faturamentos.id_financeiro_faturamento.ToString();
                if (idVendedor > 0) { SentencaSQLFinanceiro += " and v.id_vendedor = " + idVendedor.ToString(); }
                if (view_record_g_financeiro_faturamentos.id_financeiro_status > 0) { SentencaSQLFinanceiro += " and f.id_financeiro_status = " + view_record_g_financeiro_faturamentos.id_financeiro_status.ToString(); }
                SentencaSQLFinanceiro += " order by v.nome, c.nome ";


                SentencaSQLLancamentos = "select f.id_financeiro_lancamento, f.id_financeiro, f.id_financeiro_faturamento, " +
                                        "f.id_produto_servico, f.id_cliente, f.qtd, f.valor_total_bruto, " +
                                        "p.nome as \"produto\", c2.nome as \"consulta\", ctc.valor_unit as \"valor_cliente\", ctv.valor_unit as \"valor_vendedor\" " +
                                        "from g_financeiro_lancamentos f " +
                                        "left join g_clientes c on (c.id_cliente = f.id_cliente) " +
                                        "left join g_produtos p on (p.id_produto = f.id_produto_servico) " +
                                        "left join gdc_consultas c2 on (c2.id_produto = p.id_produto) " +
                                        "left join gdc_consultas_tabelas_detalhes ctc on (ctc.id_consulta_tabela = c.id_consulta_tabela and ctc.id_consulta = c2.id_consulta) " +
                                        "left join gdc_consultas_tabelas_vendedores ctv on (ctv.id_vendedor = c.id_vendedor and ctc.id_consulta = ctv.id_consulta) " +
                                        "where f.id_financeiro_faturamento = " + view_record_g_financeiro_faturamentos.id_financeiro_faturamento.ToString() + " " +
                                        "and p.id_produto_tipo = 3 ";
                if (idVendedor > 0) { SentencaSQLLancamentos += " and c.id_vendedor = " + idVendedor.ToString(); }
                if (view_record_g_financeiro_faturamentos.id_financeiro_status > 0) { SentencaSQLFinanceiro += " and f.id_financeiro_status = " + view_record_g_financeiro_faturamentos.id_financeiro_status.ToString(); }


                try
                {
                    tableFinanceiro = LibDB.GetDataTable(SentencaSQLFinanceiro, db);
                    allFinanceiro = tableFinanceiro.AsEnumerable().ToList();
                    tableLancamentos = LibDB.GetDataTable(SentencaSQLLancamentos, db);
                    allLancamentos = tableLancamentos.AsEnumerable().ToList();
                }
                catch (Exception e)
                {
                    sucesso = false;
                    erroProcessamento = true;
                    msgRetorno = LibExceptions.getExceptionShortMessage(e);
                };


                if ((allFinanceiro.Count > 0) && (erroProcessamento == false))
                {
                    arquivoSaida = "VENDEDOR;NOSSO-NUMERO;STATUS-FATURA;CPF/CNPJ;CÓD-CLIENTE-PRODUTO;QUANTIDADE;TABELA-VENDA;SUBTOTAL;FATURA;IMPOSTOS;TABELA-VENDEDOR;CUSTO;VCM;RETENÇÃO;COMISSÃO;OBS;" + quebraLinha;
                    foreach (var dsRow in allFinanceiro)
                    {
                        decimal totalQtdConsultas = 0;
                        decimal totalValorConsultas = 0;
                        decimal valorTotalBruto = 0;
                        decimal totalCustoVendedor = 0;
                        decimal valorEncargos = 0;
                        decimal valorTotalImpostos = 0;
                        decimal valorVCMCliente = 0;
                        decimal valorNfeCliente = 0;
                        decimal valorNfeClienteCalculado = 0;
                        String linhaLancamentos = string.Empty;
                        String idFinanceiro = dsRow["id_financeiro"].EmptyIfNull().ToString();
                        String nfTipo = dsRow["nf_tipo"].EmptyIfNull().ToString();
                        allLancamentosTitulo = tableLancamentos.Select("id_financeiro = " + idFinanceiro).AsEnumerable().ToList();

                        decimal.TryParse(dsRow["valor_total_bruto"].EmptyIfNull().ToString(), out valorTotalBruto);
                        decimal.TryParse(dsRow["valor_encargos"].EmptyIfNull().ToString(), out valorEncargos);
                        decimal.TryParse(dsRow["valor_total_impostos"].EmptyIfNull().ToString(), out valorTotalImpostos);
                        decimal.TryParse(dsRow["valor_consumo_minimo"].EmptyIfNull().ToString(), out valorVCMCliente);
                        decimal.TryParse(dsRow["nf_percentual"].EmptyIfNull().ToString(), out valorNfeCliente);

                        if (allLancamentosTitulo.Count > 0)
                        {
                            decimal qtdConsultas = 0;
                            decimal valorConsultas = 0;
                            decimal precoCliente = 0;
                            decimal precoVendedor = 0;
                            valorNfeClienteCalculado = 0;
                            linhaLancamentos = string.Empty;
                            foreach (var dsRowLancamentos in allLancamentosTitulo)
                            {
                                /* DEBUG DEIXA APENAS ATE HOMOLOGAR
                                if(idFinanceiro == "54")
                                {
                                    totalCustoVendedor += (qtdConsultas * precoVendedor);
                                }
                                */
                        decimal.TryParse(dsRowLancamentos["qtd"].EmptyIfNull().ToString(), out qtdConsultas);
                                decimal.TryParse(dsRowLancamentos["valor_total_bruto"].EmptyIfNull().ToString(), out valorConsultas);
                                decimal.TryParse(dsRowLancamentos["valor_cliente"].EmptyIfNull().ToString(), out precoCliente);
                                decimal.TryParse(dsRowLancamentos["valor_vendedor"].EmptyIfNull().ToString(), out precoVendedor);
                                totalQtdConsultas += qtdConsultas;
                                totalValorConsultas += valorConsultas;
                                totalCustoVendedor += (qtdConsultas * precoVendedor);
                                linhaLancamentos += dsRow["vendedor"].EmptyIfNull().ToString().Trim() + ";";
                                linhaLancamentos += dsRow["cnab_nosso_numero"].EmptyIfNull().ToString().Trim() + ";";
                                linhaLancamentos += dsRow["status"].EmptyIfNull().ToString().Trim() + ";";
                                linhaLancamentos += ";";
                                linhaLancamentos += "->   " + dsRowLancamentos["produto"].EmptyIfNull().ToString().Trim() + ";";
                                linhaLancamentos += qtdConsultas.ToString().Trim() + ";";
                                linhaLancamentos += string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", precoCliente) + ";";
                                linhaLancamentos += string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", valorConsultas) + ";";
                                linhaLancamentos += ";";
                                linhaLancamentos += ";";
                                linhaLancamentos += string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", precoVendedor) + ";";
                                linhaLancamentos += string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", (qtdConsultas * precoVendedor)) + ";";
                                linhaLancamentos += quebraLinha;
                            }
                        }
                        else
                        {
                            decimal percentualBoletoSemConsulta = 0;
                            decimal.TryParse(dsRow["regra2_perce_total_boleto_sem_consulta"].EmptyIfNull().ToString(), out percentualBoletoSemConsulta);
                            if (percentualBoletoSemConsulta > 0)
                            {
                                totalCustoVendedor = ((percentualBoletoSemConsulta / 100) * valorVCMCliente);
                                //totalCustoVendedor += valorEncargos;
                            }
                        }

                        arquivoSaida += dsRow["vendedor"].EmptyIfNull().ToString().Trim() + ";";
                        arquivoSaida += dsRow["id_financeiro"].EmptyIfNull().ToString().Trim() + ";";                                                        // NOSSO NUMERO
                        arquivoSaida += dsRow["status"].EmptyIfNull().ToString().Trim() + ";";
                        arquivoSaida += "'" + dsRow["cpf"].EmptyIfNull().ToString().Trim() + dsRow["cnpj"].EmptyIfNull().ToString().Trim() + ";";            // CPF/CNPJ
                        arquivoSaida += dsRow["id_cliente"].EmptyIfNull().ToString().Trim() + " - " + dsRow["nome"].EmptyIfNull().ToString().Trim() + ";";   // CÓD-CLIENTE-PRODUTO
                        arquivoSaida += totalQtdConsultas.ToString().Trim() + ";";                                                                           // QUANTIDADE
                        arquivoSaida += ";";                                                                                                                 // TABELA-VENDA
                        arquivoSaida += string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", totalValorConsultas) + ";";                              // SUBTOTAL
                        arquivoSaida += string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", valorTotalBruto) + ";";                                  // FATURA
                        // SO FAZ CALCULO SE TIVER PARAMETRIZADO NO CLIENTE COMO S DE SIM
                        if ((nfTipo.EmptyIfNull().ToString().Equals("S")) && (valorNfeCliente > 0))
                        {
                            valorNfeClienteCalculado = ((valorNfeCliente / 100) * valorTotalBruto);
                            arquivoSaida += string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", valorNfeClienteCalculado) + ";";                     // IMPOSTOS COM PARAMETRIZAÇÃO
                        }
                        else
                        {
                            valorNfeClienteCalculado = 0;
                            arquivoSaida += "R$ 0,00" + ";";                                                                                                 // IMPOSTOS SEM PARAMETRIZAÇÃO
                        }

                        totalCustoVendedor += valorEncargos;
                        arquivoSaida += ";";                                                                                                                 // TABELA-VENDEDOR
                        arquivoSaida += string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", totalCustoVendedor) + ";";                               // CUSTO
                        arquivoSaida += string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", valorVCMCliente) + ";";                                  // VCM
                        arquivoSaida += string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", totalCustoVendedor) + ";";                               // RETENÇÃO
                        arquivoSaida += string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", (valorTotalBruto - valorNfeClienteCalculado - totalCustoVendedor)) + ";";   // COMISSAO (FATURA - IMPOSTO - RETENÇÃO)
                        arquivoSaida += ";";                                                                                                                 // OBS
                        arquivoSaida += quebraLinha;
                        if (linhaLancamentos.Trim().Length > 0) { arquivoSaida += linhaLancamentos; }
                        arquivoSaida += quebraLinha;
                        qtdRegistros += 1;
                    }
                    fileNameExportacao = datahora.ToString("yyyy_MM_dd HHmm") + " - " + "GdiPlataform - Relatório - Faturamento por Vendedor" + ".csv";

                    // Salvar o arquivo em disco
                    String DirTempFiles = Server.MapPath("~/_filestemp");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, "reports");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    LibFilesDisk.DeleteFilesInDirectory(DirTempFiles); // Apagar todos os arquivos que estiveremno diretório do usuario
                    fileNameDestino = Path.Combine(DirTempFiles, fileNameExportacao);

                    using (StreamWriter w = new StreamWriter(fileNameDestino, true, Encoding.UTF8))
                    {
                        w.Write(arquivoSaida); // Write the text)
                    }

                    // Inserir o registro do processamento
                    g_processamento record_g_processamento = new g_processamento();
                    record_g_processamento.id_processamento_tipo = 33; // Exportação Comissão Vendedor
                    record_g_processamento.id_usuario = CachePersister.userIdentity.IdUsuario;
                    record_g_processamento.datahora_inicio = datahora;
                    record_g_processamento.datahora_final = LibDateTime.getDataHoraBrasilia();
                    record_g_processamento.qtd_registros = qtdRegistros;
                    record_g_processamento.qtd_reg_ok = qtdRegistros;
                    record_g_processamento.qtd_reg_erro = 0;
                    record_g_processamento.processando = false;
                    record_g_processamento.concluido = true;
                    record_g_processamento.pathfile = fileNameDestino;
                    record_g_processamento.id_coligada = 1;
                    record_g_processamento.id_filial = 1;
                    db.g_processamento.Add(record_g_processamento);
                    db.SaveChanges();

                    sucesso = true;
                    idProcessamentoGravado = record_g_processamento.id_processamento.EmptyIfNull().ToString().Trim();
                }
                else if (allFinanceiro.Count == 0)
                {
                    erroProcessamento = true;
                    msgErroProcessamento = "Não foram localizados títulos que atendam a pesquisa realizada!";
                }
            }
            catch (Exception e)
            {
                erroProcessamento = true;
                msgErroProcessamento = e.Message.ToString();
                if (e.InnerException.Message.ToString() != String.Empty)
                {
                    msgErroProcessamento += "<br/><br/>" + e.InnerException.Message.ToString();
                }
            }

            if (erroProcessamento == false)
            {
                sucesso = true;
                msgRetorno = "Relatório gerado com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>";
                msgRetorno += "Títulos Financeiros: " + qtdRegistros.ToString() + "<br/><br/>";

            }
            else
            {
                sucesso = false;
                msgRetorno = "Erro na Geração do Relatório " + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-solid fa-circle-xmark", "Erro", "red", "") + "<br/><br/>" + msgErroProcessamento;
            }
            return Json(new { success = sucesso, msg = msgRetorno, idProcessamento = idProcessamentoGravado }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region AjaxDeleteFaturamentoCompleto
        [HttpPost]
        public ActionResult AjaxDeleteFaturamentoCompleto(g_financeiro record_g_financeiro)
        {

            bool sucesso = false;
            String msgRetorno = String.Empty;
            int id_faturamento = 0;
            String nomeFaturamento = String.Empty;

            try
            {

                g_financeiro_faturamentos record_g_financeiro_faturamentos = new g_financeiro_faturamentos();
                record_g_financeiro_faturamentos = db.g_financeiro_faturamentos.Find(record_g_financeiro.id_financeiro_faturamento);

                id_faturamento = record_g_financeiro_faturamentos.id_financeiro_faturamento;
                nomeFaturamento = record_g_financeiro_faturamentos.descricao.ToString();


                //BUSCAR NA TABELA G_FINANCEIRO SE EXISTE TITULO QUE NÃO ESTÃO NO STATUS ABERTO
                List<g_financeiro> allTitulosFinanceiroNaoAberto = null;
                allTitulosFinanceiroNaoAberto = db.g_financeiro.Where(d => (d.id_financeiro_faturamento == record_g_financeiro.id_financeiro_faturamento) && (d.id_financeiro_status != 1)).ToList();

                //BUSCAR NA TABELA G_FINANCEIRO SE EXISTE TITULO QUE FORAM GERADOS AVULSO
                List<g_financeiro> allTitulosFinanceiroAvulso = null;
                allTitulosFinanceiroAvulso = db.g_financeiro.Where(d => (d.id_financeiro_faturamento == record_g_financeiro.id_financeiro_faturamento) && (d.geracao_manual == true)).ToList();

                if (allTitulosFinanceiroNaoAberto.Count() > 0)
                {
                    sucesso = false;
                    msgRetorno = "É permitido apagar somente faturamento com boletos no status ABERTO! " + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-down", "", "cc0000", "") + "<br/><br/>" +
                                 " Para o faturamento selecionado EXISTE boletos que não estão no status ABERTO!";
                }
                else if (allTitulosFinanceiroAvulso.Count() > 0)
                {
                    sucesso = false;
                    msgRetorno = "Não foi possivel apagar o faturamento selecionado, existe boletos gerados AVULSO! " + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-down", "", "cc0000", "") + " ";
                }
                else
                {
                    // BUSCAR TABELA G_FINANCEIRO TOTAL DE TITULOS
                    List<g_financeiro> allTitulosFinanceiroAberto = null;
                    allTitulosFinanceiroAberto = db.g_financeiro.Where(d => (d.id_financeiro_faturamento == record_g_financeiro.id_financeiro_faturamento) && (d.id_financeiro_status == 1)).ToList();

                    //APAGAR CONSULTAS TABELA GDC_CONSULTAS_EXTRATO,   É 
                    /*db.gdc_consultas_extrato.RemoveRange(db.gdc_consultas_extrato.Where(c => c.id_financeiro_faturamento == record_g_financeiro.id_financeiro_faturamento).ToList());
                    db.SaveChanges();
                    db.g_financeiro_lancamentos.RemoveRange(db.g_financeiro_lancamentos.Where(l => l.id_financeiro_faturamento == record_g_financeiro.id_financeiro_faturamento).ToList());
                    db.g_financeiro.RemoveRange(db.g_financeiro.Where(t => t.id_financeiro_faturamento == record_g_financeiro.id_financeiro_faturamento).ToList());
                    db.g_financeiro_faturamentos.RemoveRange(db.g_financeiro_faturamentos.Where(f => f.id_financeiro_faturamento == record_g_financeiro.id_financeiro_faturamento).ToList());
                    db.SaveChanges();*/

                    var paramIdFaturamento = new System.Data.SqlClient.SqlParameter("@id_financeiro_faturamento", record_g_financeiro.id_financeiro_faturamento);

                    // APAGAR LANÇAMENTOS — G_FINANCEIRO_LANCAMENTOS
                    db.Database.ExecuteSqlCommand(
                        "DELETE FROM g_financeiro_lancamentos WHERE id_financeiro_faturamento = @id_financeiro_faturamento",
                        new System.Data.SqlClient.SqlParameter("@id_financeiro_faturamento", record_g_financeiro.id_financeiro_faturamento));

                    // APAGAR TÍTULOS — G_FINANCEIRO
                    db.Database.ExecuteSqlCommand(
                        "DELETE FROM g_financeiro WHERE id_financeiro_faturamento = @id_financeiro_faturamento",
                        new System.Data.SqlClient.SqlParameter("@id_financeiro_faturamento", record_g_financeiro.id_financeiro_faturamento));

                    // APAGAR FATURAMENTO — G_FINANCEIRO_FATURAMENTOS
                    db.Database.ExecuteSqlCommand(
                        "DELETE FROM g_financeiro_faturamentos WHERE id_financeiro_faturamento = @id_financeiro_faturamento",
                        new System.Data.SqlClient.SqlParameter("@id_financeiro_faturamento", record_g_financeiro.id_financeiro_faturamento));

                    // SQL PARA TRAZER OS IDs DO PROCESSAMENTO PARA EDIÇÃO
                    DataTable tableRegistro = LibDB.GetDataTable(
                        "SELECT DISTINCT id_processamento FROM gdc_consultas_extrato WHERE id_financeiro_faturamento = @id_financeiro_faturamento",
                        db,
                        new System.Data.SqlClient.SqlParameter("@id_financeiro_faturamento", record_g_financeiro.id_financeiro_faturamento));
                    List<DataRow> rowsTableRegistro = tableRegistro.AsEnumerable().ToList();

                    // LISTA DE PROCESSO PARA EDIÇÃO
                    List<g_processamento> allProcessos = null;
                    allProcessos = db.g_processamento.Where(d => (d.id_processamento > 0)).ToList();


                    if (rowsTableRegistro.Count() > 0)
                    {
                        foreach (var linhaRegistro in rowsTableRegistro)
                        {
                            //EDITAR O PROCESSO PARA DELETADO POREM ESTÃO FATURADOS
                            g_processamento record_g_processamento_edit = allProcessos.Find(d => (d.id_processamento == int.Parse(linhaRegistro["id_processamento"].EmptyIfNull().ToString().Trim())));
                            record_g_processamento_edit.deletado = true;
                            record_g_processamento_edit.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                            record_g_processamento_edit.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                            db.Entry(record_g_processamento_edit).State = EntityState.Modified;

                        }

                    }
                    db.SaveChanges();

                    // APAGAR CONSULTAS — GDC_CONSULTAS_EXTRATO
                    db.Database.ExecuteSqlCommand(
                        "DELETE FROM gdc_consultas_extrato WHERE id_financeiro_faturamento = @id_financeiro_faturamento",
                        new System.Data.SqlClient.SqlParameter("@id_financeiro_faturamento", record_g_financeiro.id_financeiro_faturamento));


                    sucesso = true;
                    msgRetorno = "<br/>" + " Extrato [ " + id_faturamento + " - " + nomeFaturamento + " ], excluído com Sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/>" +
                                        " Quantidade de Título(s) excluído(s): [ " + allTitulosFinanceiroAberto.Count + " ]!";
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

        private JsonResult JsonDataTableException(Exception e, jQueryDataTableParamModel param, string yesFilterOnOff)
        {
            string errorMessage = LibExceptions.getExceptionShortMessage(e);
            return Json(new
            {
                errorMessage = errorMessage,
                severity = "error",
                stackTrace = e.ToString(),
                yesFilterOnOff = yesFilterOnOff ?? "0",
                sEcho = param != null ? param.sEcho : null,
                iTotalRecords = 0,
                iTotalDisplayRecords = 0,
                aaData = new List<string[]>()
            }, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (db != null) { db.Dispose(); };
            }
            base.Dispose(disposing);
        }
    }
}