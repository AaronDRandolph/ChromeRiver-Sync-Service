using AutoMapper;
using ChromeRiverService.Classes.DTOs;
using ChromeRiverService.Db.NciCommon.DbViewsModels;

namespace ChromeRiverService.Automapper
{
    public class AllocationMappingProfile : Profile
    {
        public AllocationMappingProfile()
        {
            //Configure the Mappings
            //Mapping DB View Allocations to AllocationDto
            //Source: VwChromeRiverGetAllAllocation and Destination: AllocationDto
            CreateMap<VwChromeRiverGetAllAllocation, AllocationDto>()
                .AddTransform<string>((str) => str.Trim())
                .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => "USD"))
                .ForMember(dest => dest.Locale, opt => opt.MapFrom(src => "en"));
        }
    }
}