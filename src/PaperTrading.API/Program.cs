using PaperTrading.API.Middleware;
using PaperTrading.Infrastructure;
using Serilog;
using Serilog.Context;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

// Add services to the container.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

// Correlation ID handling
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
        ?? Guid.NewGuid().ToString("N");

    context.Response.Headers["X-Correlation-Id"] = correlationId;

    using (LogContext.PushProperty("CorrelationId", correlationId))
    {
        await next();
    }
});

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "PaperTrading API v1");
    });

    // Verification endpoint for forced exception testing
    app.MapGet("/api/test/error", () =>
    {
        throw new InvalidOperationException("Forced test exception for error handling verification");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}
