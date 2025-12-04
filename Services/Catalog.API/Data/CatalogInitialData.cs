using Marten.Schema;

namespace Catalog.API.Data;

public class CatalogInitialData : IInitialData
{
    public async Task Populate(IDocumentStore store, CancellationToken cancellationToken)
    {
        await using var session = store.LightweightSession();
        if (await session.Query<Product>().AnyAsync(cancellationToken)) return;

        session.Store(GetPreconfiguredProducts());
        await session.SaveChangesAsync(cancellationToken);
    }

    private static IEnumerable<Product> GetPreconfiguredProducts() => 
    [
        new()
        {
            Id = new Guid("5334c996-8457-4cf0-815c-ed2b77c4ff61"),
            Title = "iPhone X 64GB",
            Description = "The iPhone X features an edge-to-edge OLED display, Face ID, and Apple's A11 Bionic chip.",
            ImageUrl = new List<string> { "product-1.png" },
            Price = 499.00M,
            Quantity = 15,
            Category = "SmartPhone",
            Specifications = new List<Specification>()
            {
                new("Display", "5.8-inch OLED"),
                new("Storage", "64GB"),
                new("Camera", "12MP Dual"),
                new("Battery", "2716 mAh"),
            }
        },
        new()
        {
            Id = new Guid("c67d6323-e8b1-4bdf-9a75-b0d0d2e7e914"),
            Title = "Samsung Galaxy S10",
            Description = "Galaxy S10 includes a Dynamic AMOLED display, ultrasonic fingerprint sensor, and triple rear cameras.",
            ImageUrl = new List<string> { "product-2.png" },
            Price = 420.00M,
            Quantity = 4,
            Category = "SmartPhone",
            Specifications = new List<Specification>()
            {
                new("Display", "6.1-inch Dynamic AMOLED"),
                new("Storage", "128GB"),
                new("Camera", "12MP + 16MP + 12MP"),
                new("Battery", "3400 mAh"),
            }
        },
        new()
        {
            Id = new Guid("4f136e9f-ff8c-4c1f-9a33-d12f689bdab8"),
            Title = "Huawei P30",
            Description = "Huawei P30 features an impressive camera system with 3x optical zoom and excellent battery life.",
            ImageUrl = new List<string> { "product-3.png" },
            Price = 350.00M,
            Quantity = 8,
            Category = "SmartPhone",
            Specifications = new List<Specification>()
            {
                new("Display", "6.1-inch OLED"),
                new("Storage", "128GB"),
                new("Camera", "40MP + 16MP + 8MP"),
                new("Battery", "3650 mAh"),
            }
        },
        new()
        {
            Id = new Guid("6ec1297b-ec0a-4aa1-be25-6726e3b51a27"),
            Title = "Xiaomi Mi 9",
            Description = "A budget-friendly flagship with a Snapdragon 855 processor, triple camera system, and fast charging.",
            ImageUrl = new List<string> { "product-4.png" },
            Price = 299.00M,
            Quantity = 0,
            Category = "SmartPhone",
            Specifications = new List<Specification>()
            {
                new("Display", "6.39-inch AMOLED"),
                new("Storage", "128GB"),
                new("Camera", "48MP + 12MP + 16MP"),
                new("Battery", "3300 mAh"),
            }
        },
        new()
        {
            Id = new Guid("b786103d-c621-4f5a-b498-23452610f88c"),
            Title = "HTC U11+",
            Description = "A premium HTC smartphone with Edge Sense, a large display, and powerful BoomSound speakers.",
            ImageUrl = new List<string> { "product-5.png" },
            Price = 250.00M,
            Quantity = 18,
            Category = "SmartPhone",
            Specifications = new List<Specification>()
            {
                new("Display", "6.0-inch Super LCD6"),
                new("Storage", "128GB"),
                new("Camera", "12MP UltraPixel 3"),
                new("Battery", "3930 mAh"),
            }
        },
        new()
        {
            Id = new Guid("c4bbc4a2-4555-45d8-97cc-2a99b2167bff"),
            Title = "LG NeoChef Microwave 25L",
            Description = "A 25L LG NeoChef microwave with Smart Inverter technology for fast and even heating.",
            ImageUrl = new List<string> { "product-6.png" },
            Price = 189.00M,
            Quantity = 0,
            Category = "HomeKitchen",
            Specifications = new List<Specification>()
            {
                new("Capacity", "25 Liters"),
                new("Power", "1000W"),
                new("Technology", "Smart Inverter"),
                new("Programs", "Auto Cook, Defrost"),
            }
        }
    ];
}
