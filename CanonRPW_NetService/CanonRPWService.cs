using CanonRPWService.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Events;
using Serilog;

namespace CanonRPWService
{
    public partial class CanonRPWService : ServiceBase
    {
        TcpServer tcpServer;
        CancellationTokenSource cts = new CancellationTokenSource();
        public CanonRPWService()
        {
            InitializeComponent();

            this.CanStop = true;
            this.CanPauseAndContinue = true;

            //Setup logging
            this.AutoLog = false;

            ((ISupportInitialize)this.EventLog).BeginInit();
            if (!EventLog.SourceExists(this.ServiceName))
            {
                EventLog.CreateEventSource(this.ServiceName, "Application");                
            }
            ((ISupportInitialize)this.EventLog).EndInit();
            this.EventLog.Source = this.ServiceName;
            this.EventLog.Log = "Application";
            EventLog.EntryWritten += new EntryWrittenEventHandler(MyOnEntryWritten);
        }

        public static void MyOnEntryWritten(object source, EntryWrittenEventArgs e)
        {
            //Console.WriteLine($"In event handler: {e.Entry}");
            Log.Information($"In event handler: {e.Entry}");
        }

        protected override void OnStart(string[] args)
        {
            this.EventLog.WriteEntry("Creating tcp server", EventLogEntryType.Information);
            try
            {
                cts = new CancellationTokenSource();
                Task.Run(() => RunServer(cts.Token), cts.Token);
            }
            catch(Exception e)
            {
                this.EventLog.WriteEntry($"creating TCP server error: {e.Message}", EventLogEntryType.Error);
            }
            return;
        }

        private void RunServer(CancellationToken cancellationToken)
        {
            Program.InitializeLogs();
            tcpServer = new TcpServer(Settings.Default.CommandsPort, Settings.Default.EventsPort, cancellationToken);
        }        

        protected override void OnStop()
        {
            try
            {
                cts.Cancel();
                tcpServer.Dispose();
                Log.Information("Service stopped");
                //Log.CloseAndFlush();
            }
            catch (Exception e)
            {
                this.EventLog.WriteEntry($"Stopping service error: {e.Message}", EventLogEntryType.Error);
            }
        }
        public void TestStartupAndStop(string[] args)
        {
            this.OnStart(args);
            Console.ReadLine();
            this.OnStop();
        }
    }
}
