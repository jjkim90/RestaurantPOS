using RestaurantPOS.Core.Entities;
using System;
using System.Threading.Tasks;

namespace RestaurantPOS.Core.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<T> Repository<T>() where T : class;
        IRepository<Space> SpaceRepository { get; }
        IRepository<Table> TableRepository { get; }
        IRepository<Category> CategoryRepository { get; }
        IRepository<MenuItem> MenuItemRepository { get; }
        IRepository<Order> OrderRepository { get; }
        IRepository<OrderDetail> OrderDetailRepository { get; }
        IRepository<PaymentTransaction> PaymentTransactionRepository { get; }
        
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        void ClearChangeTracker();
    }
}