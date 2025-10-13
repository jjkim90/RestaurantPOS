using AutoMapper;
using RestaurantPOS.Core.DTOs;
using RestaurantPOS.Core.Entities;

namespace RestaurantPOS.Services.Mappings
{
    public class MenuMappingProfile : Profile
    {
        public MenuMappingProfile()
        {
            // Category 매핑
            CreateMap<Category, CategoryDto>()
                .ForMember(dest => dest.MenuItemCount, opt => opt.MapFrom(src => src.MenuItems.Count));
            
            CreateMap<CategoryDto, Category>()
                .ForMember(dest => dest.MenuItems, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

            // MenuItem 매핑
            CreateMap<MenuItem, MenuItemDto>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.CategoryName));
            
            CreateMap<MenuItemDto, MenuItem>()
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.OrderDetails, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());
        }
    }
}