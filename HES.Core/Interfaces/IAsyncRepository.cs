﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace HES.Core.Interfaces
{
    public interface IAsyncRepository<T> where T : class
    {
        IQueryable<T> Query();
        IQueryable<T> SqlQuery(string sql);
        Task<T> GetByIdAsync(dynamic id);
        Task<T> AddAsync(T entity);
        Task<IList<T>> AddRangeAsync(IList<T> entity);
        Task UpdateAsync(T entity);
        Task UpdateOnlyPropAsync(T entity, string[] properties);
        Task UpdateOnlyPropAsync(IList<T> entity, string[] properties);
        Task DeleteAsync(T entity);
        Task DeleteRangeAsync(IList<T> entity);
        bool Exist(Expression<Func<T, bool>> predicate);
    }
}