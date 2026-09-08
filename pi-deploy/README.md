# Deploying Udder Mayhem to a Raspberry Pi

Everything the Pi needs to run Udder Mayhem as a kiosk cabinet: launcher, autostart, network mode switching, and USB hotfix installer.

## Prerequisites

**Hardware:** Raspberry Pi (4 or 5 recommended), touchscreen, USB port free for hotfixes.

**OS:** Raspberry Pi OS **desktop** image (Debian 13 / trixie or later). The **Lite** headless image will NOT work — this setup depends on the desktop session and its file manager. Flash via Raspberry Pi Imager.

**During Imager setup, configure:**
- Username: `uddermayhem` (all scripts hardcode `/home/uddermayhem/...` paths)
- Enable SSH (needed once for initial setup from your laptop)
- Set a wifi password so the Pi can be reached during setup

**On first boot, from the Pi's terminal** (or SSH in), enable desktop autologin:
```
sudo raspi-config
```
Navigate to *System Options → Boot / Auto Login → Desktop Autologin*. Reboot.

**Confirm required packages are present** (they ship with the desktop image, but verify on the Pi):
```
command -v chromium python3 rsync nmcli && ls /sbin/rfkill && echo OK
```
If any are missing: `sudo apt install chromium python3 rsync network-manager rfkill`.

## First-time Pi setup

From your laptop, copy this `pi-deploy/` folder to the Pi and run the installer:

```
scp -r pi-deploy uddermayhem@<pi-address>:~/
ssh uddermayhem@<pi-address> 'bash ~/pi-deploy/install.sh'
```

Replace `<pi-address>` with the Pi's IP (find it via `hostname -I` on the Pi, or your router's client list).

That's it for cabinet tooling. The installer copies scripts to `~/`, sets up the desktop icons and Cabinet folder, and wires autostart into `~/.config/autostart/`. See [File layout on the Pi](#file-layout-on-the-pi) below for what ended up where.

**Trust the launchers.** The first tap on each `.desktop` icon on the touchscreen may prompt "Trust this launcher / Allow Launching" — accept once per icon. Icons to trust:
- `EventMode` (on the desktop)
- `InstallFromUSB` and `UdderMayhem` (inside `Cabinet/`)
- `HomeMode` (inside `uddermayhem/`, once the game is installed)

## Installing / updating the game

The game itself is deployed via USB stick. Same workflow whether it's the first install or a hotfix at Maker Faire.

**On your laptop:**

1. Build the game in Unity. Under *Player Settings → Resolution and Presentation → WebGL Template*, make sure **UdderMayhemKiosk** is selected (checked into `ProjectSettings/ProjectSettings.asset`, but verify). Build to a folder named `uddermayhem/`.

2. Add the deploy marker to the build folder. This prevents random USB sticks from accidentally overwriting the game:
   ```
   touch uddermayhem/UDDERMAYHEM_DEPLOY.marker
   ```

3. Copy the whole `uddermayhem/` folder onto a USB stick (drag in Finder, or `cp -r uddermayhem /Volumes/<usbname>/`). Eject properly.

**On the Pi:**

4. Plug the USB stick in.
5. Open the `Cabinet` folder on the desktop, tap **Install from USB**.
6. A terminal window shows progress. On success, the Pi reboots after 5 seconds and comes back with the new build.

**What the installer does:**
- Scans mounted USB drives under `/media/uddermayhem/` for a folder containing the marker file
- Copies to `~/Desktop/.uddermayhem.new/` first (staging), sanity-checks
- Atomically swaps the new build in; keeps the previous build at `~/Desktop/.uddermayhem.old/` for rollback
- Re-seeds `HomeMode.desktop` inside the new build folder from the canonical `~/HomeMode.desktop`
- Reboots

**Rollback** if the hotfix breaks the game (from SSH):
```
ssh uddermayhem@<pi> '
  rm -rf ~/Desktop/uddermayhem &&
  mv ~/Desktop/.uddermayhem.old ~/Desktop/uddermayhem &&
  sudo reboot
'
```

## Network modes: Event vs Home

The cabinet operates in one of two persistent modes. State is stored by NetworkManager (`/var/lib/NetworkManager/NetworkManager.state`) and by `systemctl enable/disable bluetooth` — both survive reboots.

- **Event Mode** — wifi + bluetooth off. Zero network exposure at public events. Autostart still works (doesn't need network). Fail-safe direction: a stranger accidentally tapping this at an event just makes things safer.
- **Home Mode** — wifi + bluetooth on. Used for dev, SSH, and USB deploys prep.

**Switching:**
- Enter Event Mode: tap **Event Mode (network OFF)** on the main desktop.
- Enter Home Mode: open the `uddermayhem/` folder on the desktop, tap **Home Mode (network ON)** inside. Deliberately not on the top-level desktop so a passerby at an event can't tap through to enable networking. Alternative if wifi is off and you have no touchscreen: plug ethernet, SSH in, run `~/home-mode.sh`.

## File layout on the Pi

After `install.sh` runs and the game is installed:

```
~/Desktop/
├── EventMode.desktop              ← tap: enter Event Mode
├── Cabinet/                       ← folder with secondary tools
│   ├── InstallFromUSB.desktop     ← tap: install a new build from USB
│   └── UdderMayhem.desktop        ← tap: manually launch game (backup for autostart)
└── uddermayhem/                   ← game files (rewritten by every USB install)
    ├── index.html + Build/ + …
    └── HomeMode.desktop           ← tap: enter Home Mode (re-seeded from ~/ after each install)

~/                                 ← hidden from desktop
├── LAUNCH.sh                      ← server + kiosk launcher
├── event-mode.sh                  ← wifi/bluetooth off + reboot
├── home-mode.sh                   ← wifi/bluetooth on + reboot
├── install-from-usb.sh            ← USB install logic
└── HomeMode.desktop               ← canonical stash, re-seeded into game folder after each install

~/.config/autostart/
└── UdderMayhem.desktop            ← runs ~/LAUNCH.sh on desktop-session start
```

## Troubleshooting

**Game doesn't launch on boot.** Check autologin is Desktop Autologin (`sudo raspi-config`). Check the autostart entry: `ls ~/.config/autostart/UdderMayhem.desktop`. Check the launcher itself: `~/LAUNCH.sh` should exit cleanly and show chromium.

**Tap on a `.desktop` icon does nothing.** First-tap trust prompt not accepted, or `chmod +x` missing. Verify perms with `ls -la ~/Desktop/EventMode.desktop` — should be `rwxr-xr-x`. `install.sh` sets this correctly; re-run it if unsure.

**Mode switch didn't persist across reboot.** Not currently possible if using `nmcli` (which the mode scripts do); if you see this, the mode script may have been edited to use `rfkill` — NetworkManager overrides rfkill on boot. Re-deploy the mode scripts via `install.sh`.

**Install from USB says "No valid build found".** Confirm `~/Desktop/uddermayhem/UDDERMAYHEM_DEPLOY.marker` exists on the USB stick and the folder is named `uddermayhem/` (lowercase). Verify the Pi mounted the drive: `ls /media/uddermayhem/`.

**Port 8080 already in use** when tapping manual launch: `pkill -f "python3 -m http.server 8080"` and try again.
