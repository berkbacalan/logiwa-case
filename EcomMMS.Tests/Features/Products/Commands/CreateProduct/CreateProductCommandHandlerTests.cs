using MediatR;
using Moq;
using FluentAssertions;
using EcomMMS.Domain.Entities;
using EcomMMS.Domain.Interfaces;
using EcomMMS.Application.Features.Products.Commands.CreateProduct;
using EcomMMS.Application.Common;
using EcomMMS.Tests.TestData;
using FluentValidation;
using Xunit;

namespace EcomMMS.Tests.Features.Products.Commands.CreateProduct
{
    public class CreateProductCommandHandlerTests
    {
        private readonly Mock<IProductRepository> _mockProductRepository;
        private readonly Mock<ICategoryRepository> _mockCategoryRepository;
        private readonly Mock<IValidator<CreateProductCommand>> _mockValidator;
        private readonly CreateProductCommandHandler _handler;

        public CreateProductCommandHandlerTests()
        {
            _mockProductRepository = new Mock<IProductRepository>();
            _mockCategoryRepository = new Mock<ICategoryRepository>();
            _mockValidator = new Mock<IValidator<CreateProductCommand>>();
            _handler = new CreateProductCommandHandler(_mockProductRepository.Object, _mockCategoryRepository.Object, _mockValidator.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldCreateProductSuccessfully()
        {
            // Given
            var category = TestDataHelper.CreateTestCategory();
            var command = new CreateProductCommand
            {
                Title = "Test Product",
                Description = "Test Description",
                CategoryId = category.Id,
                StockQuantity = 15
            };

            var product = new Product(command.Title, command.Description, category.Id, command.StockQuantity);
            product.SetCategory(category);

            var validationResult = new FluentValidation.Results.ValidationResult();
            _mockValidator.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);
            _mockCategoryRepository.Setup(x => x.GetByIdAsync(category.Id))
                .ReturnsAsync(category);
            _mockProductRepository.Setup(x => x.AddAsync(It.IsAny<Product>()))
                .ReturnsAsync(product);

            // When
            var result = await _handler.Handle(command, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Title.Should().Be(command.Title);
            result.Data.CategoryId.Should().Be(category.Id);
            result.Data.CategoryName.Should().Be(category.Name);
            result.Data.StockQuantity.Should().Be(command.StockQuantity);
            result.Data.IsLive.Should().Be(product.IsLive);

            _mockValidator.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
            _mockCategoryRepository.Verify(x => x.GetByIdAsync(category.Id), Times.Once);
            _mockProductRepository.Verify(x => x.AddAsync(It.IsAny<Product>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidationFails_ShouldReturnFailureResult()
        {
            // Given
            var command = new CreateProductCommand
            {
                Title = "",
                CategoryId = Guid.NewGuid(),
                StockQuantity = -1
            };

            var validationErrors = new List<FluentValidation.Results.ValidationFailure>
            {
                new FluentValidation.Results.ValidationFailure("Title", "Title is required."),
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
            result.Errors.Should().HaveCount(2);
            result.Errors.Should().Contain("Title is required.");
            result.Errors.Should().Contain("Stock quantity cannot be negative.");

            _mockCategoryRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
            _mockProductRepository.Verify(x => x.AddAsync(It.IsAny<Product>()), Times.Never);
        }

        [Fact]
        public async Task Handle_CategoryNotFound_ShouldReturnFailureResult()
        {
            // Given
            var command = new CreateProductCommand
            {
                Title = "Test Product",
                CategoryId = Guid.NewGuid(),
                StockQuantity = 15
            };

            var validationResult = new FluentValidation.Results.ValidationResult();
            _mockValidator.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);
            _mockCategoryRepository.Setup(x => x.GetByIdAsync(command.CategoryId))
                .ReturnsAsync((Category?)null);

            // When
            var result = await _handler.Handle(command, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Data.Should().BeNull();
            result.ErrorMessage.Should().Contain("not found");

            _mockProductRepository.Verify(x => x.AddAsync(It.IsAny<Product>()), Times.Never);
        }

        [Theory]
        [InlineData("", "Title is required")]
        [InlineData("A", "Title is valid")]
        [InlineData("This is a very long title that exceeds the maximum allowed length of 200 characters. This should cause a validation error because the title is too long and should be rejected by the validation logic.", "Title cannot exceed 200 characters")]
        public async Task Handle_InvalidTitle_ShouldReturnFailureResult(string title, string expectedError)
        {
            // Given
            var command = new CreateProductCommand
            {
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

            _mockCategoryRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
            _mockProductRepository.Verify(x => x.AddAsync(It.IsAny<Product>()), Times.Never);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-10)]
        [InlineData(-100)]
        public async Task Handle_NegativeStockQuantity_ShouldReturnFailureResult(int stockQuantity)
        {
            // Given
            var command = new CreateProductCommand
            {
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

            _mockCategoryRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
            _mockProductRepository.Verify(x => x.AddAsync(It.IsAny<Product>()), Times.Never);
        }

        [Fact]
        public async Task Handle_EmptyCategoryId_ShouldReturnFailureResult()
        {
            // Given
            var command = new CreateProductCommand
            {
                Title = "Test Product",
                CategoryId = Guid.Empty,
                StockQuantity = 15
            };

            var validationErrors = new List<FluentValidation.Results.ValidationFailure>
            {
                new FluentValidation.Results.ValidationFailure("CategoryId", "Category ID is required.")
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
            result.Errors.Should().Contain("Category ID is required.");

            _mockCategoryRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
            _mockProductRepository.Verify(x => x.AddAsync(It.IsAny<Product>()), Times.Never);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task Handle_NullOrEmptyDescription_ShouldCreateProductSuccessfully(string? description)
        {
            // Given
            var category = TestDataHelper.CreateTestCategory();
            var command = new CreateProductCommand
            {
                Title = "Test Product",
                Description = description,
                CategoryId = category.Id,
                StockQuantity = 15
            };

            var product = new Product(command.Title, command.Description, category.Id, command.StockQuantity);
            product.SetCategory(category);

            var validationResult = new FluentValidation.Results.ValidationResult();
            _mockValidator.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);
            _mockCategoryRepository.Setup(x => x.GetByIdAsync(category.Id))
                .ReturnsAsync(category);
            _mockProductRepository.Setup(x => x.AddAsync(It.IsAny<Product>()))
                .ReturnsAsync(product);

            // When
            var result = await _handler.Handle(command, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Description.Should().Be(description);
        }
    }
} 