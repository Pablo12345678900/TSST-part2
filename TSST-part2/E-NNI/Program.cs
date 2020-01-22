using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Tools;
using System.Threading;
namespace E_NNI
{
    class Program
    {
        public static PointBetweenDomains enni;
        static void Main(string[] args)
        {
            enni = PointBetweenDomains.readInfo(args[0]);
            enni.socketToCloud.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), enni.portOfCloud));
            enni.socketToCloud.Send(Encoding.ASCII.GetBytes("First Message " + enni.ip.ToString()));
            byte[] buffer12 = new byte[128];
            enni.socketToCloud.Receive(buffer12);
            Console.WriteLine("received info");
            byte[] buffer = new byte[128];
            int readBytes = 0;
            // readBytes=enni.socketToCloud.Receive(buffer);
            LinkResourceManager lrm = new LinkResourceManager(enni.port1);
            lrm.IPofNode = enni.ip;
            enni.lrms.Add(lrm);
            LinkResourceManager lrm1 = new LinkResourceManager(enni.port2);
            lrm1.IPofNode = enni.ip;
            enni.lrms.Add(lrm1);
            List<byte> buffer1 = new List<byte>();
            buffer1.AddRange(Encoding.ASCII.GetBytes("CC-callin " + enni.ip.ToString() + " "));
            foreach(var lr in enni.lrms)
            {
                buffer1.AddRange(lr.convertToBytes());
            }
            enni.socketToDomain1.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), enni.portDomain1));
            enni.socketToDomain1.Send(buffer1.ToArray());
            buffer1.Clear();
            buffer1.AddRange(Encoding.ASCII.GetBytes("CC-callin " + enni.ip.ToString() + " "));
            foreach (var lr in enni.lrms)
            {
                buffer1.AddRange(lr.convertToBytes());
            }
            enni.socketToDomain2.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), enni.portDomain2));
            enni.socketToDomain2.Send(buffer1.ToArray());
            Thread thread = new Thread(WaitForData);
            thread.Start();
        }
        public static void WaitForData()
        {
            while(true)
            {
                byte[] buffer = new byte[128];
                enni.socketToCloud.Receive(buffer);
                DataStream dataStream = DataStream.toData(buffer);
                if(dataStream.currentPort.Equals(enni.port1))
                {
                    dataStream.currentPort = enni.port2;

                }
                else if(dataStream.currentPort.Equals(enni.port2))
                {
                    dataStream.currentPort = enni.port1;
                }
                enni.socketToCloud.Send(dataStream.toBytes());
            }
        }
        
    }
}
