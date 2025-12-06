using StreamForge.Application;
using StreamForge.Infrastructure;
using Serilog;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuração de Logs (Serilog)
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console()
    .ReadFrom.Configuration(ctx.Configuration));

// 2. Observabilidade (OpenTelemetry)
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("StreamForge.API"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation() // Para rastrear chamadas HTTP (incluindo AWS SDK se usar HTTP)
            // .AddAWSInstrumentation() // Requer pacote opcional OpenTelemetry.Contrib.Instrumentation.AWS
            .AddOtlpExporter(); // Envia para Jaeger/Collector (default: localhost:4317)
    });

// 3. Adicionar Camadas (Clean Architecture)
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// 4. Adicionar Controllers e OpenAPI
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 5. Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();