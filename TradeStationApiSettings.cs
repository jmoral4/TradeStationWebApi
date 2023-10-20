using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeStationWebApiDemo
{
    public class TradeStationApiSettings
    {
        public string APIKey { get; set; }
        public string APISecret { get; set; }
        public string Environment { get; set; }
        public string RedirectUri { get; set; }
    }


}
