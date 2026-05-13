using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web;
using GdiPlataform.Db;
using GdiPlataform.Security;
using GdiPlataform.Lib;

namespace GdiPlataform.Robos.CotacaoDolar
{
    public class RoboCotacaoDolar
    {
        private GdiPlataformEntities db;
        private Decimal CotacaoDolarDiaAtualizada;
        private Boolean SucessoRobo;
        public RoboCotacaoDolar()
        {
            CotacaoDolarDiaAtualizada = 0;
            SucessoRobo = false;
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        public Decimal GetCotacaoDolarDia()
        {
            SucessoRobo = false;
            try
            {
                int TempoEsperaMaximo = 20;
                int TempoEsperaAtual = 0;
                Thread th = new Thread(() => ThreadCotacaoDolarDiaUol());
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
            catch (Exception)
            {
            }
            finally
            {
                if (SucessoRobo == true)
                {
                    a_yesprodutos_extrato record_a_yesprodutos_extrato = new Db.a_yesprodutos_extrato();
                    record_a_yesprodutos_extrato.id_yesproduto = 6; // CotaçãoDolar
                    record_a_yesprodutos_extrato.log = "Cotação Dollar " + CotacaoDolarDiaAtualizada.ToString("0.0000");
                    record_a_yesprodutos_extrato.datahora_execucao = LibDateTime.getDataHoraBrasilia();
                    record_a_yesprodutos_extrato.id_usuario_execucao = CachePersister.userIdentity.IdUsuario; ;
                    db.Entry(record_a_yesprodutos_extrato).State = EntityState.Added;
                    db.SaveChanges();
                }
            }
            return CotacaoDolarDiaAtualizada;
        }

        private void ThreadCotacaoDolarDiaAwesomeApi()
        {
            String RetornoRobo = string.Empty;
            try
            {
                string URLAuth = "";
                HttpWebRequest webRequest;
                HttpWebResponse webResponse;
                StreamReader responseReader;
                string responseData;
                URLAuth = "https://economia.awesomeapi.com.br/last/USD-BRL";
                ServicePointManager.Expect100Continue = false;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3;
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                webRequest = WebRequest.Create(URLAuth) as HttpWebRequest;
                webRequest.Method = "GET";
                webRequest.ContentType = "application/json";
                webRequest.Headers.Add("Authorization", "Basic");
                responseReader = new StreamReader(webRequest.GetResponse().GetResponseStream());
                webResponse = (HttpWebResponse)webRequest.GetResponse();
                responseData = responseReader.ReadToEnd();
                responseReader.Close();
                webRequest.GetResponse().Close();
                if (webResponse.StatusCode == HttpStatusCode.OK)
                {
                    RetornoRobo = responseData;
                    var data = (JObject)JsonConvert.DeserializeObject(RetornoRobo);
                    try
                    {
                        string CotacaoStrRobo = data["USDBRL"]["bid"].Value<string>();
                        CotacaoStrRobo = CotacaoStrRobo.Trim().Replace(" ", "");
                        CotacaoStrRobo = CotacaoStrRobo.Trim().Replace(",", "").Trim();
                        CotacaoStrRobo = CotacaoStrRobo.Trim().Replace(".", ",").Trim();
                        Decimal.TryParse(CotacaoStrRobo, out CotacaoDolarDiaAtualizada);
                        if (CotacaoDolarDiaAtualizada < 0 || CotacaoDolarDiaAtualizada > 10) { CotacaoDolarDiaAtualizada = 0; };
                    }
                    catch (Exception) { };
                }
            }
            catch (Exception e)
            {
                string msg = e.Message.DefaultIfEmpty().ToString();
            }
        }

        private void ThreadCotacaoDolarDiaUol()
        {
            String RetornoRobo = string.Empty;
            try
            {
                string URLAuth = "";
                HttpWebRequest webRequest;
                HttpWebResponse webResponse;
                StreamReader responseReader;
                string responseData;
                URLAuth = "https://api.cotacoes.uol.com/currency/intraday/list?currency=1&fields=bidvalue";
                ServicePointManager.Expect100Continue = false;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3;
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                webRequest = WebRequest.Create(URLAuth) as HttpWebRequest;
                webRequest.Method = "GET";
                webRequest.ContentType = "application/json";
                webRequest.Headers.Add("Authorization", "Basic");
                responseReader = new StreamReader(webRequest.GetResponse().GetResponseStream());
                webResponse = (HttpWebResponse)webRequest.GetResponse();
                responseData = responseReader.ReadToEnd();
                responseReader.Close();
                webRequest.GetResponse().Close();
                if (webResponse.StatusCode == HttpStatusCode.OK)
                {
                    RetornoRobo = responseData;
                    var data = (JObject)JsonConvert.DeserializeObject(RetornoRobo);
                    try
                    {
                        JObject JRoot = JObject.Parse(RetornoRobo);
                        if (JRoot.SelectToken("docs") != null)
                        {
                            JArray JListaCotacoes = JArray.Parse(JRoot.SelectToken("docs").ToString());
                            if (JListaCotacoes.Count() > 0)
                            {
                                string CotacaoStrRobo = JListaCotacoes[0]["bidvalue"].ToString();
                                CotacaoStrRobo = CotacaoStrRobo.Trim().Replace(" ", "");
                                Decimal.TryParse(CotacaoStrRobo, out CotacaoDolarDiaAtualizada);
                                if (CotacaoDolarDiaAtualizada < 0 || CotacaoDolarDiaAtualizada > 10) { CotacaoDolarDiaAtualizada = 0; } else { SucessoRobo = true; };
                            }
                        }
                    }
                    catch (Exception) { };
                }
            }
            catch (Exception ex)
            {
                string msg = ex.Message.DefaultIfEmpty().ToString();
            }
        }


    }
}