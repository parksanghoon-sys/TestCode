﻿using Microservice.Doamin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ProductService.API.Configureations
{
    public class ProductModelConfiguration : IEntityTypeConfiguration<ProductModel>
    {
        public void Configure(EntityTypeBuilder<ProductModel> builder)
        {
            //builder.Property(p => p.Id)
            //    .IsRequired();

            //builder.Property(p => p.Name)
            //    .HasMaxLength(50);

            builder.HasData(new ProductModel { ProductId = 1,Id = 4, Name = "Shirt", Quantity = 100, Price = 20, CreatedBy = "Admin", DateCreated = DateTime.UtcNow });
            builder.HasData(new ProductModel { ProductId = 2,Id = 5, Name = "Pant", Quantity = 100, Price = 50, CreatedBy = "Admin", DateCreated = DateTime.UtcNow });
            builder.HasData(new ProductModel { ProductId = 3, Id = 6, Name = "Polo", Quantity = 100, Price = 100, CreatedBy = "Admin", DateCreated = DateTime.UtcNow });
        }
    }

}
