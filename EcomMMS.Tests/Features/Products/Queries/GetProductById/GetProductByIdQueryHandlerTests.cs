using MediatR;
using Moq;
using FluentAssertions;
using EcomMMS.Domain.Entities;
using EcomMMS.Domain.Interfaces;
using EcomMMS.Application.Features.Products.Queries.GetProductById;
using EcomMMS.Application.Common;
using EcomMMS.Application.DTOs;
using EcomMMS.Tests.TestData;
using Xunit;

namespace EcomMMS.Tests.Features.Products.Queries.GetProductById
{
    public class GetProductByIdQueryHandlerTests
    {
        private readonly Mock<IProductRepository> _mockProductRepository;
        private readonly Mock<ICategoryRepository> _mockCategoryRepository;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<ICacheKeyGenerator> _mockCacheKeyGenerator;
        private readonly GetProductByIdQueryHandler _handler;

        public GetProductByIdQueryHandlerTests()
        {
            _mockProductRepository = new Mock<IProductRepository>();
            _mockCategoryRepository = new Mock<ICategoryRepository>();
            _mockCacheService = new Mock<ICacheService>();
            _mockCacheKeyGenerator = new Mock<ICacheKeyGenerator>();
            _handler = new GetProductByIdQueryHandler(_mockProductRepository.Object, _mockCategoryRepository.Object, _mockCacheService.Object, _mockCacheKeyGenerator.Object);
        }

        private void SetupCacheMocks(Guid productId)
        {
            var cacheKey = $"products:id:{productId}";
            _mockCacheKeyGenerator.Setup(x => x.GenerateProductByIdKey(productId))
                .Returns(cacheKey);
            _mockCacheService.Setup(x => x.GetAsync<ProductDto>(cacheKey))
                .ReturnsAsync((ProductDto?)null);
            _mockCacheService.Setup(x => x.SetAsync(cacheKey, It.IsAny<ProductDto>(), It.IsAny<TimeSpan>()))
                .Returns(Task.CompletedTask);
        }

