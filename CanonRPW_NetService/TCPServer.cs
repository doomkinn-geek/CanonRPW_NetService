using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using CanonRPWService.Services;
using CanonRPWService.DSSDCommands;
using CanonRPWService.Spoolers;
using CanonRPWService.DetectorAPI;

namespace CanonRPWService
{
    public class TcpServer : IDisposable
    {
        private TcpListener _commandsServer;
        private TcpListener _eventsServer;
        private TcpClient dssdCommandsClient = new TcpClient();
        private TcpClient dssdEventsClient = new TcpClient();
        private ReadSpooler _commandsReadSpooler;
        private WriteSpooler _commandsWriteSpooler;
        private WriteSpooler _eventsWriteSpooler;
        private CaptureEngine _captureEngine;

        public TcpServer(int commandsPort, int eventsPort, CancellationToken cancellation)
        {
            try
            {
                _commandsWriteSpooler = new WriteSpooler(cancellation);
                _eventsWriteSpooler = new WriteSpooler(cancellation);
                _captureEngine = new CaptureEngine(_commandsWriteSpooler, _eventsWriteSpooler);
                _commandsReadSpooler = new ReadSpooler(cancellation, _captureEngine);
                Log.Information($"Start listening commands at port {commandsPort}");
                //Console.WriteLine($"Start listening commands at port {commandsPort}");
                _commandsServer = new TcpListener(IPAddress.Any, commandsPort);
                _commandsServer.Start();
                Log.Information($"Start transferring events at port {eventsPort}");
                //Console.WriteLine($"Start transferring events at port {eventsPort}");
                _eventsServer = new TcpListener(IPAddress.Any, eventsPort);
                _eventsServer.Start();
                
            }
            catch(Exception e)
            {
                Log.Fatal(e.Message, e);
                return;
            }            
            Task.Run(() => LoopClientCommands(cancellation), cancellation);
            Task.Run(() => LoopClientEvents(cancellation), cancellation);
        }

        public void LoopClientCommands(CancellationToken cancellation)
        {
            try
            {
                while (!cancellation.IsCancellationRequested)
                {
                    // wait for client connection                
                    if (!dssdCommandsClient.Connected)
                    {
                        dssdCommandsClient = _commandsServer.AcceptTcpClient();
                        _commandsWriteSpooler.CommandsClient = dssdCommandsClient;
                        Log.Information("Commands client connected");
                        //Console.WriteLine("Commands client connected");                        
                        Task.Run(() => HandleCommandsClient(cancellation), cancellation);
                    }
                    Thread.Sleep(50);
                }
                Log.Information("LoopClientCommands stopped");
            }
            catch(Exception e)
            {
                Log.Error(e.Message, e);    
            }
        }

        public void LoopClientEvents(CancellationToken cancellation)
        {
            try
            {
                while(!cancellation.IsCancellationRequested)
                {
                    if (!dssdEventsClient.Connected)
                    {
                        dssdEventsClient = _eventsServer.AcceptTcpClient();
                        _eventsWriteSpooler.CommandsClient = dssdEventsClient;
                        Log.Information("Events client connected");
                        //Console.WriteLine("Events client connected");
                        Task.Run(() => HandleEventsClient(cancellation), cancellation);
                    }
                    Thread.Sleep(50);
                }
                Log.Information("LoopClientEvents stopped");
            }
            catch (Exception e)
            {
                Log.Error(e.Message, e);
            }
        }

        public void HandleEventsClient(CancellationToken cancellation)
        {
            try
            {
                if (dssdEventsClient == null) return;
                string sData = null;
                while (dssdEventsClient.Connected || !cancellation.IsCancellationRequested)
                {
                    RawDssdCommand aCommand;
                    try
                    {
                        sData = MessagesReader.ReadMessage(dssdEventsClient.GetStream());                        
                    }
                    catch (IOException e)
                    {
                        Log.Information(e, "events client read socket");
                        break;
                    }
                    catch (Exception e)
                    {
                        Log.Error($"HandleEventsClient: {e.Message}");
                        dssdEventsClient.Close();
                        break;
                    }
                    Log.Information("Events client socket > " + sData);
                    //Console.WriteLine("Events client socket > " + sData);
                    //MessagesWriter.SendMessage($"message '{aCommand.Command}' accepted", dssdCommandsClient.GetStream());
                }
            }
            catch (Exception e)
            {
                Log.Fatal("Handle events client fatal", e);
            }
        }

        public void HandleCommandsClient(CancellationToken cancellation)
        {
            try
            {
                if (dssdCommandsClient == null) return;
                
                string sData = null;

                while (dssdCommandsClient.Connected || !cancellation.IsCancellationRequested)
                {
                    RawDssdCommand aCommand;
                    try
                    {                        
                        sData = MessagesReader.ReadMessage(dssdCommandsClient.GetStream());
                        aCommand = new RawDssdCommand(sData);
                        _commandsReadSpooler.PutCommandForProcessing(aCommand);
                    }
                    catch (IOException e)
                    {
                        Log.Information(e, "commands client read socket");                        
                        break;
                    }
                    catch (Exception e)
                    {
                        Log.Error($"HandleCommandsClient: {e.Message}");
                        dssdCommandsClient.Close();                           
                        break;
                    }
                    Log.Information("Commands client > " + sData);
                    //Console.WriteLine("Commands client > " + sData);                    
                    //MessagesWriter.SendMessage($"message '{aCommand.Command}' accepted", dssdCommandsClient.GetStream());
                }
            }
            catch(Exception e)
            {
                Log.Fatal("Handle commands client fatal", e);
            }
        }

        public void Dispose()
        {
            try
            {
                Log.Information("try to stop servers");
                if (_commandsServer != null)
                {
                    _commandsServer.Stop();
                }
                if(_eventsServer != null)
                {
                    _eventsServer.Stop();
                }
                if(_captureEngine != null)
                    _captureEngine.CloseCaptureDevice();
            }
            catch(Exception e)
            {
                Log.Error(e.Message);
            }
        }
    }
}
