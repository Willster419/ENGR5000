using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.ComponentModel;

namespace TCPClientServer
{
    class Program
    {
        static void Main(string[] args)
        {
            //create TCP client
            //theory is that the TCP client will send, while a TCPlistenre will recieve
            int numPacket = 0;
            string dataToSend = "This is a test client, packet # " + numPacket++;
            IPEndPoint ipep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 34563);
            //recieve sock done here
            TcpListener listener = new TcpListener(ipep);
            listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            Console.WriteLine("Server Connecting");
            listener.Start();
            Console.WriteLine("Connected");
            TcpClient sender = listener.AcceptTcpClient();
            Console.WriteLine("Connection accepted");
            while (true)
            {
                Console.WriteLine(TCPRecieve(sender));
            }
        }

        public static void TCPSend(NetworkStream ns, string s)
        {
            Byte[] data = Encoding.UTF8.GetBytes(s);
            ns.Write(data, 0, data.Length);
        }

        public static string TCPRecieve(TcpClient clinet)
        {
            Byte[] data = new Byte[256];
            try
            {
                return Encoding.UTF8.GetString(data, 0, clinet.GetStream().Read(data, 0, data.Length));
            }
            catch (Exception)
            {
                return "Client Disconnected";
            }
        }

    }
}
