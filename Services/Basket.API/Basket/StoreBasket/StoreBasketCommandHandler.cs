using BuildingBlocks.Messaging.Product;
using BuildingBlocks.Security;
using Microsoft.Extensions.Caching.Hybrid;

namespace Basket.API.Basket.StoreBasket;

public record StoreBasketCommand(CartProductDto Product, string Coupon) : ICommand<StoreBasketResult>;
public record StoreBasketResult(bool IsSuccess);

public class StoreBasketCommandValidator : AbstractValidator<StoreBasketCommand>
{
    public StoreBasketCommandValidator()
    {
        RuleFor(x => x.Product).NotNull()
            .When(x => string.IsNullOrEmpty(x.Coupon))
            .WithMessage("Either a Product or a Coupon must be provided.");
        RuleFor(x => x.Product.Quantity)
            .GreaterThan(0)
            .When(x => x?.Product != null)
            .WithMessage("Quantity must be greater than 0 if a Product is specified.");
        RuleFor(x => x.Coupon).NotEmpty()
            .When(x => x?.Product == null)
            .WithMessage("A coupon must be provided if no Product is specified.");
    }
}

public class StoreBasketCommandHandler(
    ProductProtoService.ProductProtoServiceClient client, 
    HybridCache cache,
    IBasketRepository repository,
    IUserContextService userContext)
    : ICommandHandler<StoreBasketCommand, StoreBasketResult>
{
    public async Task<StoreBasketResult> Handle(StoreBasketCommand command, CancellationToken cancellationToken)
    {
        var basket = await repository.GetBasket(userContext.GetUserId(), cancellationToken);
        if (basket is null && command?.Product is null) return new StoreBasketResult(false);
        
        basket ??= new ShoppingCart 
        {
            UserId = userContext.GetUserId(),
            Items = []
        };
        
        if(command?.Product != null)
        {
            var cachedProduct = await cache.GetOrCreateAsync($"products-{command.Product.ProductId}", async _ => 
                await client.GetProductAsync(new GetProductRequest {
                    ProductId = command.Product.ProductId.ToString()
                }, cancellationToken: cancellationToken),
            tags: ["products"], 
            cancellationToken: cancellationToken);
            
            if(cachedProduct.Product == null || cachedProduct.Product.Quantity < command.Product.Quantity) return new StoreBasketResult(false);

            var existingItem = basket.Items.FirstOrDefault(x => x.ProductId == command.Product.ProductId);
            if (existingItem == null)
            {
                basket.Items.Add(new ShoppingCartItem 
                {
                    ProductId = command.Product.ProductId,
                    Title = cachedProduct.Product.Title,
                    Quantity = command.Product.Quantity
                });
            }
            else
            {
                existingItem.Quantity = command.Product.Quantity;
            }
        }

        if (!string.IsNullOrWhiteSpace(command?.Coupon))
            basket.CouponCode = command?.Coupon;
        
        if (command?.Coupon == string.Empty)
            basket.CouponCode = null;
            
        await repository.StoreBasket(basket, cancellationToken);
        return new StoreBasketResult(true);
    }
}
