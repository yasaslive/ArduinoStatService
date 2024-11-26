# TempService

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


Made in Sri Lanka with ❤️