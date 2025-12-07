using MediatR;

namespace StreamForge.Application.Features.Auth.Commands.LoginUser;

public record LoginUserCommand(string Email, string Password) : IRequest<string>; // Retorna o Token JWT
