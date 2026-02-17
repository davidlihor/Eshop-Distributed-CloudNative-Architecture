using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Discount.Grpc.Models;
using Microsoft.Extensions.Options;

namespace Discount.Grpc.Data.DynamoDb;

public sealed class DynamoDbCouponRepository(
    IAmazonDynamoDB dynamoDb,
    IOptions<DynamoDbOptions> options,
    ISequenceGenerator sequences) : ICouponRepository
{
    private readonly string _tableName = options.Value.TableName;
    private const string CouponSequence = "coupons";

    public async Task<List<Coupon>> GetAllAsync(CancellationToken cancellationToken)
    {
        var request = new ScanRequest
        {
            TableName = _tableName
        };

        var response = await dynamoDb.ScanAsync(request, cancellationToken);
        return response.Items.Select(MapToCoupon).ToList();
    }

    public async Task<Coupon?> GetByCodeAndProductIdAsync(string couponCode, Guid productId, CancellationToken cancellationToken)
    {
        var request = new QueryRequest
        {
            TableName = _tableName,
            IndexName = "CouponCode-ProductId-index",
            KeyConditionExpression = "CouponCode = :couponCode AND ProductId = :productId",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":couponCode"] = new AttributeValue { S = couponCode },
                [":productId"] = new AttributeValue { S = productId.ToString() }
            }
        };

        var response = await dynamoDb.QueryAsync(request, cancellationToken);
        return response.Items.Count > 0 ? MapToCoupon(response.Items[0]) : null;
    }

    public async Task<Coupon?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var request = new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["Id"] = new AttributeValue { N = id.ToString() }
            }
        };

        var response = await dynamoDb.GetItemAsync(request, cancellationToken);
        return response.IsItemSet ? MapToCoupon(response.Item) : null;
    }

    public async Task<Coupon> CreateAsync(Coupon coupon, CancellationToken cancellationToken)
    {
        if (coupon.Id <= 0)
        {
            coupon.Id = await sequences.NextAsync(CouponSequence, cancellationToken);
        }

        var existing = await GetByCodeAndProductIdAsync(coupon.CouponCode, coupon.ProductId, cancellationToken);
        if (existing != null)
        {
            throw new InvalidOperationException($"Discount with CouponCode '{coupon.CouponCode}' and ProductId '{coupon.ProductId}' already exists.");
        }

        var item = new Dictionary<string, AttributeValue>
        {
            ["Id"] = new AttributeValue { N = coupon.Id.ToString() },
            ["ProductId"] = new AttributeValue { S = coupon.ProductId.ToString() },
            ["CouponCode"] = new AttributeValue { S = coupon.CouponCode },
            ["Description"] = new AttributeValue { S = coupon.Description },
            ["Amount"] = new AttributeValue { N = coupon.Amount.ToString() }
        };

        var request = new PutItemRequest
        {
            TableName = _tableName,
            Item = item,
            ConditionExpression = "attribute_not_exists(Id)"
        };

        try
        {
            await dynamoDb.PutItemAsync(request, cancellationToken);
            return coupon;
        }
        catch (ConditionalCheckFailedException)
        {
            throw new InvalidOperationException($"Coupon with Id {coupon.Id} already exists.");
        }
    }

    public async Task<Coupon> UpdateAsync(Coupon coupon, CancellationToken cancellationToken)
    {
        if (coupon.Id <= 0)
            throw new ArgumentException("Coupon.Id must be a positive integer for updates.", nameof(coupon));

        var existing = await GetByCodeAndProductIdAsync(coupon.CouponCode, coupon.ProductId, cancellationToken);
        if (existing != null && existing.Id != coupon.Id)
        {
            throw new InvalidOperationException($"Discount with CouponCode '{coupon.CouponCode}' and ProductId '{coupon.ProductId}' already exists.");
        }

        var request = new UpdateItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["Id"] = new AttributeValue { N = coupon.Id.ToString() }
            },
            UpdateExpression = "SET ProductId = :productId, CouponCode = :couponCode, Description = :description, Amount = :amount",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":productId"] = new AttributeValue { S = coupon.ProductId.ToString() },
                [":couponCode"] = new AttributeValue { S = coupon.CouponCode },
                [":description"] = new AttributeValue { S = coupon.Description },
                [":amount"] = new AttributeValue { N = coupon.Amount.ToString() }
            },
            ConditionExpression = "attribute_exists(Id)",
            ReturnValues = ReturnValue.ALL_NEW
        };

        try
        {
            await dynamoDb.UpdateItemAsync(request, cancellationToken);
            return coupon;
        }
        catch (ConditionalCheckFailedException)
        {
            throw new KeyNotFoundException($"Discount with Id \"{coupon.Id}\" not found.");
        }
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var request = new DeleteItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["Id"] = new AttributeValue { N = id.ToString() }
            },
            ReturnValues = ReturnValue.ALL_OLD
        };

        var response = await dynamoDb.DeleteItemAsync(request, cancellationToken);
        return response.Attributes.Count > 0;
    }

    private static Coupon MapToCoupon(Dictionary<string, AttributeValue> item)
    {
        return new Coupon
        {
            Id = int.Parse(item["Id"].N),
            ProductId = Guid.Parse(item["ProductId"].S),
            CouponCode = item["CouponCode"].S,
            Description = item["Description"].S,
            Amount = int.Parse(item["Amount"].N)
        };
    }
}
