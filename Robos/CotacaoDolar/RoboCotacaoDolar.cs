using GdiPlataform.Db;
using GdiPlataform.Lib;
using GdiPlataform.Security;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Data.Entity;
using System.Net.Http;
using System.Threading;

namespace GdiPlataform.Robos.CotacaoDolar
{
    public class RoboCotacaoDolar
    {
        private static readonly HttpClient _httpClient = new HttpClient();

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
            try
            {
                string url = "https://economia.awesomeapi.com.br/last/USD-BRL";
                var response = _httpClient.GetAsync(url).GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode) return;

                string responseData = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                var data = (JObject)JsonConvert.DeserializeObject(responseData);
                string CotacaoStrRobo = data["USDBRL"]["bid"].Value<string>();
                CotacaoStrRobo = CotacaoStrRobo.Trim().Replace(" ", "").Replace(",", "").Replace(".", ",");
                Decimal.TryParse(CotacaoStrRobo, out CotacaoDolarDiaAtualizada);
                if (CotacaoDolarDiaAtualizada < 0 || CotacaoDolarDiaAtualizada > 10)
                    CotacaoDolarDiaAtualizada = 0;
            }
            catch (Exception e)
            {
                string msg = e.Message;
            }
        }

        private void ThreadCotacaoDolarDiaUol()
        {
            try
            {
                string url = "https://api.cotacoes.uol.com/currency/intraday/list?currency=1&fields=bidvalue";
                var response = _httpClient.GetAsync(url).GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode) return;

                string responseData = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                JObject JRoot = JObject.Parse(responseData);
                if (JRoot.SelectToken("docs") != null)
                {
                    JArray JListaCotacoes = JArray.Parse(JRoot.SelectToken("docs").ToString());
                    if (JListaCotacoes.Count > 0)
                    {
                        string CotacaoStrRobo = JListaCotacoes[0]["bidvalue"].ToString().Trim().Replace(" ", "");
                        Decimal.TryParse(CotacaoStrRobo, out CotacaoDolarDiaAtualizada);
                        if (CotacaoDolarDiaAtualizada < 0 || CotacaoDolarDiaAtualizada > 10)
                            CotacaoDolarDiaAtualizada = 0;
                        else
                            SucessoRobo = true;
                    }
                }
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
            }
        }
    }
}
