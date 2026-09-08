#!/bin/bash
#
# Put the Pi into Home Mode: enable wifi + bluetooth, then reboot.
# The .desktop file for this script lives inside ~/Desktop/uddermayhem/
# (not the main desktop) so a stranger at an event can't tap it.

set -e

echo "=============================================="
echo "  Switching to HOME MODE (network ON)"
echo "=============================================="
echo
echo "Enabling wifi (NetworkManager)..."
sudo nmcli radio wifi on
echo "Enabling bluetooth (autostart on boot)..."
sudo systemctl enable --now bluetooth
sync
echo
echo "Rebooting in 3 seconds..."
sleep 3
sudo reboot
