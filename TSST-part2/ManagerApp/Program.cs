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
using CableCloud;
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
        
        public static Domain Domain;

        static void Main(string[] args)
        {
            
            Domain=Domain.readInfo(args[0]);

            Domain.domainServer.Listen(50);

            while(true)
            {
                Domain.domainDone.Reset();
                Domain.domainServer.BeginAccept(new AsyncCallback(AcceptCallBack), Domain.domainServer);
                Domain.domainDone.WaitOne();
            }
           
           // Thread2.Start();

        }
      
        public static void AcceptCallBack(IAsyncResult asyncResult)
        {
            Domain.domainDone.Set();
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
            // first message must be send to get information about connected socket: First Message <Ip address>
            if (message[0].Equals("NCC-GET")) // żądanie hosta na połączenie
            {
                String source = message[1];
                String destination = message[2];
                int speed = int.Parse(message[3]);
                IPAddress sourceAddress = Domain.NCC.DirectoryRequest(source);
                IPAddress destAddress = Domain.NCC.DirectoryRequest(destination);
                bool flag = false;
                flag = Domain.NCC.PolicyRequest(sourceAddress, destAddress);

                if (sourceAddress != null && destAddress != null)
                {                               
                }
                else
                {
                    Domain.domainClient.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), secondDomainPort));
                    List<byte> buffer = new List<byte>();
                    buffer.AddRange(Encoding.ASCII.GetBytes("RC-giveDomainPoint " + sourceAddress.GetAddressBytes() + " "+ destAddress.GetAddressBytes());                
                    Domain.domainClient.Send(buffer.ToArray());
                }
                if (flag)
                { 
                    Console.WriteLine("You can set connection");
                }
                if (sourceAddress != null && destAddress != null)
                { //RC w swoim pliku ma odległość przy danym source i destination więc to też do zrobienia

                    Domain.RC.DijkstraAlgorithm(sourceAddress, destAddress, Domain.RC.cables, Domain.RC.lrms, numberOfSlots); // prototyp funkcji Dijkstry
                }

                //Domain.NCC.ConnectionRequest(sourceAddress, destAddress, speed);
                
            }
            if(message[0].Equals("RC-SecondDomainTopology"))
            {
                IPAddress borderAddress = IPAddress.Parse(message[1]);
                Domain.RC.DijkstraAlgorithm(sourceAddress, borderAddress, Domain.RC.cables, Domain.RC.lrms, numberOfSlots);
            }
            if(message[0].Equals("CC-callin"))
            {
                Domain.CC.IPfromSocket.Add(handler, IPAddress.Parse(message[1]));
                Domain.CC.SocketfromIP.Add(IPAddress.Parse(message[1]), handler); // router wysyła też swoje LRMy więc trzeba je dodać do RC

            }
            if(message[0].Equals("RC-giveDomainPoint"))
            {
                //zwróci punkt bazując na tym kto jst sourcem a kto destination
                IPAddress ipBorderNode = null;
                ushort portBorderNode=0;
                Domain.domainClient.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), secondDomainPort));
                List<byte> buffer = new List<byte>();
                buffer.AddRange(Encoding.ASCII.GetBytes("RC-SecondDomainTopology " + ipBorderNode.GetAddressBytes() + " " +BitConverter.GetBytes(portBorderNode)));
                Domain.domainClient.Send(buffer.ToArray());
            }
            state.sb.Clear();
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallBack), state);
        }
     
        

      

      

           // zwykła dijkstra, trzeba wrzucić ją w RC i napisać prawidłową
            
            public  static List<Cable> DijkstraAlgorithm(IPAddress source, IPAddress destination, List<Cable> cables, List<LinkResourceManager> links,int requiredSpeed, int numOfSlots, List<IPAddress> Q, List<IPAddress> S)
            {
                List<Cable> usedCables = new List<Cable>();
                int n = Q.Count;
                int j = 0;
                for(int i=0;i<n;i++)
                {
                    if (source == Q[i])
                        break;
                    ++j;
                }
               
                int[] d = new int[n];
                int[] p = new int[n];
                for(int i=0;i<n;i++)
                {
                    if (i == j)
                        d[i] = 0;
                    else
                    {
                        d[i] = int.MaxValue;
                        p[i] = -1;
                    }
                }
                while(Q.Count>0)
                {
                    int min = int.MaxValue;
                    int index = 0;
                    for(int i=0;i<n;i++)
                    {
                        if(d[i]<=min)
                        {
                            min = d[i];
                            index = i;
                        }
                    }
                    IPAddress ip = Q[index];
                    S.Add(ip);
                    Q.RemoveAt(index);
                    List<IPAddress> neighbours = new List<IPAddress>();
                    foreach(var cable in cables)
                    {
                        if(cable.stateOfCable==true && cable.Node1==ip)
                        {
                            neighbours.Add(cable.Node2);
                        }
                        if (cable.stateOfCable == true && cable.Node2 == ip)
                        {
                            neighbours.Add(cable.Node1);
                        }
                    }
                    for(int i=0; i<neighbours.Count;i++)
                    {
                        bool flaga = false;
                        for(int k=0; k<Q.Count;k++)
                        {
                            if(Q[k]==neighbours[i])
                            {
                                flaga = true;
                                break;
                            }

                        }
                        if (flaga)
                        {
                            int w = 0;
                            int u = 0;
                            for(int k=0;k<Q.Count;k++)
                            {
                                if (Q[k] == ip)
                                    u = k;
                                if (Q[k] == neighbours[i])
                                    w = k;
                            }
                            Cable connectingCable=null;
                            foreach(var cable in cables)
                            {
                                if ((cable.Node1 == neighbours[i] && cable.Node2 == ip) || (cable.Node2 == neighbours[i] && cable.Node1 == ip))
                                {
                                    connectingCable = cable;
                                    break;
                                }
                            }
                            if(d[w]>d[u]+connectingCable.length)
                            {
                                d[w] = d[u] + connectingCable.length;
                                p[w] = u;
                            }
                        }
                        else
                            continue;
                    }
                }
                int o = 0;
                for(int i=0;i<Domain.nodesToAlgorithm.Count;i++)
                {
                    if(destination==Domain.nodesToAlgorithm[i])
                    {
                        o = i;
                        break;
                    }
                }
                int h = p[o];
                List<LinkResourceManager> usedLrms = new List<LinkResourceManager>();
                IPAddress ipprev=null;
                while(true)
                {
                    Cable cable = findCableBetweenNodes(Domain.nodesToAlgorithm[o], Domain.nodesToAlgorithm[h], cables);
                    usedCables.Add(cable);
                    if(Domain.nodesToAlgorithm[o]==destination)
                    {
                        IPAddress ip = Domain.nodesToAlgorithm[h];
                        ushort port = 0;
                        if(destination==cable.Node1)
                        {
                            port = cable.port2;
                        }
                        if (destination == cable.Node2)
                            port = cable.port1;

                        LinkResourceManager link = findLRM(ip, port, links);
                        usedLrms.Add(link);
                        ipprev = ip;
                    }
                    else if(Domain.nodesToAlgorithm[o]==ipprev)
                    {
                        IPAddress ip = Domain.nodesToAlgorithm[h];
                        ushort port = 0;
                        if (ipprev == cable.Node1)
                        {
                            port = cable.port2;
                        }
                        if (ipprev == cable.Node2)
                            port = cable.port1;

                        LinkResourceManager link = findLRM(ip, port, links);
                        usedLrms.Add(link);
                        ipprev = ip;
                    }

                    o = h;
                    h = p[o];
                    if (h == -1)
                    {
                        Cable lastCable = findCableBetweenNodes(Domain.nodesToAlgorithm[o], Domain.nodesToAlgorithm[h], cables);
                        usedCables.Add(lastCable);
                        break;
                    }
                }
                int sumOfLength = 0; // odtąd 
                foreach (var used in usedCables)
                {
                    sumOfLength += used.length;
                }
                int modulation = 0;
                if (sumOfLength >= 0 && sumOfLength <= 100) modulation = 64;
                else if (sumOfLength > 100 && sumOfLength <= 200) modulation = 32;
                else if (sumOfLength > 200 && sumOfLength <= 300) modulation = 16;
                else if (sumOfLength > 300 && sumOfLength <= 400) modulation = 8;
                else if (sumOfLength > 400 && sumOfLength <= 500) modulation = 4;
                else if (sumOfLength > 500) modulation = 2;

                double usedFrequency = ((requiredSpeed * 2) / (Math.Log(modulation, 2))) + 10; // 5 GHz przerwa z każdej strony

                numOfSlots = (int)Math.Round(usedFrequency); //dotąd wszystko będzie w Dijkstrze
                int counter = 0;
                int start = 0;
                int finish = 0;
                bool flaga = false;
                foreach(var lrm in usedLrms) /// jesli wyznaczymy najkrótszą ścieżkę i dany lrm nie będzie miał tyle szczelin to można go w następnej iteracji odrzucić,
                // bo przy następnym dijkstrze wyjdzie ściezka dłuższa więc i możliwe że więcej szczelin będzie wymaganych to można go juz nie brać tego lrma pod uwagę
                {
                    for(int i=0; i<lrm.slots.Length;i++)
                    {
                        if (lrm.slots[i] == true)
                            ++counter;
                        if (counter == numOfSlots)
                        {

                        }
                    }
                }



                return usedCables;
            }



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
                    new AsyncCallback(SendCallback), handler);
            }

            private static void SendCallback(IAsyncResult ar)
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
}

