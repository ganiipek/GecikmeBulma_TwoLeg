using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GecikmeBulma.MetaSocket;

namespace GecikmeBulma.Trade
{
    internal class OrderManager
    {
        static List<Order> orders = new List<Order>();

        public void AddOrder(Order order)
        {
            lock (orders)
            {
                if (!orders.Exists(_order => _order.Id == order.Id))
                {
                    if (order.Id != -1)
                    {
                        orders.Add(order);

                        string debug = String.Format("OrderManager (AddOrder): {0}",
                            order.ToSummary()
                        );
                        Utils.SendLog(LoggerService.LoggerType.DEBUG, debug);
                    }
                }
            }
        }

        Order GetOrderFromDB(int orderId)
        {
            try
            {
                return TradeManager.databaseManager.GetOrder(orderId);
            }
            catch (RecordNotFoundException exception)
            {
                return null;
            }
            catch (Exception exception)
            {
                return null;
            }
        }

        Order GetOrderFromDB(int arbitrageId, int ticket)
        {
            try
            {
                return TradeManager.databaseManager.GetOrder(arbitrageId, ticket);
            }
            catch (RecordNotFoundException exception)
            {
                return null;
            }
            catch (Exception exception)
            {
                return null;
            }
        }

        public Order GetOrder(int orderId)
        {
            lock (orders)
            {
                Order order = orders.Find(_order => _order.Id == orderId);
                if (order != null)
                {
                    return order;
                }
                else
                {
                    try
                    {
                        order = GetOrderFromDB(orderId);
                        AddOrder(order);

                        return order;
                    }
                    catch (RecordNotFoundException exception)
                    {
                        return null;
                    }
                    catch (Exception exception)
                    {
                        return null;
                    }
                }
            }
        }

        public Order GetOrder(int arbitrageId, int ticket)
        {
            lock (orders)
            {
                Order order = orders.Find(_order => _order.Arbitrage.Id == arbitrageId && _order.Ticket == ticket);
                if (order != null)
                {
                    return order;
                }
                else
                {
                    try
                    {
                        order = GetOrderFromDB(arbitrageId, ticket);
                        AddOrder(order);

                        return order;
                    }
                    catch (RecordNotFoundException exception)
                    {
                        return null;
                    }
                    catch (Exception exception)
                    {
                        return null;
                    }
                }
            }
        }

        public void RemoveOrder(Order order)
        {
            lock (orders)
            {
                orders.Remove(order);

                string debug = String.Format("OrderManager (RemoveOrder): {0}",
                        order.ToSummary()
                    );
                Utils.SendLog(LoggerService.LoggerType.DEBUG, debug);
            }
        }

        public Order CreateOrder(Pair pair, Arbitrage arbitrage, OrderType type, double volume, int step)
        {
            Order order = new Order()
            {
                Pair = pair,
                Arbitrage = arbitrage,
                Ticket = -1,
                Step = step,
                Type = type,
                Process = OrderProcess.PREPARED,
                Error = OrderError.NOT_ERROR,
                SendedTime = (DateTime)System.Data.SqlTypes.SqlDateTime.MinValue,
                SendedPrice = 0,
                OpenTime = (DateTime)System.Data.SqlTypes.SqlDateTime.MinValue,
                OpenPrice = 0,
                ClosedTime = (DateTime)System.Data.SqlTypes.SqlDateTime.MinValue,
                ClosedPrice = 0,
                Volume = volume,
                Commission = 0,
                Swap = 0,
                Profit = 0
            };

            if (order.Save())
            {
                AddOrder(order);
                return order;
            }

            Console.WriteLine("Order kaydedilmedi!");

            return null;
        }

        public void CloseOrder(Order order)
        {
            lock (orders)
            {
                if (!orders.Exists(_order => _order == order))
                {
                    orders.Add(order);
                }

                if (order.Process != OrderProcess.SEND_CLOSE && order.Process != OrderProcess.CLOSED)
                {
                    SocketSend_OrderClose(order);
                }
            }
        }

        public void SocketSend_OrderSend(Order order)
        {
            order.Process = OrderProcess.SEND_OPEN;
            order.SendedTime = DateTime.Now;
            order.Update();

            string request = String.Format("\"router\":\"{0}\",\"order_id\":\"{1}\",\"trade_type\":\"{2}\",\"volume\":\"{3}\",\"arbitrage_id\":\"{4}\"",
                "order_send",
                order.Id.ToString(),
                ((int)order.Type).ToString(),
                order.Volume.ToString().Replace(',', '.'),
                order.Arbitrage.Id.ToString()
                );

            BaseClient baseClient = TradeManager.clientManager.Get(order.Pair, ClientType.ORDER);
            if (baseClient == null)
            {
                string debug2 = String.Format("OrderManager (SocketSend_OrderSend) --> The baseClient is not found. Pair: ({0}), Order: ({1})",
                    order.Pair.ToString(),
                    order.ToSummary()
                    );
                Utils.SendLog(LoggerService.LoggerType.WARNING, debug2);
            }
            else
            {
                TradeManager.orderSocketManager.Send(baseClient.Client, request);

                string debug = String.Format("OrderManager (SocketSend_OrderSend) --> {0}",
                            order.ToSummary()
                        );
                Utils.SendLog(LoggerService.LoggerType.DEBUG, debug);
            }
        }

