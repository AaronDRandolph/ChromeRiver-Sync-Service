using AutoMapper;
using ChromeRiverService.Classes.DTOs;
using ChromeRiverService.Classes.DTOs.Subclasses;
using ChromeRiverService.Db.NciCommon.DbViewsModels;

namespace ChromeRiverService.Automapper
{
    public class EntityMappingProfile : Profile
    {
        public EntityMappingProfile()
        {
            //Configure the Mappings
            //Mapping DB View Entities to EntityDto
            //Source: VwChromeRiverGetAllEntity and Destination: EntityDto
            CreateMap<VwChromeRiverGetAllEntity, EntityDto>()
            .AddTransform<string>((str) => str.Trim())
            .AfterMap((src, dest) =>
            {
                dest.EntityNames = [new EntityName() { Name = src.EntityName, Locale = "en" }];
                dest.Status = src.Active == 1 ? "ACT" : "DEL";
            });

        }
    }
}