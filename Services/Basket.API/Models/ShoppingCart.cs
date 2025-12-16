namespace Basket.API.Models;

public class ShoppingCart
{
    public string UserId { get; set; }
    public List<ShoppingCartItem> Items { get; init; } = [];
    public string? CouponCode { get; set; }
    public decimal TotalPrice => Items.Sum(x => x.Price * x.Quantity);

    public ShoppingCart(string userId)
    {
        UserId = userId;
    }

    // Required for Mapping
    public ShoppingCart()
    {

    }
}
