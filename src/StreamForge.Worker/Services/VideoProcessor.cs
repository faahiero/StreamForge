using Amazon.S3;
using Amazon.S3.Model;
using StreamForge.Application.Interfaces; // Importar
using StreamForge.Domain.Interfaces;
using StreamForge.Worker.Models;

namespace StreamForge.Worker.Services;

public interface IVideoProcessor
{
    Task ProcessVideoAsync(S3EventNotificationRecord record);
}

public class VideoProcessor : IVideoProcessor
{
    private readonly IAmazonS3 _s3Client;
    private readonly IVideoRepository _videoRepository;
    private readonly IMessagePublisher _publisher; // Injetar Publisher
    private readonly ILogger<VideoProcessor> _logger;

    public VideoProcessor(IAmazonS3 s3Client, IVideoRepository videoRepository, 
                          IMessagePublisher publisher, ILogger<VideoProcessor> logger)
    {
        _s3Client = s3Client;
        _videoRepository = videoRepository;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task ProcessVideoAsync(S3EventNotificationRecord record)
    {
        var bucketName = record.S3?.Bucket?.Name;
        var objectKey = record.S3?.Object?.Key;

        if (string.IsNullOrEmpty(bucketName) || string.IsNullOrEmpty(objectKey))
        {
            _logger.LogWarning("Evento S3 inválido: Bucket ou Key nulos.");
            return;
        }

        objectKey = System.Net.WebUtility.UrlDecode(objectKey);

        _logger.LogInformation("🎬 Iniciando processamento do vídeo: {Key}", objectKey);

        var videoIdString = objectKey.Split('/')[1];
        if (!Guid.TryParse(videoIdString, out var videoId))
        {
            _logger.LogError("❌ Falha ao extrair VideoId da chave: {Key}", objectKey);
            return;
        }

        var video = await _videoRepository.GetByIdAsync(videoId);
        if (video == null)
        {
            _logger.LogError("❌ Vídeo não encontrado no banco: {VideoId}", videoId);
            return;
        }

        video.MarkAsProcessing();
        await _videoRepository.UpdateAsync(video);

        try
        {
            var metadata = await _s3Client.GetObjectMetadataAsync(bucketName, objectKey);
            _logger.LogInformation("📥 Arquivo verificado no S3. Tamanho: {Size} bytes", metadata.ContentLength);

            await Task.Delay(2000); 
            
            var simulatedDuration = TimeSpan.FromSeconds(new Random().Next(60, 3600));
            var simulatedFormat = "mp4";

            video.CompleteProcessing(simulatedDuration, simulatedFormat);
            await _videoRepository.UpdateAsync(video);

            _logger.LogInformation("✅ Vídeo {VideoId} processado com sucesso!", videoId);

            // Publicar Evento no SNS
            await _publisher.PublishAsync("streamforge-video-events", new 
            {
                VideoId = video.Id,
                Status = "Completed",
                Duration = video.Duration,
                ProcessedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Falha no processamento do vídeo {VideoId}", videoId);
            video.FailProcessing();
            await _videoRepository.UpdateAsync(video);
            throw;
        }
    }
}
