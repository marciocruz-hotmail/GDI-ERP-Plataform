using GdiPlataform.Db;
using GdiPlataform.Lib;
using GdiPlataform.Security;
using System;
using System.Data.Entity;
using System.Net.Http;
using System.Threading;

namespace GdiPlataform.Robos.SintegraWS
{
    public class RoboSintegraWS
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        private GdiPlataformEntities db;
        String RespostaRoboSintegra;
        Boolean SucessoRobo;

        public RoboSintegraWS()
        {
            SucessoRobo = false;
            RespostaRoboSintegra = string.Empty;
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        #region GetDadosReceitaFederalCNPJ
        public String GetDadosReceitaFederalCNPJ(String Documento)
        {
            SucessoRobo = false;
            RespostaRoboSintegra = string.Empty;
            try
            {
                int TempoEsperaMaximo = 20;
                int TempoEsperaAtual = 0;
                Thread th = new Thread(() => ThreadDadosReceitaFederalCNPJ(Documento));
                th.Start();
                while (th.IsAlive)
                {
                    Thread.Sleep(1000);
                    TempoEsperaAtual += 1;
                    if (TempoEsperaAtual >= TempoEsperaMaximo)
                    {
                        th.Abort();
                        break;
                    }
                }
            }
            catch (Exception) { }
            finally
            {
                if (SucessoRobo == true)
                {
                    a_yesprodutos_extrato record_a_yesprodutos_extrato = new Db.a_yesprodutos_extrato();
                    record_a_yesprodutos_extrato.id_yesproduto = 1; // SintegraWS
                    record_a_yesprodutos_extrato.log = "Consulta CNPJ " + Documento;
                    record_a_yesprodutos_extrato.datahora_execucao = LibDateTime.getDataHoraBrasilia();
                    record_a_yesprodutos_extrato.id_usuario_execucao = CachePersister.userIdentity.IdUsuario; ;
                    db.Entry(record_a_yesprodutos_extrato).State = EntityState.Added;
                    db.SaveChanges();
                }
            }
            return RespostaRoboSintegra;
        }

        private void ThreadDadosReceitaFederalCNPJ(String Documento)
        {
            SucessoRobo = false;
            RespostaRoboSintegra = string.Empty;
            try
            {
                string TokenAcesso = "BBEC024B-753D-41D9-A930-55C9F6678077"; // GDI
                string url = "https://www.sintegraws.com.br/api/v1/execute-api.php?token=" + TokenAcesso + "&cnpj=" + Documento + "&plugin=RF";

                var requestMsg = new HttpRequestMessage(HttpMethod.Get, url);
                requestMsg.Headers.TryAddWithoutValidation("Authorization", "Basic MzI5Y2EyNTktZjUzNS00OGUzLWIzZDctZDE5ZmRjNGIwNDAw");
                var response = _httpClient.SendAsync(requestMsg).GetAwaiter().GetResult();
                string responseData = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (response.IsSuccessStatusCode)
                {
                    SucessoRobo = true;
                    RespostaRoboSintegra = responseData;
                }
                else
                {
                    string msgErro = responseData.Replace("[", "").Replace("]", "").Replace("{", "").Replace("}", "").Replace(",", " - ");
                    RespostaRoboSintegra = "ERRO: [ " + msgErro + "]";
                }
            }
            catch (Exception ex)
            {
                SucessoRobo = false;
                RespostaRoboSintegra = "ERRO: [ " + ex.Message.ToString().Trim() + "]";
            }
        }
        #endregion

        #region GetDadosReceitaFederalCPF
        public String GetDadosReceitaFederalCPF(String Documento, String DataNasc)
        {
            SucessoRobo = false;
            RespostaRoboSintegra = string.Empty;
            try
            {
                int TempoEsperaMaximo = 20;
                int TempoEsperaAtual = 0;
                Thread th = new Thread(() => ThreadDadosReceitaFederalCPF(Documento, DataNasc));
                th.Start();
                while (th.IsAlive)
                {
                    Thread.Sleep(1000);
                    TempoEsperaAtual += 1;
                    if (TempoEsperaAtual >= TempoEsperaMaximo)
                    {
                        th.Abort();
                        break;
                    }
                }
            }
            catch (Exception) { }
            finally
            {
                if (SucessoRobo == true)
                {
                    a_yesprodutos_extrato record_a_yesprodutos_extrato = new Db.a_yesprodutos_extrato();
                    record_a_yesprodutos_extrato.id_yesproduto = 1; // SintegraWS
                    record_a_yesprodutos_extrato.log = "Consulta CPF " + Documento;
                    record_a_yesprodutos_extrato.datahora_execucao = LibDateTime.getDataHoraBrasilia();
                    record_a_yesprodutos_extrato.id_usuario_execucao = CachePersister.userIdentity.IdUsuario; ;
                    db.Entry(record_a_yesprodutos_extrato).State = EntityState.Added;
                    db.SaveChanges();
                }
            }

            return RespostaRoboSintegra;
        }

        private void ThreadDadosReceitaFederalCPF(String Documento, String DataNasc)
        {
            SucessoRobo = false;
            RespostaRoboSintegra = string.Empty;
            try
            {
                string TokenAcesso = "BBEC024B-753D-41D9-A930-55C9F6678077"; // GDI
                string url = "https://www.sintegraws.com.br/api/v1/execute-api.php?token=" + TokenAcesso + "&cpf=" + Documento + "&data-nascimento=" + DataNasc + "&plugin=CPF";

                var requestMsg = new HttpRequestMessage(HttpMethod.Get, url);
                requestMsg.Headers.TryAddWithoutValidation("Authorization", "Basic BBEC024B-753D-41D9-A930-55C9F6678077");
                var response = _httpClient.SendAsync(requestMsg).GetAwaiter().GetResult();
                string responseData = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (response.IsSuccessStatusCode)
                {
                    SucessoRobo = true;
                    RespostaRoboSintegra = responseData;
                }
                else
                {
                    string msgErro = responseData.Replace("[", "").Replace("]", "").Replace("{", "").Replace("}", "").Replace(",", " - ");
                    RespostaRoboSintegra = "ERRO: [ " + msgErro + "]";
                }
            }
            catch (Exception ex)
            {
                SucessoRobo = false;
                RespostaRoboSintegra = "ERRO: [ " + ex.Message.ToString().Trim() + "]";
            }
        }
        #endregion

        #region GetDadosSintegraCNPJ
        public String GetDadosSintegraCNPJ(String Documento)
        {
            SucessoRobo = false;
            RespostaRoboSintegra = string.Empty;
            try
            {
                int TempoEsperaMaximo = 20;
                int TempoEsperaAtual = 0;
                Thread th = new Thread(() => ThreadDadosSintegraCNPJ(Documento));
                th.Start();
                while (th.IsAlive)
                {
                    Thread.Sleep(1000);
                    TempoEsperaAtual += 1;
                    if (TempoEsperaAtual >= TempoEsperaMaximo)
                    {
                        th.Abort();
                        break;
                    }
                }
            }
            catch (Exception) { }
            finally
            {
                if (SucessoRobo == true)
                {
                    a_yesprodutos_extrato record_a_yesprodutos_extrato = new Db.a_yesprodutos_extrato();
                    record_a_yesprodutos_extrato.id_yesproduto = 1; // SintegraWS
                    record_a_yesprodutos_extrato.log = "Consulta SINTEGRA " + Documento;
                    record_a_yesprodutos_extrato.datahora_execucao = LibDateTime.getDataHoraBrasilia();
                    record_a_yesprodutos_extrato.id_usuario_execucao = CachePersister.userIdentity.IdUsuario; ;
                    db.Entry(record_a_yesprodutos_extrato).State = EntityState.Added;
                    db.SaveChanges();
                }
            }
            return RespostaRoboSintegra;
        }

        private void ThreadDadosSintegraCNPJ(String Documento)
        {
            SucessoRobo = false;
            try
            {
                string TokenAcesso = "BBEC024B-753D-41D9-A930-55C9F6678077"; // GDI
                string url = "https://www.sintegraws.com.br/api/v1/execute-api.php?token=" + TokenAcesso + "&cnpj=" + Documento + "&plugin=ST";

                var requestMsg = new HttpRequestMessage(HttpMethod.Get, url);
                requestMsg.Headers.TryAddWithoutValidation("Authorization", "Basic BBEC024B-753D-41D9-A930-55C9F6678077");
                var response = _httpClient.SendAsync(requestMsg).GetAwaiter().GetResult();
                string responseData = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (response.IsSuccessStatusCode)
                {
                    SucessoRobo = true;
                    RespostaRoboSintegra = responseData;
                }
                else
                {
                    string msgErro = responseData.Replace("[", "").Replace("]", "").Replace("{", "").Replace("}", "").Replace(",", " - ");
                    RespostaRoboSintegra = "ERRO: [ " + msgErro + "]";
                }
            }
            catch (Exception ex)
            {
                SucessoRobo = false;
                RespostaRoboSintegra = "ERRO: [ " + ex.Message.ToString().Trim() + "]";
            }
        }
        #endregion
    }
}
