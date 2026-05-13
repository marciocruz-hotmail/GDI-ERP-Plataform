using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Web;
using GdiPlataform.Db;
using GdiPlataform.Security;
using GdiPlataform.Lib;

namespace GdiPlataform.Robos.SintegraWS
{
    public class RoboSintegraWS
    {
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
            catch (Exception){}
            finally
            {
                if (SucessoRobo == true)
                {
                    a_yesprodutos_extrato record_a_yesprodutos_extrato = new Db.a_yesprodutos_extrato();
                    record_a_yesprodutos_extrato.id_yesproduto = 1; // SintegraWS
                    record_a_yesprodutos_extrato.log = "Consulta CNPJ " + Documento; // SintegraWS
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
                string URLAuth = "";
                HttpWebRequest webRequest;
                HttpWebResponse webResponse;
                StreamReader responseReader;
                string responseData;
                URLAuth = "https://www.sintegraws.com.br/api/v1/execute-api.php?token=" + TokenAcesso + "&cnpj=" + Documento + "&plugin=RF";
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3;
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                webRequest = WebRequest.Create(URLAuth) as HttpWebRequest;
                webRequest.Method = "GET";
                webRequest.ContentType = "application/json";
                webRequest.Headers.Add("Authorization", "Basic MzI5Y2EyNTktZjUzNS00OGUzLWIzZDctZDE5ZmRjNGIwNDAw");
                responseReader = new StreamReader(webRequest.GetResponse().GetResponseStream());
                webResponse = (HttpWebResponse)webRequest.GetResponse();
                responseData = responseReader.ReadToEnd();
                responseReader.Close();
                webRequest.GetResponse().Close();
                if (webResponse.StatusCode == HttpStatusCode.OK)
                {
                    SucessoRobo = true;
                    RespostaRoboSintegra = responseData;
                }
                else
                {
                    RespostaRoboSintegra = responseData;
                }
            }
            catch (WebException ex)
            {
                SucessoRobo = false;
                string MsgWebException = string.Empty;
                using (var stream = ex.Response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    MsgWebException = reader.ReadToEnd();
                }
                MsgWebException = MsgWebException.Replace("[", "").Replace("]", "").Replace("{", "").Replace("}", "").Replace(",", " - ");
                MsgWebException = "ERRO: [ " + MsgWebException + "]";
                RespostaRoboSintegra = MsgWebException;
            }
            catch (Exception ex)
            {
                SucessoRobo = false;
                String msgErro = "ERRO: [ " + ex.Message.ToString().Trim() + "]";
                RespostaRoboSintegra = msgErro;
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
            catch (Exception){}
            finally
            {
                if (SucessoRobo == true)
                {
                    a_yesprodutos_extrato record_a_yesprodutos_extrato = new Db.a_yesprodutos_extrato();
                    record_a_yesprodutos_extrato.id_yesproduto = 1; // SintegraWS
                    record_a_yesprodutos_extrato.log = "Consulta CPF " + Documento; // SintegraWS
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
                string URLAuth = "";
                HttpWebRequest webRequest;
                HttpWebResponse webResponse;
                StreamReader responseReader;
                string responseData;
                string dadosEnviar = String.Empty;
                URLAuth = "https://www.sintegraws.com.br/api/v1/execute-api.php?token=" + TokenAcesso + "&cpf=" + Documento + "&data-nascimento=" + DataNasc + "&plugin=CPF";
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3;
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                webRequest = WebRequest.Create(URLAuth) as HttpWebRequest;
                webRequest.Method = "GET";
                webRequest.Headers.Add("Authorization", "Basic BBEC024B-753D-41D9-A930-55C9F6678077");
                webRequest.ContentType = "application/json";
                responseReader = new StreamReader(webRequest.GetResponse().GetResponseStream());
                webResponse = (HttpWebResponse)webRequest.GetResponse();
                responseData = responseReader.ReadToEnd();
                responseReader.Close();
                webRequest.GetResponse().Close();
                if (webResponse.StatusCode == HttpStatusCode.OK)
                {
                    SucessoRobo = true;
                    RespostaRoboSintegra = responseData;
                }
                else
                {
                    RespostaRoboSintegra = responseData;
                }
            }
            //catch (WebException ex)
            catch (WebException ex)
            {
                SucessoRobo = false;
                string MsgWebException = string.Empty;
                using (var stream = ex.Response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    MsgWebException = reader.ReadToEnd();
                }
                MsgWebException = MsgWebException.Replace("[", "").Replace("]", "").Replace("{", "").Replace("}", "").Replace(",", " - ");
                MsgWebException = "ERRO: [ " + MsgWebException + "]";
                RespostaRoboSintegra = MsgWebException;
            }
            //catch (Exception ex)
            catch (Exception ex)
            {
                SucessoRobo = false;
                String msgErro = "ERRO: [ " + ex.Message.ToString().Trim() + "]";
                RespostaRoboSintegra = msgErro;
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
            catch (Exception){}
            finally
            {
                if (SucessoRobo == true)
                {
                    a_yesprodutos_extrato record_a_yesprodutos_extrato = new Db.a_yesprodutos_extrato();
                    record_a_yesprodutos_extrato.id_yesproduto = 1; // SintegraWS
                    record_a_yesprodutos_extrato.log = "Consulta SINTEGRA " + Documento; // SintegraWS
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
                string URLAuth = "";
                HttpWebRequest webRequest;
                HttpWebResponse webResponse;
                StreamReader responseReader;
                string responseData;
                string dadosEnviar = String.Empty;
                URLAuth = "https://www.sintegraws.com.br/api/v1/execute-api.php?token=" + TokenAcesso + "&cnpj=" + Documento + "&plugin=ST";
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3;
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                webRequest = WebRequest.Create(URLAuth) as HttpWebRequest;
                webRequest.Method = "GET";
                webRequest.Headers.Add("Authorization", "Basic BBEC024B-753D-41D9-A930-55C9F6678077");
                webRequest.ContentType = "application/json";
                responseReader = new StreamReader(webRequest.GetResponse().GetResponseStream());
                webResponse = (HttpWebResponse)webRequest.GetResponse();
                responseData = responseReader.ReadToEnd();
                responseReader.Close();
                webRequest.GetResponse().Close();
                if (webResponse.StatusCode == HttpStatusCode.OK)
                {
                    SucessoRobo = true;
                    RespostaRoboSintegra = responseData;
                }
                else
                {
                    RespostaRoboSintegra = responseData;
                }
            }
            catch (WebException ex)
            {
                SucessoRobo = false;
                string MsgWebException = string.Empty;
                using (var stream = ex.Response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    MsgWebException = reader.ReadToEnd();
                }
                MsgWebException = MsgWebException.Replace("[", "").Replace("]", "").Replace("{", "").Replace("}", "").Replace(",", " - ");
                MsgWebException = "ERRO: [ " + MsgWebException + "]";
                RespostaRoboSintegra = MsgWebException;
            }
            catch (Exception ex)
            {
                SucessoRobo = false;
                String msgErro = "ERRO: [ " + ex.Message.ToString().Trim() + "]";
                RespostaRoboSintegra = msgErro;
            }
        }
        #endregion
    }
}