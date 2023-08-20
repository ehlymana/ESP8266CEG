#include <ESP8266WiFi.h>
#include <ESP8266WebServer.h>
#include <Adafruit_NeoPixel.h>

#ifdef __AVR__
#include <avr/power.h>
#endif
#define Debouncing 100

#define ledRingPIN 14
#define LightSensorPin 4
#define IRSensorPin 13
#define VoiceSensorPin A0

Adafruit_NeoPixel LEDStrip = Adafruit_NeoPixel(24, ledRingPIN, NEO_GRB + NEO_KHZ800);
ESP8266WebServer server(80);

void handle_onConnect();
void handle_notFound();
void handle_sensors();
void handle_day();
void handle_night();
void handle_home();
void handle_away();

int light = 0; // no light == 1, light == 0
int movement = 0; // no movement == 0, movement == 1
double sound = 0; // between 0 and 256 (analog)
String color = "Green"; // Red, Yellow or Green

int system_mode = 1; // 1 - day, 2 - night
int system_regime = 1; // 1 - home, 2 - away

void setup()
{
    // enable console message printing
    Serial.begin(115200);
    Serial.println("BEGIN");

    // read WiFi network information from the user
    Serial.println("WiFi network name:");
    while (Serial.available() == 0);
    String ssid = Serial.readString();
    ssid.trim();
    
    Serial.println("WiFi network password:");
    while (Serial.available() == 0) {}
    String password = Serial.readString();
    password.trim();

    // connect ESP to wireless network
    WiFi.begin(ssid.c_str(), password.c_str());
    while (WiFi.status() != WL_CONNECTED)
      delay(500);
      
    // setup server routes
    server.on("/", handle_onConnect);
    server.on("/sensors", handle_sensors); 
    server.on("/day", handle_day);
    server.on("/night", handle_night);
    server.on("/home", handle_home);
    server.on("/away", handle_away);
    server.onNotFound(handle_notFound);
  
    // start server to communicate with the user
    server.begin();
    Serial.println("Web server started! IP address: ");
    Serial.println(WiFi.localIP());

    //initial LED strip setup
    LEDStrip.begin();
    LEDStrip.setBrightness(10); // between 0% and 100%

    int numberOfLeds = LEDStrip.numPixels() + 1;
    for(int i = 0; i < numberOfLeds; i++)
      LEDStrip.setPixelColor(i, LEDStrip.Color(0, 0, 0));

    LEDStrip.show();
}

void loop()
{
    // listen for web-requests from client
    server.handleClient();
    
    // sensors initialization
    light = 0; // no light == 1, light == 0
    movement = 0; // no movement == 0, movement == 1
    sound = 0; // between 0 and 256 (analog)

    // read data from sensors with debouncing
    for (int i = 0; i < Debouncing; i++)
    {
      light += digitalRead(LightSensorPin);
      movement += digitalRead(IRSensorPin);
      sound += analogRead(VoiceSensorPin) *  (3.0 / 1023.0) - 0.04;
    }

    // if light was active more than half of the debouncing times, then it is active
    if (light > Debouncing / 2)
      light = 1;
    else
      light = 0;

    // if movement was active more than half of the debouncing times, then it is active
    if (movement > Debouncing / 2)
      movement = 1;
    else
      movement = 0;

    // sound is analogue, so we can calculate the average value
    sound /= Debouncing;

    // sound needs to be normalized to between 0.0 and 1.0
    // maximum analogue value of 256 when the noise is the larges

    //sound /= 256;

    // show sensor information 
    Serial.println("Light: " + String(light));
    Serial.println("Movement: " + String(movement));
    Serial.println("Sound: " + String(sound));

    // activate the appropriate LED color depending on system mode and regime
    determineLED();

    Serial.println("LED: " + String(color));

    // wait 500 ms before next sensor reading
    delay(500);
}

// GET request for connecting to the controller
void handle_onConnect()
{
    Serial.println("Successfully received GET request on route /");
    server.send(200, "text/plain", "Connection: OK"); 
}

// GET request on non-existent route
void handle_notFound()
{
    server.send(404, "text/plain", "404: Not found");
}

// GET request for sending sensor values
void handle_sensors()
{
    Serial.println("Successfully received GET request on route /sensors");
    String str = "";
    str += String(light) + ",";
    str += String(movement) + ",";
    str += String(sound, 2) + ",";
    str += color;
    Serial.println(str);
    server.send(200, "text/plain", str);
}

// GET request for activating day mode
void handle_day()
{
    Serial.println("Successfully received GET request on route /day");
    system_mode = 1;
    server.send(200, "text/plain", "Change: OK");
}

// GET request for activating night mode
void handle_night()
{
    Serial.println("Successfully received GET request on route /night");
    system_mode = 2;
    server.send(200, "text/plain", "Change: OK");
}

// GET request for activating home regime
void handle_home()
{
    Serial.println("Successfully received GET request on route /home");
    system_regime = 1;
    server.send(200, "text/plain", "Change: OK");
}

// GET request for activating away regime
void handle_away()
{
    Serial.println("Successfully received GET request on route /away");
    system_regime = 2;
    server.send(200, "text/plain", "Change: OK");
}

// change LED color depending on system mode and regime
void determineLED()
{
    // call the appropriate function to determine LED color
    if (system_mode == 1 && system_regime == 1)
      color = day_home(light, movement, sound);
    else if (system_mode == 2 && system_regime == 1)
      color = night_home(light, movement, sound);
    else if (system_mode == 1 && system_regime == 2)
      color = day_away(light, movement, sound);
    else
      color = night_away(light, movement, sound);

    // initialize the corresponding color
    uint32_t LEDcolor = LEDStrip.Color(0, 0, 0);
    if (color == "Yellow")
      LEDcolor = LEDStrip.Color(255, 255, 0);
    else if (color == "GREEN")
      LEDcolor = LEDStrip.Color(0, 255, 0);

    // set all LED pixels to the required color
    int numberOfLeds = LEDStrip.numPixels() + 1;
    for(int i = 0; i < numberOfLeds; i++)
      LEDStrip.setPixelColor(i, LEDcolor);

    // show new color on the LED ring
    LEDStrip.show();
}

// determine LED color for system mode - day, system regime - home
String day_home(int light, int movement, double sound)
{
    String LED = "Red";
    
    if (light == 0)
      LED = "Green";
    else if (light == 1 && sound < 0.5)
      LED = "Yellow";
  
    return LED;
}

// determine LED color for system mode - night, system regime - home
String night_home(int light, int movement, double sound)
{
    String LED = "Red";
  
    if (light == 0 && sound < 0.25 && movement == 0)
      LED = "Green";
  
    return LED;
}

// determine LED color for system mode - day, system regime - away
String day_away(int light, int movement, double sound)
{
    String LED = "Red";
  
    if (light == 0 && sound < 0.25 && movement == 0)
      LED = "Yellow";
    else if (light == 1 && sound < 0.25 && movement == 0)
      LED = "Green";
  
    return LED;
}

// determine LED color for system mode - night, system regime - away
String night_away(int light, int movement, double sound)
{
    String LED = "Red";
  
    if (light == 1 && sound < 0.05 && movement == 0)
      LED = "Green";
  
    return LED;
}
