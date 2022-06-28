#define program_version "1.3"

#property link      "https://24capitalmanagement.com/"
#property version   program_version
#property description "Multi Breakout Strategy MetaTrader 4 Manager"
#property copyright "2022 © 24 Capital Management"
#property strict

#include <B.O.S/JAson.mqh>
#include <B.O.S/socket-library-mt4-mt5.mqh>

string developer_settings              = ""; // <------ DEVELOPER SETTINGS ------>
bool developer_debug                   = False; // Debug

extern string     category_settings    = "";           // <------------ SETTINGS ------------>
extern bool       custom_symbol_bool   = False;        // Custom Symbol
extern string     custom_symbol        = NULL;         // Custom Symbol 

extern string     category_socket      = "";           // <------------- SOCKET ------------->
extern bool       InpSocketActive      = True;         // Socket Activated
extern string     InpHostname          = "127.0.0.1";  // Server hostname or IP address
extern ushort     InpPricePort         = 3131;         // Price port
extern ushort     InpOrderPort         = 3169;         // Order port
extern ushort     InpTradePort         = 6969;         // Trade port
extern int        InpBufferSize        = 512;          // Buffer Size
extern int        InpReceiveMS         = 10;           // Receive Milliseconds

struct SOrder
{
   int id;
   int ticket;
   double volume;
   double swap;
   double profit;
   double commission;
   double sended_price;

   void SOrder()
   {
      id             = 0;
      ticket         = 0;
      volume         = 0;
      swap           = 0;
      profit         = 0;
      commission     = 0;
      sended_price   = 0;
   }
};

int broker_id  = -1;
int account_id = -1;
int symbol_id  = -1;

int volume_decimal_count = 0;

string main_symbol;

SOrder order_list[];

enum ENUM_SOCKET_CLIENT_TYPE
{
   SOCKET_CLIENT_TYPE_TRADE,
   SOCKET_CLIENT_TYPE_ORDER,
   SOCKET_CLIENT_TYPE_PRICE
};

class Socket
{
   string                     host;
   int                        port;
   int                        buffer_size;
   ENUM_SOCKET_CLIENT_TYPE    client_type;
   bool                       registered;
   datetime                   last_communication;

   public:
      int timer;
      ClientSocket * glbClientSocket;

      Socket()
      {
         registered = false;
         timer    = 0;
      }
      
      void initialize(string Host, int Port, int BufferSize, ENUM_SOCKET_CLIENT_TYPE ClientType)
      {
         glbClientSocket   = NULL;
         host              = Host;
         port              = Port;
         buffer_size       = BufferSize;
         client_type       = ClientType;
      }
      
      bool connect()
      {
         if (!glbClientSocket) 
         {
            PrintFormat("[%s] Socket is trying to connect. (%s:%s)",
               main_symbol,
               host,
               IntegerToString(port)
            );

            glbClientSocket = new ClientSocket(host, port);
            glbClientSocket.SendBufferSize = buffer_size;
            glbClientSocket.ReceiveBufferSize = buffer_size;
            
            if (isConnected()) 
            {
               PrintFormat("[%s] Socket connection is successful. (%s:%s)",
                  main_symbol,
                  host,
                  IntegerToString(port)
               );

               return true;
            } 
            else 
            {
               PrintFormat("[%s] Socket connection is unsuccessful. (%s:%s)",
                  main_symbol,
                  host,
                  IntegerToString(port)
               );

               return false;
            }
         }
         return false;
      }
      
      bool isConnected()
      {
         return glbClientSocket.IsSocketConnected();
      }
      
      void disconnect()
      {
         if (glbClientSocket) 
         {
            delete glbClientSocket;
            glbClientSocket = NULL;
            registered = false;
         }
      }

      bool reconnect()
      {
         disconnect();
         
         if(connect())
         {
            return true;
         }
         else
         {
            return false;
         }
      }

