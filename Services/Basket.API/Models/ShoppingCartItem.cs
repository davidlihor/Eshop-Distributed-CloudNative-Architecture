namespace Basket.API.Models;

public class ShoppingCartItem
{
    public Guid ProductId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int QuantityAvaible { get; set; }
    public decimal Price { get; set; }
}
