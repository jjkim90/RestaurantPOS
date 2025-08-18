using AutoMapper;
using RestaurantPOS.Core.DTOs;
using RestaurantPOS.Core.Entities;

namespace RestaurantPOS.Services.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Space mappings
            CreateMap<Space, SpaceDto>();
            CreateMap<CreateSpaceDto, Space>();
            CreateMap<UpdateSpaceDto, Space>();

            // Table mappings
            CreateMap<Table, TableDto>()
                .ForMember(dest => dest.SpaceName, opt => opt.Ignore())
                .ForMember(dest => dest.CurrentOrderAmount, opt => opt.Ignore())
                .ForMember(dest => dest.CurrentOrderId, opt => opt.Ignore())
                .ForMember(dest => dest.OccupiedSince, opt => opt.Ignore());
            
            CreateMap<CreateTableDto, Table>();
            CreateMap<UpdateTableDto, Table>();

            // Order mappings
            CreateMap<Order, OrderDTO>()
                .ForMember(dest => dest.TableName, opt => opt.MapFrom(src => src.Table.TableName));
            CreateMap<OrderDetail, OrderDetailDTO>()
                .ForMember(dest => dest.MenuItemName, opt => opt.MapFrom(src => src.MenuItem.ItemName));

            // MenuItem mappings (나중에 추가 예정)
            // CreateMap<MenuItem, MenuItemDto>();
            // CreateMap<Category, CategoryDto>();
        }
    }
}