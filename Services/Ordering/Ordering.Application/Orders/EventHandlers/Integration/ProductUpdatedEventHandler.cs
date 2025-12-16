using BuildingBlocks.Messaging.Events.Product;
using Mapster;
using MassTransit;

namespace Ordering.Application.Orders.EventHandlers.Integration;

public class ProductUpdatedEventHandler(
    IApplicationDbContext dbContext,
    ILogger<ProductUpdatedEventHandler> logger) : IConsumer<ProductUpdatedEvent>
{
    public async Task Consume(ConsumeContext<ProductUpdatedEvent> context)
    {
        var product = await dbContext.Products.FindAsync(ProductId.Of(context.Message.Id));
        if (product is null) return;

        product.Adapt(context.Message);
        await dbContext.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("Integration Event handler: {IntegrationEvent}", context.Message.GetType().Name);
    }
}