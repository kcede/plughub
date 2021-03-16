using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using HashtagChris.DotNetBlueZ;
using HashtagChris.DotNetBlueZ.Extensions;

namespace PlugHub
{
    public interface IPhilipsHuePlugService
    {
        Task Toggle();
    }

    public class PhilipsHuePlugService : IPhilipsHuePlugService
    {
        static TimeSpan timeout = TimeSpan.FromSeconds(15);

        private GattCharacteristic powerStateCharacteristic = null;

        public async Task Toggle()
        {
            await HandleToggle();
        }

        private async Task HandleToggle(int retryCount = 1) {
            var characteristic = await GetPowerStateCharacteristic();
            if (characteristic != null)
            {
                try
                {
                    Console.WriteLine("Reading current power state...");
                    var bytes = await characteristic.ReadValueAsync(timeout);
                    var poweredOn = false;
                    if (bytes.Length == 1)
                    {
                        poweredOn = bytes[0] == 1;
                        Console.WriteLine($"Powered on: {poweredOn}");
                        Console.WriteLine("Toggling powered on state...");
                        await characteristic.WriteValueAsync(new byte[] { (byte)(poweredOn ? 0 : 1) }, new Dictionary<string, object>());
                    }
                }
                catch (Tmds.DBus.DBusException ex)
                {
                    Console.WriteLine($"D-Bus exception while attempting to toggle power state: {ex.Message}");
                    if (retryCount > 3) {
                        throw;
                    } else {
                        Console.WriteLine("Retrying...");
                        powerStateCharacteristic = null;
                        await HandleToggle(retryCount + 1);
                    }
                }
                
            }
        }

        private async Task<GattCharacteristic> GetPowerStateCharacteristic()
        {
            if (powerStateCharacteristic == null)
            {
                IAdapter1 adapter;
                var adapters = await BlueZManager.GetAdaptersAsync();
                if (adapters.Count == 0)
                {
                    throw new Exception("No Bluetooth adapters found.");
                }

                adapter = adapters.First();

                var adapterPath = adapter.ObjectPath.ToString();
                var adapterName = adapterPath.Substring(adapterPath.LastIndexOf("/") + 1);
                Console.WriteLine($"Using Bluetooth adapter {adapterName}");

                var devices = await adapter.GetDevicesAsync();
                string deviceAddress = "";
                foreach (var d in devices)
                {
                    var name = await d.GetNameAsync();
                    if (name.ToLower().Contains("hue") || name.ToLower().Contains("plug")) {
                        deviceAddress = await d.GetAddressAsync();
                        break;
                    } else {
                        continue;
                    }
                }

                if (deviceAddress == "") {
                    Console.WriteLine("No matching devices found.");
                    return null;
                }

                var device = await adapter.GetDeviceAsync(deviceAddress);
                if (device == null)
                {
                    Console.WriteLine($"Bluetooth peripheral with address '{deviceAddress}' not found. Use `bluetoothctl` or Bluetooth Manager to scan and possibly pair first.");
                    return null;
                }

                Console.WriteLine("Connecting...");
                await device.ConnectAsync();
                await device.WaitForPropertyValueAsync("Connected", value: true, timeout);
                Console.WriteLine("Connected.");

                device.Disconnected += OnDisconnected;

                Console.WriteLine("Waiting for services to resolve...");
                await device.WaitForPropertyValueAsync("ServicesResolved", value: true, timeout);

                var servicesUUID = await device.GetUUIDsAsync();
                Console.WriteLine($"Device offers {servicesUUID.Length} service(s).");

                var deviceInfoServiceFound = servicesUUID.Any(uuid => String.Equals(uuid, GattConstants.DeviceInformationServiceUUID, StringComparison.OrdinalIgnoreCase));
                if (!deviceInfoServiceFound)
                {
                    Console.WriteLine("Device doesn't have the Device Information Service. Try pairing first?");
                    return null;
                }

                var service = await device.GetServiceAsync("932c32bd-0000-47a2-835a-a8d455b859dd");
                powerStateCharacteristic = await service.GetCharacteristicAsync("932C32BD-0002-47A2-835A-A8D455B859DD");
                return powerStateCharacteristic;
            }
            else
            {
                return powerStateCharacteristic;
            }
        }

        private async Task OnDisconnected(Device sender, BlueZEventArgs eventArgs) {
            Console.WriteLine($"{await sender.GetNameAsync()} has disconnected. Will attempt reconnect on next attempt.");
            powerStateCharacteristic = null;
        }
    }
}
