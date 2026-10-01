using HobbitCoder.TMTBucaramanga.Sheepfold.Application;
using HobbitCoder.TMTBucaramanga.Sheepfold.Persistence.PostgreSQL;
using HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI;
using HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI.Common.Models;
using HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI.Handlers;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Cargar variables de entorno en la configuración
builder.Configuration.AddEnvironmentVariables();

// Add services to the container.
builder.AddPostgreSQLPersistence();
builder.AddApplication();
builder.AddWebAPI();

builder.Services.AddHttpContextAccessor();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ErrorExceptionHandler>();

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var corsSettings = builder.Configuration.GetSection("CorsSettings").Get<CorsSettings>()?.PolicyName ?? "DefaultCorsPolicy";
app.UseCors(corsSettings);

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            duration = report.TotalDuration,
            entries = report.Entries.Select(entry => new { name = entry.Key, status = entry.Value.Status.ToString(), description = entry.Value.Description, data = entry.Value.Data, exception = entry.Value.Exception?.Message, duration = entry.Value.Duration.ToString(), tags = entry.Value.Tags })
        });
        await context.Response.WriteAsync(result);
    }
});

await app.RunAsync();
