using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProductService.API.Features.Product.Command.Create;
using ProductService.API.Features.Product.Queries.GetAllProduct;

namespace ProductService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ProductController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [HttpGet]
        public async Task<ActionResult<List<ProductDto>>> GetProduct()
        {
            var productList = await _mediator.Send(new GetProductsQuery());

            return Ok(productList);
        }
        [HttpPost]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Post(CreateProductCommand product)
        {
            var response = await _mediator.Send(product);
            return CreatedAtAction(nameof(GetProduct), new {});
            
        }
    }
}
