﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Tools;
using System.Xml.Serialization;
//using DomainApp;
using System.Globalization;


namespace Node
{
    public class Routing
    {
        public string Name { get; set; }
        public IPAddress IpAddress { get; set; }

        public Socket SocketToForward { get; set; }
        public Socket SocketToDomain { get; set; }
      //  public ushort Port { get; set; }
        
        public IPAddress cloudIp { get; set; }
        public ushort cloudPort { get; set; }
        
        public IPAddress DomainIP { get; set; }
        public ushort DomainPort { get; set; }

        public byte[] bufferForPacket = new byte[128];
        public byte[] bufferForManagement = new byte[12]; //optical entry has 12 bytes: 2 ints and 2 shorts
        public List<ushort> usedPorts = new List<ushort>();

        public List<LinkResourceManager> linkResources=new List<LinkResourceManager>();// traktujemy router jako podsieć dla ułatwienia dlatego dany router ma kilka LRM
        PackageHandler packageHandler=new PackageHandler();
        public Routing(string n, IPAddress ip)
        {
            Name = n;
            IpAddress = ip;
           // Port = P;
        }

        public static Routing createRouter(string conFile)
        {
            StreamReader streamReader=new StreamReader(conFile);
            string line = streamReader.ReadLine();
            string name = line.Split(' ')[0];
            IPAddress ipAddress = IPAddress.Parse(line.Split(' ')[1]);
           // ushort Port = ushort.Parse(line.Split(' ')[2]);
            Routing routing=new Routing(name, ipAddress);

            line = streamReader.ReadLine();
            routing.cloudIp = IPAddress.Parse(line.Split(' ')[0]);
            routing.cloudPort = ushort.Parse(line.Split(' ')[1]);

            line = streamReader.ReadLine();
            routing.DomainIP=IPAddress.Parse(line.Split(' ')[0]);
            routing.DomainPort = ushort.Parse(line.Split(' ')[1]);
            
            
            
            return routing;

        }

        public void ActivateRouter()
        {
            Console.WriteLine("My name is: " + this.Name);
            SocketToForward=new Socket(cloudIp.AddressFamily,SocketType.Stream,ProtocolType.Tcp);
            SocketToDomain=new Socket(DomainIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            
            SocketToForward.Connect(new IPEndPoint(cloudIp,cloudPort)); // connection with cloud
            Console.WriteLine(this.Name + ": [" + DateTime.UtcNow.ToString("HH:mm:ss.fff",
                                            System.Globalization.CultureInfo.InvariantCulture) + "] " + "Connected with cloud :)");
            SocketToForward.Send(Encoding.ASCII.GetBytes("First Message " + this.IpAddress.ToString() + " Router"));
            byte[] buffer1 = new byte[256];
            
            SocketToForward.Receive(buffer1);//node has to know about used ports
            usedPorts = givePorts(buffer1);
            for(int i=0;i<usedPorts.ToArray().Length;i++)
            {
                ushort port = usedPorts[i];
                LinkResourceManager link = new LinkResourceManager(port);
                link.IPofNode = this.IpAddress;
                linkResources.Add(link); // creating LRM's
            }
            SocketToDomain.Connect(new IPEndPoint(DomainIP, DomainPort));
            List<byte> bufferForLRMs = new List<byte>();
            
            bufferForLRMs.AddRange(Encoding.ASCII.GetBytes("CC-callin " + this.IpAddress.ToString() + " "));
            foreach (var linkResource in this.linkResources)
            {
                bufferForLRMs.AddRange(linkResource.convertToBytes());
            }
            SocketToDomain.Send(bufferForLRMs.ToArray()); // zgłasza się  do Domaina
            byte[] buffer = new byte[4096];
            Console.WriteLine(this.Name + ": [" + DateTime.UtcNow.ToString("HH:mm:ss.fff",
                                            CultureInfo.InvariantCulture) + "] " + "Connected with Domain :)");
            //byte[] msg = Encoding.ASCII.GetBytes(Name);

           // int bytesSent = SocketToDomain.Send(msg);

            Thread forwardingThread=new Thread(WaitForPackage);
            Thread managementThread=new Thread(WaitForCommands);
            managementThread.Start();
            forwardingThread.Start();

        }
        public List<ushort> givePorts(byte[] bytes) // change from bytes to port number
        {
            List<ushort> list = new List<ushort>();
            for (int i = 0; i < bytes.Length; i = i + 2)
            {
                ushort port = (ushort)((bytes[i + 1] << 8) + bytes[i]);
                if (port == 0)
                    break;
                Console.WriteLine(port);
                list.Add(port);
            }
            return list;
        }

        public void WaitForPackage()
        {
            while (true)
            {
                try
                {
                    SocketToForward.Receive(bufferForPacket);
                    DataStream dataStream = DataStream.toData(bufferForPacket);
                    Console.WriteLine(this.Name + ": [" + DateTime.UtcNow.ToString("HH:mm:ss.fff",
                                            CultureInfo.InvariantCulture) + "] " + "I received package at port: " + dataStream.currentPort);
                   // data.printInfo();

                    ForwardPacket(bufferForPacket);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }

        public void ForwardPacket(byte[] bytes)
        {
            DataStream dataStream = DataStream.toData(bytes);
            
            packageHandler.handlePackage(dataStream);

            SocketToForward.Send(dataStream.toBytes());
            Console.WriteLine(this.Name + ": [" + DateTime.UtcNow.ToString("HH:mm:ss.fff",
                                  CultureInfo.InvariantCulture) + "] " + "I sent package by port: "
                              + dataStream.currentPort);
           // dataStream.printInfo();

        }

        public void WaitForCommands()
        {
            while (true)
            {
                String data = null;

                // Domain będzie aktualizował te optical entry
                
                SocketToDomain.Receive(bufferForManagement);// from CC in management we have to check whether it is possible to take requested capacity
                Optical_Entry opticalEntry = packageHandler.FromBytesToEntry(bufferForManagement); // 
                int k = 0;
               for(int i=0; i< this.linkResources.Count;i++)
                {
                    if (opticalEntry.outPort == this.linkResources[i].port)
                    {
                        k = i;
                        break;
                    }
                }
                //SocketToDomain.Send(Encoding.ASCII.GetBytes("G8 CONF MY M8")); // ack for Domain- odsyłamy do Domaina że j
                
                 // SocketToDomain.Send(Encoding.ASCII.GetBytes("G8 CONF MY M8"));
                    for (uint j = opticalEntry.startSlot; j <= opticalEntry.lastSlot; j++)
                    {
                        linkResources[k].slots[j] = false; //zmieniamy stan tych szczelin bo są one żądane
                    }
                        packageHandler.Optical_Table.Add(opticalEntry);//można dodać wiersz do tablicy
                
                
                Console.WriteLine(this.Name + ": [" + DateTime.UtcNow.ToString("HH:mm:ss.fff",
                                     CultureInfo.InvariantCulture) + "] " + "I got new configuration!!!");
 
                packageHandler.displayTables();
                Console.WriteLine(this.Name + ": [" + DateTime.UtcNow.ToString("HH:mm:ss.fff",
                                                CultureInfo.InvariantCulture) + "] " + "I updated my MPLS tables! :) ");
            }
        }

    }
}
