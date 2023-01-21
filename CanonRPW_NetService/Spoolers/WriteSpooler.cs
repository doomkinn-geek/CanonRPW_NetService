using CanonRPWService.DSSDCommands;
using CanonRPWService.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CanonRPWService.Spoolers
{
    public class WriteSpooler : Spooler
    {
        public TcpClient CommandsClient { get; set; }
        public WriteSpooler(CancellationToken cancellation)
        {
            Task.Run(() => ProcessIncomingMessages(cancellation), cancellation);
        }
        protected override void ProcessIncomingMessages(CancellationToken cancellation)
        {
            try
            {
                while (!cancellation.IsCancellationRequested)
                {
                    if (CommandsClient == null)
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                    if(!CommandsClient.Connected)
                    {
                        this.ClearQueue();
                        Thread.Sleep(50);
                        continue;
                    }
                    RawDssdCommand message = messageQueue.Take();
                    MessagesWriter.SendMessage(message.ToString(), CommandsClient.GetStream());
                    Log.Information($"write spooler has sent data: {message}");
                    //Console.WriteLine($"write spooler has sent data: {message}");
                }
                Log.Information($"{messageQueue.Count} commands left in write spooler");
                Log.Information("Write spooler stopped");
            }
            catch (Exception e)
            {
                Log.Error(e.Message, e);
            }
        }
    }
}
