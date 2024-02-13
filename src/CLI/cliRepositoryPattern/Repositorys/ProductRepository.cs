using cliRepositoryPattern.Interfaces;
using cliRepositoryPattern.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cliRepositoryPattern.Repositorys
{
    internal class ProductRepository : IRepository<Product>
    {
        private readonly DbContext _dbContext;
        public ProductRepository(DbContext context)
        {
            _dbContext = context;
        }

        public async Task DeleteAsync(Product entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }
            _dbContext.Set<Product>().Remove(entity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _dbContext.Set<Product>().ToListAsync();
        }

        public async Task<Product> GetByIdAsync(int id)
        {
            return await _dbContext.Set<Product>().FindAsync(id);
        }

        public async Task InsertAsync(Product entity)
        {

            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }
            await _dbContext.Set<Product>().AddAsync(entity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(Product entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }
            _dbContext.Set<Product>().Update(entity);
            await _dbContext.SaveChangesAsync();
        }
    }
}
