using DryIoc;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace RestaurantPOS.WPF.Infrastructure
{
    public class DryIocServiceScopeFactory : IServiceScopeFactory
    {
        private readonly IContainer _container;

        public DryIocServiceScopeFactory(IContainer container)
        {
            _container = container;
        }

        public IServiceScope CreateScope()
        {
            var scopedContainer = _container.OpenScope();
            return new DryIocServiceScope(scopedContainer);
        }
    }

    public class DryIocServiceScope : IServiceScope
    {
        private readonly IResolverContext _scopedContainer;

        public DryIocServiceScope(IResolverContext scopedContainer)
        {
            _scopedContainer = scopedContainer;
        }

        public IServiceProvider ServiceProvider => new DryIocServiceProvider(_scopedContainer);

        public void Dispose()
        {
            _scopedContainer.Dispose();
        }
    }

    public class DryIocServiceProvider : IServiceProvider
    {
        private readonly IResolverContext _resolverContext;

        public DryIocServiceProvider(IResolverContext resolverContext)
        {
            _resolverContext = resolverContext;
        }

        public object GetService(Type serviceType)
        {
            return _resolverContext.Resolve(serviceType, IfUnresolved.ReturnDefaultIfNotRegistered);
        }
    }
}