namespace EcomMMS.Application.Common
{
    public interface ICacheKeyGenerator
    {
        string GenerateProductFilterKey(string? searchTerm, int? minStockQuantity, int? maxStockQuantity, bool? isLive, Guid? categoryId, int page, int pageSize);
        string GenerateProductByIdKey(Guid id);
        string GenerateAllProductsKey();
        string GenerateCategoryByIdKey(Guid id);
        string GenerateAllCategoriesKey();
    }
} 