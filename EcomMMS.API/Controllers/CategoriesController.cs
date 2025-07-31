using Microsoft.AspNetCore.Mvc;
using MediatR;
using EcomMMS.Application.Features.Categories.Queries.GetAllCategories;

namespace EcomMMS.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CategoriesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllCategories([FromQuery] GetAllCategoriesQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }
    }
} 