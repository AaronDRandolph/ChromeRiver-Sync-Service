﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChromeRiverService.Classes
{
    internal class PersonResponse : Response
    {
        public string PersonUniqueId { get; set; } = "";   // return empty on Success
    }
}