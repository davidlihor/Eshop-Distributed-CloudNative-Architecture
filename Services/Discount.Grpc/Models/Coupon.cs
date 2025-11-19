namespace Discount.Grpc.Models;

public class Coupon
{
    public int Id { get; set; }
    public Guid ProductId { get; set; }
    public string CouponCode { get; set; } = null!;
    public string Description { get; set; } = null!;
    public int Amount { get; set; }
}
