using System;
using System.Collections.Generic;
using System.Text;

using System.Net;
using System.Net.Sockets;
using System.IO;
using Tools;
using System.Threading;
using Subnetwork;

namespace DomainApp
{
    class Domain
    {
        public RoutingController RC { get; set; }
        public ConnectionController CC { get; set; }
        public NetworkCallController NCC { get; set; }

        public ManualResetEvent domainDone = new ManualResetEvent(false);
        public Subnet subnetwork { get; set; }
        
        public Socket domainServer { get; set; }
        public Socket domainClient { get; set; }
        public ushort port { get; set; }
        public Socket socketToSub { get; set; }

        public ushort secondDomainPort { get; set; }
        

        public Dictionary<Socket, IPAddress> IPfromSocket = new Dictionary<Socket, IPAddress>();
        public Dictionary<IPAddress, Socket> SocketfromIP = new Dictionary<IPAddress, Socket>();

        public List<IPAddress> nodesToAlgorithm = new List<IPAddress>();
        public List<LinkResourceManager> links = new List<LinkResourceManager>();
        public Domain()
        {
            RC = new RoutingController();
            CC = new ConnectionController();
            NCC = new NetworkCallController("directory1.txt", "policy.txt");
            IPAddress myhost = IPAddress.Parse("127.0.0.1");
            domainServer = new Socket(myhost.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
           
            domainClient= new Socket(myhost.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socketToSub= new Socket(myhost.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }
        public void readinfo(String conFile)
        {
            string line;
            StreamReader streamReader = new StreamReader(conFile);
            List<Cable> readCables = new List<Cable>(); //RC musi znać kable

            //RoutingController RC = new RoutingController();
            line = streamReader.ReadLine();
            port = ushort.Parse(line.Split(' ')[1]);
            line = streamReader.ReadLine();
            secondDomainPort = ushort.Parse(line.Split(' ')[1]);
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
            //return RC;
        }

    }
}
