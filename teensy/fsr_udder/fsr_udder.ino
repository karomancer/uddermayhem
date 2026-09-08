const byte FRONT_RIGHT_TEAT = A0;
const byte FRONT_LEFT_TEAT = A1;
const byte BACK_LEFT_TEAT = A2;
const byte BACK_RIGHT_TEAT = A3;

const int THRESHOLD = 900;
const int DEBOUNCE_DELAY = 50; // milliseconds

bool isFrontLeftSqueezed = false;
bool isFrontRightSqueezed = false;
bool isBackLeftSqueezed = false;
bool isBackRightSqueezed = false;

unsigned long frontLeftDebounceTime = 0;
unsigned long frontRightDebounceTime = 0;
unsigned long backLeftDebounceTime = 0;
unsigned long backRightDebounceTime = 0;

void setup() {
  pinMode(FRONT_LEFT_TEAT, INPUT);
  pinMode(FRONT_RIGHT_TEAT, INPUT);
  pinMode(BACK_LEFT_TEAT, INPUT);
  pinMode(BACK_RIGHT_TEAT, INPUT);
  Serial.begin(9600);
}

void checkForFrontLeft() {
  int value = analogRead(FRONT_LEFT_TEAT);
  if (millis() - frontLeftDebounceTime > DEBOUNCE_DELAY) {
    bool currentState = (value > THRESHOLD);
    if (currentState != isFrontLeftSqueezed) {
      isFrontLeftSqueezed = currentState;
      if (isFrontLeftSqueezed) {
        Serial.println("Front Left Teat pressed!");
        Keyboard.press(KEY_A);
      } else {
        Serial.println("Front Left Teat released!");
        Keyboard.release(KEY_A);
      }
      frontLeftDebounceTime = millis();
    }
  }
}

void checkForFrontRight() {
  int value = analogRead(FRONT_RIGHT_TEAT);
  if (millis() - frontRightDebounceTime > DEBOUNCE_DELAY) {
    bool currentState = (value > THRESHOLD);
    if (currentState != isFrontRightSqueezed) {
      isFrontRightSqueezed = currentState;
      if (isFrontRightSqueezed) {
        Serial.println("Front Right Teat pressed!");
        Keyboard.press(KEY_S);
      } else {
        Serial.println("Front Right Teat released!");
        Keyboard.release(KEY_S);
      }
      frontRightDebounceTime = millis();
    }
  }
}

void checkForBackLeft() {
  int value = analogRead(BACK_LEFT_TEAT);
  if (millis() - backLeftDebounceTime > DEBOUNCE_DELAY) {
    bool currentState = (value > THRESHOLD);
    if (currentState != isBackLeftSqueezed) {
      isBackLeftSqueezed = currentState;
      if (isBackLeftSqueezed) {
        Serial.println("Back Left Teat pressed!");
        Keyboard.press(KEY_Q);
      } else {
        Serial.println("Back Left Teat released!");
        Keyboard.release(KEY_Q);
      }
      backLeftDebounceTime = millis();
    }
  }
}

void checkForBackRight() {
  int value = analogRead(BACK_RIGHT_TEAT);
  if (millis() - backRightDebounceTime > DEBOUNCE_DELAY) {
    bool currentState = (value > THRESHOLD);
    if (currentState != isBackRightSqueezed) {
      isBackRightSqueezed = currentState;
      if (isBackRightSqueezed) {
        Serial.println("Back Right Teat pressed!");
        Keyboard.press(KEY_W);
      } else {
        Serial.println("Back Right Teat released!");
        Keyboard.release(KEY_W);
      }
      backRightDebounceTime = millis();
    }
  }
}

void loop() {
  checkForFrontLeft();
  checkForFrontRight();
  checkForBackLeft();
  checkForBackRight();
  delay(10);
}
