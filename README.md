# PlugHub

This repository contains a smart home automation hub application, similar to the Philips Hue Bridge, 
that can connect to and control a Hue Smart plug.

The application is intended for use with a Raspberry Pi device using the BlueZ Bluetooth stack.

## Vendor specific Bluetooth services

The Hue Smart plug was inspected using [nRF Connect](https://play.google.com/store/apps/details?id=no.nordicsemi.android.mcp&hl=en).
The device advertises a vendor specific service `932c32bd-0000-47a2-835a-a8d455b859dd`, and a power state characteristic `932c32bd-0002-47a2-835a-a8d455b859dd`.
The characteristic has the value `0x00` when the plug is turned off, and `0x01` when the plug is turned on.