using MediatR;

namespace ProductService.API.Features.Product.Queries.GetAllProduct
{
    public record GetProductsQuery  : IRequest<List<ProductDto>>;
}
