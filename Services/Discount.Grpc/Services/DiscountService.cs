using BuildingBlocks.Messaging.Discount;
using Discount.Grpc.Data;
using Discount.Grpc.Models;
using FluentValidation;
using Grpc.Core;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Discount.Grpc.Services;

public class DiscountService(
    DiscountContext dbContext,
    ILogger<DiscountService> logger,
    IValidator<CouponModel> couponValidator
    ) : DiscountProtoService.DiscountProtoServiceBase
{
    public override async Task<GetAllResponse> GetDiscountList(GetAllRequest request, ServerCallContext context)
    {
        var coupons = await dbContext.Coupons.ToListAsync();
        var response = new GetAllResponse();

        response.Discounts.AddRange(coupons.Adapt<List<CouponModel>>());
        return response;
    }

    public override async Task<CouponModel?> GetDiscount(GetDiscountRequest request, ServerCallContext context)
    {
        var coupon = await dbContext.Coupons.FirstOrDefaultAsync(coupon => coupon.CouponCode == request.CouponCode &&
            coupon.ProductId == Guid.Parse(request.ProductId));

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

        dbContext.Coupons.Add(coupon);
        await dbContext.SaveChangesAsync();

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

        var coupon = await dbContext.Coupons.FirstOrDefaultAsync(c => c.Id == dto.Id) ??
            throw new RpcException(new Status(StatusCode.NotFound, $"Discount with Id \"{dto.Id}\" not found."));

        dto.Adapt(coupon);

        dbContext.Coupons.Update(coupon);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Discount is successfully updated. CouponCode: {CouponCode}", coupon.CouponCode);
        return coupon.Adapt<CouponModel>();
    }


    [Authorize(Roles = "Admin")]
    public override async Task<DeleteDiscountResponse> DeleteDiscount(DeleteDiscountRequest request, ServerCallContext context)
    {
        var coupon = await dbContext.Coupons.FirstOrDefaultAsync(x => x.Id == request.DiscountId) ??
            throw new RpcException(new Status(StatusCode.NotFound, $"Discount with Id \"{request.DiscountId}\" not found."));
    
        dbContext.Coupons.Remove(coupon);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Discount is successfully deleted. CouponCode: {CouponCode}", coupon.CouponCode);
        return new DeleteDiscountResponse { Success = true };
    }
}
