using CanonRPWService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestRPWService
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Canon RPW Service client");
            Console.Write("Provide IP: ");
            String ip = Console.ReadLine();

            Console.Write("Provide Port: ");
            int port = Int32.Parse(Console.ReadLine());

            DSSDClient client = new DSSDClient(ip, port);
            Console.ReadLine();
        }
    }
}
