#!/bin/bash

pkill -f "python3 -m http.server 8080"

GAME_DIR="$HOME/Desktop/uddermayhem"
cd "$GAME_DIR"
python3 -m http.server 8080 &
SERVER_PID=$!

sleep 1
chromium --kiosk --noerrdialogs "http://localhost:8080/?v=$(date +%s)"

# When Chromium closes, kill the server
kill $SERVER_PID
