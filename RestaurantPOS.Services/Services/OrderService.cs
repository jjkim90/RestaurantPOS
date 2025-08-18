using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Core.DTOs;
using RestaurantPOS.Core.Entities;
using RestaurantPOS.Core.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RestaurantPOS.Services
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public OrderService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<OrderDTO> CreateOrderAsync(int tableId, OrderItemDTO[] orderItems)
        {
            var orderNumber = await GenerateOrderNumberAsync();
            
            var order = new Order
            {
                TableId = tableId,
                OrderNumber = orderNumber,
                OrderDate = DateTime.Now,
                TotalAmount = orderItems.Sum(x => x.SubTotal),
                Status = "Pending",
                IsPrinted = false,
                CreatedAt = DateTime.Now
            };

            // Order 생성
            await _unitOfWork.OrderRepository.AddAsync(order);
            await _unitOfWork.SaveChangesAsync();

            // OrderDetail 생성
            foreach (var item in orderItems)
            {
                var orderDetail = new OrderDetail
                {
                    OrderId = order.OrderId,
                    MenuItemId = item.MenuItemId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    SubTotal = item.SubTotal,
                    CreatedAt = DateTime.Now
                };
                await _unitOfWork.OrderDetailRepository.AddAsync(orderDetail);
            }
            
            await _unitOfWork.SaveChangesAsync();

            // 생성된 주문 조회하여 반환
            var createdOrder = await _unitOfWork.OrderRepository.Query()
                .Include(o => o.Table)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.MenuItem)
                .FirstOrDefaultAsync(o => o.OrderId == order.OrderId);

            return _mapper.Map<OrderDTO>(createdOrder);
        }

        public async Task<OrderDTO> GetActiveOrderByTableIdAsync(int tableId)
        {
            var order = await _unitOfWork.OrderRepository.Query()
                .Include(o => o.Table)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.MenuItem)
                .FirstOrDefaultAsync(o => o.TableId == tableId && (o.Status == "Pending" || o.Status == "InProgress"));

            return order != null ? _mapper.Map<OrderDTO>(order) : null;
        }

        public async Task UpdateOrderStatusAsync(int orderId, string status)
        {
            var order = await _unitOfWork.OrderRepository.GetByIdAsync(orderId);
            if (order != null)
            {
                order.Status = status;
                order.UpdatedAt = DateTime.Now;
                _unitOfWork.OrderRepository.Update(order);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        public async Task<OrderDTO> ProcessPaymentAsync(int orderId, string paymentMethod)
        {
            var order = await _unitOfWork.OrderRepository.GetByIdAsync(orderId);
            if (order == null)
                throw new InvalidOperationException("주문을 찾을 수 없습니다.");

            order.PaymentMethod = paymentMethod;
            order.PaymentDate = DateTime.Now;
            order.Status = "Completed";
            order.UpdatedAt = DateTime.Now;
            
            _unitOfWork.OrderRepository.Update(order);

            // 테이블 상태도 변경
            var table = await _unitOfWork.TableRepository.GetByIdAsync(order.TableId);
            if (table != null)
            {
                table.TableStatus = Core.Enums.TableStatus.Available;
                table.UpdatedAt = DateTime.Now;
                _unitOfWork.TableRepository.Update(table);
            }

            await _unitOfWork.SaveChangesAsync();

            // 업데이트된 주문 다시 조회
            var updatedOrder = await _unitOfWork.OrderRepository.Query()
                .Include(o => o.Table)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.MenuItem)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            return _mapper.Map<OrderDTO>(updatedOrder);
        }

        public async Task<string> GenerateOrderNumberAsync()
        {
            var today = DateTime.Now.ToString("yyyyMMdd");
            
            // 오늘 날짜의 마지막 주문 번호 조회
            var lastOrder = await _unitOfWork.OrderRepository
                .FindAsync(o => o.OrderNumber.StartsWith(today));
            
            var lastOrderNumber = lastOrder
                .OrderByDescending(o => o.OrderNumber)
                .FirstOrDefault()?.OrderNumber;

            int nextNumber = 1;
            if (!string.IsNullOrEmpty(lastOrderNumber))
            {
                var lastNumberPart = lastOrderNumber.Substring(9); // YYYYMMDD- 이후 부분
                if (int.TryParse(lastNumberPart, out int lastNum))
                {
                    nextNumber = lastNum + 1;
                }
            }

            return $"{today}-{nextNumber:D3}";
        }
    }
}