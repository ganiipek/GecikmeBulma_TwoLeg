using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GecikmeBulma.Trade
{
    internal class ArbitrageManager
    {
        static volatile List<Arbitrage> arbitrages = new List<Arbitrage>();
        int arbitrageId = 0;

        int GetArbitrageIdFromDB()
        {
            int id = TradeManager.databaseManager.GetArbitrageId();

            return id;
        }

        public int GetNewArbitrageId()
        {
            return GetArbitrageIdFromDB() + 1;
        }

        public void AddArbitrageInList(Arbitrage arbitrage)
        {
            lock (arbitrages)
            {
                arbitrages.Add(arbitrage);
            }
        }

        public List<Arbitrage> GetArbitrages()
        {
            return arbitrages;
        }

        public void RemoveArbitrageInList(Arbitrage arbitrage)
        {
            lock(arbitrages)
            {
                arbitrages.Remove(arbitrage);
            }
            UI.UIManager.dataGridView_Trades_RemoveItem(arbitrage);
        }

        public Arbitrage GetArbitrage(int arbitrageId)
        {
            foreach(var arbitrage in arbitrages)
            {
                if(arbitrage.Id == arbitrageId) return arbitrage;
            }
            return null;
        }

        public List<Arbitrage> GetArbitrages(int pairId)
        {
            return arbitrages.FindAll(_arbitrage => _arbitrage.AskPair.Id == pairId || _arbitrage.BidPair.Id == pairId);
        }

        public Arbitrage CheckArbitrage(string symbol)
        {
            List<Pair> localPairs = TradeManager.pairManager.GetActivePairs(symbol);

            if (localPairs.Count > 0)
            {
                double askPrice = localPairs.Min(pair => pair.Ask);
                double bidPrice = localPairs.Max(pair => pair.Bid);

                Pair askPair = localPairs.Find(pair => pair.Ask == askPrice);
                Pair bidPair = localPairs.Find(pair => pair.Bid == bidPrice);

                return CheckArbitrage(askPair, bidPair);
            }

            return null;
        }

        public Arbitrage CheckArbitrage(Pair askPair, Pair bidPair, int pyramid=1)
        {
            if (askPair != null && bidPair != null && askPair.Broker.Id != bidPair.Broker.Id)
            {
                double askDiff = askPair.MinPipDiff / Math.Pow(10, askPair.Digits);
                double bidDiff = bidPair.MinPipDiff / Math.Pow(10, bidPair.Digits);

                double askOffset = askPair.Offset / Math.Pow(10, askPair.Digits);
                double bidOffset = bidPair.Offset / Math.Pow(10, bidPair.Digits);

                if ((bidPair.Bid + bidOffset - (bidDiff * pyramid)) >= (askPair.Ask + askOffset + (askDiff * pyramid)))
                {
                    Arbitrage arbitrage = new Arbitrage()
                    {
                        Created = DateTime.Now,
                        AskPair = askPair,
                        BidPair = bidPair
                    };

                    return arbitrage;
                }
            }
            return null;
        }

        public void CloseArbitrage(Arbitrage arbitrage)
        {
            arbitrage.ClosedProcess = true;

            List<Order> askOrders = arbitrage.AskOrders.FindAll(_order => _order.Process != OrderProcess.CLOSED && _order.Process != OrderProcess.SEND_CLOSE);
            List<Order> bidOrders = arbitrage.BidOrders.FindAll(_order => _order.Process != OrderProcess.CLOSED && _order.Process != OrderProcess.SEND_CLOSE);

            List<Order> orders = askOrders.Concat(bidOrders).ToList();

            foreach (Order order in orders)
            {
                if (order.Process != OrderProcess.SEND_CLOSE && order.Process != OrderProcess.CLOSED)
                {
                    TradeManager.orderManager.CloseOrder(order);
                    TradeManager.SendLog(LoggerService.LoggerType.SUCCESS, arbitrage, order.Pair, "ArbitrageManager-FindArbitrage", "(Arbitrage ID: " + arbitrage.Id.ToString() + ") Arbitrage bitti. Order kapanma emri gönderildi!");
                }
            }
        }

        public void CloseArbitrage(int arbitrageId)
        {
            Arbitrage arbitrage = GetArbitrage(arbitrageId);
            if(arbitrage != null) CloseArbitrage(arbitrage);
        }

        public void FindArbitrage(Pair pair)
        {
            Arbitrage newArbitrage = CheckArbitrage(pair.Symbol);

            if (pair.Active && newArbitrage != null && !arbitrages.Any(_arbitrage => (_arbitrage.AskPair.Id == newArbitrage.AskPair.Id && _arbitrage.BidPair.Id == newArbitrage.BidPair.Id) || (_arbitrage.AskPair.Id == newArbitrage.BidPair.Id && _arbitrage.BidPair.Id == newArbitrage.AskPair.Id))) // Add New Arbitrage
            {
                AddArbitrageInList(newArbitrage);
                // TRADE
                // İlk order hazırla ve gönder
                newArbitrage.LastError = DateTime.Now;
                newArbitrage.Save();

                string debug = String.Format("(Arbitrage ID: {0}) Yeni arbitrage bulundu! Ask Broker: {1} <--> Bid Broker: {2}",
                    newArbitrage.Id.ToString(),
                    newArbitrage.AskPair.Broker.Name,
                    newArbitrage.BidPair.Broker.Name
                    );

                TradeManager.SendLog(LoggerService.LoggerType.DEBUG, newArbitrage, newArbitrage.AskPair, "ArbitrageManager-FindArbitrage", debug);

                Order askOrder = TradeManager.orderManager.CreateOrder(newArbitrage.AskPair, newArbitrage, OrderType.OP_BUY, newArbitrage.AskPair.Volume, 1);
                askOrder.SendedTime = DateTime.Now;
                askOrder.SendedPrice = newArbitrage.AskPair.Ask;
                askOrder.Slippage = newArbitrage.AskPair.Slippage;
                askOrder.Process = OrderProcess.PREPARED;

                Order bidOrder = TradeManager.orderManager.CreateOrder(newArbitrage.BidPair, newArbitrage, OrderType.OP_SELL, newArbitrage.BidPair.Volume, 1);
                bidOrder.SendedTime = DateTime.Now;
                bidOrder.SendedPrice = newArbitrage.BidPair.Bid;
                bidOrder.Slippage = newArbitrage.BidPair.Slippage;
                bidOrder.Process = OrderProcess.PREPARED;

                newArbitrage.AskOrders.Add(askOrder);
                newArbitrage.BidOrders.Add(bidOrder);
                
                TradeManager.orderManager.AddOrder(askOrder);
                TradeManager.orderManager.AddOrder(bidOrder);

                TradeManager.orderManager.SocketSend_OrderSend(askOrder);
                TradeManager.orderManager.SocketSend_OrderSend(bidOrder);

                //TradeManager.databaseManager.AddNewArbitrage(newArbitrage);

                UI.UIManager.dataGridView_Trades_AddItem(newArbitrage);
            }
            else // old arbitrage kontrol
            {
                List<Arbitrage> oldArbitrages = GetArbitrages(pair.Id);

                if (oldArbitrages.Count > 0)
                {
                    foreach (Arbitrage oldArbitrage in oldArbitrages.FindAll(_oldArbitrage => _oldArbitrage.AskOrders.Count > 0 && _oldArbitrage.BidOrders.Count > 0))
                    {
                        if (oldArbitrage.ClosedProcess) // Arbitrage kapanma sürecine girmiş
                        {
                            if (oldArbitrage.AskOrders.All(_order => _order.Process == OrderProcess.CLOSED) && oldArbitrage.BidOrders.All(_order => _order.Process == OrderProcess.CLOSED))
                            {
                                RemoveArbitrageInList(oldArbitrage);
                            }
                            else
                            {
                                CloseArbitrage(oldArbitrage);
                            }

                        }
                        else // Arbitrage devam ediyor. Kontrol et.
                        {
                            if(oldArbitrage.AskOrders.Count(_order => _order.Process >= OrderProcess.IN_PROCESS) == oldArbitrage.CurrentPyramid && oldArbitrage.BidOrders.Count(_order => _order.Process >= OrderProcess.IN_PROCESS) == oldArbitrage.CurrentPyramid)
                            {
                                double profit = oldArbitrage.GetProfit();

                                //Console.WriteLine("\n"+ profit.ToString());
                                //foreach (Order order in oldArbitrage.AskOrders)
                                //{
                                //    Console.WriteLine(order.ToString());
                                //}
                                //foreach (Order order in oldArbitrage.BidOrders)
                                //{
                                //    Console.WriteLine(order.ToString());
                                //}


                                if (profit >= oldArbitrage.AskPair.TP)
                                {
                                    // kapanacak
                                    CloseArbitrage(oldArbitrage);
                                }
                                else if (pair.Active && oldArbitrage.AskOrders.All(_order => _order.Process == OrderProcess.IN_PROCESS) && oldArbitrage.BidOrders.All(_order => _order.Process == OrderProcess.IN_PROCESS))
                                {
                                    if (oldArbitrage.CurrentPyramid < oldArbitrage.AskPair.Pyramiding)
                                    {
                                        if (CheckArbitrage(oldArbitrage.AskPair, oldArbitrage.BidPair, oldArbitrage.CurrentPyramid + 1) != null)
                                        {
                                            oldArbitrage.CurrentPyramid += 1;

                                            Order askOrder = TradeManager.orderManager.CreateOrder(oldArbitrage.AskPair, oldArbitrage, OrderType.OP_BUY, oldArbitrage.AskPair.Volume, oldArbitrage.CurrentPyramid);
                                            askOrder.SendedTime = DateTime.Now;
                                            askOrder.SendedPrice = oldArbitrage.AskPair.Ask;
                                            askOrder.Slippage = oldArbitrage.AskPair.Slippage;
                                            askOrder.Process = OrderProcess.PREPARED;

                                            Order bidOrder = TradeManager.orderManager.CreateOrder(oldArbitrage.BidPair, oldArbitrage, OrderType.OP_SELL, oldArbitrage.BidPair.Volume, oldArbitrage.CurrentPyramid);
                                            bidOrder.SendedTime = DateTime.Now;
                                            bidOrder.SendedPrice = oldArbitrage.BidPair.Bid;
                                            bidOrder.Slippage = oldArbitrage.BidPair.Slippage;
                                            bidOrder.Process = OrderProcess.PREPARED;

                                            oldArbitrage.AskOrders.Add(askOrder);
                                            oldArbitrage.BidOrders.Add(bidOrder);

                                            TradeManager.orderManager.AddOrder(askOrder);
                                            TradeManager.orderManager.AddOrder(bidOrder);

                                            TradeManager.orderManager.SocketSend_OrderSend(askOrder);
                                            TradeManager.orderManager.SocketSend_OrderSend(bidOrder);

                                            oldArbitrage.Update();
                                        }
                                    }
                                }
                            }
                            
                        }
                    }
                }
            }
        }

        void Controller()
        {
            string debug = "Arbitrage Manager Controller Starting...";
            Utils.SendLog(LoggerService.LoggerType.SUCCESS, debug);

            while (true)
            {
                foreach(Arbitrage arbitrage in arbitrages)
                {
                    if(UI.UIManager.dataGridView_Trades_UpdateItem(arbitrage))
                    {
                        UI.UIManager.dataGridView_Trades_AddItem(arbitrage);
                    }
                }

                Thread.Sleep(250);
            }
        }

        public void ControllerStart()
        {
            new Thread(new ThreadStart(Controller)).Start();
        }
    }
}
