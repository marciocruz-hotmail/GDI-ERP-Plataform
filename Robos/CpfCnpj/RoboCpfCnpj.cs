using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;
using GdiPlataform.Db;
using GdiPlataform.Models;
using GdiPlataform.Security;
using GdiPlataform.Lib;

namespace GdiPlataform.Robos.CpfCnpj
{
    public class RoboCpfCnpj
    {
        private GdiPlataformEntities db;
        ModelApiResponse RetornoRobo;
        public RoboCpfCnpj()
        {
            RetornoRobo = new ModelApiResponse();
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        #region GetDadosCNPJ
        public ModelApiResponse GetDadosCNPJ(String Documento)
        {
            RetornoRobo.SucessoRobo = false;
            RetornoRobo.MsgErro = string.Empty;
            RetornoRobo.RetornoRobo = string.Empty;
            try
            {
                int TempoEsperaMaximo = 30;
                int TempoEsperaAtual = 0;
                Thread th = new Thread(() => ThreadDadosCNPJ(Documento));
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
                if (RetornoRobo.SucessoRobo == true)
                {
                    a_yesprodutos_extrato record_a_yesprodutos_extrato = new Db.a_yesprodutos_extrato();
                    record_a_yesprodutos_extrato.id_yesproduto = 7; // CpfCnpj
                    record_a_yesprodutos_extrato.log = "Consulta CNPJ " + Documento; // CpfCnpj
                    record_a_yesprodutos_extrato.datahora_execucao = LibDateTime.getDataHoraBrasilia();
                    record_a_yesprodutos_extrato.id_usuario_execucao = CachePersister.userIdentity.IdUsuario; ;
                    db.Entry(record_a_yesprodutos_extrato).State = EntityState.Added;
                    db.SaveChanges();
                }
            }
            return RetornoRobo;
        }

        private void ThreadDadosCNPJ(String CNPJ)
        {
            RetornoRobo.SucessoRobo = false;
            RetornoRobo.MsgErro = string.Empty;
            RetornoRobo.RetornoRobo = string.Empty;
            try
            {
                string TokenAcesso = "ed7cfdc9b691f758edcd063719101d55"; // GDI
                String PacoteConsulta = "10"; // CNPJ C
                string URLAuth = "";
                HttpWebRequest webRequest;
                HttpWebResponse webResponse;
                StreamReader responseReader;
                string responseData;
                URLAuth = "https://api.cpfcnpj.com.br/" + TokenAcesso + "/" + PacoteConsulta + "/" + CNPJ + "/0";
                ServicePointManager.Expect100Continue = false;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3;
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                webRequest = WebRequest.Create(URLAuth) as HttpWebRequest;
                webRequest.Method = "GET";
                webRequest.ContentType = "application/json";
                responseReader = new StreamReader(webRequest.GetResponse().GetResponseStream());
                webResponse = (HttpWebResponse)webRequest.GetResponse();
                responseData = responseReader.ReadToEnd();
                responseReader.Close();
                webRequest.GetResponse().Close();
                if (webResponse.StatusCode == HttpStatusCode.OK)
                {
                    RetornoRobo.SucessoRobo = true;
                    RetornoRobo.RetornoRobo = responseData;
                }
                else
                {
                    RetornoRobo.SucessoRobo = false;
                    RetornoRobo.MsgErro = responseData;
                }
            }
            catch (WebException ex)
            {
                string MsgWebException = string.Empty;
                RetornoRobo.SucessoRobo = false;
                try { MsgWebException += ex.Message; } catch (Exception) { };
                try
                {
                    using (var stream = ex.Response.GetResponseStream())
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        MsgWebException = reader.ReadToEnd();
                    }
                }
                catch (Exception) { };
                MsgWebException = MsgWebException.Replace("[", "").Replace("]", "").Replace("{", "").Replace("}", "").Replace(",", " - ");
                RetornoRobo.MsgErro = "ERRO: [ " + MsgWebException + "]";
            }
            catch (Exception ex)
            {
                RetornoRobo.SucessoRobo = false;
                RetornoRobo.MsgErro = "ERRO: [ " + ex.Message.ToString() + "]";
            }
        }
        #endregion

