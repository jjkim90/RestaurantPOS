using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Core.DTOs;
using RestaurantPOS.Core.Entities;
using RestaurantPOS.Core.Enums;
using RestaurantPOS.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RestaurantPOS.Services
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IPrintService _printService;

        public OrderService(IUnitOfWork unitOfWork, IMapper mapper, IPrintService printService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _printService = printService;
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
                .Where(o => o.TableId == tableId && (o.Status == "Pending" || o.Status == "InProgress"))
                .OrderByDescending(o => o.OrderDate)  // 가장 최근 주문을 가져오도록 정렬
                .FirstOrDefaultAsync();

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

        // 새로운 메서드 구현
        public async Task<OrderDTO> GetOrCreateActiveOrderAsync(int tableId)
        {
            // 기존 활성 주문 조회
            var activeOrder = await GetActiveOrderByTableIdAsync(tableId);
            if (activeOrder != null)
            {
                return activeOrder;
            }

            // 활성 주문이 없으면 새로 생성
            var orderNumber = await GenerateOrderNumberAsync();
            var order = new Order
            {
                TableId = tableId,
                OrderNumber = orderNumber,
                OrderDate = DateTime.Now,
                TotalAmount = 0,
                Status = "Pending",
                IsPrinted = false,
                CreatedAt = DateTime.Now
            };

            await _unitOfWork.OrderRepository.AddAsync(order);
            await _unitOfWork.SaveChangesAsync();

            // 생성된 주문을 다시 조회하여 반환
            return await GetOrderWithDetailsAsync(order.OrderId);
        }

        public async Task<OrderDTO> AddOrderItemsAsync(int orderId, IEnumerable<OrderItemDTO> orderItems)
        {
            var order = await _unitOfWork.OrderRepository.GetByIdAsync(orderId);
            if (order == null)
                throw new InvalidOperationException("주문을 찾을 수 없습니다.");

            foreach (var item in orderItems)
            {
                var orderDetail = new OrderDetail
                {
                    OrderId = orderId,
                    MenuItemId = item.MenuItemId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    SubTotal = item.SubTotal,
                    CreatedAt = DateTime.Now,
                    IsNewItem = true,  // 새로 추가되는 항목
                    Status = OrderDetailStatus.Pending  // 아직 주방에 전송되지 않음
                };
                await _unitOfWork.OrderDetailRepository.AddAsync(orderDetail);
            }

            // 주문 총액 업데이트
            var allDetails = await _unitOfWork.OrderDetailRepository
                .FindAsync(od => od.OrderId == orderId);
            order.TotalAmount = allDetails.Sum(od => od.SubTotal);
            order.UpdatedAt = DateTime.Now;
            _unitOfWork.OrderRepository.Update(order);

            await _unitOfWork.SaveChangesAsync();

            return await GetOrderWithDetailsAsync(orderId);
        }

        public async Task<OrderDTO> ConfirmPendingItemsAsync(int orderId)
        {
            // Pending 상태의 항목들을 먼저 조회 (주방 출력용)
            var pendingDetails = await _unitOfWork.OrderDetailRepository.Query()
                .Include(od => od.MenuItem)
                .Where(od => od.OrderId == orderId && od.Status == OrderDetailStatus.Pending)
                .ToListAsync();

            // Pending 항목들을 DTO로 변환 (주방 출력용)
            var pendingItemDTOs = _mapper.Map<List<OrderDetailDTO>>(pendingDetails);

            // 상태를 Confirmed로 변경
            foreach (var detail in pendingDetails)
            {
                detail.Status = OrderDetailStatus.Confirmed;
                detail.ConfirmedAt = DateTime.Now;
                detail.IsNewItem = false;  // 확정되면 더 이상 새 항목이 아님
                _unitOfWork.OrderDetailRepository.Update(detail);
            }

            // 주문의 IsPrinted 상태 업데이트 (주방 프린터로 출력되었다고 가정)
            var order = await _unitOfWork.OrderRepository.GetByIdAsync(orderId);
            if (order != null)
            {
                order.IsPrinted = true;
                order.UpdatedAt = DateTime.Now;
                
                // Order의 TotalAmount를 모든 OrderDetail의 합계로 재계산
                var allDetails = await _unitOfWork.OrderDetailRepository
                    .FindAsync(od => od.OrderId == orderId);
                order.TotalAmount = allDetails.Sum(od => od.SubTotal);
                
                _unitOfWork.OrderRepository.Update(order);
            }

            await _unitOfWork.SaveChangesAsync();

            // 주방 프린터로 출력 (pending 항목들만)
            if (pendingItemDTOs.Any())
            {
                var orderDTO = await GetOrderWithDetailsAsync(orderId);
                try
                {
                    await _printService.PrintKitchenOrderAsync(orderDTO, pendingItemDTOs);
                }
                catch (Exception ex)
                {
                    // 주방 프린터 출력 실패는 주문 처리를 막지 않음
                    System.Diagnostics.Debug.WriteLine($"주방 프린터 출력 실패: {ex.Message}");
                }
            }

            return await GetOrderWithDetailsAsync(orderId);
        }

        public async Task<OrderDTO> GetOrderWithDetailsAsync(int orderId)
        {
            var order = await _unitOfWork.OrderRepository.Query()
                .Include(o => o.Table)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.MenuItem)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            return order != null ? _mapper.Map<OrderDTO>(order) : null;
        }

        public async Task<IEnumerable<OrderDetailDTO>> GetPendingOrderDetailsAsync(int orderId)
        {
            var pendingDetails = await _unitOfWork.OrderDetailRepository.Query()
                .Include(od => od.MenuItem)
                .Where(od => od.OrderId == orderId && od.Status == OrderDetailStatus.Pending)
                .ToListAsync();

            return _mapper.Map<IEnumerable<OrderDetailDTO>>(pendingDetails);
        }

        public async Task<IEnumerable<OrderDetailDTO>> GetConfirmedOrderDetailsAsync(int orderId)
        {
            var confirmedDetails = await _unitOfWork.OrderDetailRepository.Query()
                .Include(od => od.MenuItem)
                .Where(od => od.OrderId == orderId && od.Status == OrderDetailStatus.Confirmed)
                .ToListAsync();

            return _mapper.Map<IEnumerable<OrderDetailDTO>>(confirmedDetails);
        }
    }
}