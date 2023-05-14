const int FrontLeftTeat = A3;
const int FrontRightTeat = A2;
const int BackLeftTeat = A1;
const int BackRightTeat = A0;

bool isFrontLeftSqueezed = false;
bool isFrontRightSqueezed = false;
bool isBackLeftSqueezed = false;
bool isBackRightSqueezed = false;

void setup() {
  pinMode(FrontLeftTeat, INPUT);
  pinMode(FrontRightTeat, INPUT);
  pinMode(BackLeftTeat, INPUT);
  pinMode(BackRightTeat, INPUT);
  Serial.begin(9600);
}

void checkForFrontLeft() {
  int frontLeftValue = analogRead(FrontLeftTeat);
    // Serial.println(frontLeftValue);
  if (frontLeftValue > 995 && !isFrontLeftSqueezed) {
    Serial.println("Front Left Teat pressed!");
    Keyboard.press(KEY_A);
    isFrontLeftSqueezed = true;
  } else if (isFrontLeftSqueezed && frontLeftValue < 400) {
    Serial.println("Front left teat released!");
    Keyboard.release(KEY_A);
    isFrontLeftSqueezed = false;
  }
}

void checkForFrontRight() {
  int frontRightValue = analogRead(FrontRightTeat);
  if (frontRightValue > 1025 && !isFrontRightSqueezed) {
    Serial.println("Front Right Teat pressed!");
    Keyboard.press(KEY_S);
    isFrontRightSqueezed = true;
  } else if (isFrontRightSqueezed && frontRightValue < 400) {
    Serial.println("Front Right teat released!");
    Keyboard.release(KEY_S);
    isFrontRightSqueezed = false;
  }
}

void checkForBackLeft() {
  int backLeftValue = analogRead(BackLeftTeat);
  if (backLeftValue > 1015 && !isBackLeftSqueezed) {
    Serial.println("Back Left Teat pressed!");
    Keyboard.press(KEY_Q);
    isBackLeftSqueezed = true;
  } else if (isBackLeftSqueezed && backLeftValue < 400) {
    Serial.println("Back Left Teat released!");
    Keyboard.release(KEY_Q);
    isBackLeftSqueezed = false;
  }
}

void checkForBackRight() {
  int backRightValue = analogRead(BackRightTeat);
  if (backRightValue > 1020 && !isBackRightSqueezed) {
    Serial.println("Back Right Teat pressed!");
    Keyboard.press(KEY_W);
    isBackRightSqueezed = true;
  } else if (isBackRightSqueezed && backRightValue < 400) {
    Serial.println("Back Right teat released!");
    Keyboard.release(KEY_W);
    isBackRightSqueezed = false;
  }
}
 
void loop() {
  checkForFrontLeft();
  checkForFrontRight();
  checkForBackLeft();
  checkForBackRight();
  delay(10);
}