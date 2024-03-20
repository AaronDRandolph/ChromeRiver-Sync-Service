using ChromeRiverService;
using ChromeRiverService.Classes;
using ChromeRiverService.Classes.HelperClasses;
using ChromeRiverService.Db.Iam;
using ChromeRiverService.Interfaces;
using IamSyncService.Db.NciCommon;
using Microsoft.EntityFrameworkCore;
using NLog.Extensions.Logging;

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

            // Singletons
            builder.Services.AddTransient<IIamUnitOfWork, IamUnitOfWork>();
            builder.Services.AddTransient<INciCommonUnitOfWork,NciCommonUnitOfWork>();
            builder.Services.AddTransient<IEntities,Entities>();
            builder.Services.AddTransient<IPeople,People>();
            builder.Services.AddTransient<IAllocations,Allocations>();
            builder.Services.AddTransient<IHttpHelper,HttpHelper>();

            //Client Factory
            builder.Services.AddHttpClient("ChromeRiver", httpClient =>
            {
                httpClient.BaseAddress = new Uri(builder.Configuration["CHROME_RIVER_BASE_URL_TEST"] ?? throw new Exception("Chrome River Base URL was null")); // this needs to be environment based
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("x-api-key", builder.Configuration["CHROME_RIVER_API_KEY_TEST"]); // this needs to be environment based
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("customer-code", builder.Configuration["CHROME_RIVER_API_CUSTOMER_CODE"]);
            });

            var host = builder.Build();
            host.Run();
        }
    }
}
