using Amazon.DynamoDBv2.DataModel;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Interfaces;

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
        return await _context.LoadAsync<Video>(id);
    }

    public async Task AddAsync(Video video)
    {
        await _context.SaveAsync(video);
    }

    public async Task UpdateAsync(Video video)
    {
        await _context.SaveAsync(video);
    }
}
