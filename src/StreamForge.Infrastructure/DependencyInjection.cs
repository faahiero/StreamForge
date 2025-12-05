using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.S3;
using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StreamForge.Application.Interfaces;
using StreamForge.Domain.Interfaces;
using StreamForge.Infrastructure.Repositories;
using StreamForge.Infrastructure.Services;

namespace StreamForge.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuração AWS (Lê do appsettings ou Variáveis de Ambiente)
        var awsOptions = configuration.GetAWSOptions();
        services.AddDefaultAWSOptions(awsOptions);

        // Clientes AWS
        services.AddAWSService<IAmazonS3>();
        services.AddAWSService<IAmazonSQS>();
        services.AddAWSService<IAmazonDynamoDB>();

        // Contexto do DynamoDB (High-Level API)
        services.AddScoped<IDynamoDBContext, DynamoDBContext>();

        // Serviços
        services.AddScoped<IStorageService, S3StorageService>();
        services.AddScoped<IVideoRepository, VideoRepository>();

        return services;
    }
}
