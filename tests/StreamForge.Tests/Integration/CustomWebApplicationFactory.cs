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
            // Aqui podemos adicionar um appsettings.Test.json ou variáveis de ambiente
            var inMemorySettings = new Dictionary<string, string?> {
                {"Jwt:Key", "super-secret-key-for-test-1234567890"},
                {"Jwt:Issuer", "StreamForgeTest"},
                {"Jwt:Audience", "StreamForgeTestUsers"},
                {"AWS:ServiceURL", "http://localhost:4566"}, // Aponta para LocalStack se estiver rodando
                {"AWS:BucketName", "test-bucket"},
                {"AWS:AccessKey", "test"},
                {"AWS:SecretKey", "test"}
            };

            config.AddInMemoryCollection(inMemorySettings);
        });
    }
}
