using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.SQS;
using Amazon.SimpleNotificationService; // Importar SNS
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StreamForge.Application.Interfaces;
using StreamForge.Domain.Interfaces;
using StreamForge.Infrastructure.Repositories;
using StreamForge.Infrastructure.Services;
using StackExchange.Redis;

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
        
        // S3
        services.AddSingleton<IAmazonS3>(sp => 
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

        // SQS
        services.AddSingleton<IAmazonSQS>(sp => {
            var sqsConfig = new AmazonSQSConfig();
            sqsConfig.RegionEndpoint = awsOptions.Region;
            if (!string.IsNullOrEmpty(serviceUrl))
            {
                sqsConfig.ServiceURL = serviceUrl;
            }
            return new AmazonSQSClient(awsOptions.Credentials, sqsConfig);
        });

        // DynamoDB
        services.AddSingleton<IAmazonDynamoDB>(sp => {
            var dynamoDbConfig = new AmazonDynamoDBConfig();
            dynamoDbConfig.RegionEndpoint = awsOptions.Region;
            if (!string.IsNullOrEmpty(serviceUrl))
            {
                dynamoDbConfig.ServiceURL = serviceUrl;
            }
            return new AmazonDynamoDBClient(awsOptions.Credentials, dynamoDbConfig);
        });

        // SNS (Novo)
        services.AddSingleton<IAmazonSimpleNotificationService>(sp => {
            var snsConfig = new AmazonSimpleNotificationServiceConfig();
            snsConfig.RegionEndpoint = awsOptions.Region;
            if (!string.IsNullOrEmpty(serviceUrl))
            {
                snsConfig.ServiceURL = serviceUrl;
            }
            return new AmazonSimpleNotificationServiceClient(awsOptions.Credentials, snsConfig);
        });

        // Contexto do DynamoDB
        services.AddSingleton<IDynamoDBContext, DynamoDBContext>(); 

        // Serviços
        services.AddSingleton<IStorageService, S3StorageService>(); 
        services.AddSingleton<IVideoRepository, VideoRepository>(); 
        services.AddSingleton<IMessagePublisher, SnsMessagePublisher>(); 
        services.AddSingleton<IMediaAnalyzer, FfmpegMediaAnalyzer>(); // Registrar Analyzer
        
        // Auth
        services.AddSingleton<IUserRepository, UserRepository>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenService, TokenService>();
        
        // Redis
        services.AddSingleton<IConnectionMultiplexer>(sp => 
            ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis") ?? "localhost:6379"));
        services.AddSingleton<IDistributedLockService, RedisLockService>();

        return services;
    }
}