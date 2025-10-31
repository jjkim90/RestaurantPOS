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
    public class TableService : ITableService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public TableService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        #region Space Management

        public async Task<IEnumerable<SpaceDto>> GetAllSpacesAsync()
        {
            var spaces = await _unitOfWork.SpaceRepository.Query()
                .AsNoTracking()
                .ToListAsync();
            var spaceDtos = new List<SpaceDto>();

            foreach (var space in spaces)
            {
                var spaceDto = _mapper.Map<SpaceDto>(space);
                var tables = await _unitOfWork.TableRepository.Query()
                    .AsNoTracking()
                    .Where(t => t.SpaceId == space.SpaceId)
                    .ToListAsync();
                spaceDto.TableCount = tables.Count();
                spaceDto.Tables = _mapper.Map<List<TableDto>>(tables);
                spaceDtos.Add(spaceDto);
            }

            return spaceDtos.OrderBy(s => s.IsSystem).ThenBy(s => s.SpaceName);
        }

        public async Task<SpaceDto> GetSpaceByIdAsync(int spaceId)
        {
            var space = await _unitOfWork.SpaceRepository.Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SpaceId == spaceId);
            if (space == null)
                throw new InvalidOperationException($"Space with ID {spaceId} not found.");

            var spaceDto = _mapper.Map<SpaceDto>(space);
            var tables = await _unitOfWork.TableRepository.Query()
                .AsNoTracking()
                .Where(t => t.SpaceId == spaceId)
                .ToListAsync();
            spaceDto.Tables = _mapper.Map<List<TableDto>>(tables);
            spaceDto.TableCount = tables.Count();

            return spaceDto;
        }

        public async Task<SpaceDto> CreateSpaceAsync(CreateSpaceDto createSpaceDto)
        {
            // 이름 중복 체크
            var isUnique = await IsSpaceNameUniqueAsync(createSpaceDto.SpaceName);
            if (!isUnique)
                throw new InvalidOperationException($"Space name '{createSpaceDto.SpaceName}' already exists.");

            var space = new Space
            {
                SpaceName = createSpaceDto.SpaceName,
                IsActive = createSpaceDto.IsActive,
                IsSystem = false,
                CreatedAt = DateTime.Now
            };

            await _unitOfWork.SpaceRepository.AddAsync(space);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<SpaceDto>(space);
        }

        public async Task<SpaceDto> UpdateSpaceAsync(int spaceId, UpdateSpaceDto updateSpaceDto)
        {
            try
            {
                // 먼저 ChangeTracker를 정리
                _unitOfWork.ClearChangeTracker();
                
                // AsNoTracking 없이 새로 조회하여 tracking 활성화
                var space = await _unitOfWork.SpaceRepository.Query()
                    .FirstOrDefaultAsync(s => s.SpaceId == spaceId);
                if (space == null)
                    throw new InvalidOperationException($"Space with ID {spaceId} not found.");

                if (space.IsSystem)
                    throw new InvalidOperationException("System spaces cannot be modified.");

                // 이름 중복 체크
                var isUnique = await IsSpaceNameUniqueAsync(updateSpaceDto.SpaceName, spaceId);
                if (!isUnique)
                    throw new InvalidOperationException($"Space name '{updateSpaceDto.SpaceName}' already exists.");

                space.SpaceName = updateSpaceDto.SpaceName;
                space.IsActive = updateSpaceDto.IsActive;
                // ModifiedAt 속성이 없으므로 주석 처리
                // space.ModifiedAt = DateTime.Now;

                _unitOfWork.SpaceRepository.Update(space);
                await _unitOfWork.SaveChangesAsync();

                return await GetSpaceByIdAsync(spaceId);
            }
            catch (Exception ex)
            {
                // 오류 발생 시 ChangeTracker 정리
                _unitOfWork.ClearChangeTracker();
                throw;
            }
        }

        public async Task<bool> DeleteSpaceAsync(int spaceId)
        {
            try
            {
                // 먼저 ChangeTracker를 정리
                _unitOfWork.ClearChangeTracker();
                
                // AsNoTracking 없이 새로 조회하여 tracking 활성화
                var space = await _unitOfWork.SpaceRepository.Query()
                    .FirstOrDefaultAsync(s => s.SpaceId == spaceId);
                if (space == null || space.IsDeleted)
                    return false;

                if (space.IsSystem)
                    throw new InvalidOperationException("System spaces cannot be deleted.");

                // 홀에 속한 활성 테이블들도 함께 soft delete
                var tables = await _unitOfWork.TableRepository.Query()
                    .Where(t => t.SpaceId == spaceId && !t.IsDeleted)
                    .ToListAsync();
                
                foreach (var table in tables)
                {
                    table.IsDeleted = true;
                    table.DeletedAt = DateTime.Now;
                    table.UpdatedAt = DateTime.Now;
                    _unitOfWork.TableRepository.Update(table);
                }

                // 홀 soft delete 처리
                space.IsDeleted = true;
                space.DeletedAt = DateTime.Now;
                
                _unitOfWork.SpaceRepository.Update(space);
                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                // 오류 발생 시 ChangeTracker 정리
                _unitOfWork.ClearChangeTracker();
                throw;
            }
        }

        public async Task<bool> CanDeleteSpaceAsync(int spaceId)
        {
            // 시스템 공간 체크만 수행 (테이블 존재 여부는 체크하지 않음)
            var space = await _unitOfWork.SpaceRepository.Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SpaceId == spaceId);
                
            return space != null && !space.IsSystem && !space.IsDeleted;
        }

        #endregion

        #region Table Management

        public async Task<IEnumerable<TableDto>> GetTablesBySpaceAsync(int spaceId)
        {
            var tables = await _unitOfWork.TableRepository.Query()
                .AsNoTracking()
                .Where(t => t.SpaceId == spaceId && !t.IsDeleted)
                .OrderBy(t => t.TableNumber)
                .ToListAsync();
            
            var tableDtos = new List<TableDto>();
            foreach (var table in tables)
            {
                var tableDto = _mapper.Map<TableDto>(table);
                
                // 현재 주문 정보 조회
                if (table.TableStatus == TableStatus.Occupied)
                {
                    var activeOrder = await _unitOfWork.OrderRepository.Query()
                        .AsNoTracking()
                        .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.MenuItem)
                        .Where(o => o.TableId == table.TableId && 
                                 (o.Status == "Pending" || 
                                  o.Status == "InProgress"))
                        .FirstOrDefaultAsync();
                    
                    if (activeOrder != null)
                    {
                        tableDto.CurrentOrderId = activeOrder.OrderId;
                        tableDto.CurrentOrderAmount = activeOrder.TotalAmount;
                        tableDto.OccupiedSince = activeOrder.OrderDate;
                        
                        // 주문 상세 정보 추가 (메뉴 미리보기용)
                        tableDto.CurrentOrderDetails = activeOrder.OrderDetails
                            .Select(od => new Core.DTOs.OrderDetailDto
                            {
                                MenuItemName = od.MenuItem?.ItemName ?? "삭제된 메뉴",
                                Quantity = od.Quantity,
                                UnitPrice = od.UnitPrice,
                                SubTotal = od.SubTotal
                            })
                            .ToList();
                    }
                }
                
                tableDtos.Add(tableDto);
            }
            
            return tableDtos;
        }

        public async Task<TableDto> GetTableByIdAsync(int tableId)
        {
            var table = await _unitOfWork.TableRepository.Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TableId == tableId);
            if (table == null)
                throw new InvalidOperationException($"Table with ID {tableId} not found.");

            var tableDto = _mapper.Map<TableDto>(table);
            
            // Space 정보 추가
            var space = await _unitOfWork.SpaceRepository.Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SpaceId == table.SpaceId);
            if (space != null)
                tableDto.SpaceName = space.SpaceName;

            return tableDto;
        }

        public async Task<TableDto> CreateTableAsync(CreateTableDto createTableDto)
        {
            // Space 존재 여부 확인
            var space = await _unitOfWork.SpaceRepository.GetByIdAsync(createTableDto.SpaceId);
            if (space == null)
                throw new InvalidOperationException($"Space with ID {createTableDto.SpaceId} not found.");

            // 테이블 이름 중복 체크 (삭제되지 않은 테이블 중에서)
            var isUnique = await IsTableNameUniqueInSpaceAsync(
                createTableDto.TableName, createTableDto.SpaceId);
            if (!isUnique)
                throw new InvalidOperationException(
                    $"Table name '{createTableDto.TableName}' already exists in this space.");

            // 테이블 번호 중복 체크
            var existingTable = await _unitOfWork.TableRepository.Query()
                .AsNoTracking()
                .AnyAsync(t => t.SpaceId == createTableDto.SpaceId && 
                              t.TableNumber == createTableDto.TableNumber && 
                              !t.IsDeleted);
            if (existingTable)
                throw new InvalidOperationException(
                    $"Table number '{createTableDto.TableNumber}' already exists in this space.");

            var table = new Table
            {
                SpaceId = createTableDto.SpaceId,
                TableName = createTableDto.TableName,
                TableNumber = createTableDto.TableNumber,
                TableStatus = createTableDto.TableStatus,
                IsEditable = createTableDto.IsEditable,
                CreatedAt = DateTime.Now
            };

            await _unitOfWork.TableRepository.AddAsync(table);
            await _unitOfWork.SaveChangesAsync();

            return await GetTableByIdAsync(table.TableId);
        }

        public async Task<TableDto> UpdateTableAsync(int tableId, UpdateTableDto updateTableDto)
        {
            // 먼저 ChangeTracker를 정리
            _unitOfWork.ClearChangeTracker();
            
            // AsNoTracking 없이 새로 조회하여 tracking 활성화
            var table = await _unitOfWork.TableRepository.Query()
                .FirstOrDefaultAsync(t => t.TableId == tableId);
            if (table == null)
                throw new InvalidOperationException($"Table with ID {tableId} not found.");

            if (!table.IsEditable)
                throw new InvalidOperationException("This table cannot be modified.");

            // 테이블 이름 중복 체크
            var isUnique = await IsTableNameUniqueInSpaceAsync(
                updateTableDto.TableName, table.SpaceId, tableId);
            if (!isUnique)
                throw new InvalidOperationException(
                    $"Table name '{updateTableDto.TableName}' already exists in this space.");

            // 테이블 번호가 변경된 경우 중복 체크
            if (table.TableNumber != updateTableDto.TableNumber)
            {
                var existingTable = await _unitOfWork.TableRepository.Query()
                    .AsNoTracking()
                    .AnyAsync(t => t.SpaceId == table.SpaceId && 
                                  t.TableNumber == updateTableDto.TableNumber && 
                                  t.TableId != tableId &&
                                  !t.IsDeleted);
                if (existingTable)
                    throw new InvalidOperationException(
                        $"Table number '{updateTableDto.TableNumber}' already exists in this space.");
            }

            table.TableName = updateTableDto.TableName;
            table.TableNumber = updateTableDto.TableNumber;
            table.TableStatus = updateTableDto.TableStatus;
            table.UpdatedAt = DateTime.Now;

            _unitOfWork.TableRepository.Update(table);
            await _unitOfWork.SaveChangesAsync();

            return await GetTableByIdAsync(tableId);
        }

        public async Task<bool> DeleteTableAsync(int tableId)
        {
            // 먼저 ChangeTracker를 정리
            _unitOfWork.ClearChangeTracker();
            
            // AsNoTracking 없이 새로 조회하여 tracking 활성화
            var table = await _unitOfWork.TableRepository.Query()
                .FirstOrDefaultAsync(t => t.TableId == tableId);
            if (table == null || table.IsDeleted)
                return false;

            if (!table.IsEditable)
                throw new InvalidOperationException("This table cannot be deleted.");

            // 진행 중인 주문이 있는지 확인
            var hasActiveOrders = await _unitOfWork.OrderRepository.Query()
                .AsNoTracking()
                .AnyAsync(o => o.TableId == tableId && 
                         (o.Status == "Pending" || 
                          o.Status == "InProgress"));
            
            if (hasActiveOrders)
                throw new InvalidOperationException("사용 중인 테이블은 삭제할 수 없습니다. 먼저 주문을 완료하거나 취소해주세요.");

            // 논리적 삭제 수행
            table.IsDeleted = true;
            table.DeletedAt = DateTime.Now;
            table.UpdatedAt = DateTime.Now;

            _unitOfWork.TableRepository.Update(table);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateTableStatusAsync(int tableId, TableStatus newStatus)
        {
            // AsNoTracking 없이 새로 조회하여 tracking 활성화
            var table = await _unitOfWork.TableRepository.Query()
                .FirstOrDefaultAsync(t => t.TableId == tableId);
            if (table == null)
                return false;

            table.TableStatus = newStatus;
            table.UpdatedAt = DateTime.Now;

            _unitOfWork.TableRepository.Update(table);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        #endregion

        #region Table Status Management

        public async Task<bool> OccupyTableAsync(int tableId, int? orderId = null)
        {
            // AsNoTracking 없이 새로 조회하여 tracking 활성화
            var table = await _unitOfWork.TableRepository.Query()
                .FirstOrDefaultAsync(t => t.TableId == tableId);
            if (table == null)
                return false;

            if (table.TableStatus != TableStatus.Available)
                throw new InvalidOperationException("Table is not available.");

            table.TableStatus = TableStatus.Occupied;
            table.UpdatedAt = DateTime.Now;

            _unitOfWork.TableRepository.Update(table);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ReleaseTableAsync(int tableId)
        {
            // AsNoTracking 없이 새로 조회하여 tracking 활성화
            var table = await _unitOfWork.TableRepository.Query()
                .FirstOrDefaultAsync(t => t.TableId == tableId);
            if (table == null)
                return false;

            table.TableStatus = TableStatus.Available;
            table.UpdatedAt = DateTime.Now;

            _unitOfWork.TableRepository.Update(table);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ReserveTableAsync(int tableId, TableReservationDto reservationDto)
        {
            // AsNoTracking 없이 새로 조회하여 tracking 활성화
            var table = await _unitOfWork.TableRepository.Query()
                .FirstOrDefaultAsync(t => t.TableId == tableId);
            if (table == null)
                return false;

            if (table.TableStatus != TableStatus.Available)
                throw new InvalidOperationException("Table is not available for reservation.");

            table.TableStatus = TableStatus.Reserved;
            table.UpdatedAt = DateTime.Now;

            // TODO: 예약 정보 저장 (Reservation 엔티티 필요)

            _unitOfWork.TableRepository.Update(table);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<bool> SetTableCleaningAsync(int tableId)
        {
            // AsNoTracking 없이 새로 조회하여 tracking 활성화
            var table = await _unitOfWork.TableRepository.Query()
                .FirstOrDefaultAsync(t => t.TableId == tableId);
            if (table == null)
                return false;

            table.TableStatus = TableStatus.Cleaning;
            table.UpdatedAt = DateTime.Now;

            _unitOfWork.TableRepository.Update(table);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        #endregion

        #region Validation

        public async Task<bool> IsTableAvailableAsync(int tableId)
        {
            return await _unitOfWork.TableRepository.Query()
                .AsNoTracking()
                .AnyAsync(t => t.TableId == tableId && t.TableStatus == TableStatus.Available);
        }

        public async Task<bool> IsSpaceNameUniqueAsync(string spaceName, int? excludeSpaceId = null)
        {
            var query = _unitOfWork.SpaceRepository.Query()
                .AsNoTracking()
                .Where(s => s.SpaceName == spaceName);
            
            if (excludeSpaceId.HasValue)
                query = query.Where(s => s.SpaceId != excludeSpaceId.Value);

            return !await query.AnyAsync();
        }

        public async Task<bool> IsTableNameUniqueInSpaceAsync(string tableName, int spaceId, int? excludeTableId = null)
        {
            var query = _unitOfWork.TableRepository.Query()
                .AsNoTracking()
                .Where(t => t.TableName == tableName && t.SpaceId == spaceId && !t.IsDeleted);
            
            if (excludeTableId.HasValue)
                query = query.Where(t => t.TableId != excludeTableId.Value);

            return !await query.AnyAsync();
        }

        #endregion
    }
}