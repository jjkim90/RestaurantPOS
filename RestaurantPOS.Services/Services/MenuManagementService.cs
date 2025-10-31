using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Core.DTOs;
using RestaurantPOS.Core.Entities;
using RestaurantPOS.Core.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RestaurantPOS.Services.Services
{
    public class MenuManagementService : IMenuManagementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMenuCacheService _menuCacheService;
        private readonly ILogger _logger;

        public MenuManagementService(
            IUnitOfWork unitOfWork, 
            IMapper mapper, 
            IMenuCacheService menuCacheService,
            ILogger logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _menuCacheService = menuCacheService;
            _logger = logger;
        }

        #region Category Methods

        public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
        {
            try
            {
                var categories = await _unitOfWork.CategoryRepository.Query()
                    .Include(c => c.MenuItems.Where(m => !m.IsDeleted))
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .ToListAsync();

                return _mapper.Map<IEnumerable<CategoryDto>>(categories);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error getting all categories");
                throw;
            }
        }

        public async Task<CategoryDto?> GetCategoryByIdAsync(int categoryId)
        {
            try
            {
                var category = await _unitOfWork.CategoryRepository.Query()
                    .Include(c => c.MenuItems.Where(m => !m.IsDeleted))
                    .FirstOrDefaultAsync(c => c.CategoryId == categoryId);

                return category != null ? _mapper.Map<CategoryDto>(category) : null;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error getting category by id: {CategoryId}", categoryId);
                throw;
            }
        }

        public async Task<CategoryDto> CreateCategoryAsync(CategoryDto categoryDto)
        {
            try
            {
                var category = _mapper.Map<Category>(categoryDto);
                category.CreatedAt = DateTime.Now;

                // DisplayOrder가 지정되지 않은 경우에만 자동 설정
                if (categoryDto.DisplayOrder == 0)
                {
                    var maxOrder = await _unitOfWork.CategoryRepository.Query()
                        .MaxAsync(c => (int?)c.DisplayOrder) ?? 0;
                    category.DisplayOrder = maxOrder + 1;
                }

                var createdCategory = await _unitOfWork.CategoryRepository.AddAsync(category);
                await _unitOfWork.SaveChangesAsync();

                _menuCacheService.InvalidateCache();
                _logger.Information("Category created: {CategoryName}", category.CategoryName);

                return _mapper.Map<CategoryDto>(createdCategory);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error creating category");
                throw;
            }
        }

        public async Task<CategoryDto> UpdateCategoryAsync(CategoryDto categoryDto)
        {
            try
            {
                var category = await _unitOfWork.CategoryRepository.GetByIdAsync(categoryDto.CategoryId);
                if (category == null)
                {
                    throw new InvalidOperationException($"Category with ID {categoryDto.CategoryId} not found");
                }

                category.CategoryName = categoryDto.CategoryName;
                category.DisplayOrder = categoryDto.DisplayOrder;
                category.IsActive = categoryDto.IsActive;

                _unitOfWork.CategoryRepository.Update(category);
                await _unitOfWork.SaveChangesAsync();

                _menuCacheService.InvalidateCache();
                _logger.Information("Category updated: {CategoryName}", category.CategoryName);

                return _mapper.Map<CategoryDto>(category);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error updating category");
                throw;
            }
        }

        public async Task<bool> DeleteCategoryAsync(int categoryId)
        {
            try
            {
                var category = await _unitOfWork.CategoryRepository.Query()
                    .Include(c => c.MenuItems.Where(m => !m.IsDeleted))
                    .FirstOrDefaultAsync(c => c.CategoryId == categoryId);

                if (category == null)
                {
                    return false;
                }

                // 활성 메뉴 아이템이 있는 경우 먼저 모든 메뉴를 소프트 딜리트
                if (category.MenuItems.Any())
                {
                    foreach (var menuItem in category.MenuItems)
                    {
                        menuItem.IsDeleted = true;
                        menuItem.DeletedAt = DateTime.Now;
                        _unitOfWork.MenuItemRepository.Update(menuItem);
                    }
                }

                // 카테고리 소프트 딜리트
                category.IsDeleted = true;
                category.DeletedAt = DateTime.Now;
                category.IsActive = false;
                _unitOfWork.CategoryRepository.Update(category);
                await _unitOfWork.SaveChangesAsync();

                _menuCacheService.InvalidateCache();
                _logger.Information("Category soft deleted: {CategoryName}", category.CategoryName);

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error deleting category");
                throw;
            }
        }

        #endregion

        #region MenuItem Methods

        public async Task<IEnumerable<MenuItemDto>> GetAllMenuItemsAsync()
        {
            try
            {
                var menuItems = await _unitOfWork.MenuItemRepository.Query()
                    .Include(m => m.Category)
                    .Where(m => m.IsAvailable)
                    .OrderBy(m => m.Category.DisplayOrder)
                    .ThenBy(m => m.ItemName)
                    .ToListAsync();

                return _mapper.Map<IEnumerable<MenuItemDto>>(menuItems);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error getting all menu items");
                throw;
            }
        }

        public async Task<IEnumerable<MenuItemDto>> GetMenuItemsByCategoryAsync(int categoryId)
        {
            try
            {
                var menuItems = await _unitOfWork.MenuItemRepository.Query()
                    .Include(m => m.Category)
                    .Where(m => m.CategoryId == categoryId && m.IsAvailable)
                    .OrderBy(m => m.ItemName)
                    .ToListAsync();

                return _mapper.Map<IEnumerable<MenuItemDto>>(menuItems);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error getting menu items by category: {CategoryId}", categoryId);
                throw;
            }
        }

        public async Task<MenuItemDto?> GetMenuItemByIdAsync(int menuItemId)
        {
            try
            {
                var menuItem = await _unitOfWork.MenuItemRepository.Query()
                    .Include(m => m.Category)
                    .FirstOrDefaultAsync(m => m.MenuItemId == menuItemId);

                return menuItem != null ? _mapper.Map<MenuItemDto>(menuItem) : null;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error getting menu item by id: {MenuItemId}", menuItemId);
                throw;
            }
        }

        public async Task<MenuItemDto> CreateMenuItemAsync(MenuItemDto menuItemDto)
        {
            try
            {
                var menuItem = _mapper.Map<MenuItem>(menuItemDto);
                menuItem.CreatedAt = DateTime.Now;

                var createdMenuItem = await _unitOfWork.MenuItemRepository.AddAsync(menuItem);
                await _unitOfWork.SaveChangesAsync();

                // Include로 Category 정보 다시 로드
                var menuItemWithCategory = await _unitOfWork.Repository<MenuItem>().Query()
                    .Include(m => m.Category)
                    .FirstAsync(m => m.MenuItemId == createdMenuItem.MenuItemId);

                _menuCacheService.InvalidateCategoryCache(menuItem.CategoryId);
                _logger.Information("Menu item created: {ItemName}", menuItem.ItemName);

                return _mapper.Map<MenuItemDto>(menuItemWithCategory);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error creating menu item");
                throw;
            }
        }

        public async Task<MenuItemDto> UpdateMenuItemAsync(MenuItemDto menuItemDto)
        {
            try
            {
                var menuItem = await _unitOfWork.MenuItemRepository.GetByIdAsync(menuItemDto.MenuItemId);
                if (menuItem == null)
                {
                    throw new InvalidOperationException($"Menu item with ID {menuItemDto.MenuItemId} not found");
                }

                menuItem.CategoryId = menuItemDto.CategoryId;
                menuItem.ItemName = menuItemDto.ItemName;
                menuItem.Price = menuItemDto.Price;
                menuItem.Description = menuItemDto.Description;
                menuItem.IsAvailable = menuItemDto.IsAvailable;
                menuItem.UpdatedAt = DateTime.Now;

                _unitOfWork.MenuItemRepository.Update(menuItem);
                await _unitOfWork.SaveChangesAsync();

                // Include로 Category 정보 다시 로드
                var menuItemWithCategory = await _unitOfWork.Repository<MenuItem>().Query()
                    .Include(m => m.Category)
                    .FirstAsync(m => m.MenuItemId == menuItem.MenuItemId);

                _menuCacheService.InvalidateCategoryCache(menuItem.CategoryId);
                _logger.Information("Menu item updated: {ItemName}", menuItem.ItemName);

                return _mapper.Map<MenuItemDto>(menuItemWithCategory);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error updating menu item");
                throw;
            }
        }

        public async Task<bool> DeleteMenuItemAsync(int menuItemId)
        {
            try
            {
                var menuItem = await _unitOfWork.MenuItemRepository.GetByIdAsync(menuItemId);

                if (menuItem == null)
                {
                    return false;
                }

                // 소프트 딜리트 적용
                var categoryId = menuItem.CategoryId;
                menuItem.IsDeleted = true;
                menuItem.DeletedAt = DateTime.Now;
                menuItem.IsAvailable = false;
                _unitOfWork.MenuItemRepository.Update(menuItem);

                await _unitOfWork.SaveChangesAsync();

                _menuCacheService.InvalidateCategoryCache(categoryId);
                _logger.Information("Menu item deleted/deactivated: {ItemName}", menuItem.ItemName);

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error deleting menu item");
                throw;
            }
        }

        #endregion
    }
}