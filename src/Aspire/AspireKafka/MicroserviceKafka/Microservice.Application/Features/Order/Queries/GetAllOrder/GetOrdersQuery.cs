using MediatR;

namespace Microservice.Application.Features.Order.Queries.GetAllOrder
{
    public record GetOrdersQuery : IRequest<List<OrderDto>>;

}
