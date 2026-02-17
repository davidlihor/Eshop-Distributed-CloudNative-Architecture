namespace Discount.Grpc.Data.DynamoDb;

public sealed class DynamoDbOptions
{
    public const string SectionName = "DynamoDb";

    public string? ServiceUrl { get; set; }
    public string Region { get; set; } = "us-east-1";
    public string TableName { get; set; } = "Coupons";
    public string CountersTableName { get; set; } = "Counters";
    public bool Seed { get; set; } = true;
    public string? AccessKeyId { get; set; }
    public string? SecretAccessKey { get; set; }
}
