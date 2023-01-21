using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanonRPWService.DetectorAPI
{
    /// <summary>
    /// CaptureEngineUI state
    /// </summary>
    public enum States
    {
        NotInitialized,         // Before initialization
        Initialized,            // Finished with initialization (Before calling StartUpCaptureEngine)
        CaptureEngineStarted,   // Finished with StartUpCaptureEngine
        PowerBoxActivated,      // Finished with Power Box activation
        ReadyQC,                // Memory for QC is allocated
        QC,                     // Processing QC
        CloseCapture            // Finished with CloseCaptureDevice
    };
}
