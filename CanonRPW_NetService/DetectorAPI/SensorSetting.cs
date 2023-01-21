using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanonRPWService.DetectorAPI
{
    /// <summary>
    /// Parameter settings for Sensor.
    /// </summary>
    public class SensorSetting
    {
        /// <summary>
        /// Own address setting.
        /// </summary>
        public string OwnAddress { get; set; }

        /// <summary>
        /// Target address setting.
        /// </summary>
        public string TargetAddress { get; set; }

        /// <summary>
        /// SensorName setting.
        /// </summary>
        public string SensorName { get; set; }

        /// <summary>
        /// IsSupportedWireless setting.
        /// </summary>
        public bool IsSupportedWireless { get; set; }

        /// <summary>
        /// IsSupportedDynamic setting.
        /// </summary>
        public bool IsSupportedDynamic { get; set; }

        /// <summary>
        /// Construnctor.
        /// </summary>
        public SensorSetting() { }

        /// <summary>
        /// Construnctor.
        /// </summary>
        /// <param name="ownAddress">Own address</param>
        /// <param name="targetAddress">Target address</param>
        /// <param name="sensorName">SensorName</param>
        /// <param name="isSupportedWireless">IsSupportedWireless</param>
        /// <param name="isSupportedDynamic">IsSupportedDynamic</param>
        public SensorSetting(string ownAddress, string targetAddress, string sensorName, bool isSupportedWireless, bool isSupportedDynamic)
        {
            this.OwnAddress = ownAddress;
            this.TargetAddress = targetAddress;
            this.SensorName = sensorName;
            this.IsSupportedWireless = isSupportedWireless;
            this.IsSupportedDynamic = isSupportedDynamic;
        }
    }

    /// <summary>
    /// Managing parameter settings for Sensor.
    /// </summary>
    [Serializable()]
    public class SensorSettingConfig
    {
        /// <summary>
        /// SensorSetting list.
        /// </summary>
        private static List<SensorSetting> appList = new List<SensorSetting>();

        /// <summary>
        /// Filename.
        /// </summary>
        private static string fileName = "SensorSettings.config";

        /// <summary>
        /// Get unique instance of SensorSettingList.
        /// </summary>
        public static List<SensorSetting> List { get { return appList; } }

        /// <summary>
        /// Save settings to file.
        /// </summary>
        public static void SaveSettings()
        {
            System.Xml.Serialization.XmlSerializer serializer =
                    new System.Xml.Serialization.XmlSerializer(typeof(List<SensorSetting>));
            System.IO.FileStream fs =
                    new System.IO.FileStream(fileName, System.IO.FileMode.Create);
            serializer.Serialize(fs, appList);
            fs.Close();
        }

        /// <summary>
        /// Load settings from file.
        /// </summary>
        public static void LoadSettings()
        {
            //намучался. Оказывается в сервисе директория по умолчанию не та, в которой находится exe
            string exe = Process.GetCurrentProcess().MainModule.FileName;
            string path = Path.GetDirectoryName(exe);
            fileName = path + "\\SensorSettings.config";
            Log.Information($"================Settings file name is {fileName}");
            if (System.IO.File.Exists(fileName))
            {
                System.Xml.Serialization.XmlSerializer serializer =
                        new System.Xml.Serialization.XmlSerializer(typeof(List<SensorSetting>));
                System.IO.FileStream fs = new System.IO.FileStream(fileName, System.IO.FileMode.Open);
                appList = (List<SensorSetting>)serializer.Deserialize(fs);
                fs.Close();
            }
        }
    }
}
