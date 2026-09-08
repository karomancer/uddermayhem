#!/bin/bash
#
# One-command installer for the Udder Mayhem cabinet setup on a fresh Pi.
#
# Run this on the Pi (via SSH from another machine, or locally in a terminal)
# after copying the pi-deploy/ folder to the Pi. Idempotent — safe to re-run.
#
# What it does:
#   - Copies helper scripts (.sh) to $HOME (hidden from desktop)
#   - Copies the canonical HomeMode.desktop to $HOME (stash used by installer)
#   - Puts EventMode.desktop on the main desktop (fail-safe tap-target)
#   - Creates ~/Desktop/Cabinet/ with secondary tools (Install from USB, manual launch)
#   - Wires up autostart in ~/.config/autostart/
#   - Seeds HomeMode.desktop inside the game folder if the game is already installed
#   - Marks everything executable
#
# What it does NOT do:
#   - Install the game itself (that's a separate USB-install or scp step)
#   - Install system packages (chromium, python3, rfkill, nmcli — all expected on Pi OS desktop image)
#   - Set desktop autologin (do this once via `sudo raspi-config`)

set -e

SRC=$(cd "$(dirname "$0")" && pwd)
DESKTOP="$HOME/Desktop"
CABINET="$DESKTOP/Cabinet"
AUTOSTART="$HOME/.config/autostart"

echo "Installing cabinet tooling from $SRC to $HOME ..."

mkdir -p "$CABINET" "$AUTOSTART"

# Helper scripts — kept in $HOME so they don't clutter the desktop.
install -m 755 "$SRC/LAUNCH.sh"            "$HOME/LAUNCH.sh"
install -m 755 "$SRC/event-mode.sh"        "$HOME/event-mode.sh"
install -m 755 "$SRC/home-mode.sh"         "$HOME/home-mode.sh"
install -m 755 "$SRC/install-from-usb.sh"  "$HOME/install-from-usb.sh"

# Canonical stash of HomeMode.desktop — install-from-usb.sh restores this
# into the game folder after every deploy.
install -m 755 "$SRC/HomeMode.desktop" "$HOME/HomeMode.desktop"

# Main desktop: only the fail-safe Event Mode icon.
install -m 755 "$SRC/EventMode.desktop" "$DESKTOP/EventMode.desktop"

# Cabinet: secondary tools.
install -m 755 "$SRC/InstallFromUSB.desktop" "$CABINET/InstallFromUSB.desktop"
install -m 755 "$SRC/UdderMayhem.desktop"    "$CABINET/UdderMayhem.desktop"

# Autostart on desktop-session start.
install -m 644 "$SRC/UdderMayhem-autostart.desktop" "$AUTOSTART/UdderMayhem.desktop"

# If the game folder exists, seed HomeMode.desktop inside it now.
# (install-from-usb.sh will keep re-seeding after every future USB install.)
if [ -d "$DESKTOP/uddermayhem" ]; then
    install -m 755 "$SRC/HomeMode.desktop" "$DESKTOP/uddermayhem/HomeMode.desktop"
fi

echo
echo "Installed. Layout:"
echo "  ~/Desktop/EventMode.desktop           tap once to enter Event Mode"
echo "  ~/Desktop/Cabinet/                    folder with secondary tools"
echo "    InstallFromUSB.desktop              tap to install a new build from USB"
echo "    UdderMayhem.desktop                 tap to manually launch (backup)"
echo "  ~/Desktop/uddermayhem/HomeMode.desktop  (only if game is installed)"
echo "  ~/{LAUNCH,event-mode,home-mode,install-from-usb}.sh  helper scripts"
echo "  ~/.config/autostart/UdderMayhem.desktop  auto-launches game on boot"
echo
echo "Next steps:"
echo "  1. If the game isn't yet installed, plug in a USB with a build + marker file and tap Install from USB."
echo "  2. First tap of each .desktop icon may show a 'trust this launcher' prompt — accept it."
echo "  3. Set desktop autologin if not already: sudo raspi-config → System Options → Boot / Auto Login → Desktop Autologin."
