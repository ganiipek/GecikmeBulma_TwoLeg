using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace GecikmeBulma.Trade
{
    public enum ArbitrageProcess
    {
        ERROR = 0,
        PREPARED = 1,
        SEND_OPEN = 2,
        IN_PROCESS = 3,
        SEND_CLOSE = 4,
        CLOSED = 5
    }

    public class Arbitrage
    {
        public int Id { get; set; }
        public DateTime Created { get; set; }
        public ArbitrageProcess Process { get; set; }
        public Pair AskPair { get; set; }
        public Pair BidPair { get; set; }
        public int CurrentPyramid { get; set; }
        public int MaxPyramid { get; set; }
        public double TargetProfit { get; set; }
        public List<Order> AskOrders { get; set; }
        public List<Order> BidOrders { get; set; }
        public DateTime LastError { get; set; }
        public bool ClosedProcess { get; set; }
        public Arbitrage()
        {
            AskOrders = new List<Order>();
            BidOrders = new List<Order>();
            Process = ArbitrageProcess.PREPARED;
            CurrentPyramid = 1;
            MaxPyramid = 5;
            TargetProfit = 20;
            ClosedProcess = false;
        }

        public override string ToString()
        {
            return String.Format("Id: {0}, Process: {7} | askBroker:[{1} -> {2}] | bidBroker:[{3} -> {4}] | askOrders: ({5}) | bidOrders: ({6})",
                Id.ToString(),
                AskPair.Broker.Id.ToString(),
                AskPair.Broker.Name,
                BidPair.Broker.Id.ToString(),
                BidPair.Broker.Name,
                "", // AskOrder.Id.ToString(),
                "", // BidOrder.Id.ToString()
                Process.ToString()
                );
        }

        public bool Save()
        {
            try
            {
                TradeManager.databaseManager.AddArbitrage(this);
            }
            catch (MySqlException exception)
            {
                return false;
            }
            catch (Exception exception)
            {
                return false;
            }
            return true;
        }

        public bool Update()
        {
            try
            {
                TradeManager.databaseManager.UpdateArbitrage(this);
            }
            catch (MySqlException exception)
            {
                return false;
            }
            catch (Exception exception)
            {
                return false;
            }
            return true;
        }

        public double GetProfit()
        {
            return Math.Round(AskOrders.Sum(_order => _order.GetProfit()) + BidOrders.Sum(_order => _order.GetProfit()), 2);
        }

        public double GetTotalLongVolume()
        {
            return Math.Round(AskOrders.Sum(_order => _order.Volume), 2);
        }

        public double GetTotalShortVolume()
        {
            return Math.Round(BidOrders.Sum(_order => _order.Volume), 2);
        }
    }
}