        public void SocketSend_OrderInfo(Order order)
        {
            string request;

            if (order.Ticket == -1)
            {
                request = String.Format("\"router\":\"{0}\",\"order_id\":\"{1}\"",
                    "order_info_id",
                    order.Id.ToString()
                );
            }
            else
            {
                request = String.Format("\"router\":\"{0}\",\"order_id\":\"{1}\",\"order_ticket\":\"{2}\"",
                    "order_info_ticket",
                    order.Id.ToString(),
                    order.Ticket.ToString()
                );
            }

            BaseClient orderSocketClient = TradeManager.clientManager.Get(order.Pair, ClientType.ORDER);
            if (orderSocketClient == null)
            {
                string debug2 = String.Format("OrderManager (SocketSend_OrderInfo) --> The orderSocketClient is not found. Pair: ({0}), Order: ({1})",
                    order.Pair.ToString(),
                    order.ToSummary()
                    );
                Utils.SendLog(LoggerService.LoggerType.WARNING, debug2);
            }
            else
            {
                TradeManager.orderSocketManager.Send(orderSocketClient.Client, request);
            }

        }

        void SocketSend_OrderClose(Order order)
        {
            string request;

            if (order.Ticket == -1)
            {
                request = String.Format("\"router\":\"{0}\",\"order_id\":\"{1}\"",
                    "order_close_id",
                    order.Id.ToString()
                );
            }
            else
            {
                request = String.Format("\"router\":\"{0}\",\"order_id\":\"{1}\",\"order_ticket\":\"{2}\"",
                    "order_close_ticket",
                    order.Id.ToString(),
                    order.Ticket.ToString()
                );
            }

            order.Process = OrderProcess.SEND_CLOSE;
            order.LastControl = DateTime.Now;
            order.Update();

            BaseClient baseClient = TradeManager.clientManager.Get(order.Pair, ClientType.ORDER);
            if (baseClient == null)
            {
                string debug2 = String.Format("OrderManager (SocketSend_OrderClose) --> The baseClient is not found. Account: ({0}), Pair: ({1}), Order: ({2})",
                    order.Pair.ToString(),
                    order.ToSummary()
                    );
                Utils.SendLog(LoggerService.LoggerType.WARNING, debug2);
            }
            else
            {
                TradeManager.orderSocketManager.Send(baseClient.Client, request);

                string debug = String.Format("OrderManager (SocketSend_OrderClose) --> {0}",
                            order.ToSummary()
                        );
                Utils.SendLog(LoggerService.LoggerType.DEBUG, debug);
            }

        }

        public void SocketReceive_OrderSend(TcpClient client, dynamic json_data)
        {
            int orderId = (int)json_data.order_id;
            Order order = GetOrder(orderId);

            if (order == null)
            {
                string debug = String.Format("OrderManager (SocketReceive_OrderSend) --> Order '#{0}' is not found!",
                        order.Id.ToString()
                    );
                Utils.SendLog(LoggerService.LoggerType.WARNING, debug);
            }
            else
            {
                if ((bool)json_data.error)
                {
                    order.Error = OrderError.ORDER_NOT_FOUND;

                    string debug = String.Format("OrderManager (SocketReceive_OrderSend) --> Order '#{0}' is not opened! Error Code: {1}",
                        order.Id.ToString(),
                        ((int)json_data.code).ToString()
                    );
                    Utils.SendLog(LoggerService.LoggerType.WARNING, debug);
                }
                else
                {
                    order.Ticket = (int)json_data.ticket;
                    order.SendedPrice = (double)json_data.sended_price;
                    order.OpenTime = Utils.UnixTimeStampToDateTime((ulong)json_data.open_time);
                    order.OpenPrice = (double)json_data.open_price;
                    order.Volume = (double)json_data.volume;
                    order.Commission = (double)json_data.commission;
                    order.Process = OrderProcess.IN_PROCESS;
                    order.LastControl = DateTime.Now;

                    string debug = String.Format("OrderManager (SocketReceive_OrderSend) --> Order is saved. {0}",
                        order.ToString()
                    );
                    Utils.SendLog(LoggerService.LoggerType.DEBUG, debug);
                }

                order.Update();
            }
        }

