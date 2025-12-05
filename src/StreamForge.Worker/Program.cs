using Serilog;
using StreamForge.Infrastructure;
using StreamForge.Worker;
using StreamForge.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

// 1. Logs
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

builder.Services.AddSerilog((services, lc) => lc
    .WriteTo.Console()
    .ReadFrom.Configuration(builder.Configuration));

// 2. Infraestrutura (AWS, Repos, DI Compartilhada)
builder.Services.AddInfrastructure(builder.Configuration);

// 3. Serviços do Worker
builder.Services.AddSingleton<IVideoProcessor, VideoProcessor>(); // Alterado para Singleton
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();