using Discount.Grpc.Models;

namespace Discount.Grpc.Data.DynamoDb;

public interface ICouponRepository
{
    Task<List<Coupon>> GetAllAsync(CancellationToken cancellationToken);
    Task<Coupon?> GetByCodeAndProductIdAsync(string couponCode, Guid productId, CancellationToken cancellationToken);
    Task<Coupon?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<Coupon> CreateAsync(Coupon coupon, CancellationToken cancellationToken);
    Task<Coupon> UpdateAsync(Coupon coupon, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
