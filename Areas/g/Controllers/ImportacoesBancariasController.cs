// Migrado em 2020_07_15

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using GdiPlataform.Areas.g.Models;
using GdiPlataform.Db;
using GdiPlataform.Lib;
using GdiPlataform.Security;

namespace GdiPlataform.Areas.g.Controllers
{
    [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ImportacoesBancarias_*,g_ImportacoesBancarias_Default")]
    public class ImportacoesBancariasController : Controller
    {
        private GdiPlataformEntities db;

        public ImportacoesBancariasController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Importações - Bancárias";
            return View();
        }

        #region PreencherLookupsImportacao
        public void PreencherLookupsImportacao()
        {
            var comboContaCaixa = new List<SelectListItem>();
            IQueryable<g_contas_caixas> listaDbContasCaixas = db.g_contas_caixas.Where(p => p.ativo == true && p.boleto_cnab_retorno == true && p.boleto_emissao == true).OrderBy(p => p.nome);
            foreach (g_contas_caixas item1 in listaDbContasCaixas)
            {
                comboContaCaixa.Add(new SelectListItem { Value = item1.id_conta_caixa.ToString(), Text = item1.nome.ToString() });
            }
            ViewBag.comboContaCaixa = comboContaCaixa;
        }
        #endregion

        #region Importar Arquivo - CNAB Boletos
        public ActionResult ModalImportarCnabBoletos()
        {
            PreencherLookupsImportacao();
            ViewBag.Title = "Importação Bancária - CNAB Boletos";
            return View();
        }

        [HttpPost]
        public ActionResult AjaxImportarCnabBoletos(CstImportacoesBancarias record_cstImportacoesBancarias)
        {
            int ponteiro = 0;
            int linhaNumero = 1;
            int qtdBoletosLiquidados = 0;
            int qtdBoletosLiquidadosAnteriormente = 0;
            int qtdBoletosEntradasConfirmadas = 0;
            int qtdBoletosEntradasConfirmadasAnteriormente = 0;
            int qtdBoletosEntradasRejeitadas = 0;
            int qtdBoletosCanceladosConfirmadas = 0;
            int qtdBoletosCanceladosConfirmadasAnteriormente = 0;
            int qtdBoletosComandoNaoReconhecido = 0;
            int qtdBoletosNaoLocalizados = 0;
            bool processado = false;
            bool erroProcessamento = false;
            bool ComandoTituloLiquidado = false;
            bool ComandoEntradaConfirmada = false;
            bool ComandoEntradaRejeitada = false;
            bool ComandoCancelamentoConfirmado = false;
            String msgRetorno = String.Empty;
            String msgErro = String.Empty;
            String resultadoProcessamento = String.Empty;
            String idProcessamentoGravado = "0";
            String linhaAuxiliar = String.Empty;
            String ComandoNaoReconhecidoCodigo = String.Empty;
            List<g_financeiro> allFinanceiro = null;
            g_financeiro record_g_financeiro = null;
            List<gc_financeiro_lancamentos> allFinanceiroLancamentos = null;
            gc_financeiro_lancamentos record_gc_financeiro_lancamentos = null;
            int idProcessamentoTemp = int.Parse(LibDateTime.getDataHoraBrasilia().ToString("mmssffff").ToString());
            g_contas_caixas record_g_conta_caixa = null;
            var fileExt = System.IO.Path.GetExtension(record_cstImportacoesBancarias.filesource.FileName).Substring(1);
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();

            // Localizar a Conta Caixa
            record_g_conta_caixa = db.g_contas_caixas.Where(p => p.id_conta_caixa == record_cstImportacoesBancarias.id_conta_caixa).FirstOrDefault();

            if ((record_g_conta_caixa.banco.ToString() != "104") && (record_g_conta_caixa.banco.ToString() != "341") && (record_g_conta_caixa.banco.ToString() != "756"))
            {
                erroProcessamento = true;
                msgErro = "O Banco selecionado não está homologado para processamento de retorno de boletos";
            }
            else if (record_g_conta_caixa.boleto_cnab_retorno == false)
            {
                erroProcessamento = true;
                msgErro = "O Banco selecionado não está parametrizado para processar retorno CNAB";
            }
            else if (record_cstImportacoesBancarias.filesource.ContentLength > 200000)
            {
                erroProcessamento = true;
                msgErro = "O Tamanho do arquivo não pode exceder 200 Kb!";
            }
            else if (record_cstImportacoesBancarias.filesource.ContentLength > 0)
            {
                try
                {
                    // Salvar o arquivo na pasta 
                    var fileNameOrigem = Path.GetFileName(record_cstImportacoesBancarias.filesource.FileName);

                    String DirTempFiles = Server.MapPath("~/_filestemp");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, "uploads");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    LibFilesDisk.DeleteFilesInDirectory(DirTempFiles); // Apagar todos os arquivos que estiveremno diretório do usuario
                    var fileNameDestino = Path.Combine(DirTempFiles, LibDateTime.getDataHoraBrasilia().ToString("yyyyMMddhhmmss") + "_" + fileNameOrigem);
                    record_cstImportacoesBancarias.filesource.SaveAs(fileNameDestino);
                    string[] fileAux = System.IO.File.ReadAllLines(fileNameDestino);

                    // Processamento do Arquivo
                    System.IO.StreamReader file = new System.IO.StreamReader(fileNameDestino);
                    String line = file.ReadLine(); // Cabeçalho

                    if ((record_g_conta_caixa.banco.ToString().Equals("756")) && (line.ToString().Length != 400))
                    {
                        erroProcessamento = true;
                        msgErro = "Arquivo fora do layout";
                    }
                    if ((record_g_conta_caixa.banco.ToString().Equals("341")) && (line.ToString().Length != 400))
                    {
                        erroProcessamento = true;
                        msgErro = "Arquivo fora do layout";
                    }
                    else if ((record_g_conta_caixa.banco.ToString().Equals("104")) && (line.ToString().Length != 240))
                    {
                        erroProcessamento = true;
                        msgErro = "Arquivo fora do layout";
                    }
                    else
                    {
                        if (record_g_conta_caixa.financeiro_g == true) 
                        {
                            // Carregar a lista de Todos os Titulos Financeiros
                            allFinanceiro = db.g_financeiro.Where(p => (p.id_financeiro_status == 7 || p.id_financeiro_status == 11 || p.id_financeiro_status == 13 || p.id_financeiro_status == 15)).ToList(); //Em Cob. 7 - Cob. Enviada 11 - Canc. Enviado 13 - Cancelado 15
                        }
                        else if (record_g_conta_caixa.financeiro_g == true)
                        {
                            // Carregar a lista de Todos os Titulos Financeiros
                            allFinanceiroLancamentos = db.gc_financeiro_lancamentos.Where(l => l.ativo == true && l.tipo_pag_rec == 2 && l.id_financeiro_status == 2).ToList();
                        }

                        resultadoProcessamento += "Id. Financeiro;Nosso Numero;Dt. Ocorrencia;Dt. Credito;R$ Pago;Obs;" + "\r\n";
                    }

                    while (((line = file.ReadLine()) != null) && (erroProcessamento == false))
                    {
                        ponteiro += 1;
                        linhaNumero += 1;
                        linhaAuxiliar = String.Empty;
                        ComandoTituloLiquidado = false;
                        ComandoEntradaConfirmada = false;
                        ComandoEntradaRejeitada = false;
                        ComandoCancelamentoConfirmado = false;
                        ComandoNaoReconhecidoCodigo = String.Empty;
                        record_g_financeiro = null;

                        String nossoNumero = String.Empty;
                        DateTime dataOcorrencia = DataHoraAtual;
                        DateTime dataCredito = DataHoraAtual;
                        String bancoRecebedor = String.Empty;
                        String agenciaRecebedora = String.Empty;
                        Decimal valorDespesaCobranca = 0;
                        Decimal valorCreditadoConta = 0;
                        Decimal valorRecebido = 0;

                        if (record_g_conta_caixa.banco.ToString().Equals("104")) // CEF
                        {
                            if (line.Substring(13, 1).Equals("T"))
                            {
                                try { linhaAuxiliar = fileAux[ponteiro + 1]; } catch (Exception) { }; // Segmento U
                                nossoNumero = line.Substring(39, 17);
                                    if (record_g_conta_caixa.financeiro_g == true) { record_g_financeiro = allFinanceiro.Find(f => f.cnab_nosso_numero == nossoNumero); }
                                    else if (record_g_conta_caixa.financeiro_g == true) { record_gc_financeiro_lancamentos = allFinanceiroLancamentos.Find(l => l.cnab_nosso_numero == nossoNumero); }
                                bancoRecebedor = line.Substring(96, 3);
                                agenciaRecebedora = line.Substring(99, 4);
                                if (linhaAuxiliar.Substring(13, 1).Equals("U"))
                                {
                                    try { dataOcorrencia = DateTime.ParseExact(linhaAuxiliar.Substring(138, 8), "ddMMyyyy", CultureInfo.InvariantCulture); } catch (Exception) { };
                                    try { dataCredito = DateTime.ParseExact(linhaAuxiliar.Substring(146, 8), "ddMMyyyy", CultureInfo.InvariantCulture); } catch (Exception) { };
                                    Decimal.TryParse(linhaAuxiliar.Substring(77, 15), out valorRecebido);
                                    if (valorRecebido > 0) { valorRecebido = valorRecebido / 100; };
                                }

                                if (line.Substring(213, 2).Equals("02")) { ComandoEntradaConfirmada = true; }
                                else if ((line.Substring(213, 2).Equals("06")) || (line.Substring(213, 2).Equals("09"))) { ComandoTituloLiquidado = true; }
                                else ComandoNaoReconhecidoCodigo = line.Substring(213, 2);
                            }
                        }
                        else if (record_g_conta_caixa.banco.ToString().Equals("341")) // Itaú
                        {
                            if (line.Substring(0, 1).Equals("1"))
                            {
                                nossoNumero = line.Substring(62, 8);
                                try { dataOcorrencia = DateTime.ParseExact(line.Substring(110, 6), "ddMMyy", CultureInfo.InvariantCulture); } catch (Exception) { };
                                    if (record_g_conta_caixa.financeiro_g == true) { record_g_financeiro = allFinanceiro.Find(f => f.id_financeiro == int.Parse(nossoNumero)); }
                                    else if (record_g_conta_caixa.financeiro_g == true) { record_gc_financeiro_lancamentos = allFinanceiroLancamentos.Find(l => l.id_lancamento == int.Parse(nossoNumero)); }
                                bancoRecebedor = line.Substring(165, 3);
                                agenciaRecebedora = line.Substring(168, 4);
                                try { dataCredito = DateTime.ParseExact(line.Substring(295, 6), "ddMMyy", CultureInfo.InvariantCulture); } catch (Exception) { };
                                try { valorDespesaCobranca = Decimal.Parse(line.Substring(175, 13)) / 100; } catch (Exception) { };
                                try { valorCreditadoConta = Decimal.Parse(line.Substring(253, 13)) / 100; } catch (Exception) { };
                                try { valorRecebido = (valorDespesaCobranca + valorCreditadoConta); } catch (Exception) { };

                                if (line.Substring(108, 2).Equals("02")) { ComandoEntradaConfirmada = true; }
                                else if (line.Substring(108, 2).Equals("03")) { ComandoEntradaRejeitada = true; }
                                else if (line.Substring(108, 2).Equals("09")) { ComandoCancelamentoConfirmado = true; }
                                else if ((line.Substring(108, 2).Equals("06")) || (line.Substring(108, 2).Equals("07")) || (line.Substring(108, 2).Equals("08"))) { ComandoTituloLiquidado = true; }
                                else ComandoNaoReconhecidoCodigo = line.Substring(108, 2);
                            }
                        }
                        else if (record_g_conta_caixa.banco.ToString().Equals("756")) // Sicoob
                        {
                            if (line.Substring(0, 1).Equals("1"))
                            {
                                nossoNumero = line.Substring(66, 8);
                                try { dataOcorrencia = DateTime.ParseExact(line.Substring(110, 6), "ddMMyy", CultureInfo.InvariantCulture); } catch (Exception) { };
                                    if (record_g_conta_caixa.financeiro_g == true) { record_g_financeiro = allFinanceiro.Find(f => f.cnab_nosso_numero == nossoNumero); }
                                    else if (record_g_conta_caixa.financeiro_g == true) { record_gc_financeiro_lancamentos = allFinanceiroLancamentos.Find(l => l.cnab_nosso_numero == nossoNumero); }
                                bancoRecebedor = line.Substring(165, 3);
                                agenciaRecebedora = line.Substring(168, 4);
                                try { dataCredito = DateTime.ParseExact(line.Substring(175, 6), "ddMMyy", CultureInfo.InvariantCulture); } catch (Exception) { };
                                try { valorRecebido = Decimal.Parse(line.Substring(253, 13)) / 100; } catch (Exception) { };

                                if (line.Substring(108, 2).Equals("02")) { ComandoEntradaConfirmada = true; }
                                else if ((line.Substring(108, 2).Equals("05")) || (line.Substring(108, 2).Equals("06"))) { ComandoTituloLiquidado = true; }
                                else ComandoNaoReconhecidoCodigo = line.Substring(108, 2);
                            }
                        }


                        if ((record_g_conta_caixa.financeiro_g == true) && (record_g_financeiro != null))
                        {
                            if (ComandoEntradaConfirmada)       // Entrada Confirmada
                            {
                                if (record_g_financeiro.id_financeiro_status == 7)
                                {
                                    qtdBoletosEntradasConfirmadasAnteriormente += 1;
                                    resultadoProcessamento += record_g_financeiro.id_financeiro.ToString() + ";";
                                    resultadoProcessamento += "N " + record_g_financeiro.cnab_nosso_numero.EmptyIfNull().ToString() + ";";
                                    resultadoProcessamento += dataOcorrencia.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + ";";
                                    resultadoProcessamento += ";";
                                    resultadoProcessamento += ";";
                                    resultadoProcessamento += "Titulo Entrada Confirmada Anteriormente";
                                    resultadoProcessamento += "\r\n";
                                }
                                else
                                {
                                    qtdBoletosEntradasConfirmadas += 1;
                                    resultadoProcessamento += record_g_financeiro.id_financeiro.ToString() + ";";
                                    resultadoProcessamento += "N " + record_g_financeiro.cnab_nosso_numero.EmptyIfNull().ToString() + ";";
                                    resultadoProcessamento += dataOcorrencia.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + ";";
                                    resultadoProcessamento += ";";
                                    resultadoProcessamento += ";";
                                    resultadoProcessamento += "Entrada Confirmada";
                                    resultadoProcessamento += "\r\n";
                                    record_g_financeiro.id_financeiro_status = 7; // Cobrança Enviada
                                    db.Entry(record_g_financeiro).State = EntityState.Modified;

                                    // Criar o histórico do titulo para ratreabilidade
                                    g_financeiro_historicos record_g_financeiro_historicos = new g_financeiro_historicos();
                                    record_g_financeiro_historicos.id_financeiro = record_g_financeiro.id_financeiro;
                                    record_g_financeiro_historicos.id_financeiro_origem = 8; //Importação Bancária
                                    record_g_financeiro_historicos.id_financeiro_status_inicial = 11; // Cob. Enviada
                                    record_g_financeiro_historicos.id_financeiro_status_final = 7;  // Em Cobrança
                                    record_g_financeiro_historicos.id_financeiro_remessa = null;
                                    record_g_financeiro_historicos.id_conta_caixa = record_g_conta_caixa.id_conta_caixa;
                                    record_g_financeiro_historicos.id_pagamento_recebimento_tipo = record_g_financeiro.id_pagamento_recebimento_tipo.GetValueOrDefault();
                                    record_g_financeiro_historicos.historico = "ENTRADA CONFIRMADA";
                                    record_g_financeiro_historicos.id_coligada = 1;
                                    record_g_financeiro_historicos.id_filial = 1;
                                    record_g_financeiro_historicos.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                                    record_g_financeiro_historicos.datahora_cadastro = DataHoraAtual;
                                    db.g_financeiro_historicos.Add(record_g_financeiro_historicos);
                                }

                            }
                            else if (ComandoTituloLiquidado)    // Título Liquidado
                            {
                                if (record_g_financeiro.id_financeiro_status == 6)
                                {
                                    qtdBoletosLiquidadosAnteriormente += 1;
                                    resultadoProcessamento += record_g_financeiro.id_financeiro.ToString() + ";";
                                    resultadoProcessamento += "N " + record_g_financeiro.cnab_nosso_numero.EmptyIfNull().ToString() + ";";
                                    resultadoProcessamento += dataOcorrencia.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + ";";
                                    resultadoProcessamento += record_g_financeiro.cnab_data_credito.GetValueOrDefault().ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + ";";
                                    resultadoProcessamento += record_g_financeiro.valor_pago.ToString("c2") + ";";
                                    resultadoProcessamento += "Titulo Liquidado Anteriormente";
                                    resultadoProcessamento += "\r\n";
                                }
                                else
                                {
                                    qtdBoletosLiquidados += 1;
                                    record_g_financeiro.cnab_data_retorno = dataOcorrencia;
                                    record_g_financeiro.cnab_data_credito = dataCredito;
                                    record_g_financeiro.cnab_banco_cobrador = bancoRecebedor;
                                    record_g_financeiro.cnab_agencia_cobradora = agenciaRecebedora;
                                    record_g_financeiro.valor_pago = valorRecebido;
                                    record_g_financeiro.id_financeiro_status = 6; // Liquidado
                                    record_g_financeiro.id_processamento = idProcessamentoTemp;
                                    record_g_financeiro.id_usuario_liquidacao = CachePersister.userIdentity.IdUsuario;
                                    record_g_financeiro.datahora_liquidacao = LibDateTime.getDataHoraBrasilia();
                                    db.Entry(record_g_financeiro).State = EntityState.Modified;

                                    resultadoProcessamento += record_g_financeiro.id_financeiro.ToString() + ";";
                                    resultadoProcessamento += "N " + record_g_financeiro.cnab_nosso_numero.EmptyIfNull().ToString() + ";";
                                    resultadoProcessamento += dataOcorrencia.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + ";";
                                    resultadoProcessamento += dataCredito.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + ";";
                                    resultadoProcessamento += valorRecebido.ToString("c2") + ";";
                                    resultadoProcessamento += "Titulo Liquidado";
                                    resultadoProcessamento += "\r\n";
                                }
                            }
                            else if (ComandoEntradaRejeitada)       // Entrada Rejeitada
                            {
                                qtdBoletosEntradasRejeitadas += 1;
                                record_g_financeiro.id_financeiro_status = 12; // Cobrança Rejeitada
                                record_g_financeiro.id_processamento = idProcessamentoTemp;
                                db.Entry(record_g_financeiro).State = EntityState.Modified;

                                resultadoProcessamento += record_g_financeiro.id_financeiro.ToString() + ";";
                                resultadoProcessamento += "N " + record_g_financeiro.cnab_nosso_numero.EmptyIfNull().ToString() + ";";
                                resultadoProcessamento += dataOcorrencia.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + ";";
                                resultadoProcessamento += ";";
                                resultadoProcessamento += ";";
                                resultadoProcessamento += "Entrada Rejeitada";
                                resultadoProcessamento += "\r\n";
                            }
                            else if (ComandoCancelamentoConfirmado)       // Baixa Confirmada
                            {

                                if (record_g_financeiro.id_financeiro_status == 15)
                                {
                                    qtdBoletosCanceladosConfirmadasAnteriormente += 1;
                                    resultadoProcessamento += record_g_financeiro.id_financeiro.ToString() + ";";
                                    resultadoProcessamento += "N " + record_g_financeiro.cnab_nosso_numero.EmptyIfNull().ToString() + ";";
                                    resultadoProcessamento += dataOcorrencia.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + ";";
                                    resultadoProcessamento += ";";
                                    resultadoProcessamento += ";";
                                    resultadoProcessamento += "Cancelamento Confirmado Anteriormente";
                                    resultadoProcessamento += "\r\n";

                                }
                                else
                                {
                                    qtdBoletosCanceladosConfirmadas += 1;
                                    record_g_financeiro.id_financeiro_status = 15; // Cancelado
                                    record_g_financeiro.id_processamento = idProcessamentoTemp;
                                    db.Entry(record_g_financeiro).State = EntityState.Modified;

                                    resultadoProcessamento += record_g_financeiro.id_financeiro.ToString() + ";";
                                    resultadoProcessamento += "N " + record_g_financeiro.cnab_nosso_numero.EmptyIfNull().ToString() + ";";
                                    resultadoProcessamento += dataOcorrencia.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + ";";
                                    resultadoProcessamento += ";";
                                    resultadoProcessamento += ";";
                                    resultadoProcessamento += "Cancelamento Confirmado";
                                    resultadoProcessamento += "\r\n";

                                    // Criar o histórico do titulo para ratreabilidade
                                    g_financeiro_historicos record_g_financeiro_historicos = new g_financeiro_historicos();
                                    record_g_financeiro_historicos.id_financeiro = record_g_financeiro.id_financeiro;
                                    record_g_financeiro_historicos.id_financeiro_origem = 8; //Importação Bancária
                                    record_g_financeiro_historicos.id_financeiro_status_inicial = 13; // Canc. Enviado
                                    record_g_financeiro_historicos.id_financeiro_status_final = 15;  // Cancelado
                                    record_g_financeiro_historicos.id_financeiro_remessa = null;
                                    record_g_financeiro_historicos.id_conta_caixa = record_g_conta_caixa.id_conta_caixa;
                                    record_g_financeiro_historicos.id_pagamento_recebimento_tipo = record_g_financeiro.id_pagamento_recebimento_tipo.GetValueOrDefault();
                                    record_g_financeiro_historicos.historico = "CANCELAMENTO CONFIRMADO";
                                    record_g_financeiro_historicos.id_coligada = 1;
                                    record_g_financeiro_historicos.id_filial = 1;
                                    record_g_financeiro_historicos.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                                    record_g_financeiro_historicos.datahora_cadastro = DataHoraAtual;
                                    db.g_financeiro_historicos.Add(record_g_financeiro_historicos);
                                }

                            }
                            else if (ComandoNaoReconhecidoCodigo.Trim().Length > 0) // Comando não reconhecido
                            {
                                qtdBoletosComandoNaoReconhecido += 1;
                                resultadoProcessamento += record_g_financeiro.id_financeiro.ToString() + ";";
                                resultadoProcessamento += "N " + record_g_financeiro.cnab_nosso_numero.EmptyIfNull().ToString() + ";";
                                resultadoProcessamento += dataOcorrencia.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + ";";
                                resultadoProcessamento += ";";
                                resultadoProcessamento += ";";
                                resultadoProcessamento += "Comando nao reconhecido [" + ComandoNaoReconhecidoCodigo + "]";
                                resultadoProcessamento += "\r\n";
                            }
                        }
                        if ((record_g_conta_caixa.financeiro_gc == true) && (record_gc_financeiro_lancamentos != null))
                        {

                            if (ComandoTituloLiquidado)    // Título Liquidado
                            {
                                if (record_gc_financeiro_lancamentos.id_financeiro_status != 3) // O Título não está aberto
                                {
                                    qtdBoletosLiquidadosAnteriormente += 1;
                                    resultadoProcessamento += record_gc_financeiro_lancamentos.id_lancamento.ToString() + ";";
                                    resultadoProcessamento += "N " + record_gc_financeiro_lancamentos.cnab_nosso_numero.EmptyIfNull().ToString() + ";";
                                    resultadoProcessamento += dataOcorrencia.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + ";";
                                    resultadoProcessamento += record_gc_financeiro_lancamentos.cnab_data_credito.GetValueOrDefault().ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + ";";
                                    resultadoProcessamento += record_gc_financeiro_lancamentos.valor_pago.ToString("c2") + ";";
                                        if (record_gc_financeiro_lancamentos.id_financeiro_status == 1) { resultadoProcessamento += "Titulo Liquidado Anteriormente"; }
                                        else if (record_gc_financeiro_lancamentos.id_financeiro_status == 4) { resultadoProcessamento += "Titulo Baixado Anteriormente"; }
                                        else if (record_gc_financeiro_lancamentos.id_financeiro_status == 5) { resultadoProcessamento += "Titulo Cancelado Anteriormente"; }
                                    resultadoProcessamento += "\r\n";
                                }
                                else
                                {
                                    qtdBoletosLiquidados += 1;
                                    record_gc_financeiro_lancamentos.cnab_data_retorno = dataOcorrencia;
                                    record_gc_financeiro_lancamentos.cnab_data_credito = dataCredito;
                                    record_gc_financeiro_lancamentos.cnab_banco_cobrador = bancoRecebedor;
                                    record_gc_financeiro_lancamentos.cnab_agencia_cobradora = agenciaRecebedora;
                                    record_gc_financeiro_lancamentos.valor_pago = valorRecebido;
                                    record_gc_financeiro_lancamentos.id_financeiro_status = 1; // Liquidado
                                    record_gc_financeiro_lancamentos.id_usuario_liquidacao = CachePersister.userIdentity.IdUsuario;
                                    record_gc_financeiro_lancamentos.datahora_liquidacao = LibDateTime.getDataHoraBrasilia();
                                    db.Entry(record_g_financeiro).State = EntityState.Modified;

                                    resultadoProcessamento += record_gc_financeiro_lancamentos.id_lancamento.ToString() + ";";
                                    resultadoProcessamento += "N " + record_gc_financeiro_lancamentos.cnab_nosso_numero.EmptyIfNull().ToString() + ";";
                                    resultadoProcessamento += dataOcorrencia.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + ";";
                                    resultadoProcessamento += dataCredito.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + ";";
                                    resultadoProcessamento += valorRecebido.ToString("c2") + ";";
                                    resultadoProcessamento += "Titulo Liquidado";
                                    resultadoProcessamento += "\r\n";
                                }
                            }
                        }
                        else
                        {
                            if (nossoNumero.Trim().Length > 0)
                            {
                                qtdBoletosNaoLocalizados += 1;
                                resultadoProcessamento += ";";
                                resultadoProcessamento += "N " + nossoNumero.EmptyIfNull().ToString() + ";";
                                resultadoProcessamento += dataOcorrencia.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + ";";
                                resultadoProcessamento += ";";
                                resultadoProcessamento += ";";
                                resultadoProcessamento += "Titulo nao localizado na base de dados";
                                resultadoProcessamento += "\r\n";
                            }
                        }
                    }
                    file.Close();

                    if (!erroProcessamento)
                    {
                        if (!resultadoProcessamento.Trim().Equals(String.Empty))
                        {
                            if (fileNameOrigem.IndexOf(".") > 0)
                            {
                                fileNameOrigem = fileNameOrigem.Substring(0, fileNameOrigem.IndexOf("."));
                            }

                            var fileNameProcessamento = Path.Combine(DirTempFiles, LibDateTime.getDataHoraBrasilia().ToString("yyyyMMddhhmmss") + "_Processamento_" + fileNameOrigem + ".csv");

                            using (StreamWriter w = new StreamWriter(fileNameProcessamento, true, Encoding.UTF8))
                            {
                                w.Write(resultadoProcessamento); // Write the text
                            }


                            // Atualizar o registro do processamento
                            g_processamento record_g_processamento = new g_processamento();
                            record_g_processamento.id_processamento_tipo = 0;
                            record_g_processamento.id_processamento_modulo = 0; // Modulo Relatorio
                            record_g_processamento.id_usuario = CachePersister.userIdentity.IdUsuario;
                            record_g_processamento.datahora_inicio = DataHoraAtual;
                            record_g_processamento.datahora_final = LibDateTime.getDataHoraBrasilia();
                            record_g_processamento.qtd_registros = qtdBoletosLiquidados + qtdBoletosLiquidadosAnteriormente + qtdBoletosEntradasConfirmadas + qtdBoletosEntradasConfirmadasAnteriormente + qtdBoletosEntradasRejeitadas + qtdBoletosCanceladosConfirmadas + qtdBoletosCanceladosConfirmadasAnteriormente + qtdBoletosComandoNaoReconhecido + qtdBoletosNaoLocalizados;
                            record_g_processamento.qtd_reg_ok = qtdBoletosLiquidados + qtdBoletosEntradasConfirmadas + qtdBoletosCanceladosConfirmadas;
                            record_g_processamento.qtd_reg_erro = qtdBoletosComandoNaoReconhecido + qtdBoletosNaoLocalizados + qtdBoletosLiquidadosAnteriormente + qtdBoletosEntradasConfirmadasAnteriormente + qtdBoletosCanceladosConfirmadasAnteriormente + qtdBoletosEntradasRejeitadas;
                            record_g_processamento.processando = false;
                            record_g_processamento.concluido = true;
                            record_g_processamento.pathfile = fileNameProcessamento;
                            record_g_processamento.id_coligada = 1;
                            record_g_processamento.id_filial = 1;
                            db.Entry(record_g_processamento).State = EntityState.Added;
                            db.SaveChanges();
                            idProcessamentoGravado = record_g_processamento.id_processamento.ToString();

                            // Atualização do IdProcessamento
                            if (record_g_conta_caixa.financeiro_gc == true)
                            {
                                var atualizacaoFinanceiro = db.g_financeiro.Where(a => a.id_processamento == idProcessamentoTemp).ToList();
                                atualizacaoFinanceiro.ForEach(b => b.id_processamento = int.Parse(idProcessamentoGravado));
                                db.SaveChanges();
                            }
                        }

                        processado = true;
                        if (msgRetorno.EmptyIfNull().ToString().Trim().Length > 0) { msgRetorno += "<br/><br/>"; };
                        if (qtdBoletosLiquidados > 0) { msgRetorno += qtdBoletosLiquidados.ToString() + " Título(s) - Liquidados<br/>"; };
                        if (qtdBoletosEntradasConfirmadas > 0) { msgRetorno += qtdBoletosEntradasConfirmadas.ToString() + " Título(s) - Entrada Confirmada<br/>"; };
                        if (qtdBoletosEntradasConfirmadasAnteriormente > 0) { msgRetorno += qtdBoletosEntradasConfirmadasAnteriormente.ToString() + " Título(s) - Entrada Confirmada Anteriormente<br/>"; };
                        if (qtdBoletosCanceladosConfirmadas > 0) { msgRetorno += qtdBoletosCanceladosConfirmadas.ToString() + " Título(s) - Cancelamento Confirmado<br/>"; };
                        if (qtdBoletosCanceladosConfirmadasAnteriormente > 0) { msgRetorno += qtdBoletosCanceladosConfirmadasAnteriormente.ToString() + " Título(s) - Cancelamento Confirmado Anteriormente<br/>"; };
                        if (qtdBoletosLiquidadosAnteriormente > 0) { msgRetorno += qtdBoletosLiquidadosAnteriormente.ToString() + " Título(s) - Liquidados Anteriormente<br/>"; };
                        if (qtdBoletosEntradasRejeitadas > 0) { msgRetorno += qtdBoletosEntradasRejeitadas.ToString() + " Título(s) - Entradas Rejeitadas<br/>"; };
                        if (qtdBoletosComandoNaoReconhecido > 0) { msgRetorno += qtdBoletosComandoNaoReconhecido.ToString() + " Título(s) - Comando Não Reconhecido<br/>"; };
                        if (qtdBoletosNaoLocalizados > 0) { msgRetorno += qtdBoletosNaoLocalizados.ToString() + " Título(s) - Não Localizados<br/>"; };
                    }
                    else
                    {
                        processado = false;
                        msgRetorno = msgErro;
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
        #endregion Importar Arquivo - CNAB Boletos
    }
}