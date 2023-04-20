/*---------------------------------------------------------------------------------------------

  Controller for Unity
  Communication via OSC

  V4 AB/ECAL 2022


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

int firmware = 37;

char ssid[] = WIFI_SSID;  // edit WIFI_SSID + WIFI_PASS constants in the your_secret.h tab (if not present create it)
char pass[] = WIFI_PASS;

WiFiUDP Udp;                          // A UDP instance to let us send and receive packets over UDP
IPAddress outIp(192, 168, 0, 0);      // remote IP of your computer
int outPort = 8888;                   // remote port to send OSC
const unsigned int localPort = 9999;  // local port to listen for OSC packets
bool authorisedIP = false;
IPAddress lastOutIp(192, 168, 0, 0);
char ipAsChar[15];

OSCErrorCode error;
Preferences preferences;  // to save persistent data (board name)

String boardName;  // to be used as identifier and MDNS
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
int32_t encoderPrevValue = -9999;
int32_t encoderValue;
// Timing
unsigned long lastUserInteractionMillis = 0;
int sleepDelayInMillis = 1 * 60 * 1000;  // time in milliseconds to wait before standby
unsigned long lastInfoSentMillis = 0;
unsigned long lastKeepAliveReceived = 0;
bool clientConnected = false;

/* ------- Adafruit_DRV2605 */
Adafruit_DRV2605 drv;
int8_t motorCurrentMode;


// define capabilities here
uint8_t motorCount = 2;
uint8_t encoderCount = 0;
uint8_t buttonCount = 0;

/* Server for IP table update */
HTTPClient httpclient;


