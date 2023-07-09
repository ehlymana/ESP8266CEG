#include <ESP8266WiFi.h>
#include <ESP8266WebServer.h>
#include <Adafruit_NeoPixel.h>

#ifdef __AVR__
#include <avr/power.h>
#endif
#define Debouncing 100

#define ledRingPIN 14
#define LightSensorPin 16
#define IRSensorPin 13
#define VoiceSensorPin A0

Adafruit_NeoPixel LEDStrip = Adafruit_NeoPixel(24, ledRingPIN, NEO_GRB + NEO_KHZ800);

const char *ssid = "****";
const char *password = "****";
ESP8266WebServer server(80);

void handle_onConnect();
void handle_sensors();
void handle_notFound();

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

    // connect ESP to wireless network
    WiFi.begin(ssid, password);
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
      sound += analogRead(VoiceSensorPin);
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

    // sound is digital, so we can calculate the average value
    sound /= Debouncing;

    // wait 500 ms before next sensor reading
    delay(500);
}

void handle_onConnect()
{
  Serial.println("Successfully received GET request on route /");
  server.send(200, "text/plain", "Connection: OK"); 
}

void handle_notFound()
{
  server.send(404, "text/plain", "404: Not found");
}

void handle_sensors()
{
  Serial.println("Successfully received GET request on route /sensors");
  String str = "";
  str += String(light) + ",";
  str += String(movement) + ",";
  str += String(sound, 0) + ",";
  str += color + ",";
  if (system_mode == 1)
    str += "Day,";
  else
    str += "Night,";
  if (system_regime == 1)
    str += "AtHome";
  else
    str += "Away";
  Serial.println(str);
  server.send(200, "text/plain", str);
}

void handle_day()
{
  Serial.println("Successfully received GET request on route /day");
  system_mode = 1;
  server.send(200, "text/plain", "Change: OK");
}

void handle_night()
{
  Serial.println("Successfully received GET request on route /night");
  system_mode = 2;
  server.send(200, "text/plain", "Change: OK");
}

void handle_home()
{
  Serial.println("Successfully received GET request on route /home");
  system_regime = 1;
  server.send(200, "text/plain", "Change: OK");
}

void handle_away()
{
  Serial.println("Successfully received GET request on route /away");
  system_regime = 2;
  server.send(200, "text/plain", "Change: OK");
}
