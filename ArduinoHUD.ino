/*
 * ArduinoHUD.ino
 * by rappo & ZyDevs
 * GTA5-Mods.com
 */

#include <Wire.h>

/*
 * Customize the LiquidCrystal instantiator to match your setup. 
 * More info: https://www.arduino.cc/en/Tutorial/HelloWorld
 * I2C info: http://hmario.home.xs4all.nl/arduino/LiquidCrystal_I2C
 */
 
/* Using I2C library */
// #include <LiquidCrystal_I2C.h>
// LiquidCrystal_I2C lcd(0x3F, 2, 1, 0, 4, 5, 6, 7, 3, POSITIVE); 

/* Using default library */
#include <LiquidCrystal.h>
LiquidCrystal lcd(12, 11, 5, 4, 3, 2);

/*
 * Customize your preferences
 */
const int BAUD_RATE       = 9600;
const int LCD_NUM_COLUMNS = 16;
const int LCD_NUM_ROWS    = 2;

const int LED1_PIN        = 13;
const int LED2_PIN        = 10;
const int LED3_PIN        = 9;
const int LED4_PIN        = 8;
const int LED5_PIN        = 7;

const int BUTTON_PIN      = 6;

/*
 * No edits necessary beyond this point
 */
void setup() {
  Serial.begin(BAUD_RATE);
  lcd.begin(LCD_NUM_COLUMNS, LCD_NUM_ROWS);
  lcd.clear();

  pinMode(LED1_PIN, OUTPUT);
  digitalWrite(LED1_PIN, LOW);
  pinMode(LED2_PIN, OUTPUT);
  digitalWrite(LED2_PIN, LOW);
  pinMode(LED3_PIN, OUTPUT);
  digitalWrite(LED3_PIN, LOW);
  pinMode(LED4_PIN, OUTPUT);
  digitalWrite(LED4_PIN, LOW);
  pinMode(LED5_PIN, OUTPUT);
  digitalWrite(LED5_PIN, LOW);

  pinMode(BUTTON_PIN, INPUT);
}

const int COMMAND_START = 10;
const int COMMAND_CLEAR = 1;
const int COMMAND_SET_CURSOR = 2;
const int COMMAND_SET_LED_COUNT = 3;
const int COMMAND_TOGGLE = 4;

int buttonState = HIGH;
int buttonReading;
int previousButtonReading;
long lastButtonToggleTime = 0;
const int BUTTON_DEBOUNCE = 200;

int incomingByte = 0;

void loop() {
  buttonReading = digitalRead(BUTTON_PIN);
  if (buttonReading == HIGH && previousButtonReading == LOW && millis() - lastButtonToggleTime > BUTTON_DEBOUNCE) {
    if (buttonState == HIGH) {
      buttonState = LOW;
    } else {
      buttonState = HIGH;
    }

    lastButtonToggleTime = millis();
    sendCommand(COMMAND_TOGGLE);
  }
  
  previousButtonReading = buttonReading;
    
  if (Serial.available()) {
    delay(10);
    
    while (Serial.available() > 0) {
      incomingByte = Serial.read();

      if (incomingByte == COMMAND_START) {
        incomingByte = Serial.read();
        switch (incomingByte) {
          case COMMAND_CLEAR:
            lcd.clear();
            break;
            
          case COMMAND_SET_CURSOR: {
            const int COLUMN = Serial.read();
            const int ROW = Serial.read();
            lcd.setCursor(COLUMN, ROW);
          } break;

         case COMMAND_SET_LED_COUNT: {
            const int COUNT = Serial.read();
            digitalWrite(LED1_PIN, COUNT >= 1 ? HIGH : LOW);
            digitalWrite(LED2_PIN, COUNT >= 2 ? HIGH : LOW);
            digitalWrite(LED3_PIN, COUNT >= 3 ? HIGH : LOW);
            digitalWrite(LED4_PIN, COUNT >= 4 ? HIGH : LOW);
            digitalWrite(LED5_PIN, COUNT >= 5 ? HIGH : LOW);
          } break;
        }

        delay(100);
      } else {
        lcd.write(incomingByte);
      }
    }
  }
}

void sendCommand(int command) {
  if (Serial.availableForWrite()) {
    Serial.write(COMMAND_START);
    Serial.write(command);
    delay(500);
  }
}

