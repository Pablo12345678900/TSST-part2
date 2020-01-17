using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
namespace Tools
{
     public class NetworkCallController
    {
        
        public String directory { get; set; } // .txt to database
        public String policy { get; set; } //.txt to check if we can set a connection

        
        public NetworkCallController(String directory, String policy)
        {
            this.directory = directory;
            this.policy = policy;
            //this.port = port;
            
            // NCCServer.Listen(100);
        }
        public IPAddress DirectoryRequest(String hostName)
        {
            string line;
            StreamReader streamReader = new StreamReader(this.directory);
            IPAddress iPAddress=null;
            bool flaga = false; 
            while((line=streamReader.ReadLine())!=null)
            {
                if(line.Split(' ')[0].Equals(hostName))
                {
                    iPAddress = IPAddress.Parse(line.Split(' ')[1]);
                    flaga = true;
                }
            }
            if (flaga)
            {
                Console.WriteLine("Znaleziono takiego hosta w tej domenie");
            }
            else
                Console.WriteLine("Nie znaleziono takiego hosta w tej domenie");


            return iPAddress;
        }
        public bool PolicyRequest(IPAddress host1, IPAddress host2)
        {
            string line;
            string info1,info2;
            StreamReader streamReader = new StreamReader(this.policy);
            bool flaga1 = false;
            bool flaga2 = false;
            bool flaga = false;
            while ((line = streamReader.ReadLine()) != null)
            {
                if (line.Split(' ')[0].Equals(host1))
                {
                    info1 = line.Split(' ')[1];
                    if(info1.Equals("OK"))
                     flaga1 = true;
                }
                if (line.Split(' ')[0].Equals(host2))
                {
                    info2 = line.Split(' ')[1];
                    if(info2.Equals("OK"))
                        flaga2 = true;
                }
            }
            if (flaga1 && flaga2)
                flaga = true;
            return flaga;
        }
        public byte[] ConnectionRequest(IPAddress source, IPAddress destination, int capacity)
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(Encoding.ASCII.GetBytes("ConnectionRequest "));
            bytes.AddRange(source.GetAddressBytes());
            bytes.AddRange(destination.GetAddressBytes());
            bytes.AddRange(BitConverter.GetBytes(capacity));
            return bytes.ToArray();
        }


    }
}
