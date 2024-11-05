using MediatR;

namespace OrderService.API.Features.Order.Queries.GetAllOrder
{
    public record GetOrdersQuery : IRequest<List<OrderDto>>;

}
