using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using GdiPlataform.Areas.g.Controllers;
using GdiPlataform.Areas.g.Models;
using GdiPlataform.Areas.gc.Models;
using GdiPlataform.Controllers;
using GdiPlataform.Db;
using GdiPlataform.Lib;
using GdiPlataform.Robos.ENotas;
using GdiPlataform.Security;

namespace GdiPlataform.Areas.gc.Controllers
{
    [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_MovimentosEntradas_*,gc_MovimentosEntradas_Default")]

    public partial class MovimentosEntradasController : Controller
    {
        public string MsgGeral = string.Empty;

        private GdiPlataformEntities db;
        public MovimentosEntradasController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        #region Index
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Notas Fiscais Entradas";
            return View();
        }

        public ActionResult GetDadosMovimentosEntradas(jQueryDataTableParamModel param)
        {
            if (param == null) param = new jQueryDataTableParamModel();
            string filterOnOff = "1"; // aqui sempre existe filtro fixo (fechados + tipo entrada + exclusões)

            try
            {
                int start = param.iDisplayStart;
                int length = (param.iDisplayLength <= 0 ? 20 : param.iDisplayLength);

                // -----------------------------
                // Query base (sem SQL string), já filtrando somente ENTRADAS FECHADAS
                //  - status=2 (fechado)
                //  - tipo movimento "entrada" (t.tipo=1)
                //  - exclui tipos 5 e 9
                // -----------------------------
                var baseQuery =
                    from m in db.gc_movimentos.AsNoTracking()
                    join t in db.gc_movimentos_tipos.AsNoTracking() on m.id_movimento_tipo equals t.id_movimento_tipo
                    where m.id_movimento > 0
                          && m.id_movimento_status == 2
                          && t.tipo == 1
                          && m.id_movimento_tipo != 5
                          && m.id_movimento_tipo != 9
                    select new
                    {
                        // movimento (somente o que você usa abaixo)
                        m.id_movimento,
                        m.id_movimento_tipo,
                        m.id_movimento_status,
                        m.id_cliente,
                        m.id_filial,
                        m.id_estoque_cd,
                        m.id_importacao,
                        m.id_local_estoque,

                        m.datahora_cadastro,
                        m.nf_data_geracao,
                        m.nf_numero,
                        m.qtd_itens,
                        m.valor_total_bruto,
                        m.icms_vicms,

                        m.entrada_nfe_processada,
                        m.receb_estoque_processado,
                        m.movimento_transferido_filial,
                        m.id_movimento_transferencia,

                        m.nf_s3_pdf,
                        m.nf_s3_xml
                    };

                int totalRecords = baseQuery.Count();
                int totalDisplayRecords = totalRecords;

                // Ordenação + paginação (OrderBy ANTES do Skip/Take)
                var page = baseQuery
                    .OrderByDescending(m => m.id_movimento)
                    .Skip(start)
                    .Take(length)
                    .ToList();

                if (page.Count == 0)
                {
                    return Json(new
                    {
                        errorMessage = "",
                        stackTrace = "",
                        yesFilterOnOff = filterOnOff,
                        sEcho = param.sEcho,
                        iTotalRecords = totalRecords,
                        iTotalDisplayRecords = totalDisplayRecords,
                        aaData = new List<string[]>()
                    }, JsonRequestBehavior.AllowGet);
                }

                // -----------------------------
                // Pré-carregamentos (apenas o necessário para a PÁGINA)
                // -----------------------------
                var idsMov = page.Select(x => x.id_movimento).Distinct().ToList();
                var idsClientes = page.Select(x => x.id_cliente).Where(x => x > 0).Distinct().ToList();
                var idsFiliais = page.Select(x => x.id_filial).Where(x => x > 0).Distinct().ToList();
                var idsCds = page.Select(x => x.id_estoque_cd).Where(x => x > 0).Distinct().ToList();
                var idsImp = page.Select(x => x.id_importacao).Where(x => x > 0).Distinct().ToList();

                var clientesDict = db.g_clientes.AsNoTracking()
                    .Where(c => idsClientes.Contains(c.id_cliente))
                    .Select(c => new { c.id_cliente, c.nome })
                    .ToList()
                    .ToDictionary(x => x.id_cliente, x => x.nome);

                var filiaisDict = db.g_filiais.AsNoTracking()
                    .Where(f => idsFiliais.Contains(f.id_filial))
                    .Select(f => new { f.id_filial, f.sigla })
                    .ToList()
                    .ToDictionary(x => x.id_filial, x => x.sigla);

                var cdsDict = db.gc_estoque_cd.AsNoTracking()
                    .Where(c => c.ativo && idsCds.Contains(c.id_estoque_cd))
                    .Select(c => new { c.id_estoque_cd, c.sigla })
                    .ToList()
                    .ToDictionary(x => x.id_estoque_cd, x => x.sigla);

                // NFe status ativos (equivalente ao subselect do seu SQL)
                var idsNfeStatusAtivos = db.g_nfe_status.AsNoTracking()
                    .Where(s => s.nf_ativa)
                    .Select(s => s.id_nfe_status)
                    .Distinct()
                    .ToList();

                // Última NF por movimento (somente para ids da página)
                var lastNfByMov = db.gc_movimentos_nf.AsNoTracking()
                    .Where(nf => idsMov.Contains(nf.id_movimento) && idsNfeStatusAtivos.Contains(nf.id_nfe_status))
                    .Select(nf => new { nf.id_movimento, nf.id_movimento_nf, nf.nf_numero })
                    .ToList()
                    .GroupBy(x => x.id_movimento)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderByDescending(x => x.id_movimento_nf).FirstOrDefault()
                    );

                // Importações (somente das ids da página)
                var importacoesDict = (idsImp.Count == 0)
                    ? new Dictionary<int, string>()
                    : db.gc_comex_importacoes.AsNoTracking()
                        .Where(i => idsImp.Contains(i.id_importacao))
                        .Select(i => new { i.id_importacao, i.numero })
                        .ToList()
                        .ToDictionary(x => x.id_importacao, x => x.numero);

                // -----------------------------
                // Montagem do aaData
                // -----------------------------
                var list = new List<string[]>(page.Count);

                foreach (var m in page)
                {
                    // Regras de "não mostrar"
                    bool showMovimento = true;
                    if ((m.id_movimento_tipo == 6) && (m.entrada_nfe_processada == true)) showMovimento = false; // Importação pendente/processada
                    else if ((m.id_movimento_tipo == 9) && (m.entrada_nfe_processada == true)) showMovimento = false; // Devolução pendente/processada

                    if (!showMovimento) continue;

                    // Status nome/código
                    string statusCodigo;
                    string statusNome;

                    if (m.entrada_nfe_processada == false)
                    {
                        statusCodigo = "0";
                        statusNome = "[ Entrada: Não Processada | Estoque: Não Recebido ]";
                    }
                    else
                    {
                        statusNome = "[ Entrada: Processada | ";
                        if (m.receb_estoque_processado == true)
                        {
                            statusCodigo = "1";
                            statusNome += "Estoque: Recebido ]";
                        }
                        else
                        {
                            statusCodigo = "9";
                            statusNome += "Estoque: Não Recebido ]";
                        }
                    }

                    if (m.movimento_transferido_filial == true && m.id_movimento_transferencia > 0)
                        statusNome += " | Transferido";

                    // Número NF: base + última NF de gc_movimentos_nf
                    string numeroNF = "";
                    if (!string.IsNullOrWhiteSpace(m.nf_numero) && m.nf_numero.Trim() != "0")
                        numeroNF = m.nf_numero.Trim();

                    if (lastNfByMov.TryGetValue(m.id_movimento, out var nfLast) && nfLast != null)
                    {
                        var nfNum = (nfLast.nf_numero ?? "").Trim();
                        if (!string.IsNullOrWhiteSpace(nfNum) && nfNum != "0")
                        {
                            if (numeroNF.Length > 0) numeroNF += " / ";
                            numeroNF += nfNum;
                        }
                    }

                    // Nome cliente
                    string nomeCliente = clientesDict.TryGetValue(m.id_cliente, out var cn) ? (cn ?? "") : "";

                    // Tipo movimento (texto)
                    string tipoMovimento = "";
                    decimal valorTotalNf = m.valor_total_bruto;

                    if (m.id_movimento_tipo == 10)
                    {
                        tipoMovimento = "<br/>NFe Entrada Nacional " + statusNome;
                    }
                    else if (m.id_movimento_tipo == 6 || m.id_movimento_tipo == 7)
                    {
                        // Importação / Internalização
                        string numeroImp = "";
                        if (m.id_importacao > 0 && importacoesDict.TryGetValue(m.id_importacao, out var impNum))
                            numeroImp = (impNum ?? "").Trim();

                        tipoMovimento = "<br/>NFe Importação" + (numeroImp.Length > 0 ? " " + numeroImp : "") + " " + statusNome;

                        if (m.id_movimento_tipo == 7)
                            tipoMovimento = tipoMovimento.Replace("NFe Importação", "NFe Internalização");
                    }
                    else if (m.id_movimento_tipo == 11)
                    {
                        tipoMovimento = "<br/>NFe Devolução " + statusNome;
                    }

                    // Transferência (casos especiais)
                    if (m.id_movimento_tipo == 19)
                    {
                        if (m.id_cliente == 704) tipoMovimento = "<br/>NFe Transferência Filial SP > Matriz BH" + statusNome;
                        else if (m.id_cliente == 3637) tipoMovimento = "<br/>NFe Transferência Matriz BH > Filial SP" + statusNome;
                    }

                    if (!string.IsNullOrWhiteSpace(tipoMovimento))
                        nomeCliente += tipoMovimento;

                    // Regra de valor para internalização
                    if (m.id_movimento_tipo == 7)
                        valorTotalNf += m.icms_vicms;

                    // Data NF (preferir nf_data_geracao)
                    DateTime dataNF = m.nf_data_geracao ?? m.datahora_cadastro;

                    // Siglas
                    string siglaFilial = filiaisDict.TryGetValue(m.id_filial, out var sf) ? (sf ?? "") : "";
                    string siglaCd = cdsDict.TryGetValue(m.id_estoque_cd, out var scd) ? (scd ?? "") : "";

                    list.Add(new[]
                    {
                "", // seleção
                m.id_movimento.ToString(),
                numeroNF,
                nomeCliente,
                siglaFilial,
                siglaCd,
                dataNF.ToString("dd/MM/yy"),
                m.qtd_itens.ToString(),
                string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", valorTotalNf).Replace("R$ ","").Replace("R$","").Replace("$",""),
                statusCodigo,
                m.nf_s3_pdf.EmptyIfNull().ToString(),
                m.nf_s3_xml.EmptyIfNull().ToString(),
                "", // Download PDF
                ""  // Download XML
            });
                }

                return Json(new
                {
                    errorMessage = "",
                    stackTrace = "",
                    yesFilterOnOff = filterOnOff,
                    sEcho = param.sEcho,
                    iTotalRecords = totalRecords,
                    iTotalDisplayRecords = totalDisplayRecords,
                    aaData = list
                }, JsonRequestBehavior.AllowGet);
            }
            catch (DbEntityValidationException e)
            {
                return Json(new
                {
                    errorMessage = LibExceptions.getDbEntityValidationException(e),
                    severity = "error",
                    stackTrace = e.ToString(),
                    yesFilterOnOff = filterOnOff,
                    sEcho = param.sEcho,
                    iTotalRecords = 0,
                    iTotalDisplayRecords = 0,
                    aaData = new List<string[]>()
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new
                {
                    errorMessage = LibExceptions.getExceptionShortMessage(e),
                    severity = "error",
                    stackTrace = e.ToString(),
                    yesFilterOnOff = filterOnOff,
                    sEcho = param.sEcho,
                    iTotalRecords = 0,
                    iTotalDisplayRecords = 0,
                    aaData = new List<string[]>()
                }, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpPost]
        public ActionResult AjaxImportarNFEntrada(CstImportacaoNFEntrada record_cstImportacaoNFEntrada)
        {
            bool Processado = false;
            bool NFeExterior = false;
            bool ErroProcessamento = false;
            int QtdProdutosNaoCadastrados = 0;
            int qtdNomesAtualizados = 0;
            int qtdNCMAtualizados = 0;
            int qtdUnidadeMedidaAtualizados = 0;
            int qtdItensGravados = 0;
            int idProdutoAtual = -1;
            int IdImportacao = 0;
            string NomesProdutosNaoCadastrados = string.Empty;
            string _numeroNF = string.Empty;
            string _serieNF = string.Empty;
            string fileExtXML = string.Empty;
            string fileExtPDF = string.Empty;
            String LogAudit = string.Empty;
            Decimal valorTotalNF = 0;
            int QtdNcmCadastrados = 0;
            String MsgRetorno = String.Empty;
            String resultadoProcessamento = String.Empty;
            String IdMovTipo = "0";
            String IdMovimento = "0";
            String linhaAuxiliar = String.Empty;
            String listaErros = String.Empty;
            String FileNameXmlLocal = String.Empty;
            String FileNameXmlUpload = String.Empty;
            String FileNameXmlUploadTemp = String.Empty;
            String FileNameUploadGed = String.Empty;
            String InformacoesAdicionaisNF = string.Empty;
            String ClienteNome = string.Empty;
            String clienteCPFCNPJ = string.Empty;
            String LineFileXML = string.Empty;
            String NFeReferenciada_ChaveAcesso = string.Empty;
            String NFeReferenciada_Serie = string.Empty;
            String NFeReferenciada_Numero = string.Empty;
            String NfeCFOP = string.Empty;
            String CnpjFilialDestino = string.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            CstNfeEmitente record_NfeEmitente = new CstNfeEmitente();
            CstNfeIcmsTotal record_NfeIcmsTotal = new CstNfeIcmsTotal();
            CstNfeAutorizacao record_NfeAutorizacao = new CstNfeAutorizacao();
            gc_movimentos_nf record_gc_movimentos_nf_referencia = null;
            g_filiais RecordFilialDestino = null;
            XmlDocument xmlDocument = new XmlDocument();

            // Apagar itens temporários
            try
            {
                String SqlDeleteMovimentosItensTemp = "delete from gc_movimentos_itens where id_movimento_item > 0 and id_movimento in (select distinct id_movimento from gc_movimentos where (id_movimento_tipo = 5 or id_movimento_tipo = 9))";
                LibDB.dbQueryExec(SqlDeleteMovimentosItensTemp, db);

                String SqlDeleteMovimentosTemp = "delete from gc_movimentos where id_movimento > 0 and (id_movimento_tipo = 5 or id_movimento_tipo = 9)";
                LibDB.dbQueryExec(SqlDeleteMovimentosTemp, db);
            }
            catch (Exception) { };


            if (record_cstImportacaoNFEntrada.filesourceXML != null)
            {
                if (record_cstImportacaoNFEntrada.filesourceXML.FileName.EmptyIfNull().ToString().Length > 0) { fileExtXML = System.IO.Path.GetExtension(record_cstImportacaoNFEntrada.filesourceXML.FileName).Substring(1).ToLower(); };
            }
            if (record_cstImportacaoNFEntrada.filesourcePDF != null)
            {
                if (record_cstImportacaoNFEntrada.filesourcePDF.FileName.EmptyIfNull().ToString().Length > 0) { fileExtPDF = System.IO.Path.GetExtension(record_cstImportacaoNFEntrada.filesourcePDF.FileName).Substring(1).ToLower(); };
            }

            // Validações XML
            if (fileExtXML.Length == 0)
            {
                MsgRetorno += "O Arquivo XML não foi informado!" + "<br/>";
                ErroProcessamento = true;
            }
            else
            {
                if (fileExtXML != "xml")
                {
                    MsgRetorno += "O Layout do arquivo XML informado não foi identificado pelo ERP" + "<br/>";
                    ErroProcessamento = true;
                }
                if (record_cstImportacaoNFEntrada.filesourceXML.ContentLength > 5000000)
                {
                    MsgRetorno += "O Tamanho do arquivo XML não pode exceder 5 Mb!" + "<br/>";
                    ErroProcessamento = true;
                }
            }

            // Validações Danfe
            if (fileExtPDF.Length == 0)
            {
                if (record_cstImportacaoNFEntrada.id_movimento_tipo != 6) // Se for diferente de Compra - Fornecedor - Exterior  é obrigatório colocar a Danfe
                {
                    MsgRetorno += "O Arquivo PDF não foi informado!" + "<br/>";
                    ErroProcessamento = true;
                }
            }
            else
            {
                if (fileExtPDF != "pdf")
                {
                    MsgRetorno += "O Tipo do arquivo PDF informado não foi identificado pelo ERP" + "<br/>";
                    ErroProcessamento = true;
                }
                if (record_cstImportacaoNFEntrada.filesourceXML.ContentLength > 5000000)
                {
                    MsgRetorno += "O Tamanho do arquivo PDF não pode exceder 5 Mb!" + "<br/>";
                    ErroProcessamento = true;
                }
            }

            if ((ErroProcessamento == false) && (record_cstImportacaoNFEntrada.filesourceXML.ContentLength > 0))
            {
                try
                {
                    // Upload do Arquivo
                    FileNameXmlLocal = Path.GetFileName(record_cstImportacaoNFEntrada.filesourceXML.FileName);
                    Processado = false;
                    var dirUpload = Server.MapPath("~/_filestemp");
                    dirUpload = Path.Combine(dirUpload, "uploads");
                    if (!Directory.Exists(dirUpload)) { Directory.CreateDirectory(dirUpload); }
                    dirUpload = Path.Combine(dirUpload, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
                    if (!Directory.Exists(dirUpload)) { Directory.CreateDirectory(dirUpload); }
                    LibFilesDisk.DeleteFilesInDirectory(dirUpload); // Apagar todos os arquivos que estiveremno diretório do usuario
                    FileNameXmlUpload = Path.Combine(dirUpload, LibDateTime.getDataHoraBrasilia().ToString("yyyyMMdd") + "_temp_(2)_" + FileNameXmlLocal);
                    FileNameXmlUploadTemp = Path.Combine(dirUpload, LibDateTime.getDataHoraBrasilia().ToString("yyyyMMdd") + "_temp_(1)_" + FileNameXmlLocal);
                    LibCache.LiberarMemoria();
                    try { System.IO.File.Delete(FileNameXmlUpload); } catch { };
                    try { System.IO.File.Delete(FileNameXmlUploadTemp); } catch { };
                    record_cstImportacaoNFEntrada.filesourceXML.SaveAs(FileNameXmlUploadTemp);

                    // Preparar o arquivo XML para ser importado
                    String ArquivoSaidaXML = String.Empty;
                    System.IO.StreamReader FileSourceTemp = new System.IO.StreamReader(FileNameXmlUploadTemp);
                    while ((LineFileXML = FileSourceTemp.ReadLine()) != null) { ArquivoSaidaXML += LibStringFormat.RemoverCaracteresEspeciaisCodificados(HttpUtility.HtmlDecode(LibStringFormat.FormatarXML(LineFileXML))); };
                    using (StreamWriter w = new StreamWriter(FileNameXmlUpload, true, Encoding.UTF8)) { w.Write(ArquivoSaidaXML); w.Flush(); w.Close(); w.Dispose(); }
                    FileSourceTemp.Close();
                    LibCache.LiberarMemoria();
                    try { System.IO.File.Delete(FileNameXmlUploadTemp); } catch { };

                    // Processamento do Arquivo
                    g_clientes record_g_cliente = null;
                    gc_movimentos RecordMovimentoNovo = new Db.gc_movimentos();
                    List<g_produtos> ListaProdutosGDI = db.g_produtos.Where(p => p.ativo == true).ToList();
                    List<gc_movimentos_itens> listaItens = new List<gc_movimentos_itens>();
                    List<g_produtos_ncm> allProdutosNCM = db.g_produtos_ncm.ToList();
                    List<g_unidade_medida> allUnidadesMedidas = db.g_unidade_medida.ToList();

                    if (fileExtXML == "xml")
                    {
                        xmlDocument.Load(FileNameXmlUpload);
                        XmlNodeList NodeInfAutorizacao = null;
                        XmlNodeList NodeNFe = xmlDocument.GetElementsByTagName("NFe");
                        XmlNodeList NodeInf = ((XmlElement)NodeNFe[0]).GetElementsByTagName("infNFe");
                        XmlNodeList NodeCabecalho = ((XmlElement)NodeInf[0]).GetElementsByTagName("ide");
                        XmlNodeList NodeNfReferenciada = null;
                        XmlNodeList NodeEmitente = ((XmlElement)NodeInf[0]).GetElementsByTagName("emit");
                        XmlNodeList NodeDest = ((XmlElement)NodeInf[0]).GetElementsByTagName("dest");
                        XmlNodeList NodeProdutos = ((XmlElement)NodeInf[0]).GetElementsByTagName("det");
                        XmlNodeList NodeTotal = ((XmlElement)NodeInf[0]).GetElementsByTagName("total");
                        XmlNodeList NodeTransp = ((XmlElement)NodeInf[0]).GetElementsByTagName("transp");
                        XmlNodeList NodeInfAdic = ((XmlElement)NodeInf[0]).GetElementsByTagName("infAdic");
                        XmlNodeList NodeProtNFe = xmlDocument.GetElementsByTagName("protNFe");
                        if ((NodeProtNFe != null) && (NodeProtNFe.Count > 0)) { try { NodeInfAutorizacao = ((XmlElement)NodeProtNFe[0]).GetElementsByTagName("infProt"); } catch { }; };

                        record_g_cliente = null;
                        #region Nota Fiscal Referenciada
                        if (record_cstImportacaoNFEntrada.id_movimento_tipo == 9) // 9 - Devolução
                        {
                            // Validação da nota fiscal referenciada
                            try { NodeNfReferenciada = ((XmlElement)NodeCabecalho[0]).GetElementsByTagName("NFref"); } catch { };
                            if ((NodeNfReferenciada == null) || (NodeNfReferenciada.Count == 0))
                            {
                                ErroProcessamento = true;
                                MsgRetorno += " - Não foi localizado no XML a informação da Nota Fiscal Referênciada!" + "<br/>";
                            }
                            else
                            {
                                foreach (XmlElement noNfeReferenciada in NodeNfReferenciada[0].ChildNodes)
                                {
                                    if (noNfeReferenciada.Name == "refNFe") // Informação da chave de acesso
                                    {
                                        NFeReferenciada_ChaveAcesso = noNfeReferenciada.FirstChild.Value.ToString();
                                    }
                                    else if (noNfeReferenciada.Name == "refNF") // Informação do número e série da NF Referenciada
                                    {
                                        XmlNodeList nodeListTemp1 = ((XmlElement)NodeNfReferenciada[0]).GetElementsByTagName("refNF");
                                        foreach (XmlElement nodeTemp1 in nodeListTemp1[0].ChildNodes)
                                        {
                                            if (nodeTemp1.Name == "serie") { NFeReferenciada_Serie = nodeTemp1.FirstChild.Value.ToString(); }
                                            else if (nodeTemp1.Name == "nNF") { NFeReferenciada_Numero = nodeTemp1.FirstChild.Value.ToString(); }
                                        }
                                    }

                                    // Localizar a NfeReferenciada no banco de dados
                                    if (NFeReferenciada_ChaveAcesso.EmptyIfNull().ToString().Length > 0)
                                    {
                                        record_gc_movimentos_nf_referencia = db.gc_movimentos_nf.Where(m => m.nf_chave_acesso == NFeReferenciada_ChaveAcesso).FirstOrDefault();
                                        if (record_gc_movimentos_nf_referencia == null)
                                        {
                                            ErroProcessamento = true;
                                            MsgRetorno += " - Não foi localizado no ERP a chave de acesso referenciada [" + NFeReferenciada_ChaveAcesso + "]" + "<br/>";
                                        }

                                    }
                                    else if ((NFeReferenciada_Serie.EmptyIfNull().ToString().Length > 0) && (NFeReferenciada_Numero.EmptyIfNull().ToString().Length > 0))
                                    {
                                        record_gc_movimentos_nf_referencia = db.gc_movimentos_nf.Where(m => m.nf_serie == NFeReferenciada_Serie && m.nf_numero == NFeReferenciada_Numero).FirstOrDefault();
                                        if (record_gc_movimentos_nf_referencia == null)
                                        {
                                            ErroProcessamento = true;
                                            MsgRetorno += " - Não foi localizado no ERP a Nota Fiscal [Série: " + NFeReferenciada_Serie.EmptyIfNull().ToString() + " Número: " + NFeReferenciada_Serie.EmptyIfNull().ToString() + "]" + "<br/>";
                                        }
                                    }
                                    else
                                    {
                                        ErroProcessamento = true;
                                        MsgRetorno += " - Não foi localizado no XML a informação da Nota Fiscal Referênciada!" + "<br/>";
                                    }
                                }

                            }
                        }
                        #endregion

                        #region Destinatário e Emitente

                        // Verificar os dados do destinatário GDI para NF nacionais, ou um Cli/For para NF Exterior
                        foreach (XmlElement noDestinatario in NodeDest[0].ChildNodes)
                        {
                            if (noDestinatario.Name == "idEstrangeiro")
                            {
                                NFeExterior = true;
                            }

                            if (NFeExterior == true) // Emitente deverá ser um Cliente/Fornecedor
                            {
                                if ((noDestinatario.Name == "CNPJ") && (record_g_cliente == null))
                                {
                                    clienteCPFCNPJ = noDestinatario.FirstChild.Value.ToString();
                                    record_g_cliente = db.g_clientes.Where(c => c.cnpj == clienteCPFCNPJ).FirstOrDefault();
                                    if (record_g_cliente != null) { ClienteNome = record_g_cliente.nome.EmptyIfNull().ToString(); };
                                }
                                if ((noDestinatario.Name == "xNome") && (record_g_cliente == null))
                                {
                                    ClienteNome = noDestinatario.FirstChild.Value.ToString();
                                    record_g_cliente = db.g_clientes.Where(c => c.nome == ClienteNome).FirstOrDefault();
                                }
                            }
                            else if (NFeExterior == false) // Emitente deverá ser um Cliente/Fornecedor
                            {
                                if ((noDestinatario.Name == "CNPJ") && (RecordFilialDestino == null))
                                {
                                    CnpjFilialDestino = noDestinatario.FirstChild.Value.ToString();
                                    RecordFilialDestino = db.g_filiais.Where(f => f.cnpj == CnpjFilialDestino).FirstOrDefault();
                                }
                            }
                        }


                        // Verificar os dados do EMITENTE GDI para NF Exterior, ou um Cli/For para NF Brasil
                        foreach (XmlElement noEmitente in NodeEmitente[0].ChildNodes)
                        {
                            if (NFeExterior == true) // Emitente deverá ser um Cliente/Fornecedor
                            {
                                if (noEmitente.Name == "CNPJ")
                                {
                                    CnpjFilialDestino = noEmitente.FirstChild.Value.ToString();
                                    RecordFilialDestino = db.g_filiais.Where(f => f.cnpj == CnpjFilialDestino).FirstOrDefault();
                                }
                            }
                            if (NFeExterior == false) // Emitente deverá ser um Cliente/Fornecedor
                            {
                                if ((noEmitente.Name == "CNPJ") && (record_g_cliente == null))
                                {
                                    clienteCPFCNPJ = noEmitente.FirstChild.Value.ToString();
                                    record_g_cliente = db.g_clientes.Where(c => c.cnpj == clienteCPFCNPJ).FirstOrDefault();
                                    if (record_g_cliente != null) { ClienteNome = record_g_cliente.nome.EmptyIfNull().ToString(); };
                                }
                                if ((noEmitente.Name == "xNome") && (record_g_cliente == null))
                                {
                                    ClienteNome = noEmitente.FirstChild.Value.ToString();
                                    record_g_cliente = db.g_clientes.Where(c => c.nome == ClienteNome).FirstOrDefault();
                                }
                            }
                        }


                        // Validar o Cliente
                        if (record_g_cliente == null)
                        {
                            ErroProcessamento = true;
                            if (ClienteNome.EmptyIfNull().ToString().Length > 0) { MsgRetorno += " - Cliente (" + ClienteNome + ") Não localizado na base de dados!" + "<br/>"; }
                            else if (clienteCPFCNPJ.EmptyIfNull().ToString().Length > 0) { MsgRetorno += " - Cliente CNPJ (" + clienteCPFCNPJ + ") Não localizado na base de dados!" + "<br/>"; }
                        }

                        // Validar o Destinatário da NF
                        if (RecordFilialDestino == null)
                        {
                            ErroProcessamento = true;
                            if (CnpjFilialDestino.EmptyIfNull().ToString().Trim().Length == 0) { MsgRetorno += " - Não foi localizado o CNPJ do Destinatário no XML!" + "<br/>"; }
                            else { MsgRetorno += " - O CNPJ informado como destinatário dessa Nota não pertençe a GDI Aviação!" + "<br/>"; };
                        }
                        else
                        {
                            RecordMovimentoNovo.id_coligada = 1; // Grupo GDI
                            RecordMovimentoNovo.id_filial = RecordFilialDestino.id_filial;
                            if (RecordMovimentoNovo.id_filial == 1)
                            {
                                RecordMovimentoNovo.id_local_estoque = 1;
                                RecordMovimentoNovo.id_estoque_cd = 1;
                            }
                            else if (RecordMovimentoNovo.id_filial == 2)
                            {
                                RecordMovimentoNovo.id_local_estoque = 3;
                                RecordMovimentoNovo.id_estoque_cd = 3;
                            }
                        }


                        foreach (XmlElement noEmitente in NodeEmitente[0].ChildNodes)
                        {
                            if (noEmitente.Name == "CNPJ") { record_NfeEmitente.CNPJ = noEmitente.FirstChild.Value.ToString(); }
                            else if (noEmitente.Name == "xNome") { record_NfeEmitente.xNome = noEmitente.FirstChild.Value.ToString(); }
                            else if (noEmitente.Name == "xFant") { record_NfeEmitente.xFant = noEmitente.FirstChild.Value.ToString(); }
                            else if (noEmitente.Name == "IE") { record_NfeEmitente.IE = noEmitente.FirstChild.Value.ToString(); }
                            else if (noEmitente.Name == "enderEmit")
                            {
                                foreach (XmlElement noEnderecoEmitente in noEmitente.ChildNodes)
                                {
                                    if (noEnderecoEmitente.Name == "xLgr") { record_NfeEmitente.xLgr = noEnderecoEmitente.FirstChild.Value.ToString(); }
                                    else if (noEnderecoEmitente.Name == "nro") { record_NfeEmitente.nro = noEnderecoEmitente.FirstChild.Value.ToString(); }
                                    else if (noEnderecoEmitente.Name == "Bairro") { record_NfeEmitente.xBairro = noEnderecoEmitente.FirstChild.Value.ToString(); }
                                    else if (noEnderecoEmitente.Name == "Mun") { record_NfeEmitente.xMun = noEnderecoEmitente.FirstChild.Value.ToString(); }
                                    else if (noEnderecoEmitente.Name == "UF") { record_NfeEmitente.UF = noEnderecoEmitente.FirstChild.Value.ToString(); }
                                    else if (noEnderecoEmitente.Name == "CEP") { record_NfeEmitente.CEP = noEnderecoEmitente.FirstChild.Value.ToString(); }
                                    else if (noEnderecoEmitente.Name == "fone") { record_NfeEmitente.fone = noEnderecoEmitente.FirstChild.Value.ToString(); };
                                }
                            }
                        }
                        #endregion

                        #region Cabeçalho
                        foreach (XmlElement noCabecalho in NodeCabecalho[0].ChildNodes)
                        {
                            if (noCabecalho.Name == "serie") { RecordMovimentoNovo.nf_serie = noCabecalho.FirstChild.Value.ToString(); }
                            else if (noCabecalho.Name == "nNF") { RecordMovimentoNovo.nf_numero = noCabecalho.FirstChild.Value.ToString(); }
                            else if (noCabecalho.Name == "dhEmi")
                            {
                                RecordMovimentoNovo.nf_data_geracao = DateTime.Parse(noCabecalho.FirstChild.Value.ToString().Substring(0, 10));
                                RecordMovimentoNovo.nf_data_recebimento = DateTime.Parse(noCabecalho.FirstChild.Value.ToString().Substring(0, 10));
                                RecordMovimentoNovo.datahora_cadastro = DateTime.Parse(noCabecalho.FirstChild.Value.ToString().Substring(0, 10));
                            }
                            _numeroNF = RecordMovimentoNovo.nf_numero;
                            _serieNF = RecordMovimentoNovo.nf_serie;
                        }
                        #endregion

                        #region Itens/Produtos
                        for (int i = 0; i < NodeProdutos.Count; i++)
                        {
                            gc_movimentos_itens record_gc_movimentos_itens = new Db.gc_movimentos_itens();
                            record_gc_movimentos_itens.sequencia = int.Parse(NodeProdutos[i].Attributes["nItem"].Value);
                            record_gc_movimentos_itens.afrmm_valor = 0;
                            record_gc_movimentos_itens.di_imposto_bc = 0;
                            record_gc_movimentos_itens.di_desp_aduaneiras = 0;
                            record_gc_movimentos_itens.di_imposto_importacao = 0;

                            String codigoProduto = string.Empty;
                            XmlNodeList noProdutoDados = null;
                            XmlNodeList noProdutoImposto = null;
                            String _ProdutoCodigo = string.Empty;
                            String _ProdutoCodigoExterno = string.Empty;
                            String _ProdutoCodigoReduzido = string.Empty;
                            String _ProdutoDescricao = string.Empty;
                            String _ProdutoDescricaoExterna = string.Empty;
                            String _ProdutoNcm = string.Empty;
                            String _ProdutoCfop = string.Empty;
                            String _ProdutoUnidadeMedida = string.Empty;
                            Decimal _ProdutoQtdItem = 0;
                            Decimal _ProdutoValorUnitItem = 0;
                            Decimal _ProdutoValorTotalItem = 0;
                            Decimal _NotaEntradaValorDespesas = 0;
                            valorTotalNF += _ProdutoValorTotalItem;
                            record_gc_movimentos_itens.id_movimento_item = 999;

                            foreach (XmlElement noProdutoAtual in NodeProdutos[i].ChildNodes)
                            {
                                if (noProdutoAtual.Name == "prod")
                                {
                                    _ProdutoCodigo = string.Empty;
                                    _ProdutoCodigoExterno = string.Empty;
                                    _ProdutoCodigoReduzido = string.Empty;
                                    _ProdutoDescricao = string.Empty;
                                    _ProdutoDescricaoExterna = string.Empty;
                                    _ProdutoNcm = string.Empty;
                                    _ProdutoCfop = string.Empty;
                                    _ProdutoUnidadeMedida = string.Empty;
                                    _ProdutoQtdItem = 0;
                                    _ProdutoValorUnitItem = 0;
                                    _ProdutoValorTotalItem = 0;

                                    noProdutoDados = noProdutoAtual.ChildNodes;
                                    foreach (XmlElement noProdutoDadosDet in noProdutoDados)
                                    {
                                        if (noProdutoDadosDet.Name == "cProd")
                                        {
                                            if (!LibXML.HasFirstChildValue(noProdutoDadosDet)) { throw new Exception(" Tag [prod.cProd] inválida!"); }
                                            _ProdutoCodigo = noProdutoDadosDet.FirstChild.Value.EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                            _ProdutoCodigo = LibStringFormat.RemoverEspacos(_ProdutoCodigo);
                                            _ProdutoCodigoExterno = _ProdutoCodigo;
                                        }
                                        if (noProdutoDadosDet.Name == "xProd")
                                        {
                                            if (!LibXML.HasFirstChildValue(noProdutoDadosDet)) { throw new Exception(" Tag [prod.xProd] inválida!"); };
                                            _ProdutoDescricao += noProdutoDadosDet.FirstChild.Value.ToString();
                                            _ProdutoDescricaoExterna = noProdutoDadosDet.FirstChild.Value.ToString();
                                            if ((NFeExterior == false) && (_ProdutoCodigo.EmptyIfNull().ToString().Length > 0))
                                            {
                                                if (_ProdutoDescricao.IndexOf(_ProdutoCodigo) == -1) { _ProdutoDescricao = _ProdutoCodigo.Trim() + " " + _ProdutoDescricao.Trim(); }
                                            }
                                            _ProdutoDescricao = LibStringFormat.GDIFormatarDescricaoProdutoTraduzidoComPN(_ProdutoDescricao);
                                            if (_ProdutoCodigo.EmptyIfNull().ToString().Length == 0) { _ProdutoCodigo = LibStringFormat.ClienteGDIGetPartNumber(_ProdutoDescricao); };
                                            //if (_ProdutoCodigo.Length > 20) { _ProdutoCodigo = _ProdutoCodigo.Substring(0, 20); };  // Limitação de tamanho
                                            _ProdutoCodigo = _ProdutoCodigo.EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                            _ProdutoCodigo = LibStringFormat.RemoverEspacos(_ProdutoCodigo);
                                            _ProdutoCodigoReduzido = _ProdutoCodigo;
                                            _ProdutoCodigoExterno = _ProdutoCodigo;
                                        }
                                        else if (noProdutoDadosDet.Name == "NCM")
                                        {
                                            if (!LibXML.HasFirstChildValue(noProdutoDadosDet)) { throw new Exception(" Tag [prod.NCM] inválida!"); };
                                            _ProdutoNcm = noProdutoDadosDet.FirstChild.Value.ToString();
                                        }
                                        else if (noProdutoDadosDet.Name == "CFOP")
                                        {
                                            if (!LibXML.HasFirstChildValue(noProdutoDadosDet)) { throw new Exception(" Tag [prod.CFOP] inválida!"); };
                                            _ProdutoCfop = noProdutoDadosDet.FirstChild.Value.ToString();
                                            if (NfeCFOP.IndexOf(_ProdutoCfop) == -1) { NfeCFOP += _ProdutoCfop + ";"; };
                                        }
                                        else if (noProdutoDadosDet.Name == "uCom")
                                        {
                                            if (!LibXML.HasFirstChildValue(noProdutoDadosDet)) { throw new Exception(" Tag [prod.uCom] inválida!"); };
                                            _ProdutoUnidadeMedida = noProdutoDadosDet.FirstChild.Value.ToString();
                                        }
                                        else if (noProdutoDadosDet.Name == "qCom")
                                        {
                                            if (!LibXML.HasFirstChildValue(noProdutoDadosDet)) { throw new Exception(" Tag [prod.qCom] inválida!"); };
                                            _ProdutoQtdItem = LibNumbers.ConvertDecimal(noProdutoDadosDet.FirstChild.Value.ToString());
                                        }
                                        else if (noProdutoDadosDet.Name == "vUnCom")
                                        {
                                            if (!LibXML.HasFirstChildValue(noProdutoDadosDet)) { throw new Exception(" Tag [prod.vUnCom] inválida!"); };
                                            _ProdutoValorUnitItem = LibNumbers.ConvertDecimal(noProdutoDadosDet.FirstChild.Value.ToString());
                                        }
                                        else if (noProdutoDadosDet.Name == "vProd")
                                        {
                                            if (!LibXML.HasFirstChildValue(noProdutoDadosDet)) { throw new Exception(" Tag [prod.vProd] inválida!"); };
                                            _ProdutoValorTotalItem = LibNumbers.ConvertDecimal(noProdutoDadosDet.FirstChild.Value.ToString());
                                        }
                                        else if (noProdutoDadosDet.Name == "vOutro")
                                        {
                                            if (!LibXML.HasFirstChildValue(noProdutoDadosDet)) { throw new Exception(" Tag [prod.vOutro] inválida!"); };
                                            _NotaEntradaValorDespesas = LibNumbers.ConvertDecimal(noProdutoDadosDet.FirstChild.Value.ToString());
                                        }
                                        else if (noProdutoDadosDet.Name == "DI")
                                        {
                                            XmlNodeList noProdutoDadosDI = noProdutoDadosDet.ChildNodes;
                                            foreach (XmlElement noProdutoDadosDIDet in noProdutoDadosDI)
                                            {
                                                if (noProdutoDadosDIDet.Name == "nDI")
                                                {
                                                    if (!LibXML.HasFirstChildValue(noProdutoDadosDIDet)) { throw new Exception(" Tag [prod.DI.nDI] inválida!"); };
                                                    record_gc_movimentos_itens.di_numero = noProdutoDadosDIDet.FirstChild.Value.ToString();

                                                    String numeroDocumento = record_gc_movimentos_itens.di_numero;
                                                    if (RecordMovimentoNovo.documento_numero.EmptyIfNull().ToString().Length == 0)
                                                    {
                                                        RecordMovimentoNovo.documento_numero = numeroDocumento;
                                                    }
                                                    else if ((RecordMovimentoNovo.documento_numero.EmptyIfNull().ToString() == numeroDocumento) || (RecordMovimentoNovo.documento_numero.EmptyIfNull().ToString().IndexOf(numeroDocumento) >= 0))
                                                    {
                                                        // Não Faz nada;
                                                    }
                                                    else
                                                    {
                                                        RecordMovimentoNovo.documento_numero += " | " + numeroDocumento;
                                                    }
                                                }
                                                else if (noProdutoDadosDIDet.Name == "dDI")
                                                {
                                                    if (!LibXML.HasFirstChildValue(noProdutoDadosDIDet)) { throw new Exception(" Tag [prod.DI.dDI] inválida!"); };
                                                    record_gc_movimentos_itens.di_data = DateTime.Parse(noProdutoDadosDIDet.FirstChild.Value.ToString());
                                                }
                                                else if (noProdutoDadosDIDet.Name == "xLocDesemb")
                                                {
                                                    if (!LibXML.HasFirstChildValue(noProdutoDadosDIDet)) { throw new Exception(" Tag [prod.DI.xLocDesemb] inválida!"); };
                                                    record_gc_movimentos_itens.di_loc_desemb = noProdutoDadosDIDet.FirstChild.Value.ToString();
                                                }
                                                else if (noProdutoDadosDIDet.Name == "UFDesemb")
                                                {
                                                    if (!LibXML.HasFirstChildValue(noProdutoDadosDIDet)) { throw new Exception(" Tag [prod.DI.UFDesemb] inválida!"); };
                                                    record_gc_movimentos_itens.di_uf_desemb = noProdutoDadosDIDet.FirstChild.Value.ToString();
                                                }
                                                else if (noProdutoDadosDIDet.Name == "dDesemb")
                                                {
                                                    if (!LibXML.HasFirstChildValue(noProdutoDadosDIDet)) { throw new Exception(" Tag [prod.DI.dDesemb] inválida!"); };
                                                    record_gc_movimentos_itens.di_data_desemb = DateTime.Parse(noProdutoDadosDIDet.FirstChild.Value.EmptyIfNull().ToString());
                                                }
                                                else if (noProdutoDadosDIDet.Name == "tpViaTransp")
                                                {
                                                    if (!LibXML.HasFirstChildValue(noProdutoDadosDIDet)) { throw new Exception(" Tag [prod.DI.tpViaTransp] inválida!"); };
                                                    record_gc_movimentos_itens.di_via_transp = LibNumbers.ConvertInt(noProdutoDadosDIDet.FirstChild.Value.ToString());
                                                }
                                                else if (noProdutoDadosDIDet.Name == "tpIntermedio")
                                                {
                                                    if (!LibXML.HasFirstChildValue(noProdutoDadosDIDet)) { throw new Exception(" Tag [prod.DI.tpIntermedio] inválida!"); };
                                                    record_gc_movimentos_itens.di_tipo_itermedio = LibNumbers.ConvertInt(noProdutoDadosDIDet.FirstChild.Value.ToString());
                                                }
                                                else if (noProdutoDadosDIDet.Name == "CNPJ")
                                                {
                                                    if (!LibXML.HasFirstChildValue(noProdutoDadosDIDet)) { throw new Exception(" Tag [prod.DI.CNPJ] inválida!"); };
                                                    record_gc_movimentos_itens.di_cnpj = noProdutoDadosDIDet.FirstChild.Value.ToString();
                                                }
                                                else if (noProdutoDadosDIDet.Name == "UFTerceiro")
                                                {
                                                    if (!LibXML.HasFirstChildValue(noProdutoDadosDIDet)) { throw new Exception(" Tag [prod.DI.UFTerceiro] inválida!"); };
                                                    record_gc_movimentos_itens.di_uf_terceiro = noProdutoDadosDIDet.FirstChild.Value.ToString();
                                                }
                                                else if (noProdutoDadosDIDet.Name == "cExportador")
                                                {
                                                    if (!LibXML.HasFirstChildValue(noProdutoDadosDIDet)) { throw new Exception(" Tag [prod.DI.cExportador] inválida!"); };
                                                    record_gc_movimentos_itens.di_cod_exportador = noProdutoDadosDIDet.FirstChild.Value.ToString();
                                                }
                                                else if (noProdutoDadosDIDet.Name == "adi")
                                                {
                                                    XmlNodeList noProdutoDadosDiAdicao = noProdutoDadosDIDet.ChildNodes;
                                                    foreach (XmlElement noProdutoDadosDiAdicaoDet in noProdutoDadosDiAdicao)
                                                    {
                                                        if (noProdutoDadosDiAdicaoDet.Name == "nAdicao")
                                                        {
                                                            if (!LibXML.HasFirstChildValue(noProdutoDadosDiAdicaoDet)) { throw new Exception(" Tag [prod.DI.adi.nAdicao] inválida!"); };
                                                            record_gc_movimentos_itens.di_adicao_numero = LibNumbers.ConvertInt(noProdutoDadosDiAdicaoDet.FirstChild.Value.ToString());
                                                        }
                                                        else if (noProdutoDadosDiAdicaoDet.Name == "nSeqAdic")
                                                        {
                                                            if (!LibXML.HasFirstChildValue(noProdutoDadosDiAdicaoDet)) { throw new Exception(" Tag [prod.DI.adi.nSeqAdic] inválida!"); };
                                                            record_gc_movimentos_itens.di_adicao_sequencial = LibNumbers.ConvertInt(noProdutoDadosDiAdicaoDet.FirstChild.Value.ToString());
                                                        }
                                                        else if (noProdutoDadosDiAdicaoDet.Name == "cFabricante")
                                                        {
                                                            if (!LibXML.HasFirstChildValue(noProdutoDadosDiAdicaoDet)) { throw new Exception(" Tag [prod.DI.adi.cFabricante] inválida!"); };
                                                            record_gc_movimentos_itens.di_adicao_fabricante = noProdutoDadosDiAdicaoDet.FirstChild.Value.ToString();
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                else if (noProdutoAtual.Name == "imposto")
                                {
                                    noProdutoImposto = noProdutoAtual.ChildNodes;
                                    foreach (XmlElement noProdutoImpostoDet in noProdutoImposto)
                                    {
                                        if (noProdutoImpostoDet.Name == "ICMS")
                                        {
                                            XmlNodeList noProdutoImpostoIcms = noProdutoImpostoDet.ChildNodes;
                                            foreach (XmlElement noProdutoImpostoIcmsDet in noProdutoImpostoIcms)
                                            {
                                                if (noProdutoImpostoIcmsDet.Name == "ICMS00")
                                                {
                                                    XmlNodeList noProdutoImpostoIcms00 = noProdutoImpostoIcmsDet.ChildNodes;
                                                    foreach (XmlElement noProdutoImpostoIcms00Det in noProdutoImpostoIcms00)
                                                    {
                                                        if (noProdutoImpostoIcms00Det.Name == "orig")
                                                        {
                                                            if (!LibXML.HasFirstChildValue(noProdutoImpostoIcms00Det)) { throw new Exception(" Tag [imposto.ICMS.ICMS00.orig] inválida!"); };
                                                            record_gc_movimentos_itens.icms_orig = LibNumbers.ConvertInt(noProdutoImpostoIcms00Det.FirstChild.Value.ToString());
                                                        }
                                                        else if (noProdutoImpostoIcms00Det.Name == "CST")
                                                        {
                                                            if (!LibXML.HasFirstChildValue(noProdutoImpostoIcms00Det)) { throw new Exception(" Tag [imposto.ICMS.ICMS00.CST] inválida!"); };
                                                            record_gc_movimentos_itens.icms_cst = noProdutoImpostoIcms00Det.FirstChild.Value.ToString();
                                                        }
                                                        else if (noProdutoImpostoIcms00Det.Name == "modBC")
                                                        {
                                                            if (!LibXML.HasFirstChildValue(noProdutoImpostoIcms00Det)) { throw new Exception(" Tag [imposto.ICMS.ICMS00.modBC] inválida!"); };
                                                            record_gc_movimentos_itens.icms_modbc = LibNumbers.ConvertInt(noProdutoImpostoIcms00Det.FirstChild.Value.ToString());
                                                        }
                                                        else if (noProdutoImpostoIcms00Det.Name == "pRedBC")
                                                        {
                                                            if (!LibXML.HasFirstChildValue(noProdutoImpostoIcms00Det)) { throw new Exception(" Tag [imposto.ICMS.ICMS00.pRedBC] inválida!"); };
                                                            record_gc_movimentos_itens.icms_predbc = LibNumbers.ConvertDecimal(noProdutoImpostoIcms00Det.FirstChild.Value.ToString());
                                                        }
                                                        else if (noProdutoImpostoIcms00Det.Name == "vBC")
                                                        {
                                                            if (!LibXML.HasFirstChildValue(noProdutoImpostoIcms00Det)) { throw new Exception(" Tag [imposto.ICMS.ICMS00.vBC] inválida!"); };
                                                            record_gc_movimentos_itens.icms_vbc = LibNumbers.ConvertDecimal(noProdutoImpostoIcms00Det.FirstChild.Value.ToString());
                                                        }
                                                        else if (noProdutoImpostoIcms00Det.Name == "pICMS")
                                                        {
                                                            if (!LibXML.HasFirstChildValue(noProdutoImpostoIcms00Det)) { throw new Exception(" Tag [imposto.ICMS.ICMS00.pICMS] inválida!"); };
                                                            record_gc_movimentos_itens.icms_picms = LibNumbers.ConvertDecimal(noProdutoImpostoIcms00Det.FirstChild.Value.ToString());
                                                        }
                                                        else if (noProdutoImpostoIcms00Det.Name == "vICMS")
                                                        {
                                                            if (!LibXML.HasFirstChildValue(noProdutoImpostoIcms00Det)) { throw new Exception(" Tag [imposto.ICMS.ICMS00.vICMS] inválida!"); };
                                                            record_gc_movimentos_itens.icms_vicms = LibNumbers.ConvertDecimal(noProdutoImpostoIcms00Det.FirstChild.Value.ToString());
                                                        };
                                                    }
                                                }
                                                if (noProdutoImpostoIcmsDet.Name == "ICMS20")
                                                {
                                                    XmlNodeList noProdutoImpostoIcms20 = noProdutoImpostoIcmsDet.ChildNodes;
                                                    foreach (XmlElement noProdutoImpostoIcms20Det in noProdutoImpostoIcms20)
                                                    {
                                                        if (noProdutoImpostoIcms20Det.Name == "orig")
                                                        {
                                                            if (!LibXML.HasFirstChildValue(noProdutoImpostoIcms20Det)) { throw new Exception(" Tag [imposto.ICMS.ICMS20.orig] inválida!"); };
                                                            record_gc_movimentos_itens.icms_orig = LibNumbers.ConvertInt(noProdutoImpostoIcms20Det.FirstChild.Value.ToString());
                                                        }
                                                        else if (noProdutoImpostoIcms20Det.Name == "CST")
                                                        {
                                                            if (!LibXML.HasFirstChildValue(noProdutoImpostoIcms20Det)) { throw new Exception(" Tag [imposto.ICMS.ICMS20.CST] inválida!"); };
                                                            record_gc_movimentos_itens.icms_cst = noProdutoImpostoIcms20Det.FirstChild.Value.ToString();
                                                        }
                                                        else if (noProdutoImpostoIcms20Det.Name == "modBC")
                                                        {
                                                            if (!LibXML.HasFirstChildValue(noProdutoImpostoIcms20Det)) { throw new Exception(" Tag [imposto.ICMS.ICMS20.modBC] inválida!"); };
                                                            record_gc_movimentos_itens.icms_modbc = LibNumbers.ConvertInt(noProdutoImpostoIcms20Det.FirstChild.Value.ToString());
                                                        }
                                                        else if (noProdutoImpostoIcms20Det.Name == "pRedBC")
                                                        {
                                                            if (!LibXML.HasFirstChildValue(noProdutoImpostoIcms20Det)) { throw new Exception(" Tag [imposto.ICMS.ICMS20.pRedBC] inválida!"); };
                                                            record_gc_movimentos_itens.icms_predbc = LibNumbers.ConvertDecimal(noProdutoImpostoIcms20Det.FirstChild.Value.ToString());
                                                        }
                                                        else if (noProdutoImpostoIcms20Det.Name == "vBC")
                                                        {
                                                            if (!LibXML.HasFirstChildValue(noProdutoImpostoIcms20Det)) { throw new Exception(" Tag [imposto.ICMS.ICMS20.vBC] inválida!"); };
                                                            record_gc_movimentos_itens.icms_vbc = LibNumbers.ConvertDecimal(noProdutoImpostoIcms20Det.FirstChild.Value.ToString());
                                                        }
                                                        else if (noProdutoImpostoIcms20Det.Name == "pICMS")
                                                        {
                                                            if (!LibXML.HasFirstChildValue(noProdutoImpostoIcms20Det)) { throw new Exception(" Tag [imposto.ICMS.ICMS20.pICMS] inválida!"); };
                                                            record_gc_movimentos_itens.icms_picms = LibNumbers.ConvertDecimal(noProdutoImpostoIcms20Det.FirstChild.Value.ToString());
                                                        }
                                                        else if (noProdutoImpostoIcms20Det.Name == "vICMS")
                                                        {
                                                            if (!LibXML.HasFirstChildValue(noProdutoImpostoIcms20Det)) { throw new Exception(" Tag [imposto.ICMS.ICMS20.vICMS] inválida!"); };
                                                            record_gc_movimentos_itens.icms_vicms = LibNumbers.ConvertDecimal(noProdutoImpostoIcms20Det.FirstChild.Value.ToString());
                                                        };
                                                    }
                                                }

                                            }
                                        }

                                        if (noProdutoImpostoDet.Name == "IPI")
                                        {
                                            XmlNodeList noProdutoImpostoIpi = noProdutoImpostoDet.ChildNodes;
                                            foreach (XmlElement noProdutoImpostoIpiDet in noProdutoImpostoIpi)
                                            {
                                                if (noProdutoImpostoIpiDet.Name == "cEnq")
                                                {
                                                    if (!LibXML.HasFirstChildValue(noProdutoImpostoIpiDet)) { throw new Exception(" Tag [IPI.cEnq] inválida!"); }
                                                    ;
                                                    record_gc_movimentos_itens.ipi_cenq = LibNumbers.ConvertInt(noProdutoImpostoIpiDet.FirstChild.Value.ToString());
                                                }
                                                else if (noProdutoImpostoIpiDet.Name == "IPINT")
                                                {
                                                    XmlNodeList noProdutoImpostoIpiInt = noProdutoImpostoIpiDet.ChildNodes;
                                                    foreach (XmlElement noProdutoImpostoIpiIntDet in noProdutoImpostoIpiInt)
                                                    {
                                                        if (noProdutoImpostoIpiIntDet.Name == "CST")
                                                        {
                                                            if (!LibXML.HasFirstChildValue(noProdutoImpostoIpiIntDet)) { throw new Exception(" Tag [IPI.IPINT.CST] inválida!"); }
                                                            ;
                                                            record_gc_movimentos_itens.ipi_cst = noProdutoImpostoIpiIntDet.FirstChild.Value.ToString();
                                                        }
                                                    }
                                                }
                                                else if (noProdutoImpostoIpiDet.Name == "IPITrib")
                                                {
                                                    XmlNodeList noProdutoImpostoTrib = noProdutoImpostoIpiDet.ChildNodes;
                                                    foreach (XmlElement noProdutoImpostoTribDet in noProdutoImpostoTrib)
                                                    {
                                                        if (noProdutoImpostoTribDet.Name == "CST")
                                                        {
                                                            if (!LibXML.HasFirstChildValue(noProdutoImpostoTribDet)) { throw new Exception(" Tag [IPI.IPITrib.CST] inválida!"); }
                                                            ;
                                                            record_gc_movimentos_itens.ipi_cst = noProdutoImpostoTribDet.FirstChild.Value.ToString();
                                                        }

                                                        else if (noProdutoImpostoTribDet.Name == "vBC")
                                                        {
                                                            if (!LibXML.HasFirstChildValue(noProdutoImpostoTribDet)) { throw new Exception(" Tag [IPI.IPITrib.vBC] inválida!"); }
                                                            ;
                                                            record_gc_movimentos_itens.ipi_vbc = LibNumbers.ConvertDecimal(noProdutoImpostoTribDet.FirstChild.Value.ToString());
                                                        }

                                                        else if (noProdutoImpostoTribDet.Name == "pIPI")
                                                        {
                                                            if (!LibXML.HasFirstChildValue(noProdutoImpostoTribDet)) { throw new Exception(" Tag [IPI.IPITrib.pIPI] inválida!"); }
                                                            ;
                                                            record_gc_movimentos_itens.ipi_pipi = LibNumbers.ConvertDecimal(noProdutoImpostoTribDet.FirstChild.Value.ToString());
                                                        }

                                                        else if (noProdutoImpostoTribDet.Name == "vIPI")
                                                        {
                                                            if (!LibXML.HasFirstChildValue(noProdutoImpostoTribDet)) { throw new Exception(" Tag [IPI.IPITrib.vIPI] inválida!"); }
                                                            ;
                                                            record_gc_movimentos_itens.ipi_vipi = LibNumbers.ConvertDecimal(noProdutoImpostoTribDet.FirstChild.Value.ToString());
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        if (noProdutoImpostoDet.Name == "II")
                                        {
                                            // Não Utilizado
                                            XmlNodeList noProdutoImpostoII = noProdutoImpostoDet.ChildNodes;
                                            foreach (XmlElement noProdutoImpostoIIDet in noProdutoImpostoII)
                                            {
                                                if (noProdutoImpostoIIDet.Name == "vBC")
                                                {
                                                    if (!LibXML.HasFirstChildValue(noProdutoImpostoIIDet)) { throw new Exception(" Tag [II.vBC] inválida!"); }
                                                    ;
                                                    record_gc_movimentos_itens.ii_vbc = LibNumbers.ConvertDecimal(noProdutoImpostoIIDet.FirstChild.Value.ToString());
                                                }
                                                else if (noProdutoImpostoIIDet.Name == "vDespAdu")
                                                {
                                                    if (!LibXML.HasFirstChildValue(noProdutoImpostoIIDet)) { throw new Exception(" Tag [II.vDespAdu] inválida!"); }
                                                    ;
                                                    record_gc_movimentos_itens.ii_vdespadu = LibNumbers.ConvertDecimal(noProdutoImpostoIIDet.FirstChild.Value.ToString());
                                                }
                                                else if (noProdutoImpostoIIDet.Name == "vII")
                                                {
                                                    if (!LibXML.HasFirstChildValue(noProdutoImpostoIIDet)) { throw new Exception(" Tag [II.vII] inválida!"); }
                                                    ;
                                                    record_gc_movimentos_itens.ii_vii = LibNumbers.ConvertDecimal(noProdutoImpostoIIDet.FirstChild.Value.ToString());
                                                }
                                                else if (noProdutoImpostoIIDet.Name == "vIOF")
                                                {
                                                    if (!LibXML.HasFirstChildValue(noProdutoImpostoIIDet)) { throw new Exception(" Tag [II.vIOF] inválida!"); }
                                                    ;
                                                    record_gc_movimentos_itens.ii_viof = LibNumbers.ConvertDecimal(noProdutoImpostoIIDet.FirstChild.Value.ToString());
                                                }
                                            }
                                        }

                                        if (noProdutoImpostoDet.Name == "PIS")
                                        {
                                            XmlNodeList noProdutoImpostoPis = noProdutoImpostoDet.ChildNodes;
                                            foreach (XmlElement noProdutoImpostoPisDet in noProdutoImpostoPis)
                                            {
                                                if (noProdutoImpostoPisDet.Name == "PISAliq")
                                                {
                                                    XmlNodeList noProdutoImpostoPisAliq = noProdutoImpostoPisDet.ChildNodes;
                                                    foreach (XmlElement noProdutoImpostoPisAliqDet in noProdutoImpostoPisAliq)
                                                    {
                                                        if (noProdutoImpostoPisAliqDet.Name == "CST")
                                                        {
                                                            if (!LibXML.HasFirstChildValue(noProdutoImpostoPisAliqDet)) { throw new Exception(" Tag [PIS.PISAliq.CST] inválida!"); }
                                                            ;
                                                            record_gc_movimentos_itens.pis_cst = noProdutoImpostoPisAliqDet.FirstChild.Value.ToString();
                                                        }
                                                        if (noProdutoImpostoPisAliqDet.Name == "vBC")
                                                        {
                                                            if (!LibXML.HasFirstChildValue(noProdutoImpostoPisAliqDet)) { throw new Exception(" Tag [PIS.PISAliq.vBC] inválida!"); }
                                                            ;
                                                            record_gc_movimentos_itens.pis_vbc = LibNumbers.ConvertDecimal(noProdutoImpostoPisAliqDet.FirstChild.Value.ToString());
                                                        }
                                                        if (noProdutoImpostoPisAliqDet.Name == "pPIS")
                                                        {
                                                            if (!LibXML.HasFirstChildValue(noProdutoImpostoPisAliqDet)) { throw new Exception(" Tag [PIS.PISAliq.pPIS] inválida!"); }
                                                            ;
                                                            record_gc_movimentos_itens.pis_ppis = LibNumbers.ConvertDecimal(noProdutoImpostoPisAliqDet.FirstChild.Value.ToString());
                                                        }
                                                        if (noProdutoImpostoPisAliqDet.Name == "vPIS")
                                                        {
                                                            if (!LibXML.HasFirstChildValue(noProdutoImpostoPisAliqDet)) { throw new Exception(" Tag [PIS.PISAliq.vPIS] inválida!"); }
                                                            ;
                                                            record_gc_movimentos_itens.pis_vpis = LibNumbers.ConvertDecimal(noProdutoImpostoPisAliqDet.FirstChild.Value.ToString());
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        if (noProdutoImpostoDet.Name == "COFINS")
                                        {
                                            XmlNodeList noProdutoImpostoCofins = noProdutoImpostoDet.ChildNodes;
                                            foreach (XmlElement noProdutoImpostoCofinsDet in noProdutoImpostoCofins)
                                            {
                                                if (noProdutoImpostoCofinsDet.Name == "COFINSAliq")
                                                {
                                                    XmlNodeList noProdutoImpostoCofinsAliq = noProdutoImpostoCofinsDet.ChildNodes;
                                                    foreach (XmlElement noProdutoImpostoCofinsAliqDet in noProdutoImpostoCofinsAliq)
                                                    {
                                                        if (noProdutoImpostoCofinsAliqDet.Name == "CST")
                                                        {
                                                            if (!LibXML.HasFirstChildValue(noProdutoImpostoCofinsAliqDet)) { throw new Exception(" Tag [COFINS.COFINSAliq.CST] inválida!"); }
                                                            ;
                                                            record_gc_movimentos_itens.cofins_cst = noProdutoImpostoCofinsAliqDet.FirstChild.Value.ToString();
                                                        }
                                                        if (noProdutoImpostoCofinsAliqDet.Name == "vBC")
                                                        {
                                                            if (!LibXML.HasFirstChildValue(noProdutoImpostoCofinsAliqDet)) { throw new Exception(" Tag [COFINS.COFINSAliq.vBC] inválida!"); }
                                                            ;
                                                            record_gc_movimentos_itens.cofins_vbc = LibNumbers.ConvertDecimal(noProdutoImpostoCofinsAliqDet.FirstChild.Value.ToString());
                                                        }
                                                        if (noProdutoImpostoCofinsAliqDet.Name == "pCOFINS")
                                                        {
                                                            if (!LibXML.HasFirstChildValue(noProdutoImpostoCofinsAliqDet)) { throw new Exception(" Tag [COFINS.COFINSAliq.pCOFINS] inválida!"); }
                                                            ;
                                                            record_gc_movimentos_itens.cofins_pcofins = LibNumbers.ConvertDecimal(noProdutoImpostoCofinsAliqDet.FirstChild.Value.ToString());
                                                        }
                                                        if (noProdutoImpostoCofinsAliqDet.Name == "vCOFINS")
                                                        {
                                                            if (!LibXML.HasFirstChildValue(noProdutoImpostoCofinsAliqDet)) { throw new Exception(" Tag [COFINS.COFINSAliq.vCOFINS] inválida!"); }
                                                            ;
                                                            record_gc_movimentos_itens.cofins_vcofins = LibNumbers.ConvertDecimal(noProdutoImpostoCofinsAliqDet.FirstChild.Value.ToString());
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        if (noProdutoImpostoDet.Name == "IBSCBS")
                                        {
                                            XmlNodeList noProdutoImpostoIbsCbs = noProdutoImpostoDet.ChildNodes;
                                            foreach (XmlElement noProdutoImpostoIbsCbsDet in noProdutoImpostoIbsCbs)
                                            {
                                                if (noProdutoImpostoIbsCbsDet.Name == "CST")
                                                {
                                                    if (LibXML.HasFirstChildValue(noProdutoImpostoIbsCbsDet))
                                                    {
                                                        record_gc_movimentos_itens.ibs_cbs_cst = noProdutoImpostoIbsCbsDet.FirstChild.Value.ToString();
                                                    }
                                                }
                                                else if (noProdutoImpostoIbsCbsDet.Name == "cClassTrib")
                                                {
                                                    if (LibXML.HasFirstChildValue(noProdutoImpostoIbsCbsDet))
                                                    {
                                                        record_gc_movimentos_itens.c_class_trib = noProdutoImpostoIbsCbsDet.FirstChild.Value.ToString();
                                                    }
                                                }
                                                else if (noProdutoImpostoIbsCbsDet.Name == "gIBSCBS")
                                                {
                                                    XmlNodeList noProdutoImpostoGIbsCbs = noProdutoImpostoIbsCbsDet.ChildNodes;
                                                    foreach (XmlElement noProdutoImpostoGIbsCbsDet in noProdutoImpostoGIbsCbs)
                                                    {
                                                        if (noProdutoImpostoGIbsCbsDet.Name == "vBC")
                                                        {
                                                            if (LibXML.HasFirstChildValue(noProdutoImpostoGIbsCbsDet))
                                                            {
                                                                record_gc_movimentos_itens.ibs_cbs_vbc = LibNumbers.ConvertDecimal(noProdutoImpostoGIbsCbsDet.FirstChild.Value.ToString());
                                                            }
                                                        }
                                                        else if (noProdutoImpostoGIbsCbsDet.Name == "gIBSUF")
                                                        {
                                                            XmlNodeList noProdutoImpostoGIbsUf = noProdutoImpostoGIbsCbsDet.ChildNodes;
                                                            foreach (XmlElement noProdutoImpostoGIbsUfDet in noProdutoImpostoGIbsUf)
                                                            {
                                                                if (noProdutoImpostoGIbsUfDet.Name == "pIBSUF")
                                                                {
                                                                    if (LibXML.HasFirstChildValue(noProdutoImpostoGIbsUfDet))
                                                                    {
                                                                        record_gc_movimentos_itens.ibs_pibs = LibNumbers.ConvertDecimal(noProdutoImpostoGIbsUfDet.FirstChild.Value.ToString());
                                                                    }
                                                                }
                                                                else if (noProdutoImpostoGIbsUfDet.Name == "vIBSUF")
                                                                {
                                                                    if (LibXML.HasFirstChildValue(noProdutoImpostoGIbsUfDet))
                                                                    {
                                                                        record_gc_movimentos_itens.ibs_vibs = LibNumbers.ConvertDecimal(noProdutoImpostoGIbsUfDet.FirstChild.Value.ToString());
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        else if (noProdutoImpostoGIbsCbsDet.Name == "gCBS")
                                                        {
                                                            XmlNodeList noProdutoImpostoGCbs = noProdutoImpostoGIbsCbsDet.ChildNodes;
                                                            foreach (XmlElement noProdutoImpostoGCbsDet in noProdutoImpostoGCbs)
                                                            {
                                                                if (noProdutoImpostoGCbsDet.Name == "pCBS")
                                                                {
                                                                    if (LibXML.HasFirstChildValue(noProdutoImpostoGCbsDet))
                                                                    {
                                                                        record_gc_movimentos_itens.cbs_pcbs = LibNumbers.ConvertDecimal(noProdutoImpostoGCbsDet.FirstChild.Value.ToString());
                                                                    }
                                                                }
                                                                else if (noProdutoImpostoGCbsDet.Name == "vCBS")
                                                                {
                                                                    if (LibXML.HasFirstChildValue(noProdutoImpostoGCbsDet))
                                                                    {
                                                                        record_gc_movimentos_itens.cbs_vcbs = LibNumbers.ConvertDecimal(noProdutoImpostoGCbsDet.FirstChild.Value.ToString());
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            // Validação do Produto
                            if ((record_cstImportacaoNFEntrada.id_movimento_tipo == 5) || (record_cstImportacaoNFEntrada.id_movimento_tipo == 9)) // 5 - Compra - Fornecedor - Nacional | 9 - Devolução
                            {
                                record_gc_movimentos_itens.id_produto = 0;
                                record_gc_movimentos_itens.quantidade = _ProdutoQtdItem;
                                record_gc_movimentos_itens.valor_unit = (_ProdutoValorTotalItem / _ProdutoQtdItem);
                                record_gc_movimentos_itens.valor_total = _ProdutoValorTotalItem;
                                record_gc_movimentos_itens.valor_despesas = _NotaEntradaValorDespesas;
                                record_gc_movimentos_itens.produto_externo_codigo = _ProdutoCodigoExterno;
                                record_gc_movimentos_itens.produto_externo_nome = _ProdutoDescricaoExterna;
                                record_gc_movimentos_itens.id_coligada = RecordMovimentoNovo.id_coligada;
                                record_gc_movimentos_itens.id_filial = RecordMovimentoNovo.id_filial;
                                listaItens.Add(record_gc_movimentos_itens);
                            }
                            else if (record_cstImportacaoNFEntrada.id_movimento_tipo == 6)  // 6 - Compra - Fornecedor - Exterior | Importação
                            {
                                if ((_ProdutoCodigo.Length > 0) && (_ProdutoCodigoReduzido.Length > 0) && (_ProdutoDescricao.Length > 0) && (_ProdutoNcm.Length > 0))
                                {
                                    g_produtos_ncm record_g_produtos_ncm = null;
                                    g_unidade_medida record_g_unidade_medida = null;

                                    // Cadastro de NCM
                                    if (_ProdutoNcm.EmptyIfNull().ToString().Trim().Length > 0)
                                    {
                                        record_g_produtos_ncm = allProdutosNCM.Where(n => n.codigo_ncm == _ProdutoNcm).FirstOrDefault();
                                        if (record_g_produtos_ncm == null)
                                        {
                                            record_g_produtos_ncm = GDI.LibGDI.CadastrarProdutoNCM(db, _ProdutoNcm);
                                            allProdutosNCM = db.g_produtos_ncm.ToList();
                                        };
                                    }

                                    // Cadastro de Unidade de Medida
                                    if (_ProdutoUnidadeMedida.EmptyIfNull().ToString().Length > 0)
                                    {
                                        record_g_unidade_medida = allUnidadesMedidas.Where(m => m.descricao == _ProdutoUnidadeMedida).FirstOrDefault();
                                        if (record_g_unidade_medida == null)
                                        {
                                            record_g_unidade_medida = GDI.LibGDI.CadastrarUnidadeMedida(db, _ProdutoUnidadeMedida);
                                            allUnidadesMedidas = db.g_unidade_medida.ToList();
                                        };
                                    }
                                    idProdutoAtual = -1;

                                    // CADASTRO DE PRODUTOS - 3
                                    String PNOficial = _ProdutoCodigo;
                                    String PNAuxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(_ProdutoCodigo);
                                    String PNCuringaOH = PNAuxiliar.EmptyIfNull().ToString().Replace("0", "O");
                                    String PNCuringaZERO = PNAuxiliar.EmptyIfNull().ToString().Replace("O", "0");

                                    g_produtos ProdutoGDI = ListaProdutosGDI.Where(p => p.codigo == PNOficial).FirstOrDefault(); // Buscar pelo PN principal
                                    try { if (ProdutoGDI == null) { ProdutoGDI = ListaProdutosGDI.Where(p => p.codigo_auxiliar == PNAuxiliar || p.codigo_variacao1 == PNCuringaOH || p.codigo_variacao2 == PNCuringaZERO).FirstOrDefault(); }; } catch (Exception) { };// Buscar pelo PN Auxiliar

                                    // O produto já deverá estar cadastrado
                                    if (ProdutoGDI != null)
                                    {
                                        g_produtos record_old_g_produtos = LibDB.CloneTObject(ProdutoGDI);
                                        idProdutoAtual = ProdutoGDI.id_produto;

                                        // Atualizacao do item da nota
                                        record_gc_movimentos_itens.id_produto = idProdutoAtual;
                                        record_gc_movimentos_itens.quantidade = _ProdutoQtdItem;
                                        record_gc_movimentos_itens.valor_unit = (_ProdutoValorTotalItem / _ProdutoQtdItem);
                                        record_gc_movimentos_itens.valor_total = _ProdutoValorTotalItem;
                                        record_gc_movimentos_itens.valor_despesas = _NotaEntradaValorDespesas;
                                        record_gc_movimentos_itens.id_coligada = RecordMovimentoNovo.id_coligada;
                                        record_gc_movimentos_itens.id_filial = RecordMovimentoNovo.id_filial;
                                        listaItens.Add(record_gc_movimentos_itens);
                                    }
                                    else if (idProdutoAtual == -1)
                                    {
                                        ErroProcessamento = true;
                                        QtdProdutosNaoCadastrados += 1;
                                        NomesProdutosNaoCadastrados += _ProdutoCodigo + ", ";
                                    }
                                }
                            }
                        }
                        #endregion

                        #region Total do ICMS
                        foreach (XmlElement noTotal in NodeTotal[0].ChildNodes)
                        {
                            if (noTotal.Name == "ICMSTot")
                            {
                                XmlNodeList noTotalIcms = noTotal.ChildNodes;
                                foreach (XmlElement noTotalIcmsDet in noTotalIcms)
                                {
                                    /*if (noTotalIcmsDet.Name == "vBC")
                                    {
                                        if (!LibXML.HasFirstChildValue(noTotalIcmsDet)) { throw new Exception(" Tag [ICMSTot.vBC] inválida!"); };
                                        record_gc_movimentos.icms_vbc = LibNumbers.ConvertDecimal(noTotalIcmsDet.FirstChild.Value.ToString());
                                    }
                                    else if (noTotalIcmsDet.Name == "vICMS")
                                    {
                                        if (!LibXML.HasFirstChildValue(noTotalIcmsDet)) { throw new Exception(" Tag [ICMSTot.vICMS] inválida!"); };
                                        record_gc_movimentos.icms_vicms = LibNumbers.ConvertDecimal(noTotalIcmsDet.FirstChild.Value.ToString());
                                    }
                                    else if (noTotalIcmsDet.Name == "vBCST")
                                    {
                                        if (!LibXML.HasFirstChildValue(noTotalIcmsDet)) { throw new Exception(" Tag [ICMSTot.vBCST] inválida!"); };
                                        record_gc_movimentos.icms_vbcst = LibNumbers.ConvertDecimal(noTotalIcmsDet.FirstChild.Value.ToString());
                                    }
                                    else if (noTotalIcmsDet.Name == "vST")
                                    {
                                        if (!LibXML.HasFirstChildValue(noTotalIcmsDet)) { throw new Exception(" Tag [ICMSTot.vST] inválida!"); };
                                        record_gc_movimentos.icms_vst = LibNumbers.ConvertDecimal(noTotalIcmsDet.FirstChild.Value.ToString());
                                    }
                                    else if (noTotalIcmsDet.Name == "vProd")
                                    {
                                        if (!LibXML.HasFirstChildValue(noTotalIcmsDet)) { throw new Exception(" Tag [ICMSTot.vProd] inválida!"); };
                                        record_gc_movimentos.icms_vprod = LibNumbers.ConvertDecimal(noTotalIcmsDet.FirstChild.Value.ToString());
                                    }
                                    else if (noTotalIcmsDet.Name == "vFrete")
                                    {
                                        if (!LibXML.HasFirstChildValue(noTotalIcmsDet)) { throw new Exception(" Tag [ICMSTot.vFrete] inválida!"); };
                                        record_gc_movimentos.icms_vfrete = LibNumbers.ConvertDecimal(noTotalIcmsDet.FirstChild.Value.ToString());
                                    }
                                    else if (noTotalIcmsDet.Name == "vSeg")
                                    {
                                        if (!LibXML.HasFirstChildValue(noTotalIcmsDet)) { throw new Exception(" Tag [ICMSTot.vSeg] inválida!"); };
                                        record_gc_movimentos.icms_vseg = LibNumbers.ConvertDecimal(noTotalIcmsDet.FirstChild.Value.ToString());
                                    }
                                    else if (noTotalIcmsDet.Name == "vDesc")
                                    {
                                        if (!LibXML.HasFirstChildValue(noTotalIcmsDet)) { throw new Exception(" Tag [ICMSTot.vDesc] inválida!"); };
                                        record_gc_movimentos.icms_vdesc = LibNumbers.ConvertDecimal(noTotalIcmsDet.FirstChild.Value.ToString());
                                    }
                                    else if (noTotalIcmsDet.Name == "vII")
                                    {
                                        if (!LibXML.HasFirstChildValue(noTotalIcmsDet)) { throw new Exception(" Tag [ICMSTot.vII] inválida!"); };
                                        record_gc_movimentos.icms_vii = LibNumbers.ConvertDecimal(noTotalIcmsDet.FirstChild.Value.ToString());
                                    }
                                    else if (noTotalIcmsDet.Name == "vPIS")
                                    {
                                        if (!LibXML.HasFirstChildValue(noTotalIcmsDet)) { throw new Exception(" Tag [ICMSTot.vPIS] inválida!"); };
                                        record_gc_movimentos.icms_vpis = LibNumbers.ConvertDecimal(noTotalIcmsDet.FirstChild.Value.ToString());
                                    }
                                    else if (noTotalIcmsDet.Name == "vCOFINS")
                                    {
                                        if (!LibXML.HasFirstChildValue(noTotalIcmsDet)) { throw new Exception(" Tag [ICMSTot.vCOFINS] inválida!"); };
                                        record_gc_movimentos.icms_vcofins = LibNumbers.ConvertDecimal(noTotalIcmsDet.FirstChild.Value.ToString());
                                    }
                                    else if (noTotalIcmsDet.Name == "vOutro")
                                    {
                                        if (!LibXML.HasFirstChildValue(noTotalIcmsDet)) { throw new Exception(" Tag [ICMSTot.vOutro] inválida!"); };
                                        record_gc_movimentos.icms_voutro = LibNumbers.ConvertDecimal(noTotalIcmsDet.FirstChild.Value.ToString());
                                    }
                                    else if (noTotalIcmsDet.Name == "vNF")
                                    {
                                        if (!LibXML.HasFirstChildValue(noTotalIcmsDet)) { throw new Exception(" Tag [ICMSTot.vNF] inválida!"); };
                                        record_gc_movimentos.icms_vnf = LibNumbers.ConvertDecimal(noTotalIcmsDet.FirstChild.Value.ToString());
                                    }*/ ///// TAGS DE IMPOSTO
                                }
                            }
                        }
                        #endregion

                        #region Transporte
                        if (NodeTransp[0].ChildNodes != null)
                        {
                            foreach (XmlElement noTransp in NodeTransp[0].ChildNodes)
                            {
                                if (noTransp.Name == "vol")
                                {
                                    XmlNodeList noTranspVol = noTransp.ChildNodes;
                                    foreach (XmlElement noTranspVolDet in noTranspVol)
                                    {
                                        if (noTranspVolDet.Name == "qVol")
                                        {
                                            if (LibXML.HasFirstChildValue(noTranspVolDet)) { RecordMovimentoNovo.frete_qvol = LibNumbers.ConvertInt(noTranspVolDet.FirstChild.Value.ToString()); };
                                        }
                                        else if (noTranspVolDet.Name == "esp")
                                        {
                                            if (LibXML.HasFirstChildValue(noTranspVolDet)) { RecordMovimentoNovo.frete_esp = noTranspVolDet.FirstChild.Value.ToString(); };
                                        }
                                        else if (noTranspVolDet.Name == "marca")
                                        {
                                            if (LibXML.HasFirstChildValue(noTranspVolDet)) { RecordMovimentoNovo.frete_marca = noTranspVolDet.FirstChild.Value.ToString(); };
                                        }
                                        else if (noTranspVolDet.Name == "nVol")
                                        {
                                            if (LibXML.HasFirstChildValue(noTranspVolDet)) { RecordMovimentoNovo.frete_nvol = noTranspVolDet.FirstChild.Value.ToString(); };
                                        }
                                        else if (noTranspVolDet.Name == "pesoL")
                                        {
                                            if (LibXML.HasFirstChildValue(noTranspVolDet)) { RecordMovimentoNovo.frete_pesol = LibNumbers.ConvertDecimal(noTranspVolDet.FirstChild.Value.ToString()); };
                                        }
                                        else if (noTranspVolDet.Name == "pesoB")
                                        {
                                            if (LibXML.HasFirstChildValue(noTranspVolDet)) { RecordMovimentoNovo.frete_pesob = LibNumbers.ConvertDecimal(noTranspVolDet.FirstChild.Value.ToString()); };
                                        }
                                    }
                                }
                            }
                        }
                        #endregion

                        #region Informações Adicionais
                        if (NodeInfAdic[0] != null)
                        {
                            foreach (XmlElement noInfAdic in NodeInfAdic[0].ChildNodes)
                            {
                                if (noInfAdic.Name == "infCpl")
                                {
                                    if (LibXML.HasFirstChildValue(noInfAdic))
                                    {
                                        if (noInfAdic.FirstChild.Value.EmptyIfNull().ToString().Length == 0) { throw new Exception(" Tag [infCpl] inválida!"); };
                                        InformacoesAdicionaisNF = noInfAdic.FirstChild.Value.ToString().Trim();
                                        InformacoesAdicionaisNF = LibStringFormat.RemoverEspacosDuplos(InformacoesAdicionaisNF);
                                        InformacoesAdicionaisNF = LibStringFormat.SomenteAlfabetoeNumeros(InformacoesAdicionaisNF);
                                        InformacoesAdicionaisNF = LibStringFormat.RemoverAcentos(InformacoesAdicionaisNF);
                                        if (InformacoesAdicionaisNF.Length > 200) { InformacoesAdicionaisNF = InformacoesAdicionaisNF.Substring(0, 200); };
                                    }
                                }
                            }
                        }
                        #endregion

                        #region Totalizadores
                        record_NfeAutorizacao.chNFe = RecordMovimentoNovo.documento_numero;
                        #endregion

                        #region Informações de Protocolo
                        if (NodeInfAutorizacao != null)
                        {
                            foreach (XmlElement noInfAutorizacao in NodeInfAutorizacao[0].ChildNodes)
                            {
                                if (noInfAutorizacao.Name == "chNFe") { record_NfeAutorizacao.chNFe = noInfAutorizacao.FirstChild.Value.ToString(); }
                                else if (noInfAutorizacao.Name == "dhRecbto") { record_NfeAutorizacao.dhRecbto = noInfAutorizacao.FirstChild.Value.ToString(); }
                                else if (noInfAutorizacao.Name == "nProt") { record_NfeAutorizacao.nProt = noInfAutorizacao.FirstChild.Value.ToString(); }
                                else if (noInfAutorizacao.Name == "digVal") { record_NfeAutorizacao.digVal = noInfAutorizacao.FirstChild.Value.ToString(); }
                                else if (noInfAutorizacao.Name == "cStat") { record_NfeAutorizacao.cStat = noInfAutorizacao.FirstChild.Value.ToString(); }
                                else if (noInfAutorizacao.Name == "xMotivo") { record_NfeAutorizacao.xMotivo = noInfAutorizacao.FirstChild.Value.ToString(); }
                            }
                        }
                        #endregion
                    }

                    if (xmlDocument != null)
                    {
                        xmlDocument = null;
                        LibCache.LiberarMemoria();
                    }

                    if (QtdProdutosNaoCadastrados > 0)
                    {
                        MsgRetorno += "Foram identificados <b>" + QtdProdutosNaoCadastrados.ToString() + "</b> produto(s) NÃO cadastrados" + "<br/>";
                        if (NomesProdutosNaoCadastrados.EmptyIfNull().ToString().Length > 0) { MsgRetorno += NomesProdutosNaoCadastrados + "<br/>"; };
                        if (record_cstImportacaoNFEntrada.id_movimento_tipo == 6) // 6 - Compra - Fornecedor - Exterior | Importação
                        {
                            ErroProcessamento = true;
                            MsgRetorno += "Execute o upload/processamento da planilha de itens referente à essa importação no módulo COMEX!" + "<br/>";
                        }
                        else if ((record_cstImportacaoNFEntrada.id_movimento_tipo == 5) || (record_cstImportacaoNFEntrada.id_movimento_tipo == 9)) // 5 - Compra - Fornecedor - Nacional | 9 - Devolução
                        {
                            ErroProcessamento = false;
                            MsgRetorno += "Execute o processamento dos Produtos (novos)!" + "<br/>";
                        }
                    }
                    else if (listaItens.Count == 0)
                    {
                        ErroProcessamento = true;
                        MsgRetorno += "<br/>" + "Não há itens a serem importados" + "<br/>";
                    }


                    if (record_cstImportacaoNFEntrada.id_movimento_tipo == 6) // 6 - Compra - Fornecedor - Exterior | Importação
                    {

                        String SqlConsultaNF = "select * from gc_comex_importacoes where nf_despachante_numero like '%|" + _numeroNF + "|%'";
                        gc_comex_importacoes record_gc_comex_importacoes = db.gc_comex_importacoes.SqlQuery(SqlConsultaNF).FirstOrDefault();
                        if (record_gc_comex_importacoes != null)
                        {
                            IdImportacao = record_gc_comex_importacoes.id_importacao;
                            if (record_gc_comex_importacoes.di_numero.EmptyIfNull().ToString().Equals(String.Empty))
                            {
                                ErroProcessamento = true;
                                MsgRetorno += "Campo [Número DI] é de preenchimento obrigatório na importação referente à NF Número [" + _numeroNF + "]!" + "<br/>";
                            }
                            if ((record_gc_comex_importacoes.di_cambio.EmptyIfNull().ToString().Equals(String.Empty)) || (record_gc_comex_importacoes.di_cambio <= 0))
                            {
                                ErroProcessamento = true;
                                MsgRetorno += "Campo [DI Câmbio] é de preenchimento obrigatório na importação referente à NF Número [" + _numeroNF + "]!" + "<br/>";
                            }
                            int ItensNaoEncontradosQtd = 0;
                            String ItensNaoEncontradosPN = string.Empty;
                            List<gc_comex_importacoes_itens> ListaItensImportacao = db.gc_comex_importacoes_itens.Where(i => i.ativo == true && i.id_importacao == record_gc_comex_importacoes.id_importacao).ToList();
                            foreach (var item in listaItens)
                            {
                                String ItemPN = string.Empty;
                                try { ItemPN = db.g_produtos.Find(item.id_produto).codigo; } catch (Exception) { };
                                if (ItemPN.EmptyIfNull().ToString().Length > 0)
                                {

                                    String PNOficial = ItemPN;
                                    String PNAuxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(PNOficial);
                                    String PNCuringaOH = PNAuxiliar.EmptyIfNull().ToString().Replace("0", "O");
                                    String PNCuringaZERO = PNAuxiliar.EmptyIfNull().ToString().Replace("O", "0");

                                    gc_comex_importacoes_itens ComexImportacaoItem = ListaItensImportacao.Where(i => i.pn == ItemPN).FirstOrDefault();
                                    try { if (ComexImportacaoItem == null) { ComexImportacaoItem = ListaItensImportacao.Where(i => i.pn_auxiliar == PNAuxiliar || i.pn_variacao1 == PNCuringaOH || i.pn_variacao2 == PNCuringaZERO).FirstOrDefault(); }; } catch (Exception) { };// Buscar pelo PN Auxiliar

                                    if (ComexImportacaoItem == null)
                                    {
                                        ItensNaoEncontradosQtd += 1;
                                        ItensNaoEncontradosPN += ItemPN + ", ";
                                    }
                                }
                            }
                            if (ItensNaoEncontradosQtd > 0)
                            {
                                ErroProcessamento = true;
                                MsgRetorno += "Na planilha de itens referente à essa importação não foram localizados os itens com os identificadores:" + "<br/>" + ItensNaoEncontradosPN;
                            }
                        }
                        else
                        {
                            ErroProcessamento = true;
                            MsgRetorno += "Não foi localizado no módulo COMEX, a planilha de itens referente à importação da NF Número [" + _numeroNF + "]!" + "<br/>";
                        }
                    }
                    else if ((record_cstImportacaoNFEntrada.id_movimento_tipo == 5) || (record_cstImportacaoNFEntrada.id_movimento_tipo == 9)) // 5 - Compra - Fornecedor - Nacional | 9 - Devolução
                    {
                        if (NfeCFOP.EmptyIfNull().ToString().Length > 0)
                        {
                            String[] listaCFOPs = null;
                            listaCFOPs = NfeCFOP.Split(';');
                            foreach (String _cfop in listaCFOPs)
                            {
                                String _cfopReferencia = _cfop.Replace(";", "");
                                if (_cfopReferencia.EmptyIfNull().ToString().Length > 2)
                                {
                                    gc_cfop record_gc_cfop = db.gc_cfop.Where(c => c.numero == _cfopReferencia).FirstOrDefault();
                                    if (record_gc_cfop != null)
                                    {
                                        if ((record_cstImportacaoNFEntrada.id_movimento_tipo == 5) && (record_gc_cfop.nfe_devolucao == true))   // 5 - Compra - Fornecedor - Nacional
                                        {
                                            ErroProcessamento = true;
                                            MsgRetorno += " - CFOP [" + _cfopReferencia + "] Não permitido para operação de Compra" + "<br/>";
                                        }
                                        else if ((record_cstImportacaoNFEntrada.id_movimento_tipo == 9) && (record_gc_cfop.nfe_devolucao == false)) // 9 - Devolução
                                        {
                                            ErroProcessamento = true;
                                            MsgRetorno += " - CFOP [" + _cfopReferencia + "] Não permitido para operação de Devolução" + "<br/>";
                                        }
                                    }
                                    else
                                    {
                                        ErroProcessamento = true;
                                        MsgRetorno += " - CFOP [" + _cfopReferencia + "] Não foi localizado no ERP" + "<br/>";
                                    }
                                }
                            }
                        }
                    }

                    if ((ErroProcessamento == false) && (listaItens.Count > 0))
                    {
                        // Verificar movimento duplicado
                        String SentencaSQL = "select * from gc_movimentos where id_cliente = " + record_g_cliente.id_cliente.EmptyIfNull().ToString() + " and nf_numero = '" + _numeroNF.EmptyIfNull().ToString() + "' and nf_serie = '" + _serieNF.EmptyIfNull().ToString() + "' and entrada_nfe_processada = 1";
                        Db.gc_movimentos RecordMovimentoDuplicado = db.gc_movimentos.SqlQuery(SentencaSQL).ToList().FirstOrDefault();
                        if (RecordMovimentoDuplicado != null)
                        {
                            MsgRetorno += "<b style=\"color:#cc0000\">----- ATENÇÃO -----</b>" + "<br/>";
                            MsgRetorno += "<b>ENTRADA DUPLICADA - MOVIMENTO Nº "+ RecordMovimentoDuplicado.id_movimento.EmptyIfNull().ToString() + "</b>" + "<br/><br/>";
                            MsgRetorno += "Já consta no ERP o processamento da NF Nº <b>" + _numeroNF + "</b> Série <b>" + _serieNF + "</b> de " + "<br/>" + "<b>" + record_g_cliente.nome.EmptyIfNull().ToString() + "</b>" + "<br/><br/>";
                            ErroProcessamento = true;
                        }

                        if (!ErroProcessamento)
                        {
                            Decimal _valorTotalNF = 0;
                            Decimal _valorDescontosNF = 0;
                            Decimal _valorFreteNF = 0;
                            Decimal _valorSeguroNF = 0;
                            Decimal _NotaEntradaValorDespesasNF = 0;

                            foreach (var item in listaItens)
                            {
                                _valorTotalNF += item.valor_total;
                                _valorDescontosNF += item.valor_desconto;
                                _valorFreteNF += item.valor_frete;
                                _valorSeguroNF += item.valor_seguro;
                                _NotaEntradaValorDespesasNF += item.valor_despesas;
                            }
                            //_valorTotalNF += record_NfeIcmsTotal.vICMS;
                            valorTotalNF = _valorTotalNF;

                            if (record_cstImportacaoNFEntrada.id_movimento_tipo == 9) // 9 - Devolução - Movimento de referência da devolução
                            {
                                if (record_gc_movimentos_nf_referencia != null) { RecordMovimentoNovo.id_movimento_ref = record_gc_movimentos_nf_referencia.id_movimento; };
                            }

                            if ((record_cstImportacaoNFEntrada.id_movimento_tipo == 5) || (record_cstImportacaoNFEntrada.id_movimento_tipo == 9)) // Compra Nacional ou Importação
                            {
                                RecordMovimentoNovo.id_movimento_status = 99; // Temporário
                            }

                            RecordMovimentoNovo.entrada_nfe_processada = false;
                            RecordMovimentoNovo.id_movimento_tipo = record_cstImportacaoNFEntrada.id_movimento_tipo;
                            RecordMovimentoNovo.id_movimento_status = 2;
                            RecordMovimentoNovo.id_importacao = IdImportacao;
                            RecordMovimentoNovo.id_moeda = 1;
                            RecordMovimentoNovo.movimento_faturado = false;
                            RecordMovimentoNovo.id_cliente = record_g_cliente.id_cliente;
                            RecordMovimentoNovo.id_vendedor = 0;
                            RecordMovimentoNovo.nf_numero = _numeroNF;
                            RecordMovimentoNovo.nf_serie = _serieNF;
                            RecordMovimentoNovo.qtd_itens = listaItens.Count();
                            RecordMovimentoNovo.qtd_produtos = listaItens.Count();
                            RecordMovimentoNovo.nf_chave = record_NfeAutorizacao.chNFe;
                            RecordMovimentoNovo.nf_chave_referenciada = NFeReferenciada_ChaveAcesso;
                            RecordMovimentoNovo.nf_protocolo = record_NfeAutorizacao.nProt;
                            RecordMovimentoNovo.nf_digval = record_NfeAutorizacao.digVal;
                            RecordMovimentoNovo.nf_cstat = record_NfeAutorizacao.cStat;
                            RecordMovimentoNovo.nf_cmotivo = record_NfeAutorizacao.xMotivo;
                            RecordMovimentoNovo.desconto_valor = _valorDescontosNF;
                            RecordMovimentoNovo.frete_valor = _valorFreteNF;
                            RecordMovimentoNovo.seguro_valor = _valorSeguroNF;
                            RecordMovimentoNovo.despesas_acessorias_valor = _NotaEntradaValorDespesasNF;
                            RecordMovimentoNovo.valor_total_produtos = valorTotalNF;
                            RecordMovimentoNovo.valor_total_liquido = valorTotalNF;
                            RecordMovimentoNovo.valor_total_bruto = valorTotalNF + _valorFreteNF;
                            RecordMovimentoNovo.informacoes_adicionais = InformacoesAdicionaisNF;
                            RecordMovimentoNovo.id_coligada = 1; // GDI AVIAÇÃO
                            RecordMovimentoNovo.id_filial = RecordFilialDestino.id_filial;
                            RecordMovimentoNovo.id_local_estoque = RecordFilialDestino.id_local_estoque;
                            RecordMovimentoNovo.id_estoque_cd = RecordFilialDestino.id_local_estoque;
                            RecordMovimentoNovo.datahora_cadastro = DataHoraAtual;
                            RecordMovimentoNovo.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                            db.gc_movimentos.Add(RecordMovimentoNovo);
                            db.SaveChanges();
                            foreach (var item in listaItens)
                            {
                                item.id_movimento = RecordMovimentoNovo.id_movimento;
                                item.datahora_cadastro = DataHoraAtual;
                                item.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                                item.id_coligada = RecordMovimentoNovo.id_coligada;
                                item.id_filial = RecordMovimentoNovo.id_filial;
                                db.gc_movimentos_itens.Add(item);
                                qtdItensGravados += 1;
                            }
                            if (qtdItensGravados > 0)
                            {
                                db.SaveChanges();
                                LogAudit = string.Empty;
                                if (record_cstImportacaoNFEntrada.id_movimento_tipo == 5) { LogAudit += "Nova NF Entrada - Compra - Fornecedor - Nacional | "; }
                                else if (record_cstImportacaoNFEntrada.id_movimento_tipo == 6) { LogAudit += "Nova NF Entrada - Compra - Fornecedor - Exterior | "; }
                                else if (record_cstImportacaoNFEntrada.id_movimento_tipo == 9) { LogAudit += "Nova NF Entrada - Devolução | "; };
                                LogAudit += LibDB.CompareDataTable(new Db.gc_movimentos(), RecordMovimentoNovo);
                                LogAudit += "Qtd. Itens: " + qtdItensGravados.ToString();
                                if (LogAudit.EmptyIfNull().ToString().Trim().Length > 0) { LibAudit.SaveAudit(db, true, "gc_movimentos", RecordMovimentoNovo.id_movimento, LogAudit); };
                            }


                            if (record_cstImportacaoNFEntrada.id_movimento_tipo == 6) // "NF Importação"; 
                            {
                                String SqlConsultaNF = "select * from gc_comex_importacoes where nf_despachante_numero like '%|" + _numeroNF + "|%'";
                                gc_comex_importacoes record_gc_comex_importacoes = db.gc_comex_importacoes.SqlQuery(SqlConsultaNF).FirstOrDefault();
                                record_gc_comex_importacoes.id_importacao_status = 4;
                                record_gc_comex_importacoes.datahora_alteracao = DataHoraAtual;
                                record_gc_comex_importacoes.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                db.Entry(record_gc_comex_importacoes).State = EntityState.Modified;
                                db.SaveChanges();
                            }


                            // GED - PDF
                            if ((record_cstImportacaoNFEntrada.filesourcePDF != null) && (record_cstImportacaoNFEntrada.filesourcePDF.FileName.EmptyIfNull().ToString().Length > 0))
                            {
                                int VersaoGedPDF = 0;
                                String DescricaoGedPDF = String.Empty;
                                if (record_cstImportacaoNFEntrada.id_movimento_tipo == 6) { DescricaoGedPDF = "NFe Importação - " + _numeroNF + " - " + ClienteNome + " - (pdf)"; }
                                else if (record_cstImportacaoNFEntrada.id_movimento_tipo == 5) { DescricaoGedPDF = "NFe Entrada - " + _numeroNF + " - " + ClienteNome + " - (pdf)"; }
                                else if (record_cstImportacaoNFEntrada.id_movimento_tipo == 9) { DescricaoGedPDF = "NFe Devolução - " + _numeroNF + " - " + ClienteNome + " - (pdf)"; }

                                IQueryable<ged_arquivos> listaGedPDF = db.ged_arquivos.Where(g => (g.ativo == true) && (g.descricao == DescricaoGedPDF));
                                if (listaGedPDF.Count() > 0)
                                {
                                    foreach (ged_arquivos itemGedPDF in listaGedPDF)
                                    {
                                        if (itemGedPDF.versao > VersaoGedPDF) { VersaoGedPDF = itemGedPDF.versao; };
                                        itemGedPDF.ativo = false;
                                        db.Entry(itemGedPDF).State = EntityState.Modified;
                                    }
                                }
                                // Realizar o upload do PDF para o GED
                                CstUploadGed record_cstUploadGedPDF = new CstUploadGed();
                                record_cstUploadGedPDF.id_arquivo = 0;
                                record_cstUploadGedPDF.id_arquivo_tipo = 14; // Contabilidade > NFe - Entradas
                                record_cstUploadGedPDF.filesource = record_cstImportacaoNFEntrada.filesourcePDF;
                                record_cstUploadGedPDF.id_gc_movimento = RecordMovimentoNovo.id_movimento;
                                record_cstUploadGedPDF.descricao = DescricaoGedPDF;
                                record_cstUploadGedPDF.file_name_new = DescricaoGedPDF.Replace(" - (pdf)","")+".pdf";
                                record_cstUploadGedPDF.observacao = DescricaoGedPDF + ", Processado em " + DataHoraAtual.ToString("dd/MM/yyyy HH:mm") + " por " + CachePersister.userIdentity.Username;
                                record_cstUploadGedPDF.versao = VersaoGedPDF + 1;
                                ged_arquivos record_ged_arquivo = new GedController().ServiceUploadFileGed(record_cstUploadGedPDF);
                                if (record_ged_arquivo != null)
                                {
                                    RecordMovimentoNovo.nf_s3_pdf = record_ged_arquivo.id_arquivo;
                                    RecordMovimentoNovo.datahora_alteracao = DataHoraAtual;
                                    RecordMovimentoNovo.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                    db.Entry(RecordMovimentoNovo).State = EntityState.Modified;
                                }
                                db.SaveChanges();
                            }

                            // GED - XML
                            if ((record_cstImportacaoNFEntrada.filesourceXML != null) && (record_cstImportacaoNFEntrada.filesourceXML.FileName.EmptyIfNull().ToString().Length > 0))
                            {
                                int VersaoGedXML = 0;
                                String DescricaoGedXML = String.Empty;
                                if (record_cstImportacaoNFEntrada.id_movimento_tipo == 6) { DescricaoGedXML = "NFe Importação - " + _numeroNF + " - " + ClienteNome + " - (xml)"; }
                                else if (record_cstImportacaoNFEntrada.id_movimento_tipo == 5) { DescricaoGedXML = "NFe Entrada - " + _numeroNF + " - " + ClienteNome + " - (xml)"; }
                                else if (record_cstImportacaoNFEntrada.id_movimento_tipo == 9) { DescricaoGedXML = "NFe Devolução - " + _numeroNF + " - " + ClienteNome + " - (xml)"; }
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

                                // Realizar o upload do PDF para o GED
                                CstUploadGed record_cstUploadGedPDF = new CstUploadGed();
                                record_cstUploadGedPDF.id_arquivo = 0;
                                record_cstUploadGedPDF.id_arquivo_tipo = 18; // Contabilidade > NFe - Entradas (XML)
                                record_cstUploadGedPDF.filesource = record_cstImportacaoNFEntrada.filesourceXML;
                                record_cstUploadGedPDF.id_gc_movimento = RecordMovimentoNovo.id_movimento;
                                record_cstUploadGedPDF.descricao = DescricaoGedXML;
                                record_cstUploadGedPDF.file_name_new = DescricaoGedXML.Replace(" - (xml)", "") + ".xml";
                                record_cstUploadGedPDF.observacao = DescricaoGedXML + ", Processado em " + DataHoraAtual.ToString("dd/MM/yyyy HH:mm") + " por " + CachePersister.userIdentity.Username;
                                record_cstUploadGedPDF.versao = VersaoGedXML + 1;
                                ged_arquivos record_ged_arquivo = new GedController().ServiceUploadFileGed(record_cstUploadGedPDF);
                                if (record_ged_arquivo != null)
                                {
                                    RecordMovimentoNovo.nf_s3_xml = record_ged_arquivo.id_arquivo;
                                    RecordMovimentoNovo.datahora_alteracao = DataHoraAtual;
                                    RecordMovimentoNovo.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                    db.Entry(RecordMovimentoNovo).State = EntityState.Modified;
                                }
                                db.SaveChanges();
                            }
                        }
                    }
                    if (ErroProcessamento == false)
                    {
                        // Configurar o nome do XML no upload e apagar o arquivo temporário de processamento
                        DateTime DataNF = DataHoraAtual;
                        if (RecordMovimentoNovo.nf_data_geracao != null) { DataNF = RecordMovimentoNovo.nf_data_geracao.GetValueOrDefault(); };
                        String TipoEntrada = String.Empty;
                        if (record_cstImportacaoNFEntrada.id_movimento_tipo == 6) { TipoEntrada = "NFe Importação"; }
                        else if (record_cstImportacaoNFEntrada.id_movimento_tipo == 5) { TipoEntrada = "NFe Entrada Nacional"; }
                        else if (record_cstImportacaoNFEntrada.id_movimento_tipo == 9) { TipoEntrada = "NFe Devolução"; };
                        IdMovTipo = record_cstImportacaoNFEntrada.id_movimento_tipo.EmptyIfNull().ToString();
                        IdMovimento = RecordMovimentoNovo.id_movimento.EmptyIfNull().ToString();
                        try { LibCache.LiberarMemoria(); } catch { };
                        try { System.IO.File.Delete(FileNameXmlUpload); } catch { };
                        try { System.IO.File.Delete(FileNameXmlUploadTemp); } catch { };

                        Processado = true;

                        MsgRetorno += TipoEntrada + " Lida com sucesso!" + LibStringFormat.GetTabHtml(1) + "(Id: " + RecordMovimentoNovo.id_movimento.EmptyIfNull().ToString() + ")" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>";
                        MsgRetorno += qtdItensGravados.ToString() + LibStringFormat.GetTabHtml(1) + "Itens(s) Lidos!" + "<br/><br/>";
                        if (QtdNcmCadastrados > 0) { MsgRetorno += QtdNcmCadastrados.ToString() + LibStringFormat.GetTabHtml(1) + "NCM(s) Cadastrados" + "<br/><br/>"; };
                        if (qtdNomesAtualizados > 0) { MsgRetorno += qtdNomesAtualizados.ToString() + LibStringFormat.GetTabHtml(1) + "Nomes de Produtos(s) Atualizados" + "<br/>"; };
                        if (qtdNCMAtualizados > 0) { MsgRetorno += qtdNCMAtualizados.ToString() + LibStringFormat.GetTabHtml(1) + "NCM de Produtos(s) Atualizados" + "<br/>"; };
                        if (qtdUnidadeMedidaAtualizados > 0) { MsgRetorno += qtdUnidadeMedidaAtualizados.ToString() + LibStringFormat.GetTabHtml(1) + "Unidade de Medida de Produtos(s) Atualizados" + "<br/><br/>"; };

                        MsgRetorno += "<b>ATENÇÃO: Efetue o processamento da "+ TipoEntrada + " na próxima tela!";
                    }
                    else
                    {
                        try { System.IO.File.Delete(FileNameXmlUpload); } catch { };
                        try { System.IO.File.Delete(FileNameXmlUploadTemp); } catch { };
                    }
                }
                catch (DbEntityValidationException ex)
                {
                    Processado = false;
                    MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
                    try { System.IO.File.Delete(FileNameXmlUpload); } catch { };
                    try { System.IO.File.Delete(FileNameXmlUploadTemp); } catch { };
                }
                catch (Exception e)
                {
                    Processado = false;
                    MsgRetorno = LibExceptions.getExceptionShortMessage(e);
                    try { System.IO.File.Delete(FileNameXmlUpload); } catch { };
                    try { System.IO.File.Delete(FileNameXmlUploadTemp); } catch { };
                }
            }
            return Json(new { success = Processado, msg = MsgRetorno, idmov = IdMovimento, idmovtipo = IdMovTipo }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region NFe Entrada Nacional
        public ActionResult ModalImportarNFCompraNacional()
        {
            CstImportacaoNFEntrada record_cstImportacaoNFEntrada = new CstImportacaoNFEntrada();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-shop", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "NFe Entrada Nacional - Upload/Processamento";
            PreencherLookupsModalImportarNacional();
            record_cstImportacaoNFEntrada.id_movimento_tipo = 5;
            return View("ModalNFEntradaImportar", record_cstImportacaoNFEntrada);
        }

        public ActionResult FormProcessarNFCompraNacional(int? id)
        {
            CstMovimentoEntradaNF record_cstMovimentoEntradaNF = new CstMovimentoEntradaNF();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-shop", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "NFe Entrada Nacional - Processamento";
            gc_movimentos RecordMovimentoCompraNacional = db.gc_movimentos.Find(id);

            if ((RecordMovimentoCompraNacional.id_movimento_tipo == 5) && (RecordMovimentoCompraNacional.movimento_faturado == false)) // Compra - Fornecedor - Nacional
            {
                g_clientes record_g_cliente = db.g_clientes.Find(RecordMovimentoCompraNacional.id_cliente);
                record_cstMovimentoEntradaNF.id_movimento = RecordMovimentoCompraNacional.id_movimento;
                record_cstMovimentoEntradaNF.cliente_nome = record_g_cliente.nome.EmptyIfNull().ToString();
                record_cstMovimentoEntradaNF.movimento_nf = RecordMovimentoCompraNacional.nf_numero.EmptyIfNull().ToString() + "-" + RecordMovimentoCompraNacional.nf_serie.EmptyIfNull().ToString();
                record_cstMovimentoEntradaNF.movimento_data = RecordMovimentoCompraNacional.datahora_cadastro.ToString("dd/MM/yy");
                String SentencaSQL = "select mi.id_movimento_item, mi.sequencia, mi.id_produto, p.nome, mi.quantidade, mi.valor_unit, mi.valor_total, mi.produto_externo_codigo, mi.produto_externo_nome " +
                                        " from gc_movimentos_itens mi " +
                                        " left join g_produtos p on (mi.id_produto = p.id_produto) " +
                                        " where mi.id_movimento = " + RecordMovimentoCompraNacional.id_movimento.ToString() +
                                        " order by mi.sequencia";
                DataTable tableItem = LibDB.GetDataTable(SentencaSQL, db);
                List<DataRow> allItens = tableItem.AsEnumerable().ToList();
                foreach (var dsRowItem in allItens)
                {
                    int idMovimentoItem = 0;
                    Decimal sequencia = 0;
                    int idProduto = 0;
                    decimal quantidade = 0;
                    decimal valorUnit = 0;
                    decimal valorTotal = 0;
                    int.TryParse(dsRowItem["id_movimento_item"].EmptyIfNull().ToString(), out idMovimentoItem);
                    decimal.TryParse(dsRowItem["sequencia"].EmptyIfNull().ToString(), out sequencia);
                    int.TryParse(dsRowItem["id_produto"].EmptyIfNull().ToString(), out idProduto);
                    decimal.TryParse(dsRowItem["quantidade"].EmptyIfNull().ToString(), out quantidade);
                    decimal.TryParse(dsRowItem["valor_unit"].EmptyIfNull().ToString(), out valorUnit);
                    decimal.TryParse(dsRowItem["valor_total"].EmptyIfNull().ToString(), out valorTotal);
                    CstMovimentoEntradaNFItem record_cstMovimentoEntradaNFItem = new CstMovimentoEntradaNFItem();
                    record_cstMovimentoEntradaNFItem.id_movimento_item = idMovimentoItem;
                    record_cstMovimentoEntradaNFItem.sequencia = decimal.Truncate(sequencia);
                    record_cstMovimentoEntradaNFItem.id_produto = idProduto;
                    record_cstMovimentoEntradaNFItem.quantidade_geral = decimal.Truncate(quantidade);
                    record_cstMovimentoEntradaNFItem.nome_produto = dsRowItem["nome"].EmptyIfNull().ToString();
                    record_cstMovimentoEntradaNFItem.produto_externo_codigo = dsRowItem["produto_externo_codigo"].EmptyIfNull().ToString();
                    record_cstMovimentoEntradaNFItem.produto_externo_nome = dsRowItem["produto_externo_nome"].EmptyIfNull().ToString();
                    record_cstMovimentoEntradaNFItem.valor_unit = valorUnit;
                    record_cstMovimentoEntradaNFItem.valor_total_formatado = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", valorTotal).Replace("R$ ", "").Replace("R$", "").Replace("$", "");

                    String PNOficial = dsRowItem["produto_externo_codigo"].EmptyIfNull().ToString();
                    String PNAuxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(PNOficial);
                    String PNCuringaOH = PNAuxiliar.EmptyIfNull().ToString().Replace("0", "O");
                    String PNCuringaZERO = PNAuxiliar.EmptyIfNull().ToString().Replace("O", "0");
                    g_produtos RecordProdutoDefault = db.g_produtos.Where(c => (c.codigo == PNOficial || c.codigo_auxiliar == PNAuxiliar || c.codigo_variacao1 == PNCuringaOH || c.codigo_variacao2 == PNCuringaZERO)).FirstOrDefault();
                    if (RecordProdutoDefault != null) { record_cstMovimentoEntradaNFItem.id_produto = RecordProdutoDefault.id_produto; };

                    record_cstMovimentoEntradaNF.allItens.Add(record_cstMovimentoEntradaNFItem);
                }
                PreencherLookupsComboProdutosEntradaNacional();
            }
            else
            {
                if (RecordMovimentoCompraNacional.id_movimento_tipo != 5)
                {
                    record_cstMovimentoEntradaNF.movimento_permitido = false;
                    record_cstMovimentoEntradaNF.msg_erro = "Movimento selecionado não é do tipo [1.1.1 - Entrada - Fornecedor - Nacional]!";
                }
                else if (RecordMovimentoCompraNacional.entrada_nfe_processada == true)
                {
                    record_cstMovimentoEntradaNF.movimento_permitido = false;
                    record_cstMovimentoEntradaNF.msg_erro = "Movimento já foi Processado anteriormente!";
                }
                ViewBag.comboProdutos = new List<SelectListItem>();
            }
            return View(record_cstMovimentoEntradaNF);
        }

        [HttpPost]
        public ActionResult AjaxProcessarNFCompraNacional(CstMovimentoEntradaNF view_cstMovimentoEntradaNF)
        {
            bool Sucesso = false;
            int QtdErros = 0;
            int QtdItens = 0;
            String MsgRetorno = "";
            String LogAudit = "";
            gc_movimentos RecordMovimentoCompraNacional = db.gc_movimentos.Find(view_cstMovimentoEntradaNF.id_movimento);

            try
            {
                if (ModelState.IsValid)
                {
                    foreach (CstMovimentoEntradaNFItem Item in view_cstMovimentoEntradaNF.allItens)
                    {
                        if (Item.id_produto <= 0)
                        {
                            QtdErros += 1;
                            MsgRetorno += " - Produto ERP relacionado ao item [" + Item.produto_externo_codigo + " - " + Item.produto_externo_nome + "] é de preenchimento obrigatório!<br/><br/>";
                        }
                        else
                        {
                            g_produtos RecordProduto = db.g_produtos.Find(Item.id_produto);
                            if (RecordProduto.importado == false)
                            {
                                QtdErros += 1;
                                MsgRetorno += " - Não é possível relacionar o item [" + Item.produto_externo_codigo + "] a um produto temporário!<br/><br/>";
                            }
                        }
                    }
                }
                else
                {
                    MsgRetorno = String.Join(Environment.NewLine, ModelState.Values.SelectMany(v => v.Errors).Select(v => v.ErrorMessage + " " + v.Exception + "<br/>"));
                    QtdErros += 1;
                }

                if (QtdErros == 0)
                {
                    DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();

                    // Movimento Origem
                    RecordMovimentoCompraNacional.entrada_nfe_processada = true;
                    RecordMovimentoCompraNacional.movimento_faturado = true;
                    RecordMovimentoCompraNacional.id_movimento_tipo = 10; // Entrada - Fornecedor - Nacional - Processada
                    RecordMovimentoCompraNacional.datahora_faturamento = DataHoraAtual;
                    RecordMovimentoCompraNacional.id_usuario_faturamento = CachePersister.userIdentity.IdUsuario;
                    RecordMovimentoCompraNacional.datahora_alteracao = DataHoraAtual;
                    RecordMovimentoCompraNacional.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                    db.Entry(RecordMovimentoCompraNacional).State = EntityState.Modified;
                    db.SaveChanges();

                    // Itens dos Movimentos
                    gc_movimentos OldRecordMovimentoCompraNacional = LibDB.CloneTObject(RecordMovimentoCompraNacional);
                    foreach (CstMovimentoEntradaNFItem Item in view_cstMovimentoEntradaNF.allItens)
                    {
                        if (Item.id_produto > 0)
                        {
                            QtdItens += 1;
                            gc_movimentos_itens RecordMovimentoCompraNacionalItem = db.gc_movimentos_itens.Find(Item.id_movimento_item);
                            RecordMovimentoCompraNacionalItem.id_produto = Item.id_produto;
                            RecordMovimentoCompraNacionalItem.datahora_alteracao = DataHoraAtual;
                            RecordMovimentoCompraNacionalItem.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                            db.Entry(RecordMovimentoCompraNacionalItem).State = EntityState.Modified;

                            LogAudit = "Vinculação dados externos ao produto (Código:" + Item.produto_externo_codigo.EmptyIfNull() + " Descrição: "+ Item.produto_externo_nome.EmptyIfNull() + ") ao produto | processamento NFe Entrada Nacional";
                            if (LogAudit.EmptyIfNull().ToString().Trim().Length > 0) { LibAudit.SaveAudit(db, true, "g_produtos", RecordMovimentoCompraNacionalItem.id_produto, LogAudit); };
                        }
                    }

                    db.SaveChanges();
                    Sucesso = true;
                    
                    LogAudit = "NFe Entrada Nacional Processada com Sucesso | ";
                    LogAudit += LibDB.CompareDataTable(OldRecordMovimentoCompraNacional, RecordMovimentoCompraNacional);
                    LogAudit += "Qtd. Itens: " + QtdItens.ToString();
                    if (LogAudit.EmptyIfNull().ToString().Trim().Length > 0) { LibAudit.SaveAudit(db, true, "gc_movimentos", RecordMovimentoCompraNacional.id_movimento, LogAudit); };

                    MsgRetorno += "NF Entrada Nacional - Processada com Sucesso!" + LibStringFormat.GetTabHtml(1) + "(Id: " + RecordMovimentoCompraNacional.id_movimento.EmptyIfNull().ToString() + ")" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>";
                    MsgRetorno += "<b>ATENÇÃO: Solicite ao setor de Estoque que efetue o recebimento/conferência física do material e registre no ERP!<b>";
                }
            }
            catch (DbEntityValidationException ex)
            {
                QtdErros = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                QtdErros = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            return Json(new { success = Sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region NFe Devolução
        public ActionResult ModalImportarNFDevolucao()
        {
            CstImportacaoNFEntrada record_cstImportacaoNFEntrada = new CstImportacaoNFEntrada();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-arrow-rotate-left", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "NFe Devolução - Upload/Processamento";
            PreencherLookupsModalImportarDevolucao();
            record_cstImportacaoNFEntrada.id_movimento_tipo = 9;
            return View("ModalNFEntradaImportar", record_cstImportacaoNFEntrada);
        }

        public ActionResult FormProcessarNFDevolucao(int? id)
        {
            CstMovimentoEntradaNF record_cstMovimentoEntradaNF = new CstMovimentoEntradaNF();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-shop", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "NFe Devolução - Upload/Processamento";
            gc_movimentos RecordMovimentoDevolucao = db.gc_movimentos.Find(id);

            if ((RecordMovimentoDevolucao.id_movimento_tipo == 9) && (RecordMovimentoDevolucao.movimento_faturado == false)) // Devolução
            {
                try
                {
                    g_clientes record_g_cliente = db.g_clientes.Find(RecordMovimentoDevolucao.id_cliente);
                    record_cstMovimentoEntradaNF.id_movimento = RecordMovimentoDevolucao.id_movimento;
                    record_cstMovimentoEntradaNF.cliente_nome = record_g_cliente.nome.EmptyIfNull().ToString();
                    record_cstMovimentoEntradaNF.movimento_nf = RecordMovimentoDevolucao.nf_numero.EmptyIfNull().ToString() + "-" + RecordMovimentoDevolucao.nf_serie.EmptyIfNull().ToString();
                    record_cstMovimentoEntradaNF.movimento_data = RecordMovimentoDevolucao.datahora_cadastro.ToString("dd/MM/yy");
                    String SentencaSQL = "select mi.id_movimento_item, mi.sequencia, mi.id_produto, p.nome, mi.quantidade, mi.valor_unit, mi.valor_total, mi.produto_externo_codigo, mi.produto_externo_nome " +
                                            " from gc_movimentos_itens mi " +
                                            " left join g_produtos p on (mi.id_produto = p.id_produto) " +
                                            " where mi.id_movimento = " + RecordMovimentoDevolucao.id_movimento.ToString() +
                                            " order by mi.sequencia";
                    DataTable tableItem = LibDB.GetDataTable(SentencaSQL, db);
                    List<DataRow> allItens = tableItem.AsEnumerable().ToList();
                    foreach (var dsRowItem in allItens)
                    {
                        int idMovimentoItem = 0;
                        Decimal sequencia = 0;
                        int idProduto = 0;
                        decimal quantidade = 0;
                        decimal valorUnit = 0;
                        decimal valorTotal = 0;
                        int.TryParse(dsRowItem["id_movimento_item"].EmptyIfNull().ToString(), out idMovimentoItem);
                        decimal.TryParse(dsRowItem["sequencia"].EmptyIfNull().ToString(), out sequencia);
                        int.TryParse(dsRowItem["id_produto"].EmptyIfNull().ToString(), out idProduto);
                        decimal.TryParse(dsRowItem["quantidade"].EmptyIfNull().ToString(), out quantidade);
                        decimal.TryParse(dsRowItem["valor_unit"].EmptyIfNull().ToString(), out valorUnit);
                        decimal.TryParse(dsRowItem["valor_total"].EmptyIfNull().ToString(), out valorTotal);
                        CstMovimentoEntradaNFItem record_cstMovimentoEntradaNFItem = new CstMovimentoEntradaNFItem();
                        record_cstMovimentoEntradaNFItem.id_movimento_item = idMovimentoItem;
                        record_cstMovimentoEntradaNFItem.sequencia = decimal.Truncate(sequencia);
                        record_cstMovimentoEntradaNFItem.id_produto = idProduto;
                        record_cstMovimentoEntradaNFItem.quantidade_geral = decimal.Truncate(quantidade);
                        record_cstMovimentoEntradaNFItem.nome_produto = dsRowItem["nome"].EmptyIfNull().ToString();
                        record_cstMovimentoEntradaNFItem.produto_externo_codigo = dsRowItem["produto_externo_codigo"].EmptyIfNull().ToString();
                        record_cstMovimentoEntradaNFItem.produto_externo_nome = dsRowItem["produto_externo_nome"].EmptyIfNull().ToString();
                        record_cstMovimentoEntradaNFItem.valor_unit = valorUnit;
                        record_cstMovimentoEntradaNFItem.valor_total_formatado = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", valorTotal).Replace("R$ ", "").Replace("R$", "").Replace("$", "");
                        record_cstMovimentoEntradaNF.allItens.Add(record_cstMovimentoEntradaNFItem);
                    }
                    PreencherLookupsComboProdutosDevolucao(RecordMovimentoDevolucao.id_movimento_ref);
                    if (RecordMovimentoDevolucao.nf_chave_referenciada.EmptyIfNull().ToString().Length > 0)
                    {
                        gc_movimentos_nf record_gc_movimentos_nf = db.gc_movimentos_nf.Where(m => m.nf_chave_acesso == RecordMovimentoDevolucao.nf_chave_referenciada).FirstOrDefault();
                        if (record_gc_movimentos_nf != null) { record_cstMovimentoEntradaNF.url_danfe = record_gc_movimentos_nf.nf_url_pdf; };
                    }
                }
                finally { }
            }
            else
            {
                if (RecordMovimentoDevolucao.id_movimento_tipo != 9)
                {
                    record_cstMovimentoEntradaNF.movimento_permitido = false;
                    record_cstMovimentoEntradaNF.msg_erro = "Movimento selecionado não é do tipo [1.1.4 - Devolução]";
                }
                else if (RecordMovimentoDevolucao.entrada_nfe_processada == true)
                {
                    record_cstMovimentoEntradaNF.movimento_permitido = false;
                    record_cstMovimentoEntradaNF.msg_erro = "Movimento já foi Processado anteriormente!";
                }
                ViewBag.comboProdutos = new List<SelectListItem>();
            }
            return View(record_cstMovimentoEntradaNF);
        }


        [HttpPost]
        public ActionResult AjaxProcessarNFDevolucao(CstMovimentoEntradaNF view_cstMovimentoEntradaNF)
        {
            bool Sucesso = false;
            int QtdErros = 0;
            int QtdItens = 0;
            String MsgRetorno = "";
            String LogAudit = "";
            try
            {
                if (ModelState.IsValid)
                {
                    foreach (CstMovimentoEntradaNFItem Item in view_cstMovimentoEntradaNF.allItens)
                    {
                        if (Item.id_produto <= 0)
                        {
                            QtdErros += 1;
                            MsgRetorno += " - Produto ERP relacionado ao item [" + Item.produto_externo_codigo + " - " + Item.produto_externo_nome + "] é de preenchimento obrigatório!<br/><br/>";
                        }
                        else
                        {
                            g_produtos RecordProduto = db.g_produtos.Find(Item.id_produto);
                            if (RecordProduto.importado == false)
                            {
                                QtdErros += 1;
                                MsgRetorno += " - Não é possível relacionar o item [" + Item.produto_externo_codigo + "] a um produto temporário!<br/><br/>";
                            }
                        }
                    }
                }
                else
                {
                    MsgRetorno = String.Join(Environment.NewLine, ModelState.Values.SelectMany(v => v.Errors).Select(v => v.ErrorMessage + " " + v.Exception + "<br/>"));
                    QtdErros += 1;
                }

                if (QtdErros == 0)
                {
                    DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();

                    // Movimento Origem
                    gc_movimentos RecordMovimentoDevolucao = db.gc_movimentos.Find(view_cstMovimentoEntradaNF.id_movimento);
                    RecordMovimentoDevolucao.entrada_nfe_processada = true;
                    RecordMovimentoDevolucao.id_movimento_tipo = 11;
                    RecordMovimentoDevolucao.movimento_faturado = true;
                    RecordMovimentoDevolucao.datahora_faturamento = DataHoraAtual;
                    RecordMovimentoDevolucao.id_usuario_faturamento = CachePersister.userIdentity.IdUsuario;
                    RecordMovimentoDevolucao.datahora_alteracao = DataHoraAtual;
                    RecordMovimentoDevolucao.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                    db.Entry(RecordMovimentoDevolucao).State = EntityState.Modified;

                    gc_movimentos OldRecordMovimentoDevolucao = LibDB.CloneTObject(RecordMovimentoDevolucao);
                    foreach (CstMovimentoEntradaNFItem Item in view_cstMovimentoEntradaNF.allItens)
                    {
                        if (Item.id_produto > 0)
                        {
                            QtdItens += 1;
                            gc_movimentos_itens origem_gc_movimentos_itens = db.gc_movimentos_itens.Find(Item.id_movimento_item);
                            origem_gc_movimentos_itens.id_produto = Item.id_produto;
                            origem_gc_movimentos_itens.datahora_alteracao = DataHoraAtual;
                            origem_gc_movimentos_itens.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                            db.Entry(origem_gc_movimentos_itens).State = EntityState.Modified;

                            LogAudit = "Vinculação dados externos ao produto (Código:" + Item.produto_externo_codigo.EmptyIfNull() + " Descrição: " + Item.produto_externo_nome.EmptyIfNull() + ") ao produto | processamento NFe Entrada Nacional";
                            if (LogAudit.EmptyIfNull().ToString().Trim().Length > 0) { LibAudit.SaveAudit(db, true, "g_produtos", origem_gc_movimentos_itens.id_produto, LogAudit); };
                        }
                    }
                    db.SaveChanges();
                    Sucesso = true;
                    LogAudit = "NFe Devolução Processada com Sucesso | ";
                    LogAudit += LibDB.CompareDataTable(OldRecordMovimentoDevolucao, RecordMovimentoDevolucao);
                    LogAudit += "Qtd. Itens: " + QtdItens.ToString();
                    if (LogAudit.EmptyIfNull().ToString().Trim().Length > 0) { LibAudit.SaveAudit(db, true, "gc_movimentos", RecordMovimentoDevolucao.id_movimento, LogAudit); };

                    Sucesso = true;
                    MsgRetorno += "NFe Devolução - Processada com Sucesso!" + LibStringFormat.GetTabHtml(1) + "(Id: " + RecordMovimentoDevolucao.id_movimento.EmptyIfNull().ToString() + ")" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>";
                    MsgRetorno += "<b>ATENÇÃO: Solicite ao setor de Estoque que efetue o recebimento/conferência física do material e registre no ERP!<b>";
                }
            }
            catch (DbEntityValidationException ex)
            {
                QtdErros = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                QtdErros = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            return Json(new { success = Sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region NFe Entrada Exterior - Upload e Processamento
        public ActionResult ModalImportarNFImportacao()
        {
            CstImportacaoNFEntrada record_cstImportacaoNFEntrada = new CstImportacaoNFEntrada();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-passport", "", "#B7950B", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Importação - Upload (XML e Danfe)";
            PreencherLookupsModalImportarImportacao();
            record_cstImportacaoNFEntrada.id_movimento_tipo = 6;
            return View("ModalNFEntradaImportar", record_cstImportacaoNFEntrada);
        }

        public ActionResult FormProcessarNFImportacao(int? id)
        {
            ViewBag.Title = "Processar NF Importação";
            gc_movimentos record_gc_movimento = db.gc_movimentos.Find(id);
            g_clientes record_g_cliente = db.g_clientes.Find(record_gc_movimento.id_cliente);
            CstMovimentoEntradaNF record_cstMovimentoEntradaNF = new CstMovimentoEntradaNF();
            if (record_gc_movimento.entrada_nfe_processada == true)
            {
                record_cstMovimentoEntradaNF.movimento_permitido = false;
                record_cstMovimentoEntradaNF.msg_erro = "Processamento já realizado, Não é permitido novo processamento!";
            }
            else
            {
                record_cstMovimentoEntradaNF.id_movimento = record_gc_movimento.id_movimento;
                record_cstMovimentoEntradaNF.cliente_nome = record_g_cliente.nome.EmptyIfNull().ToString();
                record_cstMovimentoEntradaNF.movimento_nf = record_gc_movimento.nf_numero.EmptyIfNull().ToString() + "-" + record_gc_movimento.nf_serie.EmptyIfNull().ToString();
                record_cstMovimentoEntradaNF.movimento_data = record_gc_movimento.datahora_cadastro.ToString("dd/MM/yy");
                String SentencaSQL = "select mi.id_movimento_item, mi.sequencia, mi.id_produto, p.nome, mi.quantidade, mi.valor_unit, mi.valor_total " +
                                        " from gc_movimentos_itens mi " +
                                        " left join g_produtos p on (mi.id_produto = p.id_produto) " +
                                        " where mi.id_movimento = " + record_gc_movimento.id_movimento.ToString() +
                                        " order by mi.sequencia";
                DataTable tableItem = LibDB.GetDataTable(SentencaSQL, db);
                List<DataRow> allItens = tableItem.AsEnumerable().ToList();
                foreach (var dsRowItem in allItens)
                {
                    int idMovimentoItem = 0;
                    Decimal sequencia = 0;
                    int idProduto = 0;
                    decimal quantidade = 0;
                    decimal valorUnit = 0;
                    decimal valorTotal = 0;
                    int.TryParse(dsRowItem["id_movimento_item"].EmptyIfNull().ToString(), out idMovimentoItem);
                    decimal.TryParse(dsRowItem["sequencia"].EmptyIfNull().ToString(), out sequencia);
                    int.TryParse(dsRowItem["id_produto"].EmptyIfNull().ToString(), out idProduto);
                    decimal.TryParse(dsRowItem["quantidade"].EmptyIfNull().ToString(), out quantidade);
                    decimal.TryParse(dsRowItem["valor_unit"].EmptyIfNull().ToString(), out valorUnit);
                    decimal.TryParse(dsRowItem["valor_total"].EmptyIfNull().ToString(), out valorTotal);
                    CstMovimentoEntradaNFItem record_cstMovimentoEntradaNFItem = new CstMovimentoEntradaNFItem();
                    record_cstMovimentoEntradaNFItem.id_movimento_item = idMovimentoItem;
                    record_cstMovimentoEntradaNFItem.sequencia = decimal.Truncate(sequencia);
                    record_cstMovimentoEntradaNFItem.id_produto = idProduto;
                    record_cstMovimentoEntradaNFItem.quantidade_geral = decimal.Truncate(quantidade);
                    record_cstMovimentoEntradaNFItem.nome_produto = dsRowItem["nome"].EmptyIfNull().ToString();
                    record_cstMovimentoEntradaNFItem.valor_unit = valorUnit;
                    record_cstMovimentoEntradaNFItem.valor_unit = valorTotal;
                    record_cstMovimentoEntradaNFItem.valor_total_formatado = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", valorTotal).Replace("R$ ", "").Replace("R$", "").Replace("$", "");
                    record_cstMovimentoEntradaNFItem.qtd_cdbh_01 = quantidade;
                    record_cstMovimentoEntradaNF.allItens.Add(record_cstMovimentoEntradaNFItem);
                }
            }
            return View(record_cstMovimentoEntradaNF);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AjaxProcessarNFImportacao(CstMovimentoEntradaNF record_cstMovimentoEntradaNF)
        {
            bool Sucesso = false;
            String MsgRetorno = String.Empty;
            String msgErroTipo1 = String.Empty; // Quantidades informadas erradas na sequencia
            String msgErroTipo2 = String.Empty; // Total geral da sequencia nao bate
            String resultadoProcessamento = String.Empty;
            String idProcessamentoGravado = "0";
            gc_movimentos RecordMovimentoOrigem;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            try
            {
                msgErroTipo1 = string.Empty;
                msgErroTipo2 = string.Empty;
                foreach (var RecordItem in record_cstMovimentoEntradaNF.allItens)
                {
                    Decimal QtdTotalItem = 0;
                    try { QtdTotalItem += RecordItem.qtd_cdbh_01; } catch { msgErroTipo1 += RecordItem.sequencia.ToString() + "; "; };
                    try { QtdTotalItem += RecordItem.qtd_cdbh_02; } catch { msgErroTipo1 += RecordItem.sequencia.ToString() + "; "; };
                    try { QtdTotalItem += RecordItem.qtd_cdbh_03; } catch { msgErroTipo1 += RecordItem.sequencia.ToString() + "; "; };
                    try { QtdTotalItem += RecordItem.qtd_cdsp_01; } catch { msgErroTipo1 += RecordItem.sequencia.ToString() + "; "; };
                    try { QtdTotalItem += RecordItem.qtd_cdsp_02; } catch { msgErroTipo1 += RecordItem.sequencia.ToString() + "; "; };
                    try { QtdTotalItem += RecordItem.qtd_cdsp_03; } catch { msgErroTipo1 += RecordItem.sequencia.ToString() + "; "; };

                    if ((QtdTotalItem > RecordItem.quantidade_geral) || (QtdTotalItem < RecordItem.quantidade_geral)) { msgErroTipo2 += RecordItem.sequencia.ToString() + "; "; };
                }

                if ((msgErroTipo1.EmptyIfNull().ToString().Trim().Length + msgErroTipo2.EmptyIfNull().ToString().Trim().Length) == 0)
                {
                    RecordMovimentoOrigem = db.gc_movimentos.Find(record_cstMovimentoEntradaNF.id_movimento);
                    IQueryable<gc_movimentos_itens> allRecordsItensOrigem = db.gc_movimentos_itens.Where(i => i.id_movimento == RecordMovimentoOrigem.id_movimento).OrderBy(i => i.id_movimento_item);
                    if (RecordMovimentoOrigem.id_movimento_tipo == 6)
                    {
                        #region Compra Fornecedor Internacional - Exterior
                        List<gc_movimentos_itens> ListaItensCDBH01 = new List<gc_movimentos_itens>();
                        List<gc_movimentos_itens> ListaItensCDBH02 = new List<gc_movimentos_itens>();
                        List<gc_movimentos_itens> ListaItensCDBH03 = new List<gc_movimentos_itens>();
                        List<gc_movimentos_itens> ListaItensCDSP01 = new List<gc_movimentos_itens>();
                        List<gc_movimentos_itens> ListaItensCDSP02 = new List<gc_movimentos_itens>();
                        List<gc_movimentos_itens> ListaItensCDSP03 = new List<gc_movimentos_itens>();
                        foreach (var RecordItemView in record_cstMovimentoEntradaNF.allItens)
                        {
                            gc_movimentos_itens RecordItemOrigem = allRecordsItensOrigem.Where(i => i.id_movimento_item == RecordItemView.id_movimento_item).FirstOrDefault();
                            Decimal ItemQtdOriginal = RecordItemOrigem.quantidade;
                            Decimal ItemValorUnitOriginal = RecordItemOrigem.valor_unit;
                            Decimal ItemValorTotalOriginal = RecordItemOrigem.valor_total;
                            Decimal ItemValorDespesasOriginal = RecordItemOrigem.valor_despesas;
                            Decimal ItemIcmsValorBaseCalculoOriginal = RecordItemOrigem.icms_vbc;
                            Decimal ItemIcmsValorOriginal = RecordItemOrigem.icms_vicms;
                            Decimal ItemIpiValorBaseCalculoOriginal = RecordItemOrigem.ipi_vbc;
                            Decimal ItemIpiValorOriginal = RecordItemOrigem.ipi_vipi;
                            Decimal ItemPisValorBaseCalculoOriginal = RecordItemOrigem.pis_vbc;
                            Decimal ItemPisValorOriginal = RecordItemOrigem.pis_vpis;
                            Decimal ItemCofinsValorBaseCalculoOriginal = RecordItemOrigem.cofins_vbc;
                            Decimal ItemCofinsValorOriginal = RecordItemOrigem.cofins_vcofins;
                            Decimal ItemIIValorBaseCalculoOriginal = RecordItemOrigem.ii_vbc;
                            Decimal ItemIIValorOriginal = RecordItemOrigem.ii_vii;

                            if (RecordItemView.qtd_cdbh_01 > 0)
                            {
                                gc_movimentos_itens RecordItemCdBh01 = LibDB.CloneTObject(RecordItemOrigem);
                                RecordItemCdBh01.quantidade = RecordItemView.qtd_cdbh_01;
                                if (RecordItemView.qtd_cdbh_01 < ItemQtdOriginal)
                                {
                                    RecordItemCdBh01.valor_total = (ItemValorTotalOriginal / ItemQtdOriginal) * RecordItemCdBh01.quantidade;
                                    if (ItemValorDespesasOriginal > 0) { RecordItemCdBh01.valor_despesas = (ItemValorDespesasOriginal / ItemQtdOriginal) * RecordItemCdBh01.quantidade; };
                                    if (ItemIcmsValorBaseCalculoOriginal > 0) { RecordItemCdBh01.icms_vbc = (ItemIcmsValorBaseCalculoOriginal / ItemQtdOriginal) * RecordItemCdBh01.quantidade; };
                                    if (ItemIcmsValorOriginal > 0) { RecordItemCdBh01.icms_vicms = (ItemIcmsValorOriginal / ItemQtdOriginal) * RecordItemCdBh01.quantidade; };
                                    if (ItemIpiValorBaseCalculoOriginal > 0) { RecordItemCdBh01.ipi_vbc = (ItemIpiValorBaseCalculoOriginal / ItemQtdOriginal) * RecordItemCdBh01.quantidade; };
                                    if (ItemIpiValorOriginal > 0) { RecordItemCdBh01.ipi_vipi = (ItemIpiValorOriginal / ItemQtdOriginal) * RecordItemCdBh01.quantidade; };
                                    if (ItemPisValorBaseCalculoOriginal > 0) { RecordItemCdBh01.pis_vbc = (ItemPisValorBaseCalculoOriginal / ItemQtdOriginal) * RecordItemCdBh01.quantidade; };
                                    if (ItemPisValorOriginal > 0) { RecordItemCdBh01.pis_vpis = (ItemPisValorOriginal / ItemQtdOriginal) * RecordItemCdBh01.quantidade; };
                                    if (ItemCofinsValorBaseCalculoOriginal > 0) { RecordItemCdBh01.cofins_vbc = (ItemCofinsValorBaseCalculoOriginal / ItemQtdOriginal) * RecordItemCdBh01.quantidade; };
                                    if (ItemCofinsValorOriginal > 0) { RecordItemCdBh01.cofins_vcofins = (ItemCofinsValorOriginal / ItemQtdOriginal) * RecordItemCdBh01.quantidade; };
                                    if (ItemIIValorBaseCalculoOriginal > 0) { RecordItemCdBh01.ii_vbc = (ItemIIValorBaseCalculoOriginal / ItemQtdOriginal) * RecordItemCdBh01.quantidade; };
                                    if (ItemIIValorOriginal > 0) { RecordItemCdBh01.ii_vii = (ItemIIValorOriginal / ItemQtdOriginal) * RecordItemCdBh01.quantidade; };
                                }
                                ListaItensCDBH01.Add(RecordItemCdBh01);
                            }
                            if (RecordItemView.qtd_cdbh_02 > 0)
                            {
                                gc_movimentos_itens RecordItemCdBh02 = LibDB.CloneTObject(RecordItemOrigem);
                                RecordItemCdBh02.quantidade = RecordItemView.qtd_cdbh_02;
                                if (RecordItemView.qtd_cdbh_02 < ItemQtdOriginal)
                                {
                                    RecordItemCdBh02.valor_total = (ItemValorTotalOriginal / ItemQtdOriginal) * RecordItemCdBh02.quantidade;
                                    if (ItemValorDespesasOriginal > 0) { RecordItemCdBh02.valor_despesas = (ItemValorDespesasOriginal / ItemQtdOriginal) * RecordItemCdBh02.quantidade; };
                                    if (ItemIcmsValorBaseCalculoOriginal > 0) { RecordItemCdBh02.icms_vbc = (ItemIcmsValorBaseCalculoOriginal / ItemQtdOriginal) * RecordItemCdBh02.quantidade; };
                                    if (ItemIcmsValorOriginal > 0) { RecordItemCdBh02.icms_vicms = (ItemIcmsValorOriginal / ItemQtdOriginal) * RecordItemCdBh02.quantidade; };
                                    if (ItemIpiValorBaseCalculoOriginal > 0) { RecordItemCdBh02.ipi_vbc = (ItemIpiValorBaseCalculoOriginal / ItemQtdOriginal) * RecordItemCdBh02.quantidade; };
                                    if (ItemIpiValorOriginal > 0) { RecordItemCdBh02.ipi_vipi = (ItemIpiValorOriginal / ItemQtdOriginal) * RecordItemCdBh02.quantidade; };
                                    if (ItemPisValorBaseCalculoOriginal > 0) { RecordItemCdBh02.pis_vbc = (ItemPisValorBaseCalculoOriginal / ItemQtdOriginal) * RecordItemCdBh02.quantidade; };
                                    if (ItemPisValorOriginal > 0) { RecordItemCdBh02.pis_vpis = (ItemPisValorOriginal / ItemQtdOriginal) * RecordItemCdBh02.quantidade; };
                                    if (ItemCofinsValorBaseCalculoOriginal > 0) { RecordItemCdBh02.cofins_vbc = (ItemCofinsValorBaseCalculoOriginal / ItemQtdOriginal) * RecordItemCdBh02.quantidade; };
                                    if (ItemCofinsValorOriginal > 0) { RecordItemCdBh02.cofins_vcofins = (ItemCofinsValorOriginal / ItemQtdOriginal) * RecordItemCdBh02.quantidade; };
                                    if (ItemIIValorBaseCalculoOriginal > 0) { RecordItemCdBh02.ii_vbc = (ItemIIValorBaseCalculoOriginal / ItemQtdOriginal) * RecordItemCdBh02.quantidade; };
                                    if (ItemIIValorOriginal > 0) { RecordItemCdBh02.ii_vii = (ItemIIValorOriginal / ItemQtdOriginal) * RecordItemCdBh02.quantidade; };
                                }
                                ListaItensCDBH02.Add(RecordItemCdBh02);
                            }
                            if (RecordItemView.qtd_cdbh_03 > 0)
                            {
                                gc_movimentos_itens RecordItemCdBh03 = LibDB.CloneTObject(RecordItemOrigem);
                                RecordItemCdBh03.id_movimento_item = 0;
                                RecordItemCdBh03.quantidade = RecordItemView.qtd_cdbh_03;
                                if (RecordItemView.qtd_cdbh_03 < ItemQtdOriginal)
                                {
                                    RecordItemCdBh03.valor_total = (ItemValorTotalOriginal / ItemQtdOriginal) * RecordItemCdBh03.quantidade;
                                    if (ItemValorDespesasOriginal > 0) { RecordItemCdBh03.valor_despesas = (ItemValorDespesasOriginal / ItemQtdOriginal) * RecordItemCdBh03.quantidade; };
                                    if (ItemIcmsValorBaseCalculoOriginal > 0) { RecordItemCdBh03.icms_vbc = (ItemIcmsValorBaseCalculoOriginal / ItemQtdOriginal) * RecordItemCdBh03.quantidade; };
                                    if (ItemIcmsValorOriginal > 0) { RecordItemCdBh03.icms_vicms = (ItemIcmsValorOriginal / ItemQtdOriginal) * RecordItemCdBh03.quantidade; };
                                    if (ItemIpiValorBaseCalculoOriginal > 0) { RecordItemCdBh03.ipi_vbc = (ItemIpiValorBaseCalculoOriginal / ItemQtdOriginal) * RecordItemCdBh03.quantidade; };
                                    if (ItemIpiValorOriginal > 0) { RecordItemCdBh03.ipi_vipi = (ItemIpiValorOriginal / ItemQtdOriginal) * RecordItemCdBh03.quantidade; };
                                    if (ItemPisValorBaseCalculoOriginal > 0) { RecordItemCdBh03.pis_vbc = (ItemPisValorBaseCalculoOriginal / ItemQtdOriginal) * RecordItemCdBh03.quantidade; };
                                    if (ItemPisValorOriginal > 0) { RecordItemCdBh03.pis_vpis = (ItemPisValorOriginal / ItemQtdOriginal) * RecordItemCdBh03.quantidade; };
                                    if (ItemCofinsValorBaseCalculoOriginal > 0) { RecordItemCdBh03.cofins_vbc = (ItemCofinsValorBaseCalculoOriginal / ItemQtdOriginal) * RecordItemCdBh03.quantidade; };
                                    if (ItemCofinsValorOriginal > 0) { RecordItemCdBh03.cofins_vcofins = (ItemCofinsValorOriginal / ItemQtdOriginal) * RecordItemCdBh03.quantidade; };
                                    if (ItemIIValorBaseCalculoOriginal > 0) { RecordItemCdBh03.ii_vbc = (ItemIIValorBaseCalculoOriginal / ItemQtdOriginal) * RecordItemCdBh03.quantidade; };
                                    if (ItemIIValorOriginal > 0) { RecordItemCdBh03.ii_vii = (ItemIIValorOriginal / ItemQtdOriginal) * RecordItemCdBh03.quantidade; };
                                }
                                ListaItensCDBH03.Add(RecordItemCdBh03);
                            }
                            if (RecordItemView.qtd_cdsp_01 > 0)
                            {
                                gc_movimentos_itens RecordItemCdSp01 = LibDB.CloneTObject(RecordItemOrigem);
                                RecordItemCdSp01.quantidade = RecordItemView.qtd_cdsp_01;
                                if (RecordItemView.qtd_cdsp_01 < ItemQtdOriginal)
                                {
                                    RecordItemCdSp01.valor_total = (ItemValorTotalOriginal / ItemQtdOriginal) * RecordItemCdSp01.quantidade;
                                    if (ItemValorDespesasOriginal > 0) { RecordItemCdSp01.valor_despesas = (ItemValorDespesasOriginal / ItemQtdOriginal) * RecordItemCdSp01.quantidade; };
                                    if (ItemIcmsValorBaseCalculoOriginal > 0) { RecordItemCdSp01.icms_vbc = (ItemIcmsValorBaseCalculoOriginal / ItemQtdOriginal) * RecordItemCdSp01.quantidade; };
                                    if (ItemIcmsValorOriginal > 0) { RecordItemCdSp01.icms_vicms = (ItemIcmsValorOriginal / ItemQtdOriginal) * RecordItemCdSp01.quantidade; };
                                    if (ItemIpiValorBaseCalculoOriginal > 0) { RecordItemCdSp01.ipi_vbc = (ItemIpiValorBaseCalculoOriginal / ItemQtdOriginal) * RecordItemCdSp01.quantidade; };
                                    if (ItemIpiValorOriginal > 0) { RecordItemCdSp01.ipi_vipi = (ItemIpiValorOriginal / ItemQtdOriginal) * RecordItemCdSp01.quantidade; };
                                    if (ItemPisValorBaseCalculoOriginal > 0) { RecordItemCdSp01.pis_vbc = (ItemPisValorBaseCalculoOriginal / ItemQtdOriginal) * RecordItemCdSp01.quantidade; };
                                    if (ItemPisValorOriginal > 0) { RecordItemCdSp01.pis_vpis = (ItemPisValorOriginal / ItemQtdOriginal) * RecordItemCdSp01.quantidade; };
                                    if (ItemCofinsValorBaseCalculoOriginal > 0) { RecordItemCdSp01.cofins_vbc = (ItemCofinsValorBaseCalculoOriginal / ItemQtdOriginal) * RecordItemCdSp01.quantidade; };
                                    if (ItemCofinsValorOriginal > 0) { RecordItemCdSp01.cofins_vcofins = (ItemCofinsValorOriginal / ItemQtdOriginal) * RecordItemCdSp01.quantidade; };
                                    if (ItemIIValorBaseCalculoOriginal > 0) { RecordItemCdSp01.ii_vbc = (ItemIIValorBaseCalculoOriginal / ItemQtdOriginal) * RecordItemCdSp01.quantidade; };
                                    if (ItemIIValorOriginal > 0) { RecordItemCdSp01.ii_vii = (ItemIIValorOriginal / ItemQtdOriginal) * RecordItemCdSp01.quantidade; };
                                }
                                ListaItensCDSP01.Add(RecordItemCdSp01);
                            }
                            if (RecordItemView.qtd_cdsp_02 > 0)
                            {
                                gc_movimentos_itens RecordItemCdSp02 = LibDB.CloneTObject(RecordItemOrigem);
                                RecordItemCdSp02.id_movimento_item = 0;
                                RecordItemCdSp02.quantidade = RecordItemView.qtd_cdsp_02;
                                if (RecordItemView.qtd_cdsp_02 < ItemQtdOriginal)
                                {
                                    RecordItemCdSp02.valor_total = (ItemValorTotalOriginal / ItemQtdOriginal) * RecordItemCdSp02.quantidade;
                                    if (ItemValorDespesasOriginal > 0) { RecordItemCdSp02.valor_despesas = (ItemValorDespesasOriginal / ItemQtdOriginal) * RecordItemCdSp02.quantidade; };
                                    if (ItemIcmsValorBaseCalculoOriginal > 0) { RecordItemCdSp02.icms_vbc = (ItemIcmsValorBaseCalculoOriginal / ItemQtdOriginal) * RecordItemCdSp02.quantidade; };
                                    if (ItemIcmsValorOriginal > 0) { RecordItemCdSp02.icms_vicms = (ItemIcmsValorOriginal / ItemQtdOriginal) * RecordItemCdSp02.quantidade; };
                                    if (ItemIpiValorBaseCalculoOriginal > 0) { RecordItemCdSp02.ipi_vbc = (ItemIpiValorBaseCalculoOriginal / ItemQtdOriginal) * RecordItemCdSp02.quantidade; };
                                    if (ItemIpiValorOriginal > 0) { RecordItemCdSp02.ipi_vipi = (ItemIpiValorOriginal / ItemQtdOriginal) * RecordItemCdSp02.quantidade; };
                                    if (ItemPisValorBaseCalculoOriginal > 0) { RecordItemCdSp02.pis_vbc = (ItemPisValorBaseCalculoOriginal / ItemQtdOriginal) * RecordItemCdSp02.quantidade; };
                                    if (ItemPisValorOriginal > 0) { RecordItemCdSp02.pis_vpis = (ItemPisValorOriginal / ItemQtdOriginal) * RecordItemCdSp02.quantidade; };
                                    if (ItemCofinsValorBaseCalculoOriginal > 0) { RecordItemCdSp02.cofins_vbc = (ItemCofinsValorBaseCalculoOriginal / ItemQtdOriginal) * RecordItemCdSp02.quantidade; };
                                    if (ItemCofinsValorOriginal > 0) { RecordItemCdSp02.cofins_vcofins = (ItemCofinsValorOriginal / ItemQtdOriginal) * RecordItemCdSp02.quantidade; };
                                    if (ItemIIValorBaseCalculoOriginal > 0) { RecordItemCdSp02.ii_vbc = (ItemIIValorBaseCalculoOriginal / ItemQtdOriginal) * RecordItemCdSp02.quantidade; };
                                    if (ItemIIValorOriginal > 0) { RecordItemCdSp02.ii_vii = (ItemIIValorOriginal / ItemQtdOriginal) * RecordItemCdSp02.quantidade; };
                                }
                                ListaItensCDSP02.Add(RecordItemCdSp02);
                            }
                            if (RecordItemView.qtd_cdsp_03 > 0)
                            {
                                gc_movimentos_itens RecordItemCdSp03 = LibDB.CloneTObject(RecordItemOrigem);
                                RecordItemCdSp03.quantidade = RecordItemView.qtd_cdsp_03;
                                if (RecordItemView.qtd_cdsp_03 < ItemQtdOriginal)
                                {
                                    RecordItemCdSp03.valor_total = (ItemValorTotalOriginal / ItemQtdOriginal) * RecordItemCdSp03.quantidade;
                                    if (ItemValorDespesasOriginal > 0) { RecordItemCdSp03.valor_despesas = (ItemValorDespesasOriginal / ItemQtdOriginal) * RecordItemCdSp03.quantidade; };
                                    if (ItemIcmsValorBaseCalculoOriginal > 0) { RecordItemCdSp03.icms_vbc = (ItemIcmsValorBaseCalculoOriginal / ItemQtdOriginal) * RecordItemCdSp03.quantidade; };
                                    if (ItemIcmsValorOriginal > 0) { RecordItemCdSp03.icms_vicms = (ItemIcmsValorOriginal / ItemQtdOriginal) * RecordItemCdSp03.quantidade; };
                                    if (ItemIpiValorBaseCalculoOriginal > 0) { RecordItemCdSp03.ipi_vbc = (ItemIpiValorBaseCalculoOriginal / ItemQtdOriginal) * RecordItemCdSp03.quantidade; };
                                    if (ItemIpiValorOriginal > 0) { RecordItemCdSp03.ipi_vipi = (ItemIpiValorOriginal / ItemQtdOriginal) * RecordItemCdSp03.quantidade; };
                                    if (ItemPisValorBaseCalculoOriginal > 0) { RecordItemCdSp03.pis_vbc = (ItemPisValorBaseCalculoOriginal / ItemQtdOriginal) * RecordItemCdSp03.quantidade; };
                                    if (ItemPisValorOriginal > 0) { RecordItemCdSp03.pis_vpis = (ItemPisValorOriginal / ItemQtdOriginal) * RecordItemCdSp03.quantidade; };
                                    if (ItemCofinsValorBaseCalculoOriginal > 0) { RecordItemCdSp03.cofins_vbc = (ItemCofinsValorBaseCalculoOriginal / ItemQtdOriginal) * RecordItemCdSp03.quantidade; };
                                    if (ItemCofinsValorOriginal > 0) { RecordItemCdSp03.cofins_vcofins = (ItemCofinsValorOriginal / ItemQtdOriginal) * RecordItemCdSp03.quantidade; };
                                    if (ItemIIValorBaseCalculoOriginal > 0) { RecordItemCdSp03.ii_vbc = (ItemIIValorBaseCalculoOriginal / ItemQtdOriginal) * RecordItemCdSp03.quantidade; };
                                    if (ItemIIValorOriginal > 0) { RecordItemCdSp03.ii_vii = (ItemIIValorOriginal / ItemQtdOriginal) * RecordItemCdSp03.quantidade; };
                                }
                                ListaItensCDSP03.Add(RecordItemCdSp03);
                            }
                        }


                        if (ListaItensCDBH01.Count > 0)
                        {
                            int SequenciaItem = 0;
                            StartDBGestaoComercial ServiceCDBH01 = new StartDBGestaoComercial();
                            gc_movimentos RecordMovimentoCDBH01 = ServiceCDBH01.TotalizarMovimentosItens(1, RecordMovimentoOrigem, ListaItensCDBH01);
                            foreach (var RecordItem in ListaItensCDBH01)
                            {
                                SequenciaItem += 1;
                                RecordItem.id_movimento_item = 0;
                                RecordItem.sequencia = SequenciaItem;
                                RecordItem.id_movimento = RecordMovimentoCDBH01.id_movimento;
                                RecordItem.id_movimento_ref = RecordMovimentoOrigem.id_movimento;
                                RecordItem.id_coligada = 1;
                                RecordItem.id_filial = 1;
                                db.gc_movimentos_itens.Add(RecordItem);
                            }
                            db.SaveChanges();
                        }

                        if (ListaItensCDBH02.Count > 0)
                        {
                            int SequenciaItem = 0;
                            StartDBGestaoComercial ServiceCDBH02 = new StartDBGestaoComercial();
                            gc_movimentos RecordMovimentoCDBH02 = ServiceCDBH02.TotalizarMovimentosItens(1, RecordMovimentoOrigem, ListaItensCDBH02);
                            foreach (var RecordItem in ListaItensCDBH02)
                            {
                                SequenciaItem += 1;
                                RecordItem.id_movimento_item = 0;
                                RecordItem.sequencia = SequenciaItem;
                                RecordItem.id_movimento = RecordMovimentoCDBH02.id_movimento;
                                RecordItem.id_movimento_ref = RecordMovimentoOrigem.id_movimento;
                                RecordItem.id_coligada = 1;
                                RecordItem.id_filial = 1;
                                db.gc_movimentos_itens.Add(RecordItem);
                            }
                            db.SaveChanges();
                        }

                        if (ListaItensCDBH03.Count > 0)
                        {
                            int SequenciaItem = 0;
                            StartDBGestaoComercial ServiceCDBH03 = new StartDBGestaoComercial();
                            gc_movimentos RecordMovimentoCDBH03 = ServiceCDBH03.TotalizarMovimentosItens(1, RecordMovimentoOrigem, ListaItensCDBH03);
                            foreach (var RecordItem in ListaItensCDBH03)
                            {
                                SequenciaItem += 1;
                                RecordItem.id_movimento_item = 0;
                                RecordItem.sequencia = SequenciaItem;
                                RecordItem.id_movimento = RecordMovimentoCDBH03.id_movimento;
                                RecordItem.id_movimento_ref = RecordMovimentoOrigem.id_movimento;
                                RecordItem.id_coligada = 1;
                                RecordItem.id_filial = 1;
                                db.gc_movimentos_itens.Add(RecordItem);
                            }
                            db.SaveChanges();
                        }

                        if (ListaItensCDSP01.Count > 0)
                        {
                            int SequenciaItem = 0;
                            StartDBGestaoComercial ServiceCDSP01 = new StartDBGestaoComercial();
                            gc_movimentos RecordMovimentoCDSP01 = ServiceCDSP01.TotalizarMovimentosItens(3, RecordMovimentoOrigem, ListaItensCDSP01);
                            foreach (var RecordItem in ListaItensCDSP01)
                            {
                                SequenciaItem += 1;
                                RecordItem.id_movimento_item = 0;
                                RecordItem.sequencia = SequenciaItem;
                                RecordItem.id_movimento = RecordMovimentoCDSP01.id_movimento;
                                RecordItem.id_movimento_ref = RecordMovimentoOrigem.id_movimento;
                                RecordItem.id_coligada = 1;
                                RecordItem.id_filial = 2;
                                db.gc_movimentos_itens.Add(RecordItem);
                            }
                            db.SaveChanges();
                        }

                        if (ListaItensCDSP02.Count > 0)
                        {
                            int SequenciaItem = 0;
                            StartDBGestaoComercial ServiceCDSP02 = new StartDBGestaoComercial();
                            gc_movimentos RecordMovimentoCDSP02 = ServiceCDSP02.TotalizarMovimentosItens(3, RecordMovimentoOrigem, ListaItensCDSP02);
                            foreach (var RecordItem in ListaItensCDSP02)
                            {
                                SequenciaItem += 1;
                                RecordItem.id_movimento_item = 0;
                                RecordItem.sequencia = SequenciaItem;
                                RecordItem.id_movimento = RecordMovimentoCDSP02.id_movimento;
                                RecordItem.id_movimento_ref = RecordMovimentoOrigem.id_movimento;
                                RecordItem.id_coligada = 1;
                                RecordItem.id_filial = 2;
                                db.gc_movimentos_itens.Add(RecordItem);
                            }
                            db.SaveChanges();
                        }

                        if (ListaItensCDSP03.Count > 0)
                        {
                            int SequenciaItem = 0;
                            StartDBGestaoComercial ServiceCDSP03 = new StartDBGestaoComercial();
                            gc_movimentos RecordMovimentoCDSP03 = ServiceCDSP03.TotalizarMovimentosItens(3, RecordMovimentoOrigem, ListaItensCDSP03);
                            foreach (var RecordItem in ListaItensCDSP03)
                            {
                                SequenciaItem += 1;
                                RecordItem.id_movimento_item = 0;
                                RecordItem.sequencia = SequenciaItem;
                                RecordItem.id_movimento = RecordMovimentoCDSP03.id_movimento;
                                RecordItem.id_movimento_ref = RecordMovimentoOrigem.id_movimento;
                                RecordItem.id_coligada = 1;
                                RecordItem.id_filial = 2;
                                db.gc_movimentos_itens.Add(RecordItem);
                            }
                            db.SaveChanges();
                        }

                        db.SaveChanges();

                        // Atualizar todos os itens do movimento origem
                        foreach (gc_movimentos_itens RecordMovimentosItens in allRecordsItensOrigem)
                        {
                            RecordMovimentosItens.item_desdobrado = true;
                            RecordMovimentosItens.datahora_alteracao = DataHoraAtual;
                            RecordMovimentosItens.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                            db.Entry(RecordMovimentosItens).State = EntityState.Modified;
                        }

                        // Atualizar o movimento de origem
                        RecordMovimentoOrigem.entrada_nfe_processada = true;
                        RecordMovimentoOrigem.movimento_faturado = true;
                        RecordMovimentoOrigem.datahora_alteracao = DataHoraAtual;
                        RecordMovimentoOrigem.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                        db.Entry(RecordMovimentoOrigem).State = EntityState.Modified;
                        #endregion
                    }
                    db.SaveChanges();
                    Sucesso = true;
                    if (Sucesso == true)
                    {
                        MsgRetorno += "Entrada <b>Processada</b> com Sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>";
                    }
                }
                else
                {
                    MsgRetorno = "Foram encontrados ERROS no processamento!" + "<br/><br/>";
                    if (msgErroTipo1.EmptyIfNull().ToString().Trim().Length > 0)
                    {
                        MsgRetorno += "===== QUANTIDADES INVÁLIDAS =====" + "<br/>" + "Itens: " + msgErroTipo1 + " <br/><br/>";
                    }
                    if (msgErroTipo2.EmptyIfNull().ToString().Trim().Length > 0)
                    {
                        MsgRetorno += "===== TOTAL DAS QUANTIDADES DIVERGENTE =====" + "<br/>" + "Itens: " + msgErroTipo2 + "<br/><br/>";
                    }
                }
            }
            catch (DbEntityValidationException ex)
            {
                Sucesso = false;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            return Json(new { success = Sucesso, msg = MsgRetorno, idProcessamento = idProcessamentoGravado }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region NFe Entrada Exterior - Gerar NF Internalização
        public ActionResult ModalNFEntradaGerarNF(int? id)
        {
            ViewBag.Title = "Faturar NF Importação";
            CstDadosFaturamentoNF _cstFaturarNFImportacao = new CstDadosFaturamentoNF();
            gc_movimentos record_gc_movimentos = db.gc_movimentos.Find(id);
            if (record_gc_movimentos != null)
            {
                if ((record_gc_movimentos.movimento_faturado == false) && (record_gc_movimentos.id_movimento_ref > 0) && (record_gc_movimentos.id_movimento_tipo == 7))
                {
                    _cstFaturarNFImportacao.id_movimento = record_gc_movimentos.id_movimento;
                    _cstFaturarNFImportacao.FaturamentoLiberado = true;
                    _cstFaturarNFImportacao.FaturamentoExecutado = false;
                    _cstFaturarNFImportacao.NfeGerada = false;
                    _cstFaturarNFImportacao.NfeNaoAutorizada = false;
                    _cstFaturarNFImportacao.MsgInfo = "NF Liberada para faturamento!";
                    _cstFaturarNFImportacao.id_frete_responsavel = 2; // Destinatário
                    _cstFaturarNFImportacao.frete_qvol = record_gc_movimentos.frete_qvol;
                    _cstFaturarNFImportacao.frete_esp = record_gc_movimentos.frete_esp;
                    _cstFaturarNFImportacao.frete_marca = record_gc_movimentos.frete_marca;
                    _cstFaturarNFImportacao.frete_nvol = record_gc_movimentos.frete_nvol;
                    _cstFaturarNFImportacao.frete_pesol = record_gc_movimentos.frete_pesol;
                    _cstFaturarNFImportacao.frete_pesob = record_gc_movimentos.frete_pesob;
                    _cstFaturarNFImportacao.frete_valor = record_gc_movimentos.frete_valor;
                    _cstFaturarNFImportacao.id_ambiente_sefaz = 0;
                    _cstFaturarNFImportacao.informacoes_adicionais = record_gc_movimentos.informacoes_adicionais.Replace("  ", " ").Replace("  ", " ").Replace("  ", " ");
                }
                else if (record_gc_movimentos.movimento_faturado == true)
                {
                    _cstFaturarNFImportacao.FaturamentoLiberado = false;
                    _cstFaturarNFImportacao.FaturamentoExecutado = true;
                    _cstFaturarNFImportacao.NfeGerada = false;
                    _cstFaturarNFImportacao.NfeNaoAutorizada = false;
                    _cstFaturarNFImportacao.MsgBloqueio = "NF já faturada!";
                }
                else
                {
                    _cstFaturarNFImportacao.FaturamentoLiberado = false;
                    _cstFaturarNFImportacao.FaturamentoExecutado = false;
                    _cstFaturarNFImportacao.NfeGerada = false;
                    _cstFaturarNFImportacao.NfeNaoAutorizada = false;
                    _cstFaturarNFImportacao.MsgBloqueio = "NF não liberada para faturamento!";
                }
            }
            else
            {
                _cstFaturarNFImportacao.FaturamentoLiberado = false;
                _cstFaturarNFImportacao.FaturamentoExecutado = false;
                _cstFaturarNFImportacao.NfeGerada = false;
                _cstFaturarNFImportacao.NfeNaoAutorizada = false;
                _cstFaturarNFImportacao.MsgBloqueio = "Nota fiscal não localizada!";
            }
            PreencherLookupsFreteResponsavel();
            return View(_cstFaturarNFImportacao);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AjaxFaturarNFCompra(CstDadosFaturamentoNF record_cstFaturarNFImportacao)
        {
            int IdGateway = 0;
            LibRetornoProcessamento RetornoProcessamento = new LibRetornoProcessamento();
            try
            {
                gc_movimentos RecordMovimento = db.gc_movimentos.Find(record_cstFaturarNFImportacao.id_movimento);
                String NumeroNF = RecordMovimento.nf_numero.EmptyIfNull().ToString();
                gc_movimentos_nf record_gc_movimento_nf = new gc_movimentos_nf();
                record_gc_movimento_nf.id_movimento = record_cstFaturarNFImportacao.id_movimento;
                record_gc_movimento_nf.frete_valor = record_cstFaturarNFImportacao.frete_valor;
                record_gc_movimento_nf.frete_qvol = record_cstFaturarNFImportacao.frete_qvol;
                record_gc_movimento_nf.frete_esp = record_cstFaturarNFImportacao.frete_esp;
                record_gc_movimento_nf.frete_marca = record_cstFaturarNFImportacao.frete_marca;
                record_gc_movimento_nf.frete_nvol = record_cstFaturarNFImportacao.frete_nvol;
                record_gc_movimento_nf.frete_pesol = record_cstFaturarNFImportacao.frete_pesol;
                record_gc_movimento_nf.frete_pesob = record_cstFaturarNFImportacao.frete_pesob;
                record_gc_movimento_nf.id_frete_responsavel = record_cstFaturarNFImportacao.id_frete_responsavel;
                record_gc_movimento_nf.id_transportadora = 0;
                record_gc_movimento_nf.informacoes_adicionais = record_cstFaturarNFImportacao.informacoes_adicionais;

                if (RecordMovimento.id_filial == 1) { IdGateway = 1; }
                else if (RecordMovimento.id_filial == 2) { IdGateway = 2; }

                if ((IdGateway == 1) || (IdGateway == 2)) // ENotas
                {
                    RoboEnotasNFE _RoboFaturarNFP = new RoboEnotasNFE();
                    _RoboFaturarNFP.GerarNFPImportacaoByMovimentoNF(record_gc_movimento_nf);
                    RetornoProcessamento.Sucesso = true;
                    RetornoProcessamento.MsgProcessamento = "Nota Fiscal Entrada Importação Nº  [<b>" + NumeroNF + "</b>] transmitida com Sucesso!";
                }
            }
            catch (WebException ex)
            {
                RetornoProcessamento.Sucesso = false;
                RetornoProcessamento.MsgProcessamento = LibExceptions.getWebException(ex);
            }
            catch (DbEntityValidationException ex)
            {
                RetornoProcessamento.Sucesso = false;
                RetornoProcessamento.MsgProcessamento = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                RetornoProcessamento.Sucesso = false;
                RetornoProcessamento.MsgProcessamento = LibExceptions.getExceptionShortMessage(e);
            }
            return Json(new { success = RetornoProcessamento.Sucesso, msg = RetornoProcessamento.MsgProcessamento }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region NFe Entrada Exterior - Cancelar Movimento
        public ActionResult ModalNFEntradaCancelar(int? id)
        {
            int TempId = id.GetValueOrDefault();
            int QtdNFAutorizadas = 0;
            ViewBag.Title = "Cancelar Movimento NF Importada - Compra Exterior";
            gc_movimentos RecordMovimento = db.gc_movimentos.Find(TempId);

            QtdNFAutorizadas += db.gc_movimentos_nf.Where(n => n.id_movimento == TempId && n.id_nfe_status == 8).ToList().Count(); // Qtd NF Autorizadas para o IdMovPrincipal
            List<gc_movimentos> ListaGCMovimentos = db.gc_movimentos.Where(m => m.id_movimento_ref == TempId).ToList();
            foreach (gc_movimentos movimento in ListaGCMovimentos)
            {
                QtdNFAutorizadas += db.gc_movimentos_nf.Where(n => n.id_movimento == movimento.id_movimento && n.id_nfe_status == 8).ToList().Count(); // Qtd NF Autorizadas para o Movimento Relacionado
            }

            CstDadosFaturamentoNF _cstFaturarNFImportacao = new CstDadosFaturamentoNF();
            _cstFaturarNFImportacao.id_movimento = TempId;
            if (QtdNFAutorizadas == 0)
            {
                _cstFaturarNFImportacao.CancelamentoLiberado = true;
                _cstFaturarNFImportacao.MsgInfo = "Movimento Liberado para Cancelamento!";
            }
            else
            {
                _cstFaturarNFImportacao.CancelamentoLiberado = false;
                _cstFaturarNFImportacao.MsgBloqueio = "Movimento NÃO Liberado para Cancelamento!</br></br>Existem " + QtdNFAutorizadas.ToString() + "Notas Fiscais AUTORIZADAS para esse movimento!";
            }
            return View(_cstFaturarNFImportacao);
        }


        [HttpPost]
        public ActionResult AjaxCancelarNFCompra(CstDadosFaturamentoNF record_cstFaturarNFImportacao)
        {
            bool Sucesso = false;
            int QtdErros = 0;
            String MsgRetorno = "";
            String ListaMovimentosCancelados = "";
            int QtdMovimentosCancelados = 0;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            try
            {
                gc_movimentos RecordMovimento = db.gc_movimentos.Find(record_cstFaturarNFImportacao.id_movimento);
                List<gc_movimentos> ListaGCMovimentos = db.gc_movimentos.Where(m => m.id_movimento_ref == record_cstFaturarNFImportacao.id_movimento).ToList();
                if (QtdErros == 0)
                {
                    RecordMovimento.id_movimento_status = 3; // Cancelado
                    RecordMovimento.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario; ;
                    RecordMovimento.datahora_alteracao = DataHoraAtual;
                    QtdMovimentosCancelados += 1;
                    ListaMovimentosCancelados += RecordMovimento.id_movimento.ToString() + " ";
                    db.Entry(RecordMovimento).State = EntityState.Modified;

                    foreach (gc_movimentos RecordMovimentoDesdobrado in ListaGCMovimentos)
                    {
                        RecordMovimentoDesdobrado.id_movimento_status = 3; // Cancelado
                        RecordMovimentoDesdobrado.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario; ;
                        RecordMovimentoDesdobrado.datahora_alteracao = DataHoraAtual;
                        QtdMovimentosCancelados += 1;
                        ListaMovimentosCancelados += RecordMovimentoDesdobrado.id_movimento.ToString() + " ";
                        db.Entry(RecordMovimentoDesdobrado).State = EntityState.Modified;
                    }

                    MsgRetorno = "Movimentos CANCELADOS com Sucesso! " + LibStringFormat.GetTabHtml(2) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "</br></br>";
                    MsgRetorno += "Qtd. Movimentos: " + LibStringFormat.GetTabHtml(1) + QtdMovimentosCancelados.ToString() + "</br>";
                    MsgRetorno += "Lista Movimentos: " + LibStringFormat.GetTabHtml(1) + ListaMovimentosCancelados.ToString();
                    db.SaveChanges();
                    Sucesso = true;
                }
            }
            catch (DbEntityValidationException ex)
            {
                QtdErros = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                QtdErros = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }

            return Json(new { success = Sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        public g_clientes GetClienteByEmitente(CstNfeEmitente record_NfeEmitente)
        {
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            g_clientes record_g_cliente = db.g_clientes.Where(p => p.cnpj == record_NfeEmitente.CNPJ.ToString()).FirstOrDefault();
            if (record_g_cliente != null)
            {
                //idCliente = record_g_cliente.id_cliente;
            }
            else
            {
                g_cidades record_g_cidades = db.g_cidades.Where(c => c.nome == record_NfeEmitente.xMun.ToString()).FirstOrDefault();
                if (record_g_cidades == null)
                {
                    record_g_cidades = new Db.g_cidades();
                    record_g_cidades.nome = record_NfeEmitente.xMun.ToString();
                    record_g_cidades.id_coligada = 0;  // Definição de que Cidade é Global
                    record_g_cidades.id_filial = 0;    // Definição de que Cidade é Global
                    record_g_cidades.datahora_cadastro = DataHoraAtual;
                    record_g_cidades.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario; ;
                    db.g_cidades.Add(record_g_cidades);
                    db.SaveChanges();
                }
                g_uf record_g_uf = db.g_uf.Where(u => u.sigla == record_NfeEmitente.UF.ToString()).FirstOrDefault();

                record_g_cliente = new Db.g_clientes();
                record_g_cliente.cnpj = record_NfeEmitente.CNPJ;
                record_g_cliente.nome = record_NfeEmitente.xNome;
                record_g_cliente.razao_social = record_NfeEmitente.xNome;
                record_g_cliente.nome_fantasia = record_NfeEmitente.xFant;
                record_g_cliente.inscricao_estadual = record_NfeEmitente.IE;
                record_g_cliente.endereco_com = record_NfeEmitente.xLgr;
                record_g_cliente.endereco_com_numero = record_NfeEmitente.nro;
                record_g_cliente.bairro_com = record_NfeEmitente.xBairro;
                record_g_cliente.cep_com = record_NfeEmitente.CEP;
                record_g_cliente.telefone_principal = record_NfeEmitente.fone;
                record_g_cliente.id_cidade_com = record_g_cidades.id_cidade;
                record_g_cliente.id_uf_com = record_g_uf.id_uf;
                record_g_cliente.iss_tipo = "D";
                record_g_cliente.ir_tipo = "D";
                record_g_cliente.pis_tipo = "D";
                record_g_cliente.cofins_tipo = "D";
                record_g_cliente.csll_tipo = "D";
                record_g_cliente.nf_tipo = "N";
                record_g_cliente.pcc_tipo = "D";
                record_g_cliente.inss_tipo = "D";
                record_g_cliente.id_coligada = 0; // Global
                record_g_cliente.id_filial = 0; // Global
                record_g_cliente.datahora_cadastro = DataHoraAtual;
                record_g_cliente.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario; ;

                db.g_clientes.Add(record_g_cliente);
                db.SaveChanges();
                record_g_cliente.id_usuario_alteracao = 9999; // Validação de que é um cadastro novo
            }
            return record_g_cliente;
        }

        #region ModalPedidoTransferirFilial
        public ActionResult ModalPedidoTransferirFilial(int? id) // Continuar Daqui
        {
            int temp = id.GetValueOrDefault();
            String TitleModal = string.Empty;
            String MsgBloqueio = string.Empty;
            TitleModal = LibIcons.getIcon("fa-solid fa-eraser", "", "#008000", "fa-sm") + LibStringFormat.GetEspacesHtml(3) + "Carta de Correção";
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            CstNfeCartaCorrecao RecordCstNfeCartaCorrecao = new CstNfeCartaCorrecao();
            gc_movimentos RecordMovimento = db.gc_movimentos.Find(id);
            if (RecordMovimento != null)
            {
                TitleModal = LibIcons.getIcon("fa-solid fa-truck-arrow-right", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Transferência Filial SP Nº " + RecordMovimento.id_movimento.ToString();
                if (RecordMovimento.id_movimento_tipo != 7) { MsgBloqueio += "Somente NFe Entrada Internacional - Internalização!" + "<br/>"; };
                if (RecordMovimento.entrada_nfe_processada == true) { if (RecordMovimento.receb_estoque_processado == false) { MsgBloqueio += "NFe Entrada não foi recebida no estoque!" + "<br/>"; }; }
                else { MsgBloqueio += "NFe Entrada não foi processada!" + "<br/>"; };
                if ((RecordMovimento.movimento_transferido_filial == true) && (RecordMovimento.id_movimento_transferencia > 0)) { MsgBloqueio += "NFe Entrada já foi transferida!" + "<br/>"; };
                if (RecordMovimento.id_estoque_cd != 3) { MsgBloqueio += "Somente NFes do Centro Distribuição SP podem ser transferidas!" + "<br/>"; };
            }
            else
            {
                MsgBloqueio += "NFe Não Encontrada!" + "<br/>";
            }
            ViewBag.Title = TitleModal;
            ViewBag.MsgBloqueio = MsgBloqueio;
            return View(RecordMovimento);
        }

        [HttpPost]
        public ActionResult AjaxModalPedidoTransferirFilial(gc_movimentos view_record_gc_movimentos)
        {
            bool Sucesso = false;
            String MsgRetorno = "";
            try
            {
                gc_movimentos RecordMovimentoOrigem = db.gc_movimentos.Find(view_record_gc_movimentos.id_movimento);
                gc_movimentos RecordMovimentoTransferencia = LibDB.CloneTObject(RecordMovimentoOrigem);
                DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
                RecordMovimentoTransferencia.id_movimento = 0;
                RecordMovimentoTransferencia.id_movimento_tipo = 19; // Saída - Transferência entre Filiais
                RecordMovimentoTransferencia.id_cfop_operacao = 25; // Pedido
                RecordMovimentoTransferencia.id_movimento_posicao = 1;
                RecordMovimentoTransferencia.id_nfe_status = 0;
                RecordMovimentoTransferencia.id_movimento_ref = RecordMovimentoOrigem.id_movimento;
                RecordMovimentoTransferencia.id_cliente = 3637; // GDI SP
                RecordMovimentoTransferencia.nf_serie = "0";
                RecordMovimentoTransferencia.nf_numero = "0";
                RecordMovimentoTransferencia.nf_data_geracao = null;
                RecordMovimentoTransferencia.nf_chave = null;
                RecordMovimentoTransferencia.nf_chave_referenciada = null;
                RecordMovimentoTransferencia.nf_data_recebimento = null;
                RecordMovimentoTransferencia.nf_protocolo = null;
                RecordMovimentoTransferencia.nf_digval = null;
                RecordMovimentoTransferencia.nf_cstat = null;
                RecordMovimentoTransferencia.nf_cmotivo = null;
                RecordMovimentoTransferencia.nf_key = null;
                RecordMovimentoTransferencia.nf_url_pdf = null;
                RecordMovimentoTransferencia.nf_url_xml = null;
                RecordMovimentoTransferencia.nf_s3_pdf = 0;
                RecordMovimentoTransferencia.nf_s3_xml = 0;
                RecordMovimentoTransferencia.informacoes_adicionais = string.Empty;
                RecordMovimentoTransferencia.movimento_aprovado = true;
                RecordMovimentoTransferencia.id_usuario_aprovacao = CachePersister.userIdentity.IdUsuario;
                RecordMovimentoTransferencia.datahora_aprovacao = DataHoraAtual;
                RecordMovimentoTransferencia.movimento_faturado = false;
                RecordMovimentoTransferencia.id_usuario_faturamento = 0;
                RecordMovimentoTransferencia.datahora_faturamento = null;
                RecordMovimentoTransferencia.movimento_nf = false;
                RecordMovimentoTransferencia.movimento_nf_autorizada = false;
                RecordMovimentoTransferencia.entrada_nfe_processada = false;
                RecordMovimentoTransferencia.receb_estoque_processado = false;
                RecordMovimentoTransferencia.id_usuario_nf = 0;
                RecordMovimentoTransferencia.datahora_nf = null;
                RecordMovimentoTransferencia.id_coligada = 1; // GRUPO GDI
                RecordMovimentoTransferencia.id_filial = 1; // FILIAL BH - Material está saindo de BH
                RecordMovimentoTransferencia.id_local_estoque = 1; // FILIAL BH - Material está saindo de BH
                RecordMovimentoTransferencia.id_estoque_cd = 3;  // FILIAL SP - Material está indo para SP
                RecordMovimentoTransferencia.id_vendedor = 2; // Gustavo Furtado
                RecordMovimentoTransferencia.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                RecordMovimentoTransferencia.datahora_cadastro = DataHoraAtual;
                db.gc_movimentos.Add(RecordMovimentoTransferencia);
                db.SaveChanges();

                List<gc_movimentos_itens> ListaItensMovimentoTransferencia = db.gc_movimentos_itens.Where(i => i.id_movimento == RecordMovimentoOrigem.id_movimento).ToList();
                foreach (var RecordItemOrigem in ListaItensMovimentoTransferencia)
                {
                    gc_movimentos_itens RecordItemTransferencia = LibDB.CloneTObject(RecordItemOrigem);
                    RecordItemTransferencia.id_movimento_item = 0;
                    RecordItemTransferencia.id_movimento_ref = 0;
                    RecordItemTransferencia.id_movimento = RecordMovimentoTransferencia.id_movimento;
                    RecordItemTransferencia.id_coligada = RecordMovimentoTransferencia.id_coligada; // GRUPO GDI 
                    RecordItemTransferencia.id_filial = RecordMovimentoTransferencia.id_filial; // FILIAL SP
                    RecordItemTransferencia.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                    RecordItemTransferencia.datahora_cadastro = DataHoraAtual;
                    RecordItemTransferencia.id_usuario_alteracao = 0;
                    RecordItemTransferencia.datahora_alteracao = null;
                    db.gc_movimentos_itens.Add(RecordItemTransferencia);
                }
                db.SaveChanges();

                RecordMovimentoOrigem.movimento_transferido_filial = true;
                RecordMovimentoOrigem.id_movimento_transferencia = RecordMovimentoTransferencia.id_movimento;
                RecordMovimentoOrigem.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                RecordMovimentoOrigem.datahora_alteracao = DataHoraAtual;
                db.Entry(RecordMovimentoOrigem).State = EntityState.Modified;
                db.SaveChanges();

                Sucesso = true;
                MsgRetorno += "Pedido de Transferência Filial SP criado com sucesso, Nº " + RecordMovimentoTransferencia.id_movimento.ToString() + "!" + "<br/><br/>";
                MsgRetorno += "<b>Atenção Operador: </b>O Pedido já está APROVADO, Para concluir a transferência é necessário realizar todos os processos seguintes (Separação, NF, Expedição e Entrega)!" + "<br/><br/>";
            }
            catch (WebException ex)
            {
                Sucesso = false;
                MsgRetorno = LibExceptions.getWebException(ex);
            }
            catch (DbEntityValidationException ex)
            {
                Sucesso = false;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception ex)
            {
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(ex);
            }
            return Json(new { success = Sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }
        #endregion

    }
}