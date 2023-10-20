using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeStationWebApiDemo
{
    public class GroupOrder
    {
        public string Type { get; set; }
        public IEnumerable<Order> Orders { get; set; }
    }
}