        public void SocketReceive_OrderRegister(TcpClient client, dynamic json_data)
        {
            int arbitrageId = (int)json_data.arbitrage_id;
            int pairId = (int)json_data.pair_id;
            int breakoutId = (int)json_data.breakout_id;
            int ticket = (int)json_data.ticket;
            int step = (int)json_data.step;
            int magicNumber = (int)json_data.magic;

            Pair pair = TradeManager.pairManager.GetPair(pairId);
            Arbitrage arbitrage = TradeManager.arbitrageManager.GetArbitrage(arbitrageId);

            if (pair == null)
            {
                string debug = String.Format("OrderManager (SocketReceive_OrderRegister) --> The pair '#{0}' is not found!",
                        pairId.ToString()
                    );
                Utils.SendLog(LoggerService.LoggerType.WARNING, debug);
            }
            else if (arbitrage == null)
            {
                string debug = String.Format("OrderManager (SocketReceive_OrderRegister) --> The arbitrage '#{0}' is not found!",
                        arbitrageId.ToString()
                    );
                Utils.SendLog(LoggerService.LoggerType.WARNING, debug);
            }
            else
            {
                OrderType orderType = (OrderType)json_data.order_type;
                OrderProcess orderProcess = (OrderProcess)json_data.process;
                OrderError orderError = (OrderError)json_data.error;
                DateTime orderSendedTime = Utils.UnixTimeStampToDateTime((ulong)json_data.sended_time);
                double orderSendedPrice = (double)json_data.sended_price;
                DateTime orderOpenTime = Utils.UnixTimeStampToDateTime((ulong)json_data.open_time);
                double orderOpenPrice = (double)json_data.open_price;
                DateTime orderClosedTime = Utils.UnixTimeStampToDateTime((ulong)json_data.closed_time);
                double orderClosedPrice = (double)json_data.closed_price;
                double orderVolume = (double)json_data.volume;
                double orderCommission = (double)json_data.commission;
                double orderSwap = (double)json_data.swap;
                double orderProfit = (double)json_data.profit;

                Order order = GetOrder(arbitrage.Id, ticket);
                if (order == null)
                {
                    order = new Order();
                    order.Pair = pair;
                    order.Arbitrage = arbitrage;
                    order.Ticket = ticket;
                    order.Type = orderType;
                    order.Process = orderProcess;
                    order.Error = orderError;
                    order.SendedPrice = orderSendedPrice;
                    order.SendedTime = orderSendedTime;
                    order.OpenPrice = orderOpenPrice;
                    order.OpenTime = orderOpenTime;
                    order.ClosedPrice = orderClosedPrice;
                    order.ClosedTime = orderClosedTime;
                    order.Volume = orderVolume;
                    order.Commission = orderCommission;
                    order.Swap = orderSwap;
                    order.Profit = orderProfit;
                    order.LastControl = DateTime.Now;
                    order.Save();

                    AddOrder(order);

                    BaseClient baseClient = TradeManager.clientManager.Get(order.Pair, ClientType.ORDER);
                    if (baseClient == null)
                    {
                        string debug2 = String.Format("OrderManager (SocketReceive_OrderRegister) --> The baseClient is not found. Account: ({0}), Pair: ({1}), Order: ({2})",
                            order.Pair.ToString(),
                            order.ToSummary()
                            );
                        Utils.SendLog(LoggerService.LoggerType.WARNING, debug2);
                    }
                    else
                    {
                        string request = String.Format("\"router\":\"{0}\",\"order_id\":\"{1}\",\"order_ticket\":\"{2}\"",
                            "order_register",
                            order.Id.ToString(),
                            order.Ticket.ToString()
                        );

                        TradeManager.orderSocketManager.Send(baseClient.Client, request);

                        string debug = String.Format("OrderManager (SocketReceive_OrderRegister) --> {0}",
                                    order.ToSummary()
                                );
                        Utils.SendLog(LoggerService.LoggerType.DEBUG, debug);
                    }
                }
                else
                {
                    order.Process = orderProcess;
                    order.Error = orderError;
                    order.SendedPrice = orderSendedPrice;
                    order.SendedTime = orderSendedTime;
                    order.OpenPrice = orderOpenPrice;
                    order.OpenTime = orderOpenTime;
                    order.ClosedPrice = orderClosedPrice;
                    order.ClosedTime = orderClosedTime;
                    order.Volume = orderVolume;
                    order.Commission = orderCommission;
                    order.Swap = orderSwap;
                    order.Profit = orderProfit;
                    order.LastControl = DateTime.Now;

                    // OrderUpdate(order);
                }
            }
        }

        public void SocketReceive_OrderInfoByTicket(TcpClient client, dynamic json_data)
        {
            int orderId = (int)json_data.order_id;

            Order order = GetOrder(orderId);

            if (order == null)
            {
                string debug = String.Format("OrderManager (SocketReceive_OrderInfoByTicket) --> Order '#{0}' is not found!",
                        order.Id.ToString()
                    );
                Utils.SendLog(LoggerService.LoggerType.WARNING, debug);
            }
            else
            {
                if ((bool)json_data.error)
                {
                    order.Error = OrderError.ORDER_NOT_FOUND;

                    string debug = String.Format("OrderManager (SocketReceive_OrderInfoByTicket) --> Order '#{0}' is not found by broker!",
                        order.Id.ToString()
                    );
                    Utils.SendLog(LoggerService.LoggerType.WARNING, debug);
                }
                else
                {
                    if (order.Ticket == -1)
                    {
                        order.Ticket = (int)json_data.ticket;
                    }
                    order.Type = (OrderType)((int)json_data.order_type);
                    order.OpenTime = Utils.UnixTimeStampToDateTime((ulong)json_data.open_time);
                    order.OpenPrice = (double)json_data.open_price;
                    order.Commission = (double)json_data.commission;
                    order.Swap = (double)json_data.swap;
                    order.Volume = (double)json_data.volume;
                    order.Profit = (double)json_data.profit;
                    order.LastControl = DateTime.Now;

                    int orderClosedTime = (int)json_data.closed_time;
                    if (orderClosedTime > 0)
                    {
                        order.Process = OrderProcess.CLOSED;
                        order.ClosedTime = Utils.UnixTimeStampToDateTime((ulong)json_data.closed_time);
                        order.ClosedPrice = (double)json_data.closed_price;
                    }
                    else
                    {
                        if (order.Process == OrderProcess.SEND_CLOSE)
                        {
                            order.Error = OrderError.ORDER_NOT_CLOSED;
                        }
                        else
                        {
                            order.Process = OrderProcess.IN_PROCESS;
                        }
                    }


                    //string debug = String.Format("OrderManager (SocketReceive_OrderInfoByTicket) --> Order is updated. {0}",
                    //    order.ToString()
                    //);
                    //Utils.SendLog(LoggerService.LoggerType.DEBUG, debug);
                }
                order.Update();
            }
        }

        public void SocketReceive_OrderTickUpdate(TcpClient client, dynamic json_data)
        {
            int orderId = (int)json_data.order_id;
            int pairId = (int)json_data.pair_id;

            Order order = GetOrder(orderId);
            if (order == null)
            {
                string debug = String.Format("OrderManager (SocketReceive_OrderTickUpdate) --> Order '#{0}' is not found!",
                        orderId.ToString()
                    );
                Utils.SendLog(LoggerService.LoggerType.WARNING, debug);
            }
            else
            {
                if (order.Pair == null)
                {
                    order.Pair = TradeManager.pairManager.GetPair(pairId);
                }

                order.Commission = (double)json_data.commission;
                order.Swap = (double)json_data.swap;
                order.Volume = (double)json_data.volume;
                order.Profit = (double)json_data.profit;
                order.LastControl = DateTime.Now;
            }
        }

