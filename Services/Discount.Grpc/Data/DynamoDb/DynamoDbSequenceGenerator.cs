using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Options;

namespace Discount.Grpc.Data.DynamoDb;

public sealed class DynamoDbSequenceGenerator(
    IAmazonDynamoDB dynamoDb,
    IOptions<DynamoDbOptions> options) : ISequenceGenerator
{
    private readonly string _tableName = options.Value.CountersTableName;

    public async Task<int> NextAsync(string sequenceName, CancellationToken cancellationToken)
    {
        var request = new UpdateItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["Id"] = new AttributeValue { S = sequenceName }
            },
            UpdateExpression = "ADD #value :inc",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#value"] = "Value"
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":inc"] = new AttributeValue { N = "1" }
            },
            ReturnValues = ReturnValue.UPDATED_NEW
        };

        var response = await dynamoDb.UpdateItemAsync(request, cancellationToken);
        return int.Parse(response.Attributes["Value"].N);
    }

    public async Task EnsureAtLeastAsync(string sequenceName, int minValue, CancellationToken cancellationToken)
    {
        try
        {
            var getRequest = new GetItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["Id"] = new AttributeValue { S = sequenceName }
                }
            };

            var getResponse = await dynamoDb.GetItemAsync(getRequest, cancellationToken);

            if (!getResponse.IsItemSet || !getResponse.Item.ContainsKey("Value") ||
                int.Parse(getResponse.Item["Value"].N) < minValue)
            {
                var putRequest = new PutItemRequest
                {
                    TableName = _tableName,
                    Item = new Dictionary<string, AttributeValue>
                    {
                        ["Id"] = new AttributeValue { S = sequenceName },
                        ["Value"] = new AttributeValue { N = minValue.ToString() }
                    }
                };

                await dynamoDb.PutItemAsync(putRequest, cancellationToken);
            }
        }
        catch (Exception)
        {
            var putRequest = new PutItemRequest
            {
                TableName = _tableName,
                Item = new Dictionary<string, AttributeValue>
                {
                    ["Id"] = new AttributeValue { S = sequenceName },
                    ["Value"] = new AttributeValue { N = minValue.ToString() }
                }
            };

            await dynamoDb.PutItemAsync(putRequest, cancellationToken);
        }
    }
}
