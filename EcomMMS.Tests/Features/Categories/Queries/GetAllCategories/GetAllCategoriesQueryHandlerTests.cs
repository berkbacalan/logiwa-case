using FluentAssertions;
using MediatR;
using Moq;
using EcomMMS.Application.Features.Categories.Queries.GetAllCategories;
using EcomMMS.Domain.Entities;
using EcomMMS.Domain.Interfaces;
using EcomMMS.Application.Common;
using EcomMMS.Application.DTOs;
using EcomMMS.Tests.TestData;
using Xunit;

namespace EcomMMS.Tests.Features.Categories.Queries.GetAllCategories
{
    public class GetAllCategoriesQueryHandlerTests
    {
        private readonly Mock<ICategoryRepository> _mockCategoryRepository;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<ICacheKeyGenerator> _mockCacheKeyGenerator;
        private readonly Mock<IApplicationLogger> _mockLogger;
        private readonly GetAllCategoriesQueryHandler _handler;

        public GetAllCategoriesQueryHandlerTests()
        {
            _mockCategoryRepository = new Mock<ICategoryRepository>();
            _mockCacheService = new Mock<ICacheService>();
            _mockCacheKeyGenerator = new Mock<ICacheKeyGenerator>();
            _mockLogger = new Mock<IApplicationLogger>();
            _handler = new GetAllCategoriesQueryHandler(_mockCategoryRepository.Object, _mockCacheService.Object, _mockCacheKeyGenerator.Object, _mockLogger.Object);
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
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Data.Should().HaveCount(2);
            result.Data.Metadata.TotalCount.Should().Be(2);

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
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Data.Should().BeEmpty();
            result.Data.Metadata.TotalCount.Should().Be(0);
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
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("An error occurred while retrieving categories: Database error");
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
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Data.Should().HaveCount(5);
            result.Data.Metadata.CurrentPage.Should().Be(1);
            result.Data.Metadata.PageSize.Should().Be(5);
            result.Data.Metadata.TotalCount.Should().Be(15);
            result.Data.Metadata.TotalPages.Should().Be(3);
            result.Data.Metadata.HasNextPage.Should().BeTrue();
            result.Data.Metadata.HasPreviousPage.Should().BeFalse();
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
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Data.Should().HaveCount(5);
            result.Data.Metadata.CurrentPage.Should().Be(2);
            result.Data.Metadata.PageSize.Should().Be(5);
            result.Data.Metadata.TotalCount.Should().Be(15);
            result.Data.Metadata.TotalPages.Should().Be(3);
            result.Data.Metadata.HasNextPage.Should().BeTrue();
            result.Data.Metadata.HasPreviousPage.Should().BeTrue();
            result.Data.Metadata.PreviousPage.Should().Be(1);
            result.Data.Metadata.NextPage.Should().Be(3);
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
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Data.Should().BeEmpty();
            result.Data.Metadata.CurrentPage.Should().Be(3);
            result.Data.Metadata.TotalPages.Should().Be(1);
        }
    }
}