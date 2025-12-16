using BuildingBlocks.Messaging.Product;
using Ordering.Application.Common;

namespace Ordering.Infrastructure.GrpcServices;

public class ProductGrpcService(ProductProtoService.ProductProtoServiceClient productProto) : IProductGrpcService
{
    public async Task<List<Product>> GetProductsByIdsAsync(List<Guid> productsIds)
    {
        var ids = productsIds.Select(id => id.ToString()).ToList();
        var response = await productProto.GetProductsByIdsAsync(new GetProductsByIdsRequest { Ids = { ids } });
        return [..response.Products.Select(p =>
            Product.Create(ProductId.Of(Guid.Parse(p.ProductId)), p.Title, decimal.Parse(p.Price)))];
    }
}