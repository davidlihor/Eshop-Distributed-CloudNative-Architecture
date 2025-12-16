using BuildingBlocks.Messaging.Events.Product;
using MassTransit;

namespace Ordering.Application.Orders.EventHandlers.Integration;

public class ProductDeletedEventHandler(
    IApplicationDbContext dbContext,
    ILogger<ProductDeletedEventHandler> logger) : IConsumer<ProductDeletedEvent>
{
    public async Task Consume(ConsumeContext<ProductDeletedEvent> context)
    {
        var product = await dbContext.Products.FindAsync(ProductId.Of(context.Message.Id));
        if (product is null) return;

        dbContext.Products.Remove(product);
        await dbContext.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("Integration Event handler: {IntegrationEvent}", context.Message.GetType().Name);
    }
}