using EcomMMS.Domain.Entities;

namespace EcomMMS.Domain.Interfaces
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<IEnumerable<Product>> GetByCategoryAsync(Guid categoryId);
        Task<IEnumerable<Product>> GetLiveProductsAsync();
        Task<IEnumerable<Product>> GetByStockQuantityAsync(int minStockQuantity);
    }
} 