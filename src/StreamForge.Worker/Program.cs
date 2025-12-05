using Amazon.SQS;
using Serilog;
using StreamForge.Worker;

var builder = Host.CreateApplicationBuilder(args);

// 1. Configuração de Logs
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

builder.Services.AddSerilog((services, lc) => lc
    .WriteTo.Console()
    .ReadFrom.Configuration(builder.Configuration));

// 2. Configuração AWS (Mesma lógica da API para LocalStack)
var awsOptions = builder.Configuration.GetAWSOptions();
var accessKey = builder.Configuration["AWS:AccessKey"];
var secretKey = builder.Configuration["AWS:SecretKey"];

if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
{
    awsOptions.Credentials = new Amazon.Runtime.BasicAWSCredentials(accessKey, secretKey);
}

builder.Services.AddDefaultAWSOptions(awsOptions);
builder.Services.AddAWSService<IAmazonSQS>();

// 3. Registrar o Worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();