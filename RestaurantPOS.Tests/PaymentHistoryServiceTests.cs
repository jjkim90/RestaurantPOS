using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Core.DTOs;
using RestaurantPOS.Core.Entities;
using RestaurantPOS.Data.Context;
using RestaurantPOS.Data.Repositories;
using RestaurantPOS.Services.Services;
using RestaurantPOS.Services.Mappings;
using AutoMapper;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RestaurantPOS.Tests
{
    class PaymentHistoryServiceTests
    {
        static async Task Main(string[] args)
        {
            // In-Memory Database 설정
            var options = new DbContextOptionsBuilder<RestaurantContext>()
                .UseInMemoryDatabase(databaseName: "TestDB")
                .Options;

            using var context = new RestaurantContext(options);
            var unitOfWork = new UnitOfWork(context);
            
            // AutoMapper 설정
            var config = new MapperConfiguration(cfg => {
                cfg.AddProfile<MappingProfile>();
            });
            var mapper = config.CreateMapper();

            // 서비스 생성
            var paymentHistoryService = new PaymentHistoryService(unitOfWork, mapper);

            // 테스트 데이터 생성
            var space = new Space { SpaceId = 1, SpaceName = "테스트 홀", IsUserDefined = true };
            var table = new Table { TableId = 1, SpaceId = 1, TableName = "테이블1", TableNumber = 1 };
            var order = new Order 
            { 
                OrderId = 1, 
                TableId = 1, 
                OrderNumber = "20250101-001",
                OrderDate = DateTime.Now,
                TotalAmount = 50000,
                Status = "Completed",
                PaymentDate = DateTime.Now,
                Table = table
            };

            await unitOfWork.SpaceRepository.AddAsync(space);
            await unitOfWork.TableRepository.AddAsync(table);
            await unitOfWork.OrderRepository.AddAsync(order);

            // PaymentTransaction 추가
            var transaction1 = new PaymentTransaction
            {
                OrderId = 1,
                PaymentMethod = "Card",
                Amount = 30000,
                Status = "Completed",
                SyncStatus = "Synced",
                PaymentDate = DateTime.Now,
                CreatedAt = DateTime.Now
            };

            var transaction2 = new PaymentTransaction
            {
                OrderId = 1,
                PaymentMethod = "Cash",
                Amount = 20000,
                Status = "Completed",
                SyncStatus = "Failed", // 동기화 실패 건
                PaymentDate = DateTime.Now,
                CreatedAt = DateTime.Now
            };

            await unitOfWork.PaymentTransactionRepository.AddAsync(transaction1);
            await unitOfWork.PaymentTransactionRepository.AddAsync(transaction2);
            await unitOfWork.SaveChangesAsync();

            // 테스트 1: 결제 내역 조회
            Console.WriteLine("=== 결제 내역 조회 테스트 ===");
            var filter = new PaymentHistoryFilterDTO 
            { 
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(1)
            };
            
            var (items, totalCount) = await paymentHistoryService.GetPaymentHistoryAsync(filter);
            Console.WriteLine($"조회된 결제 내역: {totalCount}건");
            foreach (var item in items)
            {
                Console.WriteLine($"주문번호: {item.OrderNumber}, 총금액: {item.TotalAmount:N0}원");
                Console.WriteLine($"결제수단 요약: {item.PaymentMethodsSummary}");
                Console.WriteLine($"동기화 오류 여부: {item.HasSyncError}");
            }

            // 테스트 2: 동기화 실패 건 조회
            Console.WriteLine("\n=== 동기화 실패 건 조회 테스트 ===");
            var failedTransactions = await paymentHistoryService.GetFailedSyncTransactionsAsync();
            Console.WriteLine($"동기화 실패 건수: {failedTransactions.Count}");
            foreach (var failed in failedTransactions)
            {
                Console.WriteLine($"Transaction ID: {failed.PaymentTransactionId}, 금액: {failed.Amount:N0}원");
            }

            // 테스트 3: 통계 조회
            Console.WriteLine("\n=== 통계 조회 테스트 ===");
            var totalSales = await paymentHistoryService.GetTotalSalesAsync(filter);
            Console.WriteLine($"총 매출: {totalSales:N0}원");

            var salesByMethod = await paymentHistoryService.GetSalesByPaymentMethodAsync(filter);
            foreach (var kvp in salesByMethod)
            {
                Console.WriteLine($"{kvp.Key}: {kvp.Value:N0}원");
            }

            Console.WriteLine("\n테스트 완료!");
            Console.ReadKey();
        }
    }
}