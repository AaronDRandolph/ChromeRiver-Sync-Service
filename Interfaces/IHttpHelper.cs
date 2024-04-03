namespace ChromeRiverService.Interfaces
{
    public interface IHttpHelper
    {
        Task<HttpResponseMessage?> ExecutePost<T>(string endPoint, T dtoList) where T : class;
    }
}
