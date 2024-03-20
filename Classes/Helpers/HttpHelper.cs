using ChromeRiverService.Interfaces;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace ChromeRiverService.Classes.HelperClasses
{
    public class HttpHelper (ILogger<Worker> logger, IHttpClientFactory _httpClientFactory) : IHttpHelper
    {
        ILogger<Worker> _logger = logger;
        HttpClient _http = _httpClientFactory.CreateClient("ChromeRiver");
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);

        public async Task<HttpResponseMessage?> ExecutePost<T>(string endPoint, T dtoList) where T : class
        {
            string jsonBody = JsonSerializer.Serialize(dtoList,options);

            try
            {        
                StringContent content = new(jsonBody, Encoding.UTF8, "application/json");
                HttpResponseMessage response =  await _http.PostAsync(endPoint, content);
                if (!response.IsSuccessStatusCode) 
                {
                    throw new Exception($"Unsuccesful post with status code '{response.StatusCode}' | ");
                }
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Excpection: {ex} | Payload : {jsonBody}");
                return null;
            }
        }
    }
}