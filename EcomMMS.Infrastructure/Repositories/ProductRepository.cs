using EcomMMS.Domain.Entities;
using EcomMMS.Domain.Interfaces;
using EcomMMS.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace EcomMMS.Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;

        public ProductRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Product> AddAsync(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<Product?> GetByIdAsync(Guid id)
        {
            return await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .ToListAsync();
        }

        public async Task UpdateAsync(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Products.AnyAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Product>> FindAsync(Expression<Func<Product, bool>> predicate)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(predicate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetByCategoryAsync(Guid categoryId)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.CategoryId == categoryId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetLiveProductsAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsLive)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetByStockQuantityAsync(int minStockQuantity)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.StockQuantity >= minStockQuantity)
                .ToListAsync();
        }
    }
} 