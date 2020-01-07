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
using ManagerApp;
using System.Globalization;


namespace Node
{
    public class Routing
    {
        public string Name { get; set; }
        public IPAddress IpAddress { get; set; }

        public Socket SocketToForward { get; set; }
        public Socket SocketToManager { get; set; }
        public ushort Port { get; set; }
        
        public IPAddress cloudIp { get; set; }
        public ushort cloudPort { get; set; }
        
        public IPAddress ManagerIP { get; set; }
        public ushort ManagerPort { get; set; }

        public byte[] bufferForPacket = new byte[128];
        public byte[] bufferForManagement = new byte[12]; //optical entry has 12 bytes: 2 ints and 2 shorts
        public List<ushort> usedPorts = new List<ushort>();

        public List<LinkResourceManager> linkResources=new List<LinkResourceManager>();
        PackageHandler packageHandler=new PackageHandler();
        public Routing(string n, IPAddress ip, ushort P)
        {
            Name = n;
            IpAddress = ip;
            Port = P;
        }

        public static Routing createRouter(string conFile)
        {
            StreamReader streamReader=new StreamReader(conFile);
            string line = streamReader.ReadLine();
            string name = line.Split(' ')[0];
            IPAddress ipAddress = IPAddress.Parse(line.Split(' ')[1]);
            ushort Port = ushort.Parse(line.Split(' ')[2]);
            Routing routing=new Routing(name, ipAddress,Port);

            line = streamReader.ReadLine();
            routing.cloudIp = IPAddress.Parse(line.Split(' ')[0]);
            routing.cloudPort = ushort.Parse(line.Split(' ')[1]);

            line = streamReader.ReadLine();
            routing.ManagerIP=IPAddress.Parse(line.Split(' ')[0]);
            routing.ManagerPort = ushort.Parse(line.Split(' ')[1]);
            
            
            
            return routing;

        }

        public void ActivateRouter()
        {
            Console.WriteLine("My name is: " + this.Name);
            SocketToForward=new Socket(cloudIp.AddressFamily,SocketType.Stream,ProtocolType.Tcp);
            SocketToManager=new Socket(ManagerIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            
            SocketToForward.Connect(new IPEndPoint(cloudIp,cloudPort)); // connection with cloud
            Console.WriteLine(this.Name + ": [" + DateTime.UtcNow.ToString("HH:mm:ss.fff",
                                            System.Globalization.CultureInfo.InvariantCulture) + "] " + "Connected with cloud :)");
            SocketToForward.Send(Encoding.ASCII.GetBytes("First Message " + this.IpAddress.ToString()));
            byte[] buffer1 = new byte[256];
            
            SocketToForward.Receive(buffer1);//node has to know about used ports
            usedPorts = givePorts(buffer1);
            for(int i=0;i<usedPorts.ToArray().Length;i++)
            {
                ushort port = usedPorts[i];
                LinkResourceManager link = new LinkResourceManager(port);
                linkResources.Add(link); // creating LRM's
            }
            SocketToManager.Connect(new IPEndPoint(ManagerIP, ManagerPort));
            
            byte[] buffer = new byte[4096];
            Console.WriteLine(this.Name + ": [" + DateTime.UtcNow.ToString("HH:mm:ss.fff",
                                            CultureInfo.InvariantCulture) + "] " + "Connected with manager :)");
            byte[] msg = Encoding.ASCII.GetBytes(Name);

            int bytesSent = SocketToManager.Send(msg);

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

                // manager będzie aktualizował te optical entry
                
                SocketToManager.Receive(bufferForManagement);// from CC in management we have to check whether it is possible to take requested capacity
                Optical_Entry opticalEntry = packageHandler.FromBytesToEntry(bufferForManagement); // 

                bool flag = true;
                int k=0;
                for(int i=0;i<linkResources.ToArray().Length;i++) // musimy znalexc odpowiedni LRM
                {
                    if(opticalEntry.outPort==linkResources[i].port)// patrzymy czy na danym porcie jest dostępny zakres szczelin
                    {
                        k = i;
                        for(uint j=opticalEntry.startSlot;j<=opticalEntry.lastSlot;j++) // patrzymy dla każdej szczeliny czy jest wolna
                        {
                            if(linkResources[i].slots[j]==false)
                            {
                                SocketToManager.Send(Encoding.ASCII.GetBytes("NOT AVAILABLE")); // if one of demanded slots isnt available- send to MS information 
                                //then RC in MS has to calculate new path
                                flag = false;
                                break;
                            }
                        }
                        break;
                    }
                }
                //SocketToManager.Send(Encoding.ASCII.GetBytes("G8 CONF MY M8")); // ack for manager- odsyłamy do managera że jest git

                if (flag)
                {
                    SocketToManager.Send(Encoding.ASCII.GetBytes("G8 CONF MY M8"));
                    for (uint j = opticalEntry.startSlot; j <= opticalEntry.lastSlot; j++)
                    {
                        linkResources[k].slots[j] = false; //zmieniamy stan tych szczelin bo są one żądane
                    }
                        packageHandler.Optical_Table.Add(opticalEntry);//można dodać wiersz do tablicy
                } 
                
                Console.WriteLine(this.Name + ": [" + DateTime.UtcNow.ToString("HH:mm:ss.fff",
                                     CultureInfo.InvariantCulture) + "] " + "I got new configuration!!!");
 
                packageHandler.displayTables();
                Console.WriteLine(this.Name + ": [" + DateTime.UtcNow.ToString("HH:mm:ss.fff",
                                                CultureInfo.InvariantCulture) + "] " + "I updated my MPLS tables! :) ");
            }
        }

    }
}
