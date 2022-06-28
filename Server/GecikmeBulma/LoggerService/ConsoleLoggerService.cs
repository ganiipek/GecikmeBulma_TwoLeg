using GecikmeBulma.Trade;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GecikmeBulma.LoggerService
{
    internal class ConsoleLoggerService : ILoggerService
    {
        public void Send(LoggerType type, Arbitrage arbitrage, Pair pair, string callback, string message)
        {
            string debug = String.Format(Thread.CurrentThread.ManagedThreadId.ToString() + "-> {0} [{1}] [{2}-{3}] ({4}) {5}", 
                DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff"),
                type.ToString(),
                pair.Symbol.ToString(),
                pair.Broker.Name.ToString(),
                callback.ToString(),
                message.ToString()
                );

            Console.WriteLine(debug);
        }
    }
}
