using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanonRPWService.DetectorAPI
{
    /// <summary>
    /// DoseMode
    /// </summary>
    enum DoseMode : byte
    {
        Low_level = 0x00,
        Middle_level = 0x10,
        High_level = 0x20,
        High_high_level = 0x30
    }
}
