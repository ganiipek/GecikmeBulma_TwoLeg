using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GecikmeBulma.LoggerService
{
    internal class DatabaseLoggerService: ILoggerService
    {
        Database.DatabaseManager databaseManager = new Database.DatabaseManager();
        public void Send(LoggerType type, Trade.Arbitrage arbitrage, Trade.Pair pair, string callback, string message)
        {
            databaseManager.AddDebug(type.ToString(), arbitrage, pair, callback, message, DateTime.Now);
        }
    }
}
