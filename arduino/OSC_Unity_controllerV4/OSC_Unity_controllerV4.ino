/*---------------------------------------------------------------------------------------------

  Controller for Unity
  Communication via OSC

  V3 AB/ECAL 2022


  --------------------------------------------------------------------------------------------- */

#include <WiFi.h>
#include <WiFiUdp.h>
#include <HTTPClient.h>
#include <OSCMessage.h>
#include <OSCBundle.h>
#include <OSCData.h>
#include <ESPmDNS.h>
#include "your_secrets.h"
#include <Wire.h>
#include "Adafruit_DRV2605.h"
#include <ESP32Encoder.h>
#include <Preferences.h>

int firmware = 33;

char ssid[] = WIFI_SSID;                    // edit WIFI_SSID + WIFI_PASS constants in the your_secret.h tab (if not present create it)
char pass[] = WIFI_PASS;

WiFiUDP Udp;                                // A UDP instance to let us send and receive packets over UDP
IPAddress outIp(192, 168, 0, 0);            // remote IP of your computer
int outPort = 8888;                         // remote port to send OSC
const unsigned int localPort = 9999;        // local port to listen for OSC packets
bool authorisedIP = false;
IPAddress lastOutIp(192, 168, 0, 0);

OSCErrorCode error;
Preferences preferences; // to save persistent data (board name)

String boardName; // to be used as identifier and MDNS
float batteryVotage = 0;

/* ------- Define pins ann vars for button + encoder */
// Button
const int buttonPin = 27;
int buttonState = 0;
int buttonLastState = -1;
unsigned long lastButtonUpMillis = -1;

// Encoder
ESP32Encoder encoder;
const int encoder_pin_1 = 13;
const int encoder_pin_2 = 15;
int32_t encoderPrevCount = -9999;
int32_t encoderCount;
// Timing
unsigned long lastUserInteractionMillis = 0;
int sleepDelayInMillis = 1  * 60 * 1000; // time in milliseconds to wait before standby
unsigned long lastInfoSentMillis = 0;

/* ------- Adafruit_DRV2605 */
Adafruit_DRV2605 drv;
int8_t motorCurrentMode;
bool hasMotor = false;

/* Server for IP table update */
HTTPClient httpclient;


void setup() {
  Serial.begin(115200);
  delay(1000);
  Serial.println("");
  preferences.begin("device", false);
  //setBoardName(1);
  // DRV2605
  Serial.println("Starting DRV2605");
  if (drv.begin()) {
    hasMotor = true;
    Serial.println("has DRV2065");
  } else {
    hasMotor = false;
    Serial.println("no DRV2065");
  }
  if ( hasMotor == true) {
    drv.selectLibrary(1);
    drv.useERM(); // ERM or LRA
    playHapticRT(0.0); // Stop the motor in case it hang running
  }
  // BUTTON
  pinMode(buttonPin, INPUT_PULLUP);
  // ENCODER
  ESP32Encoder::useInternalWeakPullResistors = UP;
  encoder.attachHalfQuad(encoder_pin_1, encoder_pin_2);
  encoder.setFilter(1023);
  encoder.setCount(0); // reset the counter
  // Start Wifi and UDP
  startWifiAndUdp();
  // Publish to IP table (online)
  updateIpTable();
  // sleep after 10 minutes if no user activity or osc call on arduino/keepalive
  keepAlive(10);
  // send info
  outSendInfo();
}


