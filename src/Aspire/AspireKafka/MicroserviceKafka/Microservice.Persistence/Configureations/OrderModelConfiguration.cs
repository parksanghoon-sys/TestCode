using Microservice.Doamin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Microservice.Persistence.Configureations
{
    public class OrderModelConfiguration : IEntityTypeConfiguration<OrderModel>
    {
        public void Configure(EntityTypeBuilder<OrderModel> builder)
        {
            builder.HasData(new OrderModel { Id = 1, CustomerName = "Shirt", ProductId = 100, Quantity = 20 });
            builder.HasData(new OrderModel { Id = 2, CustomerName = "Pant", ProductId = 100, Quantity = 50 });
            builder.HasData(new OrderModel { Id = 3, CustomerName = "Polo", ProductId = 100, Quantity = 100 });
        }
    }

}
