using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WaterDelivery.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private static readonly List<Product> Products = GenerateProducts(5000);
        private static readonly Random Random = new();

        [HttpGet]
        [EnableRateLimiting("UserPolicy")]
        public async Task<IActionResult> GetProducts(
            [FromQuery] int page = 1,
            [FromQuery] int size = 20,
            [FromQuery] string? search = null,
            [FromQuery] string? category = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null)
        {
            // Simulate async operation
            await Task.Delay(Random.Next(1, 10));

            var query = Products.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                        p.Description.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            var total = query.Count();
            var items = query
                .Skip((page - 1) * size)
                .Take(size)
                .ToList();

            return Ok(new
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = size,
                TotalPages = (int)Math.Ceiling(total / (double)size),
                Filters = new
                {
                    Search = search,
                    Category = category,
                    MinPrice = minPrice,
                    MaxPrice = maxPrice
                }
            });
        }

        [HttpGet("{id:int}")]
        [EnableRateLimiting("UserPolicy")]
        public async Task<IActionResult> GetProduct(int id)
        {
            await Task.Delay(Random.Next(1, 5));

            var product = Products.FirstOrDefault(p => p.Id == id);
            if (product == null)
                return NotFound($"Product with ID {id} not found");

            return Ok(product);
        }

        [HttpGet("categories")]
        public IActionResult GetCategories()
        {
            var categories = Products
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            return Ok(categories);
        }

        [HttpGet("stats")]
        [EnableRateLimiting("UserPolicy")]
        public async Task<IActionResult> GetStats()
        {
            await Task.Delay(Random.Next(5, 15));

            var stats = new
            {
                TotalProducts = Products.Count,
                TotalCategories = Products.Select(p => p.Category).Distinct().Count(),
                AveragePrice = Products.Average(p => p.Price),
                MinPrice = Products.Min(p => p.Price),
                MaxPrice = Products.Max(p => p.Price),
                TotalInStock = Products.Sum(p => p.InStock),
                OutOfStock = Products.Count(p => p.InStock == 0),
                GeneratedAt = DateTime.UtcNow
            };

            return Ok(stats);
        }

        [HttpGet("bulk")]
        public async Task<IActionResult> GetBulkProducts([FromQuery] int count = 100)
        {
            if (count > 1000) count = 1000; // Limit for safety

            await Task.Delay(Random.Next(10, 50));

            var bulkProducts = Products
                .Take(count)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Price,
                    p.Category
                })
                .ToList();

            return Ok(new
            {
                Products = bulkProducts,
                Count = bulkProducts.Count,
                RequestedCount = count
            });
        }

        private static List<Product> GenerateProducts(int count)
        {
            var random = new Random(42); // Fixed seed for consistent data
            var products = new List<Product>();

            var categories = new[]
            {
                "Purified Water", "Mineral Water", "Sparkling Water",
                "Alkaline Water", "Spring Water", "Flavored Water",
                "Coconut Water", "Vitamin Water", "Sports Drinks"
            };

            var brands = new[]
            {
                "AquaPure", "CrystalClear", "BlueSpring", "NaturalFlow",
                "PureDrop", "FreshSource", "VitalWater", "CleanStream"
            };

            var sizes = new[] { "330ml", "500ml", "750ml", "1L", "1.5L", "5L", "19L" };

            for (int i = 1; i <= count; i++)
            {
                var category = categories[random.Next(categories.Length)];
                var brand = brands[random.Next(brands.Length)];
                var size = sizes[random.Next(sizes.Length)];

                products.Add(new Product
                {
                    Id = i,
                    Name = $"{brand} {category} {size}",
                    Description = $"Premium {category.ToLower()} from {brand} in {size} bottle. " +
                                 $"Perfect for hydration and daily consumption. High quality, " +
                                 $"refreshing taste, and sourced from the finest water sources.",
                    Price = Math.Round((decimal)(random.NextDouble() * 95 + 5), 2), // $5-$100
                    Category = category,
                    Brand = brand,
                    Size = size,
                    InStock = random.Next(0, 201), // 0-200 units
                    Rating = Math.Round(random.NextDouble() * 2 + 3, 1), // 3.0-5.0
                    ReviewCount = random.Next(0, 1000),
                    IsOrganic = random.Next(0, 4) == 0, // 25% chance
                    CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 365)),
                    UpdatedAt = DateTime.UtcNow.AddDays(-random.Next(0, 30))
                });
            }

            return products;
        }
    }
}