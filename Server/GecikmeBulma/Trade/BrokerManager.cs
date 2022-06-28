using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GecikmeBulma.Trade
{
    internal class BrokerManager
    {
        private List<Broker> brokers = new List<Broker>();

        public void AddBroker(Broker broker)
        {
            lock (brokers)
            {
                if (brokers.Count != 0)
                {
                    if (!brokers.Exists(bro => bro.Name == broker.Name))
                    {
                        brokers.Add(broker);
                        UI.UIManager.dataGridView2_AddItem(broker);

                        string debug = String.Format("Yeni broker eklendi: {0}",
                            broker.Name
                        );
                        TradeManager.SendLog(LoggerService.LoggerType.SUCCESS, new Arbitrage(), new Pair(), "BrokerManager-AddBroker", debug);
                    }
                }
                else
                {
                    brokers.Add(broker);
                    UI.UIManager.dataGridView2_AddItem(broker);

                    string debug = String.Format("Yeni broker eklendi: {0}",
                            broker.Name
                        );
                    TradeManager.SendLog(LoggerService.LoggerType.SUCCESS, new Arbitrage(), new Pair(), "BrokerManager-AddBroker", debug);
                }
            }
        }

        public Broker GetBroker(string name, int platformId)
        {
            lock(brokers)
            {
                if (brokers.Count != 0)
                {
                    if (brokers.Exists(bro => bro.Name == name))
                    {
                        return brokers.Find(bro => bro.Name == name);
                    }
                }

                Broker broker;

                try
                {
                    broker = TradeManager.databaseManager.GetBroker(name, platformId);
                }
                catch (RecordNotFoundException ex)
                {
                    broker = new Broker()
                    {
                        Name = name,
                        PlatformId = platformId
                    };

                    TradeManager.databaseManager.AddNewBroker(broker);
                    AddBroker(broker);
                }

                AddBroker(broker);
                return broker;
            }
        }

        public Broker GetBroker(int broker_id)
        {
            lock (brokers)
            {
                if (brokers.Count != 0)
                {
                    if (brokers.Exists(bro => bro.Id == broker_id))
                    {
                        return brokers.Find(bro => bro.Id == broker_id);
                    }
                }
                return null;
            }
        }

        public void Register(TcpClient client, dynamic json_data)
        {
            string brokerName = (string) json_data.name;
            int platformId = (int) json_data.pid;

            Broker broker = TradeManager.brokerManager.GetBroker(brokerName, platformId);

            AddBroker(broker);

            string request;
            try
            {
                request = String.Format("\"router\":\"{0}\",\"error\":{1},\"broker_id\":\"{2}\"",
                    "register_broker",
                    false,
                    broker.Id.ToString()
                );
            }
            catch (MySqlException exception)
            {
                request = String.Format("\"router\":\"{0}\",\"error\":{1},\"message\":\"{2}\"",
                    "register_broker",
                    true,
                    "Database error. Broker isn't registered. Please contact with 24 Capital IT Team!"
                );
            }
            catch (Exception exception)
            {
                request = String.Format("\"router\":\"{0}\",\"error\":{1},\"message\":\"{2}\"",
                    "register_broker",
                    true,
                    "Server error. Broker isn't registered. Please contact with 24 Capital IT Team!"
                );
            }

            TradeManager.tradeSocketManager.Send(client, request);
        }
    }
}