      bool _send(string data)
      {
         if(glbClientSocket.Send(data)) 
         {
            last_communication = TimeCurrent();
            return true;
         }
         else
         {
            reconnect();
            return false;
         }
      }
      
      string receive()
      {
         string data = glbClientSocket.Receive();
         return data;
      }

      bool receiveList(string &list[])
      {
         string data = receive();
         if(data != "")
         {
            string result1[];
            string result2[];
            
            ushort u_sep = StringGetCharacter("{", 0);
            int k = StringSplit(data, u_sep, result1);
            
            if(k > 0)
            {
               for(int i=1; i<k; i++)
               {
                  u_sep=StringGetCharacter("}", 0);
                  int j = StringSplit(result1[i], u_sep, result2);
                  
                  if(j > 0)
                  {
                     string simplified_data = "{" + result2[0] + "}";
                     
                     int size = ArraySize(list);
                     ArrayResize(list, size + 1);
                     list[size] = simplified_data;
                  }
               }
            }
         }
         
         if(ArraySize(list) > 0) return true;
         return false;
      }

      bool getRegisterStatus()
      {
         return registered;
      }

      void setRegisterStatus(bool status)
      {
         registered = status;

         PrintFormat("[%s] Socket (%s:%s) --> Setting register status is %s.",
            main_symbol,
            host,
            IntegerToString(port),
            (string) registered
         );
      }

      bool register()
      {
         if(account_id == -1 || symbol_id == -1)
         {
            PrintFormat("[%s] Socket (%s:%s) --> Register request is failed. Account id(%s) or Pair id(%s) are -1...",
               main_symbol,
               host,
               IntegerToString(port),
               IntegerToString(account_id),
               IntegerToString(symbol_id)
            );
            
            return false;
         }
         else
         {
            CJAVal json_result;
            json_result["type"]           = "register_socket";
            json_result["account_id"]     = account_id;
            json_result["pair_id"]        = symbol_id;
            json_result["magic_number"]   = -1;
            json_result["client_type"]    = (int)client_type;

            _send(json_result.Serialize());

            PrintFormat("[%s] Socket (%s:%s) --> Register request is successful.",
               main_symbol,
               host,
               IntegerToString(port)
            );

            return true;
         }
      }

      void sendPing()
      {
         CJAVal json_result;
         json_result["type"]           = "ping";
         json_result["client_type"]    = (int)client_type;

         
         PrintFormat("[%s] (%s) Ping: %s",
            main_symbol,
            EnumToString(client_type),
            TimeToString(TimeCurrent(), TIME_DATE|TIME_SECONDS)
         );
         _send(json_result.Serialize());
      }

      datetime getLastCommunicationTime()
      {
         return last_communication;
      }

      void setLastCommunicationTime(datetime time)
      {
         last_communication = time;
      }
};

class TradeSocket : public Socket
{
   public:
      bool send(string data)
      {
         PrintFormat("[%s] (Trade Socket) Send: %s",
            main_symbol,
            data
         );
         return _send(data);
      } 

      void registerBroker()
      {
         CJAVal json_result;
         json_result["type"]  = "register_broker";
         json_result["name"]  = AccountInfoString(ACCOUNT_COMPANY); // Broker name
         json_result["pid"]   = 0;
         
         send(json_result.Serialize());

         PrintFormat("[%s] (Trade Socket) Message is sent to register of broker...",
            main_symbol
         );
      }

