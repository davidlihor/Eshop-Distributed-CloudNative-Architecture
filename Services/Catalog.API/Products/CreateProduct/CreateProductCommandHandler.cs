using BuildingBlocks.Messaging.Events.Product;
using MassTransit;

namespace Catalog.API.Products.CreateProduct;

public record CreateProductCommand(string Title, string Description, decimal Price, int Quantity, string Category, List<Specification> Specifications) : ICommand<CreateProductResult>;
public record CreateProductResult(Guid Id);

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Title is required");
        RuleFor(x => x.Price).GreaterThan(0).WithMessage("Price must be greater than 0");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than 0");
        RuleFor(x => x.Category).NotEmpty().WithMessage("Category is required");
    }
}

internal class CreateProductCommandHandler(
    IDocumentSession session,
    IPublishEndpoint publishEndpoint)
    : ICommandHandler<CreateProductCommand, CreateProductResult>
{
    public async Task<CreateProductResult> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var product = Product.Create(
            command.Title,
            command.Description,
            command.Price,
            command.Quantity,
            command.Category,
            command.Specifications);

        session.Store(product);
        await session.SaveChangesAsync(cancellationToken);
        await publishEndpoint.Publish(product.Adapt<ProductCreatedEvent>(), cancellationToken);

        return new CreateProductResult(product.Id);
    }
}