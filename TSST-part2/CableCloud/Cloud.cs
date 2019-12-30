using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
namespace CableCloud
{
   
   public class Cloud
    {
        public List<Cable> cables { get; set; }
        public IPAddress cloudIp { get; set; }
        public int cloudPort { get; set; } // one port for cloud is enough, many sockets can operate on one port
        

        public Cloud(IPAddress adr, int cp)
        {
            cables = new List<Cable>();
            cloudIp = adr;
            cloudPort = cp;
        }
        public static Cloud createCloud(string conFile)
        {
            string line;
            StreamReader streamReader = new StreamReader(conFile);
            line = streamReader.ReadLine();
            IPAddress address = IPAddress.Parse(line.Split(' ')[1]);
            line = streamReader.ReadLine();
            int port = int.Parse(line.Split(' ')[1]);
            Cloud cloud = new Cloud(address, port);
            Console.WriteLine("Wczytalo cos");
            while ((line = streamReader.ReadLine()) != null)
            {
                IPAddress ip1 = IPAddress.Parse(line.Split(' ')[1]);
                line = streamReader.ReadLine();
                ushort port1 = ushort.Parse(line.Split(' ')[1]);
                line = streamReader.ReadLine();
                IPAddress ip2 = IPAddress.Parse(line.Split(' ')[1]);
                line = streamReader.ReadLine();
                ushort port2 = ushort.Parse(line.Split(' ')[1]);
                Cable cable = new Cable(ip1, ip2, port1, port2);
                cloud.cables.Add(cable);
            }
          
            return cloud;
        }
    }
}
