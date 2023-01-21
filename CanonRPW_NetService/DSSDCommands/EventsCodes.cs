using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanonRPWService.DSSDCommands
{
    enum EventsCodes
    {
        EVNT_Unknown = 99,
        EVNT_AutoSleep,
        EVNT_CoolingGotoSleep,
        EVNT_CoolingUnitInfoChange,
        EVNT_ErrorInfo,
        EVNT_FullImageDataReady,
        EVNT_GridInfoChange,
        EVNT_GridSuppressionDataReady,
        EVNT_HalfImageDataReady,
        EVNT_ImageRecv,
        EVNT_OnEndCapture,
        EVNT_OnCapturing,
        EVNT_NoBuffer,
        EVNT_OnPowerOn,
        EVNT_OnPowerOff,
        EVNT_SensorAttach,
        EVNT_SensorDetach,
        EVNT_SensorTemp,
        EVNT_SmallImageDataReady,
        EVNT_NotifyCalibrationExposureInfo,
        EVNT_CalibrationProcessFinished,
        EVNT_CalibrationFinished,
        EVNT_NotifySelfDiagnosisExposureInfo,
        EVNT_SelfDiagnosisProcessFinished,
        EVNT_SelfDiagnosisFinished,
        EVNT_SPCCDataUpdated,
        EVNT_SensorBatteryInfo,
        EVNT_SensorWirelessRssiInfo,
        EVNT_SensorActiveRequestSet,
        EVNT_StartExposure,
        EVNT_EndExposure,
        EVNT_PerformSelfDiagnosisReport
    }
}
