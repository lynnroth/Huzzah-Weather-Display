/**The MIT License (MIT)

Copyright (c) 2015 by Daniel Eichhorn

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

See more at http://blog.squix.ch
*/

#include <Wire.h>
#include <Ticker.h>
#include "ssd1306_i2c.h"
#include "icons.h"


#include <ESP8266WiFi.h>
#include "WeatherClient.h"

#define SDA 14
#define SCL 12
//#define RST 2

#define I2C 0x3D

#define WIFISSID "penguin"
#define PASSWORD "penguinpass"

#define FORECASTAPIKEY "fa6015dea8626f8bb72e494d3631b3dd"

// Pettisville, Ohio
#define LATITUDE 41.53
#define LONGITUDE -84.22

const char* weatherhost = "weather.penguin.nu";
const int weatherhttpPort = 80;

// Initialize the oled display for address 0x3c
// 0x3D is the adafruit address....
// sda-pin=14 and sdc-pin=12
SSD1306 display(I2C, SDA, SCL);
WeatherClient weather;
Ticker ticker;

// this array keeps function pointers to all frames
// frames are the single views that slide from right to left
void (*frameCallbacks[4])(int x, int y) = {drawFrame1, drawFrame2, drawFrame5, drawFrame3};

// how many frames are there?
int frameCount = 4;
// on frame is currently displayed
int currentFrame = 0;

// your network SSID (name)
char ssid[] = WIFISSID;

// your network password
char pass[] = PASSWORD;

// Go to forecast.io and register for an API KEY
String forecastApiKey = FORECASTAPIKEY;

// Coordinates of the place you want
// weather information for
double latitude = LATITUDE;
double longitude = LONGITUDE;

// flag changed in the ticker function every 10 minutes
bool readyForWeatherUpdate = true;

int mode = 0;

void setup() {
  delay(500);
  //ESP.wdtDisable();
  
  weather.setServer(weatherhost);
  weather.setPort(weatherhttpPort);

  // initialize display
  display.init();
  
  display.flipScreenVertically();

  setScrollingDisplay();

  Serial.begin(115200);
  delay(500);


  Serial.print("Flash Size: "); Serial.println(ESP.getFlashChipRealSize());
  

  Serial.println();
  Serial.println();
  // We start by connecting to a WiFi network
  Serial.print("Connecting to ");
  Serial.println(ssid);
  //Serial.print(" - ");
  //Serial.println(pass);
  
  searchForWifi();

  WiFi.begin(ssid, pass);

  int counter = 0;
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
    pinMode(4, INPUT);

    display.clear();
    display.drawXbm(34, 10, 60, 36, WiFi_Logo_bits);
    display.setColor(INVERSE);
    display.fillRect(10, 10, 108, 44);
    display.setColor(WHITE);
    drawSpinner(3, counter % 3);
    display.display();

    counter++;
  }
  Serial.println("");

  Serial.println("WiFi connected");
  Serial.print("IP address: ");
  Serial.println(WiFi.localIP());

  // update the weather information every 10 mintues only
  // forecast.io only allows 1000 calls per day
  //ticker.attach(60 * 10, setReadyForWeatherUpdate);
  ticker.attach(60 * 5, setReadyForWeatherUpdate);
  //ESP.wdtEnable();
}

void searchForWifi()
{
  int numNetworks = WiFi.scanNetworks();
  for (int thisNet = 0; thisNet < numNetworks; thisNet++) {
    Serial.print(thisNet);
    Serial.print(") ");
    Serial.print(WiFi.SSID(thisNet));
    Serial.print("\tChannel: ");
    Serial.print(WiFi.channel(thisNet));
    Serial.print("\tSignal: ");
    Serial.print(WiFi.RSSI(thisNet));
    Serial.print(" dBm");
    Serial.print("\tEncryption: ");
    printEncryptionType(WiFi.encryptionType(thisNet));
  }
}


  void printEncryptionType(int thisType) {
    // read the encryption type and print out the name:
    switch (thisType) {
    case ENC_TYPE_WEP:
      Serial.println("WEP");
      break;
    case ENC_TYPE_TKIP:
      Serial.println("WPA");
      break;
    case ENC_TYPE_CCMP:
      Serial.println("WPA2");
      break;
    case ENC_TYPE_NONE:
      Serial.println("None");
      break;
    case ENC_TYPE_AUTO:
      Serial.println("Auto");
      break;
    }
  return;
}

