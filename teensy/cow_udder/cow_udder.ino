const int FrontLeftTeat = A3;
const int FrontRightTeat = A2;
const int BackLeftTeat = A1;
const int BackRightTeat = A0;


// The larger the number, the more sensitive it is
const int FRONT_LEFT_RELEASE_THRESHOLD = 48;
const int FRONT_RIGHT_RELEASE_THRESHOLD = 250;
const int BACK_LEFT_RELEASE_THRESHOLD = 30;
const int BACK_RIGHT_RELEASE_THRESHOLD = 28;

// The larger the number, the harder you have to squeeze
const int FRONT_LEFT_PRESS_THRESHOLD = 995;
const int FRONT_RIGHT_PRESS_THRESHOLD = 1000;
const int BACK_LEFT_PRESS_THRESHOLD = 1010;
const int BACK_RIGHT_PRESS_THRESHOLD = 1013;

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
  if (frontLeftValue > FRONT_LEFT_PRESS_THRESHOLD && !isFrontLeftSqueezed) {
    Serial.println("Front Left Teat pressed!");
    Keyboard.press(KEY_A);
    isFrontLeftSqueezed = true;
  } else if (isFrontLeftSqueezed && frontLeftValue < FRONT_LEFT_RELEASE_THRESHOLD) {
    Serial.println("Front left teat released!");
    Keyboard.release(KEY_A);
    isFrontLeftSqueezed = false;
  }
}

void checkForFrontRight() {
  int frontRightValue = analogRead(FrontRightTeat);
  if (frontRightValue > FRONT_RIGHT_PRESS_THRESHOLD && !isFrontRightSqueezed) {
    Serial.println("Front Right Teat pressed!");
    Keyboard.press(KEY_S);
    isFrontRightSqueezed = true;
  } else if (isFrontRightSqueezed && frontRightValue < FRONT_RIGHT_RELEASE_THRESHOLD) {
    Serial.println("Front Right teat released!");
    Keyboard.release(KEY_S);
    isFrontRightSqueezed = false;
  }
}

void checkForBackLeft() {
  int backLeftValue = analogRead(BackLeftTeat);
  if (backLeftValue > BACK_LEFT_PRESS_THRESHOLD && !isBackLeftSqueezed) {
    Serial.println("Back Left Teat pressed!");
    Keyboard.press(KEY_Q);
    isBackLeftSqueezed = true;
  } else if (isBackLeftSqueezed && backLeftValue < BACK_LEFT_RELEASE_THRESHOLD) {
    Serial.println("Back Left Teat released!");
    Keyboard.release(KEY_Q);
    isBackLeftSqueezed = false;
  }
}

void checkForBackRight() {
  int backRightValue = analogRead(BackRightTeat);
  if (backRightValue > BACK_RIGHT_PRESS_THRESHOLD && !isBackRightSqueezed) {
    Serial.println("Back Right Teat pressed!");
    Keyboard.press(KEY_W);
    isBackRightSqueezed = true;
  } else if (isBackRightSqueezed && backRightValue < BACK_RIGHT_RELEASE_THRESHOLD) {
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