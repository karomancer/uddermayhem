const int FrontRightTeat = 3;
const int FrontLeftTeat = 22;
const int BackRightTeat = 2;
const int BackLeftTeat = 23;

bool isFrontLeftSqueezed = false;
bool isFrontRightSqueezed = false;
bool isBackLeftSqueezed = false;
bool isBackRightSqueezed = false;

void setup() {
  pinMode(FrontLeftTeat, INPUT_PULLUP);
  pinMode(FrontRightTeat, INPUT_PULLUP);
  pinMode(BackLeftTeat, INPUT_PULLUP);
  pinMode(BackRightTeat, INPUT_PULLUP);
  Serial.begin(9600);
}

void checkForFrontLeft() {
  int frontLeft = !digitalRead(FrontLeftTeat);
  if (frontLeft) {
    // if (!isFrontLeftSqueezed) {
    //   Serial.println("FRONT LEFT pressed!");
    // }
    Keyboard.press(KEY_A);
    isFrontLeftSqueezed = true;
  } else if (!frontLeft && isFrontLeftSqueezed) {
    // Serial.println("FRONT LEFT released!");
    Keyboard.release(KEY_A);
    isFrontLeftSqueezed = false;
  }
}

void checkForFrontRight() {
  int frontRight = !digitalRead(FrontRightTeat);
  if (frontRight) {
    // if (!isFrontRightSqueezed) {
    //   Serial.println("FRONT RIGHT pressed!");
    // }
    Keyboard.press(KEY_S);
    isFrontRightSqueezed = true;
  } else if (!frontRight && isFrontRightSqueezed) {
    // Serial.println("FRONT RIGHT released!");
    Keyboard.release(KEY_S);
    isFrontRightSqueezed = false;
  }
}

void checkForBackLeft() {
  int backLeft = !digitalRead(BackLeftTeat);
  if (backLeft) {
    // if (!isBackLeftSqueezed) {
    //   Serial.println("BACK LEFT pressed!");
    // }
    Keyboard.press(KEY_Q);
    isBackLeftSqueezed = true;
  } else if (!backLeft && isBackLeftSqueezed) {
    // Serial.println("BACK LEFT released!");
    Keyboard.release(KEY_Q);
    isBackLeftSqueezed = false;
  }
}

void checkForBackRight() {
  int backRight = !digitalRead(BackRightTeat);
  if (backRight) {
    // if (!isBackRightSqueezed) {
    //   Serial.println("BACK RIGHT pressed!");
    // }
    Keyboard.press(KEY_W);
    isBackRightSqueezed = true;
  } else if (!backRight && isBackRightSqueezed) {
    // Serial.println("BACK RIGHT released!");
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