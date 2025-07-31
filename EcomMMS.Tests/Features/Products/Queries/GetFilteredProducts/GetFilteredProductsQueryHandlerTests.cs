using MediatR;
using Moq;
using FluentAssertions;
using EcomMMS.Domain.Entities;
using EcomMMS.Domain.Interfaces;
using EcomMMS.Application.Features.Products.Queries.GetFilteredProducts;
using EcomMMS.Application.Common;
using EcomMMS.Application.DTOs;
using EcomMMS.Tests.TestData;
using Xunit;

namespace EcomMMS.Tests.Features.Products.Queries.GetFilteredProducts
{
    public class GetFilteredProductsQueryHandlerTests
    {
        private readonly Mock<IProductRepository> _mockProductRepository;
        private readonly Mock<ICategoryRepository> _mockCategoryRepository;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<ICacheKeyGenerator> _mockCacheKeyGenerator;
        private readonly Mock<IApplicationLogger> _mockLogger;
        private readonly GetFilteredProductsQueryHandler _handler;

        public GetFilteredProductsQueryHandlerTests()
        {
            _mockProductRepository = new Mock<IProductRepository>();
            _mockCategoryRepository = new Mock<ICategoryRepository>();
            _mockCacheService = new Mock<ICacheService>();
            _mockCacheKeyGenerator = new Mock<ICacheKeyGenerator>();
            _mockLogger = new Mock<IApplicationLogger>();
            _handler = new GetFilteredProductsQueryHandler(_mockProductRepository.Object, _mockCategoryRepository.Object, _mockCacheService.Object, _mockCacheKeyGenerator.Object, _mockLogger.Object);
        }

        private void SetupCacheMocks()
        {
            _mockCacheService.Setup(x => x.GetAsync<PaginatedResult<ProductDto>>(It.IsAny<string>()))
                .ReturnsAsync((PaginatedResult<ProductDto>?)null);
            _mockCacheKeyGenerator.Setup(x => x.GenerateFilteredProductsKey(It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns("test-cache-key");
            _mockCacheService.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<PaginatedResult<ProductDto>>(), It.IsAny<TimeSpan>()))
                .Returns(Task.CompletedTask);
        }

