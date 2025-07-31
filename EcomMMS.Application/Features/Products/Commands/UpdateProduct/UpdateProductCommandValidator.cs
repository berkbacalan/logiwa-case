using FluentValidation;

namespace EcomMMS.Application.Features.Products.Commands.UpdateProduct
{
    public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
    {
        public UpdateProductCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Product ID is required.");

            RuleFor(x => x.Title)
                .MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.Title))
                .WithMessage("Title cannot exceed 200 characters.");

            RuleFor(x => x.CategoryId)
                .NotEmpty().When(x => x.CategoryId.HasValue)
                .WithMessage("Category ID cannot be empty when provided.");

            RuleFor(x => x.StockQuantity)
                .GreaterThanOrEqualTo(0).When(x => x.StockQuantity.HasValue)
                .WithMessage("Stock quantity cannot be negative.");
        }
    }
} 