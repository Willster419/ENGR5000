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
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Dashboard
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //ip objects
        IPEndPoint ipep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 419);
        UdpClient client = new UdpClient();
        public MainWindow()
        {
            InitializeComponent();
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.Client.Bind(ipep);
            string result = Encoding.ASCII.GetString(client.Receive(ref ipep));
        }
        public async Task LoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var data = await client.ReceiveAsync();

                // Process the data as needed
            }
        }
    }
}
