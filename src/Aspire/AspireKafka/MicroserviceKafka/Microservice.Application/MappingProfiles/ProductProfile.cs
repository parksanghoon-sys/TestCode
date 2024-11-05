using Microservice.Doamin;
using AutoMapper;
using Microservice.Application.Features.Product.Command.Create;
using Microservice.Application.Features.Product.Queries.GetAllProduct;

namespace Microservice.Application.MappingProfiles
{
    public class ProductProfile :Profile
    {
        public ProductProfile()
        {
            CreateMap<CreateProductCommand, ProductModel>();
            CreateMap<ProductDto, ProductModel>().ReverseMap();
        }
    }

}
