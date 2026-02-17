using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Discount.Grpc.Models;
using Microsoft.Extensions.Options;

namespace Discount.Grpc.Data.DynamoDb;

public sealed class DynamoDbInitializerHostedService(
    ILogger<DynamoDbInitializerHostedService> logger,
    IOptions<DynamoDbOptions> options,
    IAmazonDynamoDB dynamoDb,
    ISequenceGenerator sequences) : IHostedService
{
    private readonly DynamoDbOptions _options = options.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Seed)
        {
            logger.LogInformation("DynamoDB initializer is disabled (DynamoDb:Seed=false).");
            return;
        }

        await EnsureTablesAsync(cancellationToken);
        await SeedCouponsAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task EnsureTablesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await EnsureCouponsTableAsync(cancellationToken);
            
            await EnsureCountersTableAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to ensure DynamoDB tables. They may already exist or there may be permission issues.");
        }
    }

    private async Task EnsureCouponsTableAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dynamoDb.DescribeTableAsync(_options.TableName, cancellationToken);
            logger.LogInformation("Coupons table already exists.");
        }
        catch (ResourceNotFoundException)
        {
            logger.LogInformation("Creating Coupons table...");
            
            var request = new CreateTableRequest
            {
                TableName = _options.TableName,
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition { AttributeName = "Id", AttributeType = ScalarAttributeType.N },
                    new AttributeDefinition { AttributeName = "CouponCode", AttributeType = ScalarAttributeType.S },
                    new AttributeDefinition { AttributeName = "ProductId", AttributeType = ScalarAttributeType.S }
                },
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement { AttributeName = "Id", KeyType = KeyType.HASH }
                },
                GlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
                {
                    new GlobalSecondaryIndex
                    {
                        IndexName = "CouponCode-ProductId-index",
                        KeySchema = new List<KeySchemaElement>
                        {
                            new KeySchemaElement { AttributeName = "CouponCode", KeyType = KeyType.HASH },
                            new KeySchemaElement { AttributeName = "ProductId", KeyType = KeyType.RANGE }
                        },
                        Projection = new Projection { ProjectionType = ProjectionType.ALL },
                        ProvisionedThroughput = new ProvisionedThroughput
                        {
                            ReadCapacityUnits = 5,
                            WriteCapacityUnits = 5
                        }
                    }
                },
                ProvisionedThroughput = new ProvisionedThroughput
                {
                    ReadCapacityUnits = 5,
                    WriteCapacityUnits = 5
                },
                BillingMode = BillingMode.PROVISIONED
            };

            await dynamoDb.CreateTableAsync(request, cancellationToken);
            
            await WaitForTableActiveAsync(_options.TableName, cancellationToken);
            logger.LogInformation("Coupons table created successfully.");
        }
    }

    private async Task EnsureCountersTableAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dynamoDb.DescribeTableAsync(_options.CountersTableName, cancellationToken);
            logger.LogInformation("Counters table already exists.");
        }
        catch (ResourceNotFoundException)
        {
            logger.LogInformation("Creating Counters table...");
            
            var request = new CreateTableRequest
            {
                TableName = _options.CountersTableName,
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition { AttributeName = "Id", AttributeType = ScalarAttributeType.S }
                },
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement { AttributeName = "Id", KeyType = KeyType.HASH }
                },
                ProvisionedThroughput = new ProvisionedThroughput
                {
                    ReadCapacityUnits = 5,
                    WriteCapacityUnits = 5
                },
                BillingMode = BillingMode.PROVISIONED
            };

            await dynamoDb.CreateTableAsync(request, cancellationToken);
            
            await WaitForTableActiveAsync(_options.CountersTableName, cancellationToken);
            logger.LogInformation("Counters table created successfully.");
        }
    }

    private async Task WaitForTableActiveAsync(string tableName, CancellationToken cancellationToken)
    {
        var maxAttempts = 30;
        var attempt = 0;

        while (attempt < maxAttempts)
        {
            try
            {
                var response = await dynamoDb.DescribeTableAsync(tableName, cancellationToken);
                if (response.Table.TableStatus == TableStatus.ACTIVE)
                {
                    return;
                }
            }
            catch (Exception)
            {

            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            attempt++;
        }

        logger.LogWarning("Timeout waiting for table {TableName} to become active.", tableName);
    }

    private async Task SeedCouponsAsync(CancellationToken cancellationToken)
    {
        var seed = new[]
        {
            new Coupon
            {
                Id = 1,
                CouponCode = "IPHONE15",
                ProductId = Guid.Parse("5334c996-8457-4cf0-815c-ed2b77c4ff61"),
                Description = "15$ Discount",
                Amount = 15
            },
            new Coupon
            {
                Id = 2,
                CouponCode = "Samsung10",
                ProductId = Guid.Parse("c67d6323-e8b1-4bdf-9a75-b0d0d2e7e914"),
                Description = "10$ Discount",
                Amount = 10
            }
        };

        foreach (var coupon in seed)
        {
            try
            {
                var item = new Dictionary<string, AttributeValue>
                {
                    ["Id"] = new AttributeValue { N = coupon.Id.ToString() },
                    ["ProductId"] = new AttributeValue { S = coupon.ProductId.ToString() },
                    ["CouponCode"] = new AttributeValue { S = coupon.CouponCode },
                    ["Description"] = new AttributeValue { S = coupon.Description },
                    ["Amount"] = new AttributeValue { N = coupon.Amount.ToString() }
                };

                var putRequest = new PutItemRequest
                {
                    TableName = _options.TableName,
                    Item = item
                };

                await dynamoDb.PutItemAsync(putRequest, cancellationToken);
                logger.LogInformation("Seeded coupon with Id {Id}", coupon.Id);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed seeding coupon with Id {Id}. Continuing.", coupon.Id);
            }
        }

        await sequences.EnsureAtLeastAsync("coupons", seed.Max(x => x.Id), cancellationToken);
    }
}
