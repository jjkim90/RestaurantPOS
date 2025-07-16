using Microsoft.EntityFrameworkCore.Storage;
using RestaurantPOS.Core.Entities;
using RestaurantPOS.Core.Interfaces;
using RestaurantPOS.Data.Context;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RestaurantPOS.Data.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly RestaurantContext _context;
        private readonly Dictionary<Type, object> _repositories;
        private IDbContextTransaction? _transaction;

        public UnitOfWork(RestaurantContext context)
        {
            _context = context;
            _repositories = new Dictionary<Type, object>();
        }

        public IRepository<Space> SpaceRepository => Repository<Space>();
        public IRepository<Table> TableRepository => Repository<Table>();
        public IRepository<Category> CategoryRepository => Repository<Category>();
        public IRepository<MenuItem> MenuItemRepository => Repository<MenuItem>();
        public IRepository<Order> OrderRepository => Repository<Order>();
        public IRepository<OrderDetail> OrderDetailRepository => Repository<OrderDetail>();

        public IRepository<T> Repository<T>() where T : class
        {
            var type = typeof(T);
            
            if (!_repositories.ContainsKey(type))
            {
                var repositoryInstance = new GenericRepository<T>(_context);
                _repositories.Add(type, repositoryInstance);
            }

            return (IRepository<T>)_repositories[type];
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}