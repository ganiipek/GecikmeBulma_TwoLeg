using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using GecikmeBulma.Trade;

namespace GecikmeBulma.MetaSocket
{
    internal class ClientManager
    {
        List<BaseClient> clients = new List<BaseClient>();

        public BaseClient Get(Pair pair, ClientType clientType)
        {
            lock(clients)
            {
                return clients.Find(_client => 
                    _client.Pair.Id == pair.Id &&
                    // _client.Account.Id == account.Id &&
                    //_client.MagicNumber == magicNumber &&
                    _client.Type == clientType
                );
            }
        }

        public BaseClient Get(TcpClient client)
        {
            lock(clients)
            {
                return clients.Find(_client => _client.Client == client);
            }
        }

        public void Add(BaseClient baseClient)
        {
            lock(clients)
            {
                BaseClient _baseClient = clients.Find(_client =>
                    // _client.Account == baseClient.Account &&
                    _client.Pair == baseClient.Pair &&
                    //_client.MagicNumber == baseClient.MagicNumber &&
                    _client.Type == baseClient.Type
                    );

                if(_baseClient == null)
                {
                    clients.Add(baseClient);

                    string debug = String.Format("ClientManager (Add): [{0}:{1}]",
                        ((IPEndPoint)baseClient.Client.Client.RemoteEndPoint).Address.ToString(),
                        ((IPEndPoint)baseClient.Client.Client.RemoteEndPoint).Port.ToString()
                        );
                   // Utils.SendLog(LoggerService.LoggerType.DEBUG, debug);
                }
                else
                {
                    _baseClient.Client = baseClient.Client;
                }
            }
        }

        public void Add(Pair pair, ClientType clientType, TcpClient client)
        {
            BaseClient baseClient = new BaseClient()
            {
                Pair = pair,
                Client = client,
                //MagicNumber = magicNumber,
                Type = clientType
            };

            Add(baseClient);
        }

        public void Remove(TcpClient client)
        {
            lock(clients)
            {
                BaseClient baseClient = Get(client);
                if(baseClient != null)
                {
                    clients.Remove(baseClient);

                    string debug = String.Format("ClientManager (remove): [{0}:{1}]",
                        ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString(),
                        ((IPEndPoint)client.Client.RemoteEndPoint).Port.ToString()
                        );
                    // Utils.SendLog(LoggerService.LoggerType.DEBUG, debug);
                }
            }
        }

        public bool RegisterSocket(int pairId, int magicNumber, ClientType clientType, TcpClient client)
        {
            //Account? account = BreakoutManager.accountManager.GetAccount(accountId);
            //if (account == null)
            //{
            //    string debug = String.Format("Failed to register socket. Reason: #{0} account not found.",
            //        accountId.ToString()
            //        );
            //    Utils.SendLog(LoggerService.LoggerType.ERROR, debug);

            //    string request = String.Format("\"router\":\"{0}\",\"register\":\"{1}\",\"reason\":\"Account not found.\"",
            //        "register_socket",
            //        false.ToString()
            //    );
            //    SendSocket(client, clientType, request);
            //    return false;
            //}

            Pair pair = TradeManager.pairManager.GetPair(pairId);
            if (pair == null)
            {
                string debug = String.Format("Failed to register socket. Reason: #{0} pair not found.",
                    pairId.ToString()
                    );
                //Utils.SendLog(LoggerService.LoggerType.ERROR, debug);

                string request = String.Format("\"router\":\"{0}\",\"register\":\"{1}\",\"reason\":\"Pair not found.\"",
                    "register_socket",
                    false.ToString()
                );
                SendSocket(client, clientType, request);
                return false;
            }

            Add(pair, clientType, client);

            string request2 = String.Format("\"router\":\"{0}\",\"register\":\"{1}\"",
                "register_socket",
                true.ToString()
                );
            SendSocket(client, clientType, request2);
            return true;
        }

        public bool RegisterSocket(TcpClient client, dynamic json_data)
        {
            //int accountId = (int)json_data.account_id;
            int pairId = (int)json_data.pair_id;
            int magicNumber = (int)json_data.magic_number;
            ClientType clientType = (ClientType)json_data.client_type;

            return RegisterSocket(pairId, magicNumber, clientType, client);
        }

        void SendSocket(TcpClient client, ClientType clientType, string data)
        {
            if(clientType == ClientType.TRADE)
            {
                TradeManager.tradeSocketManager.Send(client, data);
            }
            else if(clientType == ClientType.ORDER)
            {
                TradeManager.orderSocketManager.Send(client, data);
            }
            else if(clientType == ClientType.PRICE)
            {
                TradeManager.priceSocketManager.Send(client, data);
            }
        }
    }
}