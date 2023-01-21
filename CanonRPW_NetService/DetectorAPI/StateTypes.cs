using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanonRPWService.DetectorAPI
{
    //внутри CETD API определена эта структура как private
    //а мне нужно при обработке исключения отправлять эти данные клиентской программе
    //поэтому дублирую здесь с уточненным именем
    public enum StateTypes : byte
    {
        PowerOff = 0,
        Initialize = 0x10,
        Initialized = 0x20,
        Attach = 48,
        Sleep = 0x40,
        HpSleep = 65,
        D_Ready = 80,
        D_ExpReady = 96,
        D_Exposing = 112,
        DS_Exposing = 0x80,
        S_Ready = 144,
        S_HpReady = 145,
        S_ExpReady = 160,
        S_HpExpReady = 161,
        S_Exposing = 176,
        S_HpExposing = 177,
        Wait_Ready = 192,
        HpWait_Ready = 193,
        Error = 240
    }
}
