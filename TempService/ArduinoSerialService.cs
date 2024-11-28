using LibreHardwareMonitor.Hardware;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace TempService
{
    public partial class ArduinoSerialService : ServiceBase
    {
        private EventLog eventLog;

        static bool ENABLE_DEBUG = false;
        static string port = "COM5";
        static int baudRate = 500000;
        static int interval = 2500;

        static List<KeyValuePair<string, int>> resources = new List<KeyValuePair<string, int>>();
        private SerialPort serialPort;

        public class UpdateVisitor : IVisitor
        {
            public void VisitComputer(IComputer computer)
            {
                computer.Traverse(this);
            }
            public void VisitHardware(IHardware hardware)
            {
                hardware.Update();
                foreach (IHardware subHardware in hardware.SubHardware) subHardware.Accept(this);
            }
            public void VisitSensor(ISensor sensor) { }
            public void VisitParameter(IParameter parameter) { }
        }

        public static void Monitor()
        {
            Computer computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
            };

            computer.Open();
            computer.Accept(new UpdateVisitor());

            foreach (IHardware hardware in computer.Hardware)
            {
                if (ENABLE_DEBUG) Console.WriteLine("Hardware: {0}", hardware.Name);


                foreach (ISensor sensor in hardware.Sensors)
                {
                    int val = (int)sensor.Value;
                    resources.Add(new KeyValuePair<string, int>(sensor.SensorType.ToString() + " " + sensor.Name.ToString(), val));
                }
            }

            computer.Close();
        }

        public void OnTimedEvent(object sender, ElapsedEventArgs args)
        {
            try
            {
                Monitor();
                string command = "";
                foreach (KeyValuePair<string, int> item in resources)
                {
                    if (ENABLE_DEBUG) Console.WriteLine("{0}, {1}", item.Key, item.Value);
                    if (item.Key == "Load CPU Total")
                    {
                        command = item.Value.ToString().PadLeft(3, '0');
                    }
                    else if (item.Key == "Temperature CPU Package")
                    {
                        command += Math.Round(Double.Parse(item.Value.ToString())).ToString().PadLeft(3, '0');
                    }
                    else if (item.Key == "Power CPU Cores")
                    {
                        command += Math.Round(Double.Parse(item.Value.ToString())).ToString().PadLeft(3, '0');
                    }
                    else if (item.Key == "Temperature GPU Core")
                    {
                        command += Math.Round(Double.Parse(item.Value.ToString())).ToString().PadLeft(3, '0');
                    }
                    else if (item.Key == "Clock GPU Core")
                    {
                        command += Math.Round(Double.Parse(item.Value.ToString())).ToString().PadLeft(4, '0');
                    }
                    else if (item.Key == "Load GPU Core")
                    {
                        command += Math.Round(Double.Parse(item.Value.ToString())).ToString().PadLeft(3, '0');
                    }
                }
                if (ENABLE_DEBUG) eventLog.WriteEntry("Data: " + command);
                serialPort.Write(command);
                if (ENABLE_DEBUG) Console.WriteLine(command);
            }
            catch (Exception e)
            {
                eventLog.WriteEntry("Error at: " + e.StackTrace, EventLogEntryType.Error);
            }
        }

        public ArduinoSerialService()
        {
            InitializeComponent();
            eventLog = new EventLog();
            serialPort = new SerialPort(port, baudRate, Parity.None, 8, StopBits.One);

            if (!EventLog.SourceExists("ArduinoSerialLog"))
            {
                EventLog.CreateEventSource("ArduinoSerialLog", "ArduinoSerialServiceLog");
            }
            eventLog.Source = "ArduinoSerialLog";
            eventLog.Log = "ArduinoSerialServiceLog";
            
            System.Timers.Timer timer = new System.Timers.Timer
            {
                Interval = interval
            };
            timer.Elapsed += new ElapsedEventHandler(this.OnTimedEvent);
            timer.AutoReset = true;
            timer.Start();
            
        }

        protected override void OnStart(string[] args)
        {
            if(ENABLE_DEBUG) eventLog.WriteEntry("Service Starting Openning port: "+port);
            serialPort.Open();
        }

        protected override void OnStop()
        {
            if (ENABLE_DEBUG)  eventLog.WriteEntry("Service Stopping! Realesing port: "+port);
            if(serialPort.IsOpen) serialPort.Close();
        }
    }
}
