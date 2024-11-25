using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.IdentityModel.Tokens;
using System.Net;

namespace CsCrudApi.Services
{
    public class FileServices
    {
        private readonly string _bucketName;
        private readonly IAmazonS3 _s3Client;

        public FileServices()
        {
            var acessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY");
            var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_KEY");
            var region = Environment.GetEnvironmentVariable("AWS_REGION");
            _bucketName = Environment.GetEnvironmentVariable("AWS_BUCKET_NAME");

            if (acessKey.IsNullOrEmpty())
            {
                throw new Exception($"Chave de acesso vazia: {acessKey}");
            }
            if (secretKey.IsNullOrEmpty())
            {
                throw new Exception($"Chave de acesso vazia: {acessKey}");
            }
            if (region.IsNullOrEmpty())
            {
                throw new Exception($"Chave de acesso vazia: {acessKey}");
            }
            if (_bucketName.IsNullOrEmpty())
            {
                throw new Exception($"Chave de acesso vazia: {acessKey}");
            }
            _s3Client = new AmazonS3Client(acessKey, secretKey, region, Amazon.RegionEndpoint.GetBySystemName(region));
        }

        public async Task<string> UploadFileAsync(string fileName, Stream file, string contentType)
        {
            var uploadRequest = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = fileName,
                InputStream = file,
                ContentType = contentType,
                CannedACL = S3CannedACL.PublicRead
            };

            var verification = await _s3Client.GetObjectMetadataAsync(_bucketName, fileName);
            if (verification.HttpStatusCode == HttpStatusCode.OK)
            {
                throw new Exception("Arquivo já existe.");
            }

            var response = await _s3Client.PutObjectAsync(uploadRequest);

            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                Console.WriteLine($"Erro ao enviar o arquivo {fileName}: {response.HttpStatusCode}");
                throw new Exception($"Falha ao enviar o arquivo {fileName} para o S3.");
            }

            return $"https://{_bucketName}.s3.{_s3Client.Config.RegionEndpoint.SystemName}.amazonaws.com/{fileName}";
        }

        public async Task<string> UploadProfilePicture(Stream file, string extension) 
        {
            if (file.Length == 0)
            {
                throw new Exception("O arquivo não pode estar vazio.");
            }

            string fileName = $"profile-picture/{TokenServices.GenerateGUIDString()}.{extension}";
            var contentType = GetMimeType(extension);
            string fileAdress = await UploadFileAsync(fileName, file, contentType);
            return fileAdress;
        }

        public static async Task<Stream> GetStreamAsync(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"O arquivo no caminho especificado não foi encontrado: {path}");
            }

            // Abre o arquivo no modo de leitura e retorna como stream
            var memoryStream = new MemoryStream();

            await using (var fileStream = File.OpenRead(path))
            {
                await fileStream.CopyToAsync(memoryStream);
            }

            // Reposiciona o ponteiro do stream para o início
            memoryStream.Position = 0;
            return memoryStream;
        }

        public static string GetMimeType(string extension)
        {
            var mimeTypes = new Dictionary<string, string>
            {
                { ".jpg", "image/jpeg" },
                { ".jpeg", "image/jpeg" },
                { ".png", "image/png" },
                { ".gif", "image/gif" },
                { ".pdf", "application/pdf" },
                { ".txt", "text/plain" },
                { ".doc", "application/msword" },
                { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" }
            };
            return mimeTypes.TryGetValue(extension, out var mimeType) ? mimeType : "application/octet-stream";
        }

    }
}
