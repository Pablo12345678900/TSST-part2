using System.Collections.Generic;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml.Serialization;
using System.Linq;
using Tools;
using System.Globalization;
using static Tools.RoutingController;
/// <summary>
/// 
/// </summary>

namespace DomainApp
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
        
        public static Domain domain=new Domain();
        public static RoutingResult r = new RoutingResult();

        static void Main(string[] args)
        {
            try
            {
                domain.readinfo(args[0]);
                domain.NCC.directory = args[1];
            }
            catch(Exception e)
            {

            }
            Console.WriteLine(domain.port);
            
            domain.domainServer.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), domain.port));
            
            domain.domainServer.Listen(50);
            try
                {
                domain.domainClient.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), domain.secondDomainPort));
            }
            catch(Exception e)
            {
                Console.WriteLine("Retrying...");
                domain.domainClient.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), domain.secondDomainPort));
            }
            // Thread thread = new Thread(connectWithSecondDomain);
            //thread.Start();
            //   domain.secondDomainSocket.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), domain.secondDomainPort));

            while (true)
            {
                domain.domainDone.Reset();
                domain.domainServer.BeginAccept(new AsyncCallback(AcceptCallBack), domain.domainServer);
                domain.domainDone.WaitOne();
            }
           
           // Thread2.Start();

        }
        public static void connectWithSecondDomain()
        {

            try
            {
                domain.domainClient.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), domain.secondDomainPort));
            }
            catch(Exception e)
            {
                Console.WriteLine("Domain unreachable, trying reconnecting...");
                connectWithSecondDomain();
            }
            domain.domainClient.Send(Encoding.ASCII.GetBytes("Domain-callin " + IPAddress.Parse("127.0.0.1")));
            while(true)
            {
                byte[] buffer = new byte[128];
                int readBytes = 0;
                readBytes=domain.domainClient.Receive(buffer);
                StringBuilder sb = new StringBuilder();
                sb.Append(Encoding.ASCII.GetString(buffer, 0, readBytes));
                var message = sb.ToString().Split(' ');
                if(message[0].Equals("RC-giveDomainPoint"))
                {
                    IPAddress source = IPAddress.Parse(message[1]);
                    IPAddress destination = IPAddress.Parse(message[2]);
                    int speed = int.Parse(message[3]);
                    StreamReader streamReader = new StreamReader("domainPoints.txt");
                    IPAddress borderAddress = null;
                    string line;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        if ((line.Split(' ')[0].Equals(message[1]) && line.Split(' ')[1].Equals(message[2])) || (line.Split(' ')[0].Equals(message[2]) && line.Split(' ')[1].Equals(message[1])))
                        {
                            borderAddress = IPAddress.Parse(line.Split(' ')[2]);
                            break;
                        }
                            
                    }

                    domain.domainClient.Send(Encoding.ASCII.GetBytes("RC-SecondDomainTopology " + borderAddress.ToString() + " " + source.ToString() + " "+ speed + " " + destination.ToString()));
                }
                
            }
            

        }
      
        public static void AcceptCallBack(IAsyncResult asyncResult)
        {
            domain.domainDone.Set();
            Socket listener = (Socket)asyncResult.AsyncState;
            Socket handler = listener.EndAccept(asyncResult);
            StateObject stateObject = new StateObject();
            stateObject.workSocket = handler;

            handler.BeginReceive(stateObject.buffer, 0, stateObject.buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallBack), stateObject);
            //Array.Clear(stateObject.buffer, 0, stateObject.buffer.Length);

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
            // first message must be send to get information about connected socket: First Message <Ip address>
            if (message[0].Equals("NCC-GET")) // żądanie hosta na połączenie
            {
                String source = message[1];
                String destination = message[2];
                int speed = int.Parse(message[3]);
                Console.WriteLine("Speed " + speed);
                Console.WriteLine("Checking in directory...");
                IPAddress sourceAddress = domain.NCC.DirectoryRequest(source);
                IPAddress destAddress = domain.NCC.DirectoryRequest(destination);
                bool flag = false;
                Console.WriteLine("Checking policy...");
                flag = domain.NCC.PolicyRequest(sourceAddress, destAddress);

                if (sourceAddress != null && destAddress != null)
                { //RC w swoim pliku ma odległość przy danym source i destination więc to też do zrobienia
                    RoutingResult routingResult = domain.RC.DijkstraAlgorithm(sourceAddress, destAddress, domain.RC.cables, domain.RC.lrms, speed); // prototyp funkcji Dijkstry
                    List<int> idxOfSlots = new List<int>();
                    for (int i = 0; i < 10; i++)
                    {
                        if (routingResult.slots[i])
                        {
                            idxOfSlots.Add(i);
                            Console.WriteLine("Index of slot: " + i);
                        }
                    }
                    foreach (var node in routingResult.Path)
                    {
                        Console.WriteLine("Chosen node: " + node.ToString());
                    }
                    List<byte> bufferToSend = new List<byte>();
                    int ct = 0;
                    foreach (var cab in routingResult.nodeAndPortsOut)
                    {
                        bool flaga = false;
                        Socket socket = domain.CC.SocketfromIP[cab.Key];
                        Console.WriteLine("Adres: " + cab.Key + " port out: " + cab.Value);
                        foreach (var cab1 in routingResult.nodeAndPortsIn)
                        {
                            if (cab1.Key.Equals(cab.Key))
                            {
                                Console.WriteLine("Port in: " + cab1.Value);
                                bufferToSend.AddRange(Encoding.ASCII.GetBytes("ACK"));
                                bufferToSend.AddRange(BitConverter.GetBytes(idxOfSlots[0]));
                                bufferToSend.AddRange(BitConverter.GetBytes(idxOfSlots[idxOfSlots.Count - 1]));
                                bufferToSend.AddRange(BitConverter.GetBytes(cab.Value));
                                bufferToSend.AddRange(BitConverter.GetBytes(cab1.Value));
                                socket.BeginSend(bufferToSend.ToArray(), 0, bufferToSend.ToArray().Length, 0,
                        new AsyncCallback(SendCallBack), socket);
                                bufferToSend.Clear();
                                flaga = true;
                                break;
                            }
                        }
                        if (!flaga)
                        {
                            bufferToSend.AddRange(Encoding.ASCII.GetBytes("ACK"));
                            bufferToSend.AddRange(BitConverter.GetBytes(idxOfSlots[0]));
                            bufferToSend.AddRange(BitConverter.GetBytes(idxOfSlots[idxOfSlots.Count - 1]));
                            bufferToSend.AddRange(BitConverter.GetBytes(cab.Value));

                            socket.BeginSend(bufferToSend.ToArray(), 0, bufferToSend.ToArray().Length, 0,
                        new AsyncCallback(SendCallBack), socket);
                            Console.WriteLine("Send to host: " + cab.Key);
                            bufferToSend.Clear();
                            flaga = false;
                        }
                    }

                }
                else
                {
                    /*Thread thread = new Thread(connectWithSecondDomain);
                    thread.Start();*/

                    List<byte> buffer = new List<byte>();
                    buffer.AddRange(Encoding.ASCII.GetBytes("RC-giveDomainPoint " + sourceAddress.ToString() + " " + destination + " " + speed));
                    // Socket socket = domain.CC.SocketfromIP[IPAddress.Parse("127.0.0.1")];
                    //socket.BeginSend(buffer.ToArray(),0,buffer.ToArray().Length,0, new AsyncCallback(SendCallBack), socket);
                    
                    //Console.WriteLine("Connected");
                    domain.domainClient.Send(buffer.ToArray());
                    Console.WriteLine("Connected");
                   // domain.domainClient.Disconnect(true);
                    //domain.domainClient.Send(buffer.ToArray());
                }
            }
            if (message[0].Equals("RC-giveDomainPoint"))
            {
                Console.WriteLine("Rc-giveDomainPoint");
                IPAddress source = IPAddress.Parse(message[1]);
                String destination = message[2];
                Console.WriteLine("Checking in other directory...");
                IPAddress destAddress = domain.NCC.DirectoryRequest(destination);
                int speed = int.Parse(message[3]);
                StreamReader streamReader = new StreamReader("domainPoints.txt");
                IPAddress borderAddress = null;
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    if ((IPAddress.Parse(line.Split(' ')[0]).Equals(source) && IPAddress.Parse(line.Split(' ')[1]).Equals(destAddress)) || (IPAddress.Parse(line.Split(' ')[0]).Equals(destAddress) && IPAddress.Parse(line.Split(' ')[1]).Equals(source)))
                    {
                        borderAddress = IPAddress.Parse(line.Split(' ')[2]);
                        break;
                    }

                }
               // domain.domainClient.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), domain.secondDomainPort));
                domain.domainClient.Send(Encoding.ASCII.GetBytes("RC-SecondDomainTopology " + borderAddress.ToString() + " " + source.ToString() + " " + speed + " " + destAddress.ToString()));
               // domain.domainClient.Disconnect(true);
                Console.WriteLine("Send " + borderAddress.ToString());
            }
            if (message[0].Equals("SET-REST-CONNECTION"))
            {
                Console.WriteLine("I will set rest connection");
                IPAddress border = IPAddress.Parse(message[1]);
                IPAddress destination = IPAddress.Parse(message[2]);
                int speed = int.Parse(message[3]);
                int startslot = int.Parse(message[4]);
                int lastSlot = int.Parse(message[5]);
                int length_of_prev_domain = int.Parse(message[6]);
                IPAddress source = IPAddress.Parse(message[7]);
                Console.WriteLine("Checking directory...");
                Console.WriteLine("Checking policy...");
                RoutingResult result=domain.RC.SubentDijkstraAlgorithm(border, destination, domain.RC.cables, domain.RC.lrms, speed, startslot, lastSlot, length_of_prev_domain);
                List<byte> bufferToSend = new List<byte>();
                if (result.lastSlot==lastSlot)
                {
                    foreach (var cab in result.nodeAndPortsOut)
                    {
                        bool flaga = false;
                        Socket socket = domain.CC.SocketfromIP[cab.Key];
                        Console.WriteLine("Adres: " + cab.Key + " port out: " + cab.Value);
                        
                        foreach (var cab1 in result.nodeAndPortsIn)
                        {
                            if (cab1.Key.Equals(cab.Key))
                            {
                                Console.WriteLine("Port in: " + cab1.Value);
                                bufferToSend.AddRange(Encoding.ASCII.GetBytes("ACK"));
                                bufferToSend.AddRange(BitConverter.GetBytes(result.startSlot));
                                bufferToSend.AddRange(BitConverter.GetBytes(result.lastSlot));
                                bufferToSend.AddRange(BitConverter.GetBytes(cab.Value));
                                bufferToSend.AddRange(BitConverter.GetBytes(cab1.Value));
                                socket.BeginSend(bufferToSend.ToArray(), 0, bufferToSend.ToArray().Length, 0,
                        new AsyncCallback(SendCallBack), socket);
                                bufferToSend.Clear();
                                flaga = true;
                                break;
                            }
                        }
                        if (!flaga)
                        {
                            bufferToSend.AddRange(Encoding.ASCII.GetBytes("ACK"));
                            bufferToSend.AddRange(BitConverter.GetBytes(result.startSlot));
                            bufferToSend.AddRange(BitConverter.GetBytes(result.lastSlot));
                            bufferToSend.AddRange(BitConverter.GetBytes(cab.Value));

                            socket.BeginSend(bufferToSend.ToArray(), 0, bufferToSend.ToArray().Length, 0,
                        new AsyncCallback(SendCallBack), socket);
                            Console.WriteLine("Send to E-nni: " + cab.Key);
                            bufferToSend.Clear();
                            flaga = false;
                        }
                    }
                    bufferToSend.AddRange(Encoding.ASCII.GetBytes("OK"));
                    domain.domainClient.Send(bufferToSend.ToArray());
                    

                }
                else
                {
                    List<byte> buffer = new List<byte>();
                    buffer.AddRange(Encoding.ASCII.GetBytes("RETRY " + source.ToString() + " " + border.ToString() + " " + speed + " " + result.startSlot + " " + result.lastSlot + " " + result.lengthOfGivenDomain + " " + destination.ToString()));
                    domain.domainClient.Send(buffer.ToArray());               
                }

            }
            if (message[0].Equals("RETRY"))
            {
                Console.WriteLine("I will RETRY");
                IPAddress source = IPAddress.Parse(message[1]);
                IPAddress border = IPAddress.Parse(message[2]);
                int speed = int.Parse(message[3]);
                int startslot = int.Parse(message[4]);
                int lastSlot = int.Parse(message[5]);
                int length_of_prev_domain = int.Parse(message[6]);
                IPAddress destination = IPAddress.Parse(message[7]);
                RoutingResult result = domain.RC.SubentDijkstraAlgorithm(border, destination, domain.RC.cables, domain.RC.lrms, speed, startslot, lastSlot, length_of_prev_domain);
                List<byte> buffer = new List<byte>();
                buffer.AddRange(Encoding.ASCII.GetBytes("SET-REST-CONNECTION " + border.ToString() + " " + destination.ToString() + " " + speed + " " + result.startSlot + " " + result.lastSlot + " " + result.lengthOfGivenDomain + " " + source.ToString()));
                domain.domainClient.Send(buffer.ToArray());
            }
            if(message[0].Equals("OK"))
            {
                foreach (var cab in r.nodeAndPortsOut)
                {
                    bool flaga = false;
                    Socket socket = domain.CC.SocketfromIP[cab.Key];
                    Console.WriteLine("Adres: " + cab.Key + " port out: " + cab.Value);
                    List<byte> bufferToSend = new List<byte>();
                    foreach (var cab1 in r.nodeAndPortsIn)
                    {
                        if (cab1.Key.Equals(cab.Key))
                        {
                            Console.WriteLine("Port in: " + cab1.Value);
                            bufferToSend.AddRange(Encoding.ASCII.GetBytes("ACK"));
                            bufferToSend.AddRange(BitConverter.GetBytes(r.startSlot));
                            bufferToSend.AddRange(BitConverter.GetBytes(r.lastSlot));
                            bufferToSend.AddRange(BitConverter.GetBytes(cab.Value));
                            bufferToSend.AddRange(BitConverter.GetBytes(cab1.Value));
                            socket.BeginSend(bufferToSend.ToArray(), 0, bufferToSend.ToArray().Length, 0,
                    new AsyncCallback(SendCallBack), socket);
                            bufferToSend.Clear();
                            flaga = true;
                            break;
                        }
                    }
                    if (!flaga)
                    {
                        bufferToSend.AddRange(Encoding.ASCII.GetBytes("ACK"));
                        bufferToSend.AddRange(BitConverter.GetBytes(r.startSlot));
                        bufferToSend.AddRange(BitConverter.GetBytes(r.lastSlot));
                        bufferToSend.AddRange(BitConverter.GetBytes(cab.Value));

                        socket.BeginSend(bufferToSend.ToArray(), 0, bufferToSend.ToArray().Length, 0,
                    new AsyncCallback(SendCallBack), socket);
                        Console.WriteLine("Send to Host: " + cab.Key);
                        bufferToSend.Clear();
                        flaga = false;
                    }
                }
            }

            //Domain.NCC.ConnectionRequest(sourceAddress, destAddress, speed);

            if(message[0].Equals("RC-SecondDomainTopology"))
            {
                Console.WriteLine("RC second domain topology");
                IPAddress borderAddress = IPAddress.Parse(message[1]);
                IPAddress sourceAddress = IPAddress.Parse(message[2]);
                int speed = int.Parse(message[3]);
                IPAddress destinationAddress = IPAddress.Parse(message[4]);
                Console.WriteLine("Saved data");
                RoutingResult routing=domain.RC.DijkstraAlgorithm(sourceAddress, borderAddress, domain.RC.cables, domain.RC.lrms, speed);
                r = routing;
                /*List<int> idxOfSlots = new List<int>();
                for (int i = 0; i < 10; i++)
                {
                    if (routing.slots[i])
                    {
                        idxOfSlots.Add(i);
                        Console.WriteLine("Index of slot: " + i);
                    }
                }*/
                // Socket socket = domain.CC.SocketfromIP[IPAddress.Parse("127.0.0.1")];
                List<byte> buffer = new List<byte>();
                buffer.AddRange(Encoding.ASCII.GetBytes("SET-REST-CONNECTION " + borderAddress.ToString() + " "+ destinationAddress.ToString() + " " + speed + " " + routing.startSlot + " " +routing.lastSlot + " " + routing.lengthOfGivenDomain + " " + sourceAddress.ToString()));
                //domain.domainClient.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), domain.secondDomainPort));
                domain.domainClient.Send(buffer.ToArray());
                //domain.domainClient.Disconnect(true);
                // socket.BeginSend(buffer.ToArray(), 0, buffer.ToArray().Length, 0, new AsyncCallback(SendCallBack), socket);


            }
           
            if (message[0].Equals("CC-callin"))
            {
                domain.CC.IPfromSocket.Add(handler, IPAddress.Parse(message[1]));
                domain.CC.SocketfromIP.Add(IPAddress.Parse(message[1]), handler); // router wysyła też swoje LRMy więc trzeba je dodać do RC
                Console.WriteLine("Called in to domain: " +IPAddress.Parse(message[1]));
                domain.RC.nodesToAlgorithm.Add(IPAddress.Parse(message[1]));
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
                while(i<bufferLRM.Count)
                {
                    buffer = bufferLRM.GetRange(i, 16).ToArray();
                    ushort port = (ushort)((buffer[1] << 8) + buffer[0]);
                    Console.WriteLine(port);
                    LinkResourceManager LRM = LinkResourceManager.returnLRM(buffer);
                    i += 16;
                    Console.WriteLine("Port: " +LRM.port);
                    domain.RC.lrms.Add(LRM);
                }
               // Array.Clear(state.buffer, 0, state.buffer.Length);

            }        
            if(message[0].Equals("SUBNETWORK-callin"))
            {
                domain.CC.IPfromSocket.Add(handler, IPAddress.Parse(message[1]));
                domain.CC.SocketfromIP.Add(IPAddress.Parse(message[1]), handler);
                domain.RC.nodesToAlgorithm.Add(IPAddress.Parse(message[1]));
                domain.RC.ipOfSubnet = IPAddress.Parse(message[1]);
                Console.WriteLine("Subnetwork called in: " + IPAddress.Parse(message[1]));
                List<byte> bufferLRM = new List<byte>();
                bufferLRM.AddRange(Encoding.ASCII.GetBytes(message[2]));
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
                    domain.RC.lrms.Add(LRM);
                }


            }
            state.sb.Clear();
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallBack), state);
        }
        

      

           // zwykła dijkstra, trzeba wrzucić ją w RC i napisać prawidłową
            
           



            public static Cable findCableBetweenNodes(IPAddress ip1,IPAddress ip2, List<Cable> cables)
            {
                Cable cable = null;
                for(int i=0; i<cables.Count;i++)
                {
                    if((cables[i].Node1==ip1 && cables[i].Node2==ip2) || (cables[i].Node2 == ip1 && cables[i].Node1 == ip2))
                    {
                        cable = cables[i];
                        break;
                    }
                }
                return cable;
            }
            public static LinkResourceManager findLRM(IPAddress ip, ushort port, List<LinkResourceManager> links)
            {
                LinkResourceManager link = null;
                foreach(var l in links)
                {
                    if (l.IPofNode==ip && l.port == port)
                    {
                        link = l;
                        break;
                    }
                        
                }
                return link;
            }
            private static void Send(Socket handler, String data)
            {
                byte[] byteData = Encoding.ASCII.GetBytes(data);

                handler.BeginSend(byteData, 0, byteData.Length, 0,
                    new AsyncCallback(SendCallBack), handler);
            }

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
        }
}


