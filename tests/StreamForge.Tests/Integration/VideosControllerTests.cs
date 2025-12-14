using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using StreamForge.Application.Features.Auth.Commands.RegisterUser;
using StreamForge.Application.Features.Videos.Commands.InitiateUpload;
using StreamForge.Application.Interfaces;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Interfaces;
using Xunit;

namespace StreamForge.Tests.Integration;

[Collection("IntegrationTests")]
public class VideosControllerTests
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public VideosControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private async Task<string> GetAuthToken(HttpClient client)
    {
        var email = $"video-test-{Guid.NewGuid()}@test.com";
        var password = "Password123!";
        
        await client.PostAsJsonAsync("/api/auth/register", new RegisterUserCommand(email, password));
        var loginRes = await client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        var content = await loginRes.Content.ReadFromJsonAsync<JsonElement>();
        return content.GetProperty("token").GetString()!;
    }

    [Fact]
    public async Task InitiateUpload_ShouldReturn401_WhenNotAuthenticated()
    {
        var client = _factory.CreateClient();
        var command = new InitiateUploadCommand("test.mp4", 100);

        var response = await client.PostAsJsonAsync("/api/videos/init", command);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task InitiateUpload_ShouldReturn400_WhenFileExtensionIsInvalid()
    {
        var client = _factory.CreateClient();
        var token = await GetAuthToken(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var command = new InitiateUploadCommand("virus.exe", 100);

        var response = await client.PostAsJsonAsync("/api/videos/init", command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task InitiateUpload_ShouldReturn200_WhenDataIsValid()
    {
        // Arrange: Mockar infraestrutura para não depender do LocalStack neste teste
        var storageMock = Substitute.For<IStorageService>();
        storageMock.GeneratePresignedUploadUrlAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>())
            .Returns("https://mock-s3.url/upload");

        var repoMock = Substitute.For<IVideoRepository>();

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton(storageMock);
                services.AddSingleton(repoMock);
            });
        }).CreateClient();

        var token = await GetAuthToken(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var command = new InitiateUploadCommand("valid-video.mp4", 5000);

        // Act
        var response = await client.PostAsJsonAsync("/api/videos/init", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<InitiateUploadResult>();
        
        result.Should().NotBeNull();
        result!.UploadUrl.Should().Be("https://mock-s3.url/upload");
        
        // Verificar se salvou no banco mockado
        await repoMock.Received(1).AddAsync(Arg.Is<Video>(v => v.FileName == "valid-video.mp4"));
    }
}
