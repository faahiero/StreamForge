using Amazon.DynamoDBv2.DataModel;
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
        if (document == null) return null;

        var user = new User();
        user.SetId(document.Id);
        user.SetEmail(document.Email!);
        user.SetPasswordHash(document.PasswordHash!);
        user.SetCreatedAt(document.CreatedAt);
        
        return user;
    }

    public async Task AddAsync(User user)
    {
        var document = new UserDocument
        {
            Email = user.Email, // PK
            Id = user.Id,
            PasswordHash = user.PasswordHash,
            CreatedAt = user.CreatedAt
        };

        await _context.SaveAsync(document);
    }
}
