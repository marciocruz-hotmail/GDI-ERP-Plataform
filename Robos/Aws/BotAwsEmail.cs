using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using GdiPlataform.Lib;

namespace GdiPlataform.Robos.Aws
{
    public class BotAwsEmail
    {
        public void EnviarEmailAWS(String paramFromEmail, String paramFromNome, String paramDestinatarioEmail, String paramDestinatarioNome, String paramAssunto, String paramMensagem, List<string> ListaAnexos)
        {
            // Parâmetros de Emails
            try
            {
                var ses = GdiAwsSesSmtpCredentials.Resolve();
                string param_email_SMTPServer = ses.Host;
                string param_email_SMTPUsuario = ses.Username;
                string param_email_SMTPSenha = ses.Password;
                int smtpPort = ses.Port;

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
                client.Port = smtpPort;
                client.UseDefaultCredentials = false;
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

                if (paramDestinatarioEmail.IndexOf(",") > 0) { paramDestinatarioEmail = paramDestinatarioEmail.Replace(",", ""); };
                if (paramDestinatarioEmail.IndexOf(" ") > 0) { paramDestinatarioEmail = paramDestinatarioEmail.Replace(" ", ""); };
                if (paramDestinatarioEmail.IndexOf(";") > 0) // Se contiver mais de um email
                {
                    String[] listaDestinatarioEmail = paramDestinatarioEmail.Split(';');
                    String ListaEmailValidados = string.Empty;
                    foreach (string email in listaDestinatarioEmail)
                    {
                        String EmailEnviar = email.EmptyIfNull().ToString().Trim().ToLowerInvariant();
                        if (EmailEnviar.Length > 3)
                        {
                            if (ListaEmailValidados.IndexOf(EmailEnviar) == -1)
                            {
                                mail.To.Add(new MailAddress(EmailEnviar, paramDestinatarioNome));
                                ListaEmailValidados += EmailEnviar + ";";
                            }
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
        }
    }
}