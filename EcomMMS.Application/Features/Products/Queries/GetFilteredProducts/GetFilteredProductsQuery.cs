using MediatR;
using EcomMMS.Application.DTOs;
using EcomMMS.Application.Common;

namespace EcomMMS.Application.Features.Products.Queries.GetFilteredProducts
{
    public class GetFilteredProductsQuery : IRequest<Result<PaginatedResult<ProductDto>>>
    {
        public string? SearchTerm { get; set; }
        public int? MinStockQuantity { get; set; }
        public int? MaxStockQuantity { get; set; }
        public bool? IsLive { get; set; }
        public Guid? CategoryId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
} 