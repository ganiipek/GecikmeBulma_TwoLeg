#include <GecikmeBulma/JAson.mqh>
#include <GecikmeBulma/socket-library-mt4-mt5.mqh>
#include <GecikmeBulma/Others.mqh>
#include <Trade/Trade.mqh>

#define program_version "1.1"
#property link      "https://yalcinex.com/"
#property version   program_version
#property description "Gecikme Bulma"
#property copyright "2022 © Yalcin"
#property strict



string developer_settings              = ""; // <------ DEVELOPER SETTINGS ------>
bool developer_debug                   = false; // Debug

input group      "<------------ TESTER SETTINGS ------------>";
input string     custom_symbol_real_1 = "US30";                // Custom Real 1
input string     custom_symbol_1      = "US30";                // Custom Symbol 1
input string     custom_broker_1      = "Raw Trading Ltd";     // Custom Broker 1 (For only strategy testers)
input string     custom_symbol_real_2 = "US30_FXOpen";         // Custom Real 2 (For only strategy testers)
input string     custom_symbol_2      = "US30";                // Custom Symbol 2 (For only strategy testers)
input string     custom_broker_2      = "FXOpen Ltd";          // Custom Broker 2 (For only strategy testers)

input group      "<------------- SOCKET SETTINGS ------------->";
input bool       InpSocketActive      = true;         // Socket Activated
input string     InpHostname          = "127.0.0.1";  // Server hostname or IP address
input ushort     InpPricePort         = 3131;         // Price port
input ushort     InpOrderPort         = 3169;         // Order port
input ushort     InpTradePort         = 6969;         // Trade port
input int        InpBufferSize        = 512;          // Buffer Size
input int        InpReceiveMS         = 10;           // Receive Milliseconds


struct SOrder
{
   int      id;
   int      ticket;
   string   symbol;
   int      arbitrage_id;
   double   volume;
   double   swap;
   double   profit;
   double   commission;
   double   sended_price;

   void SOrder()
   {
      id             = 0;
      ticket         = 0;
      symbol         = "";
      arbitrage_id   = 0;
      volume         = 0;
      swap           = 0;
      profit         = 0;
      commission     = 0;
      sended_price   = 0;
   }
};


