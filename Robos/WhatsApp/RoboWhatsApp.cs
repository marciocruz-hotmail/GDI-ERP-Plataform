using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using GdiPlataform.Db;
using GdiPlataform.Security;
using GdiPlataform.Lib;

namespace GdiPlataform.Robos.Whatsapp
{
    public class RoboWhatsApp
    {
        private GdiPlataformEntities db;

        #region Create Robô
        public RoboWhatsApp()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }
        #endregion

        #region Enviar Texto Simples WhatsApp
        public bool EnviarTextoSimplesWhatsApp(String NotificacaoCelular, String NotificacaoMensagem)
        {
            bool Sucesso = false;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();

            try
            {
                String INSTANCE_ID = "3D933D08BC343053ACA9CE82F470C0C7";
                String INSTANCE_TOKEN = "ACE6A8DDC0AF992D50FDD712";
                String CLIENT_TOKEN = "F473838c008c242fc9ae05bf7b4727c37S";
                String URL = "https://api.z-api.io/instances/" + INSTANCE_ID + "/token/" + INSTANCE_TOKEN;
                String Resource = "/send-text";
                var client = new RestClient(URL);
                var request = new RestRequest(Resource, Method.Post);
                request.AddHeader("content-type", "application/json");
                request.AddHeader("client-token", CLIENT_TOKEN);
                request.AddParameter("application/json", "{\"phone\": \"" + NotificacaoCelular + "\", \"message\": \"" + NotificacaoMensagem + "\"}", ParameterType.RequestBody);
                var response = client.Execute(request);
                var obj = JObject.Parse(response.Content);

                // Criar o log da utilização
                String LogNotificacao = "WhatsApp | Destinatário: " + NotificacaoCelular + " | Notificação: " + NotificacaoMensagem;
                if (LogNotificacao.EmptyIfNull().ToString().Trim().Length > 200) { LogNotificacao = LogNotificacao.Substring(0, 200); };
                a_yesprodutos_extrato record_a_yesprodutos_extrato = new Db.a_yesprodutos_extrato();
                record_a_yesprodutos_extrato.id_yesproduto = 8; // Nota Fiscal Eletrônica - Enotas
                record_a_yesprodutos_extrato.log = LogNotificacao;
                record_a_yesprodutos_extrato.datahora_execucao = LibDateTime.getDataHoraBrasilia();
                record_a_yesprodutos_extrato.id_usuario_execucao = CachePersister.userIdentity.IdUsuario; ;
                db.Entry(record_a_yesprodutos_extrato).State = EntityState.Added;

                db.SaveChanges();

                Sucesso = true;
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Sucesso;
        }
        #endregion

        #region Enviar Texto Link WhatsApp
        public bool EnviarTextoLinkWhatsApp(String ParamCelularNumero, String ParamMensagem, String ParamLinkURL, String ParamTitle)
        {
            bool Sucesso = false;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();

            try
            {
                IDictionary<String, String> MessageWhatsApp = new Dictionary<String, String>();
                MessageWhatsApp.Add("phone", ParamCelularNumero);
                MessageWhatsApp.Add("message", ParamMensagem);
                MessageWhatsApp.Add("image", null);
                MessageWhatsApp.Add("linkUrl", ParamLinkURL);
                MessageWhatsApp.Add("title", ParamTitle);
                MessageWhatsApp.Add("linkDescription", null);
                MessageWhatsApp.Add("linkType", "LARGE");
                String MessageWhatsAppJson = JsonConvert.SerializeObject(MessageWhatsApp, Formatting.Indented);
                JObject MessageWhatsAppObject = JObject.Parse(MessageWhatsAppJson);
                var strJson = JsonConvert.SerializeObject(MessageWhatsAppObject);

                String INSTANCE_ID = "3D933D08BC343053ACA9CE82F470C0C7";
                String INSTANCE_TOKEN = "ACE6A8DDC0AF992D50FDD712";
                String CLIENT_TOKEN = "F473838c008c242fc9ae05bf7b4727c37S";
                String URL = "https://api.z-api.io/instances/" + INSTANCE_ID + "/token/" + INSTANCE_TOKEN;
                String Resource = "/send-link";
                var client = new RestClient(URL);
                var request = new RestRequest(Resource, Method.Post);
                request.AddHeader("content-type", "application/json");
                request.AddHeader("client-token", CLIENT_TOKEN);
                request.AddParameter("application/json", strJson, ParameterType.RequestBody);
                var response = client.Execute(request);
                var obj = JObject.Parse(response.Content);

                // Criar o log da utilização
                String LogNotificacao = "WhatsApp | Destinatário: " + ParamCelularNumero + " | Notificação: " + ParamMensagem;
                if (LogNotificacao.EmptyIfNull().ToString().Trim().Length > 200) { LogNotificacao = LogNotificacao.Substring(0, 200); };
                a_yesprodutos_extrato record_a_yesprodutos_extrato = new Db.a_yesprodutos_extrato();
                record_a_yesprodutos_extrato.id_yesproduto = 8; // Nota Fiscal Eletrônica - Enotas
                record_a_yesprodutos_extrato.log = LogNotificacao;
                record_a_yesprodutos_extrato.datahora_execucao = LibDateTime.getDataHoraBrasilia();
                record_a_yesprodutos_extrato.id_usuario_execucao = CachePersister.userIdentity.IdUsuario; ;
                db.Entry(record_a_yesprodutos_extrato).State = EntityState.Added;

                db.SaveChanges();

                Sucesso = true;
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Sucesso;
        }
        #endregion


    }
}