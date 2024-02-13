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
    internal class UserRepository : IRepository<User>
    {
        private readonly DbContext _dbContext;
        public UserRepository(DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task DeleteAsync(User entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }
            _dbContext.Set<User>().Remove(entity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _dbContext.Set<User>().ToListAsync();
        }

        public async Task<User> GetByIdAsync(int id)
        {
            return await _dbContext.Set<User>().FindAsync(id);
        }

        public async Task InsertAsync(User entity)
        {

            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }
            await _dbContext.Set<User>().AddAsync(entity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(User entity)
        {
            if(entity == null)
            {
                throw new ArgumentNullException("entity");
            }
            _dbContext.Set<User>().Update(entity);
            await _dbContext.SaveChangesAsync();
        }
    }
}
