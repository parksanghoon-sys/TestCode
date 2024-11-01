using Microservice.Doamin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Microservice.Persistence.Configureations
{
    public class ProductModelConfiguration : IEntityTypeConfiguration<ProductModel>
    {
        public void Configure(EntityTypeBuilder<ProductModel> builder)
        {
            builder.HasData(new ProductModel { Id = 1, Name = "Shirt", Quantity = 100, Price = 20 });
            builder.HasData(new ProductModel { Id = 2, Name = "Pant", Quantity = 100, Price = 50 });
            builder.HasData(new ProductModel { Id = 3, Name = "Polo", Quantity = 100, Price = 100 });
        }
    }

}
