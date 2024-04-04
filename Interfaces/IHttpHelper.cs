namespace ChromeRiverService.Interfaces
{
    public interface IHttpHelper
    {
        Task<HttpResponseMessage?> ExecutePostOrPatch<T>(string endPoint, T data, bool isPatch) where T : class;
    }
}