int volume_decimal_count = 0;

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
      int broker_id;
      int account_id;
      int symbol_id;
      string broker_name;

      string symbol_real;
      string symbol_custom;

      int timer;
      ClientSocket * glbClientSocket;

      Socket()
      {
         registered = false;
         timer    = 0;

         broker_id  = -1;
         account_id = -1;
         symbol_id  = -1;

         symbol_real    = NULL;
         symbol_custom  = NULL;
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
               symbol_custom,
               host,
               IntegerToString(port)
            );

            glbClientSocket = new ClientSocket(host, port);
            glbClientSocket.SendBufferSize = buffer_size;
            glbClientSocket.ReceiveBufferSize = buffer_size;
            
            if (isConnected()) 
            {
               PrintFormat("[%s] Socket connection is successful. (%s:%s)",
                  symbol_custom,
                  host,
                  IntegerToString(port)
               );

               return true;
            } 
            else 
            {
               PrintFormat("[%s] Socket connection is unsuccessful. (%s:%s)",
                  symbol_custom,
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
            symbol_custom,
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
               symbol_custom,
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
               symbol_custom,
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

         
         // PrintFormat("[%s] (%s) Ping: %s",
         //    symbol_custom,
         //    EnumToString(client_type),
         //    TimeToString(TimeCurrent(), TIME_DATE|TIME_SECONDS)
         // );
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
            symbol_custom,
            data
         );
         return _send(data);
      } 

      void registerBroker()
      {
         CJAVal json_result;
         json_result["type"]  = "register_broker";
         json_result["name"]  = broker_name;//AccountInfoString(ACCOUNT_COMPANY); // Broker name
         json_result["pid"]   = 3;
         
         send(json_result.Serialize());

         PrintFormat("[%s] (Trade Socket) Message is sent to register of broker...",
            symbol_custom
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
            symbol_custom
         );
      }

      void registerSymbol()
      {
         CJAVal json_result;
         json_result["type"]        = "register_symbol";
         json_result["broker_id"]   = broker_id;
         json_result["symbol"]      = symbol_custom; // symbol
         json_result["d"]           = _Digits; // digits
         json_result["cs"]          = SymbolInfoDouble(symbol_real, SYMBOL_TRADE_CONTRACT_SIZE); // contract size
         json_result["vm"]          = SymbolInfoDouble(symbol_real, SYMBOL_VOLUME_MIN);
         json_result["vd"]          = countDecimal(SymbolInfoDouble(symbol_real, SYMBOL_VOLUME_MIN));

         send(json_result.Serialize());

         PrintFormat("[%s] (Trade Socket) Message is sent to register of symbol...",
            symbol_custom
         );
      }

      void router(string data_str)
      {
         CJAVal data_json;
         data_json.Deserialize(data_str);

         bool   socket_debug  = false;
         string send_debug    = "";

         string router = data_json["router"].ToStr();
         
         if  (router == "register_broker")
         {
            socket_debug = true;
            bool error = data_json["error"].ToBool();

            if(error)
            {
               string message = data_json["message"].ToStr();

               send_debug     = StringFormat("Broker isn't registered. Error: %s",
                  message
               );
            }
            else
            {
               broker_id         = (int)data_json["broker_id"].ToInt();

               send_debug        = StringFormat("Broker id is registered as #%s",
                  IntegerToString(broker_id)
               );
            }
            
         }
         else if(router == "register_account")
         {
            socket_debug = true;
            bool error = data_json["error"].ToBool();

            if(error)
            {
               string message = data_json["message"].ToStr();

               send_debug     = StringFormat("Account isn't registered. Error: %s",
                  message
               );
            }
            else
            {
               account_id         = (int)data_json["account_id"].ToInt();

               send_debug         = StringFormat("Account id is registered as #%s",
                  IntegerToString(account_id)
               );
            }
         }
         else if(router == "register_symbol")
         {
            socket_debug = true;
            bool error = data_json["error"].ToBool();

            if(error)
            {
               string message = data_json["message"].ToStr();

               send_debug     = StringFormat("Symbol isn't registered. Error: %s",
                  message
               );
            }
            else
            {
               symbol_id         = (int)data_json["symbol_id"].ToInt(); 
                
               send_debug        = StringFormat("Symbol id is registered as #%s",
                  IntegerToString(symbol_id)
               );
            }
         }
         else if (router == "register_socket")
         {
            socket_debug = true;
            setRegisterStatus(data_json["register"].ToBool());
         }
          
         if(socket_debug)
         {
            PrintFormat("[%s] (Trade Socket) Recv: %s",
               symbol_custom,
               data_str
            );
            PrintFormat("[%s] (Trade Socket) Send: %s",
               symbol_custom,
               send_debug
            );
         }
      }

      void onTimer()
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
         // PrintFormat("[%s] (Price Socket) Send: %s",
         //    symbol_custom,
         //    data
         // );
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
            // PrintFormat("[%s] (Price Socket) Tick data is sended. | Ask: %s, Bid: %s, Time: %s",
            //    symbol_custom,
            //    DoubleToString(ask, _Digits),
            //    DoubleToString(bid, _Digits),
            //    TimeToString(time, TIME_DATE|TIME_SECONDS)
            // );
            return true;
         }

         return false;
      }

      void router(string data_str)
      {
         CJAVal data_json;
         data_json.Deserialize(data_str);

         bool   socket_debug  = false;
         string send_debug    = "";

         string router = data_json["router"].ToStr();
         
         if (router == "register_socket")
         {
            socket_debug = true;
            setRegisterStatus(data_json["register"].ToBool());
         }

         if(socket_debug)
         {
            PrintFormat("[%s] (Price Socket) Recv: %s",
               symbol_custom,
               data_str
            );
            PrintFormat("[%s] (Price Socket) Send: %s",
               symbol_custom,
               send_debug
            );
         }
      }

      void onTimer()
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

      void onTick(MqlTick &tick)
      {
         if(isConnected())
         {
            if(getRegisterStatus() && broker_id != -1 && account_id != -1 && symbol_id != -1)
            {
               sendPrice(tick.ask, tick.bid, tick.time);
            }
         }
      }
};

