using DocumentFormat.OpenXml.Drawing.Diagrams;
using GdiPlataform.Db;
using GdiPlataform.Lib;
using GdiPlataform.Robos.Whatsapp;
using GdiPlataform.Security;
using NPOI.SS.Formula.Functions;
using System;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using static NPOI.HSSF.Util.HSSFColor;

namespace GdiPlataform.Controllers
{
    /// <summary>
    /// API para disparo de jobs em background (estilo job server).
    /// Não utiliza autenticação de sessão; o chamador deve enviar os parâmetros corretos (Key).
    /// O processamento é assíncrono: a resposta retorna imediatamente (202 Accepted) e o job roda em background,
    /// sem depender da conexão do cliente e sem timeout de requisição.
    /// </summary>
    [AllowAnonymous]
    [RoutePrefix("api/JobServer")]
    public class JobServerController : ApiController
    {
        public string hostHeader = string.Empty;
        public string dominio = string.Empty;
        public string host = string.Empty;
        public string database = string.Empty;

        /// <summary>
        /// Dispara a execução do job em background.
        /// POST api/JobServer/Run
        /// </summary>
        /// <param name="request">Key (obrigatória se JobServer:Key estiver no Web.config), JobName e Parameters opcionais.</param>
        /// <returns>202 Accepted com mensagem e identificador, ou 400 em caso de parâmetros inválidos.</returns>
        [HttpPost]
        [Route("Run")]
        public IHttpActionResult Run([FromBody] JobRunRequest request)
        {
            hostHeader = Request.Headers.Host ?? "";
            dominio = hostHeader.ToLower().Trim();
            host = dominio.Replace("http://", "").Replace("https://", "").Replace("www.", "").Trim();
            if (host.IndexOf(":") >= 0) { host = host.Substring(0, host.IndexOf(":")); }
            var index = host.IndexOf(".");
            if (index < 0) { host = host.Trim(); }
            else { host = host.Substring(0, index); };

            var tenant = UserIdentityController.SetTenants()
                .FirstOrDefault(t => t.subDominio.Equals(host, StringComparison.OrdinalIgnoreCase));
            if (tenant != null) database = tenant.database;

            if (request == null)
                return BadRequest("Corpo da requisição inválido.");

            string configKey = ConfigurationManager.AppSettings["JobServer:Key"];
            if (!string.IsNullOrWhiteSpace(configKey))
            {
                if (string.IsNullOrWhiteSpace(request.Key))
                    return BadRequest("Parâmetro Key é obrigatório.");
                if (request.Key.Trim() != configKey.Trim())
                    return BadRequest("Key inválida.");
            }

            string jobId = Guid.NewGuid().ToString("N").Substring(0, 8);
            string jobName = request?.JobName ?? "Default";
            string parameters = request?.Parameters ?? "";

            HostingEnvironment.QueueBackgroundWorkItem(cancellationToken =>
            {
                try
                {
                    RunJobInBackground(jobId, jobName, parameters, cancellationToken, database);
                }
                catch (Exception ex)
                {
                    LibLogger.Error($"[JobServer] Job {jobId} ({jobName}) erro no background", ex);
                }
            });

            return ResponseMessage(Request.CreateResponse(HttpStatusCode.Accepted, new
            {
                accepted = true,
                jobId,
                message = "Job enfileirado e será processado em background.",
                jobName
            }));
        }

        /// <summary>
        /// Ponto de extensão: implemente aqui a lógica do job (ou delegue por JobName).
        /// Executado em background, sem timeout de requisição.
        /// </summary>
        private static void RunJobInBackground(string jobId, string jobName, string parameters, System.Threading.CancellationToken cancellationToken, string database)
        {
            LibLogger.Info($"[JobServer] Início job {jobId} | Nome: {jobName} | Params: {parameters}");

            if (cancellationToken.IsCancellationRequested)
                return;

            // Exemplo de roteamento por nome do job
            switch (jobName)
            {
                case "GerarRelatorioPosicaoEstoqueAtual":
                    GerarRelatorioPosicaoEstoqueAtual(jobId, parameters, cancellationToken, database);
                    break;

                case "EnviarResumoGerencialWhatsApp":
                    RoboWhatsAppGerencial.EnviarResumoGerencial(jobId, parameters, cancellationToken, database);
                    break;

                default:
                    // Por ora apenas simula um processamento genérico.
                    System.Threading.Thread.Sleep(100);
                    break;
            }
        }

