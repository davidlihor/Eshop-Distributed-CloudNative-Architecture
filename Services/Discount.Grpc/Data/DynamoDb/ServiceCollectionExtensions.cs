using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Microsoft.Extensions.Options;

namespace Discount.Grpc.Data.DynamoDb;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDiscountDynamoDb(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DynamoDbOptions>()
            .Bind(configuration.GetSection(DynamoDbOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Region), "DynamoDB region must be provided");

        services.AddSingleton<IAmazonDynamoDB>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<DynamoDbOptions>>().Value;
            var config = new AmazonDynamoDBConfig
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(opts.Region)
            };

            if (!string.IsNullOrWhiteSpace(opts.ServiceUrl))
            {
                config.ServiceURL = opts.ServiceUrl;
            }

            if (!string.IsNullOrWhiteSpace(opts.AccessKeyId) && !string.IsNullOrWhiteSpace(opts.SecretAccessKey))
            {
                var credentials = new BasicAWSCredentials(opts.AccessKeyId, opts.SecretAccessKey);
                return new AmazonDynamoDBClient(credentials, config);
            }

            return new AmazonDynamoDBClient(config);
        });

        services.AddSingleton<ISequenceGenerator, DynamoDbSequenceGenerator>();
        services.AddSingleton<ICouponRepository, DynamoDbCouponRepository>();
        services.AddHostedService<DynamoDbInitializerHostedService>();

        return services;
    }
}
