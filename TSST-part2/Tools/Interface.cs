using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
namespace Tools
{
    class Interface
    {
        public ushort port1 { get; set; }
        public ushort port2 { get; set; }
        public IPAddress ip { get; set; }
        public Interface(IPAddress ip1, ushort p1)
        {
            ip = ip1;
            port1 = p1;
            port2 = (ushort)(port1 + 1);
        }
    }
}
