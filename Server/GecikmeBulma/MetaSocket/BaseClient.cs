using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using GecikmeBulma.Trade;

namespace GecikmeBulma.MetaSocket
{
    enum ClientType
    {
        TRADE,
        ORDER,
        PRICE
    };

    internal class BaseClient
    {
        public Pair Pair { get; set; }
        public TcpClient Client { get; set; }
        //public int MagicNumber { get; set; }
        public ClientType Type { get; set; }

        public override string ToString()
        {
            return String.Format("Account: {0}, Pair: {1}, Type: {2}",
                "",
                Pair.ToString(),
                //MagicNumber.ToString(),
                Type.ToString()
            );
        }
    }
}
