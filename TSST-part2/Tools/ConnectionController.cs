using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Tools
{
    public class ConnectionController
    {
        public Dictionary<Socket, IPAddress> IPfromSocket = new Dictionary<Socket, IPAddress>();
        public Dictionary<IPAddress, Socket> SocketfromIP = new Dictionary<IPAddress, Socket>();
        
        public ConnectionController()
        {
            
            
        }
        public byte[] RouteTableQueryRequest(IPAddress source, IPAddress destination, int capacity)
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(source.GetAddressBytes());
            bytes.AddRange(destination.GetAddressBytes());
            bytes.AddRange(BitConverter.GetBytes(capacity));
            return bytes.ToArray();
        }

    }
}