        public void SocketReceive_OrderClose(TcpClient client, dynamic json_data)
        {
            int orderId = (int)json_data.order_id;

            Order order = GetOrder(orderId);
            if (order == null)
            {
                string debug = String.Format("OrderManager (SocketReceive_OrderClose) --> Order '#{0}' is not found!",
                        orderId.ToString()
                    );
                Utils.SendLog(LoggerService.LoggerType.WARNING, debug);
            }
            else
            {
                bool error = (bool)json_data.error;
                if (error)
                {
                    int lastError = (int)json_data.code;

                    string debug = String.Format("OrderManager (SocketReceive_OrderClose) --> Order '#{0}' didn't close! Error code: {1}",
                        orderId.ToString(),
                        lastError.ToString()
                    );
                    Utils.SendLog(LoggerService.LoggerType.WARNING, debug);
                }
                else
                {
                    order.Commission = (double)json_data.commission;
                    order.Swap = (double)json_data.swap;
                    order.Profit = (double)json_data.profit;
                    order.Process = OrderProcess.CLOSED;
                    order.ClosedTime = Utils.UnixTimeStampToDateTime((ulong)json_data.closed_time);
                    order.ClosedPrice = (double)json_data.closed_price;
                    order.LastControl = DateTime.Now;

                    order.Update();
                }
            }
        }

        public void SocketReceive_ArbitrageTickUpdate(TcpClient client, dynamic json_data)
        {
            int arbitrageId = (int)json_data.arbitrage_id;
            int pairId = (int)json_data.pair_id;
            double profit = (double)json_data.profit;

            Arbitrage arbitrage = TradeManager.arbitrageManager.GetArbitrage(arbitrageId);
            if(arbitrage == null)
            {
                string debug = String.Format("OrderManager (SocketReceive_ArbitrageTickUpdate) --> Arbitrage '#{0}' is not found!",
                        arbitrageId.ToString()
                    );
                Utils.SendLog(LoggerService.LoggerType.WARNING, debug);
            }
            else
            {
                if(arbitrage.AskPair.Id == pairId)
                {
                    arbitrage.AskPairProfit = profit;
                }
                else if(arbitrage.BidPair.Id == pairId)
                {
                    arbitrage.BidPairProfit = profit;
                }

                if (UI.UIManager.dataGridView_Trades_UpdateItem(arbitrage))
                {
                    UI.UIManager.dataGridView_Trades_AddItem(arbitrage);
                }
            }
        }

        void Controller()
        {
            string debug = "Order Manager Controller Starting...";
            Utils.SendLog(LoggerService.LoggerType.SUCCESS, debug);

            while (true)
            {
                foreach (Order order in orders.ToList())
                {
                    // if(order.MagicNumber > 0) continue;

                    if (order.Process == OrderProcess.SEND_OPEN)
                    {
                        if (order.Error != OrderError.NOT_ERROR)
                        {
                            string debug2 = String.Format("OrderManager (Controller) --> There was an error in the order. Send again. ({0})",
                                order.ToSummary()
                            );
                            Utils.SendLog(LoggerService.LoggerType.WARNING, debug2);

                            SocketSend_OrderSend(order);
                        }
                        else if ((DateTime.Now - order.LastControl).TotalSeconds >= 2)
                        {
                            SocketSend_OrderInfo(order);
                        }
                    }
                    else if (order.Process == OrderProcess.IN_PROCESS)
                    {
                        if ((DateTime.Now - order.LastControl).TotalSeconds >= 5)
                        {
                            order.LastControl = DateTime.Now;

                            //string debug2 = String.Format("OrderManager (Controller) --> The order has not been heard from for more than 5 seconds. It's being info checked. ({0})",
                            //    order.ToSummary()
                            //);
                            //Utils.SendLog(LoggerService.LoggerType.DEBUG, debug2);

                            SocketSend_OrderInfo(order);
                        }
                    }
                    else if (order.Process == OrderProcess.SEND_CLOSE)
                    {
                        if (order.Error != OrderError.NOT_ERROR)
                        {
                            string debug2 = String.Format("OrderManager (Controller) --> There was an error in the order. Close again. ({0})",
                                order.ToSummary()
                            );
                            Utils.SendLog(LoggerService.LoggerType.WARNING, debug2);

                            SocketSend_OrderClose(order);
                            order.Error = OrderError.NOT_ERROR;
                        }
                        else if ((DateTime.Now - order.LastControl).TotalSeconds >= 1)
                        {
                            order.LastControl = DateTime.Now;

                            string debug2 = String.Format("OrderManager (Controller) --> The order has not been heard from for more than 1 seconds. It's being info checked. ({0})",
                                order.ToSummary()
                            );
                            Utils.SendLog(LoggerService.LoggerType.DEBUG, debug2);

                            SocketSend_OrderInfo(order);
                        }
                    }
                    else if (order.Process == OrderProcess.CLOSED)
                    {
                        order.LastControl = DateTime.Now;

                        string debug2 = String.Format("OrderManager (Controller) --> Order is successfully closed. It is removed from the list. ({0})",
                                order.ToSummary()
                            );
                        Utils.SendLog(LoggerService.LoggerType.DEBUG, debug2);

                        RemoveOrder(order);
                    }
                }
                //Console.WriteLine("orders count: " + orders.Count.ToString());
                Thread.Sleep(1000);
            }
        }

        public void ControllerStart()
        {
            new Thread(new ThreadStart(Controller)).Start();
        }

    }

