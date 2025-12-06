using Serilog;
using StreamForge.Infrastructure;
using StreamForge.Worker;
using StreamForge.Worker.Services;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;

var builder = Host.CreateApplicationBuilder(args);

// 1. Logs
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

builder.Services.AddSerilog((services, lc) => lc
    .WriteTo.Console()
    .ReadFrom.Configuration(builder.Configuration));

// 2. Observabilidade (OpenTelemetry)
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("StreamForge.Worker"))
            .AddHttpClientInstrumentation()
            .AddOtlpExporter();
    });

// 3. Infraestrutura (AWS, Repos, DI Compartilhada)
builder.Services.AddInfrastructure(builder.Configuration);

// 4. Serviços do Worker
builder.Services.AddSingleton<QueueInitializer>(); // Inicializador de Fila
builder.Services.AddSingleton<IVideoProcessor, VideoProcessor>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

// 5. Inicialização de Infraestrutura (Auto-Provisionamento em Dev)
// Resolvemos o QueueInitializer e rodamos a verificação
var queueInitializer = host.Services.GetRequiredService<QueueInitializer>();
await queueInitializer.EnsureQueueExistsAsync();

host.Run();