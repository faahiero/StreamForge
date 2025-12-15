using StreamForge.Application;
using StreamForge.Infrastructure;
using StreamForge.Infrastructure.HealthChecks; 
using Serilog;
using StreamForge.API.Middlewares;
using StreamForge.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 1. Logging
builder.AddStreamForgeLogging();

// 2. Observabilidade
builder.Services.AddStreamForgeObservability();

// 3. Camadas (Clean Arch)
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// 4. Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<S3HealthCheck>("S3")
    .AddCheck<DynamoDBHealthCheck>("DynamoDB");

// 6. Tratamento de Erros
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddStreamForgeAuthentication(builder.Configuration);

// 5. Autenticação e Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddStreamForgeSwagger();

var app = builder.Build();

// 7. Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (httpContext, elapsed, ex) =>
    {
        if (ex != null || httpContext.Response.StatusCode > 499)
        {
            if (ex is StreamForge.Domain.Exceptions.DomainException || 
                ex is FluentValidation.ValidationException ||
                ex is UnauthorizedAccessException)
            {
                return Serilog.Events.LogEventLevel.Warning;
            }
            return Serilog.Events.LogEventLevel.Error;
        }
        return Serilog.Events.LogEventLevel.Information;
    };
});

app.UseExceptionHandler(); // DEPOIS do Serilog
app.UseHttpsRedirection();

app.UseAuthentication(); 
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();