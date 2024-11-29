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
        static int interval = 2500;
        static bool logState = false;

        static List<KeyValuePair<string, int>> resources = new List<KeyValuePair<string, int>>();
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
                Monitor();
                string command = "";
                port = DetectArduinoPort();
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
