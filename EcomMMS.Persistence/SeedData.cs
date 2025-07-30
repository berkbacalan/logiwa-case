using EcomMMS.Domain.Entities;

namespace EcomMMS.Persistence
{
    public static class SeedData
    {
        public static void SeedCategories(ApplicationDbContext context)
        {
            if (!context.Categories.Any())
            {
                var categories = new List<Category>
                {
                    new Category("Electronics", 10),
                    new Category("Books", 500),
                    new Category("Clothing", 15),
                    new Category("Home & Garden", 8),
                    new Category("Digital Cards", 50),
                    new Category("Games", 20),
                    new Category("Furniture", 10)
                };
                

                context.Categories.AddRange(categories);
                context.SaveChanges();
            }
        }
    }
} 