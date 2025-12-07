using StreamForge.Domain.Entities;

namespace StreamForge.Application.Interfaces;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface ITokenService
{
    string GenerateToken(User user);
}
