using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Sockets;
using System.Net;
using System.Globalization;
using Tools;
using System.Threading;
//przeczytajcie komnentarze, to tylko koncept

namespace Host
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        Client client;
        RestOfHosts destinationClient { get; set; }
        int selectedSpeed { get; set; }
        bool flag = false;
        public MainWindow()
        {
           
            var args = Environment.GetCommandLineArgs();

            InitializeComponent();
            try
            {
                if (args.Length > 1)
                {
                    client = Client.createHost(args[1]);
                    SendMessage.IsEnabled = false;
                    StopSend.IsEnabled = false;
                    fillTheComboBox();
                }

            }
            catch (Exception e)
            {
                Environment.Exit(1);
            }
            Task.Run(GetConnectionWithCloud); // łączymy się z chmurą
        }


        public void fillTheComboBox()
        {
            comboBox1.Items.Clear();
            Capacity.Items.Clear();
            foreach (var client in client.Neighbours)
            {
               comboBox1.Items.Add(client);
            }
            Capacity.Items.Add(1);
            Capacity.Items.Add(5); //available speed
            Capacity.Items.Add(10);
           

        }
        public void unableButton()
        {
            SendMessage.IsEnabled = !flag;
            StopSend.IsEnabled = flag;
        }
        public void GetConnectionWithCloud()
        {

            try
            {

                client.socketToCloud = new Socket(client.cloudIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp); // Stream uses TCP protocol
                Dispatcher.Invoke(() => ListBox12.Items.Add("This is " + client.clientName));
                client.socketToCloud.Connect(new IPEndPoint(client.cloudIP, client.cloudPort)); //connect with server
                Dispatcher.Invoke(() => ListBox12.Items.Add("This is " + client.clientName));
                Dispatcher.Invoke(() => ListBox12.Items.Add(client.clientName + ": [" + DateTime.UtcNow.ToString("HH:mm:ss.fff",
                                            CultureInfo.InvariantCulture) + "] " + "I got connection with cable cloud"));
                client.socketToCloud.Send(Encoding.ASCII.GetBytes("First Message " + client.clientIP.ToString())); // zgłasza się do chmury
                byte[] buffer1 = new byte[256];

                client.socketToCloud.Receive(buffer1);//node has to know about used ports
                client.usedPorts = givePorts(buffer1);
                Dispatcher.Invoke(() => ListBox12.Items.Add("Received"));


                for (int i = 0; i < client.usedPorts.ToArray().Length; i++)
                {
                    ushort port = client.usedPorts[i];
                    
                    Dispatcher.Invoke(() => ListBox12.Items.Add(port));
                    LinkResourceManager link = new LinkResourceManager(port);
                    link.IPofNode = client.clientIP;
                    client.linkResources.Add(link); // adding LRMs
                }

            }
            catch (SocketException e)
            {
                ListBox12.Items.Add(client.clientName + ": Cant get connection");
                Task.Run(GetConnectionWithCloud);
            }

            Task.Run(ConnectWithDomain); // CPCC łączy się z domeną
        }
        public List<ushort> givePorts(byte[] bytes) // change from bytes to port number
        {
            List<ushort> list = new List<ushort>();
            for (int i = 0; i < bytes.Length; i = i + 2)
            {
                ushort port = (ushort)((bytes[i + 1] << 8) + bytes[i]);
                if (port.Equals(0))
                    break;
                list.Add(port);
            }
            return list;
        }
        public void ConnectWithDomain()
        {
            try
            {

                client.socketToDomain = new Socket(client.cloudIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp); // Stream uses TCP protocol
                client.socketToDomain.Connect(new IPEndPoint(client.cloudIP, client.domainPort)); //connect with server
                Dispatcher.Invoke(() => ListBox12.Items.Add("This is " + client.clientName));
                Dispatcher.Invoke(() => ListBox12.Items.Add(client.clientName + ": [" + DateTime.UtcNow.ToString("HH:mm:ss.fff",
                                            CultureInfo.InvariantCulture) + "] " + "I got connection with management system"));

                List<byte> bufferLRM = new List<byte>();
                List<byte> buffer2 = new List<byte>();
                bufferLRM.AddRange(Encoding.ASCII.GetBytes("CC-callin " + client.clientIP.ToString()+ " "));
                int i = 0;
                foreach(LinkResourceManager lrm in client.linkResources)
                {
                    //bufferLRM.Add(0);
                    bufferLRM.AddRange(lrm.convertToBytes());
                    buffer2.AddRange(lrm.convertToBytes());
                    Console.WriteLine((ushort)((buffer2[i + 1] << 8) + buffer2[i]));
                    i += 16;
                }
               
                
                client.socketToDomain.Send(bufferLRM.ToArray()); // zgłasza się  do Domaina

            }
            catch (SocketException e)
            {
                ListBox12.Items.Add(client.clientName + ": Cant get connection");
                Task.Run(ConnectWithDomain);
            }
            WaitForData();
            // GiveConnectionWithHost(neighbour); // wysyła żądanie polączenia z innymi hostami

        }
        public void GiveConnectionWithHost(RestOfHosts destination)
        {
            byte[] buffer = new byte[16];
            Dispatcher.Invoke(() => ListBox12.Items.Add("Selected speed " + client.clientName));
            client.socketToDomain.Send(Encoding.ASCII.GetBytes("NCC-GET " +client.clientName + " " + destination.Name + " " + selectedSpeed)); //callRequest(adres A, adres B, speed)
            //probnie 10Gbps ale chyba zrobimy mozliwosc wyboru tej szybkosci bitowej, wysyła to callRequest że chce taką przepustowość do takiego hosta
            client.socketToDomain.Receive(buffer); // odpowiedź od Domaina
            
            if(buffer[0].ToString()=="A" && buffer[1].ToString()=="C" && buffer[2].ToString()=="K") // jeśli okej to wyśle ACK
            {
                destination.modulation = BitConverter.ToInt32(buffer, 4); // dla danego sąsiada na podstawie zwróconej długości ścieżki w Routing Controller w Domainze
                // Domain zadecyduje jakiej modulacji użyć
                destination.firstFrequencySlot = BitConverter.ToUInt32(buffer, 8); // używane szczeliny do danego sąsiada
                destination.lastFrequencySlot = BitConverter.ToUInt32(buffer, 12);
                ListBox12.Items.Add("I've got path to " + destination.Name + ". You can start sending messages to this destination.");
                comboBox1.Items.Add(destination); // dodajemy do możliwych adresatów wiadomości danego sąsiada
            }
            WaitForData(); // po uzyskaniu wszystkich informacji przechodzimy w stan taki jak w 1 etapie
        }
        public void WaitForData()
        {
            while (true)
            {
                byte[] buffer = new byte[256];
                client.socketToCloud.Receive(buffer);
                DataStream dataStream = DataStream.toData(buffer);
                //log o tym ze dostałem strumień
            }
        }

        private void comboBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            destinationClient = (RestOfHosts)comboBox1.SelectedItem;
            SendMessage.IsEnabled = true;
        }

        private void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            SendMessage.IsEnabled = false;
            StopSend.IsEnabled = true;
            flag = true;
            Task.Run(async () =>
            {
                while (flag)
                {
                    var flag1 = flag;
                    sendStream();
                    await Task.Delay(3000);
                    if (flag1 != flag)
                    {
                        SendMessage.IsEnabled = true;
                        StopSend.IsEnabled = false;
                        break;
                    }

                }
            });
            
            

        }

        public void sendStream()
        {
           
                DataStream dataStream = new DataStream();
                dataStream.sourceHost = client.clientIP;
                
                dataStream.currentNode = client.clientIP;
                dataStream.currentPort = client.portOut;
            Dispatcher.Invoke(() =>
                {
                    dataStream.payload = textBox1.Text;
                    dataStream.modulation = destinationClient.modulation;
                    dataStream.firstFrequencySlot = destinationClient.firstFrequencySlot;
                    dataStream.lastFrequencySlot = destinationClient.lastFrequencySlot;
                    dataStream.destinationHost = destinationClient.ip;
                });
            client.socketToCloud.Send(dataStream.toBytes());
            
        }

        private void StopSend_Click(object sender, RoutedEventArgs e)
        {
            flag = false;
        }

        private void Request_Click(object sender, RoutedEventArgs e)
        {
            GiveConnectionWithHost(destinationClient);
        }

        private void Capacity_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedSpeed = (int)Capacity.SelectedItem;
        }
    }
}
