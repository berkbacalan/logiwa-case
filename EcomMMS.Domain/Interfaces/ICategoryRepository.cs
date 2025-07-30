using EcomMMS.Domain.Entities;

namespace EcomMMS.Domain.Interfaces
{
    public interface ICategoryRepository : IRepository<Category>
    {
        Task<Category?> GetByNameAsync(string name);
        Task<IEnumerable<Category>> GetByMinimumStockQuantityAsync(int minStockQuantity);
        Task SaveChangesAsync();
    }
} 