using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Core.Interfaces;
using RestaurantPOS.Data.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace RestaurantPOS.Data.Repositories
{
    public class GenericRepository<T> : IRepository<T> where T : class
    {
        protected readonly RestaurantContext _context;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(RestaurantContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public virtual async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<T?> GetByIdAsync(int id, bool tracking)
        {
            if (!tracking)
            {
                var keyName = _context.Model.FindEntityType(typeof(T))?.FindPrimaryKey()?.Properties[0].Name;
                if (keyName != null)
                {
                    return await _dbSet.AsNoTracking().FirstOrDefaultAsync(e => EF.Property<int>(e, keyName) == id);
                }
            }
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        public virtual IQueryable<T> Query()
        {
            return _dbSet.AsQueryable();
        }

        public virtual IQueryable<T> QueryAsNoTracking()
        {
            return _dbSet.AsNoTracking();
        }

        public virtual async Task<T> AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            return entity;
        }

        public virtual async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _dbSet.AddRangeAsync(entities);
        }

        public virtual void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        public virtual void Remove(T entity)
        {
            _dbSet.Remove(entity);
        }

        public virtual void RemoveRange(IEnumerable<T> entities)
        {
            _dbSet.RemoveRange(entities);
        }

        public virtual async Task<int> CountAsync()
        {
            return await _dbSet.CountAsync();
        }

        public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }
    }
}