using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data;
using MySql.Data.MySqlClient;
using GecikmeBulma.Trade;
using System.Data;

namespace GecikmeBulma.Database
{
    internal class DatabaseManager
    {
        MySqlConnection con;
        MySqlCommand cmd;
        DatabaseBase db;
        string connectionString;

        public void Initialize()
        {
            db = new DatabaseBase()
            {
                Host = "198.244.179.150",
                Port = "3306",
                DatabaseName = "yalcinarbitrage",
                User = "yalcinarbi",
                Password = "aliveli4950."
            };

            connectionString = String.Format(
                // "Server={0};Database={1};Uid={2};Pwd={3};MultipleActiveResultSets=True",
                "Server={0};Database={1};Uid={2};Pwd={3}",
                db.Host,
                db.DatabaseName,
                db.User,
                db.Password
            );
        }

        public void Start()
        {
            try
            {
                con = new MySqlConnection(connectionString);
                con.Open();

                string debug = String.Format("Database ({0}/{1}) Connection is successful!",
                    db.Host,
                    db.DatabaseName
                    );
                Utils.SendLog(LoggerService.LoggerType.SUCCESS, debug);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Database Error: " + ex.Message);
            }
        }

        public bool IsConnected()
        {
            try
            {
                if (con == null) return false;
                if (con.State == ConnectionState.Open) return true;
                con.Open();
                return true;
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"Can not open connection ! ErrorCode: {ex.ErrorCode} Error: {ex.Message}");
                Console.WriteLine("Database Connection: False");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Can not open connection ! Error: {ex.Message}");
                Console.WriteLine("Database Connection: False");
                return false;
            }
        }

