using GecikmeBulma.Trade;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GecikmeBulma.LoggerService
{
    internal class ActivityLoggerService : ILoggerService
    {
        public void Send(LoggerType type, Arbitrage arbitrage, Pair pair, string callback, string message)
        {
            UI.UIManager.listView3_AddItem(DateTime.Now, type, message);
        }
    }
}