    //internal class OrderManager
    //{
    //    public List<Order> orders = new List<Order>();

    //    public Order GetOrder(int id)
    //    {
    //        return orders.Find(o => o.Id == id);
    //    }

    //    public void AddOrderInList(Order order)
    //    {
    //        lock (orders)
    //        {
    //            if (!orders.Any(o => o.Id == order.Id))
    //            {
    //                orders.Add(order);
    //            }
    //        }
    //    }

    //    public void RemoveOrderInList(Order order)
    //    {
    //        lock (orders)
    //        {
    //            orders.Remove(order);
    //        }
    //    }

    //    public void OrderSend(Pair pair, Order order, Arbitrage arbitrage)
    //    {
    //        string request = String.Format("\"sym\":\"{0}\",\"t\":\"{1}\",\"tt\":\"{2}\",\"v\":{3},\"p\":{4},\"s\":\"{5}\",\"aid\":\"{6}\"",
    //            pair.Symbol,
    //            "os", // t: Type 
    //            order.Type.ToString(), // tt: Trade Type 
    //            order.Volume.ToString().Replace(',', '.'), // v:  Volume
    //            order.SendedPrice.ToString().Replace(',', '.'), // p:  Price
    //            (order.Slippage * 10).ToString(), //  s:  Slippage
    //            arbitrage.Id.ToString() // aid: arbitrage id
    //        );

    //        TradeManager.orderSocketManager.Send(pair.Client, request);
    //        order.Process = OrderProcess.SEND_OPEN;

    //        string debug = String.Format("Arbitrage ID: {4} | Type: {0} | Price: {1} | Volume: {2} | Slippage: {3} | Process: {5}",
    //            order.Type.ToString(),
    //            order.SendedPrice.ToString(),
    //            order.Volume.ToString(),
    //            order.Slippage.ToString(),
    //            arbitrage.Id.ToString(),
    //            order.Process.ToString()
    //        );
    //        TradeManager.SendLog(LoggerService.LoggerType.INFO, arbitrage, pair, "OrderManager-OrderSend", debug);
    //    }

    //    public void OrderInfo(Pair pair, Order order, Arbitrage arbitrage)
    //    {
    //        string request = String.Format("\"sym\":\"{0}\",\"t\":\"{1}\",\"id\":\"{2}\",\"aid\":\"{3}\"",
    //            pair.Symbol,
    //            "oi", // t: Type 
    //            order.Id,
    //            arbitrage.Id.ToString()
    //        );

    //        TradeManager.orderSocketManager.Send(pair.Client, request);

    //        string debug = String.Format("ID: {0} | ArbitrageId: {1} | Process: {2}",
    //                order.Id.ToString(),
    //                arbitrage.Id.ToString(),
    //                order.Process.ToString()
    //            );
    //        TradeManager.SendLog(LoggerService.LoggerType.INFO, arbitrage, pair, "OrderManager-OrderInfo", debug);
    //    }

    //    public void OrderCheckStatus(Pair pair, Arbitrage arbitrage)
    //    {
    //        string request = String.Format("\"sym\":\"{0}\",\"t\":\"{1}\",\"aid\":\"{2}\"",
    //            pair.Symbol,
    //            "ocs", // t: Type 
    //            arbitrage.Id
    //        );

    //        TradeManager.orderSocketManager.Send(pair.Client, request);

    //        string debug = String.Format("Arbitrage ID: {0}",
    //                arbitrage.Id.ToString()
    //            );

    //        TradeManager.SendLog(LoggerService.LoggerType.INFO, arbitrage, pair, "OrderManager-OrderCheckStatus", debug);
    //    }

    //    public void OrderClose(Pair pair, Order order, Arbitrage arbitrage)
    //    {
    //        // TcpClient client, string symbol, string broker, int ticket
    //        string request = String.Format("\"sym\":\"{0}\",\"t\":\"{1}\",\"id\":\"{2}\",\"aid\":\"{3}\",\"s\":\"{4}\"",
    //            pair.Symbol,
    //            "oc", // t: Type 
    //            order.Id,
    //            arbitrage.Id.ToString(),
    //            (order.Slippage * 10).ToString()
    //        );

    //        TradeManager.orderSocketManager.Send(pair.Client, request);
    //        order.Process = OrderProcess.SEND_CLOSE;

    //        string debug = String.Format("ID: {0} | Arbitrage ID: {1} | Process: {2} | Market Price: {3}",
    //            order.Id.ToString(),
    //            arbitrage.Id.ToString(),
    //            order.Process.ToString(),
    //            order.Type == OrderType.OP_BUY ? pair.Bid.ToString() : pair.Ask.ToString()
    //            );

    //        TradeManager.SendLog(LoggerService.LoggerType.INFO, arbitrage, pair, "OrderManager-OrderClose", debug);
    //    }

    //    public void ReceiveOrderSend(TcpClient client, dynamic json_data)
    //    {
    //        Arbitrage arbitrage = TradeManager.arbitrageManager.GetArbitrage((int)json_data.aid);
    //        Pair pair = TradeManager.pairManager.GetPair(client);

    //        OrderType orderType = OrderType.OP_BUY;

    //        if ((string)json_data.tt == "OP_BUY") orderType = OrderType.OP_BUY;
    //        else if ((string)json_data.tt == "OP_SELL") orderType = OrderType.OP_SELL;

    //        if ((bool)json_data.e)
    //        {
    //            if((int)json_data.code == 4109) // Autotrade disabled
    //            {
    //                if (orderType == OrderType.OP_BUY)
    //                {
    //                    arbitrage.AskOrder.Process = OrderProcess.NOT;

