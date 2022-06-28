using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GecikmeBulma.LoggerService
{
    public enum LoggerType
    {
        ERROR = 0,
        WARNING = 1,
        INFO = 2,
        DEBUG = 3,
        SUCCESS = 4
    }

    internal interface ILoggerService
    {
        void Send(LoggerType type, Trade.Arbitrage arbitrage, Trade.Pair pair, string callback, string message);
    }
}
