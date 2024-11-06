using Microservice.Doamin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace OrderService.API.Configureations
{
    public class OrderModelConfiguration : IEntityTypeConfiguration<OrderModel>
    {
        public void Configure(EntityTypeBuilder<OrderModel> builder)
        {
            builder.HasData(new OrderModel {Id =1, OrderId = 1, CustomerName = "Park", ProductId = 100, Quantity = 20, CreatedBy = "User", DateCreated = DateTime.UtcNow });
            builder.HasData(new OrderModel { Id = 2, OrderId = 2, CustomerName = "Sang", ProductId = 100, Quantity = 50 , CreatedBy = "User", DateCreated = DateTime.UtcNow });
            builder.HasData(new OrderModel { Id = 3, OrderId = 3, CustomerName = "Hoon", ProductId = 100, Quantity = 100 , CreatedBy = "User", DateCreated = DateTime.UtcNow });
        }
    }

}
