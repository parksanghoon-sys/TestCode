using MediatR;

namespace Microservice.Application.Features.Product.Queries.GetAllProduct
{
    public record GetProductsQuery  : IRequest<List<ProductDto>>;
}
