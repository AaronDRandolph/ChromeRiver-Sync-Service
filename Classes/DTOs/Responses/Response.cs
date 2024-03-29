using System.Text.Json.Serialization;

namespace ChromeRiverService.Classes
{
    public class Response
    {
        public string Result { get; set; } = "";

        public string ErrorMessage { get; set; } = "";
    }
}