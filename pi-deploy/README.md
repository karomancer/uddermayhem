# Deploying Udder Mayhem to Raspberry Pi

## Prerequisites

- Raspberry Pi with username `uddermayhem`
- Chromium browser installed (should already be there)
- Python 3 installed (should also already be there)

## Step 1: Build the Game in Unity

1. Open the project in Unity
2. Go to **File → Build Settings**
3. Select **WebGL** as the platform (click "Switch Platform" if needed)
4. Click **Build**
5. Choose a destination folder and name it `uddermayhem`

## Step 2: Copy Files to Pi Desktop

Copy the following to the Pi's desktop (`/home/uddermayhem/Desktop/`):

```
Desktop/
├── uddermayhem/          # The WebGL build folder from Step 1
│   ├── Build/
│   ├── index.html
│   └── ...
├── LAUNCH.sh             # From this pi-deploy folder
└── UdderMayhem.desktop   # From this pi-deploy folder
```

## Step 3: Make Files Executable

On the Pi, open a terminal and run:

```bash
chmod +x ~/Desktop/LAUNCH.sh
chmod +x ~/Desktop/UdderMayhem.desktop
```

## Step 4: Allow Desktop Shortcut

Right-click `UdderMayhem.desktop` on the Pi desktop and select "Allow Launching" (if prompted).

## Running the Game

Double-click the **Udder Mayhem** icon on the desktop. This will:
1. Start a local web server on port 8080
2. Launch Chromium in kiosk mode (fullscreen, no UI)
3. When you close the browser, the server shuts down automatically

## Troubleshooting

### Game won't start
- Ensure the `uddermayhem` folder is on the Desktop
- Check that `LAUNCH.sh` is executable: `ls -la ~/Desktop/LAUNCH.sh`

### Port 8080 already in use
- Kill existing server: `pkill -f "python3 -m http.server 8080"`

### Exit kiosk mode
- Press `Alt+F4` or `Ctrl+W` to close Chromium

## Hardcoded Paths

These files have hardcoded paths and assume:
- Pi username: `uddermayhem`
- Game location: `~/Desktop/uddermayhem/`
- Launcher location: `/home/uddermayhem/Desktop/LAUNCH.sh`

If your setup differs, edit the paths in `LAUNCH.sh` and `UdderMayhem.desktop`.