void setup() {
  Serial.begin(115200);
  delay(1000);
  Serial.println("");
  preferences.begin("device", false);
  //setBoardName(1);
  if (motorCount > 0) {

    // DRV2605
    // TODO init all motors
    Serial.println("Starting DRV2605");
    if (drv.begin()) {
      Serial.println("has DRV2065");
      drv.selectLibrary(1);
      drv.useERM();  // ERM or LRA
      stopMotors();  // Stop the motor in case it hang running

    } else {
      motorCount = 0;
      Serial.println("no DRV2065");
    }
  }
  // BUTTON
  if (buttonCount > 0) {
    pinMode(buttonPin, INPUT_PULLUP);
  }

  if (encoderCount > 0) {
    // ENCODER
    ESP32Encoder::useInternalWeakPullResistors = UP;
    encoder.attachHalfQuad(encoder_pin_1, encoder_pin_2);
    encoder.setFilter(1023);
    encoder.setCount(0);  // reset the counter
  }
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
    goToSleep(0);
  }
  /* --------- if no diconnect message received but no keepAlive message comming then disconnect */
  if (millis() - lastKeepAliveReceived > 15 * 1000 && clientConnected == true) {
    autoDisconnect();
    Serial.println("auto disconnect");
  }

  /* --------- Send battery level and other info every minutes and check wifi connection */
  if (millis() - lastInfoSentMillis > 60 * 1000) {
    // send info
    outSendInfo();
    lastInfoSentMillis = millis();
    // check wifi
    if (WiFi.status() != WL_CONNECTED) {  // restart the esp if no WIFI anymore
      restartESP();
    }
  }

  /* --------- SEND OSC MSGS */

  if (encoderCount > 0) {
    // ENCODER
    // read the state of the Encoder
    encoderValue = encoder.getCount();
    if (encoderValue != encoderPrevValue) {
      outSendEncoderValues();
      //Serial.println("Encoder count = " + String((int32_t)encoderValue));
      encoderPrevValue = encoderValue;
      keepAlive(0);  // reset countdown for deepsleep
    }
  }

  if (buttonCount > 0) {
    // BUTTON
    // read the state of the pushbutton value:
    buttonState = digitalRead(buttonPin) ? 0 : 1;

    if (buttonState != buttonLastState) {
      outSendButtonValues();
      buttonLastState = buttonState;
      keepAlive(0);  // reset countdown for deepsleep
    }
    // Check for long press (used to restart board)
    if (buttonState == 1) {
      if (millis() - lastButtonUpMillis > 5000) {  // if long press 5s
        restartESP();
      }
    } else {
      lastButtonUpMillis = millis();
    }
  }

  /* --------- CHECK INCOMMING OSC MSGS */
  OSCMessage msg;
  int size = Udp.parsePacket();
  if (size > 0) {
    // check if the message come from the same Out IP to avoid multiple connections
    if (Udp.remoteIP().toString() != outIp.toString()) {
      //Serial.print("NOT allowed senders IP: ");
      //Serial.println(Udp.remoteIP());
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
        if (motorCount > 0) {
          msg.dispatch("/arduino/motor/rt", inMotorRealtime);
          msg.dispatch("/arduino/motor/cmd", inMotorCommand);
          msg.dispatch("/arduino/motor/stopall", inMotorStopAll);
        }
        msg.dispatch("/arduino/restart", inRestartESP);
        msg.dispatch("/arduino/keepalive", inKeepAlive);
        msg.dispatch("/arduino/disconnect", inDisconnect);
        msg.dispatch("/arduino/sleep", inSleep);
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
void outSendButtonValues() {  // in button, encoder
  OSCMessage msg("/unity/state/button/");
  msg.add(ipAsChar);
  msg.add(buttonState);
  Udp.beginPacket(outIp, outPort);
  msg.send(Udp);
  Udp.endPacket();
  msg.empty();
  Serial.printf("send button state: %i\n", buttonState);
}
void outSendEncoderValues() {  // in button, encoder
  OSCMessage msg("/unity/state/encoder/");
  msg.add(ipAsChar);
  msg.add(encoderValue);
  Udp.beginPacket(outIp, outPort);
  msg.send(Udp);
  Udp.endPacket();
  msg.empty();
  Serial.printf("send encoder state: %i\n", encoderValue);
}

void outSendInfo() {
  OSCMessage msg("/unity/info/");
  char brd_name[12];
  getBoardName().toCharArray(brd_name, 12);
  msg.add(ipAsChar);
  msg.add(brd_name);
  msg.add(firmware);
  msg.add(getBatteryLevel());
  msg.add(motorCount);
  msg.add(encoderCount);
  msg.add(buttonCount);
  Udp.beginPacket(outIp, outPort);
  msg.send(Udp);
  Udp.endPacket();
  msg.empty();
}

void outSendDisconnect() {
  OSCMessage msg("/unity/disconnect/");
  msg.add(ipAsChar);
  Udp.beginPacket(outIp, outPort);
  msg.send(Udp);
  Udp.endPacket();
  msg.empty();
  delay(10);
  Serial.println("disconnect sent");
}

void outSendAlive(int msg_id) {
  OSCMessage msg("/unity/alive/");
  msg.add(ipAsChar);
  msg.add(msg_id);
  Udp.beginPacket(outIp, outPort);
  msg.send(Udp);
  Udp.endPacket();
  msg.empty();
}


/* --------- INCOMMING OSC COMMANDS FUNCTIONS ------------ */

void inMotorRealtime(OSCMessage &msg) {  // int value 0-100

  int motorId = msg.getInt(0);
  if (motorId >= motorCount)
    return;
  int motorValue = msg.getInt(1);
  Serial.printf("/arduino/motor/rt: %i %i\n", motorId, motorValue);
  double motorInput = (float)motorValue / 100;
  playHapticRT(motorId, motorInput);
}

void inMotorCommand(OSCMessage &msg) {  // int value 0-117
  int motorId = msg.getInt(0);
  if (motorId >= motorCount)
    return;
  int motorCommand = msg.getInt(1);
  Serial.printf("/arduino/motor/cmd: %i %i\n", motorId, motorCommand);
  playHaptic(motorId, motorCommand);
}

void inMotorStopAll(OSCMessage &msg) {
  Serial.printf("/arduino/motor/stopall\n");
  stopMotors();
}

void inConnect(OSCMessage &msg) {  // string value "ip:port"
  lastKeepAliveReceived = millis();
  clientConnected = true;

  stopMotors();
  resetEncoders();

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
  // save iP as Char Array for sending
  WiFi.localIP().toString().toCharArray(ipAsChar, 15);

  Serial.print("New remote IP: ");
  Serial.println(outIp);
  Serial.print("New remote Port: ");
  Serial.println(outPort);
  // answer
  outSendInfo();
}

void inDisconnect(OSCMessage &msg) {
  autoDisconnect();
}

void inKeepAlive(OSCMessage &msg) {
  int msg_id = msg.getInt(0);
  keepAlive(0);
  lastKeepAliveReceived = millis();
  outSendAlive(msg_id);  // answer
}

void inRestartESP(OSCMessage &msg) {  // no value needed
  restartESP();
}

void inSleep(OSCMessage &msg) {  // no value needed
  goToSleep(1);
}

void inSetName(OSCMessage &msg) {  // int (will be used a unique identifier number)
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
      goToSleep(1);  // go to sleep in not connected after 15 sec
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
  return board_name;
}

float getBatteryLevel() {
  // Reference voltage on ESP32 is 1.1V
  // https://docs.espressif.com/projects/esp-idf/en/latest/esp32/api-reference/peripherals/adc.html#adc-calibration
  // See also: https://bit.ly/2zFzfMT
  int rawValue = analogRead(A13);
  float voltageLevel = (rawValue / 4096.0) * 2 * 1.1 * 3.3;  // calculate voltage level
  return (voltageLevel);
}

void autoDisconnect() {
  clientConnected = false;
  outIp.fromString("192.168.0.0");
  stopMotors();
  Serial.println("distant Host disconnected");
}

void setMode(uint8_t mode) {
  if (mode == motorCurrentMode) {
    return;
  }
  drv.setMode(mode);
  motorCurrentMode = mode;
}
void playHaptic(uint8_t motorId, uint8_t effect) {

  // TODO check motorId
  setMode(DRV2605_MODE_INTTRIG);

  drv.setWaveform(0, effect);  // play effect
  drv.setWaveform(1, 0);       // end waveform
  drv.go();
}
void playHapticRT(uint8_t motorId, double val) {  // 0 - 1

  // TODO check motorId
  setMode(DRV2605_MODE_REALTIME);

  int intV = min(1.0, max(0.0, val)) * 0x7F;
  //Serial.println(intV);
  drv.setRealtimeValue(intV);
}

void stopMotors() {

  for (int i = 0; i < motorCount; i++) {
    playHapticRT(0, 0.0);
  }
}

void resetEncoders() {

  if (encoderCount == 0)
    return;

  encoder.setCount(0);  // reset the counter
}

void keepAlive(int sleepdelay) {  // send value in minutes, if 0 no update of active value
  if (sleepdelay > 0) {
    sleepDelayInMillis = sleepdelay * 60 * 1000;
    Serial.println("");
    Serial.println("New sleep delay: " + String(sleepdelay));
  }
  lastUserInteractionMillis = millis();  // reset countdown for deepsleep
}

void goToSleep(int force) {
  if (getBatteryLevel() > 4.2 && force != 1) {  // fully charged or plugged to a charger so no need to sleep
    keepAlive(0);
    return;
  }
  Serial.println("************* going to sleep, bye! *************");
  outSendDisconnect();
  stopMotors();  // Stop the motor in case it hang running
  pinMode(encoder_pin_1, OUTPUT);
  digitalWrite(encoder_pin_1, LOW);  // turn of the led
  esp_sleep_enable_ext0_wakeup(GPIO_NUM_27, 0);
  esp_deep_sleep_start();
}

void updateIpTable() {
  httpclient.begin("https://ecal-mid.ch/magicleap/update.php?name=" + getBoardName() + "&ip=" + WiFi.localIP().toString() + "&wifi=" + WIFI_SSID + "&battery=" + getBatteryLevel() + "&motor=" + (motorCount > 0) + "&firmware=" + firmware);
  int httpResponseCode = httpclient.GET();
  if (httpResponseCode > 0) {
    //Serial.print("HTTP Response code: ");
    //Serial.println(httpResponseCode);
    String payload = httpclient.getString();
    Serial.println(payload);
  } else {
    Serial.print("Error code: ");
    Serial.println(httpResponseCode);
  }
  // Free resources
  httpclient.end();
}

void restartESP() {
  stopMotors();  // Stop the motor in case it hang running
  outSendDisconnect();
  pinMode(encoder_pin_1, OUTPUT);
  digitalWrite(encoder_pin_1, LOW);  // turn of the led
  Serial.print("Restarting now");
  delay(10);
  ESP.restart();
}
