# Arduino Stat Service

## Arduino Based System Stat Viewer

This is a small Windows service and a Arduino based Serial reciver.

The Windows Service sends system stats on a pre configred timer to Arduino using Serial. The Arduino recives the data adn decodes the vales and display it in a 0.96inch OLED display/s.

Currently following stats are added to the Windows Service:

	CPU Temp
	CPU Utilization
	CPU Power
	GPU Temp
	GPU Utilization
	GPU Clock

Addtional parameters can be added to the service!

The current data is sent as a string.
Ex:
	
	0320650400691353038

	032  - CPU Utlization
	065  - CPU Temp
	040  - CPU Power
	069  - GPU Temp
	1353 - GPU Clock
	038  - GPU Utilization 

Default parameter values are:

	Baud Rate - 500000
	Port      - COM5 // Needs to change according to your USB port
	Intervel  - 2500 // This is in Millis


### How to deploy:

#### Service Build

1. Microsoft Visual Studio.
2. Add .NET develpment tools without extras(downloads unnecessary packages for this build)
3. Clone the repository to you computer.
4. Open project by double clicking TempService.sln file.
5. Change the parameters in the (COM port, Baud Rate) ArduinoSerialService.cs file.
6. Right click on project from Solution Explorer and click Build.
7. Open Command Prompt as Administrtor.
8. Navigate to the .NET installation folder.

	`cd C:\Windows\Microsoft.NET\Framework\v4.0.30319` 
9. Execute the following command to install the service.

	`InstallUtil.exe "PATH TO YOUR BUILD"`
10. The Service will automatically install and will start.
11. You can view the running service through Services(services.msc).
12. If you wish to remove the service from the system execute following command:
	
	`InstallUtil.exe -u "PATH TO YOUR BUILD"`

#### Arduino Setup

1. Install Arduino IDE from official site(www.arduino.cc).
2. Open the Serial.ino file inside the SerialReciver folder.
3. Select the COM port and board.
4. Change the Baud Rate if you changed it in the Windows Service.
5. Click Compile and Upload button.

### Bill Of Materials

1. Arduino Uno or any USB compatible Arduino x 1.
2. 0.96in OLED 4 pin Display x 4.
3. USB Cable.
4. Wires

### Arduino Wiring

#### CPU Temp Display

Display | Arduino |
--- | --- |
VCC | 3.3v|
GND | GND|
SCL | A5 |
SDA | A4 |

#### CPU Utilization
ETA
#### CPU Power
ETA
#### GPU Temp Display
ETA
#### GPU Utilization
ETA
#### GPU Clock
ETA

### Troubleshooting:

#### Display shows ""No Data!" message.

This means the Windows Service is inactive. Start the ""Arduino Serial Service"" to recive the data from the system.

#### Cannot Start the "Arduino Serial Service" service.

This means the service had an issue and it's throwing errors! To view the errors go to ""Windows Event Viwer"" and under Application Logs you will see an error for Arduino Serial Service. Resolve the error and rebuild the solution.

Made in Sri Lanka with ❤️