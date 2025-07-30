using MediatR;
using EcomMMS.Application.Common;

namespace EcomMMS.Application.Features.Products.Commands.DeleteProduct
{
    public class DeleteProductCommand : IRequest<Result<bool>>
    {
        public Guid Id { get; set; }
    }
} 