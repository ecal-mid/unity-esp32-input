/*---------------------------------------------------------------------------------------------

  Controller for Unity 
  Communication via OSC

  V1 AB/ECAL 2022

 
  --------------------------------------------------------------------------------------------- */

#include <WiFi.h>
#include <WiFiUdp.h>
#include <OSCMessage.h>
#include <OSCBundle.h>
#include <OSCData.h>
#include <ESPmDNS.h>
#include "your_secrets.h"

                                            
char ssid[] = WIFI_SSID;                    // edit WIFI_SSID + WIFI_PASS constants in the your_secret.h tab (if not present create it)
char pass[] = WIFI_PASS;                    

WiFiUDP Udp;                                // A UDP instance to let us send and receive packets over UDP
IPAddress outIp(192, 168, 45, 63);          // remote IP of your computer
const unsigned int outPort = 8888;          // remote port to receive OSC
const unsigned int localPort = 9999;        // local port to listen for OSC packets (actually not used for sending)

OSCErrorCode error;

// Button
const int buttonPin = 21;
int buttonState = 0;
int buttonLastState = -1;
// potentiometer
const int potentiometerPin = 34;
int potentiometerState = 0;
int potentiometerLastState = -1;
// LED
unsigned int ledState = LOW;

void setup() {
  Serial.begin(115200);
  //analogReadResolution(9); // 0-511
  //analogSetAttenuation(ADC_0db);

  // initialize the pushbutton pin as an input:
  pinMode(buttonPin, INPUT);
  // initialize the LED pin as an output:
  pinMode(LED_BUILTIN, OUTPUT);


  // Connect to WiFi network
  Serial.println();
  Serial.println();
  Serial.print("Connecting to ");
  Serial.println(ssid);
  WiFi.begin(ssid, pass);

  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }
  Serial.println("");

  Serial.println("WiFi connected");
  Serial.println("IP address: ");
  Serial.println(WiFi.localIP());

  Serial.println("Starting UDP");
  Udp.begin(localPort);
  Serial.print("Local port: ");
  Serial.println(localPort);
  Serial.print("Remote IP: ");
  Serial.println(outIp);

  // Start MDNS
  String unique_addr = WiFi.macAddress();
  
  unique_addr.replace(":", "");
  unique_addr = "encoder-"+ unique_addr.substring(0, 3);
  char mdns[30];
  unique_addr.toCharArray(mdns, 30);
  
  Serial.println(mdns);
  
  if (!MDNS.begin(mdns)) {
    Serial.println("Error starting mDNS");
    return;
  }
  
}

void led(OSCMessage &msg) {
  ledState = msg.getInt(0);
  digitalWrite(BUILTIN_LED, ledState);
  Serial.print("/arduino/led: ");
  Serial.println(ledState);
}

void sendValues() {
  //Serial.println(outIp);
  OSCMessage msg("/unity/state/");
  msg.add(buttonState);
  msg.add(potentiometerState);
  Udp.beginPacket(outIp, outPort);
  msg.send(Udp);
  Udp.endPacket();
  msg.empty();
  delay (10);

}

void updateIp(OSCMessage &msg) {
  char newIp[15];
  int str_length = msg.getString(0, newIp, 15);
  String ipString = String(newIp);
  uint8_t IP_part_1 = ipString.substring(0, 3).toInt();
  uint8_t IP_part_2 = ipString.substring(4, 7).toInt();
  uint8_t IP_part_3 = ipString.substring(8, 10).toInt();
  uint8_t IP_part_4 = ipString.substring(11, 13).toInt();

  outIp[IP_part_1, IP_part_2, IP_part_3, IP_part_4];

  Serial.print("New remote IP: ");
  Serial.println(outIp);
}

void loop() {
  // read the state of the pushbutton value:
  buttonState = digitalRead(buttonPin);
  // read the state of the potentiometer
  potentiometerState = analogRead(potentiometerPin);
  //potentiometerState = map(potentiometerState, 0, 511, 0, 100) ;

  if (potentiometerState != potentiometerLastState) {
    sendValues();
    potentiometerLastState = potentiometerState;
    /*
      OSCMessage msg("/unity/potentiometer/");
      msg.add(potentiometerState);
      Udp.beginPacket(outIp, outPort);
      msg.send(Udp);
      Udp.endPacket();
      msg.empty();

      Serial.println(potentiometerState);
      delay(10);*/
  }
  if (buttonState != buttonLastState) {
    sendValues();
    buttonLastState = buttonState;
    /*
      OSCMessage msg("/unity/button/");
      msg.add(buttonState);
      Udp.beginPacket(outIp, outPort);
      msg.send(Udp);
      Udp.endPacket();
      msg.empty();

      Serial.println(buttonState);
      delay(10);
    */
  }

  // IN
  OSCMessage msg;
  int size = Udp.parsePacket();

  if (size > 0) {
    while (size--) {
      msg.fill(Udp.read());
    }
    if (!msg.hasError()) {
      msg.dispatch("/arduino/led", led);
      msg.dispatch("/arduino/updateip", updateIp);
    } else {
      error = msg.getError();
      Serial.print("error: ");
      Serial.println(error);
    }
  }
}
