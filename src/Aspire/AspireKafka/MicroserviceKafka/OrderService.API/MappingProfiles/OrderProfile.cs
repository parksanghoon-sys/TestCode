using Microservice.Doamin;
using AutoMapper;
using OrderService.API.Features.Order.Command.Create;
using OrderService.API.Features.Order.Queries.GetAllOrder;

namespace OrderService.API.MappingProfiles
{
    public class OrderProfile : Profile
    {
        public OrderProfile()
        {
            CreateMap<CreateOrderCommand, OrderModel>().ReverseMap();
            CreateMap<OrderDto, OrderModel>().ReverseMap();            
        }
    }

}
