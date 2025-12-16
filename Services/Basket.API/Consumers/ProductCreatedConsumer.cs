using System.Globalization;
using BuildingBlocks.Messaging.Events.Product;
using BuildingBlocks.Messaging.Product;
using MassTransit;
using Microsoft.Extensions.Caching.Hybrid;

namespace Basket.API.Consumers;

public class ProductCreatedConsumer(HybridCache cache) : IConsumer<ProductCreatedEvent>
{
    public async Task Consume(ConsumeContext<ProductCreatedEvent> context)
    {
        await cache.SetAsync($"products-{context.Message.Id}", new GetProductResponse
        {
            Product = new ProductModel
            {
                ProductId = context.Message.Id.ToString(),
                Title = context.Message.Title,
                Quantity = context.Message.Quantity,
                Price = context.Message.Price.ToString(CultureInfo.InvariantCulture)
            }
        },
            tags: ["products"],
            cancellationToken: context.CancellationToken);
    }
}