      void registerAccount()
      {
         CJAVal json_result;
         json_result["type"]     = "register_account";
         json_result["broker"]   = broker_id;
         json_result["owner"]    = AccountInfoString(ACCOUNT_NAME);
         json_result["number"]   = AccountInfoInteger(ACCOUNT_LOGIN);
         json_result["tta"]      = TerminalInfoInteger(TERMINAL_TRADE_ALLOWED) ? true : false; // "Check if automated trading is allowed in the terminal settings!"
         json_result["ate"]      = AccountInfoInteger(ACCOUNT_TRADE_EXPERT) ? true : false; // "Automated trading is forbidden for the account ",AccountInfoInteger(ACCOUNT_LOGIN)
         json_result["ata"]      = AccountInfoInteger(ACCOUNT_TRADE_ALLOWED) ? true : false; // "Trading is forbidden for the account ",AccountInfoInteger(ACCOUNT_LOGIN)
         json_result["b"]        = NormalizeDouble(AccountInfoDouble(ACCOUNT_BALANCE), 2);
         
         send(json_result.Serialize());

         PrintFormat("[%s] (Trade Socket) Message is sent to register of account...",
            main_symbol
         );
      }

      void registerSymbol()
      {
         CJAVal json_result;
         json_result["type"]        = "register_symbol";
         json_result["broker_id"]   = broker_id;
         json_result["symbol"]      = main_symbol; // symbol
         json_result["d"]           = _Digits; // digits
         json_result["cs"]          = SymbolInfoDouble(_Symbol, SYMBOL_TRADE_CONTRACT_SIZE); // contract size
         json_result["vm"]          = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MIN);
         json_result["vd"]          = countDecimal(SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MIN));

         send(json_result.Serialize());

         PrintFormat("[%s] (Trade Socket) Message is sent to register of symbol...",
            main_symbol
         );
      }

      void router(string data_str)
      {
         CJAVal data_json;
         data_json.Deserialize(data_str);

         string router = data_json["router"].ToStr();
         
         if  (router == "register_broker")
         {
            bool error = data_json["error"].ToBool();

            if(error)
            {
               string message = data_json["message"].ToStr();

               PrintFormat("[%s] (Trade Socket) Broker isn't registered. Error: %s",
                  main_symbol,
                  IntegerToString(broker_id),
                  message
               );
            }
            else
            {
               broker_id         = (int)data_json["broker_id"].ToInt();

               PrintFormat("[%s] (Trade Socket) Broker id is registered as #%s",
                  main_symbol,
                  IntegerToString(broker_id)
               );
            }
            
         }
         else if(router == "register_account")
         {
            bool error = data_json["error"].ToBool();

            if(error)
            {
               string message = data_json["message"].ToStr();

               PrintFormat("[%s] (Trade Socket) Account isn't registered. Error: %s",
                  main_symbol,
                  IntegerToString(broker_id),
                  message
               );
            }
            else
            {
               account_id         = (int)data_json["account_id"].ToInt();

               PrintFormat("[%s] (Trade Socket) Account id is registered as #%s",
                  main_symbol,
                  IntegerToString(account_id)
               );
            }
         }
         else if(router == "register_symbol")
         {
            bool error = data_json["error"].ToBool();

            if(error)
            {
               string message = data_json["message"].ToStr();

               PrintFormat("[%s] (Trade Socket) Symbol isn't registered. Error: %s",
                  main_symbol,
                  IntegerToString(broker_id),
                  message
               );
            }
            else
            {
               symbol_id         = (int)data_json["symbol_id"].ToInt(); 

               PrintFormat("[%s] (Trade Socket) Symbol id is registered as #%s",
                  main_symbol,
                  IntegerToString(symbol_id)
               );
            }
         }
         else if (router == "register_socket")
         {
            setRegisterStatus(data_json["register"].ToBool());
         }
          
      }

      void OnTimer()
      {
         if(isConnected())
         {
            if(broker_id == -1 && timer >= 1000)
            {
               registerBroker();
               timer = 0;
            }
            else if(account_id == -1 && timer >= 1000)
            {
               registerAccount();
               timer = 0;
            }
            else if(symbol_id == -1 && timer >= 1000)
            {
               registerSymbol();
               timer = 0;
            }
            else if(!getRegisterStatus() && timer >= 1000)
            {
               register();
               timer = 0;
            }
            
            
            string data[];
            bool anyData = receiveList(data);
            
            if(anyData)
            {
               for(int i=0; i<ArraySize(data); i++)
               {
                  setLastCommunicationTime(TimeCurrent());
                  PrintFormat("[%s] (Trade Socket) Recv: %s",
                     main_symbol,
                     data[i]
                  );

                  router(data[i]);
               }
            }

            if(getLastCommunicationTime() + 5 < TimeCurrent())
            {
               sendPing();
            }
         }
         else
         {
            if(timer >= 1000)
            {
               reconnect();
               timer = 0;

               broker_id = -1;
               account_id = -1;
               symbol_id = -1;
            }
         }
         
         timer += InpReceiveMS;
      }
};

