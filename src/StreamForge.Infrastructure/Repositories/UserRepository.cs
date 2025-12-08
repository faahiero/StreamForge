using Amazon.DynamoDBv2.DataModel;
using Mapster;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Interfaces;
using StreamForge.Infrastructure.Persistence.DynamoDb;

namespace StreamForge.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDynamoDBContext _context;

    public UserRepository(IDynamoDBContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        var document = await _context.LoadAsync<UserDocument>(email);
        var documento = document;
        return document?.Adapt<User>();
    }

    public async Task AddAsync(User user)
    {
        var document = user.Adapt<UserDocument>();
        await _context.SaveAsync(document);
    }
}
