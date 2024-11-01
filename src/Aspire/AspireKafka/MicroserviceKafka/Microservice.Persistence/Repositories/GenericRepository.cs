using Microservice.Application.Repositories;
using Microservice.Doamin.Common;
using Microservice.Persistence.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microservice.Persistence.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T>
        where T : BaseEntity
    {
        private readonly MicrosoftDatabaseContext _microsoftDatabaseContext;

        public GenericRepository(MicrosoftDatabaseContext microsoftDatabaseContext)
        {
            _microsoftDatabaseContext = microsoftDatabaseContext;
        }
        public async Task CreateAsync(T entity)
        {
            await _microsoftDatabaseContext.AddAsync(entity);
            await _microsoftDatabaseContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(T entity)
        {
            _microsoftDatabaseContext.Remove(entity);
            await _microsoftDatabaseContext.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<T>> GetAsync()
        {
            return await _microsoftDatabaseContext.Set<T>().AsNoTracking().ToListAsync();
        }

        public async Task<T> GetByIdAsync(int id)
        {
            return await _microsoftDatabaseContext.Set<T>()
                .AsNoTracking().FirstOrDefaultAsync(q => q.Id == id);
        }

        public async Task UpdateAsync(T entity)
        {
            _microsoftDatabaseContext.Entry(entity).State = EntityState.Modified;
            await _microsoftDatabaseContext.SaveChangesAsync();
        }
    }

}
