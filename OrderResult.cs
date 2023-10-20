using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeStationWebApiDemo
{
    public class OrderResult
    {
        public string Message { get; set; }
        public string OrderID { get; set; }
        public string OrderStatus { get; set; }
        public int StatusCode { get; set; }
    }
}
