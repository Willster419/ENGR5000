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
            TcpClient tcpClient = new TcpClient(ipep);
            tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            tcpClient.Connect(ipep);
            NetworkStream ns = tcpClient.GetStream();

            //setup the thread for the reciever
            TcpListener tcpListener = new TcpListener(ipep);
            //only need to accept the one connection
            Socket sock = tcpListener.AcceptSocket();
            tcpListener.Start();
            sock.Blocking = true;
            //recieve sock done here
            //use a thread in case we can take a syncronous approach
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += RecieveNetworkData;
            //constantly send
            while (true)
            {
                TCPSend(ns, dataToSend);
                numPacket++;
                System.Threading.Thread.Sleep(500);
            }
        }

        private static void RecieveNetworkData(object sender, DoWorkEventArgs e)
        {
            throw new NotImplementedException();
        }

        public static void TCPSend(NetworkStream ns, string s)
        {
            Byte[] data = Encoding.UTF8.GetBytes(s);
            ns.Write(data, 0, data.Length);
        }

        public static string TCPRecieve(NetworkStream ns)
        {
            Byte[] data = new Byte[256];
            Int32 bytes = ns.Read(data, 0, data.Length);
            return Encoding.UTF8.GetString(data, 0, bytes);
        }

    }
}
