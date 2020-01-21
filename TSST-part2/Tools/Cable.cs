using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
namespace Tools
{

    public class Cable // nodes must be "physically" connected because of TCP, cables are connected between ports, 
                         //to completely specify node we need his name and his port
    {
        public IPAddress Node1 { get; set; }
        public IPAddress Node2 { get; set; }
        public ushort port1 { get; set; }
        public ushort port2 { get; set; }
        //public int capacity { get; set; }
        public bool stateOfCable { get; set; } 
        public int length { get; set; }
        public Cable()
        {

        }
        public Cable(IPAddress n1, IPAddress n2, ushort p1, ushort p2, int length)
        {
            Node1 = n1;
            Node2 = n2;
            port1 = p1;
            port2 = p2;
            stateOfCable = true;
            this.length = length;
        }
        public override string ToString()
        {
            return $"{Node1.ToString()} {port1.ToString()} {Node2.ToString()} {port2.ToString()}";
        }
    }
}
