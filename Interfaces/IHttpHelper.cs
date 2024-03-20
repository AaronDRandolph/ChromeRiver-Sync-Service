using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChromeRiverService.Interfaces
{
    public interface IHttpHelper
    {
        Task<HttpResponseMessage?> ExecutePost<T>(string endPoint, T dtoList) where T : class;
    }
}
