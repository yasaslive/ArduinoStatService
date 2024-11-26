using LibreHardwareMonitor.Hardware;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Xml.Linq;



namespace TempService
{
    internal static class Program
    {
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new ArduinoSerialService()
            };
            ServiceBase.Run(ServicesToRun);

        }
    }
}
