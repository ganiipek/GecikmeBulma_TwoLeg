using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GecikmeBulma.Trade
{
    public class Pair
    {
        public int Id { get; set; }
        public TcpClient Client;
        public string Symbol;
        public Broker Broker;
        public double Bid;
        public double Ask;
        public int ContractSize;
        public int Digits;
        public int Spread;
        public double VolumeMin { get; set; }
        public DateTime Time;

        public bool Active { get; set; }
        public int MinPipDiff { get; set; }
        public double Volume { get; set; }
        public int Slippage { get; set; }
        public int Offset { get; set; }
        public double TP { get; set; }
        public int Pyramiding { get; set; }
        public int OffDay { get; set; }

        public Pair()
        {
            Id = 0;
            Symbol = "";
            Broker = new Broker();
            ContractSize = 0;
            Bid = 0;
            Ask = 0;
            Digits = 0;
            Spread = 0;
            Active = false;
            MinPipDiff = 0;
            Volume = 0.01;
            Slippage = 1;
            Offset = 0;

            TP = 20;
            Pyramiding = 6;
            OffDay = 0;
        }

        public override string ToString()
        {
            if(Broker == null)
            {
                return String.Format("ID: {0} | Symbol: {1} | Broker: {2} | Ask: {3} | Bid: {4} | Digits: {5} | Spread: {6} | Time: {7} | Active: {8} | MinPipDiff: {9} | Volume: {10} | Slippage: {11} | Offset: {12}",
                    Id.ToString(),
                    Symbol,
                    "Null",
                    Bid.ToString(),
                    Ask.ToString(),
                    Digits.ToString(),
                    Spread.ToString(),
                    Time.ToString(),
                    Active.ToString(),
                    MinPipDiff.ToString(),
                    Volume.ToString(),
                    Slippage.ToString(),
                    Offset.ToString()
                );
            }
            else
            {
                return String.Format("ID: {0} | Symbol: {1} | Broker: {2} | Ask: {3} | Bid: {4} | Digits: {5} | Spread: {6} | Time: {7} | Active: {8} | MinPipDiff: {9} | Volume: {10} | Slippage: {11} | Offset: {12}",
                    Id.ToString(),
                    Symbol,
                    Broker.Name,
                    Bid.ToString(),
                    Ask.ToString(),
                    Digits.ToString(),
                    Spread.ToString(),
                    Time.ToString(),
                    Active.ToString(),
                    MinPipDiff.ToString(),
                    Volume.ToString(),
                    Slippage.ToString(),
                    Offset.ToString()
                );
            }
            
        }
    }
}
