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
            IPEndPoint ip = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 419);
            UdpClient client = new UdpClient();
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.Connect(ip);
            Console.WriteLine("sending test messages...");
            while (true)
            {
                Console.WriteLine("test");
                client.Send(Encoding.ASCII.GetBytes("test\n"), "test\n".Length);
                System.Threading.Thread.Sleep(499);
                break;
            }
        }
    }
}