class PriceSocket : public Socket
{
   public:
      bool send(string data)
      {
         PrintFormat("[%s] (Price Socket) Send: %s",
            main_symbol,
            data
         );
         return _send(data);
      } 

      bool sendPrice(double ask, double bid, datetime time)
      {
         CJAVal json_result;
         json_result["s_id"]  = symbol_id;
         json_result["type"]  = "update_tick";
         json_result["ask"]   = NormalizeDouble(ask, _Digits);
         json_result["bid"]   = NormalizeDouble(bid, _Digits);
         json_result["time"]  = (int) time;

         if(send(json_result.Serialize())) 
         {
            PrintFormat("[%s] (Price Socket) Tick data is sended. | Ask: %s, Bid: %s, Time: %s",
               main_symbol,
               DoubleToString(ask, _Digits),
               DoubleToString(bid, _Digits),
               TimeToString(time, TIME_DATE|TIME_SECONDS)
            );
            return true;
         }

         return false;
      }

      void router(string data_str)
      {
         CJAVal data_json;
         data_json.Deserialize(data_str);

         string router = data_json["router"].ToStr();
         
         if (router == "register_socket")
         {
            setRegisterStatus(data_json["register"].ToBool());
         }
      }

      void OnTimer()
      {
         if(isConnected())
         {
            if(broker_id != -1 && account_id != -1 && symbol_id != -1)
            {
               if(!getRegisterStatus() && timer >= 1000)
               {
                  register();
                  timer = 0;
               }
               
               string data[];
               bool anyData = receiveList(data);
               
               if(anyData)
               {
                  for(int i=0; i<ArraySize(data); i++)
                  {
                     PrintFormat("[%s] (Price Socket) Recv: %s",
                        main_symbol,
                        data[i]
                     );
   
                     router(data[i]);
                  }
               }
            }
         }
         else
         {
            if(timer > 1000)
            {
               reconnect();
               timer = 0;
            }
         }
         timer += InpReceiveMS;
      }

      void OnTick(MqlTick &tick)
      {
         if(isConnected())
         {
            if(getRegisterStatus() && broker_id != -1 && account_id != -1 && symbol_id != -1)
            {
               // sendPrice(tick.ask, tick.bid, tick.time);
            }
         }
      }
};

