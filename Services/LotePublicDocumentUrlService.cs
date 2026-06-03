using System;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using GdiPlataform.Db;

namespace GdiPlataform.Services
{
    /// <summary>
    /// Resolve a URL pública do PDF vinculado ao lote de estoque (GED), com URL de fallback parametrizada.
    /// API pública: GET /api/public/lote-documento?idLote={id}
    /// </summary>
    public sealed class LotePublicDocumentUrlService : IDisposable
    {
        public const string AppKeyFallbackUrl = "PublicApi:LoteDocumentoFallbackUrl";
        public const string AppKeyEntityConnectionName = "PublicApi:EntityConnectionName";

        public const string UrlFallbackPadrao =
            "https://bucket-gdi-public-files.s3.sa-east-1.amazonaws.com/produtos-documentos/2026/04/lote-nao-encontrado_id-12522.pdf";

        private readonly GdiPlataformEntities _db;
        private readonly string _urlFallback;
        private bool _disposed;

        public LotePublicDocumentUrlService(string entityConnectionName = null)
        {
            _urlFallback = ObterUrlFallbackConfigurada();
            _db = CriarContexto(entityConnectionName);
        }

        /// <summary>Para testes ou uso com contexto já configurado.</summary>
        internal LotePublicDocumentUrlService(GdiPlataformEntities db, string urlFallback = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _urlFallback = string.IsNullOrWhiteSpace(urlFallback) ? ObterUrlFallbackConfigurada() : urlFallback.Trim();
        }

        public static string ObterUrlFallbackConfigurada()
        {
            var url = ConfigurationManager.AppSettings[AppKeyFallbackUrl];
            return string.IsNullOrWhiteSpace(url) ? UrlFallbackPadrao : url.Trim();
        }

        private static GdiPlataformEntities CriarContexto(string entityConnectionName)
        {
            if (!string.IsNullOrWhiteSpace(entityConnectionName))
            {
                return new GdiPlataformEntities(entityConnectionName.Trim());
            }

            var connectionName = ConfigurationManager.AppSettings[AppKeyEntityConnectionName];
            return string.IsNullOrWhiteSpace(connectionName)
                ? new GdiPlataformEntities()
                : new GdiPlataformEntities(connectionName.Trim());
        }

        public LotePublicDocumentUrlResult Resolver(int idLote)
        {
            if (idLote <= 0)
            {
                return LotePublicDocumentUrlResult.Fallback(_urlFallback);
            }

            var arquivo = _db.ged_arquivos.AsNoTracking()
                .Where(a => a.ativo == true && a.id_estoque_lote == idLote)
                .OrderByDescending(a => a.id_arquivo)
                .FirstOrDefault();

            if (arquivo == null)
            {
                return LotePublicDocumentUrlResult.Fallback(_urlFallback);
            }

            if (string.IsNullOrWhiteSpace(arquivo.public_url))
            {
                return LotePublicDocumentUrlResult.Fallback(_urlFallback);
            }

            var urlBanco = arquivo.public_url.Trim();
            if (!TryValidarUrlPublicaSegura(urlBanco, out var uriValida))
            {
                return LotePublicDocumentUrlResult.Fallback(_urlFallback);
            }

            return LotePublicDocumentUrlResult.FromDatabase(uriValida);
        }

        /// <summary>
        /// Evita open redirect e esquemas perigosos (javascript:, file:, etc.).
        /// </summary>
        internal static bool TryValidarUrlPublicaSegura(string candidata, out string urlNormalizada)
        {
            urlNormalizada = null;
            if (string.IsNullOrWhiteSpace(candidata))
            {
                return false;
            }

            if (!Uri.TryCreate(candidata, UriKind.Absolute, out var uri))
            {
                return false;
            }

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                return false;
            }

            urlNormalizada = uri.ToString();
            return true;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _db.Dispose();
        }
    }

    public sealed class LotePublicDocumentUrlResult
    {
        private LotePublicDocumentUrlResult(LotePublicDocumentUrlStatus status, string url, string mensagemErro, string codigoErro)
        {
            Status = status;
            Url = url;
            MensagemErro = mensagemErro;
            CodigoErro = codigoErro;
        }

        public LotePublicDocumentUrlStatus Status { get; }
        public string Url { get; }
        public string MensagemErro { get; }
        public string CodigoErro { get; }

        public static LotePublicDocumentUrlResult FromDatabase(string urlValidada) =>
            new LotePublicDocumentUrlResult(LotePublicDocumentUrlStatus.ResolvidoBanco, urlValidada, null, null);

        public static LotePublicDocumentUrlResult Fallback(string fallbackValidado) =>
            new LotePublicDocumentUrlResult(LotePublicDocumentUrlStatus.FallbackSemDocumento, fallbackValidado, null, null);

        public static LotePublicDocumentUrlResult InvalidParameter(string mensagem) =>
            new LotePublicDocumentUrlResult(LotePublicDocumentUrlStatus.ParametroInvalido, null, mensagem, "PARAMETRO_INVALIDO");

        public static LotePublicDocumentUrlResult ErroConfiguracao(string mensagem) =>
            new LotePublicDocumentUrlResult(LotePublicDocumentUrlStatus.ErroConfiguracao, null, mensagem, "ERRO_CONFIGURACAO");
    }

    public enum LotePublicDocumentUrlStatus
    {
        ResolvidoBanco,
        FallbackSemDocumento,
        ParametroInvalido,
        ErroConfiguracao
    }
}
