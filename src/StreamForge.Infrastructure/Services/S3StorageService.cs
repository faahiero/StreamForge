using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options; // Importar Options
using StreamForge.Application.Interfaces;
using StreamForge.Infrastructure.Options; // Importar Options Class

namespace StreamForge.Infrastructure.Services;

public class S3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    // _externalServiceUrl removido por simplificação ou deve ser adicionado ao AwsSettings se crítico.
    // Para este refactor, vamos focar no padrão Options.

    public S3StorageService(IAmazonS3 s3Client, IOptions<AwsSettings> options)
    {
        _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
        _bucketName = options.Value.BucketName ?? "streamforge-videos";
    }

    public async Task<string> GeneratePresignedUploadUrlAsync(string key, string contentType, TimeSpan expiration)
    {
        await EnsureBucketExistsAsync();

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(expiration),
            ContentType = contentType
        };

        request.ResponseHeaderOverrides.ContentType = contentType;

        // Retorna a URL gerada pelo SDK (que já deve estar configurado com a ServiceURL correta via DI)
        return _s3Client.GetPreSignedURL(request);
    }

    private async Task EnsureBucketExistsAsync()
    {
        try
        {
            await _s3Client.GetBucketLocationAsync(_bucketName);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            await _s3Client.PutBucketAsync(_bucketName);
        }
    }
}
