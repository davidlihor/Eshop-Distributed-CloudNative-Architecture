using BuildingBlocks.Messaging.Discount;
using FluentValidation;

namespace Discount.Grpc.Validators;

public class CouponModelValidator : AbstractValidator<CouponModel>
{
  public CouponModelValidator()
  {
    RuleFor(x => x.ProductId)
        .NotEmpty()
        .Must(pid => Guid.TryParse(pid, out _)).WithMessage("ProductId must be a valid GUID");

    RuleFor(x => x.CouponCode)
        .NotEmpty()
        .MaximumLength(50);

    RuleFor(x => x.Description)
        .MaximumLength(500);

    RuleFor(x => x.Amount)
        .GreaterThanOrEqualTo(0);
  }
}
