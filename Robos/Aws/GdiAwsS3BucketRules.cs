using System;

namespace GdiPlataform.Robos.Aws
{
    /// <summary>
    /// Regras GDI para os dois buckets S3 do ERP:
    /// <list type="bullet">
    /// <item><description><b>bucket ERP</b> (<see cref="GdiAwsS3Credentials.ResolveBucketErp"/>): GED privado e público (ACL), LMS, documentos da aplicação — leitura privada via presigned URL.</description></item>
    /// <item><description><b>bucket público</b> (<see cref="GdiAwsS3Credentials.ResolveBucketPublicFiles"/>): apenas ficheiros com leitura pública intencional (site/ícones); <b>não</b> gravar GED privado aqui.</description></item>
    /// </list>
    /// </summary>
    public static class GdiAwsS3BucketRules
    {
        /// <summary>Compara nomes de bucket (trim, sem diferenciar maiúsculas).</summary>
        public static bool NamesMatch(string a, string b)
        {
            return string.Equals((a ?? string.Empty).Trim(), (b ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>O bucket é um dos dois buckets configurados para o ERP?</summary>
        public static bool IsAllowedBucketName(string bucketName)
        {
            var b = (bucketName ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(b))
                return false;
            return NamesMatch(b, GdiAwsS3Credentials.ResolveBucketErp())
                || NamesMatch(b, GdiAwsS3Credentials.ResolveBucketPublicFiles());
        }

        /// <summary>Falha se o bucket não for exatamente um dos dois autorizados (evita dados / IAM fora do padrão).</summary>
        public static void ThrowIfBucketNotAllowed(string bucketName, string contextoOperacao)
        {
            var b = (bucketName ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(b))
                throw new InvalidOperationException((contextoOperacao ?? "S3") + ": nome do bucket não informado.");

            if (!IsAllowedBucketName(b))
            {
                var erp = GdiAwsS3Credentials.ResolveBucketErp();
                var pub = GdiAwsS3Credentials.ResolveBucketPublicFiles();
                throw new InvalidOperationException(
                    (contextoOperacao ?? "S3") + ": bucket '" + b + "' não está autorizado. Utilize apenas '" + erp + "' (ERP) ou '" + pub + "' (público).");
            }
        }

        /// <summary>
        /// Escrita: ficheiro privado só no bucket ERP; ficheiro marcado público pode ir ao ERP (ACL public-read) ou ao bucket público.
        /// </summary>
        public static void ValidateGedUpload(string bucketName, bool uploadPublico, string contextoOperacao)
        {
            ThrowIfBucketNotAllowed(bucketName, contextoOperacao);

            var pub = GdiAwsS3Credentials.ResolveBucketPublicFiles();
            if (!uploadPublico && NamesMatch(bucketName, pub))
            {
                var erp = GdiAwsS3Credentials.ResolveBucketErp();
                throw new InvalidOperationException(
                    (contextoOperacao ?? "GED") + ": anexos privados não podem ser gravados no bucket público '" + pub + "'. Ajuste o tipo GED (bucket_s3) para '" + erp + "' ou marque o tipo como ficheiro público se o conteúdo for realmente público.");
            }
        }

        /// <summary>Consistência: <c>ged_arquivos.bucket</c> deve coincidir com <c>ged_arquivos_tipos.bucket_s3</c> quando ambos preenchidos.</summary>
        public static void ThrowIfGedRowBucketDiffersFromTipo(string bucketFromTipo, string bucketFromRow, string contextoOperacao)
        {
            var t = (bucketFromTipo ?? string.Empty).Trim();
            var r = (bucketFromRow ?? string.Empty).Trim();
            if (t.Length == 0 || r.Length == 0)
                return;
            if (!NamesMatch(t, r))
            {
                throw new InvalidOperationException(
                    (contextoOperacao ?? "GED") + ": o bucket no registo do arquivo (" + r + ") não coincide com o do tipo (" + t + ").");
            }
        }

        /// <summary>
        /// Valida URL HTTPS virtual-hosted do S3 para um dos buckets ERP (evita redirecionamento arbitrário em public_url persistida).
        /// </summary>
        public static bool TryValidateStoredPublicUrl(string publicUrl, out string mensagemErro)
        {
            mensagemErro = null;
            var raw = (publicUrl ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(raw))
            {
                mensagemErro = "URL pública vazia.";
                return false;
            }

            if (!Uri.TryCreate(raw, UriKind.Absolute, out var u)
                || !string.Equals(u.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                mensagemErro = "URL pública inválida (HTTPS obrigatório).";
                return false;
            }

            var h = (u.Host ?? string.Empty).Trim();
            var erp = GdiAwsS3Credentials.ResolveBucketErp().Trim();
            var pub = GdiAwsS3Credentials.ResolveBucketPublicFiles().Trim();
            var prefixErp = erp + ".s3.";
            var prefixPub = pub + ".s3.";

            if (h.StartsWith(prefixErp, StringComparison.OrdinalIgnoreCase)
                || h.StartsWith(prefixPub, StringComparison.OrdinalIgnoreCase))
                return true;

            var dualErp = erp + ".s3.dualstack.";
            var dualPub = pub + ".s3.dualstack.";
            if (h.StartsWith(dualErp, StringComparison.OrdinalIgnoreCase)
                || h.StartsWith(dualPub, StringComparison.OrdinalIgnoreCase))
                return true;

            mensagemErro = "URL pública não corresponde aos buckets S3 autorizados (" + erp + " / " + pub + ").";
            return false;
        }
    }
}
