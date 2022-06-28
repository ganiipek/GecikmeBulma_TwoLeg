using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GecikmeBulma.MetaSocket;

namespace GecikmeBulma.Trade
{
    internal static class TradeManager
    {
        public static List<LoggerService.ILoggerService> loggerServices = new List<LoggerService.ILoggerService>();

        public static ArbitrageManager arbitrageManager = new ArbitrageManager();
        public static BrokerManager brokerManager = new BrokerManager();
        public static OrderManager orderManager = new OrderManager();
        public static PairManager pairManager = new PairManager();

        static public SocketManager tradeSocketManager = new SocketManager();
        static public SocketManager priceSocketManager = new SocketManager();
        static public SocketManager orderSocketManager = new SocketManager();
        static public ClientManager clientManager = new ClientManager();

        public static Database.DatabaseManager databaseManager = new Database.DatabaseManager();

        public static bool AddOrUpdatePairs(TcpClient client, dynamic json_data)
        {
            //GCHandle handle = GCHandle.Alloc(orderManager, GCHandleType.Normal);
            //int address = GCHandle.ToIntPtr(handle).ToInt32();
            //Console.WriteLine(Thread.CurrentThread.ManagedThreadId.ToString() + " -> " + address.ToString());

            Pair pair = pairManager.GetPair((int)json_data.s_id);

            pair.Ask = (double)json_data.ask;
            pair.Bid = (double)json_data.bid;
            pair.Spread = Convert.ToInt32(((double)json_data.ask - (double)json_data.bid) * Math.Pow(10, pair.Digits));
            pair.Time = UI.UIManager.UnixTimeStampToDateTime((ulong)json_data.time);

            //pair.Broker.Balance = (double)json_data.bal;
            //pair.Broker.Profit = (double)json_data.pro;
            //pair.Broker.Latency = (double)json_data.ms;

            if(pair.Broker.AutoTrade) arbitrageManager.FindArbitrage(pair);

            UI.UIManager.dataGridView1_UpdateItem(pair);
            //UI.UIManager.dataGridView2_UpdateItem(pair.Broker);

            return false;
        }

        public static async Task SendLog(LoggerService.LoggerType type, Arbitrage arbitrage, Pair pair, string callback, string message)
        {
            foreach (LoggerService.ILoggerService loggerService in loggerServices)
            {
                if(loggerService.GetType().Name == "DatabaseLoggerService")
                {
                    Thread thread = new Thread(() => loggerService.Send(type, arbitrage, pair, callback, message));
                    thread.Start();
                }
                else
                {
                    loggerService.Send(type, arbitrage, pair, callback, message);
                }
            }
        }
    }
}
