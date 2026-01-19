using System.Net;
using System.Reflection;
using BuildingBlocks.Messaging.Product;
using BuildingBlocks.OpenTelemetry;
using BuildingBlocks.Resilience;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ordering.Application.Common;
using Ordering.Application.Orders.EventHandlers.Integration;
using Ordering.Infrastructure.Caching;
using Ordering.Infrastructure.GrpcServices;
using Ordering.Infrastructure.Identity;
using Polly;
using RabbitMQ.Client;
using StackExchange.Redis;

namespace Ordering.Infrastructure.Config;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILoggingBuilder logging,
        Assembly assembly)
    {
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<DispatchDomainEventsInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var entityInterceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
            var eventsInterceptor = sp.GetRequiredService<DispatchDomainEventsInterceptor>();

            options.AddInterceptors(entityInterceptor, eventsInterceptor);
            options.UseNpgsql(configuration.GetConnectionString("Database"));
        });
        services.AddScoped<IApplicationDbContext, ApplicationDbContext>();

        services.AddHttpClient<IKeycloakService, KeycloakService>().AddPolicyHandler(_ =>
            Policy.HandleResult<HttpResponseMessage>(result =>
                    result.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.ServiceUnavailable)
            .WaitAndRetryAsync(5, retryCount => TimeSpan.FromSeconds(Math.Pow(2, retryCount)))
        );
        services.Configure<KeycloakOptions>(configuration.GetSection("Keycloak"));

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis")!));
        services.AddScoped<ICacheService, RedisCacheService>();

        services.AddResilienceGrpcClient<ProductProtoService.ProductProtoServiceClient>
            (configuration, environment, "GrpcSettings:CatalogUrl");
        services.AddScoped<IProductGrpcService, ProductGrpcService>();

        services.AddMassTransit(config =>
        {
            config.SetKebabCaseEndpointNameFormatter();
            config.AddConsumers(assembly);
            config.UsingRabbitMq((context, configurator) =>
            {
                configurator.Host(new Uri(configuration["MessageBroker:Host"]!), host =>
                {
                    host.Username(configuration["MessageBroker:UserName"]!);
                    host.Password(configuration["MessageBroker:Password"]!);
                });
                configurator.ReceiveEndpoint("keycloak-events", endpoint =>
                {
                    endpoint.Bind("keycloak.exchange", callback =>
                    {
                        callback.RoutingKey = "KK.EVENT.CLIENT.eshop.SUCCESS.*.REGISTER";
                        callback.ExchangeType = ExchangeType.Topic;
                    });
                    endpoint.ClearSerialization();
                    endpoint.UseRawJsonSerializer();
                    endpoint.ConfigureConsumer<KeycloakUserEventHandler>(context);
                });
                configurator.ConfigureEndpoints(context);
            });
        });

        var otlpEndpoint = configuration["Observability:OtlpEndpoint"] ?? "http://localhost:4317";
        services.AddObservability("Ordering.API", otlpEndpoint);
        logging.AddObservabilityLogging("Ordering.API", otlpEndpoint);

        return services;
    }
}
