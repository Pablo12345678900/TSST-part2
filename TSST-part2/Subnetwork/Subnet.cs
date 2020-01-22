using System;
using System.Collections.Generic;
using System.Text;
using Tools;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace Subnetwork
{
   public class Subnet
    {  
        public ConnectionController CC { get; set; }
        public RoutingController RC { get; set; }
        public IPAddress ip { get; set; }
        public ushort port { get; set; }
        public IPAddress ipDomain { get; set; }
        public ushort portDomain { get; set; }
        public ushort cloudPort { get; set; }
        public Socket subServer { get; set; }
        public Socket subClient { get; set; }
        public Socket subClientToCloud { get; set; }
        public List<LinkResourceManager> lrms = new List<LinkResourceManager>();

        public ManualResetEvent subDone = new ManualResetEvent(false);

        public Subnet()
        {
            CC = new ConnectionController();
            RC = new RoutingController();
          
            subServer = new Socket(IPAddress.Parse("127.0.0.1").AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            subClient= new Socket(IPAddress.Parse("127.0.0.1").AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            subClientToCloud = new Socket(IPAddress.Parse("127.0.0.1").AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            //subServer.Bind(new IPEndPoint(iplocal, (int)port));

        }
        public void readInfo(string conFile)
        {
           
            string line;
            
            StreamReader streamReader = new StreamReader(conFile);
           
            List<Cable> readCables = new List<Cable>(); //RC musi znać kable

            //RoutingController RC = new RoutingController();
            
            line = streamReader.ReadLine();
            
            port = ushort.Parse(line.Split(' ')[1]);
            Console.WriteLine("Wczytywanie" + port);
            line = streamReader.ReadLine();
            portDomain = ushort.Parse(line.Split(' ')[1]);
            line = streamReader.ReadLine();
            cloudPort = ushort.Parse(line.Split(' ')[1]);
            line = streamReader.ReadLine();
            ip = IPAddress.Parse(line.Split(' ')[1]);
            //Domain.port = ushort.Parse(line.Split(' ')[1]);
            while ((line = streamReader.ReadLine()) != null)
            {
                IPAddress ip1 = IPAddress.Parse(line.Split(' ')[1]);
                line = streamReader.ReadLine();
                ushort port1 = ushort.Parse(line.Split(' ')[1]);
                line = streamReader.ReadLine();
                IPAddress ip2 = IPAddress.Parse(line.Split(' ')[1]);
                line = streamReader.ReadLine();
                ushort port2 = ushort.Parse(line.Split(' ')[1]);
                line = streamReader.ReadLine();
                int len = int.Parse(line.Split(' ')[1]);
                Cable cable = new Cable(ip1, ip2, port1, port2, len);
                readCables.Add(cable);
            }
            RC.cables = readCables;
        }
        public List<ushort> givePorts(byte[] bytes) // change from bytes to port number
        {
            List<ushort> list = new List<ushort>();
            for (int i = 0; i < bytes.Length; i = i + 2)
            {
                ushort port = (ushort)((bytes[i + 1] << 8) + bytes[i]);
                if (port.Equals(0))
                    break;
                list.Add(port);
            }
            return list;
        }

    }
}