        private void VerifyCacheMocks(Guid productId)
        {
            var cacheKey = $"products:id:{productId}";
            _mockCacheKeyGenerator.Verify(x => x.GenerateProductByIdKey(productId), Times.Once);
            _mockCacheService.Verify(x => x.GetAsync<ProductDto>(cacheKey), Times.Once);
            _mockCacheService.Verify(x => x.SetAsync(cacheKey, It.IsAny<ProductDto>(), It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidProductId_ShouldReturnProductDto()
        {
            // Given
            var category = TestDataHelper.CreateTestCategory("Electronics", 10);
            var product = new Product("Laptop", "High performance laptop", category.Id, 25);
            product.SetCategory(category);
            
            var query = new GetProductByIdQuery
            {
                Id = product.Id
            };

            _mockProductRepository.Setup(x => x.GetByIdAsync(product.Id))
                .ReturnsAsync(product);
            _mockCategoryRepository.Setup(x => x.GetByIdAsync(category.Id))
                .ReturnsAsync(category);
            SetupCacheMocks(product.Id);

            // When
            var result = await _handler.Handle(query, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(product.Id);
            result.Data.Title.Should().Be(product.Title);
            result.Data.Description.Should().Be(product.Description);
            result.Data.CategoryId.Should().Be(category.Id);
            result.Data.CategoryName.Should().Be(category.Name);
            result.Data.StockQuantity.Should().Be(product.StockQuantity);
            result.Data.IsLive.Should().Be(product.IsLive);
            result.Data.CreatedAt.Should().Be(product.CreatedAt);
            result.Data.UpdatedAt.Should().Be(product.UpdatedAt);

            _mockProductRepository.Verify(x => x.GetByIdAsync(product.Id), Times.Once);
            _mockCategoryRepository.Verify(x => x.GetByIdAsync(category.Id), Times.Once);
            VerifyCacheMocks(product.Id);
        }

        [Fact]
        public async Task Handle_ProductNotFound_ShouldReturnFailureResult()
        {
            // Given
            var productId = Guid.NewGuid();
            var query = new GetProductByIdQuery
            {
                Id = productId
            };

            _mockProductRepository.Setup(x => x.GetByIdAsync(productId))
                .ReturnsAsync((Product?)null);
            SetupCacheMocks(productId);

            // When
            var result = await _handler.Handle(query, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Data.Should().BeNull();
            result.ErrorMessage.Should().Contain("not found");

            _mockProductRepository.Verify(x => x.GetByIdAsync(productId), Times.Once);
            _mockCategoryRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
            _mockCacheKeyGenerator.Verify(x => x.GenerateProductByIdKey(productId), Times.Once);
            _mockCacheService.Verify(x => x.GetAsync<ProductDto>(It.IsAny<string>()), Times.Once);
            _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<ProductDto>(), It.IsAny<TimeSpan>()), Times.Never);
        }

        [Fact]
        public async Task Handle_CategoryNotFound_ShouldReturnProductWithEmptyCategoryName()
        {
            // Given
            var product = TestDataHelper.CreateTestProduct();
            var query = new GetProductByIdQuery
            {
                Id = product.Id
            };

            _mockProductRepository.Setup(x => x.GetByIdAsync(product.Id))
                .ReturnsAsync(product);
            _mockCategoryRepository.Setup(x => x.GetByIdAsync(product.CategoryId))
                .ReturnsAsync((Category?)null);
            SetupCacheMocks(product.Id);

            // When
            var result = await _handler.Handle(query, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.CategoryName.Should().BeEmpty();

            _mockProductRepository.Verify(x => x.GetByIdAsync(product.Id), Times.Once);
            _mockCategoryRepository.Verify(x => x.GetByIdAsync(product.CategoryId), Times.Once);
            VerifyCacheMocks(product.Id);
        }

        [Theory]
        [InlineData("00000000-0000-0000-0000-000000000000")]
        [InlineData("11111111-1111-1111-1111-111111111111")]
        [InlineData("22222222-2222-2222-2222-222222222222")]
        public async Task Handle_DifferentProductIds_ShouldReturnCorrectProduct(string productIdString)
        {
            // Given
            var productId = Guid.Parse(productIdString);
            var category = TestDataHelper.CreateTestCategory();
            var product = TestDataHelper.CreateTestProduct();
            product.Id = productId;

            var query = new GetProductByIdQuery
            {
                Id = productId
            };

            _mockProductRepository.Setup(x => x.GetByIdAsync(productId))
                .ReturnsAsync(product);
            _mockCategoryRepository.Setup(x => x.GetByIdAsync(product.CategoryId))
                .ReturnsAsync(category);
            SetupCacheMocks(productId);

            // When
            var result = await _handler.Handle(query, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(productId);

            _mockProductRepository.Verify(x => x.GetByIdAsync(productId), Times.Once);
            _mockCategoryRepository.Verify(x => x.GetByIdAsync(product.CategoryId), Times.Once);
            VerifyCacheMocks(productId);
        }

        [Fact]
        public async Task Handle_ProductWithNullDescription_ShouldReturnProductWithNullDescription()
        {
            // Given
            var category = TestDataHelper.CreateTestCategory();
            var product = new Product("Test Product", null, category.Id, 15);
            product.SetCategory(category);

            var query = new GetProductByIdQuery
            {
                Id = product.Id
            };

            _mockProductRepository.Setup(x => x.GetByIdAsync(product.Id))
                .ReturnsAsync(product);
            _mockCategoryRepository.Setup(x => x.GetByIdAsync(category.Id))
                .ReturnsAsync(category);
            SetupCacheMocks(product.Id);

            // When
            var result = await _handler.Handle(query, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Description.Should().BeNull();

            _mockProductRepository.Verify(x => x.GetByIdAsync(product.Id), Times.Once);
            _mockCategoryRepository.Verify(x => x.GetByIdAsync(category.Id), Times.Once);
            VerifyCacheMocks(product.Id);
        }

        [Fact]
        public async Task Handle_ProductWithUpdatedAtNull_ShouldReturnProductWithNullUpdatedAt()
        {
            // Given
            var category = TestDataHelper.CreateTestCategory();
            var product = new Product("Test Product", "Test Description", category.Id, 15);
            product.SetCategory(category);

            var query = new GetProductByIdQuery
            {
                Id = product.Id
            };

            _mockProductRepository.Setup(x => x.GetByIdAsync(product.Id))
                .ReturnsAsync(product);
            _mockCategoryRepository.Setup(x => x.GetByIdAsync(category.Id))
                .ReturnsAsync(category);
            SetupCacheMocks(product.Id);

            // When
            var result = await _handler.Handle(query, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.UpdatedAt.Should().BeNull();

            _mockProductRepository.Verify(x => x.GetByIdAsync(product.Id), Times.Once);
            _mockCategoryRepository.Verify(x => x.GetByIdAsync(category.Id), Times.Once);
            VerifyCacheMocks(product.Id);
        }

        [Fact]
        public async Task Handle_CacheHit_ShouldReturnCachedProduct()
        {
            // Given
            var productId = Guid.NewGuid();
            var cachedProduct = new ProductDto
            {
                Id = productId,
                Title = "Cached Product",
                Description = "Cached Description",
                CategoryId = Guid.NewGuid(),
                CategoryName = "Cached Category",
                StockQuantity = 10,
                IsLive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var query = new GetProductByIdQuery
            {
                Id = productId
            };

            var cacheKey = $"products:id:{productId}";
            _mockCacheKeyGenerator.Setup(x => x.GenerateProductByIdKey(productId))
                .Returns(cacheKey);
            _mockCacheService.Setup(x => x.GetAsync<ProductDto>(cacheKey))
                .ReturnsAsync(cachedProduct);

            // When
            var result = await _handler.Handle(query, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(productId);
            result.Data.Title.Should().Be("Cached Product");

            _mockProductRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
            _mockCategoryRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
            _mockCacheKeyGenerator.Verify(x => x.GenerateProductByIdKey(productId), Times.Once);
            _mockCacheService.Verify(x => x.GetAsync<ProductDto>(cacheKey), Times.Once);
            _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<ProductDto>(), It.IsAny<TimeSpan>()), Times.Never);
        }

        [Fact]
        public async Task Handle_CacheServiceThrowsException_ShouldStillReturnProduct()
        {
            // Given
            var category = TestDataHelper.CreateTestCategory();
            var product = new Product("Test Product", "Test Description", category.Id, 15);
            product.SetCategory(category);

            var query = new GetProductByIdQuery
            {
                Id = product.Id
            };

            _mockProductRepository.Setup(x => x.GetByIdAsync(product.Id))
                .ReturnsAsync(product);
            _mockCategoryRepository.Setup(x => x.GetByIdAsync(category.Id))
                .ReturnsAsync(category);
            _mockCacheKeyGenerator.Setup(x => x.GenerateProductByIdKey(product.Id))
                .Returns($"products:id:{product.Id}");
            _mockCacheService.Setup(x => x.GetAsync<ProductDto>(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Cache service error"));
            _mockCacheService.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<ProductDto>(), It.IsAny<TimeSpan>()))
                .ThrowsAsync(new Exception("Cache service error"));

            // When
            var result = await _handler.Handle(query, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Title.Should().Be("Test Product");

            _mockProductRepository.Verify(x => x.GetByIdAsync(product.Id), Times.Once);
            _mockCategoryRepository.Verify(x => x.GetByIdAsync(category.Id), Times.Once);
        }
    }
} 