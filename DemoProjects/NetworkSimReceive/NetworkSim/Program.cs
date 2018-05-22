using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace NetworkSim
{
    class Program
    {
        static void Main(string[] args)
        {
            //simulates the network output of the Pi
            //we can test read the data from the console output
            IPEndPoint ip = new IPEndPoint(IPAddress.Parse("10.0.0.69"), 42069);
            UdpClient client = new UdpClient();
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.Client.Bind(ip);
            Console.WriteLine("recieveing test messages...");
            while (true)
            {
                Console.WriteLine(Encoding.UTF8.GetString(client.Receive(ref ip)));
            }
        }
    }
}
