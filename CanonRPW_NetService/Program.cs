using CanonRPWService.Properties;
using Serilog.Events;
using Serilog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace CanonRPWService
{
    internal static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        static void Main(string[] args)
        {   
            bool startAsConsole = false;
            if(args != null && args.Length != 0)
            {
                switch (args[0])
                {
                    case "--console":
                        startAsConsole = true;
                        break;
                    case "--install":
                        try
                        {
                            var appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                            System.Configuration.Install.ManagedInstallerClass.InstallHelper(new string[] { appPath });
                        }
                        catch (Exception ex) 
                        { 
                            Log.Fatal(ex.Message, ex); 
                        }
                        break;
                    case "--uninstall":
                        try
                        {
                            var appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                            System.Configuration.Install.ManagedInstallerClass.InstallHelper(new string[] { "/u", appPath });
                        }
                        catch (Exception ex) 
                        {
                            Log.Fatal(ex.Message, ex);
                        }
                        break;
                }
            }

            if (!startAsConsole)
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                    new CanonRPWService()
                };
                ServiceBase.Run(ServicesToRun);
            }
            else
            {

                if (Environment.UserInteractive)
                {
                    //InitializeLogs();
                    CanonRPWService service1 = new CanonRPWService();
                    service1.TestStartupAndStop(null);
                }                
            }
        }
        public static void InitializeLogs()
        {
            string logFileName = $"{System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\\logs\\log_{DateTime.Now.ToString("yyyy-MM-dd")}.txt";
            Log.Logger = new LoggerConfiguration()
                            .MinimumLevel.Debug()
                            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                            .Enrich.FromLogContext()
                            .WriteTo.File(logFileName, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                            .WriteTo.Console()
                            .CreateLogger();
            try
            {
                Log.Information("Starting up the Service");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "There was a problem starting Service");
                return;
            }
        }
    }
}
