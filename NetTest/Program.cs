using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var s = RunServer();
            RunClient();
            RunClient();

            Console.WriteLine("Press any key to quit...");
            Console.ReadKey(true);
        }
        static async Task RunServer()
        {
            Server server = new Server(5325);
            await server.Run(); // ensure it is started before returning control to caller
        }

        static void RunClient()
        {
            var tcpClient = new TcpClient("localhost", 5325);
            Client c = new Client(tcpClient);

            for (int i = 1; i < 5; i++)
            {
                c.SendMessage("Hello! " + i);
                Console.WriteLine("Client: " + c.ReadMessage());
            }

            c.Close();
        }
    }
}
