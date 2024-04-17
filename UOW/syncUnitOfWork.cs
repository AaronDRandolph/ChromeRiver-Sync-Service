using AutoMapper;
using ChromeRiverService.Classes;
using ChromeRiverService.Db.Iam;
using ChromeRiverService.Db.NciCommon;
using ChromeRiverService.Interfaces;

namespace ChromeRiverService.UOW
{
    public class SynchUnitOfWork (IIamUnitOfWork iamUnitOfWork, INciCommonUnitOfWork nciCommonUnitOfWork, IConfiguration configuration, ILogger<Worker> logger, IHttpHelper httpHelper, IMapper mapper ) : ISynchUnitOfWork
    {

        private readonly IIamUnitOfWork _iamUnitOfWork = iamUnitOfWork;
        private readonly INciCommonUnitOfWork _nciCommonUnitOfWork = nciCommonUnitOfWork;
        private readonly IConfiguration _configuration =  configuration;
        private readonly ILogger<Worker> _logger =  logger;
        private readonly IHttpHelper _httpHelper =  httpHelper;
        private readonly IMapper _mapper=  mapper;


        private IEntities? _entity {get; set;}
        private IPeople? _people {get; set;}
        private IAllocations? _allocations {get; set;}

        public IEntities Entities() 
        {
            _entity ??= new Entities(_nciCommonUnitOfWork, _configuration, _logger, _httpHelper, _mapper);
            return _entity;
        } 

        public IPeople People() 
        {
            _people ??= new People (_iamUnitOfWork, _nciCommonUnitOfWork, _configuration, _logger, _httpHelper, _mapper);
            return _people;
        } 

        public IAllocations Allocations() 
        {
            _allocations ??= new Allocations(_nciCommonUnitOfWork, _configuration, _logger, _httpHelper, _mapper);
            return _allocations;
        } 
    }
}