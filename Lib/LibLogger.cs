using System;
using System.IO;
using System.Text;
using System.Web;

namespace GdiPlataform.Lib
{
    public static class LibLogger
    {
        private static readonly object _lock = new object();

        public static void Error(string mensagem, Exception ex = null) => Escrever("ERROR", mensagem, ex);
        public static void Warn(string mensagem, Exception ex = null)  => Escrever("WARN ", mensagem, ex);
        public static void Info(string mensagem)                        => Escrever("INFO ", mensagem, null);

        private static void Escrever(string nivel, string mensagem, Exception ex)
        {
            try
            {
                string caminhoLogs = Path.Combine(HttpRuntime.AppDomainAppPath, "App_Data", "Logs");
                Directory.CreateDirectory(caminhoLogs);
                string arquivo = Path.Combine(caminhoLogs, $"erp-{DateTime.Now:yyyy-MM-dd}.log");

                var sb = new StringBuilder();
                sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                sb.Append(" [").Append(nivel).Append("] ");
                sb.AppendLine(mensagem);

                if (ex != null)
                {
                    sb.Append("  Type:       ").AppendLine(ex.GetType().FullName);
                    sb.Append("  Message:    ").AppendLine(ex.Message);
                    if (ex.InnerException != null)
                        sb.Append("  Inner:      ").AppendLine(ex.InnerException.Message);
                    if (!string.IsNullOrEmpty(ex.StackTrace))
                        sb.Append("  StackTrace: ").AppendLine(ex.StackTrace.Trim());
                }

                lock (_lock)
                {
                    File.AppendAllText(arquivo, sb.ToString(), Encoding.UTF8);
                }
            }
            catch
            {
                // Logging nunca deve interromper a aplicação
            }
        }
    }
}
