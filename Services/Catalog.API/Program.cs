using BuildingBlocks.Messaging.MassTransit;
using BuildingBlocks.OpenTelemetry;
using BuildingBlocks.Security;
using Catalog.API.Mappings;
using Catalog.API.Services;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

//Application Services
var assembly = typeof(Program).Assembly;
builder.Services.AddCarter();
builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(assembly);
    config.AddOpenBehavior(typeof(ValidationBehavior<,>));
    config.AddOpenBehavior(typeof(LoggingBehavior<,>));
});
builder.Services.AddValidatorsFromAssembly(assembly);
TypeAdapterConfig.GlobalSettings.Scan(typeof(MappingRegister).Assembly);

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorizationWithRoles();

//Data Services
builder.Services.AddMarten(options =>
{
    options.Connection(builder.Configuration.GetConnectionString("Database")!);
}).UseLightweightSessions();

if (builder.Environment.IsDevelopment())
    builder.Services.InitializeMartenWith<CatalogInitialData>();

//Grpc Services
builder.Services.AddGrpc();

//Async Communication Services
builder.Services.AddMessageBroker(builder.Configuration, assembly);

//Cross-Cutting Services
builder.Services.AddExceptionHandler<CustomExceptionHandler>();
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Database")!);

var otlpEndpoint = builder.Configuration["Observability:OtlpEndpoint"] ?? "http://localhost:4317";
builder.Services.AddObservability("Catalog.API", otlpEndpoint);
builder.Logging.AddObservabilityLogging("Catalog.API", otlpEndpoint);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseAuthentication();
app.UseAuthorization();
app.MapCarter();
app.MapGrpcService<ProductService>();
app.UseExceptionHandler(options => { });
app.UseHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();