class OrderSocket : public Socket
{
   CJAVal orderSend(int order_id, int arbitrage_id, ENUM_ORDER_TYPE order_type, double volume)
   {
      MqlTradeRequest request = {};
      MqlTradeResult  result  = {};
      
      string   debug;
      bool     error = true;
      
      double order_price = 0;
      if       (order_type == ORDER_TYPE_BUY)  order_price = NormalizeDouble(SymbolInfoDouble(symbol_real, SYMBOL_ASK), _Digits);
      else if  (order_type == ORDER_TYPE_SELL) order_price = NormalizeDouble(SymbolInfoDouble(symbol_real, SYMBOL_BID), _Digits);
      
      request.action          = TRADE_ACTION_DEAL;
      request.price           = order_price;
      request.symbol          = symbol_real;
      request.type            = order_type;
      request.volume          = volume;
      request.type_filling    = GetFilling(symbol_real);
      request.comment         = IntegerToString(order_id) + "#" + IntegerToString(arbitrage_id);
      
      CJAVal json_result;
      json_result["type"]           = "order_send";
      // json_result["symbol_id"]      = symbol_id;
      // json_result["account_id"]     = account_id;
      json_result["order_id"]       = order_id;
      json_result["sended_price"]   = DoubleToString(order_price, _Digits);
      
      if(OrderSend(request, result))
      {
         if (result.retcode == 10009)
         {
            error = false;
            json_result["error"]       = false;
            json_result["ticket"]      = (int) result.order;
            json_result["open_time"]   = 0;
            json_result["open_price"]  = result.price;
            json_result["volume"]      = result.volume;
            json_result["commission"]  = 0;
            
            SOrder order;
            order.id             = order_id;
            order.ticket         = (int) result.order;
            order.sended_price   = order_price;
            order.symbol         = symbol_real;
            AddValueInOrderList(order);
   
            debug = StringFormat("[%s] (OrderSocket) orderSend --> Error: %s, Ticket: %s, Id: %s, Arbitrage Id: %s, Type: %s, Volume: %s, Price: %s",
               symbol_custom,
               "false",
               IntegerToString(result.order),
               IntegerToString(order_id),
               IntegerToString(arbitrage_id),
               IntegerToString(order_type),
               DoubleToString(volume, _Digits),
               DoubleToString(order_price, _Digits)
            );
         }
      }
      
      if(error)
      {
         json_result["error"]    = true;
         json_result["code"]     = GetLastError();

         debug = StringFormat("[%s] (OrderSocket) orderSend --> Error: %s, Code: %s, Id: %s, Arbitrage Id: %s, Type: %s, Volume: %s, Price: %s",
            symbol_custom,
            "true",
            IntegerToString(GetLastError()),
            IntegerToString(order_id),
            IntegerToString(arbitrage_id),
            IntegerToString(order_type),
            DoubleToString(volume, _Digits),
            DoubleToString(order_price, _Digits)
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
      
      if(PositionSelectByTicket(ticket))
      {
         json_result["error"]          = false;
         json_result["order_type"]     = PositionGetInteger(POSITION_TYPE);
         json_result["open_price"]     = DoubleToString(PositionGetDouble(POSITION_PRICE_OPEN), _Digits);
         json_result["open_time"]      = PositionGetInteger(POSITION_TIME);
         json_result["volume"]         = DoubleToString(PositionGetDouble(POSITION_VOLUME), volume_decimal_count);
         json_result["commission"]     = DoubleToString(getComission(ticket), 2);
         json_result["swap"]           = DoubleToString(PositionGetDouble(POSITION_SWAP), 2);
         json_result["profit"]         = DoubleToString(PositionGetDouble(POSITION_PROFIT), 2);
         json_result["closed_time"]    = 0;
         json_result["closed_price"]   = 0;
         
         SOrder order;
         order.id       = order_id;
         order.ticket   = ticket;
         AddValueInOrderList(order);
      }
      else if(HistorySelectByPosition(ticket))
      {
         int total_deals = HistoryDealsTotal();
         
         if(total_deals > 1)
         {
            json_result["error"]          = false;
            double commission             = 0;
            
            for(int i=0; i<total_deals; i++)
            {
               long deal_ticket            = (long) HistoryDealGetTicket(i);
               ENUM_DEAL_ENTRY deal_entry  = (ENUM_DEAL_ENTRY) HistoryDealGetInteger(deal_ticket, DEAL_ENTRY);
               
               if(deal_entry == DEAL_ENTRY_IN)
               {
                  json_result["order_type"]     = HistoryDealGetInteger(deal_ticket, DEAL_TYPE);
                  json_result["open_time"]      = HistoryDealGetInteger(deal_ticket, DEAL_TIME);
                  json_result["open_price"]     = DoubleToString(HistoryDealGetDouble(deal_ticket, DEAL_PRICE), _Digits);
                  json_result["volume"]         = DoubleToString(HistoryDealGetDouble(deal_ticket, DEAL_VOLUME), volume_decimal_count);
                  json_result["swap"]           = DoubleToString(HistoryDealGetDouble(deal_ticket, DEAL_SWAP), 2);
                  commission                    += NormalizeDouble(HistoryDealGetDouble(deal_ticket, DEAL_COMMISSION), 2);
               }
               else if(deal_entry == DEAL_ENTRY_OUT)
               {
                  json_result["closed_time"]    = HistoryDealGetInteger(deal_ticket, DEAL_TIME);
                  json_result["closed_price"]   = DoubleToString(HistoryDealGetDouble(deal_ticket, DEAL_PRICE), _Digits);
                  commission                    += NormalizeDouble(HistoryDealGetDouble(deal_ticket, DEAL_COMMISSION), 2);
                  json_result["profit"]         = DoubleToString(HistoryDealGetDouble(deal_ticket, DEAL_PROFIT), 2);
               }
               
            }
            json_result["commission"] = commission;
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

      int total_positions = PositionsTotal();
      for(int i=0; i < total_positions; i++)
      {
         long     ticket   = (long) PositionGetTicket(i);
         string   symbol   = PositionGetString(POSITION_SYMBOL);
         long     magic    = PositionGetInteger(POSITION_MAGIC);
         string   comment  = PositionGetString(POSITION_COMMENT);
            
         if(comment == IntegerToString(order_id))
         {
            json_result["error"]          = false;
            json_result["ticket"]         = ticket;
            json_result["order_type"]     = PositionGetInteger(POSITION_TYPE);
            json_result["open_price"]     = DoubleToString(PositionGetDouble(POSITION_PRICE_OPEN), _Digits);
            json_result["open_time"]      = PositionGetInteger(POSITION_TIME);
            json_result["commission"]     = DoubleToString(getComission(ticket), 2);
            json_result["swap"]           = DoubleToString(PositionGetDouble(POSITION_SWAP), 2);
            json_result["volume"]         = DoubleToString(PositionGetDouble(POSITION_VOLUME), volume_decimal_count);
            json_result["profit"]         = DoubleToString(PositionGetDouble(POSITION_PROFIT), 2);
            json_result["closed_time"]    = 0;
            json_result["closed_price"]   = 0;
            
            SOrder order;
            order.id       = order_id;
            order.ticket   = (int) ticket;
            AddValueInOrderList(order);
            
            break;
         }
      }

      if(json_result["error"])
      {
         HistorySelect(0, TimeCurrent());
         
         int total_history_orders = HistoryDealsTotal();
         for(int ii = total_history_orders - 1; ii >= 0; ii--)
         {
            long              deal_ticket    = (long) HistoryDealGetTicket(ii);
            ENUM_DEAL_ENTRY   deal_entry     = (ENUM_DEAL_ENTRY) HistoryDealGetInteger(deal_ticket, DEAL_ENTRY);
            string            deal_comment   =  HistoryDealGetString(deal_ticket, DEAL_COMMENT);
            
            if(deal_comment == IntegerToString(order_id))
            {
               long position_id = HistoryDealGetInteger(deal_ticket, DEAL_POSITION_ID);
               if(HistorySelectByPosition(position_id))
               {
                  int total_deals = HistoryDealsTotal();
                  
                  if(total_deals > 1)
                  {
                     json_result["error"]          = false;
                     double commission             = 0;
                     
                     for(int iii=0; iii<total_deals; iii++)
                     {
                        long deal_ticket_iii             = (long) HistoryDealGetTicket(iii);
                        ENUM_DEAL_ENTRY deal_entry_iii   = (ENUM_DEAL_ENTRY) HistoryDealGetInteger(deal_ticket_iii, DEAL_ENTRY);
                        
                        if(deal_entry_iii == DEAL_ENTRY_IN)
                        {
                           json_result["ticket"]         = HistoryDealGetInteger(deal_ticket_iii, DEAL_ORDER);
                           json_result["order_type"]     = HistoryDealGetInteger(deal_ticket_iii, DEAL_TYPE);
                           json_result["open_time"]      = HistoryDealGetInteger(deal_ticket_iii, DEAL_TIME);
                           json_result["open_price"]     = DoubleToString(HistoryDealGetDouble(deal_ticket_iii, DEAL_PRICE), _Digits);
                           json_result["volume"]         = DoubleToString(HistoryDealGetDouble(deal_ticket_iii, DEAL_VOLUME), volume_decimal_count);
                           json_result["swap"]           = DoubleToString(HistoryDealGetDouble(deal_ticket_iii, DEAL_SWAP), 2);
                           commission                    = NormalizeDouble(HistoryDealGetDouble(deal_ticket_iii, DEAL_COMMISSION), 2);
                        }
                        else if(deal_entry_iii == DEAL_ENTRY_OUT)
                        {
                           json_result["closed_time"]    = HistoryDealGetInteger(deal_ticket_iii, DEAL_TIME);
                           json_result["closed_price"]   = DoubleToString(HistoryDealGetDouble(deal_ticket_iii, DEAL_PRICE), _Digits);
                           commission                    = NormalizeDouble(HistoryDealGetDouble(deal_ticket_iii, DEAL_COMMISSION), 2);
                           json_result["profit"]         = DoubleToString(HistoryDealGetDouble(deal_ticket_iii, DEAL_PROFIT), 2);
                        }
                        
                     }
                     json_result["commission"] = commission;
                  }
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
      
      if(PositionSelectByTicket(ticket))
      {
         CTrade trade;
         trade.SetAsyncMode(true);
         if (trade.PositionClose(ticket))
         {
            json_result["error"]    = false;
         }
         else
         {
            json_result["code"]    = (int) trade.ResultRetcode();
         }
      }
      
      
      
      
      
      
      
      /*
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
      */
      return json_result;
   }

   CJAVal orderCloseById(int order_id)
   {
      bool issetOrder = false;
      CJAVal json_result;
      
      int total_positions = PositionsTotal();
      for(int i=0; i < total_positions; i++)
      {
         long     ticket   = (long) PositionGetTicket(i);
         string   comment  = PositionGetString(POSITION_COMMENT);
            
         if(comment == IntegerToString(order_id))
         {
            issetOrder = true;
            json_result = orderCloseByTicket(order_id, ticket);
            break;
         }
      }

      if(!issetOrder)
      {
         json_result = orderInfoById(order_id);
      }
      
      return json_result;
   }

   CJAVal orderCloseByArbitrageId(int arbitrage_id)
   {
      bool issetOrder = false;
      CJAVal json_result;
      
      int total_positions = PositionsTotal();
      for(int i=0; i < total_positions; i++)
      {
         long     ticket   = (long) PositionGetTicket(i);
         string   comment  = PositionGetString(POSITION_COMMENT);
            
         string comment_array[];
         commentSep(comment, comment_array, "#");

         if(ArraySize(comment_array) > 1 && comment_array[1] == IntegerToString(arbitrage_id))
         {
            issetOrder = true;
            json_result = orderCloseByTicket(comment_array[0], ticket);
         }
      }

      if(!issetOrder)
      {
         json_result["error"]          = false;
         json_result["arbitrage_id"]   = arbitrage_id;
      }
      
      return json_result;
   }

   bool isOrderOpen(int order_id)
   {
      int total_positions = PositionsTotal();
      for(int i=0; i < total_positions; i++)
      {
         long     ticket   = (long) PositionGetTicket(i);
         string   comment  = PositionGetString(POSITION_COMMENT);

         string comment_array[];
         commentSep(comment, comment_array, "#");
            
         if(ArraySize(comment_array) > 1 && comment_array[0] == IntegerToString(order_id))
         {
            return true;
         }
      }

      return false;
   }

   public: 
      bool send(string data)
      {
         // PrintFormat("[%s] (Order Socket) Send: %s",
         //    symbol_custom,
         //    data
         // );
         return _send(data);
      } 
      
      void router(string data_str)
      {
         CJAVal data_json;
         data_json.Deserialize(data_str);

         string router        = data_json["router"].ToStr();
         bool   socket_debug  = false;
         string send_debug    = "";

         if (router == "register_socket")
         {
            socket_debug = true;
            setRegisterStatus(data_json["register"].ToBool());
         }
         else if  (router == "order_send")
         {
            socket_debug = true;
            int      order_id    = (int)data_json["order_id"].ToInt(); 
            if(isOrderOpen(order_id))
            {
               CJAVal json_result = orderInfoById(order_id);
               send(json_result.Serialize());

               send_debug = json_result.Serialize();
            }
            else
            {
               ENUM_ORDER_TYPE   order_type           = (ENUM_ORDER_TYPE) data_json["trade_type"].ToInt();
               double            order_volume         = (double)data_json["volume"].ToDbl();
               int               order_arbitrage_id   = (int)data_json["arbitrage_id"].ToInt();

               CJAVal json_result = orderSend(order_id, order_arbitrage_id, order_type, order_volume);
               send(json_result.Serialize());

               send_debug = json_result.Serialize();
            }
         }
         else if  (router == "order_info_ticket")
         {
            socket_debug = true;
            int      order_id       = (int)data_json["order_id"].ToInt(); 
            int      order_ticket   = (int)data_json["order_ticket"].ToInt();

            CJAVal json_result = orderInfoByTicket(order_ticket, order_id);
            send(json_result.Serialize());

            send_debug = json_result.Serialize();
         }
         else if  (router == "order_info_id")
         {
            socket_debug = true;
            int      order_id       = (int)data_json["order_id"].ToInt(); 

            CJAVal json_result = orderInfoById(order_id);
            send(json_result.Serialize());

            send_debug = json_result.Serialize();
         }
         else if  (router == "order_close_ticket")
         {
            socket_debug = true;
            int      order_id       = (int)data_json["order_id"].ToInt(); 
            int      order_ticket   = (int)data_json["order_ticket"].ToInt();

            CJAVal json_result = orderCloseByTicket(order_id, order_ticket);
            // send(json_result.Serialize());

            send_debug = json_result.Serialize();
         }
         else if  (router == "order_close_id")
         {
            socket_debug = true;
            int      order_id       = (int) data_json["order_id"].ToInt(); 

            CJAVal json_result = orderCloseById(order_id);
            // send(json_result.Serialize());

            send_debug = json_result.Serialize();
         }
         else if  (router == "order_close_arbitrage_id")
         {
            socket_debug = true;
            int arbitrage_id = (int) data_json["arbitrage_id"].ToInt(); 

            CJAVal json_result = orderCloseByArbitrageId(arbitrage_id);
            // send(json_result.Serialize());

            send_debug = json_result.Serialize();
         }


         if(socket_debug)
         {
            PrintFormat("[%s] (Order Socket) Recv: %s",
               symbol_custom,
               data_str
            );
            PrintFormat("[%s] (Order Socket) Send: %s",
               symbol_custom,
               send_debug
            );
         }
      }

      void onTimer()
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

      void arbitrageTickUpdate(int arbitrage_id, double profit)
      {
         CJAVal json_result;
      
         json_result["type"]              = "arbitrage_profit_update";
         json_result["arbitrage_id"]      = arbitrage_id;
         json_result["pair_id"]           = symbol_id;
         json_result["profit"]            = profit;
         
         send(json_result.Serialize());
      }
};


TradeSocket tradeSocket;
PriceSocket priceSocket;
OrderSocket orderSocket;

TradeSocket tradeSocket_2;
PriceSocket priceSocket_2;
OrderSocket orderSocket_2;

MqlTick last_tick;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit(void)
{
   int sleep_seconds = 0 + (5000*MathRand())/32768;
   Sleep(sleep_seconds);
   
   if (MQLInfoInteger(MQL_TESTER))
   {
      if(iCustom(custom_symbol_real_1, PERIOD_M1, "Difference Bot v2/difference_bot_v2", ChartID(), 0) == INVALID_HANDLE) 
      { 
      PrintFormat("Error in setting of spy on %s", custom_symbol_real_1); 
      return INIT_FAILED;
      }

      if(iCustom(custom_symbol_real_2, PERIOD_M1, "Difference Bot v2/difference_bot_v2", ChartID(), 1) == INVALID_HANDLE) 
      { 
      PrintFormat("Error in setting of spy on %s", custom_symbol_real_2); 
      return INIT_FAILED;
      }

      tradeSocket_2.symbol_real   = custom_symbol_real_2;
      tradeSocket_2.symbol_custom = custom_symbol_2;
      tradeSocket_2.broker_name   = custom_broker_2;
      orderSocket_2.symbol_real   = custom_symbol_real_2;
      orderSocket_2.symbol_custom = custom_symbol_2;
      orderSocket_2.broker_name   = custom_broker_2;
      priceSocket_2.symbol_real   = custom_symbol_real_2;
      priceSocket_2.symbol_custom = custom_symbol_2;
      priceSocket_2.broker_name   = custom_broker_2;
   }

   string _symbol_real = MQLInfoInteger(MQL_TESTER) ? custom_symbol_real_1 : _Symbol;
   string _broker_real = MQLInfoInteger(MQL_TESTER) ? custom_broker_1 : AccountInfoString(ACCOUNT_COMPANY);

   Print("Symbol real ", _symbol_real);
   Print("Broker real ", _broker_real);

   tradeSocket.symbol_real   = _symbol_real;
   tradeSocket.symbol_custom = custom_symbol_1;
   tradeSocket.broker_name   = _broker_real;
   orderSocket.symbol_real   = _symbol_real;
   orderSocket.symbol_custom = custom_symbol_1;
   orderSocket.broker_name   = _broker_real;
   priceSocket.symbol_real   = _symbol_real;
   priceSocket.symbol_custom = custom_symbol_1;
   priceSocket.broker_name   = _broker_real;

   volume_decimal_count = countDecimal(SymbolInfoDouble(_symbol_real, SYMBOL_VOLUME_MIN));
   
   if(InpSocketActive)
   {
      tradeSocket.initialize(InpHostname, InpTradePort, InpBufferSize, SOCKET_CLIENT_TYPE_TRADE);
      tradeSocket.connect();

      priceSocket.initialize(InpHostname, InpPricePort, InpBufferSize, SOCKET_CLIENT_TYPE_PRICE);
      priceSocket.connect();

      orderSocket.initialize(InpHostname, InpOrderPort, InpBufferSize, SOCKET_CLIENT_TYPE_ORDER);
      orderSocket.connect();

      if (MQLInfoInteger(MQL_TESTER))
      {
         tradeSocket_2.initialize(InpHostname, InpTradePort, InpBufferSize, SOCKET_CLIENT_TYPE_TRADE);
         tradeSocket_2.connect();

         priceSocket_2.initialize(InpHostname, InpPricePort, InpBufferSize, SOCKET_CLIENT_TYPE_PRICE);
         priceSocket_2.connect();

         orderSocket_2.initialize(InpHostname, InpOrderPort, InpBufferSize, SOCKET_CLIENT_TYPE_ORDER);
         orderSocket_2.connect();
      }
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

   tradeSocket_2.disconnect();
   priceSocket_2.disconnect();
   orderSocket_2.disconnect();
}

void OnTimer()
{
   tradeSocket.onTimer();
   priceSocket.onTimer();
   orderSocket.onTimer();

   if (MQLInfoInteger(MQL_TESTER))
   {
      tradeSocket_2.onTimer();
      priceSocket_2.onTimer();
      orderSocket_2.onTimer();
   }
}

void OnChartEvent(const int     id,         // event id:
                  const long&   lparam, // chart period
                  const double& dparam, // price
                  const string& sparam  // symbol
                 )
{
   if (MQLInfoInteger(MQL_TESTER))
   {
      if(id >= CHARTEVENT_CUSTOM)
      {
         if       (sparam == custom_symbol_real_1)
         {
            OnTick_1();
         }
         else if (sparam == custom_symbol_real_2)
         {
            OnTick_2();
         }
      }
   }
}

void OnTick()
{
   if (!MQLInfoInteger(MQL_TESTER))
   {
      OnTick_1();
   }
}

void OnTick_1()
{
   MqlTick new_tick;
   
   if(SymbolInfoTick(tradeSocket.symbol_real, new_tick))
   {
      orderSocket.broker_id   = tradeSocket.broker_id;
      orderSocket.account_id  = tradeSocket.account_id;
      orderSocket.symbol_id   = tradeSocket.symbol_id;

      priceSocket.broker_id   = tradeSocket.broker_id;
      priceSocket.account_id  = tradeSocket.account_id;
      priceSocket.symbol_id   = tradeSocket.symbol_id;

      priceSocket.onTick(new_tick);

      if(orderSocket.getRegisterStatus() && orderSocket.symbol_id != -1 && orderSocket.account_id != -1 && orderSocket.symbol_id != -1)
      {
         int arbitrage_id = -1;
         double total_profit = 0;
         bool is_arbitrage_profit_updated = false;

         for(int i=0; i<ArraySize(order_list); i++)
         {
            if(PositionSelectByTicket(order_list[i].ticket) && order_list[i].symbol == tradeSocket.symbol_real)
            {
               double position_volume        = PositionGetDouble(POSITION_VOLUME);
               double position_swap          = PositionGetDouble(POSITION_SWAP);
               double position_profit        = PositionGetDouble(POSITION_PROFIT);
               double position_commission    = 0;
               string position_comment       = PositionGetString(POSITION_COMMENT);

               string comment_array[];
               commentSep(position_comment, comment_array, "#");
               
               arbitrage_id = (int)StringToInteger(comment_array[1]); 

               if(order_list[i].volume != position_volume || order_list[i].swap != position_swap || order_list[i].profit != position_profit || order_list[i].commission != position_commission)
               {
                  is_arbitrage_profit_updated = true;
                  
                  order_list[i].volume       = position_volume;
                  order_list[i].swap         = position_swap;
                  order_list[i].profit       = position_profit;
                  order_list[i].commission   = position_commission;

                  // orderSocket.orderTickUpdate(order_list[i]);

                  total_profit += position_volume + position_swap + position_profit + position_commission;
                  
                  if(HistorySelectByPosition(order_list[i].ticket))
                  {
                     int total_deals = HistoryDealsTotal();
                     
                     if(total_deals > 1)
                     {
                        RemoveValueFromOrderList(order_list[i]);
                     }
                  }
               }
            }
         }

         
         if(arbitrage_id != -1 && is_arbitrage_profit_updated)
         {
            orderSocket.arbitrageTickUpdate(arbitrage_id, total_profit);
         }
      }
   }
}

void OnTick_2()
{
   MqlTick new_tick;
   
   if(SymbolInfoTick(custom_symbol_real_2, new_tick))
   {
      orderSocket_2.broker_id   = tradeSocket_2.broker_id;
      orderSocket_2.account_id  = tradeSocket_2.account_id;
      orderSocket_2.symbol_id   = tradeSocket_2.symbol_id;

      priceSocket_2.broker_id   = tradeSocket_2.broker_id;
      priceSocket_2.account_id  = tradeSocket_2.account_id;
      priceSocket_2.symbol_id   = tradeSocket_2.symbol_id;

      priceSocket_2.onTick(new_tick);

      if(orderSocket_2.getRegisterStatus() && orderSocket_2.symbol_id != -1 && orderSocket_2.account_id != -1 && orderSocket_2.symbol_id != -1)
      {
         for(int i=0; i<ArraySize(order_list); i++)
         {
            if(PositionSelectByTicket(order_list[i].ticket) && order_list[i].symbol == custom_symbol_real_2)
            {
               double position_volume        = PositionGetDouble(POSITION_VOLUME);
               double position_swap          = PositionGetDouble(POSITION_SWAP);
               double position_profit        = PositionGetDouble(POSITION_PROFIT);
               double position_commission    = 0;
               
               
               if(order_list[i].volume != position_volume || order_list[i].swap != position_swap || order_list[i].profit != position_profit || order_list[i].commission != position_commission)
               {
                  order_list[i].volume       = position_volume;
                  order_list[i].swap         = position_swap;
                  order_list[i].profit       = position_profit;
                  order_list[i].commission   = position_commission;

                  orderSocket_2.orderTickUpdate(order_list[i]);
                  
                  if(HistorySelectByPosition(order_list[i].ticket))
                  {
                     int total_deals = HistoryDealsTotal();
                     
                     if(total_deals > 1)
                     {
                        RemoveValueFromOrderList(order_list[i]);
                     }
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

ENUM_ORDER_TYPE_FILLING GetFilling(const string Symb, const uint Type = ORDER_FILLING_IOC)
{
  const ENUM_SYMBOL_TRADE_EXECUTION ExeMode = (ENUM_SYMBOL_TRADE_EXECUTION)::SymbolInfoInteger(Symb, SYMBOL_TRADE_EXEMODE);
  const int FillingMode = (int)::SymbolInfoInteger(Symb, SYMBOL_FILLING_MODE);

  return ((FillingMode == 0 || (Type >= ORDER_FILLING_RETURN) || ((FillingMode & (Type + 1)) != Type + 1)) ? (((ExeMode == SYMBOL_TRADE_EXECUTION_EXCHANGE) || (ExeMode == SYMBOL_TRADE_EXECUTION_INSTANT)) ? ORDER_FILLING_RETURN : ((FillingMode == SYMBOL_FILLING_IOC) ? ORDER_FILLING_IOC : ORDER_FILLING_FOK)) : (ENUM_ORDER_TYPE_FILLING)Type);
}

double getComission(long position_ticket)
{
   HistorySelectByPosition(position_ticket);
   int total_deals = HistoryDealsTotal();

   double commission = 0;
   for(int k=0; k<total_deals; k++)
   {
      long deal_ticket  = (long) HistoryDealGetTicket(k);
      commission       += HistoryDealGetDouble(deal_ticket, DEAL_COMMISSION) + HistoryDealGetDouble(deal_ticket, DEAL_FEE);
   }
   return commission*2;
}
