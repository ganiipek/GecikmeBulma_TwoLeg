using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GecikmeBulma.UI
{
    internal static class UIManager
    {
        public static Form1 _form1;

        public static bool thread = true;
        static Thread thread_socket_receive;
        //public static LoggerService.DatabaseLoggerService databaseLoggerService = new LoggerService.DatabaseLoggerService();

        static public void Initialize()
        {
            Trade.TradeManager.databaseManager.Initialize();
            Trade.TradeManager.databaseManager.Start();

            LoggerService.ConsoleLoggerService consoleLoggerService = new LoggerService.ConsoleLoggerService();
            LoggerService.DatabaseLoggerService databaseLoggerService = new LoggerService.DatabaseLoggerService();
            LoggerService.ActivityLoggerService activityLoggerService = new LoggerService.ActivityLoggerService();
            Trade.TradeManager.loggerServices.Add(consoleLoggerService);
            Trade.TradeManager.loggerServices.Add(databaseLoggerService);
            Trade.TradeManager.loggerServices.Add(activityLoggerService);

            Trade.TradeManager.tradeSocketManager.Initialize(IPAddress.Any, 6969, 512);
            new Thread(new ThreadStart(Trade.TradeManager.tradeSocketManager.Start)).Start();

            Thread.Sleep(1000);
            Trade.TradeManager.priceSocketManager.Initialize(IPAddress.Any, 3131, 512);
            new Thread(new ThreadStart(Trade.TradeManager.priceSocketManager.Start)).Start();

            Thread.Sleep(1000);
            Trade.TradeManager.orderSocketManager.Initialize(IPAddress.Any, 3169, 512);
            new Thread(new ThreadStart(Trade.TradeManager.orderSocketManager.Start)).Start();

            Trade.TradeManager.orderManager.ControllerStart();
            Trade.TradeManager.arbitrageManager.ControllerStart();
        }

        static public void ThreadStop()
        {
            thread_socket_receive.Abort();
        }

        static public DateTime UnixTimeStampToDateTime(ulong unixTimeStamp)
        {
            System.DateTime dtDateTime = new System.DateTime(1970, 1, 1, 0, 0, 0, 0);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp);
            return dtDateTime;
        }

        static public void dataGridView1_AddItem(Trade.Pair pair)
        {
            _form1.dataGridView1_AddItem(pair);
        }

        static public void dataGridView1_UpdateItem(Trade.Pair pair)
        {
            _form1.dataGridView1_UpdateItem(pair);
        }

        static public void dataGridView1_RemoveItem(Trade.Pair pair)
        {
            _form1.dataGridView1_RemoveItem(pair);
        }

        static public void dataGridView2_AddItem(Trade.Broker broker)
        {
            _form1.dataGridView2_AddItem(broker);
        }

        static public void dataGridView2_UpdateItem(Trade.Broker broker)
        {
            _form1.dataGridView2_UpdateItem(broker);
        }

        static public void dataGridView2_RemoveItem(Trade.Broker broker)
        {
            _form1.dataGridView2_RemoveItem(broker);
        }

        static public void dataGridView_Trades_AddItem(Trade.Arbitrage arbitrage)
        {
            _form1.dataGridView4_AddItem(arbitrage);
        }

        static public bool dataGridView_Trades_UpdateItem(Trade.Arbitrage arbitrage)
        {
            return _form1.dataGridView4_UpdateItem(arbitrage);
        }

        static public void dataGridView_Trades_RemoveItem(Trade.Arbitrage arbitrage)
        {
            _form1.dataGridView4_RemoveItem(arbitrage);
        }

        static public void listView3_AddItem(DateTime time, LoggerService.LoggerType loggerType, string log)
        {
            _form1.listView3_AddItem(time, loggerType, log);
        }

        static public void PairSetActive(string symbol, string broker, bool value)
        {
            Trade.Pair pair = Trade.TradeManager.pairManager.GetPair(symbol, broker);
            pair.Active = value;
        }

        static public double PairSetVolume(string symbol, string broker, double value)
        {
            Trade.Pair pair = Trade.TradeManager.pairManager.GetPair(symbol, broker);

            if(value < pair.VolumeMin) return pair.VolumeMin;

            pair.Volume = value;
            return pair.Volume;
        }

        static public void PairSetSlippage(string symbol, string broker, int value)
        {
            Trade.Pair pair = Trade.TradeManager.pairManager.GetPair(symbol, broker);
            pair.Slippage = value;
        }

        static public void PairSetMinDiff(string symbol, string broker, int value)
        {
            Trade.Pair pair = Trade.TradeManager.pairManager.GetPair(symbol, broker);
            pair.MinPipDiff = value;
        }

        static public void PairSetOffset(string symbol, string broker, int value)
        {
            Trade.Pair pair = Trade.TradeManager.pairManager.GetPair(symbol, broker);
            pair.Offset = value;

            Console.WriteLine(pair.ToString());
            Console.WriteLine(pair.Broker.ToString());
        }
        static public void PairSetTP(string symbol, string broker, double value)
        {
            Trade.Pair pair = Trade.TradeManager.pairManager.GetPair(symbol, broker);
            pair.TP = value;

            Console.WriteLine(pair.ToString());
            Console.WriteLine(pair.Broker.ToString());
        }

        static public void PairSetPyramiding(string symbol, string broker, int value)
        {
            Trade.Pair pair = Trade.TradeManager.pairManager.GetPair(symbol, broker);
            pair.Pyramiding = value;

            Console.WriteLine(pair.ToString());
            Console.WriteLine(pair.Broker.ToString());
        }

        static public void BrokerSetActive(string brokerName, int platformId, bool value)
        {
            Trade.Broker broker = Trade.TradeManager.brokerManager.GetBroker(brokerName, platformId);
            broker.AutoTrade = value;
        }

    }
}
