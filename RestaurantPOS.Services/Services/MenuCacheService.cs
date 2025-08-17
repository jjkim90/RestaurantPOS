using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using RestaurantPOS.Core.Entities;
using RestaurantPOS.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RestaurantPOS.Services
{
    public class MenuCacheService : IMenuCacheService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);
        
        private const string CATEGORIES_CACHE_KEY = "categories_all";
        private const string MENU_ITEMS_BY_CATEGORY_PREFIX = "menu_items_category_";

        public MenuCacheService(IServiceScopeFactory scopeFactory, IMemoryCache cache)
        {
            _scopeFactory = scopeFactory;
            _cache = cache;
        }

        public async Task<IEnumerable<Category>> GetCategoriesAsync()
        {
            return await _cache.GetOrCreateAsync(CATEGORIES_CACHE_KEY, async entry =>
            {
                entry.SetAbsoluteExpiration(_cacheExpiration);
                
                using (var scope = _scopeFactory.CreateScope())
                {
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    var categories = await unitOfWork.CategoryRepository.GetAllAsync();
                    return categories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToList();
                }
            });
        }

        public async Task<IEnumerable<MenuItem>> GetMenuItemsByCategoryAsync(int categoryId)
        {
            var cacheKey = $"{MENU_ITEMS_BY_CATEGORY_PREFIX}{categoryId}";
            System.Diagnostics.Debug.WriteLine($"MenuCacheService.GetMenuItemsByCategoryAsync called for categoryId: {categoryId}");
            
            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                System.Diagnostics.Debug.WriteLine($"Cache miss for key: {cacheKey}, loading from database");
                entry.SetAbsoluteExpiration(_cacheExpiration);
                
                using (var scope = _scopeFactory.CreateScope())
                {
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    var menuItems = await unitOfWork.MenuItemRepository.FindAsync(m => m.CategoryId == categoryId);
                    var availableItems = menuItems.Where(m => m.IsAvailable).ToList();
                    System.Diagnostics.Debug.WriteLine($"Found {availableItems.Count} available menu items for categoryId: {categoryId}");
                    return availableItems;
                }
            });
        }

        public void InvalidateCache()
        {
            // 모든 카테고리 캐시 제거
            _cache.Remove(CATEGORIES_CACHE_KEY);
            
            // 개별 카테고리의 메뉴 아이템 캐시는 필요할 때 제거
            // 실제로는 더 정교한 캐시 무효화 전략이 필요할 수 있음
        }
    }
}