using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetTest
{
    public class Server
    {
        private TcpListener Listener { get; }

        public Server(int port)
        {
            Listener = new TcpListener(port);
        }

        public async Task Run()
        {
            Listener.Start();

            while (true)
            {
                var client = await Listener.AcceptTcpClientAsync();
                ProcessClient(client);
            }
        }

        private void ProcessClient(TcpClient tcpClient)
        {
            Client client = new Client(tcpClient);
            try
            {
                while (true)
                {
                    Console.WriteLine("Server: " + client.ReadMessage());
                    client.SendMessage("Goodbye!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }
        }
    }
}
