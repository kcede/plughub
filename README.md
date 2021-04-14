# PlugHub

This repository contains a smart home automation hub application, similar to the Philips Hue Bridge, 
that can connect to and control a Hue Smart plug.

The application is intended for use with a Raspberry Pi device using the BlueZ Bluetooth stack.

## Motivation

Using the native [Philips Hue Bluetooth app](https://www.philips-hue.com/en-us/explore-hue/apps/bluetooth)
is okay, but it feels too slow. You need to open the app, wait for it to connect, and then toggle the light. 
I would like to be able to press a button (preferably a physical one) and the light would turn on. 
Having the lamp plugged in through the smart plug also means you can't really manipulate the light directly 
if you still want the smart plug to do its thing.

Having a Raspberry Pi always connected to the smart plug, and always ready to perform a command using 
an API means I can toggle the light by pressing a button on my keyboard or use an NFC tag reader app.

## Vendor specific Bluetooth services

The Hue Smart plug was inspected using [nRF Connect](https://play.google.com/store/apps/details?id=no.nordicsemi.android.mcp&hl=en).
The device advertises a vendor specific service `932c32bd-0000-47a2-835a-a8d455b859dd`, and a power state characteristic `932c32bd-0002-47a2-835a-a8d455b859dd`.
The characteristic has the value `0x00` when the plug is turned off, and `0x01` when the plug is turned on.

## Quirks

If you previously connected to and paired with the Hue Smart plug, reset it using the [Hue Bluetooth app](https://www.philips-hue.com/en-us/explore-hue/apps/bluetooth) before attempting to connect to it using a Raspberry Pi.
Otherwise you will see authorization errors, since the plug can only be paired to one device.

You might notice the plug getting disconnected immediately or after being connected for a short while, even if it was functioning normally before. If this happens, check the supervision timeout configuration parameter in `/sys/kernel/debug/bluetooth/hci0/supervision_timeout`. This is set to `42` (420 ms) by default, but should be increased to `72` (720 ms) or even `2000` (20 s). Based on [this article](https://blog.classycode.com/a-short-story-about-android-ble-connection-timeouts-and-gatt-internal-errors-fa89e3f6a456).

## Deploying

This section assumes the repository is cloned locally on the Raspberry Pi.

First, publish the app using the `dotnet` CLI:

```shell
dotnet publish -r linux-arm -c Release
```

Then, create a systemd service `/etc/systemd/system/plughub.service` with the following contents:

```
[Unit]
Description=PlugHub service
After=network.target
StartLimitIntervalSec=1

[Service]
Type=simple
Restart=always
RestartSec=1
User=pi
ExecStart=/home/pi/src/plughub/bin/Release/netcoreapp3.1/linux-arm/publish/PlugHub

[Install]
WantedBy=multi-user.target
```

The `ExecStart` directive should point to the location of the published executable.

Start the service and enable automatically starting on boot:

```shell
systemctl start plughub
systemctl enable plughub
```