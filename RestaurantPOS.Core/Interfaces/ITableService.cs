using RestaurantPOS.Core.DTOs;
using RestaurantPOS.Core.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RestaurantPOS.Core.Interfaces
{
    public interface ITableService
    {
        // Space Management
        Task<IEnumerable<SpaceDto>> GetAllSpacesAsync();
        Task<SpaceDto> GetSpaceByIdAsync(int spaceId);
        Task<SpaceDto> CreateSpaceAsync(CreateSpaceDto createSpaceDto);
        Task<SpaceDto> UpdateSpaceAsync(int spaceId, UpdateSpaceDto updateSpaceDto);
        Task<bool> DeleteSpaceAsync(int spaceId);
        Task<bool> CanDeleteSpaceAsync(int spaceId);

        // Table Management
        Task<IEnumerable<TableDto>> GetTablesBySpaceAsync(int spaceId);
        Task<TableDto> GetTableByIdAsync(int tableId);
        Task<TableDto> CreateTableAsync(CreateTableDto createTableDto);
        Task<TableDto> UpdateTableAsync(int tableId, UpdateTableDto updateTableDto);
        Task<bool> DeleteTableAsync(int tableId);
        Task<bool> UpdateTableStatusAsync(int tableId, TableStatus newStatus);
        
        // Table Status Management
        Task<bool> OccupyTableAsync(int tableId, int? orderId = null);
        Task<bool> ReleaseTableAsync(int tableId);
        Task<bool> ReserveTableAsync(int tableId, TableReservationDto reservationDto);
        Task<bool> SetTableCleaningAsync(int tableId);
        
        // Validation
        Task<bool> IsTableAvailableAsync(int tableId);
        Task<bool> IsSpaceNameUniqueAsync(string spaceName, int? excludeSpaceId = null);
        Task<bool> IsTableNameUniqueInSpaceAsync(string tableName, int spaceId, int? excludeTableId = null);
    }
}