void loop() {

  /* --------- Timing and sleep */
  if (millis() - lastUserInteractionMillis > sleepDelayInMillis) {
    // start deepSleep
    goToSleep();
  }

  /* --------- Send battery level and other info every minutes and check wifi connection */
  if (millis() - lastInfoSentMillis > 60 * 1000) {
    // send info
    outSendInfo();
    lastInfoSentMillis = millis();
    // check wifi
    if (WiFi.status() != WL_CONNECTED) { // restart the esp if no WIFI anymore
      restartESP();
    }
  }

  /* --------- SEND OSC MSGS */
  // ENCODER
  // read the state of the Encoder
  encoderCount = encoder.getCount();

  if (encoderCount != encoderPrevCount) {
    outSendValues();
    //Serial.println("Encoder count = " + String((int32_t)encoderCount));
    encoderPrevCount = encoderCount;
    keepAlive(0); // reset countdown for deepsleep
  }

  // BUTTON
  // read the state of the pushbutton value:
  buttonState = digitalRead(buttonPin);

  if (buttonState != buttonLastState) {
    outSendValues();
    buttonLastState = buttonState;
    keepAlive(0); // reset countdown for deepsleep
  }
  // Check for long press (used to restart board)
  if (buttonState == 0) {
    if (millis() - lastButtonUpMillis > 5000) { // if long press 5s
      restartESP();
    }
  } else {
    lastButtonUpMillis = millis();
  }

  /* --------- CHECK INCOMMING OSC MSGS */
  OSCMessage msg;
  int size = Udp.parsePacket();
  if (size > 0) {
    // check if the message come from the same Out IP to avoid multiple connections
    if (Udp.remoteIP().toString() != outIp.toString()) {
      Serial.print("NOT allowed senders IP: ");
      Serial.println(Udp.remoteIP());
      authorisedIP = false;
    } else {
      Serial.println("allowed senders IP ");
      authorisedIP = true;
    }
    while (size--) {
      msg.fill(Udp.read());
    }
    if (!msg.hasError()) {
      if (authorisedIP == true) {
        // access only if authorised IP
        msg.dispatch("/arduino/motor/rt", inMotorRealtime);
        msg.dispatch("/arduino/motor/cmd", inMotorCommand);
        msg.dispatch("/arduino/restart", inRestartESP);
        msg.dispatch("/arduino/keepalive", inKeepAlive);
        msg.dispatch("/arduino/disconnect", inDisconnect);
      }
      // pass trough access to allow update of outgoing Port and IP
      msg.dispatch("/arduino/setname", inSetName);
      msg.dispatch("/arduino/connect", inConnect);
    } else {
      error = msg.getError();
      Serial.print("error: ");
      Serial.println(error);
    }
  }
}

/* --------- FUNCTIONS ------------ */


/* --------- OUTGOING OSC COMMANDS FUNCTIONS ------------ */
void outSendValues() { // in button, encoder
  OSCMessage msg("/unity/state/");
  char brd_name[12];
  char ip_char[15];
  getBoardName().toCharArray(brd_name, 12);
  WiFi.localIP().toString().toCharArray(ip_char, 15);
  msg.add(ip_char);
  msg.add(buttonState);
  msg.add(encoderCount);
  Udp.beginPacket(outIp, outPort);
  msg.send(Udp);
  Udp.endPacket();
  msg.empty();
  delay (10);
}

void outSendInfo() {
  OSCMessage msg("/unity/info/");
  char brd_name[12];
  char ip_char[15];
  getBoardName().toCharArray(brd_name, 12);
  WiFi.localIP().toString().toCharArray(ip_char, 15);
  msg.add(ip_char);
  msg.add(brd_name);
  msg.add(firmware);
  msg.add(getBatteryLevel());
  msg.add(int(hasMotor));
  Udp.beginPacket(outIp, outPort);
  msg.send(Udp);
  Udp.endPacket();
  msg.empty();
  delay (10);
}

void outSendDisconnect() {
  OSCMessage msg("/unity/disconnect/");
  char ip_char[15];
  WiFi.localIP().toString().toCharArray(ip_char, 15);
  msg.add(ip_char);
  Udp.beginPacket(outIp, outPort);
  msg.send(Udp);
  Udp.endPacket();
  msg.empty();
  delay (10);
  Serial.println("disconnect sent");
}

/* --------- INCOMMING OSC COMMANDS FUNCTIONS ------------ */

void inMotorRealtime(OSCMessage &msg) { // int value 0-100
  int motorValue = msg.getInt(0);
  Serial.print("/arduino/motor/rt: ");
  Serial.println(motorValue);
  double motorInput = (float) motorValue / 100;
  playHapticRT(motorInput);
}

void inMotorCommand(OSCMessage &msg) { // int value 0-117
  int motorCommand = msg.getInt(0);
  Serial.print("/arduino/motor/cmd: ");
  Serial.println(motorCommand);
  playHaptic(motorCommand);
}

void inConnect(OSCMessage &msg) { // string value "ip:port"
  char newIpAndPort[20];
  int str_length = msg.getString(0, newIpAndPort, 20);
  String ipAndportString = String(newIpAndPort);
  // split IP and Port
  int colonPos = ipAndportString.indexOf(":");
  String ipString = ipAndportString.substring(0, colonPos);
  String PortString = ipAndportString.substring(colonPos + 1, ipAndportString.length());

  if (outIp.toString() != ipString) {
    // send disconnect to last IP
    outSendDisconnect();
  }

  outIp.fromString(ipString);
  outPort = PortString.toInt();

  Serial.print("New remote IP: ");
  Serial.println(outIp);
  Serial.print("New remote Port: ");
  Serial.println(outPort);
  // answer
  outSendInfo();
}

void inDisconnect(OSCMessage &msg) { // int minutes of delay before sleep, send 0 if no change
  outIp.fromString("192.168.0.0");
  Serial.println("distant Host disconnected");
}