    //                }
    //                else if (orderType == OrderType.OP_SELL)
    //                {
    //                    arbitrage.BidOrder.Process = OrderProcess.NOT;
    //                }
    //            }
    //            else
    //            {
    //                if (orderType == OrderType.OP_BUY)
    //                {
    //                    arbitrage.AskOrder.Process = OrderProcess.ERROR;

    //                }
    //                else if (orderType == OrderType.OP_SELL)
    //                {
    //                    arbitrage.BidOrder.Process = OrderProcess.ERROR;
    //                }
    //            }


    //            string debug = String.Format("JSON: {0}",
    //                    json_data.ToString()
    //                );
    //            TradeManager.SendLog(LoggerService.LoggerType.ERROR, arbitrage, pair, "OrderManager-ReceiveOrderSend", debug);
    //        }
    //        else
    //        {
    //            Order order = null;

    //            if (orderType == OrderType.OP_BUY)
    //            {
    //                order = arbitrage.AskOrder;
    //            }
    //            else if(orderType == OrderType.OP_SELL)
    //            {
    //                order = arbitrage.BidOrder;
    //            }

    //            order.Id = (int)json_data.id;
    //            order.OpenTime = UI.UIManager.UnixTimeStampToDateTime((ulong)json_data.ot);
    //            order.OpenPrice = (double)json_data.op;
    //            order.Pair = pair;
    //            order.Type = orderType;
    //            order.Volume = (double)json_data.v;
    //            order.Commission = (double)json_data.c;
    //            order.Swap = (double)json_data.s;
    //            order.Profit = (double)json_data.p;
    //            order.Process = OrderProcess.IN_PROCESS;

    //            AddOrderInList(order);

    //            UI.UIManager.dataGridView4_UpdateItem(arbitrage);

    //            TradeManager.databaseManager.UpdateOrder(order, arbitrage.Id);

    //            string debug = String.Format("ID: {0} | ArbitrageId: {1} | OpenTime: {2} | OpenPrice: {3} | Type: {4} | Volume: {5} | Commission: {6} | Process: {7}",
    //                    order.Id.ToString(),
    //                    arbitrage.Id.ToString(),
    //                    order.OpenTime.ToString(),
    //                    order.OpenPrice.ToString(),
    //                    order.Type.ToString(),
    //                    order.Volume.ToString(),
    //                    order.Commission.ToString(),
    //                    order.Process.ToString()
    //                );

    //            TradeManager.SendLog(LoggerService.LoggerType.SUCCESS, arbitrage, pair, "OrderManager-ReceiveOrderSend", debug);
    //        }
    //    }

    //    public void ReceiveOrderClose(TcpClient client, dynamic json_data)
    //    {
    //        Arbitrage arbitrage = TradeManager.arbitrageManager.GetArbitrage((int)json_data.aid);
    //        Order order = GetOrder((int)json_data.id);
    //        Pair pair = TradeManager.pairManager.GetPair(client);

    //        if ((bool)json_data.e)
    //        {
    //            if ((int)json_data.code == 404)
    //            {

    //                if(arbitrage.AskPair.Id == (int)json_data.sym)
    //                {
    //                    arbitrage.AskOrder.Process = OrderProcess.NOT;
    //                }
    //                else if (arbitrage.BidPair.Id == (int)json_data.sym)
    //                {
    //                    arbitrage.BidOrder.Process = OrderProcess.NOT;
    //                }
    //            }
    //            else if ((int)json_data.code == 4108) // Invalid ticket
    //            {
    //                if (order != null)
    //                {
    //                    order.CloseTime = UI.UIManager.UnixTimeStampToDateTime((ulong)json_data.ct);
    //                    order.ClosePrice = (double)json_data.cp;
    //                    order.Process = OrderProcess.CLOSED;
    //                    order.Swap = (double)json_data.s;
    //                    order.Commission = (double)json_data.c;
    //                    order.Profit = (double)json_data.p;
    //                }

    //                if (arbitrage.AskPair.Id == (int)json_data.sym)
    //                {
    //                    arbitrage.AskOrder.Process = OrderProcess.CLOSED;
    //                }
    //                else if (arbitrage.BidPair.Id == (int)json_data.sym)
    //                {
    //                    arbitrage.BidOrder.Process = OrderProcess.CLOSED;
    //                }
    //            }
    //            else if((int) json_data.ct > 0)
    //            {
    //                if (order != null)
    //                {
    //                    order.CloseTime = UI.UIManager.UnixTimeStampToDateTime((ulong)json_data.ct);
    //                    order.ClosePrice = (double) json_data.cp;
    //                    order.Process = OrderProcess.CLOSED;
    //                    order.Swap = (double) json_data.s;
    //                    order.Commission = (double)json_data.c;
    //                    order.Profit = (double)json_data.p;
    //                }
    //            }
    //            else
    //            {
    //                if (arbitrage.AskPair.Id == (int)json_data.sym)
    //                {
    //                    arbitrage.AskOrder.Process = OrderProcess.ERROR;
    //                }
    //                else if (arbitrage.BidPair.Id == (int)json_data.sym)
    //                {
    //                    arbitrage.BidOrder.Process = OrderProcess.ERROR;
    //                }
    //            }

    //            string debug = String.Format("JSON: {0}",
    //                    json_data.ToString()
    //                );
    //            TradeManager.SendLog(LoggerService.LoggerType.ERROR, arbitrage, pair, "OrderManager-ReceiveOrderClose", debug);
    //        }
    //        else
    //        {
    //            order.Swap = (double) json_data.s;
    //            order.Profit = (double)json_data.p;
    //            order.CloseTime = UI.UIManager.UnixTimeStampToDateTime((ulong)json_data.ct);
    //            order.ClosePrice = (double)json_data.cp;
    //            order.Process = OrderProcess.CLOSED;

