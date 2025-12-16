using BuildingBlocks.Messaging.Events.Product;
using MassTransit;

namespace Ordering.Application.Orders.EventHandlers.Integration;

public class ProductCreatedEventHandler(
    IApplicationDbContext dbContext,
    ILogger<ProductCreatedEventHandler> logger) : IConsumer<ProductCreatedEvent>
{
    public async Task Consume(ConsumeContext<ProductCreatedEvent> context)
    {
        dbContext.Products.Add(Product.Create(ProductId.Of(context.Message.Id), context.Message.Title, context.Message.Price));
        await dbContext.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("Integration Event handler: {IntegrationEvent}", context.Message.GetType().Name);
    }
}