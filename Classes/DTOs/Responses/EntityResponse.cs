using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChromeRiverService.Classes.DTOs.Responses
{
    internal class EntityResponse : Response
    {
        public string EntityCode { get; set; } = "";
        public string EntityTypeCode { get; set; } = "";
    }
}