void inKeepAlive(OSCMessage &msg) { // int minutes of delay before sleep, send 0 if no change
  int sleepDelay = msg.getInt(0);
  keepAlive(sleepDelay);
}

void inRestartESP(OSCMessage &msg) { // no value needed
  restartESP();
}

void inSetName(OSCMessage &msg) { // int (will be used a unique identifier number)
  setBoardName(msg.getInt(0));
}


/* --------- OTHER FUNCTIONS ------------ */

void startWifiAndUdp() {
  // Connect to WiFi network
  Serial.println();
  Serial.println();
  Serial.print("Connecting to ");
  Serial.println(ssid);
  WiFi.begin(ssid, pass);
  int tryCount = 0;
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
    if (tryCount > 30) {
      goToSleep(); // go to sleep in not connected after 15 sec
    }
    tryCount++;
  }
  Serial.println("");

  Serial.println("WiFi connected");
  Serial.println("IP address: ");
  Serial.println(WiFi.localIP());
  // Start UDP
  Serial.println("Starting UDP");
  if (!Udp.begin(localPort)) {
    Serial.println("Error starting UDP");
    return;
  }
  Serial.print("Local port: ");
  Serial.println(localPort);
  Serial.print("Remote IP: ");
  Serial.println(outIp);
  // Start MDNS
  boardName = getBoardName();
  char mdns[30];
  boardName.toCharArray(mdns, 30);
  Serial.println(mdns);

  WiFi.setHostname(mdns);
  MDNS.begin(mdns);
  MDNS.addService("_osc", "_udp", localPort);
  MDNS.addServiceTxt("osc", "udp", "board", "ESP32Board");
}

void setBoardName(int nbr) {
  preferences.putInt("name", nbr);
}

String getBoardName() {
  int nbr = preferences.getInt("name", 0);
  String board_name = "controller" + String(nbr);
  //String board_name = WiFi.macAddress();
  //board_name.replace(":", "");
  //board_name = "magic" + board_name.substring(0, 7);
  return board_name;
}

float getBatteryLevel() {
  // Reference voltage on ESP32 is 1.1V
  // https://docs.espressif.com/projects/esp-idf/en/latest/esp32/api-reference/peripherals/adc.html#adc-calibration
  // See also: https://bit.ly/2zFzfMT
  int rawValue = analogRead(A13);
  float voltageLevel = (rawValue / 4096.0) * 2 * 1.1 * 3.3; // calculate voltage level
  return (voltageLevel);
}


void setMode(uint8_t mode) {
  if (mode == motorCurrentMode) {
    return;
  }
  drv.setMode(mode);
  motorCurrentMode = mode;
}
void playHaptic(uint8_t effect) {
  setMode(DRV2605_MODE_INTTRIG);

  drv.setWaveform(0, effect);  // play effect
  drv.setWaveform(1, 0);       // end waveform
  drv.go();
  //delay(200);
}

void playHapticRT(double val) { // 0 - 1
  setMode(DRV2605_MODE_REALTIME);

  int intV = min(1.0, max(0.0, val)) * 0x7F;
  //Serial.println(intV);
  drv.setRealtimeValue(intV);
}

void keepAlive(int sleepdelay) { // send value in minutes, if 0 no update of active value
  if (sleepdelay > 0) {
    sleepDelayInMillis = sleepdelay * 60 * 1000;
    Serial.println("");
    Serial.println("New sleep delay: " + String(sleepdelay));
  }
  lastUserInteractionMillis = millis(); // reset countdown for deepsleep
}

void goToSleep() {
  if (getBatteryLevel() > 4.2) { // fully charged or plugged to a charger so no need to sleep
    keepAlive(0);
    return;
  }
  Serial.println("************* going to sleep, bye! *************");
  esp_sleep_enable_ext0_wakeup(GPIO_NUM_27, 0);
  esp_deep_sleep_start();
}

void updateIpTable() {
  httpclient.begin("https://ecal-mid.ch/magicleap/update.php?name=" + getBoardName() + "&ip=" + WiFi.localIP().toString() + "&wifi=" + WIFI_SSID + "&battery=" + getBatteryLevel() + "&motor=" + hasMotor + "&firmware=" + firmware);
  int httpResponseCode = httpclient.GET();
  if (httpResponseCode > 0) {
    //Serial.print("HTTP Response code: ");
    //Serial.println(httpResponseCode);
    String payload = httpclient.getString();
    Serial.println(payload);
  }
  else {
    Serial.print("Error code: ");
    Serial.println(httpResponseCode);
  }
  // Free resources
  httpclient.end();
}

void restartESP() {
  Serial.print("Restarting now");
  ESP.restart();
}
