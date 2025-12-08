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
    private readonly IMessagePublisher _publisher;
    private readonly IMediaAnalyzer _mediaAnalyzer; // Injetar Analyzer
    private readonly ILogger<VideoProcessor> _logger;

    public VideoProcessor(IAmazonS3 s3Client, IVideoRepository videoRepository, 
                          IMessagePublisher publisher, IMediaAnalyzer mediaAnalyzer, ILogger<VideoProcessor> logger)
    {
        _s3Client = s3Client;
        _videoRepository = videoRepository;
        _publisher = publisher;
        _mediaAnalyzer = mediaAnalyzer;
        _logger = logger;
    }

    public async Task ProcessVideoAsync(S3EventNotificationRecord record)
    {
        var bucketName = record.S3?.Bucket?.Name;
        var objectKey = record.S3?.Object?.Key;

        if (string.IsNullOrEmpty(bucketName) || string.IsNullOrEmpty(objectKey)) return;

        objectKey = System.Net.WebUtility.UrlDecode(objectKey);
        var videoIdString = objectKey.Split('/')[1];
        if (!Guid.TryParse(videoIdString, out var videoId)) return;

        var video = await _videoRepository.GetByIdAsync(videoId);
        if (video == null) return;

        video.MarkAsProcessing();
        await _videoRepository.UpdateAsync(video);

        var tempFile = Path.Combine(Path.GetTempPath(), $"{videoId}.mp4");

        try
        {
            _logger.LogInformation("📥 Baixando arquivo do S3...");
            var s3Object = await _s3Client.GetObjectAsync(bucketName, objectKey);
            using (var fileStream = File.Create(tempFile))
            {
                await s3Object.ResponseStream.CopyToAsync(fileStream);
            }

            _logger.LogInformation("🎥 Analisando mídia com FFprobe...");
            var metadata = await _mediaAnalyzer.AnalyzeAsync(tempFile);

            _logger.LogInformation("✅ Metadados: Duração {Duration}, Formato {Format}", metadata.Duration, metadata.Format);

            video.CompleteProcessing(metadata.Duration, metadata.Format);
            await _videoRepository.UpdateAsync(video);

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
            _logger.LogError(ex, "❌ Falha no processamento real");
            video.FailProcessing();
            await _videoRepository.UpdateAsync(video);
            throw;
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
