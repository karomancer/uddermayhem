const int FSR_WHITE_PIN = A0;
const int FSR_GREEN_PIN = A1;
const int FSR_YELLOW_PIN = A2;
const int FSR_ORANGE_PIN = A3;

void setup() {
  pinMode(FSR_WHITE_PIN, INPUT);
  pinMode(FSR_GREEN_PIN, INPUT);
  pinMode(FSR_YELLOW_PIN, INPUT);
  pinMode(FSR_ORANGE_PIN, INPUT);
  Serial.begin(9600);
}

void loop() {
  const int whiteValue = analogRead(FSR_WHITE_PIN);
  const int greenValue = analogRead(FSR_GREEN_PIN);
  const int yellowValue = analogRead(FSR_YELLOW_PIN);
  const int orangeValue = analogRead(FSR_ORANGE_PIN);

  // Step 1: See what values you see:
  Serial.print("White: ");
  Serial.println(whiteValue);
  Serial.print("Green: ");
  Serial.println(greenValue);
  Serial.print("Yellow: ");
  Serial.println(yellowValue);
  Serial.print("Orange: ");
  Serial.println(orangeValue);

  // Step 2: Adjust this numeric value depending on what values you see
  // if (value < 75) {
  //   Serial.println("PRESSED!");
  // }

  delay(100);
}
