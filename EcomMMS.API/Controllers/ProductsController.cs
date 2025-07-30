using Microsoft.AspNetCore.Mvc;
using MediatR;
using EcomMMS.Application.Features.Products.Commands.CreateProduct;
using EcomMMS.Application.Features.Products.Commands.UpdateProduct;
using EcomMMS.Application.Features.Products.Commands.DeleteProduct;
using EcomMMS.Application.Features.Products.Queries.GetProductById;
using EcomMMS.Application.Features.Products.Queries.GetFilteredProducts;

namespace EcomMMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ProductsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductCommand command)
        {
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                if (result.Errors.Any())
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = result.ErrorMessage,
                        Errors = result.Errors
                    });
                }

                return BadRequest(new
                {
                    Success = false,
                    Message = result.ErrorMessage
                });
            }

            return CreatedAtAction(nameof(GetProductById), new { id = result.Data!.Id }, new
            {
                Success = true,
                Data = result.Data
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductCommand command)
        {
            command.Id = id;
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                if (result.Errors.Any())
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = result.ErrorMessage,
                        Errors = result.Errors
                    });
                }

                return NotFound(new
                {
                    Success = false,
                    Message = result.ErrorMessage
                });
            }

            return Ok(new
            {
                Success = true,
                Data = result.Data
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(Guid id)
        {
            var command = new DeleteProductCommand { Id = id };
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                if (result.Errors.Any())
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = result.ErrorMessage,
                        Errors = result.Errors
                    });
                }

                return NotFound(new
                {
                    Success = false,
                    Message = result.ErrorMessage
                });
            }

            return Ok(new
            {
                Success = true,
                Message = "Product deleted successfully"
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(Guid id)
        {
            var query = new GetProductByIdQuery { Id = id };
            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = result.ErrorMessage,
                    Errors = result.Errors
                });
            }

            if (result.Data == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Product not found"
                });
            }

            return Ok(new
            {
                Success = true,
                Data = result.Data
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetFilteredProducts(
            [FromQuery] string? searchTerm,
            [FromQuery] int? minStockQuantity,
            [FromQuery] int? maxStockQuantity,
            [FromQuery] bool? isLive,
            [FromQuery] Guid? categoryId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = new GetFilteredProductsQuery
            {
                SearchTerm = searchTerm,
                MinStockQuantity = minStockQuantity,
                MaxStockQuantity = maxStockQuantity,
                IsLive = isLive,
                CategoryId = categoryId,
                Page = page,
                PageSize = pageSize
            };

            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = result.ErrorMessage,
                    Errors = result.Errors
                });
            }

            return Ok(new
            {
                Success = true,
                Data = result.Data
            });
        }
    }
} 