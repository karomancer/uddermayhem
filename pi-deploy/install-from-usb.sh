#!/bin/bash
#
# Install a new Udder Mayhem build from a USB drive.
# Tapped from the "Install from USB" desktop icon.
#
# The USB drive must contain, at its root:
#   uddermayhem/UDDERMAYHEM_DEPLOY.marker
#   uddermayhem/index.html
#   uddermayhem/Build/
#
# The marker file protects against random USB sticks with a similarly-named
# folder overwriting the game. Create it during deploy prep: `touch uddermayhem/UDDERMAYHEM_DEPLOY.marker`

set -e

USB_ROOT="/media/uddermayhem"
TARGET="$HOME/Desktop/uddermayhem"
STAGING="$HOME/Desktop/.uddermayhem.new"
BACKUP="$HOME/Desktop/.uddermayhem.old"
MARKER="UDDERMAYHEM_DEPLOY.marker"

banner() {
    echo "=============================================="
    echo "  $1"
    echo "=============================================="
}

fail() {
    banner "FAILED"
    echo
    echo "$1"
    echo
    echo "This window will close in 60 seconds."
    sleep 60
    exit 1
}

banner "Udder Mayhem: Install from USB"
echo

SOURCE=""
if [ -d "$USB_ROOT" ]; then
    for mount in "$USB_ROOT"/*/; do
        [ -d "$mount" ] || continue
        candidate="${mount}uddermayhem"
        if [ -f "$candidate/$MARKER" ] && [ -f "$candidate/index.html" ]; then
            SOURCE="$candidate"
            break
        fi
    done
fi

if [ -z "$SOURCE" ]; then
    MOUNTED=$(ls -1 "$USB_ROOT" 2>/dev/null || echo "(none)")
    fail "No valid build found on any USB drive.

Expected on the USB stick:
  uddermayhem/$MARKER
  uddermayhem/index.html
  uddermayhem/Build/

Currently mounted USB drives:
$MOUNTED"
fi

echo "Found build: $SOURCE"
echo

echo "Copying files..."
rm -rf "$STAGING"
rsync -a --info=progress2 "$SOURCE/" "$STAGING/"

if [ ! -f "$STAGING/index.html" ] || [ ! -d "$STAGING/Build" ]; then
    rm -rf "$STAGING"
    fail "Copy incomplete — index.html or Build/ missing after rsync. Game NOT modified."
fi

echo
echo "Installing..."
rm -rf "$BACKUP"
if [ -d "$TARGET" ]; then
    mv "$TARGET" "$BACKUP"
fi
mv "$STAGING" "$TARGET"

# Restore the Home Mode tap-target inside the build folder. It gets wiped by
# every install (build folder is fully replaced), so we re-inject from the
# canonical copy stashed in $HOME.
if [ -f "$HOME/HomeMode.desktop" ]; then
    cp "$HOME/HomeMode.desktop" "$TARGET/HomeMode.desktop"
    chmod +x "$TARGET/HomeMode.desktop"
fi

sync

echo
banner "SUCCESS. Rebooting in 5 seconds."
echo "(Previous build kept at $BACKUP in case of rollback.)"
sleep 5
sudo reboot
