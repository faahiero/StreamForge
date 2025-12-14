using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace StreamForge.Tests.Integration;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var inMemorySettings = new Dictionary<string, string?> {
                {"Jwt:Key", "super-secret-key-for-test-1234567890"},
                {"Jwt:Issuer", "StreamForgeTest"},
                {"Jwt:Audience", "StreamForgeTestUsers"},
                {"AWS:ServiceURL", "http://localhost:4566"},
                {"AWS:BucketName", "test-bucket"},
                {"AWS:AccessKey", "test"},
                {"AWS:SecretKey", "test"}
            };

            config.AddInMemoryCollection(inMemorySettings);
        });
    }
}
