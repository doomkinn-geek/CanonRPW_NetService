using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanonRPWService.DSSDCommands
{
    enum CommandsCodes
    {
        CMD_Unknown = -1,
        CMD_Initialize,
        CMD_StartUpCaptureEngine,
        CMD_SpecifyActiveSensor,
        CMD_RequestExpPermit,
        CMD_RetryRequestExpPermit,
        CMD_CloseCaptureDevice,
        CMD_SpecifyDeactivateSensor,
        CMD_RequestSensorReady,
        CMD_RequestSensorSleep,
        CMD_ResendImage,
        CMD_SendImage,
        CMD_StartExposureRequest,
        CMD_EndExposureRequest
    }
}