class OrderSocket : public Socket
{
   CJAVal orderSend(int order_id, int trade_type, double volume)
   {
      string debug;
      double price=0;

      if       (trade_type == OP_BUY)  price = NormalizeDouble(MarketInfo(_Symbol, MODE_BID), Digits);
      else if  (trade_type == OP_SELL) price = NormalizeDouble(MarketInfo(_Symbol, MODE_ASK), Digits);
      
      int ticket = OrderSend(
         _Symbol,
         trade_type,
         volume,
         price,
         0, // SLIPPAGE
         0, // STOP LOSS
         0, // TAKE PROFİT
         IntegerToString(order_id)
      );

      CJAVal json_result;
      json_result["type"]           = "order_send";
      // json_result["symbol_id"]      = symbol_id;
      // json_result["account_id"]     = account_id;
      json_result["order_id"]       = order_id;
      json_result["sended_price"]   = DoubleToString(price, _Digits);
      

      if(ticket == -1)
      {
         json_result["error"]    = true;
         json_result["code"]     = GetLastError();

         debug = StringFormat("[%s] (OrderSocket) orderSend --> Error: %s, Code: %s, Id: %s, Type: %s, Volume: %s, Price: %s",
            _Symbol,
            "true",
            IntegerToString(GetLastError()),
            IntegerToString(order_id),
            IntegerToString(trade_type),
            DoubleToString(volume, _Digits),
            DoubleToString(price, _Digits)
         );
      }
      else
      {
         json_result["error"]    = false;
         json_result["ticket"]   = ticket;

         if(OrderSelect(ticket, SELECT_BY_TICKET))
         {
            json_result["open_time"]   = (int) OrderOpenTime();
            json_result["open_price"]  = DoubleToString(OrderOpenPrice(), _Digits);
            json_result["volume"]      = DoubleToString(OrderLots(), volume_decimal_count);
            json_result["commission"]  = DoubleToString(OrderCommission(), 2);
         }

         SOrder order;
         order.id             = order_id;
         order.ticket         = ticket;
         order.sended_price   = price;
         AddValueInOrderList(order);

         debug = StringFormat("[%s] (OrderSocket) orderSend --> Error: %s, Ticket: %s, Id: %s, Type: %s, Volume: %s, Price: %s",
            _Symbol,
            "false",
            IntegerToString(ticket),
            IntegerToString(order_id),
            IntegerToString(trade_type),
            DoubleToString(volume, _Digits),
            DoubleToString(price, _Digits)
         );
      }

      Print(debug);

      return json_result;
   }

   CJAVal orderInfoByTicket(int ticket, int order_id)
   {
      CJAVal json_result;
      
      json_result["type"]     = "order_info_ticket";
      json_result["order_id"] = order_id;
      json_result["ticket"]   = ticket;
      json_result["error"]    = true;

      if(OrderSelect(ticket, SELECT_BY_TICKET))
      {
         json_result["error"]          = false;
         json_result["order_type"]     = (int) OrderType();
         json_result["open_price"]     = DoubleToString(OrderOpenPrice(), _Digits);
         json_result["open_time"]      = (int) OrderOpenTime();
         json_result["volume"]         = DoubleToString(OrderLots(), volume_decimal_count);
         json_result["commission"]     = DoubleToString(OrderCommission(), 2);
         json_result["swap"]           = DoubleToString(OrderSwap(), 2);
         json_result["profit"]         = DoubleToString(OrderProfit(), 2);
         json_result["closed_time"]    = (int) OrderCloseTime();
         json_result["closed_price"]   = DoubleToString(OrderClosePrice(), _Digits);

         if((int) OrderCloseTime() == 0)
         {
            SOrder order;
            order.id       = order_id;
            order.ticket   = ticket;
            AddValueInOrderList(order);
         }
      }
      
      return json_result;
   }

