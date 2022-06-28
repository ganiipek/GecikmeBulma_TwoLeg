using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GecikmeBulma.MetaSocket
{
    public class MetaSocket
    {
        public dynamic Host;
        public int Port;
        public int BufferSize;

        public MetaSocket()
        {
            Host = IPAddress.Any;
            Port = 3131;
            BufferSize = 512;
        }
    }
}
