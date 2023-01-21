using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanonRPWService.DetectorAPI
{
    /// <summary>
    /// Power Box class for CaptureEngineUI
    /// </summary>
    public class PowerBox
    {
        /// <summary>
        /// ID
        /// </summary>
        private int id = -1;

        /// <summary>
        /// Wireless type for PBox
        /// </summary>
        private bool isWireless = false;

        /// <summary>
        /// Own Address
        /// </summary>
        private string ownAddress = "";


        /// <summary>
        /// Target Address
        /// </summary>
        private string targetAddress = "";

        /// <summary>
        /// Sensor name
        /// </summary>
        private string sensorName = "";

        /// <summary>
        /// DataType
        /// </summary>
        private byte dataType = 0x00;

        /// <summary>
        /// Constructor
        /// </summary>
        public PowerBox()
        {
            DataType = 1;
        }

        /// <summary>
        /// Get/Set ID
        /// </summary>
        public string ID
        {
            get
            {
                if (id < 0)
                {
                    return "New";
                }
                else
                {
                    return id.ToString();
                }
            }
            set
            {
                id = int.Parse(value);
            }
        }

        /// <summary>
        /// Get/Set Wireless type for PBox
        /// </summary>
        public bool IsWireless
        {
            get
            {
                return isWireless;
            }
            set
            {
                isWireless = value;
            }
        }

        /// <summary>
        /// Get/Set Own Address
        /// </summary>
        public string OwnAddress
        {
            get
            {
                return ownAddress;
            }
            set
            {
                ownAddress = value;
            }
        }


        /// <summary>
        /// Get/Set Target Address
        /// </summary>
        public string TargetAddress
        {
            get
            {
                return targetAddress;
            }
            set
            {
                targetAddress = value;
            }
        }

        /// <summary>
        /// Get/Set SensorName
        /// </summary>
        public string SensorName
        {
            get
            {
                return sensorName;
            }
            set
            {
                sensorName = value;
            }
        }

        /// <summary>
        /// Get/Set DataType
        /// </summary>
        public byte DataType
        {
            get
            {
                return dataType;
            }
            set
            {
                dataType = value;
            }
        }
    }
}
