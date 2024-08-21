using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cliProgressExample
{
    public class ApplicationDbContext : DbContext
    {
        private readonly string _connectionString = "User ID=prosgres;Password=123456;Server=localhost;Port=5432;Database=ticketdb";

        public DbSet<Ticket> Tickets { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if(optionsBuilder.IsConfigured == false)
            {                
                optionsBuilder.UseNpgsql(_connectionString);
            }
            
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Ticket>()
                .Property(t => t.Id)
                .ValueGeneratedOnAdd(); // Id가 자동으로 증가되도록 설정
        }
    }
}
