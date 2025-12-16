using Discount.Grpc.Models;
using Microsoft.EntityFrameworkCore;

namespace Discount.Grpc.Data;

public class DiscountContext(DbContextOptions<DiscountContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Coupon>().HasData(
            new Coupon
            {
                Id = 1,
                CouponCode = "IPHONE15",
                ProductId = Guid.Parse("5334c996-8457-4cf0-815c-ed2b77c4ff61"),
                Description = "15$ Discount",
                Amount = 15
            },
            new Coupon
            {
                Id = 2,
                CouponCode = "Samsung10",
                ProductId = Guid.Parse("c67d6323-e8b1-4bdf-9a75-b0d0d2e7e914"),
                Description = "10$ Discount",
                Amount = 10
            }
        );
    }

    public DbSet<Coupon> Coupons => Set<Coupon>();
}
