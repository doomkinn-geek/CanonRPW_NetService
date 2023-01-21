using CanonRPWService.DetectorAPI;
using CanonRPWService.DSSDCommands;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CanonRPWService.Spoolers
{
    public class ReadSpooler : Spooler
    {
        private CaptureEngine engine;
        public ReadSpooler(CancellationToken cancellation, CaptureEngine _engine)
        {
            engine = _engine;
            Task.Run(() => ProcessIncomingMessages(cancellation), cancellation);
        }
        protected override void ProcessIncomingMessages(CancellationToken cancellation)
        {
            try
            {
                while (!cancellation.IsCancellationRequested)
                {
                    RawDssdCommand message = messageQueue.Take();
                    try
                    {
                        switch (message.Command)
                        {
                            case "C_CMD":
                                Thread.Sleep(2000);
                                Log.Information($"{message.Command} processed");
                                //Console.WriteLine($"{message.Command} processed");
                                break;
                            case "C_CMD2":
                                Thread.Sleep(1500);
                                Log.Information($"{message.Command} processed");
                                //Console.WriteLine($"{message.Command} processed");
                                break;
                            case "C_CMD3":
                                Thread.Sleep(1000);
                                Log.Information($"{message.Command} processed");
                                //Console.WriteLine($"{message.Command} processed");
                                break;
                            case "CMD_Initialize":
                                engine.Init();
                                break;
                            case "CMD_StartUpCaptureEngine":
                                engine.StartUpCaptureEngine();
                                break;
                            case "CMD_SpecifyActiveSensor":
                                int sensorIndex = 0;
                                Int32.TryParse(message.Arguments[0], out sensorIndex);
                                engine.SpecifyActiveSensor(sensorIndex);
                                break;
                            case "CMD_CloseCaptureDevice":
                                engine.CloseCaptureDevice();
                                break;
                            case "CMD_RequestExpPermit":
                                int xRayStorageTime = 0;
                                Int32.TryParse(message.Arguments[0], out xRayStorageTime);
                                engine.RequestExpPermit(xRayStorageTime);
                                break;
                            case "CMD_RetryRequestExpPermit":
                                engine.RetryRequestExpPermit();
                                break;
                            case "CMD_SpecifyDeactivateSensor":
                                engine.SpecifyDeactivateSensor();
                                break;
                            case "CMD_RequestSensorReady":
                                engine.RequestSensorReady();
                                break;
                            case "CMD_RequestSensorSleep":
                                engine.RequestSensorSleep();
                                break;
                            case "CMD_ResendImage":
                                engine.ResendImage();
                                break;
                            case "CMD_SendImage":
                                engine.SendImage();
                                break;
                            default:
                                break;
                        }
                    }
                    catch(Exception ex)
                    {
                        Log.Error($"processing income command: {ex.Message}", ex);
                        continue;
                    }
                }
                Log.Information($"{messageQueue.Count} commands left in readSpooler");
                Log.Information("ReadSpooler stopped");
            }
            catch (Exception e)
            {
                Log.Error(e.Message, e);
            }
        }        
    }
}
