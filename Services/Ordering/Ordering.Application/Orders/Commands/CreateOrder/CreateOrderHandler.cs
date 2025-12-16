using BuildingBlocks.Messaging.Events.Product;
using MassTransit;
using Newtonsoft.Json;
using Ordering.Application.Config;

namespace Ordering.Application.Orders.Commands.CreateOrder;

public class CreateOrderHandler(
    IApplicationDbContext dbContext,
    ICacheService cacheService,
    IProductGrpcService productService,
    IKeycloakService keycloakService,
    IPublishEndpoint publishEndpoint)
    : ICommandHandler<CreateOrderCommand, CreateOrderResult>
{
    public async Task<CreateOrderResult> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var settings = new JsonSerializerSettings { Converters = { new ProductIdJsonConverter() } };
        var customer = await dbContext.Customers.FindAsync([CustomerId.Of(command.Order.CustomerId)], cancellationToken);
        if (customer == null)
        {
            var user = await keycloakService.GetUserByUserIdAsync(command.Order.CustomerId);
            if (user == null) throw new Exception("Customer not found");

            dbContext.Customers.Add(Customer.Create(CustomerId.Of(user.Id), $"{user.FirstName} {user.LastName}", user.Email));
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var order = command.Order.ToOrder();
        var productIds = order.OrderItems.Select(x => x.ProductId.Value).ToList();
        var redisKeys = productIds.Select(id => $"product:{id}").ToArray();
        var redisResults = await cacheService.GetMultipleAsync(redisKeys);

        var foundInRedis = new List<Product>();
        var missingIds = new List<Guid>();

        foreach (var productId in productIds)
        {
            var redisKey = $"product:{productId}";
            if (redisResults.TryGetValue(redisKey, out var redisValue) && redisValue != null)
            {
                if (redisValue == "not_found")
                {
                    order.Remove(ProductId.Of(productId));
                    continue;
                };
                if (!string.IsNullOrEmpty(redisValue))
                    foundInRedis.Add(JsonConvert.DeserializeObject<Product>(redisValue, settings)!);
            }
            else
            {
                missingIds.Add(productId);
            }
        }

        if (missingIds.Count != 0)
        {
            var productsFromDb = await dbContext.Products
                .Where(p => missingIds.Select(ProductId.Of).Contains(p.Id))
                .ToListAsync(cancellationToken);

            foreach (var product in productsFromDb)
            {
                var redisKey = $"product:{product.Id.Value}";
                await cacheService.SetAsync(redisKey, JsonConvert.SerializeObject(product, settings), TimeSpan.FromMinutes(10));
                foundInRedis.Add(product);
                missingIds.Remove(product.Id.Value);
            }
        }

        if (missingIds.Count != 0)
        {
            var productsFromCatalog = await productService.GetProductsByIdsAsync(missingIds);

            await dbContext.Products.AddRangeAsync(productsFromCatalog, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            foreach (var product in productsFromCatalog)
            {
                var redisKey = $"product:{product.Id.Value}";
                await cacheService.SetAsync(redisKey, JsonConvert.SerializeObject(product, settings), TimeSpan.FromMinutes(10));
                foundInRedis.Add(product);
                missingIds.Remove(product.Id.Value);
            }

            foreach (var redisKey in missingIds.Select(missingId => $"product:{missingId}"))
            {
                await cacheService.SetAsync(redisKey, "not_found", TimeSpan.FromMinutes(10));
            }

            foreach (var missingId in missingIds) order.Remove(ProductId.Of(missingId));
        }

        if (foundInRedis.Count != 0)
        {
            dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync(cancellationToken);

            var soldProducts = order.OrderItems.Select(x => new ProductSold { Id = x.ProductId.Value, Quantity = x.Quantity }).ToList();
            await publishEndpoint.Publish(new ProductSoldEvent { ProductsSold = soldProducts }, cancellationToken);
        }

        return new CreateOrderResult(order.Id.Value);
    }
}
