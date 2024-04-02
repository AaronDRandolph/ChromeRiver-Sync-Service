using ChromeRiverService.Interfaces;
using IAMRepository.Models;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace ChromeRiverService.Classes.HelperClasses
{
    public class HttpHelper (ILogger<Worker> logger, IHttpClientFactory _httpClientFactory) : IHttpHelper
    {
        readonly ILogger<Worker> _logger = logger;
        readonly HttpClient _http = _httpClientFactory.CreateClient("ChromeRiver");
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);

        public async Task<HttpResponseMessage?> ExecutePost<T>(string endPoint, T dtoList) where T : class
        {
            string jsonBody = JsonSerializer.Serialize(dtoList,options);
            StringBuilder errorLog = new();

            try
            {
                string pipe = " | ";
                StringContent content = new(jsonBody, Encoding.UTF8, "application/json");
                HttpResponseMessage response =  await _http.PostAsync(endPoint, content);

                if (!response.IsSuccessStatusCode) 
                {
                    errorLog.Append($"Exception: Unsuccessful post with status code '{response.StatusCode}'")
                            .Append(pipe).Append($"Payload : ${jsonBody}");

                    throw new Exception(errorLog.ToString());
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Expection thrown while executing post");
                return null;
            }
        }
    }
}