using EcomMMS.Infrastructure.Services;
using EcomMMS.Application.Common;
using FluentAssertions;
using Xunit;

namespace EcomMMS.Tests.Features.Cache
{
    public class CacheKeyGeneratorTests
    {
        private readonly ICacheKeyGenerator _cacheKeyGenerator;

        public CacheKeyGeneratorTests()
        {
            _cacheKeyGenerator = new CacheKeyGenerator();
        }

        [Fact]
        public void GenerateProductFilterKey_WithAllParameters_ShouldGenerateConsistentKey()
        {
            // Given
            var searchTerm = "test";
            var minStockQuantity = 10;
            var maxStockQuantity = 100;
            var isLive = true;
            var categoryId = Guid.NewGuid();
            var page = 1;
            var pageSize = 10;

            // When
            var key1 = _cacheKeyGenerator.GenerateProductFilterKey(searchTerm, minStockQuantity, maxStockQuantity, isLive, categoryId, page, pageSize);
            var key2 = _cacheKeyGenerator.GenerateProductFilterKey(searchTerm, minStockQuantity, maxStockQuantity, isLive, categoryId, page, pageSize);

            // Then
            key1.Should().NotBeNullOrEmpty();
            key1.Should().Be(key2);
        }

        [Fact]
        public void GenerateProductFilterKey_WithNullParameters_ShouldGenerateConsistentKey()
        {
            // Given
            string? searchTerm = null;
            int? minStockQuantity = null;
            int? maxStockQuantity = null;
            bool? isLive = null;
            Guid? categoryId = null;
            var page = 1;
            var pageSize = 10;

            // When
            var key1 = _cacheKeyGenerator.GenerateProductFilterKey(searchTerm, minStockQuantity, maxStockQuantity, isLive, categoryId, page, pageSize);
            var key2 = _cacheKeyGenerator.GenerateProductFilterKey(searchTerm, minStockQuantity, maxStockQuantity, isLive, categoryId, page, pageSize);

            // Then
            key1.Should().NotBeNullOrEmpty();
            key1.Should().Be(key2);
        }

        [Fact]
        public void GenerateProductFilterKey_WithDifferentParameters_ShouldGenerateDifferentKeys()
        {
            // Given
            var searchTerm1 = "test1";
            var searchTerm2 = "test2";
            var minStockQuantity = 10;
            var maxStockQuantity = 100;
            var isLive = true;
            var categoryId = Guid.NewGuid();
            var page = 1;
            var pageSize = 10;

            // When
            var key1 = _cacheKeyGenerator.GenerateProductFilterKey(searchTerm1, minStockQuantity, maxStockQuantity, isLive, categoryId, page, pageSize);
            var key2 = _cacheKeyGenerator.GenerateProductFilterKey(searchTerm2, minStockQuantity, maxStockQuantity, isLive, categoryId, page, pageSize);

            // Then
            key1.Should().NotBe(key2);
        }

        [Fact]
        public void GenerateProductByIdKey_ShouldGenerateConsistentKey()
        {
            // Given
            var productId = Guid.NewGuid();

            // When
            var key1 = _cacheKeyGenerator.GenerateProductByIdKey(productId);
            var key2 = _cacheKeyGenerator.GenerateProductByIdKey(productId);

            // Then
            key1.Should().NotBeNullOrEmpty();
            key1.Should().Be(key2);
            key1.Should().Contain(productId.ToString());
        }

        [Fact]
        public void GenerateAllProductsKey_ShouldReturnConsistentKey()
        {
            // When
            var key1 = _cacheKeyGenerator.GenerateAllProductsKey();
            var key2 = _cacheKeyGenerator.GenerateAllProductsKey();

            // Then
            key1.Should().NotBeNullOrEmpty();
            key1.Should().Be(key2);
            key1.Should().Be("products:all");
        }

        [Fact]
        public void GenerateCategoryByIdKey_ShouldGenerateConsistentKey()
        {
            // Given
            var categoryId = Guid.NewGuid();

            // When
            var key1 = _cacheKeyGenerator.GenerateCategoryByIdKey(categoryId);
            var key2 = _cacheKeyGenerator.GenerateCategoryByIdKey(categoryId);

            // Then
            key1.Should().NotBeNullOrEmpty();
            key1.Should().Be(key2);
            key1.Should().Contain(categoryId.ToString());
        }

        [Fact]
        public void GenerateAllCategoriesKey_ShouldReturnConsistentKey()
        {
            // When
            var key1 = _cacheKeyGenerator.GenerateAllCategoriesKey();
            var key2 = _cacheKeyGenerator.GenerateAllCategoriesKey();

            // Then
            key1.Should().NotBeNullOrEmpty();
            key1.Should().Be(key2);
            key1.Should().Be("categories:all");
        }
    }
} 