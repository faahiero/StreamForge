using MediatR;
using StreamForge.Application.Interfaces;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Exceptions; // Importar
using StreamForge.Domain.Interfaces;

namespace StreamForge.Application.Features.Auth.Commands.RegisterUser;

public class RegisterUserHandler : IRequestHandler<RegisterUserCommand, Guid>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUserHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Guid> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // 1. Check if user exists
        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new ValidationDomainException("User already exists.");
        }

        // 2. Hash Password
        var passwordHash = _passwordHasher.Hash(request.Password);

        // 3. Create User
        var user = new User(request.Email, passwordHash);

        // 4. Save
        await _userRepository.AddAsync(user);

        return user.Id;
    }
}
