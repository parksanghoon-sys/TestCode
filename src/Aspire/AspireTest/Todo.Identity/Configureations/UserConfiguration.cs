﻿using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Todo.Domain;

namespace Todo.Identity.Configureations
{
    public class UserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            var hasher = new PasswordHasher<ApplicationUser>();
            builder.HasData(
                 new ApplicationUser
                 {
                     Id = "8e445865-a24d-4543-a6c6-9443d048cdb9",
                     Email = "admin@localhost.com",
                     NormalizedEmail = "ADMIN@LOCALHOST.COM",
                     FirstName = "System",
                     LastName = "Admin",
                     UserName = "admin@localhost.com",
                     NormalizedUserName = "ADMIN@LOCALHOST.COM",
                     PasswordHash = hasher.HashPassword(null, "123456"),
                     EmailConfirmed = true
                 },
                 new ApplicationUser
                 {
                     Id = "9e224968-33e4-4652-b7b7-8574d048cdb9",
                     Email = "user@localhost.com",
                     NormalizedEmail = "USER@LOCALHOST.COM",
                     FirstName = "System",
                     LastName = "User",
                     UserName = "user@localhost.com",
                     NormalizedUserName = "USER@LOCALHOST.COM",
                     PasswordHash = hasher.HashPassword(null, "123456"),
                     EmailConfirmed = true
                 },
                new ApplicationUser
                {
                    Id = "9e224968-33e4-4652-b7b7-8574d048cd10",
                    Email = "user2@localhost.com",
                    NormalizedEmail = "USER@LOCALHOST.COM",
                    FirstName = "System1",
                    LastName = "User",
                    UserName = "user2@localhost.com",
                    NormalizedUserName = "USER@2LOCALHOST.COM",
                    PasswordHash = hasher.HashPassword(null, "123456"),
                    EmailConfirmed = true
                },       
                new ApplicationUser
                {
                    Id = "9e224968-33e4-4652-b7b7-8574d048cd11",
                    Email = "user3@localhost.com",
                    NormalizedEmail = "USER@LOCALHOST.COM",
                    FirstName = "System2",
                    LastName = "User",
                    UserName = "user3@localhost.com",
                    NormalizedUserName = "USER23LOCALHOST.COM",
                    PasswordHash = hasher.HashPassword(null, "123456"),
                    EmailConfirmed = true
                }

            );
        }
    }
}