    //            string debug = String.Format("ID: {0} | ArbitrageId: {1} | Swap: {2} | Comm.: {3} | Profit: {4} | CloseTime: {5} | ClosePrice: {6} | Process: {7}",
    //                order.Id.ToString(),
    //                arbitrage.Id.ToString(),
    //                order.Swap.ToString(),
    //                order.Commission.ToString(),
    //                order.Profit.ToString(),
    //                order.CloseTime.ToString(),
    //                order.ClosePrice.ToString(),
    //                order.Process.ToString()
    //                );
    //            TradeManager.SendLog(LoggerService.LoggerType.SUCCESS, arbitrage, pair, "OrderManager-ReceiveOrderClose", debug);
    //        }
    //    }

    //    public void ReceiveOrderInfo(TcpClient client, dynamic json_data)
    //    {
    //        Arbitrage arbitrage = TradeManager.arbitrageManager.GetArbitrage((int)json_data.aid);
    //        Order order = GetOrder((int)json_data.id);
    //        Pair pair = TradeManager.pairManager.GetPair(client);

    //        if ((bool)json_data.e)
    //        {
    //            if(order != null) order.Process = OrderProcess.ERROR;

    //            string debug = String.Format("JSON: {0}",
    //                    json_data.ToString()
    //                );
    //            TradeManager.SendLog(LoggerService.LoggerType.ERROR, arbitrage, pair, "OrderManager-ReceiveOrderInfo", debug);
    //        }
    //        else
    //        {
    //            if(order == null)
    //            {
    //                OrderType orderType = OrderType.OP_BUY;

    //                if ((string)json_data.tt == "OP_BUY") orderType = OrderType.OP_BUY;
    //                else if ((string)json_data.tt == "OP_SELL") orderType = OrderType.OP_SELL;

    //                if (orderType == OrderType.OP_BUY)
    //                {
    //                    order = arbitrage.AskOrder;
    //                }
    //                else if (orderType == OrderType.OP_SELL)
    //                {
    //                    order = arbitrage.BidOrder;
    //                }

    //                order.Id = (int)json_data.id;
    //                order.OpenTime = UI.UIManager.UnixTimeStampToDateTime((ulong)json_data.ot);
    //                order.OpenPrice = (double)json_data.op;
    //                order.Pair = pair;
    //                order.Type = orderType;
    //                order.Volume = (double)json_data.v;
    //                order.Commission = (double)json_data.c;
    //                order.Swap = (double)json_data.s;
    //                order.Profit = (double)json_data.p;
    //                order.Process = OrderProcess.IN_PROCESS;

    //                AddOrderInList(order);

    //                TradeManager.databaseManager.UpdateOrder(order, arbitrage.Id);
    //            }

    //            int integerOrderClosedTime = (int) json_data.ct;

    //            if(integerOrderClosedTime > 0)
    //            {
    //                order.Process = OrderProcess.CLOSED;

    //                order.CloseTime = UI.UIManager.UnixTimeStampToDateTime((ulong)json_data.ct);
    //                order.ClosePrice = (double)json_data.cp;
    //            }
    //            else
    //            {
    //                order.Process = OrderProcess.IN_PROCESS;
    //            }

    //            order.OpenTime = UI.UIManager.UnixTimeStampToDateTime((ulong)json_data.ot);
    //            order.OpenPrice = (double)json_data.op;
    //            order.Volume = (double)json_data.v;
    //            order.Commission = (double)json_data.c;
    //            order.Swap = (double)json_data.s;
    //            order.Profit = (double)json_data.p;

    //            UI.UIManager.dataGridView4_UpdateItem(arbitrage);

    //            string debug = String.Format("ID: {0} | ArbitrageId: {1} | OpenTime: {2} | OpenPrice: {3} | Type: {4} | Volume: {5} | Commission: {6} | Swap: {7} | Profit: {8} | Process: {9}",
    //                order.Id.ToString(),
    //                arbitrage.Id.ToString(),
    //                order.OpenTime.ToString(),
    //                order.OpenPrice.ToString(),
    //                order.Type.ToString(),
    //                order.Volume.ToString(),
    //                order.Commission.ToString(),
    //                order.Swap.ToString(),
    //                order.Profit.ToString(),
    //                order.Process.ToString()
    //                );

    //            TradeManager.SendLog(LoggerService.LoggerType.SUCCESS, arbitrage, pair, "OrderManager-ReceiveOrderInfo", debug);
    //        }
    //    }

    //    public void ReceiveOrderCheckStatus(TcpClient client, dynamic json_data)
    //    {
    //        Arbitrage arbitrage = TradeManager.arbitrageManager.GetArbitrage((int)json_data.aid);
    //        Pair pair = TradeManager.pairManager.GetPair(client);

    //        if ((bool)json_data.e)
    //        {
    //            if(arbitrage.AskPair.Id == (int)json_data.sym)
    //            {
    //                arbitrage.AskOrder.Process = OrderProcess.ERROR;
    //            }
    //            else if (arbitrage.BidPair.Id == (int)json_data.sym)
    //            {
    //                arbitrage.BidOrder.Process = OrderProcess.ERROR;
    //            }

    //            string debug = String.Format("JSON: {0}",
    //                    json_data.ToString()
    //                );
    //            TradeManager.SendLog(LoggerService.LoggerType.ERROR, arbitrage, pair, "OrderManager-ReceiveOrderCheckStatus", debug);
    //        }
    //        else
    //        {
    //            Order order = null;

