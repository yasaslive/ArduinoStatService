/*

  Serial.ino

  Read from Serial, output to display.

  Universal 8bit Graphics Library (https://github.com/olikraus/u8g2/)

  Copyright (c) 2018, olikraus@gmail.com
  All rights reserved.

  Redistribution and use in source and binary forms, with or without modification, 
  are permitted provided that the following conditions are met:

  * Redistributions of source code must retain the above copyright notice, this list 
    of conditions and the following disclaimer.
    
  * Redistributions in binary form must reproduce the above copyright notice, this 
    list of conditions and the following disclaimer in the documentation and/or other 
    materials provided with the distribution.

  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND 
  CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, 
  INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF 
  MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE 
  DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR 
  CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
  SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT 
  NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; 
  LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER 
  CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, 
  STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
  ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF 
  ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.  

*/

#include <Arduino.h>
#include <U8g2lib.h>

#ifdef U8X8_HAVE_HW_SPI
#include <SPI.h>
#endif
#ifdef U8X8_HAVE_HW_I2C
#include <Wire.h>
#endif

U8G2_SSD1306_128X64_NONAME_1_SW_I2C u8g2(U8G2_R0, /* clock=*/ SCL, /* data=*/ SDA, /* reset=*/ U8X8_PIN_NONE);   // All Boards without Reset of the Display


// setup the terminal (U8G2LOG) and connect to u8g2 for automatic refresh of the display
// The size (width * height) depends on the selected font and the display

#define U8LOG_WIDTH 20
#define U8LOG_HEIGHT 20

uint8_t u8log_buffer[U8LOG_WIDTH*U8LOG_HEIGHT];

U8G2LOG u8g2log;
String rawData;
String state;
int cpuTemp;
int cpuUtilization;
int cpuPower;
int gpuTemp;
int gpuUtilization;
int gpuClock;

int count = 0;
bool ENABLE_DEBUG = true;

void setup(void) {
  Serial.begin(500000);				// Start reading from Serial communication interface

  u8g2.begin();  
  u8g2.enableUTF8Print();
}

void processData(){
  cpuUtilization = rawData.substring(0,3).toInt();
  cpuTemp = rawData.substring(3,6).toInt();
  cpuPower = rawData.substring(6,9).toInt();
  gpuTemp = rawData.substring(9,12).toInt();
  gpuClock = rawData.substring(12,16).toInt();
  gpuUtilization = rawData.substring(16,19).toInt();
}

void loop(void) {  
  if(Serial.available()){
    rawData = "";
    while(Serial.available() > 0){   
      rawData += Serial.readString();
    }
    count = 0;
    processData();
    
    if(ENABLE_DEBUG){ Serial.println(cpuTemp);}

    if(cpuTemp > 85){
      state = "(Hot🔥)";
    }else if(cpuTemp > 65){
      state = "(Warm)";
    }else if (cpuTemp > 0){
      state = "(Normal)";
    }
  }

  if(count > 20){state = "No Data!";}
  
  u8g2.firstPage();
  do {
    u8g2.setFont(u8g2_font_6x13_tr);		// font for the title
    u8g2.setCursor(0, 13);			// title position on the display
    u8g2.print("CPU Temp: "+state);			// output title
    u8g2.setFont(u8g2_font_inr33_mf);		// set the font for the terminal window
    u8g2.setCursor(0, 64);
    u8g2.print(String(cpuTemp)+"°C");
  } while ( u8g2.nextPage() );
  count++;
}
