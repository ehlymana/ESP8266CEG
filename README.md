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

 After compiling the programming code, the Arduino IDE serial monitor needs to be opened, where the microcontroller will ask for WiFi credentials (IP address and password), as shown in the example below. After successfully establishing connection, the subsystem IP address will be shown on the serial monitor. Sensor values are printed out in real-time.
 
![](https://github.com/ehlymana/ESP8266CEG/blob/main/README/iot_setup.gif)
 
 The microcontroller can be used in two modes (day/night) and regimes (at home/away). By default, the Day mode and Home regime are active. Depending on the light, sound, and movement sensor values, the LED ring light shows the red, yellow, or green color, indicating whether there is possibility of an intruder and potential robbery. Two examples of the changes to LED colors (day and night) when movement is detected are shown below.
 
![](https://github.com/ehlymana/ESP8266CEG/blob/main/README/iot_day.gif)

![](https://github.com/ehlymana/ESP8266CEG/blob/main/README/iot_night.gif)

## The web-server subsystem

After the IoT embedded subsystem is configured and running, it can be connected to the web-server. The programming code of the web-server subsystem is available in the **ESPClient** folder of the repository.

To connect the ESP8266 microcontroller to the web-server, the user needs to input the previously obtained IP address of the microcontroller. The user interface is refreshed every couple of seconds, so the web-page does not need to be manually refreshed. When connection is successfully established, the current sensor and LED color values will be shown on the UI.

![](https://github.com/ehlymana/ESP8266CEG/blob/main/README/connection_to_iot.gif)

ESP8266 constantly sends information about current sensor values, so these values are changed in the web-page in real-time.

![](https://github.com/ehlymana/ESP8266CEG/blob/main/README/sensor_change.gif)

The system mode and regime can be changed at any moment. It sometimes takes a while for these changes to take effect, as they are sent via two different HTTP routes to the microcontroller.

![](https://github.com/ehlymana/ESP8266CEG/blob/main/README/change_mode_regime.gif)

## The mobile phone subsystem

The programming code of the mobile phone subsystem is available in the **MobileController** folder of the repository.

To connect a new mobile phone to the web-server, the mobile application needs to be started, and then the full IP address of the web-server must be specified, along with the desired name of the camera. The web-server and mobile phone must either be on the same IP address (for localhost deployment), or the web-server needs to be deployed to a publicly available web domain.

![](https://github.com/ehlymana/ESP8266CEG/blob/main/README/connecting_to_server.gif)

Upon automatic refresh of the UI, the new camera will be available in the list of all cameras and basic information about the connection will be available to the user.

![](https://github.com/ehlymana/ESP8266CEG/blob/main/README/connection_to_phone.gif)

The mobile phone automatically sends photos to the web-server every 10 seconds. The latest received image is always shown on the UI when it is refreshed.

![](https://github.com/ehlymana/ESP8266CEG/blob/main/README/receiving_photos.gif)

A face detection API is used for the detection of faces on images sent by the mobile phone. If a face is detected on the image, automatic alarm signal is sent to the phone, which plays the default alarm sound and calls the police (via 911).

![](https://github.com/ehlymana/ESP8266CEG/blob/main/README/face_detection.gif)

## Black-box testing

Automatic black-box testing is setup on the web-server. It is performed by clicking on the **Perform Testing** button. The black-box testing is based on fault injection into cause-effect graphs describing the system requirements. This results in the list of all tests which detect different faults, available in the following file:

```
ESPClient/ESPClient/detection_of_faults.txt
```

and a list of software quality and reliability metrics values after random fault injection into the system in 100 iterations, available in the following file:

```
ESPClient/ESPClient/BB_metrics.txt
```