using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GecikmeBulma.Trade
{
    internal class PairManager
    {
        static List<Pair> pairs = new List<Pair>(); // .ConvertAll(x => (PairSettings)x)

        public int arbitrageId = 0;

        public void AddPair(Pair pair)
        {
            lock (pairs)
            {
                if (!pairs.Exists(local_pair => local_pair.Id == pair.Id))
                {
                    pairs.Add(pair);
                    UI.UIManager.dataGridView1_AddItem(pair);

                    string debug = String.Format("Yeni Pair eklendi! ({0}) {1}",
                            pair.Broker.Name,
                            pair.Symbol
                        );
                    TradeManager.SendLog(LoggerService.LoggerType.SUCCESS, new Arbitrage(), new Pair(), "PairManager-AddPair", debug);
                }
            }
        }

        public Pair GetPair(int id)
        {
            Pair pair = pairs.Find(local_pair => local_pair.Id == id);
            if (pair != null) return pair;

            try
            {
                pair = TradeManager.databaseManager.GetPair(id);
            }
            catch (RecordNotFoundException)
            {
                return null;
            }
            return pair;
        }

        public List<Pair> GetActivePairs(string symbol)
        {
            if (pairs.Count > 0) return pairs.FindAll(pair => 
                pair.Symbol == symbol && 
                pair.Active &&
                pair.Broker.AutoTrade
                );
            return pairs;
        }

        public Pair GetPair(string symbol, string brokerName)
        {
            if (pairs.Count > 0) return pairs.Find(pair => pair.Symbol == symbol && pair.Broker.Name == brokerName);
            return null;
        }

        public List<Pair> GetPairs(string symbol)
        {
            if (pairs.Count > 0) return pairs.FindAll(pair => pair.Symbol == symbol);
            return pairs;
        }

        public Pair GetPairInPairs(string symbol, string broker)
        {
            if(pairs.Exists(pair => pair.Symbol == symbol && pair.Broker.Name == broker))
            {
                return pairs.Find(pair =>
                    pair.Symbol == symbol &&
                    pair.Broker.Name == broker
                    );
            }

            return null;
        }

        public int GetPairId(Pair pair)
        {
            Pair oldPair = pairs.Find(local_pair => local_pair.Client == pair.Client);
            if(oldPair != null) return oldPair.Id;

            try
            {
                oldPair = TradeManager.databaseManager.GetPair(pair.Symbol, pair.Broker.Id);
            }
            catch (MySqlException)
            {
                return -1;
            }
            catch (RecordNotFoundException)
            {
                TradeManager.databaseManager.AddNewPair(pair);
                return pair.Id;
            }
             
            return oldPair.Id;
        }

        public void Register(TcpClient client, dynamic json_data)
        {
            Pair pair = new Pair()
            {
                Client = client,
                Symbol = (string) json_data.symbol,
                Broker = TradeManager.brokerManager.GetBroker((int) json_data.broker_id),
                ContractSize =json_data.cs,
                Digits = json_data.d,
                VolumeMin = json_data.vm,
                Volume = json_data.vm
            };
            pair.Id = GetPairId(pair);

            AddPair(pair);

            string request;

            try
            {
                request = String.Format("\"router\":\"{0}\",\"error\":{1},\"symbol_id\":\"{2}\"",
                    "register_symbol",
                    false,
                    pair.Id.ToString()
                );
            }
            //catch (MySqlException exception)
            //{
            //    request = String.Format("\"router\":\"{0}\",\"error\":{1},\"message\":\"{2}\"",
            //        "register_symbol",
            //        true,
            //        "Database error. Pair isn't registered. Please contact with 24 Capital IT Team!"
            //    );
            //}
            catch (Exception exception)
            {
                request = String.Format("\"router\":\"{0}\",\"error\":{1},\"message\":\"{2}\"",
                    "register_symbol",
                    true,
                    "Server error. Pair isn't registered. Please contact with 24 Capital IT Team!"
                );
            }

            TradeManager.tradeSocketManager.Send(client, request);
        }

        public void RemovePairInList(Pair pair)
        {
            pairs.Remove(pair);
            UI.UIManager.dataGridView1_RemoveItem(pair);
        }

        public void RemoveParities(TcpClient client)
        {
            Pair new_pair = pairs.Find(local_parity =>
                    local_parity.Client == client
                    );

            RemovePairInList(new_pair);
        }

    }
}