   CJAVal orderInfoById(int order_id)
   {
      CJAVal json_result;
      
      json_result["type"]     = "order_info_id";
      json_result["order_id"] = order_id;
      json_result["error"]    = true;

      int total_orders = OrdersTotal();
      for(int i=0; i<total_orders; i++)
      {
         if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
         {
            if(OrderComment() == IntegerToString(order_id))
            {
               json_result["error"]          = false;
               json_result["ticket"]         = OrderTicket();
               json_result["order_type"]     = (int) OrderType();
               json_result["open_price"]     = DoubleToString(OrderOpenPrice(), _Digits);
               json_result["open_time"]      = (int) OrderOpenTime();
               json_result["commission"]     = DoubleToString(OrderCommission(), 2);
               json_result["swap"]           = DoubleToString(OrderSwap(), 2);
               json_result["volume"]         = DoubleToString(OrderLots(), volume_decimal_count);
               json_result["profit"]         = DoubleToString(OrderProfit(), 2);
               json_result["closed_time"]    = (int) OrderCloseTime();
               json_result["closed_price"]   = DoubleToString(OrderClosePrice(), _Digits);
               
               SOrder order;
               order.id       = order_id;
               order.ticket   = OrderTicket();
               AddValueInOrderList(order);
               
               break;
            }
         }
      }

      if(json_result["error"])
      {
         int total_history_orders = OrdersHistoryTotal();
         for(int ii=0; ii<total_history_orders; ii++)
         {
            if(OrderSelect(ii, SELECT_BY_POS, MODE_HISTORY))
            {
               if(OrderComment() == IntegerToString(order_id))
               {
                  json_result["error"]          = false;
                  json_result["ticket"]         = OrderTicket();
                  json_result["order_type"]     = (int) OrderType();
                  json_result["open_price"]     = DoubleToString(OrderOpenPrice(), _Digits);
                  json_result["open_time"]      = (int) OrderOpenTime(); 
                  json_result["commission"]     = DoubleToString(OrderCommission(), 2);
                  json_result["swap"]           = DoubleToString(OrderSwap(), 2);
                  json_result["volume"]         = DoubleToString(OrderLots(), volume_decimal_count);
                  json_result["profit"]         = DoubleToString(OrderProfit(), 2);
                  json_result["closed_time"]    = (int) OrderCloseTime();
                  json_result["closed_price"]   = DoubleToString(OrderClosePrice(), _Digits);
                  
                  break;
               }
            }
         }
      }
      
      return json_result;
   }

   CJAVal orderCloseByTicket(int order_id, int ticket)
   {
      CJAVal json_result;
      
      json_result["type"]     = "order_close";
      json_result["order_id"] = order_id;
      json_result["error"]    = true;

      if(OrderSelect(ticket, SELECT_BY_TICKET) == true)
      {
         double closePrice=0;

         if       (OrderType() == OP_BUY)  closePrice = NormalizeDouble(MarketInfo(OrderSymbol(), MODE_BID), Digits);
         else if  (OrderType() == OP_SELL) closePrice = NormalizeDouble(MarketInfo(OrderSymbol(), MODE_ASK), Digits);
         
         ResetLastError();
         
         bool error              = !OrderClose(ticket, OrderLots(), closePrice, 0, CLR_NONE);
         json_result["error"]    = error;
         json_result["code"]     = GetLastError();

         if(!error) 
         {
            RemoveValueFromOrderList(order_id);

            CJAVal json_order_info        = orderInfoByTicket(ticket, order_id);
            json_result["profit"]         = json_order_info["profit"];
            json_result["swap"]           = json_order_info["swap"];
            json_result["commission"]     = json_order_info["commission"];
            json_result["closed_time"]    = json_order_info["closed_time"];
            json_result["closed_price"]   = json_order_info["closed_price"];
         }
         else
         {
            CJAVal json_order_info        = orderInfoByTicket(ticket, order_id);
            if(!json_order_info["error"].ToBool())
            {
               int closed_time = json_order_info["closed_time"].ToInt();
               if(closed_time > 0)
               {
                  json_result["error"]          = false;
                  json_result["profit"]         = json_order_info["profit"];
                  json_result["swap"]           = json_order_info["swap"];
                  json_result["commission"]     = json_order_info["commission"];
                  json_result["closed_time"]    = json_order_info["closed_time"];
                  json_result["closed_price"]   = json_order_info["closed_price"];
               }
            }
         }
      }
      
      return json_result;
   }

   CJAVal orderCloseById(int order_id)
   {
      bool issetOrder = false;
      CJAVal json_result;

      int total_orders = OrdersTotal();
      for(int i=0; i<total_orders; i++)
      {
         if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
         {
            if(OrderComment() == IntegerToString(order_id))
            {
               issetOrder = true;
               int ticket = OrderTicket();
               json_result = orderCloseByTicket(order_id, ticket);
               break;
            }
         }

      }

      if(!issetOrder)
      {
         json_result["type"]     = "order_close";
         json_result["order_id"] = order_id;
         json_result["error"]    = true;
      }
      
      return json_result;
   }

