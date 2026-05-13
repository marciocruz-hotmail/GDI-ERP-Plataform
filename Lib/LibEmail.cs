using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;

namespace GdiPlataform.Lib
{
    public static class LibEmail
    {
        /*public static void EnviarEmailAWS(String paramFromEmail, String paramFromNome, String paramDestinatarioEmail, String paramDestinatarioNome, String paramAssunto, String paramMensagem, List<string> ListaAnexos)
        {
            // Parâmetros de Emails
            try
            {
                string param_email_SMTPServer = "email-smtp.sa-east-1.amazonaws.com";
                string param_email_SMTPUsuario = "(ver GdiAwsSesSmtpCredentials / aws-ses-smtp.local.json ou aws-ses-smtp.template.json)";
                string param_email_SMTPSenha = "(ver GdiAwsSesSmtpCredentials / aws-ses-smtp.local.json ou aws-ses-smtp.template.json)";

                if (paramFromEmail.Trim().Length == 0) { paramFromEmail = "financeiro@gdiaviacao.com.br"; };
                if (paramFromNome.Trim().Length == 0) { paramFromNome = paramFromEmail; };
                string param_email_FromEmail = paramFromEmail;
                string param_email_FromNome = paramFromNome;
                string param_email_Assunto = paramAssunto;

                System.Net.ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3;
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

                System.Net.Mail.SmtpClient client = new System.Net.Mail.SmtpClient();
                client.Host = param_email_SMTPServer;
                client.Port = 587;
                client.UseDefaultCredentials = true;
                client.EnableSsl = true;
                client.Credentials = new System.Net.NetworkCredential(param_email_SMTPUsuario, param_email_SMTPSenha);
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(param_email_FromEmail, param_email_FromNome);

                if (ListaAnexos != null)
                {
                    if (ListaAnexos.Count > 0)
                    {
                        foreach (String FileNameAnexo in ListaAnexos)
                        {
                            if (File.Exists(FileNameAnexo))
                            {
                                Attachment fileAnexo = new Attachment(FileNameAnexo);
                                mail.Attachments.Add(fileAnexo);
                            }
                        }
                    }
                }

                // Destinatários
                if (paramDestinatarioEmail.Trim().Length == 0) { paramDestinatarioEmail = "consultorsoft@gmail.com"; };
                if (paramDestinatarioEmail.IndexOf(";") > 0) // Se contiver mais de um email
                {
                    String[] listaDestinatarioEmail = paramDestinatarioEmail.Split(';');
                    foreach (string email in listaDestinatarioEmail)
                    {
                        if (email.ToString().Trim().Length > 3)
                        {
                            mail.To.Add(new MailAddress(email, paramDestinatarioNome));
                        }
                    }
                }
                else
                {
                    mail.To.Add(new MailAddress(paramDestinatarioEmail, paramDestinatarioNome));
                }

                mail.Subject = param_email_Assunto;

                mail.SubjectEncoding = System.Text.Encoding.GetEncoding("utf-8");
                mail.BodyEncoding = System.Text.Encoding.GetEncoding("utf-8");
                mail.Body = paramMensagem;
                mail.IsBodyHtml = true;

                mail.Priority = MailPriority.High;
                client.Send(mail);
            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }*/

        public static string RenderView<TModel>(this Controller controller, string viewName, TModel model, bool partial = false)
        {
            var controllerContext = controller.ControllerContext;
            controllerContext.Controller.ViewData.Model = model;
            var viewResult = partial ? ViewEngines.Engines.FindPartialView(controllerContext, viewName) : ViewEngines.Engines.FindView(controllerContext, viewName, null);
            StringWriter stringWriter;
            using (stringWriter = new StringWriter())
            {
                var viewContext = new ViewContext(
                    controllerContext,
                    viewResult.View,
                    controllerContext.Controller.ViewData,
                    controllerContext.Controller.TempData,
                    stringWriter);

                viewResult.View.Render(viewContext, stringWriter);
                viewResult.ViewEngine.ReleaseView(controllerContext, viewResult.View);
            }
            return stringWriter.ToString();
        }
    }
}