void loop() {

  if (readyForWeatherUpdate && display.getFrameState() == display.FRAME_STATE_FIX) {
    readyForWeatherUpdate = false;
    weather.updateWeatherData(forecastApiKey, latitude, longitude);
  }
  int a = digitalRead(4);
  
  
  //Serial.println(a);
  
  if (mode == 0)
  {
    display.clear();
    display.nextFrameTick();
    display.display();
  }
  else if (mode == 1)
  {
    display.clear();
    drawFrame4(0, 0);
    display.display();
  }
  
}

void setReadyForWeatherUpdate() {
  readyForWeatherUpdate = true;
}

void setScrollingDisplay()
{
  mode = 0;
  // set the drawing functions
  display.setFrameCallbacks(4, frameCallbacks);
  // how many ticks does a slide of frame take?
  display.setFrameTransitionTicks(10);

  display.clear();
  display.display();
}


void drawFrame1(int x, int y) {
  //Serial.println("Draw Frame 1"); 
  
  display.setFontScale2x2(false);
  display.drawString(65 + x, 8 + y, "Now");
  display.drawXbm(x + 7, y + 7, 50, 50, getIconFromString(weather.getCurrentIcon()));
  display.setFontScale2x2(true);
  display.drawString(64 + x, 20 + y, String(weather.getCurrentTemp()) + "F");

  //display.setFontScale2x2(false);
  //display.drawString(50 + x, 40 + y, String(weather.getSummaryToday()));

}

void drawFrame2(int x, int y) {
  //Serial.println("Draw Frame 2"); 
  
  display.setFontScale2x2(false);
  display.drawString(65 + x, 0 + y, "Today");
  display.drawXbm(x, y, 60, 60, xbmtemp);
  display.setFontScale2x2(true);
  display.drawString(64 + x, 14 + y, String(weather.getCurrentTemp()) + "F");
  display.setFontScale2x2(false);
  display.drawString(66 + x, 40 + y, String(weather.getMinTempToday()) + "F/" + String(weather.getMaxTempToday()) + "F");

}

void drawFrame3(int x, int y) {
 
  display.drawXbm(x + 7, y + 7, 50, 50, getIconFromString(weather.getIconTomorrow()));
  display.setFontScale2x2(false);
  display.drawString(65 + x, 7 + y, "Tomorrow");
  display.setFontScale2x2(true);
  display.drawString(64 + x, 20 + y, String(weather.getMaxTempTomorrow()) + "F");
}

//Upcoming summary
void drawFrame4(int x, int y) {
 
  display.setFontScale2x2(false);
  String text = weather.getHourlySummary();
  
  display.drawString(2 + x, 2 + y, text);
  Serial.println("---------------------");
  Serial.println(text);
  Serial.println("---------------------");
  Serial.println("012345678901234"); 
  displayLines(2 + x, 2 + y, 0, text);
  Serial.println("---------------------");

}
const int linespace = 9;

void displayLines(int x, int y, int line, String text)
{
  int firstTab = text.indexOf(9);
  if (firstTab == -1)
  {
    display.drawString(x, y + line * linespace, text + "       ");
    Serial.println(text);
    return;
  }

  displayLines(x, y, line, text.substring(0, firstTab));
  displayLines(x, y, line + 1, text.substring(firstTab + 1));
}


//Next Precipitation
void drawFrame5(int x, int y) {
 
  display.setFontScale2x2(true);
  display.drawString(2 + x, 5 + y, String(weather.getNextPrecipType()));
  display.setFontScale2x2(false);
  display.drawString(2 + x, 20+ y, "at");
  display.drawString(2 + x, 30 + y, String(weather.getNextPrecipTime()));
  
}

void drawSpinner(int count, int active) {
  for (int i = 0; i < count; i++) {
    const char *xbm;
    if (active == i) {
      xbm = active_bits;
    } else {
      xbm = inactive_bits;
    }
    display.drawXbm(64 - (12 * count / 2) + 12 * i, 56, 8, 8, xbm);
  }
}


const char* getIconFromString(String icon) {
  //"clear-day, clear-night, rain, snow, sleet, wind, fog, cloudy, partly-cloudy-day, or partly-cloudy-night"
  if (icon == "clear-day") {
    return clear_day_bits;
  }
  else if (icon == "clear-night") {
    return clear_night_bits;
  }
  else if (icon == "rain") {
    return rain_bits;
  }
  else if (icon == "snow") {
    return snow_bits;
  }
  else if (icon == "sleet") {
    return sleet_bits;
  }
  else if (icon == "wind") {
    return wind_bits;
  }
  else if (icon == "fog") {
    return fog_bits;
  }
  else if (icon == "cloudy") {
    return cloudy_bits;
  }
  else if (icon == "partly-cloudy-day") {
    return partly_cloudy_day_bits;
  }
  else if (icon == "partly-cloudy-night") {
    return partly_cloudy_night_bits;
  }
  return cloudy_bits;
}


