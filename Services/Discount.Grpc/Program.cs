using BuildingBlocks.Security;
using Discount.Grpc.Behaviors;
using Discount.Grpc.Data;
using Discount.Grpc.Mappings;
using Discount.Grpc.Middleware;
using Discount.Grpc.Services;
using FluentValidation;
using HealthChecks.UI.Client;
using Mapster;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(typeof(Program).Assembly);
    config.AddOpenBehavior(typeof(RequestLoggingPipelineBehavior<,>));
});
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
TypeAdapterConfig.GlobalSettings.Scan(typeof(MappingRegister).Assembly);

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorizationWithRoles();

builder.Services.AddDbContext<DiscountContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("Database"));
});

builder.Services.AddGrpc()
    .AddJsonTranscoding();

builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig.ReadFrom.Configuration(context.Configuration);
    var seqUrl = context.Configuration.GetValue<string>("Seq:ServerUrl");
    
    if (!string.IsNullOrWhiteSpace(seqUrl))
    {
        loggerConfig.WriteTo.Seq(seqUrl);
    }
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("Discount.Grpc"))
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter();
    });

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseMigration();
app.MapGrpcService<DiscountService>();
app.UseMiddleware<RequestLogContextMiddleware>();
app.UseSerilogRequestLogging();
app.UseHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
app.Run();
