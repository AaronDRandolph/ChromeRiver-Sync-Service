using ChromeRiverService.Interfaces;
using System.Text;
using System.Text.Json;

namespace ChromeRiverService.Classes.Helpers
{
    public class HttpHelper(ILogger<Worker> logger, IHttpClientFactory _httpClientFactory) : IHttpHelper
    {
        readonly ILogger<Worker> _logger = logger;
        readonly HttpClient _http = _httpClientFactory.CreateClient("ChromeRiver");
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);

        public async Task<HttpResponseMessage?> ExecutePostOrPatch<T>(string endPoint, T data, bool isPatch) where T : class
        {
            string jsonBody = JsonSerializer.Serialize(data, options);

            try
            {
                StringContent content = new(jsonBody, Encoding.UTF8, "application/json");
                HttpResponseMessage response =  isPatch ? await _http.PatchAsync(endPoint, content) : await _http.PostAsync(endPoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    ErrorsSummary.IncrementNumLowPriorityErrors();
                    string responseMessage = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Unsuccessful post with status code '{statusCode}' | Message: {message} | Payload : {jsonBody}",response.StatusCode, responseMessage, jsonBody);
                }

                return response;
            }
            catch (Exception ex)
            {
                ErrorsSummary.IncrementNumHighPriorityErrors();
                _logger.LogCritical(ex,"Expection thrown while executing batch post for {class} with payload : {payload}", data.GetType().Name, jsonBody);
                return null;
            }
        }
    }
}