using EcomMMS.Application.Common;
using System.Security.Cryptography;
using System.Text;

namespace EcomMMS.Infrastructure.Services
{
    public class CacheKeyGenerator : ICacheKeyGenerator
    {
        public string GenerateProductFilterKey(string? searchTerm, int? minStockQuantity, int? maxStockQuantity, bool? isLive, Guid? categoryId, int page, int pageSize)
        {
            var keyBuilder = new StringBuilder("products:filter:");
            
            keyBuilder.Append($"search={searchTerm ?? "null"}:");
            keyBuilder.Append($"minStock={minStockQuantity?.ToString() ?? "null"}:");
            keyBuilder.Append($"maxStock={maxStockQuantity?.ToString() ?? "null"}:");
            keyBuilder.Append($"isLive={isLive?.ToString() ?? "null"}:");
            keyBuilder.Append($"categoryId={categoryId?.ToString() ?? "null"}:");
            keyBuilder.Append($"page={page}:");
            keyBuilder.Append($"pageSize={pageSize}");

            return GenerateHashKey(keyBuilder.ToString());
        }

        public string GenerateProductByIdKey(Guid id)
        {
            return $"products:id:{id}";
        }

        public string GenerateAllProductsKey()
        {
            return "products:all";
        }

        public string GenerateCategoryByIdKey(Guid id)
        {
            return $"categories:id:{id}";
        }

        public string GenerateAllCategoriesKey()
        {
            return "categories:all";
        }

        private string GenerateHashKey(string input)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hashBytes).Replace("/", "_").Replace("+", "-").Replace("=", "");
        }
    }
} 