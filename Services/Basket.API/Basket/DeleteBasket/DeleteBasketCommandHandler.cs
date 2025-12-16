using BuildingBlocks.Security;

namespace Basket.API.Basket.DeleteBasket;

public record DeleteBasketCommand(Guid ProductId) : ICommand<DeleteBasketResult>;
public record DeleteBasketResult(bool IsSuccess);

public class DeleteBasketCommandHandler(
    IBasketRepository repository,
    IUserContextService userContext)
    : ICommandHandler<DeleteBasketCommand, DeleteBasketResult>
{
    public async Task<DeleteBasketResult> Handle(DeleteBasketCommand command, CancellationToken cancellationToken)
    {
        var basket = await repository.GetBasket(userContext.GetUserId(), cancellationToken);
        if (basket is null) return new DeleteBasketResult(true);

        var result = basket.Items.FirstOrDefault(x => x.ProductId == command.ProductId);
        if (result != null) basket.Items.Remove(result);

        if (basket.Items.Count == 0) await repository.DeleteBasket(userContext.GetUserId(), cancellationToken);
        if (basket.Items.Count != 0) await repository.StoreBasket(basket, cancellationToken);

        return new DeleteBasketResult(true);
    }
}
