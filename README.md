# ESP8266CEG

This repository contains the programming code for the case study of black-box testing an embedded IoT alarm software system by using ESP-8266 microcontroller (Arduino IDE), web-server (ASP.NET) and mobile phones (Xamarin).

If you use this dataset for your research, please cite the following work:

```
E. Krupalija, E. Cogo, A. Karabegović, S. Omanović, and I. Bešić, "Usage of cause-effect graphs for reliability analysis of an embedded IoT alarm software system", submitted to Quality and Reliability Engineering International, 2023.
```

## The embedded IoT subsystem

The programming code of the embedded IoT subsystem is available in the **ESP8266_LED_Ring** folder of the repository.

The ESP8266 microcontroller and its components (light sensor, sound sensor, PIR movement sensor and NeoPixel LED ring) need to be assembled in the following setup:

![](https://github.com/ehlymana/ESP8266CEG/blob/main/README/schema_esp8266.png)

 After compiling the programming code, the Arduino IDE serial monitor needs to be opened, where the microcontroller will ask for WiFi credentials (IP address and password), as shown in the following example:
 
 // image - IP address serial monitor
 
 The microcontroller can be used in two modes (day/night) and regimes (at home/away). By default, the Day mode and Home regime are active. Depending on the light, sound, and movement sensor values, the LED ring light shows the red, yellow, or green color, indicating whether there is possibility of an intruder and potential robbery.
 
 // image - example of ESP8266 functionalities