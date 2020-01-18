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

                    Domain.RC.DijkstraAlgorithm1(sourceAddress, destAddress, Domain.RC.cables, Domain.RC.lrms, numberOfSlots); // prototyp funkcji Dijkstry
                }

                //Domain.NCC.ConnectionRequest(sourceAddress, destAddress, speed);
                
            }
            if(message[0].Equals("RC-SecondDomainTopology"))
            {
                IPAddress borderAddress = IPAddress.Parse(message[1]);
                Domain.RC.DijkstraAlgorithm1(sourceAddress, borderAddress, Domain.RC.cables, Domain.RC.lrms, numberOfSlots);
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

