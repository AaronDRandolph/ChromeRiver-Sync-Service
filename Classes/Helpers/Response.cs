using System.Text.Json.Serialization;

namespace ChromeRiverService.Classes.HelperClasses
{
    public class Response 
    {
            [JsonPropertyName("result")]
            public string Result { get; set; } = "";

            [JsonPropertyName("errorMessage")]
            public string ErrorMessage { get; set; } = "";
    }
}