using Bogus;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Core.Entities;
using RestaurantPOS.Data.Context;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RestaurantPOS.Data.Seeders
{
    public class DatabaseSeeder
    {
        private readonly RestaurantContext _context;

        public DatabaseSeeder(RestaurantContext context)
        {
            _context = context;
        }

        public async Task SeedAsync()
        {
            // 트랜잭션으로 실행
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 이미 데이터가 있으면 스킵
                if (await _context.Spaces.AnyAsync())
                {
                    return;
                }

                // 1. 고정 공간 생성
                var systemSpace = new Space
                {
                    SpaceName = "포장/배달/대기",
                    IsSystem = true,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                _context.Spaces.Add(systemSpace);
                await _context.SaveChangesAsync();

                // 2. 고정 테이블 생성
                var fixedTables = new[]
                {
                    "포장-1", "포장-2", "포장-3", "포장-4", "포장-5",
                    "배달-1", "배달-2", "배달-3", "배달-4", "배달-5",
                    "대기-1", "대기-2", "대기-3", "대기-4", "대기-5"
                };

                foreach (var tableName in fixedTables)
                {
                    _context.Tables.Add(new Table
                    {
                        SpaceId = systemSpace.SpaceId,
                        TableName = tableName,
                        Status = "Available",
                        IsEditable = false,
                        CreatedAt = DateTime.Now
                    });
                }

                // 3. 기본 공간 생성
                var defaultSpace = new Space
                {
                    SpaceName = "1층 홀",
                    IsSystem = false,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                _context.Spaces.Add(defaultSpace);
                await _context.SaveChangesAsync();

                // 4. 카테고리 생성
                var categories = new[]
                {
                    new Category { CategoryName = "메인요리", DisplayOrder = 1, IsActive = true },
                    new Category { CategoryName = "사이드메뉴", DisplayOrder = 2, IsActive = true },
                    new Category { CategoryName = "음료", DisplayOrder = 3, IsActive = true },
                    new Category { CategoryName = "디저트", DisplayOrder = 4, IsActive = true }
                };
                _context.Categories.AddRange(categories);
                await _context.SaveChangesAsync();

                // 5. Bogus로 메뉴 데이터 생성
                var menuFaker = new Faker<MenuItem>("ko")
                    .RuleFor(m => m.ItemName, f => f.Commerce.ProductName())
                    .RuleFor(m => m.Price, f => Math.Round(f.Random.Decimal(5000, 50000) / 1000) * 1000)
                    .RuleFor(m => m.Description, f => f.Lorem.Sentence())
                    .RuleFor(m => m.IsAvailable, f => f.Random.Bool(0.9f))
                    .RuleFor(m => m.CreatedAt, f => f.Date.Past(1));

                // 카테고리별로 메뉴 생성
                foreach (var category in categories)
                {
                    var menuItems = menuFaker
                        .RuleFor(m => m.CategoryId, f => category.CategoryId)
                        .Generate(10);
                    
                    _context.MenuItems.AddRange(menuItems);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}