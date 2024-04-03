using System.Text.Json.Serialization;

namespace ChromeRiverService.Classes.DTOs.Responses
{
    public class Response
    {
        public string Result { get; set; } = "";

        public string ErrorMessage { get; set; } = "";
    }
}