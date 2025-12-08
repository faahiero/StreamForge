using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.SQS;
using Amazon.SimpleNotificationService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options; // Importar
using StreamForge.Application.Interfaces;
using StreamForge.Domain.Interfaces;
using StreamForge.Infrastructure.Repositories;
using StreamForge.Infrastructure.Services;
using StreamForge.Infrastructure.Mappers;
using StreamForge.Infrastructure.Options; // Importar
using StackExchange.Redis;

namespace StreamForge.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configurar Options
        services.Configure<AwsSettings>(configuration.GetSection(AwsSettings.SectionName));
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        // Obter configurações para setup inicial dos clientes (que são Singletons)
        // Nota: Para Singleton, precisamos extrair o valor agora ou usar factory com IOptions
        var awsSettings = new AwsSettings();
        configuration.GetSection(AwsSettings.SectionName).Bind(awsSettings);

        // Configuração AWS SDK Base
        var awsOptions = configuration.GetAWSOptions();
        
        // Override com valores do Options se existirem (LocalStack/Dev)
        if (!string.IsNullOrEmpty(awsSettings.AccessKey) && !string.IsNullOrEmpty(awsSettings.SecretKey))
        {
            awsOptions.Credentials = new BasicAWSCredentials(awsSettings.AccessKey, awsSettings.SecretKey);
        }
        if (!string.IsNullOrEmpty(awsSettings.Region))
        {
            awsOptions.Region = Amazon.RegionEndpoint.GetBySystemName(awsSettings.Region);
        }

        // Clientes AWS (Singleton Factory)
        
        // S3
        services.AddSingleton<IAmazonS3>(sp => 
        {
            var settings = sp.GetRequiredService<IOptions<AwsSettings>>().Value;
            var s3Config = new AmazonS3Config();
            
            // Se o Region do appsettings for válido, usa. Senão usa o do profile.
            if(!string.IsNullOrEmpty(settings.Region)) 
                s3Config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(settings.Region);
            else 
                s3Config.RegionEndpoint = awsOptions.Region;

            if (!string.IsNullOrEmpty(settings.ServiceURL))
            {
                s3Config.ServiceURL = settings.ServiceURL;
                s3Config.ForcePathStyle = true;
            }

            var creds = awsOptions.Credentials; // Default
            if (!string.IsNullOrEmpty(settings.AccessKey) && !string.IsNullOrEmpty(settings.SecretKey))
                creds = new BasicAWSCredentials(settings.AccessKey, settings.SecretKey);

            return new AmazonS3Client(creds, s3Config);
        });

        // SQS
        services.AddSingleton<IAmazonSQS>(sp => {
            var settings = sp.GetRequiredService<IOptions<AwsSettings>>().Value;
            var sqsConfig = new AmazonSQSConfig();
            
            if(!string.IsNullOrEmpty(settings.Region)) 
                sqsConfig.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(settings.Region);
            else 
                sqsConfig.RegionEndpoint = awsOptions.Region;

            if (!string.IsNullOrEmpty(settings.ServiceURL))
            {
                sqsConfig.ServiceURL = settings.ServiceURL;
            }

            var creds = awsOptions.Credentials;
            if (!string.IsNullOrEmpty(settings.AccessKey) && !string.IsNullOrEmpty(settings.SecretKey))
                creds = new BasicAWSCredentials(settings.AccessKey, settings.SecretKey);

            return new AmazonSQSClient(creds, sqsConfig);
        });

        // DynamoDB
        services.AddSingleton<IAmazonDynamoDB>(sp => {
            var settings = sp.GetRequiredService<IOptions<AwsSettings>>().Value;
            var dbConfig = new AmazonDynamoDBConfig();
            
            if(!string.IsNullOrEmpty(settings.Region)) 
                dbConfig.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(settings.Region);
            else 
                dbConfig.RegionEndpoint = awsOptions.Region;

            if (!string.IsNullOrEmpty(settings.ServiceURL))
            {
                dbConfig.ServiceURL = settings.ServiceURL;
            }

            var creds = awsOptions.Credentials;
            if (!string.IsNullOrEmpty(settings.AccessKey) && !string.IsNullOrEmpty(settings.SecretKey))
                creds = new BasicAWSCredentials(settings.AccessKey, settings.SecretKey);

            return new AmazonDynamoDBClient(creds, dbConfig);
        });

        // SNS
        services.AddSingleton<IAmazonSimpleNotificationService>(sp => {
            var settings = sp.GetRequiredService<IOptions<AwsSettings>>().Value;
            var snsConfig = new AmazonSimpleNotificationServiceConfig();
            
            if(!string.IsNullOrEmpty(settings.Region)) 
                snsConfig.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(settings.Region);
            else 
                snsConfig.RegionEndpoint = awsOptions.Region;

            if (!string.IsNullOrEmpty(settings.ServiceURL))
            {
                snsConfig.ServiceURL = settings.ServiceURL;
            }

            var creds = awsOptions.Credentials;
            if (!string.IsNullOrEmpty(settings.AccessKey) && !string.IsNullOrEmpty(settings.SecretKey))
                creds = new BasicAWSCredentials(settings.AccessKey, settings.SecretKey);

            return new AmazonSimpleNotificationServiceClient(creds, snsConfig);
        });

        // Contexto do DynamoDB (High-Level API)
        services.AddSingleton<IDynamoDBContext, DynamoDBContext>(); 

        // Configurar Mapster
        MapsterConfig.Configure();

        // Serviços de Infraestrutura
        services.AddSingleton<IStorageService, S3StorageService>(); 
        services.AddSingleton<IVideoRepository, VideoRepository>(); 
        services.AddSingleton<IMessagePublisher, SnsMessagePublisher>(); 
        services.AddSingleton<IMediaAnalyzer, FfmpegMediaAnalyzer>();
        
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
