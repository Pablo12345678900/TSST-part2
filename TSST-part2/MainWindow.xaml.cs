using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Net;
using System.Net.Sockets;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Tools;
using System.Data;
using System.IO;
using System.Threading;
using System.Globalization;

namespace CableCloud
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    ///
    //
    /// </summary>
// State object for reading client data asynchronously  
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


    public partial class MainWindow : Window
    {
        public ManualResetEvent thread1 = new ManualResetEvent(false);
        public Cloud cableCloud { get; set; }
        public Socket socketServer { get; set; }
        public Dictionary<Socket, IPAddress> SocketFromIP = new Dictionary<Socket, IPAddress>();
        public Dictionary<IPAddress, Socket> IPFromSocket = new Dictionary<IPAddress, Socket>();

        public Dictionary<Socket, List<ushort>> usedPortsOfNode = new Dictionary<Socket, List<ushort>>();
        public Cable cable;
        public MainWindow()
        {
            var args = Environment.GetCommandLineArgs();
            InitializeComponent();
            try
            {

                cableCloud = Cloud.createCloud("DataForCloud.txt");

            }
            catch (Exception e)
            {
                Console.WriteLine("Failure, wrong arguments");
                Environment.Exit(1);
            }
            fillTheComboBox();
            Task.Run(RunCloudServer);
        }

        public void fillTheComboBox()
        {
            foreach (var cable in cableCloud.cables)
            {
                Cables.Items.Add(cable);
            }
        }

        public void RunCloudServer()
        {
            // cloud is waiting for events            

            socketServer = new Socket(cableCloud.cloudIp.AddressFamily, SocketType.Stream, ProtocolType.Tcp);


            //Logs.Items.Add("Run Server2" + cableCloud.cloudIp + " " + cableCloud.cloudPort);
            try
            {
                socketServer.Bind(new IPEndPoint(cableCloud.cloudIp, cableCloud.cloudPort)); //cloud is the server        
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            socketServer.Listen(50);
            while (true)
            {
                thread1.Reset();
                socketServer.BeginAccept(new AsyncCallback(AcceptCallBack), socketServer);
                thread1.WaitOne();
            }

        }



        public void AcceptCallBack(IAsyncResult asyncResult)
        {
            thread1.Set();
            Socket listener = (Socket)asyncResult.AsyncState;
            Socket handler = listener.EndAccept(asyncResult);
            StateObject stateObject = new StateObject();
            stateObject.workSocket = handler;

            handler.BeginReceive(stateObject.buffer, 0, stateObject.buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallBack), stateObject);

        }

        public void ReceiveCallBack(IAsyncResult asyncResult)
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
            if (message[0].Equals("First") && message[1].Equals("Message"))
            {
                IPAddress node = IPAddress.Parse(message[2]);
                SocketFromIP.TryAdd(handler, node);
                IPFromSocket.TryAdd(node, handler);

                List<ushort> ports = new List<ushort>();
                foreach (var cab in cableCloud.cables)
                {

                    if (cab.Node1 == node)
                    {
                        ports.Add(cab.port1);
                    }
                    if (cab.Node2 == node)
                    {
                        ports.Add(cab.port2);
                    }
                }
                usedPortsOfNode.Add(handler, ports);
                handler.BeginSend(giveArrayInBytes(ports), 0, giveArrayInBytes(ports).Length, 0, new AsyncCallback(SendCallBack), handler);

                Dispatcher.Invoke(() => Logs.Items.Add("[" + DateTime.UtcNow.ToString("HH:mm:ss.fff",
                                              CultureInfo.InvariantCulture) + "] " + node.ToString() + " called in"));
            }
            else
            {
                DataStream dataStream = DataStream.toData(state.buffer);
                Dispatcher.Invoke(() => Logs.Items.Add("[" + DateTime.UtcNow.ToString("HH:mm:ss.fff",
                                              CultureInfo.InvariantCulture) + "] " + "I received package from " + dataStream.currentNode + ":" + dataStream.currentPort + "ID and payload" + " " + dataStream.payload));
                ForwardPackage(state, handler, asyncResult, dataStream);
            }

            state.sb.Clear();
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallBack), state);
        }
        public byte[] giveArrayInBytes(List<ushort> ports)
        {
            List<byte> buffer = new List<byte>();
            foreach (var port in ports)
            {
                buffer.AddRange(BitConverter.GetBytes(port));
            }
            return buffer.ToArray();
        }

        public void ForwardPackage(StateObject stateObject, Socket handler, IAsyncResult asyncResult, DataStream dataStream)
        {


            //Package recPackage=Package.returnToPackage(stateObject.buffer);
            IPAddress node1 = SocketFromIP[handler];
            ushort port1 = dataStream.currentPort;
            //Console.WriteLine(node1.ToString() + " " +port1.ToString());
            for (int i = 0; i < cableCloud.cables.Count; i++)
            {
                if ((cableCloud.cables[i].Node1.Equals(node1) && cableCloud.cables[i].port1.Equals(port1)) && cableCloud.cables[i].stateOfCable == false)
                {
                    Dispatcher.Invoke(() => Logs.Items.Add("[" + DateTime.UtcNow.ToString("HH:mm:ss.fff",
                                            CultureInfo.InvariantCulture) + "] " + "Cable is destroyed, package discarded"));
                    break;
                }

                if ((cableCloud.cables[i].Node2.Equals(node1) && cableCloud.cables[i].port2.Equals(port1)) &&
                    cableCloud.cables[i].stateOfCable == false)
                {
                    Dispatcher.Invoke(() => Logs.Items.Add("[" + DateTime.UtcNow.ToString("HH:mm:ss.fff",
                                                               CultureInfo.InvariantCulture) + "] " + "Cable is destroyed, package discarded"));
                    break;
                }

                if ((cableCloud.cables[i].Node1.Equals(node1) && cableCloud.cables[i].port1.Equals(port1)) && cableCloud.cables[i].stateOfCable)
                {

                    dataStream.currentNode = cableCloud.cables[i].Node2;
                    dataStream.currentPort = cableCloud.cables[i].port2;
                    Socket socket = IPFromSocket[dataStream.currentNode];
                    socket.BeginSend(dataStream.toBytes(), 0, dataStream.toBytes().Length, 0,
                        new AsyncCallback(SendCallBack), socket);
                    Dispatcher.Invoke(() => Logs.Items.Add("[" + DateTime.UtcNow.ToString("HH:mm:ss.fff",
                                             CultureInfo.InvariantCulture) + "] " + "I sent message: ID-> " + "payload: " + dataStream.payload + " to: " + dataStream.currentNode + " on port: " + dataStream.currentPort));
                    break;


                }

                if ((cableCloud.cables[i].Node2.Equals(node1) && cableCloud.cables[i].port2.Equals(port1)) && cableCloud.cables[i].stateOfCable)
                {
                    dataStream.currentNode = cableCloud.cables[i].Node1;
                    dataStream.currentPort = cableCloud.cables[i].port1;
                    Socket socket = IPFromSocket[dataStream.currentNode];
                    socket.BeginSend(dataStream.toBytes(), 0, dataStream.toBytes().Length, 0,
                        new AsyncCallback(SendCallBack), socket);
                    Dispatcher.Invoke(() => Logs.Items.Add("[" + DateTime.UtcNow.ToString("HH:mm:ss.fff",
                                             CultureInfo.InvariantCulture) + "] " + "I sent message: ID-> " + "payload: " + dataStream.payload + " to: " + dataStream.currentNode + " on port: " + dataStream.currentPort));
                    break;
                }
            }
        }

        public void SendCallBack(IAsyncResult asyncResult)
        {
            // Retrieve the socket from the state object.  
            Socket handler = (Socket)asyncResult.AsyncState;
            // Complete sending the data to the remote device.  
            handler.EndSend(asyncResult);
        }
        public void unableButton()
        {
            Destroy.IsEnabled = Cables.SelectedItem != null;
        }

        private void Destroy_Click(object sender, RoutedEventArgs e)
        {

            //cable = (Cable)Cables.SelectedItem;
            cable.stateOfCable = false;
            Cables.SelectedItem = null;
            unableButton();
        }

        private void Cables_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cable = (Cable)Cables.SelectedItem;
            unableButton();
        }

        private void Repair_Click(object sender, RoutedEventArgs e)
        {
            cable.stateOfCable = true;
            Cables.SelectedItem = null;
            unableButton();
        }
    }

}



