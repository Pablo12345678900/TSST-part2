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

namespace Host
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Client client;
        RestOfHosts destinationClient { get; set; }
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
            Task.Run(GetConnectionWithCloud);
        }


        public void fillTheComboBox()
        {
            comboBox1.Items.Clear();
            Capacity.Items.Clear();
           /* foreach (var client in client.Neighbours)
            {
                comboBox1.Items.Add(client);
            }*/
            Capacity.Items.Add("1");
            Capacity.Items.Add("5"); //available speed
            Capacity.Items.Add("10");

        }
       /* public void unableButton()
        {
            SendMessage.IsEnabled = !flag;
            StopSend.IsEnabled = flag;
        }*/
        public void GetConnectionWithCloud()
        {

            try
            {

                client.socketToCloud = new Socket(client.cloudIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp); // Stream uses TCP protocol
                client.socketToCloud.Connect(new IPEndPoint(client.cloudIP, client.cloudPort)); //connect with server
                Dispatcher.Invoke(() => ListBox12.Items.Add("This is " + client.clientName));
                Dispatcher.Invoke(() => ListBox12.Items.Add(client.clientName + ": [" + DateTime.UtcNow.ToString("HH:mm:ss.fff",
                                            CultureInfo.InvariantCulture) + "] " + "I got connection with cable cloud"));
                client.socketToCloud.Send(Encoding.ASCII.GetBytes("First Message " + client.clientIP.ToString()));

            }
            catch (SocketException e)
            {
                ListBox12.Items.Add(client.clientName + ": Cant get connection");
                Task.Run(GetConnectionWithCloud);
            }

            Task.Run(ConnectWithManagement);
        }
        public void ConnectWithManagement()
        {
            try
            {

                client.socketToManager = new Socket(client.cloudIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp); // Stream uses TCP protocol
                client.socketToManager.Connect(new IPEndPoint(client.cloudIP, client.cloudPort)); //connect with server
                Dispatcher.Invoke(() => ListBox12.Items.Add("This is " + client.clientName));
                Dispatcher.Invoke(() => ListBox12.Items.Add(client.clientName + ": [" + DateTime.UtcNow.ToString("HH:mm:ss.fff",
                                            CultureInfo.InvariantCulture) + "] " + "I got connection with management system"));
                client.socketToManager.Send(Encoding.ASCII.GetBytes("First Message " + client.clientIP.ToString()));

            }
            catch (SocketException e)
            {
                ListBox12.Items.Add(client.clientName + ": Cant get connection");
                Task.Run(ConnectWithManagement);
            }
            foreach(var neighbour in client.Neighbours)
            {
                GiveConnectionWithHost(neighbour);
            }
        }
        public void GiveConnectionWithHost(RestOfHosts destination)
        {
            byte[] buffer = new byte[8];
            client.socketToManager.Send(Encoding.ASCII.GetBytes(client.clientIP.ToString() + " " + destination.ip.ToString() + " 10"));
            client.socketToManager.Receive(buffer);
            
            if(buffer[0].ToString()=="A" && buffer[1].ToString()=="C" && buffer[2].ToString()=="K")
            {
                client.usedModulation = BitConverter.ToInt32(buffer, 4); // management returns modulation based on the length of calculated path
                ListBox12.Items.Add("I've got path to " + destination.Name + ". You can start sending messages to this destination.");
                comboBox1.Items.Add(destination);
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


        }
    }
}
