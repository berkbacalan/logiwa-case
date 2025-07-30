using EcomMMS.Application.Features.Products.Commands.CreateProduct;
using FluentAssertions;
using FluentValidation;
using Xunit;

namespace EcomMMS.Tests.Features.Products.Commands.CreateProduct
{
    public class CreateProductCommandValidatorTests
    {
        private readonly CreateProductCommandValidator _validator;

        public CreateProductCommandValidatorTests()
        {
            _validator = new CreateProductCommandValidator();
        }

        [Fact]
        public void Validate_ValidCommand_ShouldPassValidation()
        {
            // Given
            var command = new CreateProductCommand
            {
                Title = "Valid Product Title",
                Description = "Valid description",
                CategoryId = Guid.NewGuid(),
                StockQuantity = 10
            };

            // When
            var result = _validator.Validate(command);

            // Then
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Validate_EmptyOrNullTitle_ShouldFailValidation(string? title)
        {
            // Given
            var command = new CreateProductCommand
            {
                Title = title ?? string.Empty,
                Description = "Valid description",
                CategoryId = Guid.NewGuid(),
                StockQuantity = 10
            };

            // When
            var result = _validator.Validate(command);

            // Then
            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle(x => x.PropertyName == "Title");
            result.Errors.First(x => x.PropertyName == "Title").ErrorMessage.Should().Be("Title is required.");
        }

        [Fact]
        public void Validate_TitleExceeds200Characters_ShouldFailValidation()
        {
            // Given
            var longTitle = new string('A', 201);
            var command = new CreateProductCommand
            {
                Title = longTitle,
                Description = "Valid description",
                CategoryId = Guid.NewGuid(),
                StockQuantity = 10
            };

            // When
            var result = _validator.Validate(command);

            // Then
            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle(x => x.PropertyName == "Title");
            result.Errors.First(x => x.PropertyName == "Title").ErrorMessage.Should().Be("Title cannot exceed 200 characters.");
        }

        [Fact]
        public void Validate_TitleExactly200Characters_ShouldPassValidation()
        {
            // Given
            var title = new string('A', 200);
            var command = new CreateProductCommand
            {
                Title = title,
                Description = "Valid description",
                CategoryId = Guid.NewGuid(),
                StockQuantity = 10
            };

            // When
            var result = _validator.Validate(command);

            // Then
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_EmptyCategoryId_ShouldFailValidation()
        {
            // Given
            var command = new CreateProductCommand
            {
                Title = "Valid Product Title",
                Description = "Valid description",
                CategoryId = Guid.Empty,
                StockQuantity = 10
            };

            // When
            var result = _validator.Validate(command);

            // Then
            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle(x => x.PropertyName == "CategoryId");
            result.Errors.First(x => x.PropertyName == "CategoryId").ErrorMessage.Should().Be("Category ID is required.");
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-10)]
        [InlineData(-100)]
        public void Validate_NegativeStockQuantity_ShouldFailValidation(int stockQuantity)
        {
            // Given
            var command = new CreateProductCommand
            {
                Title = "Valid Product Title",
                Description = "Valid description",
                CategoryId = Guid.NewGuid(),
                StockQuantity = stockQuantity
            };

            // When
            var result = _validator.Validate(command);

            // Then
            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle(x => x.PropertyName == "StockQuantity");
            result.Errors.First(x => x.PropertyName == "StockQuantity").ErrorMessage.Should().Be("Stock quantity cannot be negative.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(1000)]
        public void Validate_ValidStockQuantity_ShouldPassValidation(int stockQuantity)
        {
            // Given
            var command = new CreateProductCommand
            {
                Title = "Valid Product Title",
                Description = "Valid description",
                CategoryId = Guid.NewGuid(),
                StockQuantity = stockQuantity
            };

            // When
            var result = _validator.Validate(command);

            // Then
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_MultipleValidationErrors_ShouldReturnAllErrors()
        {
            // Given
            var command = new CreateProductCommand
            {
                Title = "",
                Description = "Valid description",
                CategoryId = Guid.Empty,
                StockQuantity = -5
            };

            // When
            var result = _validator.Validate(command);

            // Then
            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(3);
            result.Errors.Should().Contain(x => x.PropertyName == "Title");
            result.Errors.Should().Contain(x => x.PropertyName == "CategoryId");
            result.Errors.Should().Contain(x => x.PropertyName == "StockQuantity");
        }

        [Fact]
        public void Validate_NullDescription_ShouldPassValidation()
        {
            // Given
            var command = new CreateProductCommand
            {
                Title = "Valid Product Title",
                Description = null,
                CategoryId = Guid.NewGuid(),
                StockQuantity = 10
            };

            // When
            var result = _validator.Validate(command);

            // Then
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_EmptyDescription_ShouldPassValidation()
        {
            // Given
            var command = new CreateProductCommand
            {
                Title = "Valid Product Title",
                Description = "",
                CategoryId = Guid.NewGuid(),
                StockQuantity = 10
            };

            // When
            var result = _validator.Validate(command);

            // Then
            result.IsValid.Should().BeTrue();
        }
    }
} 