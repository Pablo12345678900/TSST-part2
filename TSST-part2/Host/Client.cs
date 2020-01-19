using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using Tools;
using System.IO;
using System.Net.Sockets;
namespace Host
{
    public class Client
    {
        public List<RestOfHosts> Neighbours { get; set; } // rest of hosts in site
        public IPAddress clientIP { get; set; }
        public string clientName { get; set; } // H1, H2 etc...
        public IPAddress cloudIP { get; set; }
       // public ushort domainPort { get; set; }

        public IPAddress managementIP { get; set; }
        public ushort domainPort { get; set; }
        public ushort cloudPort { get; set; }
        public ushort portOut { get; set; }
        public int usedModulation { get; set; }

        public Socket socketToCloud;
        public Socket socketToDomain;
        ///public List<LinkResourceManager> linkResources = new List<LinkResourceManager>();
        public List<ushort> usedPorts = new List<ushort>();
        public List<LinkResourceManager> linkResources = new List<LinkResourceManager>();
        public Client()
        {
            Neighbours = new List<RestOfHosts>();

        }

        public static Client createHost(string ConFile)
        {
            Client host = new Client();
            string line;
            StreamReader streamReader = new StreamReader(ConFile);

            line = streamReader.ReadLine();
            host.clientName = line.Split(' ')[1];

            line = streamReader.ReadLine();
            host.clientIP = IPAddress.Parse(line.Split(' ')[1]);

            line = streamReader.ReadLine();
            host.portOut = ushort.Parse(line.Split(' ')[1]);

            line = streamReader.ReadLine();
            host.cloudIP = IPAddress.Parse(line.Split(' ')[1]);

            line = streamReader.ReadLine();
            host.cloudPort = ushort.Parse(line.Split(' ')[1]);

            /*line = streamReader.ReadLine();
            host.managementIP = IPAddress.Parse(line.Split(' ')[1]);*/

            line = streamReader.ReadLine();
            host.domainPort = ushort.Parse(line.Split(' ')[1]);
            RestOfHosts neighbour;

            while ((line = streamReader.ReadLine()) != null)
            {
                string Name = line.Split(' ')[0];
                IPAddress ip = IPAddress.Parse(line.Split(' ')[1]);


                neighbour = new RestOfHosts(line);
                host.Neighbours.Add(neighbour);
            }
            Console.WriteLine("Host has been created");
            return host;

        }
    }
}
