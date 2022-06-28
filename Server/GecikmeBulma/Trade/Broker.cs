using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GecikmeBulma.Trade
{
    public class Broker
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int PlatformId { get; set; }
        public double Latency { get; set; }
        public bool Connected { get; set; }
        public bool TerminalTradeAllowed { get; set; }
        public bool AccountTradeExpert { get; set; }
        public bool AccountTradeAllowed { get; set; }
        public double Balance { get; set; }
        public double Profit { get; set; }
        public bool AutoTrade { get; set; }

        public Broker()
        {
            Id = 0;
            Name = "";
            PlatformId = 1;
            Latency = 0.0;
            Connected = true;
            TerminalTradeAllowed = false;
            AccountTradeExpert = false;
            AccountTradeAllowed = false;
            AutoTrade = false;
        }

        public override string ToString()
        {
            return String.Format("Id: {0} | Name: {1} | PlatformId: {2} | Balance: {3} | TerminalTradeAllowed: {4} | AccountTradeExpert: {5} | AccountTradeAllowed: {6} | AutoTrade: {7}",
                Id.ToString(),
                Name,
                PlatformId.ToString(),
                Balance.ToString(),
                TerminalTradeAllowed.ToString(),
                AccountTradeExpert.ToString(),
                AccountTradeAllowed.ToString(),
                AutoTrade.ToString()
                );
        }
    }
}
