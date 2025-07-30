using MediatR;
using EcomMMS.Application.DTOs;
using EcomMMS.Application.Common;

namespace EcomMMS.Application.Features.Products.Commands.CreateProduct
{
    public class CreateProductCommand : IRequest<Result<ProductDto>>
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid CategoryId { get; set; }
        public int StockQuantity { get; set; }
    }
} 