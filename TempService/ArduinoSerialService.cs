using LibreHardwareMonitor.Hardware;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
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
        static int interval = 1500;
        static bool logState = false;

        static Dictionary<string, int> componentList = new Dictionary<string, int>()
        {
            { "Load CPU Total", 3 },
            { "Temperature CPU Package", 3 },
            { "Power CPU Cores", 3 },
            { "Temperature GPU Core", 3 },
            { "Clock GPU Core", 4 },
            { "Load GPU Core", 3 },

        };
        private SerialPort serialPort;
        System.Timers.Timer timer;

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

        public static string Monitor(Dictionary<string, int> components)
        {
            Computer computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
            };

            computer.Open();
            computer.Accept(new UpdateVisitor());

            string rawData = "";

            foreach (IHardware hardware in computer.Hardware)
            {
                if (ENABLE_DEBUG) Console.WriteLine("Hardware: {0}", hardware.Name);

                foreach (ISensor sensor in hardware.Sensors)
                {
                    if (components.ContainsKey(sensor.SensorType.ToString() + " " + sensor.Name.ToString()))
                    {
                        rawData += Math.Round(Double.Parse(sensor.Value.ToString())).ToString().PadLeft(components[sensor.SensorType.ToString() + " " + sensor.Name.ToString()], '0');
                    }
                }
            }
            computer.Close();
            return rawData += "\\n";
        }

        private string DetectArduinoPort()
        {
            ManagementScope connectionScope = new ManagementScope();
            SelectQuery serialQuery = new SelectQuery("SELECT * FROM Win32_SerialPort");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(connectionScope, serialQuery);

            try
            {
                foreach (ManagementObject item in searcher.Get())
                {
                    string desc = item["Description"].ToString();
                    string deviceId = item["DeviceID"].ToString();

                    if (desc.Contains("Arduino"))
                    {
                        return deviceId;
                    }
                }
            }
            catch (ManagementException e)
            {
                eventLog.WriteEntry("Error Detecting Port: " + e.StackTrace, EventLogEntryType.Error);
            }

            return null;
        }

        public void OnTimedEvent(object sender, ElapsedEventArgs args)
        {
            try
            {
                string command = Monitor(componentList);
                port = DetectArduinoPort();
                if (ENABLE_DEBUG) eventLog.WriteEntry("Data: " + command);
                serialPort.Write(command);
                if (ENABLE_DEBUG) Console.WriteLine(command);
                
            }
            catch (Exception e)
            {
                if (e is InvalidOperationException || e is IOException)
                {
                    if(port != null)
                    {
                        serialPort = new SerialPort(port, baudRate, Parity.None, 8, StopBits.One);
                        serialPort.Open();
                        logState = false;
                    }
                    if (!logState)
                    {
                        eventLog.WriteEntry("Arduino Disconnected: " + e.Message, EventLogEntryType.Error);
                        logState = true;
                    }
                }
                else
                {
                    eventLog.WriteEntry("Error at: " + e.StackTrace + " " + e.GetType().Name, EventLogEntryType.Error);

                }
            }
        }



        public ArduinoSerialService()
        {
            InitializeComponent();
            eventLog = new EventLog();

            if (!EventLog.SourceExists("Arduino Serial Log"))
            {
                EventLog.CreateEventSource("Arduino Serial Log", "Arduino Serial Service Log");
            }
            eventLog.Source = "Arduino Serial Log";
            eventLog.Log = "Arduino Serial Service Log";
                
            port = DetectArduinoPort();
                
            if(port == "" || port == null)
            {
                eventLog.WriteEntry("No Arduino Port Detected!" + port, EventLogEntryType.Error);
            }

            while (port == "" || port == null)
            {
                port = DetectArduinoPort();
            }

            eventLog.WriteEntry("Detected Arduino Port at: " + port, EventLogEntryType.Information);

            serialPort = new SerialPort(port, baudRate, Parity.None, 8, StopBits.One);
            
            timer = new System.Timers.Timer
            {
                Interval = interval
            };
            timer.Elapsed += OnTimedEvent;
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