   bool isOrderOpen(int order_id)
   {
      int total_orders = OrdersTotal();
      for(int i=0; i<total_orders; i++)
      {
         if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
         {
            if(OrderComment() == IntegerToString(order_id))
            {
               return true;
            }
         }
      }

      return false;
   }

   public: 
      bool send(string data)
      {
         PrintFormat("[%s] (Order Socket) Send: %s",
            main_symbol,
            data
         );
         return _send(data);
      } 
      
      void router(string data_str)
      {
         CJAVal data_json;
         data_json.Deserialize(data_str);

         string router = data_json["router"].ToStr();
         
         if (router == "register_socket")
         {
            setRegisterStatus(data_json["register"].ToBool());
         }
         else if  (router == "order_send")
         {
            int      order_id    = (int)data_json["order_id"].ToInt(); 
            if(isOrderOpen(order_id))
            {
               CJAVal json_result = orderInfoById(order_id);
               send(json_result.Serialize());
            }
            else
            {
               int      trade_type  = (int)data_json["trade_type"].ToInt();
               double   volume      = (double)data_json["volume"].ToDbl();

               CJAVal json_result = orderSend(order_id, trade_type, volume);
               send(json_result.Serialize());
            }
         }
         else if  (router == "order_info_ticket")
         {
            int      order_id       = (int)data_json["order_id"].ToInt(); 
            int      order_ticket   = (int)data_json["order_ticket"].ToInt();

            CJAVal json_result = orderInfoByTicket(order_ticket, order_id);
            send(json_result.Serialize());
         }
         else if  (router == "order_info_id")
         {
            int      order_id       = (int)data_json["order_id"].ToInt(); 

            CJAVal json_result = orderInfoById(order_id);
            send(json_result.Serialize());
         }
         else if  (router == "order_close_ticket")
         {
            int      order_id       = (int)data_json["order_id"].ToInt(); 
            int      order_ticket   = (int)data_json["order_ticket"].ToInt();

            CJAVal json_result = orderCloseByTicket(order_id, order_ticket);
            Print(json_result.Serialize());
            send(json_result.Serialize());
         }
         else if  (router == "order_close_id")
         {
            int      order_id       = (int) data_json["order_id"].ToInt(); 

            CJAVal json_result = orderCloseById(order_id);
            Print(json_result.Serialize());
            send(json_result.Serialize());
         }
      }

      void OnTimer()
      {
         if(isConnected())
         {
            if(broker_id != -1 && account_id != -1 && symbol_id != -1)
            {
               if(!getRegisterStatus() && timer >= 1000)
               {
                  register();
                  timer = 0;
               }

               string data[];
               bool anyData = receiveList(data);
               
               if(anyData)
               {
                  for(int i=0; i<ArraySize(data); i++)
                  {
                     setLastCommunicationTime(TimeCurrent());
                     PrintFormat("[%s] (Order Socket) Recv: %s",
                        main_symbol,
                        data[i]
                     );
   
                     router(data[i]);
                  }
               }

               if(getLastCommunicationTime() + 5 < TimeCurrent())
               {
                  sendPing();
               }
            }
         }
         else
         {
            if(timer >= 1000)
            {
               reconnect();
               timer = 0;
            }
         }
         
         timer += InpReceiveMS;
      }

      void orderTickUpdate(SOrder &order)
      {
         CJAVal json_result;
      
         json_result["type"]        = "order_tick_update";
         json_result["order_id"]    = order.id;
         json_result["ticket"]      = order.ticket;
         json_result["pair_id"]     = symbol_id;
         json_result["account_id"]  = account_id;
         json_result["volume"]      = DoubleToString(order.volume, volume_decimal_count);
         json_result["swap"]        = DoubleToString(order.swap, 2);
         json_result["profit"]      = DoubleToString(order.profit, 2);
         json_result["commission"]  = DoubleToString(order.commission, 2);

         send(json_result.Serialize());
      }
};

