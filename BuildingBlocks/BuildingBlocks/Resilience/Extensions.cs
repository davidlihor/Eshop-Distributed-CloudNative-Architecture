using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Polly;

namespace BuildingBlocks.Resilience;

public static class Extensions
{
    public static IServiceCollection AddResilienceGrpcClient<TClient>(
        this IServiceCollection services,
        IConfiguration config,
        IHostEnvironment environment,
        string grpcSettingsKey) where TClient : class
    {
        services.AddGrpcClient<TClient>((serviceProvider, options) =>
        {
            options.Address = new Uri(config[grpcSettingsKey]!);
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
            if (environment.IsDevelopment())
            {
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }
            return handler;
        })
        .AddResilienceHandler(typeof(TClient).Name + "GrpcResilienceHandler", options =>
        {
            var serviceProvider = services.BuildServiceProvider();
            var logger = serviceProvider.GetService<ILogger<TClient>>();
            var random = new Random();

            options.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 5,
                OnRetry = args =>
                {
                    logger?.LogWarning("Delaying for {TotalSeconds} seconds, then making retry {AttemptNumber}",
                        args.Duration.TotalSeconds, args.AttemptNumber);
                    return ValueTask.CompletedTask;
                },
                DelayGenerator = args =>
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, args.AttemptNumber - 1))
                                       + TimeSpan.FromMilliseconds(random.Next(1000));
                    return new ValueTask<TimeSpan?>(delay);
                }
            });
            options.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                BreakDuration = TimeSpan.FromSeconds(15),
                FailureRatio = 0.5,
                MinimumThroughput = 10,
                OnHalfOpened = _ =>
                {
                    logger?.LogInformation("Circuit is half-open. Testing service...");
                    return default;
                },
                OnOpened = args =>
                {
                    logger?.LogInformation("Circuit opened: {Exception}, Duration: for {TotalSeconds} seconds",
                        args.Outcome.Exception, args.BreakDuration.TotalSeconds);
                    return default;
                },
                OnClosed = _ =>
                {
                    logger?.LogInformation("Circuit closed. Service is back to normal.");
                    return default;
                }
            });
        });

        return services;
    }
}