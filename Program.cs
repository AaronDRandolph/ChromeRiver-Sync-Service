using ChromeRiverService.Db.Iam;
using ChromeRiverService.Interfaces;
using ChromeRiverService.UOW;
using Microsoft.EntityFrameworkCore;
using NLog.Extensions.Logging;
using ChromeRiverService.Classes.Helpers;
using ChromeRiverService.Db.NciCommon;

namespace ChromeRiverService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddHostedService<Worker>();

            //Logging
            builder.Logging.ClearProviders();
            builder.Logging.AddNLog("nlog.config");

            //IAM DB
            builder.Services.AddDbContext<IAMRepository.IamDatabaseContext>(options => {
                options.UseSqlServer(builder.Configuration["IAM_PROD_CONNECTION_STRING"]);
            }, ServiceLifetime.Transient, ServiceLifetime.Transient);

            //NCI Common DB
            builder.Services.AddDbContext<NciCommonContext>(options => {
                options.UseSqlServer(builder.Configuration["NCI_LOCAL_CONNECTION_STRING"]);   // this needs to be environment based
            }, ServiceLifetime.Transient, ServiceLifetime.Transient);

            // Transients 
            builder.Services.AddTransient<IIamUnitOfWork, IamUnitOfWork>();
            builder.Services.AddTransient<INciCommonUnitOfWork,NciCommonUnitOfWork>();
            builder.Services.AddTransient<IHttpHelper,HttpHelper>();
            builder.Services.AddTransient<ISynchUnitOfWork,SynchUnitOfWork>();


            //Client Factory
            builder.Services.AddHttpClient("ChromeRiver", httpClient =>
            {
                httpClient.BaseAddress = new Uri(builder.Configuration["CHROME_RIVER_BASE_URL_TEST"] ?? throw new Exception("CHROME_RIVER_BASE_URL_TEST was null")); // this needs to be environment based
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("x-api-key", builder.Configuration["CHROME_RIVER_API_KEY_TEST"] ?? throw new Exception("CHROME_RIVER_API_KEY_TEST was null")); // this needs to be environment based
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("customer-code", builder.Configuration["CHROME_RIVER_API_CUSTOMER_CODE"] ?? throw new Exception("CHROME_RIVER_API_CUSTOMER_CODE was null"));
            });

            //Automapper
            builder.Services.AddAutoMapper(typeof(Program).Assembly);

            var host = builder.Build();
            host.Run();
        }
    }
}
