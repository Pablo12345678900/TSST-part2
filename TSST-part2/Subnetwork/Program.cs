using System;
using System.Net.Sockets;
using System.Text;
using System.Net;
using System.Collections.Generic;
using Tools;
using System.Threading;
using static Tools.RoutingController;

namespace Subnetwork
{
    public class StateObject
    {
        // Client  socket.  
        public Socket workSocket = null;

        // Size of receive buffer.  
        public const int BufferSize = 128;

        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];

        // Received data string.  
        public StringBuilder sb = new StringBuilder();
    }
    class Program
    {
        public static Subnet subnetwork = new Subnet();
        static void Main(string[] args)
        {
            try
            {
                subnetwork.readInfo(args[0]);
            }
            catch(Exception e)
            {

            }
            byte[] buffer = new byte[128];
            subnetwork.subClientToCloud.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), (int)subnetwork.cloudPort));
            subnetwork.subClientToCloud.Send(Encoding.ASCII.GetBytes("First Message " + subnetwork.ip.ToString()));
            subnetwork.subClientToCloud.Receive(buffer);
            List<ushort> ports = subnetwork.givePorts(buffer);
            /*foreach(var port in ports)
            {
                LinkResourceManager link = new LinkResourceManager(port);
                link.IPofNode = subnetwork.ip;
                subnetwork.lrms.Add(link);
            }*/
            // tworzenie interfaceów
            //1.znajdz min port
            //0ushort p = ushort.MaxValue;
            
            while(ports.Count>0)
            {
                ushort p = ushort.MaxValue;
                foreach (var port in ports)
                {
                    if (port < p)
                    {
                        p = port;
                    }
                }
                ports.Remove(p);
                foreach(var port in ports)
                {
                    if ((port - p) == 1)
                    { 
                        Interface inter = new Interface(subnetwork.ip, p);
                        LinkResourceManager link = new LinkResourceManager(p);
                        LinkResourceManager link1 = new LinkResourceManager(port);
                        link1.IPofNode = subnetwork.ip;
                        link.IPofNode = subnetwork.ip;
                        subnetwork.lrmForDomain.Add(link);
                        //subnetwork.lrms
                        subnetwork.interfaces.Add(inter);
                        ports.Remove(port);
                        // p = port;
                        break;
                    }
                }
            }
            List<byte> bufferLRM = new List<byte>();
            bufferLRM.AddRange(Encoding.ASCII.GetBytes("SUBNETWORK-callin " + subnetwork.ip.ToString() + " " + subnetwork.port + " "));
           // int i = 0;
            foreach (LinkResourceManager lrm in subnetwork.lrmForDomain)
            {
                //bufferLRM.Add(0);
                bufferLRM.AddRange(lrm.convertToBytes());
              //  buffer2.AddRange(lrm.convertToBytes());
               // Console.WriteLine((ushort)((buffer2[i + 1] << 8) + buffer2[i]));
               // i += 16;
            }
            subnetwork.subClient.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), (int)subnetwork.portDomain));

            subnetwork.subClient.Send(bufferLRM.ToArray());
            Thread thread = new Thread(WaitForData);
            thread.Start();
            subnetwork.subServer.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), (int)subnetwork.port));
           // subnetwork.subClient = new Socket(IPAddress.Parse("127.0.0.1").AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            subnetwork.subServer.Listen(50);
            Console.WriteLine((int)subnetwork.portDomain + " " + (int)subnetwork.port);
            
            while(true)
            {
                subnetwork.subDone.Reset();
                subnetwork.subServer.BeginAccept(new AsyncCallback(AcceptCallBack), subnetwork.subServer);
                subnetwork.subDone.WaitOne();
            }
        }
        public static void AcceptCallBack(IAsyncResult asyncResult)
        {
            subnetwork.subDone.Set();
            Socket listener = (Socket)asyncResult.AsyncState;
            Socket handler = listener.EndAccept(asyncResult);
            StateObject stateObject = new StateObject();
            stateObject.workSocket = handler;

            handler.BeginReceive(stateObject.buffer, 0, stateObject.buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallBack), stateObject);

        }
        public static void ReceiveCallBack(IAsyncResult asyncResult)
        {
            StateObject state = (StateObject)asyncResult.AsyncState;
            Socket handler = state.workSocket; //socket of client
            int ReadBytes;
            try
            {
                ReadBytes = handler.EndReceive(asyncResult);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, ReadBytes));
            var message = state.sb.ToString().Split(' ');
            if(message[0].Equals("CC-callin"))
            {
                IPAddress ip = IPAddress.Parse(message[1]);
                subnetwork.CC.IPfromSocket.Add(handler, ip);
                subnetwork.CC.SocketfromIP.Add(ip, handler);



                subnetwork.RC.nodesToAlgorithm.Add(IPAddress.Parse(message[1]));
                List<byte> bufferLRM = new List<byte>();
                bufferLRM.AddRange(Encoding.ASCII.GetBytes(message[2]));
                /* for(int j=0; j<bufferLRM.Count;j++)
                 {
                     Console.Write(bufferLRM[j] + " ");

                 }*/
                Console.WriteLine();
                /*  ushort port1 = (ushort)((bufferLRM[1] << 8) + bufferLRM[0]);
                  Console.WriteLine(port1);
                  Console.WriteLine(bufferLRM.Count);*/
                byte[] buffer = new byte[16];
                int i = 0;
                while (i < bufferLRM.Count)
                {
                    buffer = bufferLRM.GetRange(i, 16).ToArray();
                    ushort port = (ushort)((buffer[1] << 8) + buffer[0]);
                    Console.WriteLine(port);
                    LinkResourceManager LRM = LinkResourceManager.returnLRM(buffer);
                    i += 16;
                    Console.WriteLine("Port: " + LRM.port);
                    subnetwork.RC.lrms.Add(LRM);
                }
            }
            if(message[0].Equals("SET-CONNECTION"))
            {
                ushort portS = ushort.Parse(message[1]);
                ushort portF = ushort.Parse(message[2]);
                int startSlot = int.Parse(message[3]);
                int finishSlot = int.Parse(message[4]);
                int speed= int.Parse(message[5]);
                int len_from_domain = int.Parse(message[6]);
                ushort innerPortSource = (ushort)(portS + 1);
                ushort innerPortDest = (ushort)(portF + 1);
                ushort sourceInPort = 0;
                ushort sourceOutPort = 0;
                ushort destOutPort = 0;
                ushort destInPort = 0;
                IPAddress source = null;
                IPAddress destination = null;
                Console.WriteLine("ports from domain: " + portS + " " + portF);
                Console.WriteLine("Start and last slot: " + startSlot + " " + finishSlot);
                Console.WriteLine("Speed: " + speed);
                Console.WriteLine("Length from domain: " + len_from_domain);

                foreach(var cable in subnetwork.RC.cables)
                {
                    if(cable.port1.Equals(innerPortSource))
                    {
                        source = cable.Node2;
                        sourceInPort = cable.port2;
                    }
                    else if(cable.port2.Equals(innerPortSource))
                    {
                        source = cable.Node1;
                        sourceInPort = cable.port1;
                    }
                    if (cable.port1.Equals(innerPortDest))
                    {
                        destination = cable.Node2;
                        destOutPort = cable.port2;
                    }
                    else if (cable.port2.Equals(innerPortDest))
                    {
                        destination = cable.Node1;
                        destOutPort=cable.port1;
                    }
                }

                RoutingResult routingResult=subnetwork.RC.SubentDijkstraAlgorithm(source, destination, subnetwork.RC.cables, subnetwork.RC.lrms, speed, startSlot, finishSlot, len_from_domain);

                foreach(var node in routingResult.Path)
                {
                    Cable cable = findCableBetweenNodes(node, destination, subnetwork.RC.cables);
                    if(!cable.Equals(null))
                    {
                        if(cable.Node1.Equals(destination))
                        {
                            destInPort = cable.port1;
                            break;
                        }
                        if (cable.Node2.Equals(destination))
                        {
                            destInPort = cable.port2;
                            break;
                        }
                    }
                }


                List<byte> bufferToSend = new List<byte>();
                int ct = 0;
                Socket destSocket = subnetwork.CC.SocketfromIP[destination];
                
               
                bufferToSend.AddRange(Encoding.ASCII.GetBytes("ACK"));
                bufferToSend.AddRange(BitConverter.GetBytes(startSlot));
                bufferToSend.AddRange(BitConverter.GetBytes(finishSlot));
                bufferToSend.AddRange(BitConverter.GetBytes(destOutPort));
                bufferToSend.AddRange(BitConverter.GetBytes(destInPort));
                destSocket.BeginSend(bufferToSend.ToArray(), 0, bufferToSend.ToArray().Length, 0,
        new AsyncCallback(SendCallBack), destSocket);
                bufferToSend.Clear();
                foreach (var cab in routingResult.nodeAndPortsOut)
                {
                    
                    if(cab.Key.Equals(source))
                    {
                        Socket socket1 = subnetwork.CC.SocketfromIP[cab.Key];
                        sourceOutPort = cab.Value;
                        bufferToSend.AddRange(Encoding.ASCII.GetBytes("ACK"));
                        bufferToSend.AddRange(BitConverter.GetBytes(startSlot));
                        bufferToSend.AddRange(BitConverter.GetBytes(finishSlot));
                        bufferToSend.AddRange(BitConverter.GetBytes(sourceOutPort));
                        bufferToSend.AddRange(BitConverter.GetBytes(sourceInPort));
                        socket1.BeginSend(bufferToSend.ToArray(), 0, bufferToSend.ToArray().Length, 0,
                new AsyncCallback(SendCallBack), socket1);
                        bufferToSend.Clear();
                        continue;
                    }
                    bool flaga = false;
                    Socket socket = subnetwork.CC.SocketfromIP[cab.Key];
                    Console.WriteLine("Adres: " + cab.Key + " port out: " + cab.Value);
                    foreach (var cab1 in routingResult.nodeAndPortsIn)
                    {
                        if (cab1.Key.Equals(cab.Key))
                        {
                            Console.WriteLine("Port in: " + cab1.Value);
                            bufferToSend.AddRange(Encoding.ASCII.GetBytes("ACK"));
                            bufferToSend.AddRange(BitConverter.GetBytes(startSlot));
                            bufferToSend.AddRange(BitConverter.GetBytes(finishSlot));
                            bufferToSend.AddRange(BitConverter.GetBytes(cab.Value));
                            bufferToSend.AddRange(BitConverter.GetBytes(cab1.Value));
                            socket.BeginSend(bufferToSend.ToArray(), 0, bufferToSend.ToArray().Length, 0,
                    new AsyncCallback(SendCallBack), socket);
                            bufferToSend.Clear();
                            flaga = true;
                            break;
                        }
                    }
                    
                }
                Console.WriteLine("Send");
            }
            state.sb.Clear();
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallBack), state);
        }
        public static void WaitForData()// dane które przyjdą na adres 10.0.0.4 wiadomo że to będą styki czyli trzeba tylko ->znaleźć styk-> zmienić port i odesłać do chmury
        {
            while(true)
            {
                byte[] buffer = new byte[128];
                subnetwork.subClientToCloud.Receive(buffer);
                Console.WriteLine("I received data");
                DataStream dataStream = DataStream.toData(buffer);
                foreach(var inter in subnetwork.interfaces)
                {
                    if(inter.port1.Equals(dataStream.currentPort))
                    {
                        dataStream.currentPort = inter.port2;
                        break;
                    }
                    if (inter.port2.Equals(dataStream.currentPort))
                    {
                        dataStream.currentPort = inter.port1;
                        break;
                    }
                }
                subnetwork.subClientToCloud.Send(dataStream.toBytes());
            }
        }
        // styki można utworzyć przy zgłaszaniu się do chmury tej podsieci: wiemy że port po jednej stronie to x a po drugiej to x+1, z portów które zwróciło, szukamy gdzie jest różnica w portach o 1
        public static void SendCallBack(IAsyncResult ar)
        {
            try
            {
                Socket handler = (Socket)ar.AsyncState;

                int bytesSent = handler.EndSend(ar);


            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        public static Cable findCableBetweenNodes(IPAddress ip1, IPAddress ip2, List<Cable> cables)
        {
            Cable cable = new Cable();
            for (int i = 0; i < cables.Count; i++)
            {
                if ((cables[i].Node1.Equals(ip1) && cables[i].Node2.Equals(ip2) || (cables[i].Node2.Equals(ip1) && cables[i].Node1.Equals(ip2))))
                {
                    cable = cables[i];
                    break;
                }
            }
            return cable;
        }
    }
}
