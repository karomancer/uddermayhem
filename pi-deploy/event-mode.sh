#!/bin/bash
#
# Put the Pi into Event Mode: disable wifi + bluetooth, then reboot.
#
# Persistence mechanisms (both survive reboot on their own):
#   - wifi: NetworkManager remembers `radio wifi off` in
#     /var/lib/NetworkManager/NetworkManager.state
#   - bluetooth: systemctl disable prevents the bluetooth service from
#     autostarting on next boot
#
# We do NOT use `rfkill` — NetworkManager overrides rfkill on startup,
# so `rfkill block` does not survive a reboot.

set -e

echo "=============================================="
echo "  Switching to EVENT MODE (network OFF)"
echo "=============================================="
echo
echo "Disabling wifi (NetworkManager)..."
sudo nmcli radio wifi off
echo "Disabling bluetooth (will not autostart)..."
sudo systemctl disable --now bluetooth
sync
echo
echo "Rebooting in 3 seconds..."
sleep 3
sudo reboot
