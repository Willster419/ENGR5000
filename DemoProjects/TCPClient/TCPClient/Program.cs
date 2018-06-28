using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace TCPClient
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
            TcpClient tcpClient = new TcpClient();
            tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            Console.WriteLine("Client Connecting");
            bool connected = false;
            while(!connected)
            {
                try
                {
                    tcpClient.Connect(ipep);
                    connected = true;
                }
                catch
                {

                }
            }
            Console.WriteLine("Connected");
            NetworkStream ns = tcpClient.GetStream();
            //constantly send
            while (true)
            {
                TCPSend(ns, dataToSend);
                dataToSend = "This is a test client, packet # " + numPacket++;
                System.Threading.Thread.Sleep(1);
            }
        }

        public static void TCPSend(NetworkStream ns, string s)
        {
            Byte[] data = Encoding.UTF8.GetBytes(s);
            try
            {
                ns.Write(data, 0, data.Length);
            }
            catch (System.IO.IOException ex)
            {
                Console.WriteLine("Server Disconnected");
            }
        }
    }
}