        /// <summary>
        /// Exemplo de job: geração de relatório de posição de estoque atual.
        /// Aqui você deverá implementar a regra real (acesso a banco, geração de arquivo, envio de e-mail, etc.).
        /// </summary>
        private static void GerarRelatorioPosicaoEstoqueAtual(string jobId, string parameters, System.Threading.CancellationToken cancellationToken, string database)
        {
            try
            {
                LibLogger.Info($"[JobServer] GerarRelatorioPosicaoEstoqueAtual iniciando | JobId: {jobId} | Params: {parameters}");

                using (var db = new GdiPlataformEntities(database))
                {
                    g_jobserver RecordJobServer = new g_jobserver();
                    RecordJobServer.job_id = jobId;
                    RecordJobServer.job_parameters = parameters;
                    RecordJobServer.job_name = "GerarRelatorioPosicaoEstoqueAtual";
                    RecordJobServer.datahora_inicio = DateTime.Now;
                    db.g_jobserver.Add(RecordJobServer);
                    db.SaveChanges();

                    String SqlDeleteSaldo = "delete from gc_estoque_competencia_saldo where id_estoque_competencia = 1 and id_estoque_competencia_saldo > 0";
                    int QtdRowsDeleteSaldo = LibDB.dbQueryExec(SqlDeleteSaldo, db);


                    String SqlAtualizaSaldo = "INSERT INTO gc_estoque_competencia_saldo " +
                                                "( "+
                                                "    id_estoque_competencia, " +
                                                "    id_produto, " +
                                                "    saldo_01_disponivel, " +
                                                "    saldo_01_consignado, " +
                                                "    saldo_01_reservado, " +
                                                "    saldo_01_separado, " +
                                                "    saldo_01_quarentena, " +
                                                "    saldo_03_disponivel, " +
                                                "    saldo_03_consignado, " +
                                                "    saldo_03_reservado, " +
                                                "    saldo_03_separado, " +
                                                "    saldo_03_quarentena, " +
                                                "    fob1_dollar) " +
                                                "SELECT " +
                                                "    1, " +
                                                "    id_produto, " +
                                                "    ISNULL(saldo_01_disponivel, 0), " +
                                                "    ISNULL(saldo_01_consignado, 0), " +
                                                "    ISNULL(saldo_01_reservado, 0), " +
                                                "    ISNULL(saldo_01_separado, 0), " +
                                                "    ISNULL(saldo_01_quarentena, 0), " +
                                                "    ISNULL(saldo_03_disponivel, 0), " +
                                                "    ISNULL(saldo_03_consignado, 0), " +
                                                "    ISNULL(saldo_03_reservado, 0), " +
                                                "    ISNULL(saldo_03_separado, 0), " +
                                                "    ISNULL(saldo_03_quarentena, 0), " +
                                                "    fob1_dollar " +
                                                "FROM g_produtos WHERE(g_produtos.saldo_01_disponivel > 0 or g_produtos.saldo_03_disponivel > 0)";
                    int QtdRowsAtualizado = LibDB.dbQueryExec(SqlAtualizaSaldo, db);

                    LibLogger.Info($"[JobServer] GerarRelatorioPosicaoEstoqueAtual: delete={QtdRowsDeleteSaldo} insert={QtdRowsAtualizado} JobId={jobId}");

                    if (cancellationToken.IsCancellationRequested)
                        return;

                    // TODO: implementar a lógica real da geração do relatório de posição de estoque atual.
                    System.Threading.Thread.Sleep(100);
                    LibLogger.Info($"[JobServer] GerarRelatorioPosicaoEstoqueAtual concluído JobId={jobId}");

                    RecordJobServer.qtd_rows_sucesso = QtdRowsDeleteSaldo + QtdRowsAtualizado;
                    RecordJobServer.concluido = true;
                    RecordJobServer.datahora_fim = DateTime.Now;
                    db.Entry(RecordJobServer).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                LibLogger.Error($"[JobServer] GerarRelatorioPosicaoEstoqueAtual erro JobId={jobId}", ex);
            }
        }
    }
}
