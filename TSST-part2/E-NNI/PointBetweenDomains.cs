using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Tools;
using System.IO;
namespace E_NNI
{
     public class PointBetweenDomains
    {
        public ushort port1 { get; set; }
        public ushort port2 { get; set; }
        public  IPAddress ip { get; set; }

        public ushort portOfCloud { get; set; }
        public Socket socketToCloud { get; set; }
        public Socket socketToDomain1 { get; set; }
        public ushort portDomain1 { get; set; }
        public Socket socketToDomain2 { get; set; }
        public ushort portDomain2 { get; set; }
        public List<LinkResourceManager> lrms = new List<LinkResourceManager>();
        public PointBetweenDomains()
        {
            

        }
        public static PointBetweenDomains readInfo(String str)
        {
            PointBetweenDomains enni = new PointBetweenDomains();
            
            StreamReader stream = new StreamReader(str);
            string line = stream.ReadLine();
            enni.port1 = ushort.Parse(line.Split(' ')[0]);
            enni.port2 = ushort.Parse(line.Split(' ')[1]);
            enni.ip = IPAddress.Parse(line.Split(' ')[2]);
            IPAddress myhost = IPAddress.Parse("127.0.0.1");
            enni.socketToCloud = new Socket(myhost.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            enni.socketToDomain1 = new Socket(myhost.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            enni.socketToDomain2 = new Socket(myhost.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            line = stream.ReadLine();
            enni.portOfCloud= ushort.Parse(line.Split(' ')[0]);
            enni.portDomain1= ushort.Parse(line.Split(' ')[1]);
            enni.portDomain2= ushort.Parse(line.Split(' ')[2]);
            return enni;
        }

    }
}
