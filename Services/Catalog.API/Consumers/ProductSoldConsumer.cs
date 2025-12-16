using BuildingBlocks.Messaging.Events.Product;
using MassTransit;

namespace Catalog.API.Consumers;

public class ProductSoldConsumer(IDocumentSession session) : IConsumer<ProductSoldEvent>
{
    public async Task Consume(ConsumeContext<ProductSoldEvent> context)
    {
        var ids = context.Message.ProductsSold.Select(x => x.Id).ToList();

        var existingProducts = await session.Query<Product>()
            .Where(p => ids.Contains(p.Id))
            .ToListAsync();

        var soldLookup = context.Message.ProductsSold.ToDictionary(x => x.Id, x => x.Quantity);

        foreach (var product in existingProducts)
        {
            if (soldLookup.TryGetValue(product.Id, out var quantitySold))
            {
                product.Quantity -= quantitySold;
                session.Update(product);
            }
        }

        await session.SaveChangesAsync(context.CancellationToken);
    }
}