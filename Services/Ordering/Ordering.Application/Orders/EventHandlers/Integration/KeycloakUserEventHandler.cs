using BuildingBlocks.Messaging.Events;
using MassTransit;

namespace Ordering.Application.Orders.EventHandlers.Integration;

public class KeycloakUserEventHandler(
    IApplicationDbContext dbContext,
    ILogger<ProductUpdatedEventHandler> logger) : IConsumer<KeycloakUserEvent>
{
    public async Task Consume(ConsumeContext<KeycloakUserEvent> context)
    {
        dbContext.Customers.Add(Customer.Create(CustomerId.Of(context.Message.UserId),
            $"{context.Message.Details.FirstName} {context.Message.Details.LastName}", context.Message.Details.Email));
        await dbContext.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("Integration Event handler: {IntegrationEvent}", context.Message.GetType().Name);
    }
}