using MediatR;
using EcomMMS.Application.DTOs;
using EcomMMS.Application.Common;

namespace EcomMMS.Application.Features.Products.Queries.GetProductById
{
    public class GetProductByIdQuery : IRequest<Result<ProductDto?>>
    {
        public Guid Id { get; set; }
    }
} 