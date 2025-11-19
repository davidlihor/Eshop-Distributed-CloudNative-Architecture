using BuildingBlocks.Messaging.Discount;
using Discount.Grpc.Models;
using Mapster;

namespace Discount.Grpc.Mappings;

public class MappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.Default.NameMatchingStrategy(NameMatchingStrategy.Flexible).IgnoreNullValues(true);
        config.NewConfig<CouponModel, Coupon>()
            .ConstructUsing(src => new Coupon {
                Id = src.Id,
                ProductId = Guid.Parse(src.ProductId),
                CouponCode = src.CouponCode,
                Description = src.Description,
                Amount = src.Amount
            });
    }
}
