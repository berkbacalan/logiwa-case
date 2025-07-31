using MediatR;
using Moq;
using FluentAssertions;
using EcomMMS.Domain.Entities;
using EcomMMS.Domain.Interfaces;
using EcomMMS.Application.Features.Products.Commands.DeleteProduct;
using EcomMMS.Application.Common;
using EcomMMS.Tests.TestData;
using FluentValidation;
using Xunit;

namespace EcomMMS.Tests.Features.Products.Commands.DeleteProduct
{
    public class DeleteProductCommandHandlerTests
    {
        private readonly Mock<IProductRepository> _mockProductRepository;
        private readonly Mock<IValidator<DeleteProductCommand>> _mockValidator;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<IApplicationLogger> _mockLogger;
        private readonly DeleteProductCommandHandler _handler;

        public DeleteProductCommandHandlerTests()
        {
            _mockProductRepository = new Mock<IProductRepository>();
            _mockValidator = new Mock<IValidator<DeleteProductCommand>>();
            _mockCacheService = new Mock<ICacheService>();
            _mockLogger = new Mock<IApplicationLogger>();
            _handler = new DeleteProductCommandHandler(_mockProductRepository.Object, _mockValidator.Object, _mockCacheService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldDeleteProductSuccessfully()
        {
            // Given
            var category = TestDataHelper.CreateTestCategory();
            var product = new Product("Test Product", "Test Description", category.Id, 15);
            product.SetCategory(category);

            var command = new DeleteProductCommand
            {
                Id = product.Id
            };

            var validationResult = new FluentValidation.Results.ValidationResult();
            _mockValidator.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);
            _mockProductRepository.Setup(x => x.GetByIdAsync(product.Id))
                .ReturnsAsync(product);
            _mockProductRepository.Setup(x => x.DeleteAsync(product.Id))
                .Returns(Task.CompletedTask);
            _mockCacheService.Setup(x => x.RemoveByPatternAsync("products:*"))
                .Returns(Task.CompletedTask);

            // When
            var result = await _handler.Handle(command, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();

            _mockValidator.Verify(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()), Times.Once);
            _mockProductRepository.Verify(x => x.GetByIdAsync(product.Id), Times.Once);
            _mockProductRepository.Verify(x => x.DeleteAsync(product.Id), Times.Once);
            _mockCacheService.Verify(x => x.RemoveByPatternAsync("products:*"), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidationFails_ShouldReturnFailureResult()
        {
            // Given
            var command = new DeleteProductCommand
            {
                Id = Guid.Empty
            };

            var validationErrors = new List<FluentValidation.Results.ValidationFailure>
            {
                new FluentValidation.Results.ValidationFailure("Id", "Product ID is required.")
            };

            var validationResult = new FluentValidation.Results.ValidationResult(validationErrors);
            _mockValidator.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            // When
            var result = await _handler.Handle(command, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Data.Should().BeFalse();
            result.ErrorMessage.Should().Be("Validation failed");
            result.Errors.Should().Contain("Product ID is required.");

            _mockProductRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
            _mockProductRepository.Verify(x => x.DeleteAsync(It.IsAny<Guid>()), Times.Never);
            _mockCacheService.Verify(x => x.RemoveByPatternAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ProductNotFound_ShouldReturnFailureResult()
        {
            // Given
            var command = new DeleteProductCommand
            {
                Id = Guid.NewGuid()
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
            result.Data.Should().BeFalse();
            result.ErrorMessage.Should().Contain("not found");

            _mockProductRepository.Verify(x => x.DeleteAsync(It.IsAny<Guid>()), Times.Never);
            _mockCacheService.Verify(x => x.RemoveByPatternAsync(It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData("00000000-0000-0000-0000-000000000000")]
        [InlineData("11111111-1111-1111-1111-111111111111")]
        [InlineData("22222222-2222-2222-2222-222222222222")]
        public async Task Handle_DifferentProductIds_ShouldDeleteCorrectProduct(string productIdString)
        {
            // Given
            var productId = Guid.Parse(productIdString);
            var category = TestDataHelper.CreateTestCategory();
            var product = new Product("Test Product", "Test Description", category.Id, 15);
            product.SetCategory(category);

            var command = new DeleteProductCommand
            {
                Id = productId
            };

            var validationResult = new FluentValidation.Results.ValidationResult();
            _mockValidator.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);
            _mockProductRepository.Setup(x => x.GetByIdAsync(productId))
                .ReturnsAsync(product);
            _mockProductRepository.Setup(x => x.DeleteAsync(productId))
                .Returns(Task.CompletedTask);
            _mockCacheService.Setup(x => x.RemoveByPatternAsync("products:*"))
                .Returns(Task.CompletedTask);

            // When
            var result = await _handler.Handle(command, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();

            _mockProductRepository.Verify(x => x.GetByIdAsync(productId), Times.Once);
            _mockProductRepository.Verify(x => x.DeleteAsync(productId), Times.Once);
            _mockCacheService.Verify(x => x.RemoveByPatternAsync("products:*"), Times.Once);
        }

        [Fact]
        public async Task Handle_CacheServiceThrowsException_ShouldStillDeleteProduct()
        {
            // Given
            var category = TestDataHelper.CreateTestCategory();
            var product = new Product("Test Product", "Test Description", category.Id, 15);
            product.SetCategory(category);

            var command = new DeleteProductCommand
            {
                Id = product.Id
            };

            var validationResult = new FluentValidation.Results.ValidationResult();
            _mockValidator.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);
            _mockProductRepository.Setup(x => x.GetByIdAsync(product.Id))
                .ReturnsAsync(product);
            _mockProductRepository.Setup(x => x.DeleteAsync(product.Id))
                .Returns(Task.CompletedTask);
            _mockCacheService.Setup(x => x.RemoveByPatternAsync("products:*"))
                .ThrowsAsync(new Exception("Cache service error"));

            // When
            var result = await _handler.Handle(command, CancellationToken.None);

            // Then
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();

            _mockProductRepository.Verify(x => x.GetByIdAsync(product.Id), Times.Once);
            _mockProductRepository.Verify(x => x.DeleteAsync(product.Id), Times.Once);
            _mockCacheService.Verify(x => x.RemoveByPatternAsync("products:*"), Times.Once);
        }
    }
} 