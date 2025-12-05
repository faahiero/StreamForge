using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Runtime;
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
        // Configuração AWS
        var awsOptions = configuration.GetAWSOptions();

        // Forçar credenciais se estiverem no config (Fix para LocalStack/Dev)
        var accessKey = configuration["AWS:AccessKey"];
        var secretKey = configuration["AWS:SecretKey"];
        var serviceUrl = configuration["AWS:ServiceURL"];

        if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
        {
            awsOptions.Credentials = new BasicAWSCredentials(accessKey, secretKey);
        }

        services.AddDefaultAWSOptions(awsOptions);

        // Clientes AWS
        
        // S3 com configuração Customizada (ForcePathStyle para LocalStack)
        services.AddScoped<IAmazonS3>(sp =>
        {
            var s3Config = new AmazonS3Config();
            
            // Mapeia região
            s3Config.RegionEndpoint = awsOptions.Region;
            
            // Se tem ServiceURL explicita no JSON, aplica ForcePathStyle
            if (!string.IsNullOrEmpty(serviceUrl))
            {
                s3Config.ServiceURL = serviceUrl;
                s3Config.ForcePathStyle = true;
            }

            return new AmazonS3Client(awsOptions.Credentials, s3Config);
        });

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
