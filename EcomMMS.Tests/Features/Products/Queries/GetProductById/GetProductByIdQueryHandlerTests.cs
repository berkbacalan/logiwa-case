using MediatR;
using Moq;
using FluentAssertions;
using EcomMMS.Domain.Entities;
using EcomMMS.Domain.Interfaces;
using EcomMMS.Application.Features.Products.Queries.GetProductById;
using EcomMMS.Application.Common;
using EcomMMS.Tests.TestData;
using Xunit;

namespace EcomMMS.Tests.Features.Products.Queries.GetProductById
{
    public class GetProductByIdQueryHandlerTests
    {
        private readonly Mock<IProductRepository> _mockProductRepository;
        private readonly Mock<ICategoryRepository> _mockCategoryRepository;
        private readonly GetProductByIdQueryHandler _handler;

        public GetProductByIdQueryHandlerTests()
        {
            _mockProductRepository = new Mock<IProductRepository>();
            _mockCategoryRepository = new Mock<ICategoryRepository>();
            _handler = new GetProductByIdQueryHandler(_mockProductRepository.Object, _mockCategoryRepository.Object);
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
        }

        [Fact]
        public async Task Handle_ProductNotFound_ShouldReturnSuccessWithNull()
        {
            // Given
            var query = new GetProductByIdQuery
            {
                Id = Guid.NewGuid()
            };

            _mockProductRepository.Setup(x => x.GetByIdAsync(query.Id))
                .ReturnsAsync((Product?)null);

            // When
            var result = await _handler.Handle(query, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeNull();

            _mockProductRepository.Verify(x => x.GetByIdAsync(query.Id), Times.Once);
            _mockCategoryRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
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

            // When
            var result = await _handler.Handle(query, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.CategoryName.Should().BeEmpty();

            _mockProductRepository.Verify(x => x.GetByIdAsync(product.Id), Times.Once);
            _mockCategoryRepository.Verify(x => x.GetByIdAsync(product.CategoryId), Times.Once);
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

            // When
            var result = await _handler.Handle(query, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(productId);

            _mockProductRepository.Verify(x => x.GetByIdAsync(productId), Times.Once);
            _mockCategoryRepository.Verify(x => x.GetByIdAsync(product.CategoryId), Times.Once);
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

            // When
            var result = await _handler.Handle(query, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Description.Should().BeNull();

            _mockProductRepository.Verify(x => x.GetByIdAsync(product.Id), Times.Once);
            _mockCategoryRepository.Verify(x => x.GetByIdAsync(category.Id), Times.Once);
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

            // When
            var result = await _handler.Handle(query, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.UpdatedAt.Should().BeNull();

            _mockProductRepository.Verify(x => x.GetByIdAsync(product.Id), Times.Once);
            _mockCategoryRepository.Verify(x => x.GetByIdAsync(category.Id), Times.Once);
        }
    }
} 