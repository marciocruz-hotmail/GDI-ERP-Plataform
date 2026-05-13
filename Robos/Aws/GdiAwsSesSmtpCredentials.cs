using Newtonsoft.Json;
using System;
using System.IO;
using System.Web.Hosting;

namespace GdiPlataform.Robos.Aws
{
    /// <summary>
    /// Credenciais SMTP do AWS SES (usuário/senha SMTP do console IAM), fora do código-fonte.
    /// Ordem: variáveis de ambiente, depois <c>App_Data/Secrets/aws-ses-smtp.local.json</c> (gitignored; modelo sem segredos: <c>aws-ses-smtp.template.json</c>).
    /// Região omissão: <see cref="AwsSesSmtpRegionSaoPaulo"/> (South America — São Paulo; endpoint <c>email-smtp.sa-east-1.amazonaws.com</c>).
    /// </summary>
    public static class GdiAwsSesSmtpCredentials
    {
        /// <summary>Código oficial AWS da região São Paulo (não confundir com us-east-1 Virgínia).</summary>
        public const string AwsSesSmtpRegionSaoPaulo = "sa-east-1";

        private const string LocalSecretsRelativePath = "~/App_Data/Secrets/aws-ses-smtp.local.json";
        private const int DefaultPort = 587;

        private sealed class AwsSesSmtpLocalSecretsDto
        {
            [JsonProperty("SmtpHost")]
            public string SmtpHost { get; set; }

            /// <summary>Opcional; se <see cref="SmtpHost"/> vazio, usa <c>email-smtp.{região}.amazonaws.com</c> (ex.: sa-east-1).</summary>
            [JsonProperty("SmtpRegion")]
            public string SmtpRegion { get; set; }

            [JsonProperty("SmtpPort")]
            public int? SmtpPort { get; set; }

            [JsonProperty("SmtpUsername")]
            public string SmtpUsername { get; set; }

            [JsonProperty("SmtpPassword")]
            public string SmtpPassword { get; set; }
        }

        public sealed class SesSmtpSettings
        {
            public string Host { get; set; }
            public int Port { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
        }

        /// <summary>
        /// AWS_SES_SMTP_HOST, AWS_SES_SMTP_USERNAME, AWS_SES_SMTP_PASSWORD (opcional AWS_SES_SMTP_PORT, AWS_SES_SMTP_REGION).
        /// </summary>
        public static SesSmtpSettings Resolve()
        {
            var host = Environment.GetEnvironmentVariable("AWS_SES_SMTP_HOST")?.Trim();
            var regionFromEnv = Environment.GetEnvironmentVariable("AWS_SES_SMTP_REGION")?.Trim();
            var user = Environment.GetEnvironmentVariable("AWS_SES_SMTP_USERNAME")?.Trim();
            var pass = Environment.GetEnvironmentVariable("AWS_SES_SMTP_PASSWORD")?.Trim();
            var portStr = Environment.GetEnvironmentVariable("AWS_SES_SMTP_PORT")?.Trim();
            int port = DefaultPort;
            if (!string.IsNullOrEmpty(portStr))
                int.TryParse(portStr, out port);

            if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pass))
            {
                host = ResolveSmtpHost(host, regionFromEnv);
                return new SesSmtpSettings
                {
                    Host = host,
                    Port = port > 0 ? port : DefaultPort,
                    Username = user,
                    Password = pass
                };
            }

            var path = MapLocalSecretsPath();
            if (path == null || !File.Exists(path))
            {
                throw new InvalidOperationException(
                    "Credenciais AWS SES SMTP não configuradas. Defina AWS_SES_SMTP_USERNAME e AWS_SES_SMTP_PASSWORD " +
                    "(e opcionalmente AWS_SES_SMTP_HOST / AWS_SES_SMTP_PORT / AWS_SES_SMTP_REGION; omissão de host/região = São Paulo sa-east-1) " +
                    "ou crie App_Data/Secrets/aws-ses-smtp.local.json (copie de aws-ses-smtp.template.json).");
            }

            AwsSesSmtpLocalSecretsDto dto;
            try
            {
                dto = JsonConvert.DeserializeObject<AwsSesSmtpLocalSecretsDto>(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Não foi possível ler o ficheiro aws-ses-smtp.local.json.", ex);
            }

            user = dto?.SmtpUsername?.Trim();
            pass = dto?.SmtpPassword?.Trim();
            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                throw new InvalidOperationException(
                    "aws-ses-smtp.local.json existe mas SmtpUsername ou SmtpPassword estão vazios. Veja aws-ses-smtp.template.json.");
            }

            host = dto?.SmtpHost?.Trim();
            var regionFromFile = dto?.SmtpRegion?.Trim();
            host = ResolveSmtpHost(host, regionFromFile);

            var p = dto?.SmtpPort;
            if (p.HasValue && p.Value > 0)
                port = p.Value;

            return new SesSmtpSettings
            {
                Host = host,
                Port = port,
                Username = user,
                Password = pass
            };
        }

        private static string MapLocalSecretsPath()
        {
            try
            {
                if (!string.IsNullOrEmpty(HostingEnvironment.ApplicationPhysicalPath))
                {
                    var mapped = HostingEnvironment.MapPath(LocalSecretsRelativePath);
                    if (!string.IsNullOrEmpty(mapped))
                        return mapped;
                }
            }
            catch
            {
            }

            var baseDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return Path.Combine(baseDir, "App_Data", "Secrets", "aws-ses-smtp.local.json");
        }

        /// <summary>Host explícito tem prioridade; senão monta a partir da região; senão São Paulo (<see cref="AwsSesSmtpRegionSaoPaulo"/>).</summary>
        private static string ResolveSmtpHost(string smtpHost, string smtpRegionOrNull)
        {
            var h = (smtpHost ?? string.Empty).Trim();
            if (!string.IsNullOrEmpty(h))
                return h;
            var r = (smtpRegionOrNull ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(r))
                r = AwsSesSmtpRegionSaoPaulo;
            return "email-smtp." + r + ".amazonaws.com";
        }
    }
}
