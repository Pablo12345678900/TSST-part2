using System;
using System.Net.Sockets;
using System.Text;
using System.Net;
using System.Collections.Generic;
using Tools;
using System.Threading;
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
            foreach(var port in ports)
            {
                LinkResourceManager link = new LinkResourceManager(port);
                link.IPofNode = subnetwork.ip;
                subnetwork.lrms.Add(link);
            }
            List<byte> bufferLRM = new List<byte>();
            bufferLRM.AddRange(Encoding.ASCII.GetBytes("SUBNETWORK-callin " + subnetwork.ip.ToString() + " "));
           // int i = 0;
            foreach (LinkResourceManager lrm in subnetwork.lrms)
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
                Console.WriteLine("Connected to subnetwork: " + IPAddress.Parse(message[1]));
            }
        }
        public static void WaitForData()// dane które przyjdą na adres 10.0.0.4 wiadomo że to będą styki czyli trzeba tylko ->znaleźć styk-> zmienić port i odesłać do chmury
        {
            while(true)
            {
                byte[] buffer = new byte[128];
                subnetwork.subClientToCloud.Receive(buffer);
            }
        }
        // styki można utworzyć przy zgłaszaniu się do chmury tej podsieci: wiemy że port po jednej stronie to x a po drugiej to x+1, z portów które zwróciło, szukamy gdzie jest różnica w portach o 1

        }
}
