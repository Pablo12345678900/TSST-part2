using System;
using System.Net.Sockets;
using System.Text;
using System.Net;
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
            subnetwork.subServer.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), (int)subnetwork.port));
            subnetwork.subClient = new Socket(IPAddress.Parse("127.0.0.1").AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            subnetwork.subServer.Listen(50);
            Console.WriteLine((int)subnetwork.portDomain + " " + (int)subnetwork.port);
            subnetwork.subClient.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"),(int)subnetwork.portDomain));
            
            subnetwork.subClient.Send(Encoding.ASCII.GetBytes("SUBNETWORK-callin " + subnetwork.ip));
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
        }
        }
}
