using MediatR;

namespace StreamForge.Application.Features.Auth.Commands.RegisterUser;

public record RegisterUserCommand(string Email, string Password) : IRequest<Guid>;
