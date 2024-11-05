using Microservice.Doamin;
using AutoMapper;
using ProductService.API.Features.Product.Command.Create;
using ProductService.API.Features.Product.Queries.GetAllProduct;

namespace ProductService.API.MappingProfiles
{
    public class ProductProfile :Profile
    {
        public ProductProfile()
        {
            CreateMap<CreateProductCommand, ProductModel>().ReverseMap();
            CreateMap<ProductDto, ProductModel>().ReverseMap();
        }
    }

}
