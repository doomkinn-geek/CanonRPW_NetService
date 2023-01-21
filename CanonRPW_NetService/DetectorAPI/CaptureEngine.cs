using Canon.Medical.DR.GAIA.Engine.DynamicCapture;
using CanonRPWService.DSSDCommands;
using CanonRPWService.Properties;
using CanonRPWService.Spoolers;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CanonRPWService.DetectorAPI
{
    public class CaptureEngine
    {
        public BindingList<PowerBox> PowerBoxList = new BindingList<PowerBox>();
        public States CurrentEngineState { get; set; }

        private PBoxList AssignedPBList = new PBoxList();        
        private OnEndCaptureEventArgs lastEndExposingEventArgs;  // last OnEndCaptureEventArgs
        private CapController xdCapController;
        List<CapController.QCExposureInfo> qcExposureInfoList = new List<CapController.QCExposureInfo>();   // QCExposureinfo List
        private uint currentImageSetId1;
        private uint currentImageSetId3;
        private uint currentImageSetId4;
        private ushort imageSetNum1 = 2;
        private ushort imageSetNum3 = 1;
        private ushort imageSetNum4 = 2;
        private bool imageReceived1 = false;
        private bool imageReceived3 = false;
        private bool imageReceived4 = false;
        private bool calledCloseCapture = false;

        private DateTime onCapturingTime;
        private TimeSpan smallImageTime;
        private TimeSpan halfImageTime;
        private TimeSpan fullImageTime;

        private List<int> AssignedPBIdxList = new List<int>();
        private List<int> BootPBIdxList = new List<int>();
        private List<int> SelectedPBIdxList = new List<int>();
        private byte[] exposureID = null;

        private bool UseTrig1 = true;//checkBox триггер
        private WriteSpooler commandsWriteSpooler { get; }
        private WriteSpooler eventsWriteSpooler { get; }

        private int CurrentSensorIndex { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public CaptureEngine(WriteSpooler _commandsSpooler, WriteSpooler _eventsSpooler)
        {
            Log.Information("==================CaptureEngine");
            lastEndExposingEventArgs = null;
            commandsWriteSpooler = _commandsSpooler;
            eventsWriteSpooler = _eventsSpooler;
            SensorSettingConfig.LoadSettings();
            int i = 0;
            foreach (SensorSetting tmp in SensorSettingConfig.List)
            {
                Log.Information($"==================SensorSetting[{i}]");
                PowerBoxList.Add(new PowerBox());
                PowerBoxList[i].OwnAddress = SensorSettingConfig.List[i].OwnAddress;
                PowerBoxList[i].TargetAddress = SensorSettingConfig.List[i].TargetAddress;
                PowerBoxList[i].IsWireless = SensorSettingConfig.List[i].IsSupportedWireless;                
                PowerBoxList[i].SensorName = SensorSettingConfig.List[i].SensorName;
                Log.Information($"{tmp}");
                i++;
            }
            ClearRAWFolder();
        }

        private void ClearRAWFolder()
        {
            try
            {
                System.IO.DirectoryInfo di = new DirectoryInfo(Settings.Default.ImageDir);

                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }
            }
            catch(Exception e)
            {
                Log.Error("ClearRAWFolder", e);
            }
        }

        void xdCapController_AutoSleepeEvent(object sender, CapEventArgs e)
        {
            Log.Information("CapController.AutoSleepEvent", ShowText("CapController.AutoSleepEvent", e, false));
            eventsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{EventsCodes.EVNT_AutoSleep.ToString()}"));
        }

        void xdCapController_CoolingGotoSleepEvent(object sender, CoolingUnitInfoChangeEventArgs e)
        {
            Log.Information("CapController.CoolingGotoSleepEvent", ShowText("CapController.CoolingGotoSleepEvent", e, false));
            eventsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{EventsCodes.EVNT_CoolingGotoSleep.ToString()}"));
        }

        void xdCapController_CoolingUnitInfoChangeEvent(object sender, CoolingUnitInfoChangeEventArgs e)
        {
            Log.Information("CapController.CoolingUnitInfoChangeEvent", ShowText("CapController.CoolingUnitInfoChangeEvent", e, false));
            eventsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{EventsCodes.EVNT_CoolingUnitInfoChange.ToString()}"));
        }

        void xdCapController_ErrorInfoEvent(object sender, ErrorInfoEventArgs e)
        {
            Log.Information("CapController.ErrorInfoEvent", ShowText("CapController.ErrorInfoEvent", e, false));
            eventsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{EventsCodes.EVNT_ErrorInfo.ToString()}\n{e.ErrorCode}\n{e.ErrorLevel}"));
        }

        void xdCapController_GridInfoChangeEvent(object sender, GridInfoChangeEventArgs e)
        {
            Log.Information("CapController.GridInfoChangeEvent", ShowText("CapController.GridInfoChangeEvent", e, false));
            eventsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{EventsCodes.EVNT_GridInfoChange.ToString()}\n{e.GridInfo.StateMode.ToString()}"));
        }

        void xdCapController_ImageRecvEvent(object sender, ImageRecvEventArgs e)
        {
            Log.Information("CapController.ImageRecvEvent", ShowText("CapController.ImageRecvEvent", e, false));

            if (3 == e.TriggerChannel)
            {
                currentImageSetId3 = e.ImageSetId;
                imageReceived3 = true;
            }
            else if (4 == e.TriggerChannel)
            {
                currentImageSetId4 = e.ImageSetId;
                imageReceived4 = true;
            }

            try
            {
                string fileName = Settings.Default.ImageDir + @"\" + DateTime.Now.ToString("yyyyMMdd-HHmmss.fff") + ".raw";
                saveImage(fileName, e.FrameInfo);
                eventsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{EventsCodes.EVNT_ImageRecv.ToString()}\n" +
                                                                            $"{e.FrameInfo.ImageSizeX}\n" +
                                                                            $"{e.FrameInfo.ImageSizeY}\n" +
                                                                            $"{e.FrameInfo.BitStored}\n" +
                                                                            $"{e.ImageSetId}\n" +
                                                                            $"{fileName}"));
            }
            catch(Exception ex)
            {
                Log.Error("ImageRecvEvent", ex);
                eventsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{EventsCodes.EVNT_ImageRecv.ToString()}\n" +
                                                                            $"{0}\n" +
                                                                            $"{0}\n" +
                                                                            $"{0}\n" +
                                                                            $"{-1}\n" +
                                                                            $"{ex.Message}"));
            }            
        }        

        void xdCapController_SmallImageDataReadyEvent(object sender, ImageRecvEventArgs e)
        {
            Log.Information("CapController.SmallImageDataReadyEvent", ShowText("CapController.SmallImageDataReadyEvent", e, false, smallImageTime));

            currentImageSetId1 = e.ImageSetId;
            imageReceived1 = true;

            try
            {
                string fileName = Settings.Default.ImageDir + @"\small_" + DateTime.Now.ToString("yyyyMMdd-HHmmss.fff") + ".raw";
                saveImage(fileName, e.FrameInfo);
                eventsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{EventsCodes.EVNT_SmallImageDataReady.ToString()}\n" +
                                                                            $"{e.FrameInfo.ImageSizeX}\n" +
                                                                            $"{e.FrameInfo.ImageSizeY}\n" +
                                                                            $"{e.FrameInfo.BitStored}\n" +
                                                                            $"{e.ImageSetId}\n" +
                                                                            $"{fileName}"));
            }
            catch (Exception ex)
            {
                Log.Error("ImageRecvEvent", ex);
                eventsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{EventsCodes.EVNT_SmallImageDataReady.ToString()}\n" +
                                                                            $"{0}\n" +
                                                                            $"{0}\n" +
                                                                            $"{0}\n" +
                                                                            $"{-1}\n" +
                                                                            $"{ex.Message}"));
            }
        }

        void xdCapController_HalfImageDataReadyEvent(object sender, ImageRecvEventArgs e)
        {
            Log.Information("CapController.HalfImageDataReadyEvent", ShowText("CapController.HalfImageDataReadyEvent", e, false, halfImageTime));

            currentImageSetId1 = e.ImageSetId;
            imageReceived1 = true;

            try
            {
                string fileName = Settings.Default.ImageDir + @"\half_" + DateTime.Now.ToString("yyyyMMdd-HHmmss.fff") + ".raw";
                saveImage(fileName, e.FrameInfo);
                eventsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{EventsCodes.EVNT_HalfImageDataReady.ToString()}\n" +
                                                                            $"{e.FrameInfo.ImageSizeX}\n" +
                                                                            $"{e.FrameInfo.ImageSizeY}\n" +
                                                                            $"{e.FrameInfo.BitStored}\n" +
                                                                            $"{e.ImageSetId}\n" +
                                                                            $"{fileName}"));
            }
            catch (Exception ex)
            {
                Log.Error("ImageRecvEvent", ex);
                eventsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{EventsCodes.EVNT_HalfImageDataReady.ToString()}\n" +
                                                                            $"{0}\n" +
                                                                            $"{0}\n" +
                                                                            $"{0}\n" +
                                                                            $"{-1}\n" +
                                                                            $"{ex.Message}"));
            }
        }

        void xdCapController_FullImageDataReadyEvent(object sender, ImageRecvEventArgs e)
        {
            Log.Information("CapController.FullImageDataReadyEvent", ShowText("CapController.FullImageDataReadyEvent", e, false, fullImageTime));

            currentImageSetId1 = e.ImageSetId;
            imageReceived1 = true;

            try
            {
                string fileName = Settings.Default.ImageDir + @"\full_" + DateTime.Now.ToString("yyyyMMdd-HHmmss.fff") + ".raw";
                saveImage(fileName, e.FrameInfo);
                eventsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{EventsCodes.EVNT_FullImageDataReady.ToString()}\n" +
                                                                            $"{e.FrameInfo.ImageSizeX}\n" +
                                                                            $"{e.FrameInfo.ImageSizeY}\n" +
                                                                            $"{e.FrameInfo.BitStored}\n" +
                                                                            $"{e.ImageSetId}\n" +
                                                                            $"{fileName}"));
            }
            catch (Exception ex)
            {
                Log.Error("ImageRecvEvent", ex);
                eventsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{EventsCodes.EVNT_FullImageDataReady.ToString()}\n" +
                                                                            $"{0}\n" +
                                                                            $"{0}\n" +
                                                                            $"{0}\n" +
                                                                            $"{-1}\n" +
                                                                            $"{ex.Message}"));
            }

        }

        /// <summary>
        /// Save image
        /// </summary>
        /// <param name="folder">Save folder name</param>
        /// <param name="fileName">Save file name</param>
        /// <param name="frameInfo">Save frameInfo</param>
        private void saveImage(string fileName, FrameInfo frameInfo)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            Int32 size = frameInfo.ImageSizeX * frameInfo.ImageSizeY * 2;
            byte[] managedArray = new byte[size];
            Marshal.Copy(frameInfo.ImagePtr, managedArray, 0, size);
            using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                fs.Write(managedArray, 0, size);
            }            
        }

        void xdCapController_GridSuppressionDataReadyEvent(object sender, ImageRecvEventArgs e)
        {
            Log.Information("CapController.GridSuppressionDataReadyEvent", ShowText("CapController.GridSuppressionDataReadyEvent", e, false));

            currentImageSetId1 = e.ImageSetId;
            imageReceived1 = true;            
        }

        void xdCapController_OnEndCaptureEvent(object sender, OnEndCaptureEventArgs e)
        {
            Log.Information("CapController.OnEndCaptureEvent", ShowText("CapController.OnEndCaptureEvent", e, false));
            eventsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{EventsCodes.EVNT_OnEndCapture.ToString()}\n{e.TriggerChannel}\n{e.FrameNum}\n{e.ImageSetID}\n" +
                                                                            $"{((byte)xdCapController.PBoxList[CurrentSensorIndex].CapStateMode)}"));

            lastEndExposingEventArgs = e;            
        }

        void xdCapController_OnCapturingEvent(object sender, OnCapturingEventArgs e)
        {            
            exposureID = e.ExposureID;
            Log.Information("CapController.OnCapturingEvent", ShowText("CapController.OnCapturingEvent", e, false));
            eventsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{EventsCodes.EVNT_OnCapturing.ToString()}\n{e.TriggerChannel}\n" +
                $"{((byte)xdCapController.PBoxList[CurrentSensorIndex].CapStateMode)}"));
        }

        void xdCapController_NoBufferEvent(object sender, CapEventArgs e)
        {
            Log.Information("CapController.NoBufferEvent", ShowText("CapController.NoBufferEvent", e, false));
            eventsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{EventsCodes.EVNT_NoBuffer.ToString()}\n" +
                $"{((byte)xdCapController.PBoxList[CurrentSensorIndex].CapStateMode)}"));
        }

        void xdCapController_PerformSelfDiagnosisReport(object sender, PerformSelfDiagnosisEventArgs e)
        {
            Log.Information("CapController.PerformSelfDiagnosisReport", ShowText("CapController.PerformSelfDiagnosisReport", e, false));
            eventsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{EventsCodes.EVNT_PerformSelfDiagnosisReport.ToString()}\n" +
                $"{((byte)xdCapController.PBoxList[CurrentSensorIndex].CapStateMode)}"));

        }

        void xdCapController_OnPowerOnEvent(object sender, OnPowerOnEventArgs e)
        {
            Log.Information("CapController.OnPowerOnEvent", ShowText("CapController.OnPowerOnEvent", e, false));
            BootPBIdxList.Add(e.PBoxIndex);
            eventsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{EventsCodes.EVNT_OnPowerOn.ToString()}\n" +
                                                                            $"{e.SensorInfo.SensorFirmVersion}\n" +
                                                                            $"{e.SensorInfo.SensorFPGAVersion}\n" +
                                                                            $"{e.SensorInfo.SensorSerialNumber}\n" +
                                                                            $"{((byte)xdCapController.PBoxList[CurrentSensorIndex].CapStateMode)}"));
        }

        void xdCapController_OnPowerOffEvent(object sender, OnPowerOffEventArgs e)
        {
            Log.Information("CapController.OnPowerOffEvent", ShowText("CapController.OnPowerOffEvent", e, false));
            eventsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{EventsCodes.EVNT_OnPowerOff.ToString()}\n" +
                $"{((byte)xdCapController.PBoxList[CurrentSensorIndex].CapStateMode)}"));
        }

        void xdCapController_SensorAttachEvent(object sender, SensorAttachEventArgs e)
        {
            Log.Information("CapController.SensorAttachEvent", ShowText("CapController.SensorAttachEvent", e, false));
            eventsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{EventsCodes.EVNT_SensorAttach.ToString()}\n" +
                $"{((byte)xdCapController.PBoxList[CurrentSensorIndex].CapStateMode)}"));
        }

        void xdCapController_SensorDetachEvent(object sender, SensorDetachEventArgs e)
        {
            Log.Information("CapController.SensorDetachEvent", ShowText("CapController.SensorDetachEvent", e, false));
            eventsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{EventsCodes.EVNT_SensorDetach.ToString()}\n" +
                $"{((byte)xdCapController.PBoxList[CurrentSensorIndex].CapStateMode)}"));
        }

        void xdCapController_SensorTempEvent(object sender, SensorTempEventArgs e)
        {
            Log.Information("CapController.SensorTempEvent", ShowText("CapController.SensorTempEvent", e, false));
            eventsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{EventsCodes.EVNT_SensorTemp.ToString()}\n" +
                                                                            $"{e.SensorLevel}\n" +
                                                                            $"{e.SensorTemperature}\n" +
                                                                            $"{e.SensorMargin}\n" +
                                                                            $"{((byte)xdCapController.PBoxList[CurrentSensorIndex].CapStateMode)}"));
        }

        void xdCapController_SensorWirelessRssiInfo(object sender, WirelessRssiInfoChangeEventArgs e)
        {
            Log.Information("CapController.SensorWirelessRssiInfoEvent", ShowText("CapController.SensorWirelessRssiInfoEvent", e, false));
            eventsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{EventsCodes.EVNT_SensorWirelessRssiInfo.ToString()}\n" +
                                                                            $"{e.WirelessRssiInfo.SNR}\n" +
                                                                            $"{e.WirelessRssiInfo.Rssi}\n" +
                                                                            $"{e.WirelessRssiInfo.RssiLevel}"));
        }

        void xdCapController_SensorBatteryInfoEvent(object sender, BatteryInfoChangeEventArgs e)
        {
            Log.Information("CapController.SensorBatteryInfoEvent", ShowText("CapController.SensorBatteryInfoEvent", e, false));
            eventsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{EventsCodes.EVNT_SensorBatteryInfo.ToString()}\n" +
                                                                            $"{e.BatteryInfo.SerialNumber}\n" +
                                                                            $"{e.BatteryInfo.CycleCount}\n" +
                                                                            $"{e.BatteryInfo.ChargeValue}"));
        }

        private void xdCapController_SensorActiveRequestSetEvent(object sender, SensorActiveRequestSetEventArgs e)
        {
            Log.Information("CapController.SensorActiveRequestSetEvent", ShowText("CapController.SensorActiveRequestSetEvent", e, false));
            eventsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{EventsCodes.EVNT_SensorActiveRequestSet.ToString()}\n"));
        }
              

        

        /// <summary>
        /// Initialize CETD library
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Init()
        {
            try
            {
                PBoxList tmpPBList = new PBoxList();

                for (int i = 0; i < PowerBoxList.Count; i++)
                {
                    PowerBox powerBox = PowerBoxList[i];                    

                    try
                    {
                        IPAddress.Parse(powerBox.OwnAddress);
                    }
                    catch
                    {
                        //dataGridView2["ColumnOwnAddress", i].Selected = true;
                        throw new ControlValidationException("Power Box ID " + i.ToString() + " Own Address", ControlValidationExceptionReason.InvalidFormat);
                    }

                    try
                    {
                        IPAddress.Parse(powerBox.TargetAddress);
                    }
                    catch
                    {                        
                        throw new ControlValidationException("Power Box ID " + i.ToString() + " Target Address", ControlValidationExceptionReason.InvalidFormat);
                    }

                    // Create PBoxInstance.
                    // You can get SensroName by calling Maintenance.IndentifySensor function.
                    // Wireless sensors need two PBox instance for wireless(IsWireless = true) and wired(IsWireless = false) communication.
                    PBox pbox = PBox.CreateInstance(powerBox.SensorName, powerBox.OwnAddress, powerBox.TargetAddress, powerBox.IsWireless);

                    if (pbox != null)
                    {
                        tmpPBList.Add(pbox);
                    }
                }

                // Create CapController instance.
                // Calling this function, if you use only static detector.
                // xdCapController = new CapController(tmpPBList, imageSetNum1);
                // Calling this function, if you use dynamic detector.
                xdCapController = new CapController(tmpPBList, imageSetNum1, imageSetNum3, 1, imageSetNum4, 3);

                // Set EventHandlers.
                xdCapController.AutoSleepEvent += new CapController.AutoSleepEventHandler(xdCapController_AutoSleepeEvent);
                xdCapController.CoolingGotoSleepEvent += new CapController.CoolingGotoSleepEventHandler(xdCapController_CoolingGotoSleepEvent);
                xdCapController.CoolingUnitInfoChangeEvent += new CapController.CoolingUnitInfoChangeEventHandler(xdCapController_CoolingUnitInfoChangeEvent);
                xdCapController.ErrorInfoEvent += new CapController.ErrorInfoEventHandler(xdCapController_ErrorInfoEvent);
                xdCapController.FullImageDataReadyEvent += new CapController.ImageRecvEventHandler(xdCapController_FullImageDataReadyEvent);
                xdCapController.GridInfoChangeEvent += new CapController.GridInfoChangeEventHandler(xdCapController_GridInfoChangeEvent);
                xdCapController.GridSuppressionDataReadyEvent += new CapController.ImageRecvEventHandler(xdCapController_GridSuppressionDataReadyEvent);
                xdCapController.HalfImageDataReadyEvent += new CapController.ImageRecvEventHandler(xdCapController_HalfImageDataReadyEvent);
                xdCapController.ImageRecvEvent += new CapController.ImageRecvEventHandler(xdCapController_ImageRecvEvent);
                xdCapController.OnEndCaptureEvent += new CapController.OnEndCaptureEventHandler(xdCapController_OnEndCaptureEvent);
                xdCapController.OnCapturingEvent += new CapController.OnCapturingEventHandler(xdCapController_OnCapturingEvent);
                xdCapController.NoBufferEvent += new CapController.NoBufferEventHandler(xdCapController_NoBufferEvent);

                xdCapController.OnPowerOnEvent += new CapController.OnPowerOnEventHandler(xdCapController_OnPowerOnEvent);
                xdCapController.OnPowerOffEvent += new CapController.OnPowerOffEventHandler(xdCapController_OnPowerOffEvent);

                xdCapController.SensorAttachEvent += new CapController.SensorAttachEventHandler(xdCapController_SensorAttachEvent);
                xdCapController.SensorDetachEvent += new CapController.SensorDetachEventHandler(xdCapController_SensorDetachEvent);
                xdCapController.SensorTempEvent += new CapController.SensorTempEventHandler(xdCapController_SensorTempEvent);
                xdCapController.SmallImageDataReadyEvent += new CapController.ImageRecvEventHandler(xdCapController_SmallImageDataReadyEvent);                

                xdCapController.SensorBatteryInfoEvent += new CapController.SensorBatteryInfoEventHandler(xdCapController_SensorBatteryInfoEvent);
                xdCapController.SensorWirelessRssiInfoEvent += new CapController.SensorWirelessRssiInfoEventHandler(xdCapController_SensorWirelessRssiInfo);
                xdCapController.SensorActiveRequestSetEvent += new CapController.SensorActiveRequestSetEventHandler(xdCapController_SensorActiveRequestSetEvent);

                xdCapController.StartExposureEvent += new CapController.StartExposureEventHandler(xdCapController_StartExposureEvent);
                xdCapController.EndExposureEvent += new CapController.EndExposureEventHandler(xdCapController_EndExposureEvent);

                BootPBIdxList.Clear();

                if (xdCapController.PBoxList.Count > 0)
                {
                    commandsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{CommandsCodes.CMD_Initialize.ToString()}\n0\n" +
                        $"{((byte)xdCapController.PBoxList[CurrentSensorIndex].CapStateMode)}"));
                }
                else
                {
                    commandsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{CommandsCodes.CMD_Initialize.ToString()}\n0\n{((int)StateTypes.Initialize)}"));
                }
                setState(States.Initialized);               
                

            }
            catch (ControlValidationException exception)
            {
                Log.Fatal("Init", exception);
                commandsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{CommandsCodes.CMD_Initialize.ToString()}\n-1\n{((int)StateTypes.Error)}"));                
            }
        }       

        /// <summary>
        /// Start Exposure Event from SyncControlLibrary
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void xdCapController_StartExposureEvent(object sender, CapEventArgs e)
        {
            Log.Information("CapController.StartExposureEvent", ShowText("CapController.StartExposureEvent", e, false));
            eventsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{EventsCodes.EVNT_StartExposure.ToString()}\n"));
        }

        /// <summary>
        /// End Exposure Event from SyncControlLibrary
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void xdCapController_EndExposureEvent(object sender, CapEventArgs e)
        {
            Log.Information("CapController.EndExposureEvent", ShowText("CapController.EndExposureEvent", e, false));
            eventsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{EventsCodes.EVNT_EndExposure.ToString()}\n"));
        }

        /// <summary>
        /// CapController.StartUpCaptureEngine
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void StartUpCaptureEngine()
        {
            Log.Debug("CapController.StartUpCaptureEngine", ShowText("CapController.StartUpCaptureEngine", null, true));
            // Initialize CapController instance and establish connection to PBox.
            int result = xdCapController.StartUpCaptureEngine();

            if (result < 0)
            {
                ShowText("StartUpCaptureEngine", result);
                commandsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{CommandsCodes.CMD_StartUpCaptureEngine.ToString()}\n{result}"));
            }
            else
            {
                setState(States.CaptureEngineStarted);
                commandsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{CommandsCodes.CMD_StartUpCaptureEngine.ToString()}\n{result}"));
            }

        }
        /// <summary>
        /// Set CaptureEngineUIState
        /// </summary>
        /// <param name="state">state</param>
        private void setState(States state)
        {
            CurrentEngineState = state;
        }

        /// <summary>
        /// Выбрать активный сенсор из двух
        /// Схема работы - на 0 сенсор прописываем 2 рабочее место
        /// на 1 сенсор - 3е рабочее место
        /// Томографию запускаем на 0 сенсоре RequestExposurePermit с параметром 1, т.е. время накопления 3 секунды
        /// </summary>
        public void SpecifyActiveSensor(int sensorIndex)
        {
            int result = -1;
            try
            {
                CurrentSensorIndex = sensorIndex;
                

                Log.Debug("CapController.SpecifyActiveSensor", ShowText("CapController.SpecifyActiveSensor", null, true));
                               
                               
                // Set PBoxes assigned internally.
                result = xdCapController.SpecifyActiveSensor(CurrentSensorIndex);
                

                if (result < 0)
                {
                    Log.Error("SpecifyActiveSensor error", ShowText("SpecifyActiveSensor ", result));
                    commandsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{CommandsCodes.CMD_SpecifyActiveSensor.ToString()}\n{result}\n" +
                        $"{((byte)xdCapController.PBoxList[CurrentSensorIndex].CapStateMode)}"));
                }
                else
                {                    
                    setState(States.PowerBoxActivated);                    
                    commandsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{CommandsCodes.CMD_SpecifyActiveSensor.ToString()}\n{result}\n" +
                        $"{((byte)xdCapController.PBoxList[CurrentSensorIndex].CapStateMode)}"));


                    //Сразу после активации уходит в сон, поэтому нет смысла передавать батарею и wi-fi
                    try
                    {
                        //Отправляем информацию о батарее и вай фае сразу после инициализации один раз
                        Thread.Sleep(50);
                        if (xdCapController.PBoxList[CurrentSensorIndex].SensorInfo.BatteryInfo != null)
                        {
                            eventsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{EventsCodes.EVNT_SensorBatteryInfo.ToString()}\n" +
                                                                                            $"{xdCapController.PBoxList[CurrentSensorIndex].SensorInfo.BatteryInfo.SerialNumber}\n" +
                                                                                            $"{xdCapController.PBoxList[CurrentSensorIndex].SensorInfo.BatteryInfo.CycleCount}\n" +
                                                                                            $"{xdCapController.PBoxList[CurrentSensorIndex].SensorInfo.BatteryInfo.ChargeValue}"));
                        }
                        if(xdCapController.PBoxList[CurrentSensorIndex].SensorInfo.WirelessRssiInfo != null)
                        {
                            eventsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{EventsCodes.EVNT_SensorWirelessRssiInfo.ToString()}\n" +
                                                                            $"{xdCapController.PBoxList[CurrentSensorIndex].SensorInfo.WirelessRssiInfo.SNR}\n" +
                                                                            $"{xdCapController.PBoxList[CurrentSensorIndex].SensorInfo.WirelessRssiInfo.Rssi}\n" +
                                                                            $"{xdCapController.PBoxList[CurrentSensorIndex].SensorInfo.WirelessRssiInfo.RssiLevel}"));
                        }
                    }
                    catch(Exception ex)
                    {
                        Log.Warning("Getting battery and wireless info error", ex);
                    }

                }
            }
            catch(Exception e)
            {
                Log.Error("SpecifyActiveSensor", e);
                commandsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{CommandsCodes.CMD_SpecifyActiveSensor.ToString()}\n{result}\n" +
                        $"{((int)StateTypes.Error)}"));
            }
        }

        /// <summary>
        /// Запрос разрешения на экспозицию
        /// </summary>
        /// <param name="xRayStorageTime">Время экспозиции в мс. (0 - 1000, 1 - 3000)</param>
        public void RequestExpPermit(int xRayStorageTime)
        {
            int result = -1;
            TriggerChannelSetting triggerCh1 = null;
            TriggerChannelSetting triggerCh3 = null;
            TriggerChannelSetting triggerCh4 = null;

            Log.Debug("RequestExpPermit", ShowText("CapController.RequestExpPermit", null, true));

            switch (xRayStorageTime)
            {
                case 0:
                    triggerCh1 = new TriggerChannelSetting(1000);
                    break;
                case 1:
                    triggerCh1 = new TriggerChannelSetting(3000);
                    break;
                default:
                    triggerCh1 = new TriggerChannelSetting(1000);
                    break;
            }
            // Set ImageSetId to next Id
            if (null != lastEndExposingEventArgs)
            {
                if (1 == lastEndExposingEventArgs.TriggerChannel)
                {
                    if (1 == currentImageSetId1)
                    {
                        currentImageSetId1 = 0;
                    }
                    else
                    {
                        currentImageSetId1 = 1;
                    }
                }

                if (3 == lastEndExposingEventArgs.TriggerChannel)
                {
                    currentImageSetId3 = 0;
                }

                if (4 == lastEndExposingEventArgs.TriggerChannel)
                {
                    if (1 == currentImageSetId4)
                    {
                        currentImageSetId4 = 0;
                    }
                    else
                    {
                        currentImageSetId4 = 1;
                    }
                }
            }

            // Call this function, if you radiography.
            result = xdCapController.RequestExpPermit(triggerCh1, currentImageSetId1);
            
            if (result < 0)
            {
                Log.Error("RequestExpPermit", ShowText("RequestExpPermit", result));
                commandsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{CommandsCodes.CMD_RequestExpPermit.ToString()}\n{result}\n" +
                                                                                $"{((byte)xdCapController.PBoxList[CurrentSensorIndex].CapStateMode)}"));
            }
            else
            {                
                commandsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{CommandsCodes.CMD_RequestExpPermit.ToString()}\n" +
                                                                                $"{result}\n" +
                                                                                $"{((byte)xdCapController.PBoxList[CurrentSensorIndex].CapStateMode)}"));
            }
        }

        public void RetryRequestExpPermit()
        {
            int result;


            Log.Debug("CapController.RetryRequestExpPermit", ShowText("CapController.RetryRequestExpPermit", null, true));


            // Set ImageSetId to next Id
            if (null != lastEndExposingEventArgs)
            {
                if (1 == lastEndExposingEventArgs.TriggerChannel)
                {
                    if (1 == currentImageSetId1)
                    {
                        currentImageSetId1 = 0;
                    }
                    else
                    {
                        currentImageSetId1 = 1;
                    }
                }

                if (3 == lastEndExposingEventArgs.TriggerChannel)
                {
                    currentImageSetId3 = 0;
                }

                if (4 == lastEndExposingEventArgs.TriggerChannel)
                {
                    if (1 == currentImageSetId4)
                    {
                        currentImageSetId4 = 0;
                    }
                    else
                    {
                        currentImageSetId4 = 1;
                    }
                }
            }
            
            // Call this function, if you radiography under the same condigions.
            result = xdCapController.RetryRequestExpPermit(currentImageSetId1);
            

            if (result < 0)
            {
                Log.Error("RetryRequestExpPermit", ShowText("RetryRequestExpPermit", result));
            }
            commandsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{CommandsCodes.CMD_RetryRequestExpPermit.ToString()}\n{result}\n" +
                $"{((byte)xdCapController.PBoxList[CurrentSensorIndex].CapStateMode)}"));
        }

        public void CloseCaptureDevice()
        {
            int result = 0;
            Log.Information("CapController.CloseCaptureDevice", ShowText("CapController.CloseCaptureDevice", null, true));

            try
            {
                calledCloseCapture = true;

                if (xdCapController != null)
                {
                    // Disconnect sensor and free ImageSetMemory.
                    result = xdCapController.CloseCaptureDevice();
                }

                for (int i = AssignedPBIdxList.Count; i >= 0; i--)
                {
                    this.AssignedPBIdxList.Remove(i);
                }
            }
            catch (Exception e2)
            {                
                Log.Error("CloseCaptureDevice error", e2);
                if (xdCapController != null)
                {
                    commandsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{CommandsCodes.CMD_CloseCaptureDevice.ToString()}\n{result}\n" +
                                                                                        $"{((byte)xdCapController.PBoxList[CurrentSensorIndex].CapStateMode)}"));
                }
                else
                {
                    commandsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{CommandsCodes.CMD_CloseCaptureDevice.ToString()}\n{result}\n" +
                                                                                        $"{240}"));
                }
            }

            if(result >= 0)
            {
                if (xdCapController != null)
                {
                    commandsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{CommandsCodes.CMD_CloseCaptureDevice.ToString()}\n{result}\n" +
                                                                                        $"{((byte)xdCapController.PBoxList[CurrentSensorIndex].CapStateMode)}"));
                }
                else
                {
                    commandsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{CommandsCodes.CMD_CloseCaptureDevice.ToString()}\n{result}\n" +
                                                                                        $"{240}"));
                }
            }
            else
            {
                commandsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{CommandsCodes.CMD_CloseCaptureDevice.ToString()}\n{result}\n" +
                                                                                        $"{240}"));
            }

            BootPBIdxList.Clear();
            setState(States.CloseCapture);

            ClearRAWFolder();
        }

        public void SpecifyDeactivateSensor()
        {
            try
            {
                int result;
                Log.Information("CapController.SpecifyDeactivateSensor", ShowText("CapController.SpecifyDeactivateSensor", null, true));

                if(xdCapController == null)
                {
                    commandsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{CommandsCodes.CMD_SpecifyDeactivateSensor.ToString()}\n{-1}\n{((int)StateTypes.Error)}"));
                    return;
                }
                // Set active sensor unselected internally.
                result = xdCapController.SpecifyDeactivateSensor();

                if (result < 0)
                {
                    Log.Error("SpecifyDeactivateSensor", ShowText("SpecifyDeactivateSensor", result));
                }
                else
                {
                    this.AssignedPBIdxList.Clear();
                    setState(States.CaptureEngineStarted);
                }
                commandsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{CommandsCodes.CMD_SpecifyDeactivateSensor.ToString()}\n{result}\n{((int)StateTypes.Initialized)}"));
            }
            catch(Exception e)
            {
                Log.Error("SpecifyDeactivateSensor", e);
                commandsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{CommandsCodes.CMD_SpecifyDeactivateSensor.ToString()}\n{-1}\n{((int)StateTypes.Error)}"));
            }
        }

        public void RequestSensorReady()
        {
            try
            {
                int result;
                Log.Information("CapController.RequestSensorReady", ShowText("CapController.RequestSensorReady", null, true));

                // Change to Ready state.
                // You can use this function, If you already set exposure condition by calling RequestExpPermit().
                result = xdCapController.RequestSensorReady();

                if (result < 0)
                {
                    Log.Error("RequestSensorReady", ShowText("RequestSensorReady", result));
                }
                commandsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{CommandsCodes.CMD_RequestSensorReady.ToString()}\n{result}\n" +
                    $"{((byte)xdCapController.PBoxList[CurrentSensorIndex].CapStateMode)}"));

                try
                {
                    //Отправляем информацию о батарее и вай фае сразу после инициализации один раз
                    Thread.Sleep(50);
                    if (xdCapController.PBoxList[CurrentSensorIndex].SensorInfo.BatteryInfo != null)
                    {
                        eventsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{EventsCodes.EVNT_SensorBatteryInfo.ToString()}\n" +
                                                                                        $"{xdCapController.PBoxList[CurrentSensorIndex].SensorInfo.BatteryInfo.SerialNumber}\n" +
                                                                                        $"{xdCapController.PBoxList[CurrentSensorIndex].SensorInfo.BatteryInfo.CycleCount}\n" +
                                                                                        $"{xdCapController.PBoxList[CurrentSensorIndex].SensorInfo.BatteryInfo.ChargeValue}"));
                    }
                    if (xdCapController.PBoxList[CurrentSensorIndex].SensorInfo.WirelessRssiInfo != null)
                    {
                        eventsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{EventsCodes.EVNT_SensorWirelessRssiInfo.ToString()}\n" +
                                                                        $"{xdCapController.PBoxList[CurrentSensorIndex].SensorInfo.WirelessRssiInfo.SNR}\n" +
                                                                        $"{xdCapController.PBoxList[CurrentSensorIndex].SensorInfo.WirelessRssiInfo.Rssi}\n" +
                                                                        $"{xdCapController.PBoxList[CurrentSensorIndex].SensorInfo.WirelessRssiInfo.RssiLevel}"));
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("Getting battery and wireless info error", ex);
                }

            }
            catch(Exception e)
            {
                Log.Error("RequestSensorReady", e);
                commandsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{CommandsCodes.CMD_RequestSensorReady.ToString()}\n{-1}\n" +
                    $"{StateTypes.Error}"));
            }
        }

        public void RequestSensorSleep()
        {
            int result;

            Log.Information("CapController.RequestSensorSleep", ShowText("CapController.RequestSensorSleep", null, true));

            // Cange to Sleep state.
            result = xdCapController.RequestSensorSleep();

            if (result < 0)
            {
                Log.Error("RequestSensorSleep", ShowText("RequestSensorSleep", result));
            }
            commandsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{CommandsCodes.CMD_RequestSensorSleep.ToString()}\n{result}\n" +
                $"{((byte)xdCapController.PBoxList[CurrentSensorIndex].CapStateMode)}"));

        }
        public void ResendImage()
        {            
            Log.Information("CapController.ResendImage", ShowText("CapController.ResendImage", null, true));

            // Set ImageSetId to next Id
            if (null != lastEndExposingEventArgs && 1 == lastEndExposingEventArgs.TriggerChannel)
            {
                if (1 == currentImageSetId1) currentImageSetId1 = 0;
                else currentImageSetId1 = 1;
            }
            int result = xdCapController.ResendImage(currentImageSetId1);
            if (result < 0) Log.Error("ResendImage", ShowText("ResendImage", result));
            commandsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{CommandsCodes.CMD_ResendImage.ToString()}\n{result}\n" +
                $"{((byte)xdCapController.PBoxList[CurrentSensorIndex].CapStateMode)}"));
         
        }

        public void SendImage()
        {            
            Log.Information("CapController.SendImage", ShowText("CapController.SendImage", null, true));

            // Set ImageSetId to next Id
            if (null != lastEndExposingEventArgs && 1 == lastEndExposingEventArgs.TriggerChannel)
            {
                if (1 == currentImageSetId1) currentImageSetId1 = 0;
                else currentImageSetId1 = 1;
            }

            int result = xdCapController.SendImage(currentImageSetId1, exposureID);
            if (result < 0) Log.Error("SendImage", ShowText("SendImage", result));
            commandsWriteSpooler.PutCommandForProcessing(new RawDssdCommand($"{CommandsCodes.CMD_SendImage.ToString()}\n{result}\n" +
                $"{((byte)xdCapController.PBoxList[CurrentSensorIndex].CapStateMode)}"));

        }
        public void StartExposureRequest()
        {            
            Log.Information("CapController.StartExposureRequest", ShowText("CapController.StartExposureRequest", null, true));
            if (xdCapController != null)
            {
                xdCapController.StartExposureRequest();
            }
        }

        public void btnEndExposureRequest()
        {            
            Log.Information("CapController.EndExposureRequest", ShowText("CapController.EndExposureRequest", null, true));
            if (xdCapController != null)
            {
                xdCapController.EndExposureRequest();
            }
        }

        internal string[] ShowText(TimeSpan cycleTime)
        {
            string[] text = new string[1];
            text[0] = "CycleTime : " + cycleTime.TotalMilliseconds.ToString() + "ms";
            return text;
        }

        internal string[] ShowText(string api)
        {
            string[] text = new string[1];
            text[0] = api + "is failed";
            return text;
        }

        internal string[] ShowText(string api, int result)
        {
            string[] text = new string[1];
            text[0] = api + "is failed. return:" + result.ToString();
            return text;
        }

        internal string[] ShowText(string target, string src1, string sar2, bool bActive)
        {            
            string pre1 = "Passive : ";
            string pre2 = "Pas-Resp: ";
            string pre3 = "Pas-Req: ";
            if (bActive == true)
            {
                pre1 = "Active : ";
                pre2 = "Act-Req: ";
                pre3 = "Act-Resp: ";
            }

            pre1 = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff") + "  " + pre1;
            pre2 = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff") + "  " + pre2;
            pre3 = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff") + "  " + pre3;

            string[] log ={ pre1 + target,
                            pre2 + src1,
                            pre3 + sar2,
                            "" };
            return log;
        }

        private string MakeFrameInfoText(FrameInfo frameInfo)
        {
            string str = string.Empty;

            System.Reflection.PropertyInfo[] frameInfoProperty = frameInfo.GetType().GetProperties();
            foreach (System.Reflection.PropertyInfo property in frameInfoProperty)
            {
                object obj = property.GetValue(frameInfo, null);
                string strType = property.Name.ToString();
                str += strType + "=" + obj + "; ";
            }

            return str;
        }

        internal string[] ShowText(string target, EventArgs eventarg, bool bActive, TimeSpan procTime)
        {
            string pre1 = "Passive : ";
            string pre2 = "Pas-EventProperty : ";
            string param2 = "";
            if (bActive == true)
            {
                pre1 = "Active : ";
                pre2 = "Act-EventProperty : ";
            }

            pre1 = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff") + "  " + pre1;
            pre2 = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff") + "  " + pre2;

            if (eventarg != null)
            {
                System.Reflection.PropertyInfo[] PropertyInfosReq = eventarg.GetType().GetProperties();
                foreach (System.Reflection.PropertyInfo property in PropertyInfosReq)
                {
                    object str = property.GetValue(eventarg, null);
                    string strType = property.Name.ToString();
                    if (null != str && typeof(FrameInfo) == str.GetType())
                    {
                        param2 += strType + "=[" + MakeFrameInfoText((FrameInfo)str) + "]; ";
                    }
                    else
                    {
                        param2 += strType + "=" + str + "; ";
                    }
                }
            }

            string time = "ProcTime : " + procTime.TotalMilliseconds.ToString() + "ms";

            string[] log = { pre1 + target,
                        pre2 + param2, time,
                        "" };

            return log;
        }

        internal string[] ShowText(string target, EventArgs eventarg, bool bActive)
        {
            string pre1 = "Passive : ";
            string pre2 = "Pas-EventProperty : ";
            string param2 = "";
            if (bActive == true)
            {
                pre1 = "Active : ";
                pre2 = "Act-EventProperty : ";
            }

            pre1 = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff") + "  " + pre1;
            pre2 = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff") + "  " + pre2;

            if (eventarg != null)
            {
                System.Reflection.PropertyInfo[] PropertyInfosReq = eventarg.GetType().GetProperties();
                foreach (System.Reflection.PropertyInfo property in PropertyInfosReq)
                {
                    if (typeof(GridInfo) == property.PropertyType ||
                        typeof(WirelessRssiInfo) == property.PropertyType ||
                        typeof(BatteryInfo) == property.PropertyType ||
                        typeof(CoolingUnitInfo) == property.PropertyType ||
                        typeof(TempInfo) == property.PropertyType)
                    {
                        object gi = property.GetValue(eventarg, null);
                        System.Reflection.PropertyInfo[] chi_propeties = gi.GetType().GetProperties();
                        foreach (var tmp in chi_propeties)
                        {
                            object str = tmp.GetValue(gi, null);
                            string strType = tmp.Name.ToString();
                            if (str != null) param2 += strType + "=" + str + "; ";
                        }
                    }
                    else
                    {
                        object str = property.GetValue(eventarg, null);
                        string strType = property.Name.ToString();
                        if (null != str && typeof(FrameInfo) == str.GetType())
                        {
                            param2 += strType + "=[" + MakeFrameInfoText((FrameInfo)str) + "]; ";
                        }
                        else if (null != str && typeof(byte[]) == str.GetType())
                        {
                            param2 += strType + "=" + BitConverter.ToString(str as byte[]) + "; ";
                        }
                        else
                        {
                            param2 += strType + "=" + str + "; ";
                        }
                    }
                }
            }

            string[] log = { pre1 + target,
                        pre2 + param2,
                        "" };

            return log;
        }        
    }
}

