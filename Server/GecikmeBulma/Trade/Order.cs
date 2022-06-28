using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace GecikmeBulma.Trade
{
    public enum OrderType
    {
        OP_BUY = 0,
        OP_SELL = 1,
        OP_BUYLIMIT = 2,
        OP_SELLLIMIT = 3,
        OP_BUYSTOP = 4,
        OP_SELLSTOP = 5
    }

    public enum OrderProcess
    {
        ERROR = 0,
        NOT = 1,
        PREPARED = 2,
        SEND_OPEN = 3,
        IN_PROCESS = 4,
        SEND_CLOSE = 5,
        CLOSED = 6
    }

    public enum OrderError
    {
        NOT_ERROR = 0,
        ORDER_NOT_FOUND = 1,
        ORDER_NOT_CLOSED = 2
    }

    public class Order
    {
        public int Id { get; set; }
        public int Ticket { get; set; }
        public Pair Pair { get; set; }
        public Arbitrage Arbitrage { get; set; }
        public OrderType Type { get; set; }
        public OrderProcess Process { get; set; }
        public OrderError Error { get; set; }
        public DateTime SendedTime { get; set; }
        public double SendedPrice { get; set; }
        public DateTime OpenTime { get; set; }
        public double OpenPrice { get; set; }
        public DateTime ClosedTime { get; set; }
        public double ClosedPrice { get; set; }
        public double Volume { get; set; }
        public int Slippage { get; set; }
        public double Commission { get; set; }
        public double Swap { get; set; }
        public double Profit { get; set; }
        public int Step { get; set; }
        public DateTime LastControl { get; set; }

        public Order()
        {
            //Id = 0;
            //OpenPrice = 0;
            //Type = OrderType.OP_BUY;
            //Volume = 0;
            //Commission = 0;
            //Swap = 0;
            //Profit = 0;
            OpenTime = (DateTime)System.Data.SqlTypes.SqlDateTime.MinValue;
            ClosedTime = (DateTime)System.Data.SqlTypes.SqlDateTime.MinValue;
            ClosedPrice = 0;
            Process = OrderProcess.NOT;
            LastControl = DateTime.MinValue;
        }

        public override string ToString()
        {
            return String.Format("{0} - {1} --> ID: {2} | Type: {3} | Step: {13} | Open: ({4}) - {5} | Volume: {6} | Com.: {7} | Swap: {8} | Profit: {9} | Close: ({10}) - {11} | Process: {12}",
                Pair.Symbol,
                Pair.Broker.Name,
                Id.ToString(),
                Type.ToString(),
                OpenTime.ToString(),
                OpenPrice.ToString(),
                Volume.ToString(),
                Commission.ToString(),
                Swap.ToString(),
                Profit.ToString(),
                ClosedTime.ToString(),
                ClosedPrice.ToString(),
                Process.ToString(),
                Step.ToString()
            );
        }

        public string ToSummary()
        {
            return String.Format("Id: {0}, Ticket: {1}, Type: {2}, Process: {3}, Step: {4}",
                Id.ToString(),
                Ticket.ToString(),
                Type.ToString(),
                Process.ToString(),
                Step.ToString()
            );
        }

        public bool Save()
        {
            try
            {
                TradeManager.databaseManager.AddOrder(this);
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
                TradeManager.databaseManager.UpdateOrder(this);
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
            return this.Profit + this.Commission + this.Swap;
        }

        
    }
}
