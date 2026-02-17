using BuildingBlocks.OpenTelemetry;
using BuildingBlocks.Security;
using Discount.Grpc.Data.DynamoDb;
using Discount.Grpc.Mappings;
using Discount.Grpc.Services;
using FluentValidation;
using HealthChecks.UI.Client;
using Mapster;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(typeof(Program).Assembly);
});
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
TypeAdapterConfig.GlobalSettings.Scan(typeof(MappingRegister).Assembly);

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorizationWithRoles();

builder.Services.AddDiscountDynamoDb(builder.Configuration);

builder.Services.AddGrpc()
    .AddJsonTranscoding();

builder.Services.AddHealthChecks();

var otlpEndpoint = builder.Configuration["Observability:OtlpEndpoint"] ?? "http://localhost:4317";
builder.Services.AddObservability("Discount.gRPC", otlpEndpoint);
builder.Logging.AddObservabilityLogging("Discount.gRPC", otlpEndpoint);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapGrpcService<DiscountService>();
app.UseHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
app.Run();
