using AutoMapper;
using MediatR;
using Microservice.Application.Repositories;
using Microservice.Doamin;

namespace OrderService.API.Features.Order.Command.Create
{
    public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, int>
    {
        private readonly IMapper _mapper;
        private readonly IOrderRepository _orderRepository;

        public CreateOrderCommandHandler(IMapper mapper, IOrderRepository orderRepository)
        {
            _mapper = mapper;
            _orderRepository = orderRepository;
        }
        public async Task<int> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
        {
            var validator = new CreateOrderCommandValidator(_orderRepository);

            var validationResult = await validator.ValidateAsync(request, cancellationToken);

            if ((validationResult.Errors.Any()))
            {
                throw new Exception("Create Order Err");
            }

            var orderCreate = _mapper.Map<OrderModel>(request);
            orderCreate.Quantity = 1;
            orderCreate.CreatedBy = "user";
            await _orderRepository.CreateAsync(orderCreate);

            return orderCreate.ProductId;
        }
    }
}
