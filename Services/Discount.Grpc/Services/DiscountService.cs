using BuildingBlocks.Messaging.Discount;
using Discount.Grpc.Data.DynamoDb;
using Discount.Grpc.Models;
using FluentValidation;
using Grpc.Core;
using Mapster;
using Microsoft.AspNetCore.Authorization;

namespace Discount.Grpc.Services;

public class DiscountService(
    ICouponRepository coupons,
    ILogger<DiscountService> logger,
    IValidator<CouponModel> couponValidator
    ) : DiscountProtoService.DiscountProtoServiceBase
{
    public override async Task<GetAllResponse> GetDiscountList(GetAllRequest request, ServerCallContext context)
    {
        var couponsList = await coupons.GetAllAsync(context.CancellationToken);
        var response = new GetAllResponse();

        response.Discounts.AddRange(couponsList.Adapt<List<CouponModel>>());
        return response;
    }

    public override async Task<CouponModel?> GetDiscount(GetDiscountRequest request, ServerCallContext context)
    {
        var coupon = await coupons.GetByCodeAndProductIdAsync(
            request.CouponCode,
            Guid.Parse(request.ProductId),
            context.CancellationToken);

        if (coupon is null) return new CouponModel();

        logger.LogInformation("Discount is retrieved for: {@coupon}", coupon);

        var couponModel = coupon.Adapt<CouponModel>();
        return couponModel;
    }

    [Authorize(Roles = "Admin")]
    public override async Task<CouponModel> CreateDiscount(CreateDiscountRequest request, ServerCallContext context)
    {
        var dto = request?.Coupon ?? throw new RpcException(new Status(StatusCode.InvalidArgument, "Missing coupon"));

        var validation = await couponValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            throw new RpcException(new Status(StatusCode.InvalidArgument, string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));

        var coupon = request.Coupon.Adapt<Coupon>();

        try
        {
            coupon = await coupons.CreateAsync(coupon, context.CancellationToken);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, "Discount already exists for this CouponCode and ProductId."));
        }

        logger.LogInformation("Discount is successfully created. CouponCode: {@CouponCode}", coupon);
        return coupon.Adapt<CouponModel>();
    }

    [Authorize(Roles = "Admin")]
    public override async Task<CouponModel> UpdateDiscount(UpdateDiscountRequest request, ServerCallContext context)
    {
        var dto = request?.Coupon ?? throw new RpcException(new Status(StatusCode.InvalidArgument, "Missing coupon"));

        var validation = await couponValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            throw new RpcException(new Status(StatusCode.InvalidArgument, string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));

        var coupon = dto.Adapt<Coupon>();
        try
        {
            coupon = await coupons.UpdateAsync(coupon, context.CancellationToken);
        }
        catch (KeyNotFoundException)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Discount with Id \"{dto.Id}\" not found."));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, "Discount already exists for this CouponCode and ProductId."));
        }

        logger.LogInformation("Discount is successfully updated. CouponCode: {CouponCode}", coupon.CouponCode);
        return coupon.Adapt<CouponModel>();
    }


    [Authorize(Roles = "Admin")]
    public override async Task<DeleteDiscountResponse> DeleteDiscount(DeleteDiscountRequest request, ServerCallContext context)
    {
        var existing = await coupons.GetByIdAsync(request.DiscountId, context.CancellationToken) ?? throw new RpcException(new Status(StatusCode.NotFound, $"Discount with Id \"{request.DiscountId}\" not found."));
        if (!await coupons.DeleteAsync(request.DiscountId, context.CancellationToken))
            throw new RpcException(new Status(StatusCode.NotFound, $"Discount with Id \"{request.DiscountId}\" not found."));

        logger.LogInformation("Discount is successfully deleted. CouponCode: {CouponCode}", existing.CouponCode);
        return new DeleteDiscountResponse { Success = true };
    }
}
