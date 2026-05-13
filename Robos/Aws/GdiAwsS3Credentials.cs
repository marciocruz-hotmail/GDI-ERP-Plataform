using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Web.Hosting;

namespace GdiPlataform.Robos.Aws
{
    /// <summary>
    /// Resolve credenciais e região para o SDK AWS S3 sem armazená-las no código-fonte.
    /// Ordem: variáveis de ambiente (padrão de mercado / AWS CLI), depois arquivo local gitignored.
    /// Buckets padrão GDI: <c>bucket-erp-gdi</c> (app/GED), <c>bucket-gdi-public-files</c> (estáticos públicos);
    /// podem ser sobrescritos por <c>AWS_S3_BUCKET_ERP</c> / <c>AWS_S3_BUCKET_PUBLIC</c> ou campos no JSON local.
    /// </summary>
    public static class GdiAwsS3Credentials
    {
        private const string LocalSecretsRelativePath = "~/App_Data/Secrets/aws-s3.local.json";
        private const string DefaultBucketErp = "bucket-erp-gdi";
        private const string DefaultBucketPublicFiles = "bucket-gdi-public-files";

        private sealed class AwsS3LocalSecretsDto
        {
            [JsonProperty("AccessKeyId")]
            public string AccessKeyId { get; set; }

            [JsonProperty("SecretAccessKey")]
            public string SecretAccessKey { get; set; }

            [JsonProperty("Region")]
            public string Region { get; set; }

            /// <summary>Bucket principal do ERP (ex.: GED, LMS). Opcional; omissão = bucket-erp-gdi.</summary>
            [JsonProperty("BucketErp")]
            public string BucketErp { get; set; }

            /// <summary>Bucket de ficheiros públicos (URLs estáticas nas views). Opcional; omissão = bucket-gdi-public-files.</summary>
            [JsonProperty("BucketPublicFiles")]
            public string BucketPublicFiles { get; set; }
        }

        /// <summary>
        /// Região: AWS_REGION / AWS_DEFAULT_REGION, senão campo Region no JSON local, senão sa-east-1.
        /// </summary>
        public static RegionEndpoint ResolveRegion()
        {
            var fromEnv = Environment.GetEnvironmentVariable("AWS_REGION")
                ?? Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION");
            if (!string.IsNullOrWhiteSpace(fromEnv))
            {
                try { return RegionEndpoint.GetBySystemName(fromEnv.Trim()); }
                catch (ArgumentException) { /* ignorar valor inválido */ }
            }

            try
            {
                var path = MapLocalSecretsPath();
                if (path != null && File.Exists(path))
                {
                    var dto = TryDeserializeSecrets(path);
                    if (!string.IsNullOrWhiteSpace(dto?.Region))
                    {
                        try { return RegionEndpoint.GetBySystemName(dto.Region.Trim()); }
                        catch (ArgumentException) { }
                    }
                }
            }
            catch
            {
                // falha ao ler ficheiro: usar omissão abaixo
            }

            return RegionEndpoint.SAEast1;
        }

        /// <summary>
        /// AWS_ACCESS_KEY_ID + AWS_SECRET_ACCESS_KEY, senão App_Data/Secrets/aws-s3.local.json.
        /// </summary>
        public static AWSCredentials ResolveCredentials()
        {
            var access = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID")?.Trim();
            var secret = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY")?.Trim();
            if (!string.IsNullOrEmpty(access) && !string.IsNullOrEmpty(secret))
                return new BasicAWSCredentials(access, secret);

            var path = MapLocalSecretsPath();
            if (path == null || !File.Exists(path))
            {
                throw new InvalidOperationException(
                    "Credenciais AWS S3 não configuradas. Defina AWS_ACCESS_KEY_ID e AWS_SECRET_ACCESS_KEY " +
                    "(recomendado em produção / IIS) ou crie App_Data/Secrets/aws-s3.local.json a partir de aws-s3.local.json.example.");
            }

            AwsS3LocalSecretsDto dto;
            try
            {
                dto = TryDeserializeSecrets(path);
                if (dto == null)
                    throw new InvalidOperationException("Conteúdo inválido em aws-s3.local.json.");
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Não foi possível ler o ficheiro aws-s3.local.json.", ex);
            }

            access = dto?.AccessKeyId?.Trim();
            secret = dto?.SecretAccessKey?.Trim();
            if (string.IsNullOrEmpty(access) || string.IsNullOrEmpty(secret))
            {
                throw new InvalidOperationException(
                    "aws-s3.local.json existe mas AccessKeyId ou SecretAccessKey estão vazios. Veja aws-s3.local.json.example.");
            }

            return new BasicAWSCredentials(access, secret);
        }

        public static AmazonS3Client CreateS3Client()
        {
            return new AmazonS3Client(ResolveCredentials(), ResolveRegion());
        }

        /// <summary>Variável <c>AWS_S3_BUCKET_ERP</c>, senão <c>BucketErp</c> no JSON, senão bucket-erp-gdi.</summary>
        public static string ResolveBucketErp()
        {
            var b = Environment.GetEnvironmentVariable("AWS_S3_BUCKET_ERP")?.Trim();
            if (!string.IsNullOrEmpty(b))
                return b;
            var dto = TryLoadSecretsDtoFromFile();
            if (!string.IsNullOrWhiteSpace(dto?.BucketErp))
                return dto.BucketErp.Trim();
            return DefaultBucketErp;
        }

        /// <summary>Variável <c>AWS_S3_BUCKET_PUBLIC</c>, senão <c>BucketPublicFiles</c> no JSON, senão bucket-gdi-public-files.</summary>
        public static string ResolveBucketPublicFiles()
        {
            var b = Environment.GetEnvironmentVariable("AWS_S3_BUCKET_PUBLIC")?.Trim();
            if (!string.IsNullOrEmpty(b))
                return b;
            var dto = TryLoadSecretsDtoFromFile();
            if (!string.IsNullOrWhiteSpace(dto?.BucketPublicFiles))
                return dto.BucketPublicFiles.Trim();
            return DefaultBucketPublicFiles;
        }

        private static AwsS3LocalSecretsDto TryLoadSecretsDtoFromFile()
        {
            try
            {
                var path = MapLocalSecretsPath();
                if (path == null || !File.Exists(path))
                    return null;
                return TryDeserializeSecrets(path);
            }
            catch
            {
                return null;
            }
        }

        private static AwsS3LocalSecretsDto TryDeserializeSecrets(string path)
        {
            return JsonConvert.DeserializeObject<AwsS3LocalSecretsDto>(File.ReadAllText(path));
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
                // continuar para fallback
            }

            var baseDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return Path.Combine(baseDir, "App_Data", "Secrets", "aws-s3.local.json");
        }
    }
}
