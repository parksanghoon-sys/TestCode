﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace cliProgressExample
{
    public interface IRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> GetByIdAsync(int id);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task AddAsync(T entity);
        Task RemoveAsync(T entity);
        Task UpdateAsync(T entity);
    }
}