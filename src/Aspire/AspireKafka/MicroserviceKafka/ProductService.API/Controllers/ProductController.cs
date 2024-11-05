using Confluent.Kafka;
using MediatR;
using Microservice.Application.Kafka;
using Microservice.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using ProductService.API.Features.Product.Command.Create;
using ProductService.API.Features.Product.Queries.GetAllProduct;
using System.Text.Json;

namespace ProductService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IKafkaProducer<string, string> _producer;

        public ProductController(IMediator mediator, IKafkaProducer<string, string> producer)
        {
            _mediator = mediator;
            _producer = producer;
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
            var pruductMessage = new ProductMessage
            {
                OrderId = product.Id,
                ProductId = product.Id,
                Quantity = product.Quantity
            };

            await _producer.ProduceAsync("order-topic", new Message<string, string>
            {
                Key = product.Id.ToString(),
                Value = JsonSerializer.Serialize(pruductMessage)
            });

            return CreatedAtAction(nameof(GetProduct), new {});
            
        }
    }
}
