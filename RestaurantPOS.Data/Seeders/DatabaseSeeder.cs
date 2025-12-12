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
                        TableNumber = int.Parse(tableName.Split('-')[1]),
                        TableStatus = Core.Enums.TableStatus.Available,
                        IsEditable = false,
                        CreatedAt = DateTime.Now
                    });
                }

                // 3. 기본 공간 생성 - 홀1만 생성
                var hall1Space = new Space
                {
                    SpaceName = "홀1",
                    IsSystem = false,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                _context.Spaces.Add(hall1Space);
                await _context.SaveChangesAsync();
                
                // 홀1에 기본 테이블 6개 생성
                for (int i = 1; i <= 6; i++)
                {
                    _context.Tables.Add(new Table
                    {
                        SpaceId = hall1Space.SpaceId,
                        TableName = $"테이블{i}",
                        TableNumber = i,
                        TableStatus = Core.Enums.TableStatus.Available,
                        IsEditable = true,
                        CreatedAt = DateTime.Now
                    });
                }
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

                // 5. 실제 한식당 메뉴 데이터 생성
                var mainDishesCategory = categories[0];
                var sideDishesCategory = categories[1];
                var beveragesCategory = categories[2];
                var dessertsCategory = categories[3];

                // 메인요리
                var mainDishes = new[]
                {
                    new MenuItem { CategoryId = mainDishesCategory.CategoryId, ItemName = "김치찌개", Price = 8000, Description = "얼큰하고 깊은 맛의 김치찌개", IsAvailable = true },
                    new MenuItem { CategoryId = mainDishesCategory.CategoryId, ItemName = "된장찌개", Price = 8000, Description = "구수한 된장과 신선한 야채", IsAvailable = true },
                    new MenuItem { CategoryId = mainDishesCategory.CategoryId, ItemName = "제육볶음", Price = 9000, Description = "매콤달콤한 돼지고기 볶음", IsAvailable = true },
                    new MenuItem { CategoryId = mainDishesCategory.CategoryId, ItemName = "불고기", Price = 12000, Description = "달콤한 간장 양념의 소불고기", IsAvailable = true },
                    new MenuItem { CategoryId = mainDishesCategory.CategoryId, ItemName = "갈비탕", Price = 13000, Description = "진한 육수의 소갈비탕", IsAvailable = true },
                    new MenuItem { CategoryId = mainDishesCategory.CategoryId, ItemName = "비빔밥", Price = 9000, Description = "고소한 참기름과 신선한 나물", IsAvailable = true },
                    new MenuItem { CategoryId = mainDishesCategory.CategoryId, ItemName = "냉면", Price = 10000, Description = "시원한 동치미 육수의 평양냉면", IsAvailable = true },
                    new MenuItem { CategoryId = mainDishesCategory.CategoryId, ItemName = "삼계탕", Price = 15000, Description = "영계와 인삼, 대추가 들어간 보양식", IsAvailable = true },
                    new MenuItem { CategoryId = mainDishesCategory.CategoryId, ItemName = "해물파전", Price = 14000, Description = "신선한 해물과 파가 듬뿍", IsAvailable = true },
                    new MenuItem { CategoryId = mainDishesCategory.CategoryId, ItemName = "순두부찌개", Price = 8000, Description = "부드러운 순두부와 매운 양념", IsAvailable = true }
                };

                // 사이드메뉴
                var sideDishes = new[]
                {
                    new MenuItem { CategoryId = sideDishesCategory.CategoryId, ItemName = "김치전", Price = 7000, Description = "바삭한 김치전", IsAvailable = true },
                    new MenuItem { CategoryId = sideDishesCategory.CategoryId, ItemName = "계란찜", Price = 6000, Description = "부드러운 계란찜", IsAvailable = true },
                    new MenuItem { CategoryId = sideDishesCategory.CategoryId, ItemName = "공기밥", Price = 1000, Description = "갓 지은 따뜻한 밥", IsAvailable = true },
                    new MenuItem { CategoryId = sideDishesCategory.CategoryId, ItemName = "김치", Price = 2000, Description = "직접 담근 배추김치", IsAvailable = true },
                    new MenuItem { CategoryId = sideDishesCategory.CategoryId, ItemName = "잡채", Price = 8000, Description = "고소한 참기름 향의 잡채", IsAvailable = true },
                    new MenuItem { CategoryId = sideDishesCategory.CategoryId, ItemName = "떡갈비", Price = 10000, Description = "달콤한 양념의 떡갈비", IsAvailable = true },
                    new MenuItem { CategoryId = sideDishesCategory.CategoryId, ItemName = "두부김치", Price = 9000, Description = "따뜻한 두부와 볶은 김치", IsAvailable = true },
                    new MenuItem { CategoryId = sideDishesCategory.CategoryId, ItemName = "감자전", Price = 6000, Description = "고소한 감자전", IsAvailable = true },
                    new MenuItem { CategoryId = sideDishesCategory.CategoryId, ItemName = "도토리묵", Price = 7000, Description = "양념장을 곁들인 도토리묵무침", IsAvailable = true },
                    new MenuItem { CategoryId = sideDishesCategory.CategoryId, ItemName = "멸치볶음", Price = 5000, Description = "달콤짭짤한 멸치볶음", IsAvailable = true }
                };

                // 음료
                var beverages = new[]
                {
                    new MenuItem { CategoryId = beveragesCategory.CategoryId, ItemName = "콜라", Price = 2000, Description = "시원한 콜라", IsAvailable = true },
                    new MenuItem { CategoryId = beveragesCategory.CategoryId, ItemName = "사이다", Price = 2000, Description = "청량한 사이다", IsAvailable = true },
                    new MenuItem { CategoryId = beveragesCategory.CategoryId, ItemName = "식혜", Price = 3000, Description = "전통 음료 식혜", IsAvailable = true },
                    new MenuItem { CategoryId = beveragesCategory.CategoryId, ItemName = "수정과", Price = 3000, Description = "계피향 가득한 수정과", IsAvailable = true },
                    new MenuItem { CategoryId = beveragesCategory.CategoryId, ItemName = "소주", Price = 5000, Description = "참이슬 소주", IsAvailable = true },
                    new MenuItem { CategoryId = beveragesCategory.CategoryId, ItemName = "맥주", Price = 4000, Description = "카스 생맥주", IsAvailable = true },
                    new MenuItem { CategoryId = beveragesCategory.CategoryId, ItemName = "막걸리", Price = 6000, Description = "생막걸리 750ml", IsAvailable = true },
                    new MenuItem { CategoryId = beveragesCategory.CategoryId, ItemName = "매실주", Price = 8000, Description = "달콤한 매실주", IsAvailable = true },
                    new MenuItem { CategoryId = beveragesCategory.CategoryId, ItemName = "오렌지주스", Price = 3000, Description = "100% 오렌지주스", IsAvailable = true },
                    new MenuItem { CategoryId = beveragesCategory.CategoryId, ItemName = "포도주스", Price = 3000, Description = "100% 포도주스", IsAvailable = true }
                };

                // 디저트
                var desserts = new[]
                {
                    new MenuItem { CategoryId = dessertsCategory.CategoryId, ItemName = "팥빙수", Price = 8000, Description = "고운 얼음과 달콤한 팥", IsAvailable = true },
                    new MenuItem { CategoryId = dessertsCategory.CategoryId, ItemName = "호떡", Price = 2000, Description = "달콤한 꿀이 들어간 호떡", IsAvailable = true },
                    new MenuItem { CategoryId = dessertsCategory.CategoryId, ItemName = "약과", Price = 3000, Description = "전통 한과 약과", IsAvailable = true },
                    new MenuItem { CategoryId = dessertsCategory.CategoryId, ItemName = "유과", Price = 3000, Description = "바삭한 유과", IsAvailable = true },
                    new MenuItem { CategoryId = dessertsCategory.CategoryId, ItemName = "떡", Price = 5000, Description = "모듬떡 세트", IsAvailable = true },
                    new MenuItem { CategoryId = dessertsCategory.CategoryId, ItemName = "수박화채", Price = 6000, Description = "시원한 수박화채", IsAvailable = true },
                    new MenuItem { CategoryId = dessertsCategory.CategoryId, ItemName = "찹쌀도너츠", Price = 3000, Description = "쫄깃한 찹쌀도너츠", IsAvailable = true },
                    new MenuItem { CategoryId = dessertsCategory.CategoryId, ItemName = "경단", Price = 4000, Description = "고소한 콩가루 경단", IsAvailable = true },
                    new MenuItem { CategoryId = dessertsCategory.CategoryId, ItemName = "인절미", Price = 4000, Description = "쫄깃한 인절미", IsAvailable = true },
                    new MenuItem { CategoryId = dessertsCategory.CategoryId, ItemName = "아이스크림", Price = 2000, Description = "바닐라 아이스크림", IsAvailable = true }
                };

                // 모든 메뉴 추가
                _context.MenuItems.AddRange(mainDishes);
                _context.MenuItems.AddRange(sideDishes);
                _context.MenuItems.AddRange(beverages);
                _context.MenuItems.AddRange(desserts);

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