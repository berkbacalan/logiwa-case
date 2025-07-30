using Microsoft.AspNetCore.Mvc;
using MediatR;
using EcomMMS.Application.Features.Categories.Queries.GetAllCategories;

namespace EcomMMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CategoriesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCategories(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = new GetAllCategoriesQuery
            {
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