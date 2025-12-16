namespace Catalog.API.Models;

public class Product
{
    public Guid Id { get; init; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string Category { get; set; } = string.Empty;
    public List<string> ImageUrl { get; set; } = null!;
    public List<Specification> Specifications { get; set; } = [];

    public static Product Create(string title, string description, decimal price, int quantity, string category, List<Specification> specifications)
    {
        return new Product
        {
            Title = title,
            Description = description,
            Price = price,
            Quantity = quantity,
            Category = category,
            Specifications = specifications
        };
    }

    public void AddSpecification(string key, string value)
    {
        var order = Specifications.Count != 0 ? Specifications.Max(s => s.Order) + 1 : 1;
        Specifications.Add(new Specification { Key = key, Value = value, Order = order });
    }

    public void RemoveSpecification(string key)
    {
        var specification = Specifications.Find(s => s.Key == key);
        if (specification != null) Specifications.Remove(specification);
    }

    public void ReorderSpecification(string key, int newOrder)
    {
        var specification = Specifications.Find(s => s.Key == key);
        if (specification == null) return;

        Specifications.Remove(specification);
        specification.Order = newOrder;
        Specifications.Add(specification);
        Specifications = Specifications.OrderBy(s => s.Order).ToList();
    }
}