        #region GetDadosCPF
        public ModelApiResponse GetDadosCPF(String Documento)
        {
            RetornoRobo.SucessoRobo = false;
            RetornoRobo.MsgErro = string.Empty;
            RetornoRobo.RetornoRobo = string.Empty;
            try
            {
                int TempoEsperaMaximo = 30;
                int TempoEsperaAtual = 0;
                Thread th = new Thread(() => ThreadDadosCPF(Documento));
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
                if (RetornoRobo.SucessoRobo == true)
                {
                    a_yesprodutos_extrato record_a_yesprodutos_extrato = new Db.a_yesprodutos_extrato();
                    record_a_yesprodutos_extrato.id_yesproduto = 7; // CpfCnpj - CPF D
                    record_a_yesprodutos_extrato.log = "Consulta CPF " + Documento; // CpfCnpj
                    record_a_yesprodutos_extrato.datahora_execucao = LibDateTime.getDataHoraBrasilia();
                    record_a_yesprodutos_extrato.id_usuario_execucao = CachePersister.userIdentity.IdUsuario; ;
                    db.Entry(record_a_yesprodutos_extrato).State = EntityState.Added;
                    db.SaveChanges();
                }
            }
            return RetornoRobo;
        }

        private void ThreadDadosCPF(String CPF)
        {
            RetornoRobo.SucessoRobo = false;
            RetornoRobo.MsgErro = string.Empty;
            RetornoRobo.RetornoRobo = string.Empty;
            try
            {
                string TokenAcesso = "ed7cfdc9b691f758edcd063719101d55"; // GDI
                String PacoteConsulta = "8"; // CPF
                string URLAuth = "";
                HttpWebRequest webRequest;
                HttpWebResponse webResponse;
                StreamReader responseReader;
                string responseData;
                URLAuth = "https://api.cpfcnpj.com.br/" + TokenAcesso + "/" + PacoteConsulta + "/" + CPF + "/0";
                ServicePointManager.Expect100Continue = false;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3;
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                webRequest = WebRequest.Create(URLAuth) as HttpWebRequest;
                webRequest.Method = "GET";
                webRequest.ContentType = "application/json";
                responseReader = new StreamReader(webRequest.GetResponse().GetResponseStream());
                webResponse = (HttpWebResponse)webRequest.GetResponse();
                responseData = responseReader.ReadToEnd();
                responseReader.Close();
                webRequest.GetResponse().Close();
                if (webResponse.StatusCode == HttpStatusCode.OK)
                {
                    RetornoRobo.SucessoRobo = true;
                    RetornoRobo.RetornoRobo = responseData;
                }
                else
                {
                    RetornoRobo.SucessoRobo = false;
                    RetornoRobo.MsgErro = responseData;
                }
            }
            catch (WebException ex)
            {
                string MsgWebException = string.Empty;
                RetornoRobo.SucessoRobo = false;
                try { MsgWebException += ex.Message; } catch (Exception) { };
                try
                {
                    using (var stream = ex.Response.GetResponseStream())
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        MsgWebException = reader.ReadToEnd();
                    }
                }
                catch (Exception) { };
                MsgWebException = MsgWebException.Replace("[", "").Replace("]", "").Replace("{", "").Replace("}", "").Replace(",", " - ");
                RetornoRobo.MsgErro = "ERRO: [ " + MsgWebException + "]";
            }
            catch (Exception ex)
            {
                RetornoRobo.SucessoRobo = false;
                RetornoRobo.MsgErro = "ERRO: [ " + ex.Message.EmptyIfNull().ToString() + "]";
            }
        }
        #endregion
    }
}