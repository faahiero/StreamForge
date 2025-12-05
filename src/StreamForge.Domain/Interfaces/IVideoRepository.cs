using StreamForge.Domain.Entities;

namespace StreamForge.Domain.Interfaces;

public interface IVideoRepository
{
    Task<Video?> GetByIdAsync(Guid id);
    Task AddAsync(Video video);
    Task UpdateAsync(Video video);
}
