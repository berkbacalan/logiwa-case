using EcomMMS.Application.Features.Categories.Queries.GetAllCategories;
using EcomMMS.Application.Common;
using EcomMMS.Application.DTOs;
using EcomMMS.Domain.Entities;
using EcomMMS.Domain.Interfaces;
using EcomMMS.Tests.TestData;
using Moq;
using Xunit;

namespace EcomMMS.Tests.Features.Categories.Queries.GetAllCategories
{
    public class GetAllCategoriesQueryHandlerTests
    {
        private readonly Mock<ICategoryRepository> _mockCategoryRepository;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<ICacheKeyGenerator> _mockCacheKeyGenerator;
        private readonly GetAllCategoriesQueryHandler _handler;

        public GetAllCategoriesQueryHandlerTests()
        {
            _mockCategoryRepository = new Mock<ICategoryRepository>();
            _mockCacheService = new Mock<ICacheService>();
            _mockCacheKeyGenerator = new Mock<ICacheKeyGenerator>();
            _handler = new GetAllCategoriesQueryHandler(_mockCategoryRepository.Object, _mockCacheService.Object, _mockCacheKeyGenerator.Object);
        }

        [Fact]
        public async Task Handle_ValidRequest_ReturnsSuccessResult()
        {
            // Given
            var categories = TestDataHelper.CreateTestCategories(3);
            var query = new GetAllCategoriesQuery { Page = 1, PageSize = 10 };
            var cacheKey = "categories:all";

            _mockCacheKeyGenerator.Setup(x => x.GenerateAllCategoriesKey())
                .Returns(cacheKey);
            _mockCacheService.Setup(x => x.GetAsync<List<CategoryDto>>(cacheKey))
                .ReturnsAsync((List<CategoryDto>?)null);
            _mockCategoryRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(categories);
            _mockCacheService.Setup(x => x.SetAsync(cacheKey, It.IsAny<List<CategoryDto>>(), It.IsAny<TimeSpan>()))
                .Returns(Task.CompletedTask);

            // When
            var result = await _handler.Handle(query, CancellationToken.None);

            // Then
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(3, result.Data.Count);
            
            for (int i = 0; i < categories.Count(); i++)
            {
                var category = categories.ElementAt(i);
                var categoryDto = result.Data[i];
                
                Assert.Equal(category.Id, categoryDto.Id);
                Assert.Equal(category.Name, categoryDto.Name);
                Assert.Equal(category.MinimumStockQuantity, categoryDto.MinimumStockQuantity);
                Assert.Equal(category.CreatedAt, categoryDto.CreatedAt);
                Assert.Equal(category.UpdatedAt, categoryDto.UpdatedAt);
            }

            _mockCacheService.Verify(x => x.GetAsync<List<CategoryDto>>(cacheKey), Times.Once);
            _mockCategoryRepository.Verify(x => x.GetAllAsync(), Times.Once);
            _mockCacheService.Verify(x => x.SetAsync(cacheKey, It.IsAny<List<CategoryDto>>(), It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public async Task Handle_CachedData_ReturnsCachedResult()
        {
            // Given
            var cachedCategories = new List<CategoryDto>
            {
                new CategoryDto { Id = Guid.NewGuid(), Name = "Cached Category 1", MinimumStockQuantity = 10 },
                new CategoryDto { Id = Guid.NewGuid(), Name = "Cached Category 2", MinimumStockQuantity = 20 }
            };
            var query = new GetAllCategoriesQuery { Page = 1, PageSize = 10 };
            var cacheKey = "categories:all";

            _mockCacheKeyGenerator.Setup(x => x.GenerateAllCategoriesKey())
                .Returns(cacheKey);
            _mockCacheService.Setup(x => x.GetAsync<List<CategoryDto>>(cacheKey))
                .ReturnsAsync(cachedCategories);

            // When
            var result = await _handler.Handle(query, CancellationToken.None);

            // Then
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.Count);

            _mockCacheService.Verify(x => x.GetAsync<List<CategoryDto>>(cacheKey), Times.Once);
            _mockCategoryRepository.Verify(x => x.GetAllAsync(), Times.Never);
            _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<List<CategoryDto>>(), It.IsAny<TimeSpan>()), Times.Never);
        }

        [Fact]
        public async Task Handle_EmptyCategories_ReturnsEmptyList()
        {
            // Given
            var categories = new List<Category>();
            var query = new GetAllCategoriesQuery();
            var cacheKey = "categories:all";

            _mockCacheKeyGenerator.Setup(x => x.GenerateAllCategoriesKey())
                .Returns(cacheKey);
            _mockCacheService.Setup(x => x.GetAsync<List<CategoryDto>>(cacheKey))
                .ReturnsAsync((List<CategoryDto>?)null);
            _mockCategoryRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(categories);
            _mockCacheService.Setup(x => x.SetAsync(cacheKey, It.IsAny<List<CategoryDto>>(), It.IsAny<TimeSpan>()))
                .Returns(Task.CompletedTask);

            // When
            var result = await _handler.Handle(query, CancellationToken.None);

            // Then
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Empty(result.Data);
        }

