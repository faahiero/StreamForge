using Amazon.DynamoDBv2.DataModel;
using Mapster; // Importar Mapster
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
        return document?.Adapt<Video>();
    }

    public async Task AddAsync(Video video)
    {
        var document = video.Adapt<VideoDocument>();
        await _context.SaveAsync(document);
    }

    public async Task UpdateAsync(Video video)
    {
        var document = video.Adapt<VideoDocument>();
        await _context.SaveAsync(document);
    }
}