#include <ESP8266WiFi.h>
#include <PubSubClient.h>
#include <Adafruit_NeoPixel.h>
#include <ESP8266WiFiAP.h>
#include <ESP8266WiFiGeneric.h>
#include <ESP8266WiFiMulti.h>
#include <ESP8266WiFiScan.h>
#include <ESP8266WiFiSTA.h>
#include <ESP8266WiFiType.h>
#include <WiFiClient.h>
#include <WiFiClientSecure.h>
#include <WiFiServer.h>
 
// the data pin for the NeoPixels
int neoPixelPin = 14;

// How many NeoPixels we will be using, change accordingly
int numPixels = 7;

// Connect to the WiFi
String ssid = "LightShow";
String pass = "LightShow";
const char* mqtt_server = "192.168.4.1";


int red[] = {0,128,247,192,255,255,128,154,255,245};
int green[] = {128,0,11,192,155,128,255,226,242,90};
int blue[] = {0,255,75,192,255,128,0,170,125,254};




// Instatiate the NeoPixel from the ibrary
Adafruit_NeoPixel strip = Adafruit_NeoPixel(numPixels, neoPixelPin, NEO_GRB + NEO_KHZ800);

//Wifi client.
WiFiClient espClient;

//MQTT Client
PubSubClient client(espClient);

void callback(char* topic, byte* payload, unsigned int length) {
  Serial.print("Message arrived [");
  Serial.print(topic);
  Serial.print("] ");
  String data = "";
  for (int i=0;i<length;i++) {
    data += (char)payload[i];
  }
  Serial.println(data);

  String topicName(topic);
  if (topicName.equals("/lights"))
  {
    int level = data.toInt();
    Serial.print("Setting level to:");
    Serial.println(level);
    for( int i = 0; i < numPixels; i++ ) {
         strip.setPixelColor(i, red[level], green[level], blue[level] );
    }
    strip.show();
  }
  else if (topicName.equals("/dinner"))
  {
    Serial.println("Setting for dinner lights");
    for( int i = 0; i < numPixels; i++ ) {
         strip.setPixelColor(i, 255,255,255);
    }
    strip.show();
  }
  
}

void reconnect() {
 // Loop until we're reconnected
 while (!client.connected()) {
 Serial.print("Attempting MQTT connection...");
 // Attempt to connect
 //String clientId = WiFi.localIP().toString().c_str();
 if (client.connect(WiFi.localIP().toString().c_str())) {
  Serial.println("connected");
  // ... and subscribe to topic
  client.subscribe("/lights");
  client.subscribe("/dinner");
 } else {
  Serial.print("failed, rc=");
  Serial.print(client.state());
  Serial.println(" try again in 5 seconds");
  // Wait 5 seconds before retrying
  delay(5000);
  }
 }
}

void setup()
{
 Serial.begin(115200);
 Serial.println("starting");

 initWifi();
 strip.begin();  // initialize the strip
 strip.show();   // make sure it is visible
 strip.clear();  // Initialize all pixels to 'off'

 for( int i = 0; i < numPixels; i++ ) {
         strip.setPixelColor(i, 255,255,255);
  }
  strip.show();

 Serial.println("Setting MQTT server name");
 client.setServer(mqtt_server, 1883);

 Serial.println("Setting callback method");
 client.setCallback(callback);
}
 
void loop()
{
  initWifi();
 if (!client.connected()) {
  reconnect();
 }
 client.loop();
}

void initWifi() {
  //If the wifi is not connected, set it up, and reinitialize the time and iothub connection.
    if (WiFi.status() != WL_CONNECTED) 
    {
        WiFi.stopSmartConfig();
        WiFi.enableAP(false);

        // Connect to WPA/WPA2 network. Change this line if using open or WEP network:
        WiFi.begin(ssid.c_str(), pass.c_str());
    
        Serial.print("Waiting for Wifi connection.");
        while (WiFi.status() != WL_CONNECTED) {
            Serial.print(".");
            delay(500);
        }
    
        Serial.println("Connected to wifi");
        IPAddress ip = WiFi.localIP();
        Serial.println(ip);
        
    }
}