    //            if (arbitrage.AskPair.Id == (int)json_data.sym)
    //            {
    //                order = arbitrage.AskOrder;
    //            }
    //            else if (arbitrage.BidPair.Id == (int)json_data.sym)
    //            {
    //                order = arbitrage.BidOrder;
    //            }
    //            OrderType orderType = OrderType.OP_BUY;

    //            if ((string)json_data.tt == "OP_BUY") orderType = OrderType.OP_BUY;
    //            else if ((string)json_data.tt == "OP_SELL") orderType = OrderType.OP_SELL;

    //            order.Id = (int)json_data.id;
    //            order.OpenTime = UI.UIManager.UnixTimeStampToDateTime((ulong)json_data.ot);
    //            order.OpenPrice = (double)json_data.op;
    //            order.Pair = pair;
    //            order.Type = orderType;
    //            order.Volume = (double)json_data.v;
    //            order.Commission = (double)json_data.c;
    //            order.Swap = (double)json_data.s;
    //            order.Profit = (double)json_data.p;
    //            order.Process = OrderProcess.IN_PROCESS;

    //            AddOrderInList(order);

    //            UI.UIManager.dataGridView4_UpdateItem(arbitrage);

    //            TradeManager.databaseManager.UpdateOrder(order, arbitrage.Id);

    //            string debug = String.Format("ID: {0} | ArbitrageId: {1} | OpenTime: {2} | OpenPrice: {3} | Type: {4} | Volume: {5} | Commission: {6} | Swap: {7} | Profit: {8} | Process: {9}",
    //                order.Id.ToString(),
    //                arbitrage.Id.ToString(),
    //                order.OpenTime.ToString(),
    //                order.OpenPrice.ToString(),
    //                order.Type.ToString(),
    //                order.Volume.ToString(),
    //                order.Commission.ToString(),
    //                order.Swap.ToString(),
    //                order.Profit.ToString(),
    //                order.Process.ToString()
    //                );
    //            TradeManager.SendLog(LoggerService.LoggerType.SUCCESS, arbitrage, pair, "OrderManager-ReceiveOrderCheckStatus", debug);
    //        }
    //    }

    //    public void ReceiveOrderPrice(TcpClient client, dynamic json_data)
    //    {
    //        Order order = GetOrder((int)json_data.id);

    //        if(order != null)
    //        {
    //            order.Profit = (double)json_data.p;
    //        }
    //    }

    //    void Controller()
    //    {
    //        string debug = "Order Manager Controller Starting...";
    //        // Utils.SendLog(LoggerService.LoggerType.SUCCESS, debug);

    //        while (true)
    //        {
    //            foreach (Order order in orders.ToList())
    //            {
    //                // if(order.MagicNumber > 0) continue;

    //                if (order.Process == OrderProcess.SEND_OPEN)
    //                {
    //                    if (order.Error != OrderError.NOT_ERROR)
    //                    {
    //                        string debug2 = String.Format("OrderManager (Controller) --> There was an error in the order. Send again. ({0})",
    //                            order.ToSummary()
    //                        );
    //                        Utils.SendLog(LoggerService.LoggerType.WARNING, debug2);

    //                        SocketSend_OrderSend(order);
    //                    }
    //                    else if ((DateTime.Now - order.LastControl).TotalSeconds >= 2)
    //                    {
    //                        SocketSend_OrderInfo(order);
    //                    }
    //                }
    //                else if (order.Process == OrderProcess.IN_PROCESS)
    //                {
    //                    if ((DateTime.Now - order.LastControl).TotalSeconds >= 5)
    //                    {
    //                        order.LastControl = DateTime.Now;

    //                        //string debug2 = String.Format("OrderManager (Controller) --> The order has not been heard from for more than 5 seconds. It's being info checked. ({0})",
    //                        //    order.ToSummary()
    //                        //);
    //                        //Utils.SendLog(LoggerService.LoggerType.DEBUG, debug2);

    //                        SocketSend_OrderInfo(order);
    //                    }
    //                }
    //                else if (order.Process == OrderProcess.SEND_CLOSE)
    //                {
    //                    if (order.Error != OrderError.NOT_ERROR)
    //                    {
    //                        string debug2 = String.Format("OrderManager (Controller) --> There was an error in the order. Close again. ({0})",
    //                            order.ToSummary()
    //                        );
    //                        Utils.SendLog(LoggerService.LoggerType.WARNING, debug2);

    //                        SocketSend_OrderClose(order);
    //                        order.Error = OrderError.NOT_ERROR;
    //                    }
    //                    else if ((DateTime.Now - order.LastControl).TotalSeconds >= 1)
    //                    {
    //                        order.LastControl = DateTime.Now;

    //                        string debug2 = String.Format("OrderManager (Controller) --> The order has not been heard from for more than 1 seconds. It's being info checked. ({0})",
    //                            order.ToSummary()
    //                        );
    //                        Utils.SendLog(LoggerService.LoggerType.DEBUG, debug2);

    //                        SocketSend_OrderInfo(order);
    //                    }
    //                }
    //                else if (order.Process == OrderProcess.CLOSED)
    //                {
    //                    order.LastControl = DateTime.Now;

    //                    string debug2 = String.Format("OrderManager (Controller) --> Order is successfully closed. It is removed from the list. ({0})",
    //                            order.ToSummary()
    //                        );
    //                    Utils.SendLog(LoggerService.LoggerType.DEBUG, debug2);

    //                    RemoveOrder(order);
    //                }
    //            }
    //            //Console.WriteLine("orders count: " + orders.Count.ToString());
    //            Thread.Sleep(250);
    //        }
    //    }

    //    public void ControllerStart()
    //    {
    //        new Thread(new ThreadStart(Controller)).Start();
    //    }
    //}

}
