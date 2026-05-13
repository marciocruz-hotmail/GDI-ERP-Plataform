using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Transfer;
using GdiPlataform.Db;
using System.Linq;
using NPOI.POIFS.Crypt;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace GdiPlataform.Robos.Aws
{
    public static class BotAwsS3
    {
        static void uploadRequest_UploadPartProgressEvent(object sender, UploadProgressArgs e)
        {
            Console.WriteLine("{0}/{1}", e.TransferredBytes, e.TotalBytes);
        }

        /// <summary>
        /// Monta a URL de acesso direto (virtual-hosted-style) ao objeto no S3, para buckets com leitura pública.
        /// </summary>
        public static string BuildPublicObjectUrl(string bucketName, string objectKey)
        {
            if (string.IsNullOrWhiteSpace(bucketName) || string.IsNullOrWhiteSpace(objectKey))
                return null;
            GdiAwsS3BucketRules.ThrowIfBucketNotAllowed(bucketName, "BuildPublicObjectUrl");
            var encodedKey = string.Join("/", objectKey.Split(new[] { '/' }, StringSplitOptions.None).Select(s => Uri.EscapeDataString(s)));
            var region = GdiAwsS3Credentials.ResolveRegion();
            return string.Format("https://{0}.s3.{1}.amazonaws.com/{2}", bucketName.Trim(), region.SystemName, encodedKey);
        }

        public static void UploadStreamS3(string BucketNameS3, string FilePathS3, Stream InputStream, bool publicRead = false)
        {
            try
            {
                GdiAwsS3BucketRules.ValidateGedUpload(BucketNameS3, publicRead, "Upload S3");
                using (AmazonS3Client s3Client = GdiAwsS3Credentials.CreateS3Client())
                using (TransferUtility transferUtility = new TransferUtility(s3Client))
                {
                    TransferUtilityUploadRequest fileTransferUtilityRequest = new TransferUtilityUploadRequest
                    {
                        BucketName = BucketNameS3,
                        InputStream = InputStream,
                        Key = FilePathS3
                    };
                    if (publicRead)
                    {
                        fileTransferUtilityRequest.CannedACL = S3CannedACL.PublicRead;
                    }
                    var task = transferUtility.UploadAsync(fileTransferUtilityRequest);
                    task.Wait();
                }
            }
            catch (AmazonServiceException e)
            {
                throw (e);
            }
            catch (Exception e)
            {
                throw (e);
            }
        }

    }
}