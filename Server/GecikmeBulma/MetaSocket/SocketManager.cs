using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GecikmeBulma.Trade;

using Newtonsoft.Json;

namespace GecikmeBulma.MetaSocket
{
    internal class SocketManager
    {
        TcpListener client;
        MetaSocket metaSocket = new MetaSocket();

        public int connections = 0;

        public void Initialize(dynamic host, int port, int bufferSize)
        {
            metaSocket.Host = host;
            metaSocket.Port = port;
            metaSocket.BufferSize = bufferSize;
        }

        public void Start()
        {
            client = new TcpListener(metaSocket.Host, metaSocket.Port);
            client.Start();

            string debug = String.Format("Socket is starting! {0}:{1} is listening...",
                    metaSocket.Host.ToString(),
                    metaSocket.Port.ToString()
                    );
            System.Diagnostics.Debug.WriteLine(debug);

            while (UI.UIManager.thread)
            {
                while (!client.Pending())
                {
                    Thread.Sleep(1000);
                }
                MetaConnectionThread newconnection = new MetaConnectionThread();
                newconnection.threadListner = client;
                newconnection.buffer_size = metaSocket.BufferSize;
                Thread newthread = new Thread(new ThreadStart(newconnection.HandleConnection));
                newthread.Start();
            }
        }

        public void Send(TcpClient client, string data)
        {
            try
            {
                if (client != null)
                {
                    byte[] byteData = new byte[metaSocket.BufferSize];
                    byteData = Encoding.ASCII.GetBytes("{" + data + "}");
                    client.GetStream().Write(byteData, 0, byteData.Length);
                }
            }
            catch (Exception ex)
            {
                string debug = String.Format("SocketManager (Send) --> {0}",
                    ex.Message
                    );
                Console.WriteLine(debug);
            }
        }

        public void RemoveClient(TcpClient client)
        {
            Trade.TradeManager.pairManager.RemoveParities(client);
        }
    }

    internal class MetaConnectionThread
    {
        public TcpListener threadListner;
        public int buffer_size;
        public void HandleConnection()
        {
            int recv=31;
            byte[] data = new byte[buffer_size];
            TcpClient client = threadListner.AcceptTcpClient();
            client.NoDelay = true;

            NetworkStream ns = client.GetStream();
            
            while (UI.UIManager.thread)
            {
                data = new byte[buffer_size];

                try
                {
                    do
                    {
                        recv = ns.Read(data, 0, data.Length);
                    }
                    while (ns.DataAvailable);
                }
                catch (IOException ex) when (ex.HResult == -2146232800)
                {
                    string debug = String.Format("SocketManager (Receive) IOException --> [{0}:{1}] {2}",
                        ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString(),
                        ((IPEndPoint)client.Client.RemoteEndPoint).Port.ToString(),
                        ex.Message
                    );
                    Console.WriteLine(debug);

                    break;
                }
                catch (Exception ex)
                {
                    string debug = String.Format("SocketManager (Receive) IOException --> [{0}:{1}] {2}",
                        ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString(),
                        ((IPEndPoint)client.Client.RemoteEndPoint).Port.ToString(),
                        ex.Message
                    );
                    Console.WriteLine(debug);
                }

                if (recv == 0) break;

                string data_str = Encoding.ASCII.GetString(data).Split('}')[0] + "}";
                dynamic data_json = null;

                try
                {
                    data_json = JsonConvert.DeserializeObject(data_str);
                }
                catch (Exception ex)
                {
                    //Console.WriteLine("\n[ERROR] SocketReceive Json: " + ex.Message.ToString());
                    //Console.WriteLine(data_str);
                }

                if(data_json != null) newJSONData(client, data_json);
            }

            ns.Close();
            client.Close();
        }

        public static void newJSONData(TcpClient client, dynamic json_data)
        {
            if (Trade.TradeManager.databaseManager.IsConnected())
            {
                if (json_data.type == "ping")
                {
                    string request = String.Format("\"router\":\"{0}\",\"server_time\":\"{1}\"",
                        "pong",
                        DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff")
                        );

                    ClientType client_type = (ClientType)json_data.client_type;
                    if (client_type == ClientType.TRADE) TradeManager.tradeSocketManager.Send(client, request);
                    else if (client_type == ClientType.PRICE) TradeManager.priceSocketManager.Send(client, request);
                    else if (client_type == ClientType.ORDER) TradeManager.orderSocketManager.Send(client, request);
                }
                #region Trade Socket
                else if (json_data.type == "register_broker")
                {
                    TradeManager.brokerManager.Register(client, json_data);
                }
                else if (json_data.type == "register_account")
                {
                    //TradeManager.accountManager.Register(client, json_data);

                    string request = String.Format("\"router\":\"{0}\",\"error\":{1},\"account_id\":\"{2}\"",
                        "register_account",
                        false,
                        "1"
                    );

                    TradeManager.tradeSocketManager.Send(client, request);
                }
                else if (json_data.type == "register_symbol")
                {
                    TradeManager.pairManager.Register(client, json_data);
                }
                else if (json_data.type == "register_socket")
                {
                    TradeManager.clientManager.RegisterSocket(client, json_data);
                }
                #endregion
                else if (json_data.type == "update_tick")
                {
                    Trade.TradeManager.AddOrUpdatePairs(client, json_data);
                }

                #region Order Socket
                else if (json_data.type == "order_send")
                {
                    TradeManager.orderManager.SocketReceive_OrderSend(client, json_data);
                }
                else if (json_data.type == "order_register")
                {
                    TradeManager.orderManager.SocketReceive_OrderRegister(client, json_data);
                }
                else if (json_data.type == "order_info_ticket")
                {
                    TradeManager.orderManager.SocketReceive_OrderInfoByTicket(client, json_data);
                }
                else if (json_data.type == "order_info_id")
                {
                    TradeManager.orderManager.SocketReceive_OrderInfoByTicket(client, json_data);
                }
                else if (json_data.type == "order_tick_update")
                {
                    TradeManager.orderManager.SocketReceive_OrderTickUpdate(client, json_data);
                }
                else if (json_data.type == "arbitrage_profit_update")
                {
                    TradeManager.orderManager.SocketReceive_ArbitrageTickUpdate(client, json_data);
                }
                else if (json_data.type == "order_close")
                {
                    TradeManager.orderManager.SocketReceive_OrderClose(client, json_data);
                }
                #endregion
            }
        }
    }
}
