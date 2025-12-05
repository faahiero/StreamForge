using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using StreamForge.Application.Interfaces;

namespace StreamForge.Infrastructure.Services;

public class S3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public S3StorageService(IAmazonS3 s3Client, IConfiguration configuration)
    {
        _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
        _bucketName = configuration["AWS:BucketName"] ?? "streamforge-videos"; // Fallback ou lança erro
    }

    public async Task<string> GeneratePresignedUploadUrlAsync(string key, string contentType, TimeSpan expiration)
    {
        // Garante que o bucket existe (apenas para ambiente de dev/localstack)
        // Em produção, o bucket já deve existir via Terraform/Bicep
        await EnsureBucketExistsAsync();

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(expiration),
            ContentType = contentType
        };

        // Metadata opcional para rastreamento
        request.ResponseHeaderOverrides.ContentType = contentType;

        return _s3Client.GetPreSignedURL(request);
    }

    private async Task EnsureBucketExistsAsync()
    {
        // Check simplificado. Em produção de alta escala, evite fazer isso a cada request.
        // Aqui é útil porque o LocalStack zera quando reinicia.
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