        private void VerifyCacheMocks()
        {
            _mockCacheService.Verify(x => x.GetAsync<PaginatedResult<ProductDto>>(It.IsAny<string>()), Times.Once);
            _mockCacheKeyGenerator.Verify(x => x.GenerateFilteredProductsKey(It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
            _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<PaginatedResult<ProductDto>>(), It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public async Task Handle_NoFilters_ShouldReturnAllProducts()
        {
            // Given
            var category = TestDataHelper.CreateTestCategory();
            var products = TestDataHelper.CreateTestProducts(3);
            var categories = new List<Category> { category };
            var query = new GetFilteredProductsQuery();

            _mockProductRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(products);
            _mockCategoryRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(categories);
            SetupCacheMocks();

            // When
            var result = await _handler.Handle(query, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Data.Should().HaveCount(3);
            result.Data.Metadata.Should().NotBeNull();
            result.Data.Metadata.CurrentPage.Should().Be(1);
            result.Data.Metadata.PageSize.Should().Be(10);
            result.Data.Metadata.TotalCount.Should().Be(3);
            result.Data.Metadata.TotalPages.Should().Be(1);
            result.Data.Metadata.HasNextPage.Should().BeFalse();
            result.Data.Metadata.HasPreviousPage.Should().BeFalse();

            _mockProductRepository.Verify(x => x.GetAllAsync(), Times.Once);
            _mockCategoryRepository.Verify(x => x.GetAllAsync(), Times.Once);
            VerifyCacheMocks();
        }

        [Fact]
        public async Task Handle_SearchTermFilter_ShouldReturnFilteredProducts()
        {
            // Given
            var category = TestDataHelper.CreateTestCategory();
            var products = new List<Product>
            {
                new Product("Laptop", "High performance laptop", category.Id, 15),
                new Product("Mouse", "Wireless mouse", category.Id, 25),
                new Product("Keyboard", "Mechanical keyboard", category.Id, 35)
            };

            foreach (var product in products)
            {
                product.SetCategory(category);
            }

            var categories = new List<Category> { category };
            var query = new GetFilteredProductsQuery
            {
                SearchTerm = "laptop"
            };

            _mockProductRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(products);
            _mockCategoryRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(categories);
            SetupCacheMocks();

            // When
            var result = await _handler.Handle(query, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Data.Should().HaveCount(1);
            result.Data.Data.First().Title.ToLowerInvariant().Should().Contain("laptop");
            result.Data.Metadata.TotalCount.Should().Be(1);

            _mockProductRepository.Verify(x => x.GetAllAsync(), Times.Once);
            _mockCategoryRepository.Verify(x => x.GetAllAsync(), Times.Once);
            VerifyCacheMocks();
        }

        [Fact]
        public async Task Handle_SearchTermInDescription_ShouldReturnFilteredProducts()
        {
            // Given
            var category = TestDataHelper.CreateTestCategory();
            var products = new List<Product>
            {
                new Product("Product1", "High performance laptop", category.Id, 15),
                new Product("Product2", "Wireless mouse", category.Id, 25),
                new Product("Product3", "Mechanical keyboard", category.Id, 35)
            };

            foreach (var product in products)
            {
                product.SetCategory(category);
            }

            var categories = new List<Category> { category };
            var query = new GetFilteredProductsQuery
            {
                SearchTerm = "performance"
            };

            _mockProductRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(products);
            _mockCategoryRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(categories);
            SetupCacheMocks();

            // When
            var result = await _handler.Handle(query, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Data.Should().HaveCount(1);
            result.Data.Data.First().Description!.ToLowerInvariant().Should().Contain("performance");
            result.Data.Metadata.TotalCount.Should().Be(1);

            _mockProductRepository.Verify(x => x.GetAllAsync(), Times.Once);
            _mockCategoryRepository.Verify(x => x.GetAllAsync(), Times.Once);
            VerifyCacheMocks();
        }

        [Fact]
        public async Task Handle_MinStockQuantityFilter_ShouldReturnFilteredProducts()
        {
            // Given
            var category = TestDataHelper.CreateTestCategory();
            var products = new List<Product>
            {
                new Product("Product1", "Description1", category.Id, 5),
                new Product("Product2", "Description2", category.Id, 15),
                new Product("Product3", "Description3", category.Id, 25)
            };

            foreach (var product in products)
            {
                product.SetCategory(category);
            }

            var categories = new List<Category> { category };
            var query = new GetFilteredProductsQuery
            {
                MinStockQuantity = 10
            };

            _mockProductRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(products);
            _mockCategoryRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(categories);
            SetupCacheMocks();

            // When
            var result = await _handler.Handle(query, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Data.Should().HaveCount(2);
            result.Data.Data.Should().AllSatisfy(p => p.StockQuantity.Should().BeGreaterThanOrEqualTo(10));
            result.Data.Metadata.TotalCount.Should().Be(2);

            _mockProductRepository.Verify(x => x.GetAllAsync(), Times.Once);
            _mockCategoryRepository.Verify(x => x.GetAllAsync(), Times.Once);
            VerifyCacheMocks();
        }

        [Fact]
        public async Task Handle_MaxStockQuantityFilter_ShouldReturnFilteredProducts()
        {
            // Given
            var category = TestDataHelper.CreateTestCategory();
            var products = new List<Product>
            {
                new Product("Product1", "Description1", category.Id, 5),
                new Product("Product2", "Description2", category.Id, 15),
                new Product("Product3", "Description3", category.Id, 25)
            };

            foreach (var product in products)
            {
                product.SetCategory(category);
            }

            var categories = new List<Category> { category };
            var query = new GetFilteredProductsQuery
            {
                MaxStockQuantity = 20
            };

            _mockProductRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(products);
            _mockCategoryRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(categories);
            SetupCacheMocks();

            // When
            var result = await _handler.Handle(query, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Data.Should().HaveCount(2);
            result.Data.Data.Should().AllSatisfy(p => p.StockQuantity.Should().BeLessThanOrEqualTo(20));

            _mockProductRepository.Verify(x => x.GetAllAsync(), Times.Once);
            _mockCategoryRepository.Verify(x => x.GetAllAsync(), Times.Once);
            VerifyCacheMocks();
        }

        [Fact]
        public async Task Handle_StockQuantityRangeFilter_ShouldReturnFilteredProducts()
        {
            // Given
            var category = TestDataHelper.CreateTestCategory();
            var products = new List<Product>
            {
                new Product("Product1", "Description1", category.Id, 5),
                new Product("Product2", "Description2", category.Id, 15),
                new Product("Product3", "Description3", category.Id, 25),
                new Product("Product4", "Description4", category.Id, 35)
            };

            foreach (var product in products)
            {
                product.SetCategory(category);
            }

            var categories = new List<Category> { category };
            var query = new GetFilteredProductsQuery
            {
                MinStockQuantity = 10,
                MaxStockQuantity = 30
            };

            _mockProductRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(products);
            _mockCategoryRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(categories);
            SetupCacheMocks();

            // When
            var result = await _handler.Handle(query, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Data.Should().HaveCount(2);
            result.Data.Data.Should().AllSatisfy(p => p.StockQuantity.Should().BeInRange(10, 30));

            _mockProductRepository.Verify(x => x.GetAllAsync(), Times.Once);
            _mockCategoryRepository.Verify(x => x.GetAllAsync(), Times.Once);
            VerifyCacheMocks();
        }

        [Fact]
        public async Task Handle_IsLiveFilter_ShouldReturnFilteredProducts()
        {
            // Given
            var category = TestDataHelper.CreateTestCategory("Electronics", 10);
            var products = new List<Product>
            {
                new Product("Product1", "Description1", category.Id, 5),
                new Product("Product2", "Description2", category.Id, 15),
                new Product("Product3", "Description3", category.Id, 25)
            };

            foreach (var product in products)
            {
                product.SetCategory(category);
            }

            var categories = new List<Category> { category };
            var query = new GetFilteredProductsQuery
            {
                IsLive = true
            };

            _mockProductRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(products);
            _mockCategoryRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(categories);
            SetupCacheMocks();

            // When
            var result = await _handler.Handle(query, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Data.Should().HaveCount(2);
            result.Data.Data.Should().AllSatisfy(p => p.IsLive.Should().BeTrue());

            _mockProductRepository.Verify(x => x.GetAllAsync(), Times.Once);
            _mockCategoryRepository.Verify(x => x.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_CategoryIdFilter_ShouldReturnFilteredProducts()
        {
            // Given
            var category1 = TestDataHelper.CreateTestCategory("Electronics", 10);
            var category2 = TestDataHelper.CreateTestCategory("Books", 5);
            
            var product1 = new Product("Product1", "Description1", category1.Id, 15);
            product1.SetCategory(category1);
            var product2 = new Product("Product2", "Description2", category2.Id, 15);
            product2.SetCategory(category2);
            var product3 = new Product("Product3", "Description3", category1.Id, 15);
            product3.SetCategory(category1);
            
            var products = new List<Product> { product1, product2, product3 };
            var categories = new List<Category> { category1, category2 };
            var query = new GetFilteredProductsQuery
            {
                CategoryId = category1.Id
            };

            _mockProductRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(products);
            _mockCategoryRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(categories);
            SetupCacheMocks();

            // When
            var result = await _handler.Handle(query, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Data.Should().HaveCount(2);
            result.Data.Data.Should().AllSatisfy(p => p.CategoryId.Should().Be(category1.Id));
        }

        [Fact]
        public async Task Handle_MultipleFilters_ShouldReturnFilteredProducts()
        {
            // Given
            var category = TestDataHelper.CreateTestCategory("Electronics", 10);
            
            var product1 = new Product("Laptop", "High performance laptop", category.Id, 15);
            product1.SetCategory(category);
            var product2 = new Product("Mouse", "Wireless mouse", category.Id, 5);
            product2.SetCategory(category);
            var product3 = new Product("Keyboard", "Mechanical keyboard", category.Id, 25);
            product3.SetCategory(category);
            var product4 = new Product("Monitor", "4K monitor", category.Id, 8);
            product4.SetCategory(category);
            
            var products = new List<Product> { product1, product2, product3, product4 };
            var categories = new List<Category> { category };
            var query = new GetFilteredProductsQuery
            {
                SearchTerm = "laptop",
                MinStockQuantity = 10,
                IsLive = true
            };

            _mockProductRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(products);
            _mockCategoryRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(categories);
            SetupCacheMocks();

            // When
            var result = await _handler.Handle(query, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Data.Should().HaveCount(1);
            result.Data.Data.First().Title.ToLowerInvariant().Should().Contain("laptop");
            result.Data.Data.First().StockQuantity.Should().BeGreaterThanOrEqualTo(10);
            result.Data.Data.First().IsLive.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_EmptyProductsList_ShouldReturnEmptyList()
        {
            // Given
            var products = new List<Product>();
            var categories = TestDataHelper.CreateTestCategories(1);
            var query = new GetFilteredProductsQuery { Page = 1, PageSize = 10 };

            _mockProductRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(products);
            _mockCategoryRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(categories);

            // When
            var result = await _handler.Handle(query, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Data.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_EmptyCategoriesList_ShouldReturnProductsWithEmptyCategoryNames()
        {
            // Given
            var products = TestDataHelper.CreateTestProducts(2);
            var categories = new List<Category>();
            var query = new GetFilteredProductsQuery { Page = 1, PageSize = 10 };

            _mockProductRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(products);
            _mockCategoryRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(categories);
            SetupCacheMocks();

            // When
            var result = await _handler.Handle(query, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Data.Should().HaveCount(2);
            result.Data.Data.Should().AllSatisfy(p => p.CategoryName.Should().BeEmpty());
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task Handle_EmptyOrNullSearchTerm_ShouldReturnAllProducts(string? searchTerm)
        {
            // Given
            var category = TestDataHelper.CreateTestCategory();
            var products = new List<Product>
            {
                new Product("Product1", "Description1", category.Id, 15),
                new Product("Product2", "Description2", category.Id, 25)
            };

            foreach (var product in products)
            {
                product.SetCategory(category);
            }

            var categories = new List<Category> { category };
            var query = new GetFilteredProductsQuery
            {
                SearchTerm = searchTerm,
                Page = 1,
                PageSize = 10
            };

            _mockProductRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(products);
            _mockCategoryRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(categories);
            SetupCacheMocks();

            // When
            var result = await _handler.Handle(query, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Data.Should().HaveCount(2);
        }

        [Fact]
        public async Task Handle_PaginationFirstPage_ShouldReturnCorrectNumberOfItems()
        {
            // Given
            var category = TestDataHelper.CreateTestCategory();
            var products = TestDataHelper.CreateTestProducts(25);
            var categories = new List<Category> { category };
            var query = new GetFilteredProductsQuery
            {
                Page = 1,
                PageSize = 10
            };

            _mockProductRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(products);
            _mockCategoryRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(categories);
            SetupCacheMocks();

            // When
            var result = await _handler.Handle(query, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Data.Should().HaveCount(10);
            result.Data.Metadata.CurrentPage.Should().Be(1);
            result.Data.Metadata.PageSize.Should().Be(10);
            result.Data.Metadata.TotalCount.Should().Be(25);
            result.Data.Metadata.TotalPages.Should().Be(3);
            result.Data.Metadata.HasNextPage.Should().BeTrue();
            result.Data.Metadata.HasPreviousPage.Should().BeFalse();

            _mockProductRepository.Verify(x => x.GetAllAsync(), Times.Once);
            _mockCategoryRepository.Verify(x => x.GetAllAsync(), Times.Once);
            VerifyCacheMocks();
        }

        [Fact]
        public async Task Handle_PaginationSecondPage_ShouldReturnCorrectItems()
        {
            // Given
            var category = TestDataHelper.CreateTestCategory();
            var products = TestDataHelper.CreateTestProducts(25);
            var categories = new List<Category> { category };
            var query = new GetFilteredProductsQuery
            {
                Page = 2,
                PageSize = 10
            };

            _mockProductRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(products);
            _mockCategoryRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(categories);
            SetupCacheMocks();

            // When
            var result = await _handler.Handle(query, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Data.Should().HaveCount(10);
            result.Data.Metadata.CurrentPage.Should().Be(2);
            result.Data.Metadata.PageSize.Should().Be(10);
            result.Data.Metadata.TotalCount.Should().Be(25);
            result.Data.Metadata.TotalPages.Should().Be(3);
            result.Data.Metadata.HasNextPage.Should().BeTrue();
            result.Data.Metadata.HasPreviousPage.Should().BeTrue();
            result.Data.Metadata.PreviousPage.Should().Be(1);
            result.Data.Metadata.NextPage.Should().Be(3);

            _mockProductRepository.Verify(x => x.GetAllAsync(), Times.Once);
            _mockCategoryRepository.Verify(x => x.GetAllAsync(), Times.Once);
            VerifyCacheMocks();
        }

        [Fact]
        public async Task Handle_PaginationLastPage_ShouldReturnRemainingItems()
        {
            // Given
            var category = TestDataHelper.CreateTestCategory();
            var products = TestDataHelper.CreateTestProducts(12);

            var categories = new List<Category> { category };
            var query = new GetFilteredProductsQuery { Page = 3, PageSize = 5 };

            _mockProductRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(products);
            _mockCategoryRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(categories);
            SetupCacheMocks();

            // When
            var result = await _handler.Handle(query, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Data.Should().HaveCount(2);
            result.Data.Metadata.CurrentPage.Should().Be(3);
            result.Data.Metadata.TotalPages.Should().Be(3);
            result.Data.Metadata.HasNextPage.Should().BeFalse();
            result.Data.Metadata.HasPreviousPage.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_PaginationPageBeyondData_ShouldReturnEmptyList()
        {
            // Given
            var category = TestDataHelper.CreateTestCategory();
            var products = TestDataHelper.CreateTestProducts(5);

            var categories = new List<Category> { category };
            var query = new GetFilteredProductsQuery { Page = 3, PageSize = 5 };

            _mockProductRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(products);
            _mockCategoryRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(categories);
            SetupCacheMocks();

            // When
            var result = await _handler.Handle(query, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Data.Should().BeEmpty();
            result.Data.Metadata.CurrentPage.Should().Be(3);
            result.Data.Metadata.TotalPages.Should().Be(1);
        }

        [Fact]
        public async Task Handle_PaginationWithFilters_ShouldReturnFilteredAndPaginatedResults()
        {
            // Given
            var category = TestDataHelper.CreateTestCategory();
            var products = TestDataHelper.CreateTestProducts(20);

            var categories = new List<Category> { category };
            var query = new GetFilteredProductsQuery 
            { 
                SearchTerm = "Test",
                MinStockQuantity = 10,
                Page = 2,
                PageSize = 3
            };

            _mockProductRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(products);
            _mockCategoryRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(categories);
            SetupCacheMocks();

            // When
            var result = await _handler.Handle(query, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Data.Should().HaveCount(3);

            foreach (var product in result.Data.Data)
            {
                product.Title.Should().Contain("Test");
            }
        }
    }
} 