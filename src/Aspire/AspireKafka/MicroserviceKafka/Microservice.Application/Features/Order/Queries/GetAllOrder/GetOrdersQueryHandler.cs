using AutoMapper;
using MediatR;
using Microservice.Application.Repositories;

namespace Microservice.Application.Features.Order.Queries.GetAllOrder
{
    public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, List<OrderDto>>
    {
        private readonly IMapper _mapper;
        private readonly IOrderRepository _orderRepository;

        public GetOrdersQueryHandler(IMapper mapper, IOrderRepository orderRepository)
        {
            _mapper = mapper;
            _orderRepository = orderRepository;
        }
        public async Task<List<OrderDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
        {
            var orders = await _orderRepository.GetAsync();

            var datas = _mapper.Map<List<OrderDto>>(orders);

            return datas;
        }
    }

}
