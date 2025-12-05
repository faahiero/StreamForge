using Amazon.DynamoDBv2.DataModel;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Interfaces;
using StreamForge.Infrastructure.Persistence.DynamoDb;

namespace StreamForge.Infrastructure.Repositories;

public class VideoRepository : IVideoRepository
{
    private readonly IDynamoDBContext _context;

    public VideoRepository(IDynamoDBContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Video?> GetByIdAsync(Guid id)
    {
        var document = await _context.LoadAsync<VideoDocument>(id);
        return ToDomain(document);
    }

    public async Task AddAsync(Video video)
    {
        var document = ToDocument(video);
        await _context.SaveAsync(document);
    }

    public async Task UpdateAsync(Video video)
    {
        var document = ToDocument(video);
        await _context.SaveAsync(document);
    }

    // Mapeamento Manual (Domain -> Document)
    private VideoDocument ToDocument(Video video)
    {
        return new VideoDocument
        {
            Id = video.Id,
            FileName = video.FileName,
            OriginalName = video.OriginalName,
            FileSize = video.FileSize,
            Status = video.Status,
            CreatedAt = video.CreatedAt,
            ProcessedAt = video.ProcessedAt,
            S3Key = video.S3Key,
            Duration = video.Duration,
            Format = video.Format
        };
    }

    // Mapeamento Manual (Document -> Domain)
    private Video? ToDomain(VideoDocument? document)
    {
        if (document == null) return null;

        // Usando construtor padrão e setters (ou setters expostos)
        var video = new Video();
        video.SetId(document.Id);
        video.SetFileName(document.FileName!);
        video.SetOriginalName(document.OriginalName!);
        if(document.FileSize.HasValue) video.SetFileSize(document.FileSize.Value);
        video.SetStatus(document.Status);
        video.SetCreatedAt(document.CreatedAt);
        video.SetProcessedAt(document.ProcessedAt);
        video.SetS3Key(document.S3Key!);
        video.SetDuration(document.Duration);
        video.SetFormat(document.Format);

        return video;
    }
}