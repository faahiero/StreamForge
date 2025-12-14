using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using StreamForge.Application.Features.Auth.Commands.LoginUser;
using StreamForge.Application.Features.Auth.Commands.RegisterUser;
using Xunit;

namespace StreamForge.Tests.Integration;

[Collection("IntegrationTests")]
public class AuthControllerTests
{
    private readonly HttpClient _client;

    public AuthControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ShouldReturnCreated_WhenDataIsValid()
    {
        // Arrange
        var command = new RegisterUserCommand($"test-{Guid.NewGuid()}@example.com", "Password123!");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Login_ShouldReturnToken_WhenCredentialsAreValid()
    {
        // Arrange
        var email = $"login-{Guid.NewGuid()}@example.com";
        var password = "Password123!";
        
        // 1. Registrar primeiro
        var registerCommand = new RegisterUserCommand(email, password);
        await _client.PostAsJsonAsync("/api/auth/register", registerCommand);

        // Act
        var loginCommand = new LoginUserCommand(email, password);
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<LoginResponse>();
        content.Should().NotBeNull();
        content!.Token.Should().NotBeNullOrEmpty();
    }

    private record LoginResponse(string Token);
}