        #region Broker
        public Broker GetBroker(string broker_name, int platform_id)
        {
            Broker broker = new Broker();

            ExceptionManager.Handle(() =>
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = conn;
                        cmd.CommandText = "SELECT * FROM brokers WHERE name=@name AND platform_id=@platform_id";
                        cmd.Parameters.AddWithValue("platform_id", platform_id);
                        cmd.Parameters.AddWithValue("name", broker_name);

                        using (MySqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.HasRows)
                            {
                                while (rdr.Read())
                                {
                                    broker.Id = rdr.GetInt32(rdr.GetOrdinal("id"));
                                    broker.Name = (string)rdr["name"];
                                    broker.PlatformId = rdr.GetInt32(rdr.GetOrdinal("id"));
                                }
                            }
                            else
                            {
                                throw new RecordNotFoundException("Broker '" + broker_name + "' is not found!");
                            }
                        }
                    }
                }
            }, "DatabaseManager.GetBroker(string, int)");

            return broker;
        }

        public void AddNewBroker(Broker broker)
        {
            ExceptionManager.Handle(() =>
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = conn;
                        cmd.CommandText = "INSERT INTO brokers (platform_id, name) VALUES (@platform_id, @name); SELECT LAST_INSERT_ID();";
                        cmd.Parameters.AddWithValue("platform_id", broker.PlatformId);
                        cmd.Parameters.AddWithValue("name", broker.Name);

                        cmd.ExecuteNonQuery();
                        broker.Id = (int)cmd.LastInsertedId;
                    }
                }
            }, "DatabaseManager.AddNewBroker(Broker)");
        }
        #endregion

        #region Pair
        public Pair GetPair(int pair_id)
        {
            Pair pair = new Pair();

            ExceptionManager.Handle(() =>
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = conn;
                        cmd.CommandText = "SELECT * FROM pairs p INNER JOIN brokers b ON p.broker_id = b.id WHERE p.id=@pair_id";
                        cmd.Parameters.AddWithValue("pair_id", pair_id);

                        using (MySqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.HasRows)
                            {
                                while (rdr.Read())
                                {
                                    pair.Id = rdr.GetInt32(rdr.GetOrdinal("id"));
                                    pair.Client = null;
                                    pair.Symbol = (string)rdr["symbol"];
                                    pair.Broker = new Trade.Broker()
                                    {
                                        Id = rdr.GetInt32(rdr.GetOrdinal("broker_id")),
                                        Name = (string)rdr["name"],
                                        PlatformId = rdr.GetInt32(rdr.GetOrdinal("platform_id"))
                                    };
                                    pair.Digits = rdr.GetInt32(rdr.GetOrdinal("digits"));
                                }
                            }
                            else
                            {
                                throw new RecordNotFoundException("Pair '#" + pair_id.ToString() + "' is not found!");
                            }
                        }
                    }
                }
            }, "DatabaseManager.GetPair(int)");

            return pair;
        }

        public Pair GetPair(string symbol, int broker_id)
        {
            Pair pair = new Pair();

            ExceptionManager.Handle(() =>
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = conn;
                        cmd.CommandText = "SELECT * FROM pairs p INNER JOIN brokers b ON p.broker_id=b.id WHERE p.broker_id=@broker_id AND p.symbol=@symbol";
                        cmd.Parameters.AddWithValue("broker_id", broker_id);
                        cmd.Parameters.AddWithValue("symbol", symbol);

                        using (MySqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.HasRows)
                            {
                                while (rdr.Read())
                                {
                                    pair.Id = rdr.GetInt32(rdr.GetOrdinal("id"));
                                    pair.Client = null;
                                    pair.Symbol = (string)rdr["symbol"];
                                    pair.Broker = new Trade.Broker()
                                    {
                                        Id = rdr.GetInt32(rdr.GetOrdinal("broker_id")),
                                        Name = (string)rdr["name"],
                                        PlatformId = rdr.GetInt32(rdr.GetOrdinal("platform_id"))
                                    };
                                    pair.Digits = rdr.GetInt32(rdr.GetOrdinal("digits"));
                                }
                            }
                            else
                            {
                                throw new RecordNotFoundException("Pair (Broker Id: " + broker_id.ToString() + ", Symbol: " + symbol + ") is not found!");
                            }
                        }
                    }
                }
            }, "DatabaseManager.GetPair(string, int)");

            return pair;
        }

        public void AddNewPair(Pair pair)
        {
            ExceptionManager.Handle(() =>
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = conn;
                        cmd.CommandText = "INSERT INTO pairs (broker_id, symbol, bid, ask, digits) VALUES (@broker_id, @symbol, @bid, @ask, @digits); SELECT LAST_INSERT_ID();";
                        cmd.Parameters.AddWithValue("broker_id", pair.Broker.Id);
                        cmd.Parameters.AddWithValue("symbol", pair.Symbol);
                        cmd.Parameters.AddWithValue("bid", pair.Bid);
                        cmd.Parameters.AddWithValue("ask", pair.Ask);
                        cmd.Parameters.AddWithValue("digits", pair.Digits);

                        cmd.ExecuteNonQuery();
                        pair.Id = (int)cmd.LastInsertedId;
                    }
                }
            }, "DatabaseManager.GetOrder(int)");
        }

        #endregion

        #region Arbitrage
        public int GetArbitrageId()
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "SELECT TOP 1 * FROM arbitrages ORDER BY id DESC";

                    using (MySqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.HasRows)
                        {
                            while (rdr.Read())
                            {
                                return rdr.GetInt32(rdr.GetOrdinal("id")) + 1;
                            }
                        }
                    }
                }
            }

            return 1;
        }

        public void AddArbitrage(Arbitrage arbitrage)
        {
            ExceptionManager.Handle(() =>
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = conn;
                        cmd.CommandText = "INSERT INTO arbitrages (ask_pair_id, bid_pair_id, created_at, current_pyramid, max_pyramid, target_profit) VALUES (@ask_pair_id, @bid_pair_id, @created_at, @current_pyramid, @max_pyramid, @target_profit); SELECT LAST_INSERT_ID();";
                        cmd.Parameters.AddWithValue("ask_pair_id", arbitrage.AskPair.Id);
                        cmd.Parameters.AddWithValue("bid_pair_id", arbitrage.BidPair.Id);
                        cmd.Parameters.AddWithValue("created_at", arbitrage.Created);
                        cmd.Parameters.AddWithValue("current_pyramid", arbitrage.CurrentPyramid);
                        cmd.Parameters.AddWithValue("max_pyramid", arbitrage.MaxPyramid);
                        cmd.Parameters.AddWithValue("target_profit", arbitrage.TargetProfit);

                        cmd.ExecuteNonQuery();
                        arbitrage.Id = (int)cmd.LastInsertedId;
                    }
                }
            }, "DatabaseManager.AddArbitrage(Arbitrage)");
        }

        public void UpdateArbitrage(Arbitrage arbitrage)
        {
            ExceptionManager.Handle(() =>
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = conn;
                        cmd.CommandText = "UPDATE arbitrages SET current_pyramid=@current_pyramid, max_pyramid=@max_pyramid, target_profit=@target_profit WHERE id=@id";
                        cmd.Parameters.AddWithValue("id", arbitrage.Id);
                        cmd.Parameters.AddWithValue("current_pyramid", arbitrage.CurrentPyramid);
                        cmd.Parameters.AddWithValue("max_pyramid", arbitrage.MaxPyramid);
                        cmd.Parameters.AddWithValue("target_profit", arbitrage.TargetProfit);

                        cmd.ExecuteNonQuery();
                    }
                }
            }, "DatabaseManager.AddArbitrage(Arbitrage)");
        }

        #endregion

        #region Order
        public Order GetOrder(int orderId)
        {
            Order order = new Order();

            ExceptionManager.Handle(() =>
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = conn;
                        cmd.CommandText = "SELECT * FROM orders WHERE id=@id";
                        cmd.Parameters.AddWithValue("id", orderId);

                        using (MySqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.HasRows)
                            {
                                while (rdr.Read())
                                {
                                    order.Id = rdr.GetInt32(rdr.GetOrdinal("id"));
                                    order.Arbitrage = TradeManager.arbitrageManager.GetArbitrage(rdr.GetInt32(rdr.GetOrdinal("arbitrage_id")));
                                    order.Pair = TradeManager.pairManager.GetPair(rdr.GetInt32(rdr.GetOrdinal("pair_id")));
                                    order.Type = (OrderType)rdr.GetInt32(rdr.GetOrdinal("type_id"));
                                    order.Process = (OrderProcess)rdr.GetInt32(rdr.GetOrdinal("process_id"));
                                    order.Error = (OrderError)rdr.GetInt32(rdr.GetOrdinal("error_id"));
                                    order.Ticket = rdr.GetInt32(rdr.GetOrdinal("ticket"));
                                    order.SendedTime = rdr.GetDateTime(rdr.GetOrdinal("sended_time"));
                                    order.SendedPrice = rdr.GetDouble(rdr.GetOrdinal("sended_price"));
                                    order.OpenTime = rdr.GetDateTime(rdr.GetOrdinal("open_time"));
                                    order.OpenPrice = rdr.GetDouble(rdr.GetOrdinal("open_price"));
                                    order.ClosedTime = rdr.GetDateTime(rdr.GetOrdinal("closed_time"));
                                    order.ClosedPrice = rdr.GetDouble(rdr.GetOrdinal("closed_price"));
                                    order.Volume = rdr.GetDouble(rdr.GetOrdinal("volume"));
                                    order.Commission = rdr.GetDouble(rdr.GetOrdinal("commission"));
                                    order.Swap = rdr.GetDouble(rdr.GetOrdinal("swap"));
                                    order.Profit = rdr.GetDouble(rdr.GetOrdinal("profit"));
                                    break;
                                }
                            }
                            else
                            {
                                throw new RecordNotFoundException("Order '#" + orderId.ToString() + "' is not found!");
                            }
                        }
                    }
                }
            }, "DatabaseManager.GetOrder(int)");

            return order;
        }

        public Order GetOrder(int arbitrageId, int ticket)
        {
            Order order = new Order();

            ExceptionManager.Handle(() =>
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = conn;
                        cmd.CommandText = "SELECT * FROM orders WHERE ticket=@ticket AND arbitrage_id=@arbitrage_id";
                        cmd.Parameters.AddWithValue("arbitrage_id", arbitrageId);
                        cmd.Parameters.AddWithValue("ticket", ticket);

                        using (MySqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.HasRows)
                            {
                                while (rdr.Read())
                                {
                                    order.Id = rdr.GetInt32(rdr.GetOrdinal("id"));
                                    order.Arbitrage = TradeManager.arbitrageManager.GetArbitrage(rdr.GetInt32(rdr.GetOrdinal("arbitrage_id")));
                                    order.Pair = TradeManager.pairManager.GetPair(rdr.GetInt32(rdr.GetOrdinal("pair_id")));
                                    order.Type = (OrderType)rdr.GetInt32(rdr.GetOrdinal("type_id"));
                                    order.Process = (OrderProcess)rdr.GetInt32(rdr.GetOrdinal("process_id"));
                                    order.Error = (OrderError)rdr.GetInt32(rdr.GetOrdinal("error_id"));
                                    order.Ticket = rdr.GetInt32(rdr.GetOrdinal("ticket"));
                                    order.SendedTime = rdr.GetDateTime(rdr.GetOrdinal("sended_time"));
                                    order.SendedPrice = rdr.GetDouble(rdr.GetOrdinal("sended_price"));
                                    order.OpenTime = rdr.GetDateTime(rdr.GetOrdinal("open_time"));
                                    order.OpenPrice = rdr.GetDouble(rdr.GetOrdinal("open_price"));
                                    order.ClosedTime = rdr.GetDateTime(rdr.GetOrdinal("closed_time"));
                                    order.ClosedPrice = rdr.GetDouble(rdr.GetOrdinal("closed_price"));
                                    order.Volume = rdr.GetDouble(rdr.GetOrdinal("volume"));
                                    order.Commission = rdr.GetDouble(rdr.GetOrdinal("commission"));
                                    order.Swap = rdr.GetDouble(rdr.GetOrdinal("swap"));
                                    order.Profit = rdr.GetDouble(rdr.GetOrdinal("profit"));
                                    break;
                                }
                            }
                            else
                            {
                                throw new RecordNotFoundException("Order '(Arbitrage Id: " + arbitrageId.ToString() + ", Ticket: " + ticket.ToString() + ")' is not found!");
                            }
                        }
                    }
                }
            }, "DatabaseManager.GetOrder(int, int)");

            return order;
        }

        public void AddOrder(Order order)
        {
            ExceptionManager.Handle(() =>
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        string query = "INSERT INTO orders (arbitrage_id, pair_id, sended_time, sended_price, type_id, volume, slippage, process, step) VALUES (@arbitrage_id, @pair_id, @sended_time, @sended_price, @type_id, @volume, @slippage, @process, @step); SELECT LAST_INSERT_ID();";

                        cmd.Connection = conn;
                        cmd.CommandText = query;
                        cmd.Parameters.AddWithValue("arbitrage_id", order.Arbitrage.Id);
                        cmd.Parameters.AddWithValue("pair_id", order.Pair.Id);
                        cmd.Parameters.AddWithValue("sended_time", order.SendedTime);
                        cmd.Parameters.AddWithValue("sended_price", order.SendedPrice);
                        cmd.Parameters.AddWithValue("type_id", order.Type);
                        cmd.Parameters.AddWithValue("volume", order.Volume);
                        cmd.Parameters.AddWithValue("slippage", order.Slippage);
                        cmd.Parameters.AddWithValue("process", order.Process);
                        cmd.Parameters.AddWithValue("step", order.Step);

                        cmd.ExecuteNonQuery();
                        order.Id = (int)cmd.LastInsertedId;
                    }
                }
            }, "DatabaseManager.AddOrder(Order)");
        }

        public void UpdateOrder(Order order)
        {
            ExceptionManager.Handle(() =>
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        string query = @"UPDATE orders 
                        SET ticket=@ticket, sended_time=@sended_time, sended_price=@sended_price, open_time=@open_time, open_price=@open_price, closed_time=@closed_time, closed_price=@closed_price, volume=@volume, commission=@commission, swap=@swap, profit=@profit, process=@process
                        WHERE id=@id";
                        //WHERE arbitrage_id=@arbitrage_id AND type=@type AND (ticket IS NULL OR ticket = @ticket)";

                        cmd.Connection = conn;
                        cmd.CommandText = query;
                        cmd.Parameters.AddWithValue("id", order.Id);
                        cmd.Parameters.AddWithValue("ticket", order.Ticket);
                        cmd.Parameters.AddWithValue("arbitrage_id", order.Arbitrage.Id);
                        cmd.Parameters.AddWithValue("sended_time", order.SendedTime);
                        cmd.Parameters.AddWithValue("sended_price", order.SendedPrice);
                        cmd.Parameters.AddWithValue("open_time", order.OpenTime);
                        cmd.Parameters.AddWithValue("open_price", order.OpenPrice);
                        cmd.Parameters.AddWithValue("closed_time", order.ClosedTime);
                        cmd.Parameters.AddWithValue("closed_price", order.ClosedPrice);
                        cmd.Parameters.AddWithValue("type_id", order.Type);
                        cmd.Parameters.AddWithValue("volume", order.Volume);
                        cmd.Parameters.AddWithValue("slippage", order.Slippage);
                        cmd.Parameters.AddWithValue("commission", order.Commission);
                        cmd.Parameters.AddWithValue("swap", order.Swap);
                        cmd.Parameters.AddWithValue("profit", order.Profit);
                        cmd.Parameters.AddWithValue("process", order.Process);

                        cmd.ExecuteNonQuery();
                    }
                }
            }, "DatabaseManager.UpdateOrder(Order)");
        }
        #endregion

        public void AddDebug(string type, Trade.Arbitrage arbitrage, Trade.Pair pair, string callback, string message, DateTime created_at)
        {
            //using (MySqlConnection conn = new MySqlConnection(connectionString))
            //{
            //    conn.Open();
            //    using (MySqlCommand cmd = new MySqlCommand())
            //    {
            //        cmd.Connection = conn;
            //        cmd.CommandText = "INSERT INTO debug (type, pair_id, arbitrage_id, callback, message, created_at) VALUES (@type, @pair_id, @arbitrage_id, @callback, @message, @created_at)";
            //        cmd.Parameters.AddWithValue("type", type);
            //        cmd.Parameters.AddWithValue("pair_id", pair.Id);
            //        cmd.Parameters.AddWithValue("arbitrage_id", arbitrage.Id);
            //        cmd.Parameters.AddWithValue("callback", callback);
            //        cmd.Parameters.AddWithValue("message", message);
            //        cmd.Parameters.AddWithValue("created_at", created_at);
            //        cmd.ExecuteNonQuery();
            //    }
            //}
        }
    }
}