TradeSocket tradeSocket;
PriceSocket priceSocket;
OrderSocket orderSocket;

MqlTick last_tick;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
   int sleep_seconds = 0 + (5000*MathRand())/32768;
   Sleep(sleep_seconds);

   volume_decimal_count = countDecimal(SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MIN));
   
   if(custom_symbol_bool)  main_symbol = custom_symbol;
   else                    main_symbol = _Symbol;

   if(InpSocketActive)
   {
      tradeSocket.initialize(InpHostname, InpTradePort, InpBufferSize, SOCKET_CLIENT_TYPE_TRADE);
      tradeSocket.connect();

      // priceSocket.initialize(InpHostname, InpPricePort, InpBufferSize, SOCKET_CLIENT_TYPE_PRICE);
      // priceSocket.connect();

      orderSocket.initialize(InpHostname, InpOrderPort, InpBufferSize, SOCKET_CLIENT_TYPE_ORDER);
      orderSocket.connect();
   }

   EventSetMillisecondTimer(InpReceiveMS);

   return(INIT_SUCCEEDED);
}
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
   EventKillTimer();

   tradeSocket.disconnect();
   priceSocket.disconnect();
   orderSocket.disconnect();
}

void OnTimer()
{
   tradeSocket.OnTimer();
   // priceSocket.OnTimer();
   orderSocket.OnTimer();
}

void OnTick()
{
   MqlTick new_tick;
   
   if(SymbolInfoTick(_Symbol,new_tick))
   {
      // priceSocket.OnTick(new_tick);

      if(orderSocket.getRegisterStatus() && symbol_id != -1 && account_id != -1 && symbol_id != -1)
      {
         for(int i=0; i<ArraySize(order_list); i++)
         {
            if(OrderSelect(order_list[i].ticket, SELECT_BY_TICKET))
            {
               if(order_list[i].volume != OrderLots() || order_list[i].swap != OrderSwap() || order_list[i].profit != OrderProfit() || order_list[i].commission != OrderCommission())
               {
                  order_list[i].volume      = OrderLots();
                  order_list[i].swap        = OrderSwap();
                  order_list[i].profit      = OrderProfit();
                  order_list[i].commission  = OrderCommission();

                  orderSocket.orderTickUpdate(order_list[i]);
                  
                  if(OrderCloseTime() > 0) 
                  {
                     RemoveValueFromOrderList(order_list[i]);
                  }
                  Sleep(5);
               }
            }
         }
      }
   }
}

int countDecimal(double val)
{
  int digits=0;
  while(NormalizeDouble(val,digits)!=NormalizeDouble(val,8)) digits++;
  return digits;
}

void AddValueInOrderList(SOrder &order)
{
   bool isExists = false;

   int size = ArraySize(order_list);
   for(int i=0; i<size; i++)
   {
      if(order_list[i].id == order.id)
      {
         isExists = true;
         break;
      }
   }
   
   if(!isExists)
   {
      ArrayResize(order_list, size + 1);
      order_list[size] = order;
   }
}

void RemoveValueFromOrderList(SOrder &order)
{
   bool isShiftOn = false;
   for(int i=0; i < ArraySize(order_list) - 1; i++) 
   {
      if(order_list[i].id == order.id) 
      {
         isShiftOn = true;
      }

      if(isShiftOn == true) 
      {
         order_list[i] = order_list[i + 1];
      }
   }

   ArrayResize(order_list, ArraySize(order_list) - 1);
}

void RemoveValueFromOrderList(int order_id)
{
   bool isShiftOn = false;
   for(int i=0; i < ArraySize(order_list) - 1; i++) 
   {
      if(order_list[i].id == order_id) 
      {
         isShiftOn = true;
      }

      if(isShiftOn == true) 
      {
         order_list[i] = order_list[i + 1];
      }
   }

   ArrayResize(order_list, ArraySize(order_list) - 1);
}


