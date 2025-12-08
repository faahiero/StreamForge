using StreamForge.Domain.Entities;

namespace StreamForge.Application.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
}
