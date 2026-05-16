using GdiPlataform.Db;
using GdiPlataform.Lib;
using GdiPlataform.Models;
using GdiPlataform.Security;
using System;
using System.Data.Entity;
using System.Net.Http;
using System.Threading;

namespace GdiPlataform.Robos.CpfCnpj
{
    public class RoboCpfCnpj
    {
        private static readonly HttpClient _httpClient = new HttpClient();

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
                    record_a_yesprodutos_extrato.log = "Consulta CNPJ " + Documento;
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
                string url = "https://api.cpfcnpj.com.br/" + TokenAcesso + "/" + PacoteConsulta + "/" + CNPJ + "/0";

                var response = _httpClient.GetAsync(url).GetAwaiter().GetResult();
                string responseData = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (response.IsSuccessStatusCode)
                {
                    RetornoRobo.SucessoRobo = true;
                    RetornoRobo.RetornoRobo = responseData;
                }
                else
                {
                    RetornoRobo.SucessoRobo = false;
                    string msgErro = responseData.Replace("[", "").Replace("]", "").Replace("{", "").Replace("}", "").Replace(",", " - ");
                    RetornoRobo.MsgErro = "ERRO: [ " + msgErro + "]";
                }
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
                    record_a_yesprodutos_extrato.log = "Consulta CPF " + Documento;
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
                string url = "https://api.cpfcnpj.com.br/" + TokenAcesso + "/" + PacoteConsulta + "/" + CPF + "/0";

                var response = _httpClient.GetAsync(url).GetAwaiter().GetResult();
                string responseData = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (response.IsSuccessStatusCode)
                {
                    RetornoRobo.SucessoRobo = true;
                    RetornoRobo.RetornoRobo = responseData;
                }
                else
                {
                    RetornoRobo.SucessoRobo = false;
                    string msgErro = responseData.Replace("[", "").Replace("]", "").Replace("{", "").Replace("}", "").Replace(",", " - ");
                    RetornoRobo.MsgErro = "ERRO: [ " + msgErro + "]";
                }
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
