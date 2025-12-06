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
using StackExchange.Redis; // Importar

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

        // Clientes AWS
        services.AddSingleton<IAmazonS3>(sp => // Registrado como Singleton
        {
            var s3Config = new AmazonS3Config();
            s3Config.RegionEndpoint = awsOptions.Region;
            if (!string.IsNullOrEmpty(serviceUrl))
            {
                s3Config.ServiceURL = serviceUrl;
                s3Config.ForcePathStyle = true;
            }
            return new AmazonS3Client(awsOptions.Credentials, s3Config);
        });

        services.AddSingleton<IAmazonSQS>(sp => {
            var sqsConfig = new AmazonSQSConfig();
            sqsConfig.RegionEndpoint = awsOptions.Region;
            if (!string.IsNullOrEmpty(serviceUrl))
            {
                sqsConfig.ServiceURL = serviceUrl;
            }
            return new AmazonSQSClient(awsOptions.Credentials, sqsConfig);
        });

        services.AddSingleton<IAmazonDynamoDB>(sp => {
            var dynamoDbConfig = new AmazonDynamoDBConfig();
            dynamoDbConfig.RegionEndpoint = awsOptions.Region;
            if (!string.IsNullOrEmpty(serviceUrl))
            {
                dynamoDbConfig.ServiceURL = serviceUrl;
            }
            return new AmazonDynamoDBClient(awsOptions.Credentials, dynamoDbConfig);
        });

        // Contexto do DynamoDB (High-Level API)
        services.AddSingleton<IDynamoDBContext, DynamoDBContext>(); 

        // Serviços de Infraestrutura
        services.AddSingleton<IStorageService, S3StorageService>(); 
        services.AddSingleton<IVideoRepository, VideoRepository>(); 
        
        // Redis
        services.AddSingleton<IConnectionMultiplexer>(sp => 
            ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis") ?? "localhost:6379"));
        services.AddSingleton<IDistributedLockService, RedisLockService>();

        return services;
    }
}
