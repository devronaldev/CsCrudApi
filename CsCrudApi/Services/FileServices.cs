using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CsCrudApi.Services
{
    public class FileServices
    {
        private readonly string _bucketName;
        private readonly RegionEndpoint _region;
        private readonly IAmazonS3 _s3Client;
        public static readonly List<string> AllowedProfileContentTypes = new List<string>
        {
            "image/jpeg", // JPG/JPEG
            "image/png",  // PNG
            "image/bmp",  // BMP
            "image/webp", // WEBP
            "image/tiff" // TIFF
        };


        public FileServices()
        {
            var bucketName = Environment.GetEnvironmentVariable("AWS_BUCKET_NAME");
            var region = Environment.GetEnvironmentVariable("AWS_REGION");
            var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY");
            var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_KEY");

            if (string.IsNullOrWhiteSpace(bucketName) || string.IsNullOrWhiteSpace(region) ||
                string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey))
            {
                throw new Exception($"Erro ao obter variáveis de ambiente obrigatórias. Certifique-se de que todas as variáveis estejam definidas: " +
                                    $"AWS_BUCKET_NAME, AWS_REGION, AWS_ACCESS_KEY, AWS_SECRET_KEY.");
            }

            _bucketName = bucketName;

            try
            {
                _region = RegionEndpoint.GetBySystemName(region);
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao determinar a região AWS '{region}'. Verifique o valor de AWS_REGION.", ex);
            }

            _s3Client = new AmazonS3Client(accessKey, secretKey, _region);
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            try
            {
                var transferUtility = new TransferUtility(_s3Client);

                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = fileStream,
                    Key = fileName,
                    BucketName = _bucketName,
                    ContentType = contentType,
                    CannedACL = S3CannedACL.PublicRead
                };

                await transferUtility.UploadAsync(uploadRequest);

                string fileUrl = $"https://{_bucketName}.s3.{_region.SystemName}.amazonaws.com/{fileName}";
                return fileUrl;
            }
            catch (Exception ex)
            {
                // Trate ou registre a exceção conforme necessário
                throw new ApplicationException($"Erro ao enviar arquivo para S3: {ex.Message}", ex);
            }
        }
    }
}
