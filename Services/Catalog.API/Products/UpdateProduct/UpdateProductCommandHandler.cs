using BuildingBlocks.Messaging.Events.Product;
using MassTransit;

namespace Catalog.API.Products.UpdateProduct;

public record UpdateProductCommand(Guid Id, string Title, string Description, decimal Price, int Quantity, string Category, List<Specification> Specifications) : ICommand<UpdateProductResult>;
public record UpdateProductResult(bool IsSuccess);

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("Product ID is required");
        RuleFor(command => command.Title)
            .NotEmpty().WithMessage("Title is required")
            .Length(2, 150).WithMessage("Title must be between 2 and 150 characters");
        RuleFor(command => command.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0");
    }
}

internal class UpdateProductCommandHandler(
    IDocumentSession session,
    IPublishEndpoint publishEndpoint)
    : ICommandHandler<UpdateProductCommand, UpdateProductResult>
{
    public async Task<UpdateProductResult> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        var product = await session.LoadAsync<Product>(command.Id, cancellationToken);
        if (product is null) throw new ProductNotFoundException(command.Id);

        product = command.Adapt(product);

        session.Update(product);
        await session.SaveChangesAsync(cancellationToken);
        await publishEndpoint.Publish(product.Adapt<ProductUpdatedEvent>(), cancellationToken);

        return new UpdateProductResult(true);
    }
}