        [Fact]
        public async Task Handle_RepositoryThrowsException_ReturnsFailureResult()
        {
            // Given
            var query = new GetAllCategoriesQuery { Page = 1, PageSize = 10 };
            var cacheKey = "categories:all";

            _mockCacheKeyGenerator.Setup(x => x.GenerateAllCategoriesKey())
                .Returns(cacheKey);
            _mockCacheService.Setup(x => x.GetAsync<List<CategoryDto>>(cacheKey))
                .ReturnsAsync((List<CategoryDto>?)null);
            _mockCategoryRepository.Setup(x => x.GetAllAsync())
                .ThrowsAsync(new Exception("Database error"));

            // When
            var result = await _handler.Handle(query, CancellationToken.None);

            // Then
            Assert.False(result.IsSuccess);
            Assert.Equal("An error occurred while retrieving categories: Database error", result.ErrorMessage);
        }

        [Fact]
        public async Task Handle_PaginationFirstPage_ReturnsCorrectNumberOfItems()
        {
            // Given
            var categories = TestDataHelper.CreateTestCategories(15);
            var query = new GetAllCategoriesQuery { Page = 1, PageSize = 5 };
            var cacheKey = "categories:all";

            _mockCacheKeyGenerator.Setup(x => x.GenerateAllCategoriesKey())
                .Returns(cacheKey);
            _mockCacheService.Setup(x => x.GetAsync<List<CategoryDto>>(cacheKey))
                .ReturnsAsync((List<CategoryDto>?)null);
            _mockCategoryRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(categories);
            _mockCacheService.Setup(x => x.SetAsync(cacheKey, It.IsAny<List<CategoryDto>>(), It.IsAny<TimeSpan>()))
                .Returns(Task.CompletedTask);

            // When
            var result = await _handler.Handle(query, CancellationToken.None);

            // Then
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(5, result.Data.Count);
        }

        [Fact]
        public async Task Handle_PaginationSecondPage_ReturnsCorrectItems()
        {
            // Given
            var categories = TestDataHelper.CreateTestCategories(15);
            var query = new GetAllCategoriesQuery { Page = 2, PageSize = 5 };
            var cacheKey = "categories:all";

            _mockCacheKeyGenerator.Setup(x => x.GenerateAllCategoriesKey())
                .Returns(cacheKey);
            _mockCacheService.Setup(x => x.GetAsync<List<CategoryDto>>(cacheKey))
                .ReturnsAsync((List<CategoryDto>?)null);
            _mockCategoryRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(categories);
            _mockCacheService.Setup(x => x.SetAsync(cacheKey, It.IsAny<List<CategoryDto>>(), It.IsAny<TimeSpan>()))
                .Returns(Task.CompletedTask);

            // When
            var result = await _handler.Handle(query, CancellationToken.None);

            // Then
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(5, result.Data.Count);
            
            // Verify it's the second page (items 6-10)
            var expectedCategories = categories.Skip(5).Take(5).ToList();
            for (int i = 0; i < result.Data.Count; i++)
            {
                Assert.Equal(expectedCategories[i].Id, result.Data[i].Id);
            }
        }

        [Fact]
        public async Task Handle_PaginationLastPage_ReturnsRemainingItems()
        {
            // Given
            var categories = TestDataHelper.CreateTestCategories(12);
            var query = new GetAllCategoriesQuery { Page = 3, PageSize = 5 };
            var cacheKey = "categories:all";

            _mockCacheKeyGenerator.Setup(x => x.GenerateAllCategoriesKey())
                .Returns(cacheKey);
            _mockCacheService.Setup(x => x.GetAsync<List<CategoryDto>>(cacheKey))
                .ReturnsAsync((List<CategoryDto>?)null);
            _mockCategoryRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(categories);
            _mockCacheService.Setup(x => x.SetAsync(cacheKey, It.IsAny<List<CategoryDto>>(), It.IsAny<TimeSpan>()))
                .Returns(Task.CompletedTask);

            // When
            var result = await _handler.Handle(query, CancellationToken.None);

            // Then
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.Count);
        }

        [Fact]
        public async Task Handle_PaginationPageBeyondData_ReturnsEmptyList()
        {
            // Given
            var categories = TestDataHelper.CreateTestCategories(5);
            var query = new GetAllCategoriesQuery { Page = 3, PageSize = 5 };
            var cacheKey = "categories:all";

            _mockCacheKeyGenerator.Setup(x => x.GenerateAllCategoriesKey())
                .Returns(cacheKey);
            _mockCacheService.Setup(x => x.GetAsync<List<CategoryDto>>(cacheKey))
                .ReturnsAsync((List<CategoryDto>?)null);
            _mockCategoryRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(categories);
            _mockCacheService.Setup(x => x.SetAsync(cacheKey, It.IsAny<List<CategoryDto>>(), It.IsAny<TimeSpan>()))
                .Returns(Task.CompletedTask);

            // When
            var result = await _handler.Handle(query, CancellationToken.None);

            // Then
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Empty(result.Data);
        }
    }
}