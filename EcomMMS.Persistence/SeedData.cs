using EcomMMS.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace EcomMMS.Persistence
{
    public static class SeedData
    {
        public static void SeedCategories(ApplicationDbContext context, ILogger? logger = null)
        {
            try
            {
                if (!context.Database.CanConnect())
                {
                    logger?.LogWarning("Cannot connect to database, skipping seed");
                    return;
                }

                var hasCategories = false;
                try
                {
                    hasCategories = context.Categories.Any();
                }
                catch
                {
                    hasCategories = false;
                }

                if (!hasCategories)
                {
                    logger?.LogInformation("Starting to seed categories");
                    
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
                    var result = context.SaveChanges();
                    
                    logger?.LogInformation("Successfully seeded {Count} categories", result);
                }
                else
                {
                    logger?.LogInformation("Categories already exist, skipping seed");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error occurred while seeding categories");
                throw;
            }
        }
    }
} 