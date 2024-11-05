using Microservice.Application.Features.Order.Command.Create;
using Microservice.Doamin;
using AutoMapper;
using Microservice.Application.Features.Order.Queries.GetAllOrder;

namespace Microservice.Application.MappingProfiles
{
    public class OrderProfile : Profile
    {
        public OrderProfile()
        {
            CreateMap<CreateOrderCommand, OrderModel>();
            CreateMap<OrderDto, OrderModel>().ReverseMap();            
        }
    }

}
