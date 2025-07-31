using MediatR;
using Moq;
using FluentAssertions;
using EcomMMS.Domain.Entities;
using EcomMMS.Domain.Interfaces;
using EcomMMS.Application.Features.Products.Commands.UpdateProduct;
using EcomMMS.Application.Common;
using EcomMMS.Tests.TestData;
using FluentValidation;
using Xunit;

namespace EcomMMS.Tests.Features.Products.Commands.UpdateProduct
{
    public class UpdateProductCommandHandlerTests
    {
        private readonly Mock<IProductRepository> _mockProductRepository;
        private readonly Mock<ICategoryRepository> _mockCategoryRepository;
        private readonly Mock<IValidator<UpdateProductCommand>> _mockValidator;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly UpdateProductCommandHandler _handler;

        public UpdateProductCommandHandlerTests()
        {
            _mockProductRepository = new Mock<IProductRepository>();
            _mockCategoryRepository = new Mock<ICategoryRepository>();
            _mockValidator = new Mock<IValidator<UpdateProductCommand>>();
            _mockCacheService = new Mock<ICacheService>();
            _handler = new UpdateProductCommandHandler(_mockProductRepository.Object, _mockCategoryRepository.Object, _mockValidator.Object, _mockCacheService.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldUpdateProductSuccessfully()
        {
            // Given
            var category = TestDataHelper.CreateTestCategory();
            var existingProduct = new Product("Old Title", "Old Description", category.Id, 10);
            existingProduct.SetCategory(category);

            var command = new UpdateProductCommand
            {
                Id = existingProduct.Id,
                Title = "Updated Title",
                Description = "Updated Description",
                CategoryId = category.Id,
                StockQuantity = 20
            };

            var validationResult = new FluentValidation.Results.ValidationResult();
            _mockValidator.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);
            _mockProductRepository.Setup(x => x.GetByIdAsync(existingProduct.Id))
                .ReturnsAsync(existingProduct);
            _mockCategoryRepository.Setup(x => x.GetByIdAsync(category.Id))
                .ReturnsAsync(category);
            _mockProductRepository.Setup(x => x.UpdateAsync(It.IsAny<Product>()))
                .Returns(Task.CompletedTask);
            _mockCacheService.Setup(x => x.RemoveByPatternAsync("products:*"))
                .Returns(Task.CompletedTask);

            // When
            var result = await _handler.Handle(command, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Title.Should().Be(command.Title);
            result.Data.Description.Should().Be(command.Description);
            result.Data.CategoryId.Should().Be(category.Id);
            result.Data.StockQuantity.Should().Be(command.StockQuantity);

            _mockValidator.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
            _mockProductRepository.Verify(x => x.GetByIdAsync(existingProduct.Id), Times.Once);
            _mockCategoryRepository.Verify(x => x.GetByIdAsync(category.Id), Times.Exactly(2)); // Once for validation, once for DTO creation
            _mockProductRepository.Verify(x => x.UpdateAsync(It.IsAny<Product>()), Times.Once);
            _mockCacheService.Verify(x => x.RemoveByPatternAsync("products:*"), Times.Once);
        }

        [Fact]
        public async Task Handle_PartialUpdate_ShouldUpdateOnlyProvidedFields()
        {
            // Given
            var category = TestDataHelper.CreateTestCategory();
            var existingProduct = new Product("Old Title", "Old Description", category.Id, 10);
            existingProduct.SetCategory(category);

            var command = new UpdateProductCommand
            {
                Id = existingProduct.Id,
                Title = "Updated Title"
            };

            var validationResult = new FluentValidation.Results.ValidationResult();
            _mockValidator.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);
            _mockProductRepository.Setup(x => x.GetByIdAsync(existingProduct.Id))
                .ReturnsAsync(existingProduct);
            _mockCategoryRepository.Setup(x => x.GetByIdAsync(category.Id))
                .ReturnsAsync(category);
            _mockProductRepository.Setup(x => x.UpdateAsync(It.IsAny<Product>()))
                .Returns(Task.CompletedTask);
            _mockCacheService.Setup(x => x.RemoveByPatternAsync("products:*"))
                .Returns(Task.CompletedTask);

            // When
            var result = await _handler.Handle(command, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Title.Should().Be("Updated Title");
            result.Data.Description.Should().Be("Old Description");
            result.Data.CategoryId.Should().Be(category.Id);
            result.Data.StockQuantity.Should().Be(10);

            _mockValidator.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
            _mockProductRepository.Verify(x => x.GetByIdAsync(existingProduct.Id), Times.Once);
            _mockCategoryRepository.Verify(x => x.GetByIdAsync(category.Id), Times.Once); // Called for DTO creation
            _mockProductRepository.Verify(x => x.UpdateAsync(It.IsAny<Product>()), Times.Once);
            _mockCacheService.Verify(x => x.RemoveByPatternAsync("products:*"), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidationFails_ShouldReturnFailureResult()
        {
            // Given
            var command = new UpdateProductCommand
            {
                Id = Guid.Empty,
                Title = "A".PadRight(201, 'A'),
                CategoryId = Guid.Empty,
                StockQuantity = -1
            };

            var validationErrors = new List<FluentValidation.Results.ValidationFailure>
            {
                new FluentValidation.Results.ValidationFailure("Id", "Product ID is required."),
                new FluentValidation.Results.ValidationFailure("Title", "Title cannot exceed 200 characters."),
                new FluentValidation.Results.ValidationFailure("CategoryId", "Category ID cannot be empty when provided."),
                new FluentValidation.Results.ValidationFailure("StockQuantity", "Stock quantity cannot be negative.")
            };

            var validationResult = new FluentValidation.Results.ValidationResult(validationErrors);
            _mockValidator.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            // When
            var result = await _handler.Handle(command, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Data.Should().BeNull();
            result.ErrorMessage.Should().Be("Validation failed");
            result.Errors.Should().HaveCount(4);

            _mockProductRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
            _mockCategoryRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
            _mockProductRepository.Verify(x => x.UpdateAsync(It.IsAny<Product>()), Times.Never);
            _mockCacheService.Verify(x => x.RemoveByPatternAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ProductNotFound_ShouldReturnFailureResult()
        {
            // Given
            var command = new UpdateProductCommand
            {
                Id = Guid.NewGuid(),
                Title = "Test Product",
                CategoryId = Guid.NewGuid(),
                StockQuantity = 15
            };

            var validationResult = new FluentValidation.Results.ValidationResult();
            _mockValidator.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);
            _mockProductRepository.Setup(x => x.GetByIdAsync(command.Id))
                .ReturnsAsync((Product?)null);

            // When
            var result = await _handler.Handle(command, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Data.Should().BeNull();
            result.ErrorMessage.Should().Contain("not found");

            _mockCategoryRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
            _mockProductRepository.Verify(x => x.UpdateAsync(It.IsAny<Product>()), Times.Never);
            _mockCacheService.Verify(x => x.RemoveByPatternAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_CategoryNotFound_ShouldReturnFailureResult()
        {
            // Given
            var category = TestDataHelper.CreateTestCategory();
            var existingProduct = new Product("Old Title", "Old Description", category.Id, 10);
            existingProduct.SetCategory(category);

            var command = new UpdateProductCommand
            {
                Id = existingProduct.Id,
                Title = "Updated Title",
                CategoryId = Guid.NewGuid(),
                StockQuantity = 20
            };

            var validationResult = new FluentValidation.Results.ValidationResult();
            _mockValidator.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);
            _mockProductRepository.Setup(x => x.GetByIdAsync(existingProduct.Id))
                .ReturnsAsync(existingProduct);
            _mockCategoryRepository.Setup(x => x.GetByIdAsync(command.CategoryId!.Value))
                .ReturnsAsync((Category?)null);

            // When
            var result = await _handler.Handle(command, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Data.Should().BeNull();
            result.ErrorMessage.Should().Contain("not found");

            _mockProductRepository.Verify(x => x.UpdateAsync(It.IsAny<Product>()), Times.Never);
            _mockCacheService.Verify(x => x.RemoveByPatternAsync(It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", "Title cannot exceed 200 characters")]
        public async Task Handle_InvalidTitle_ShouldReturnFailureResult(string title, string expectedError)
        {
            // Given
            var command = new UpdateProductCommand
            {
                Id = Guid.NewGuid(),
                Title = title,
                CategoryId = Guid.NewGuid(),
                StockQuantity = 15
            };

            var validationErrors = new List<FluentValidation.Results.ValidationFailure>
            {
                new FluentValidation.Results.ValidationFailure("Title", expectedError)
            };

            var validationResult = new FluentValidation.Results.ValidationResult(validationErrors);
            _mockValidator.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            // When
            var result = await _handler.Handle(command, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Data.Should().BeNull();
            result.ErrorMessage.Should().Be("Validation failed");
            result.Errors.Should().Contain(expectedError);

            _mockProductRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
            _mockCategoryRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
            _mockProductRepository.Verify(x => x.UpdateAsync(It.IsAny<Product>()), Times.Never);
            _mockCacheService.Verify(x => x.RemoveByPatternAsync(It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-10)]
        [InlineData(-100)]
        public async Task Handle_NegativeStockQuantity_ShouldReturnFailureResult(int stockQuantity)
        {
            // Given
            var command = new UpdateProductCommand
            {
                Id = Guid.NewGuid(),
                Title = "Test Product",
                CategoryId = Guid.NewGuid(),
                StockQuantity = stockQuantity
            };

            var validationErrors = new List<FluentValidation.Results.ValidationFailure>
            {
                new FluentValidation.Results.ValidationFailure("StockQuantity", "Stock quantity cannot be negative.")
            };

            var validationResult = new FluentValidation.Results.ValidationResult(validationErrors);
            _mockValidator.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            // When
            var result = await _handler.Handle(command, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Data.Should().BeNull();
            result.ErrorMessage.Should().Be("Validation failed");
            result.Errors.Should().Contain("Stock quantity cannot be negative.");

            _mockProductRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
            _mockCategoryRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
            _mockProductRepository.Verify(x => x.UpdateAsync(It.IsAny<Product>()), Times.Never);
            _mockCacheService.Verify(x => x.RemoveByPatternAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_EmptyCategoryId_ShouldReturnFailureResult()
        {
            // Given
            var command = new UpdateProductCommand
            {
                Id = Guid.NewGuid(),
                Title = "Test Product",
                CategoryId = Guid.Empty,
                StockQuantity = 15
            };

            var validationErrors = new List<FluentValidation.Results.ValidationFailure>
            {
                new FluentValidation.Results.ValidationFailure("CategoryId", "Category ID cannot be empty when provided.")
            };

            var validationResult = new FluentValidation.Results.ValidationResult(validationErrors);
            _mockValidator.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            // When
            var result = await _handler.Handle(command, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Data.Should().BeNull();
            result.ErrorMessage.Should().Be("Validation failed");
            result.Errors.Should().Contain("Category ID cannot be empty when provided.");

            _mockProductRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
            _mockCategoryRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
            _mockProductRepository.Verify(x => x.UpdateAsync(It.IsAny<Product>()), Times.Never);
            _mockCacheService.Verify(x => x.RemoveByPatternAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_CacheServiceThrowsException_ShouldStillUpdateProduct()
        {
            // Given
            var category = TestDataHelper.CreateTestCategory();
            var existingProduct = new Product("Old Title", "Old Description", category.Id, 10);
            existingProduct.SetCategory(category);

            var command = new UpdateProductCommand
            {
                Id = existingProduct.Id,
                Title = "Updated Title",
                Description = "Updated Description",
                CategoryId = category.Id,
                StockQuantity = 20
            };

            var validationResult = new FluentValidation.Results.ValidationResult();
            _mockValidator.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);
            _mockProductRepository.Setup(x => x.GetByIdAsync(existingProduct.Id))
                .ReturnsAsync(existingProduct);
            _mockCategoryRepository.Setup(x => x.GetByIdAsync(category.Id))
                .ReturnsAsync(category);
            _mockProductRepository.Setup(x => x.UpdateAsync(It.IsAny<Product>()))
                .Returns(Task.CompletedTask);
            _mockCacheService.Setup(x => x.RemoveByPatternAsync("products:*"))
                .ThrowsAsync(new Exception("Cache service error"));

            // When
            var result = await _handler.Handle(command, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Title.Should().Be(command.Title);
            result.Data.Description.Should().Be(command.Description);
            result.Data.CategoryId.Should().Be(category.Id);
            result.Data.StockQuantity.Should().Be(command.StockQuantity);

            _mockValidator.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
            _mockProductRepository.Verify(x => x.GetByIdAsync(existingProduct.Id), Times.Once);
            _mockCategoryRepository.Verify(x => x.GetByIdAsync(category.Id), Times.Exactly(2));
            _mockProductRepository.Verify(x => x.UpdateAsync(It.IsAny<Product>()), Times.Once);
            _mockCacheService.Verify(x => x.RemoveByPatternAsync("products:*"), Times.Once);
        }
    }
} 