namespace Discount.Grpc.Data.DynamoDb;

public interface ISequenceGenerator
{
    Task<int> NextAsync(string sequenceName, CancellationToken cancellationToken);
    Task EnsureAtLeastAsync(string sequenceName, int minValue, CancellationToken cancellationToken);
}
