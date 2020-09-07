using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MadLedFrameworkSDK;
using AuraServiceLib;
using System.Drawing;
using System.Reflection;
using System.IO;

namespace Driver.Asus
{
    public class AsusDriver : ISimpleLEDDriver
    {
        private IAuraSdk2 _sdk;

        public IAuraSyncDeviceCollection _collection;

        public void Configure(DriverDetails driverDetails)
        {
            _sdk = (IAuraSdk2)new AuraSdk();
            _sdk.SwitchMode();
            _collection = _sdk.Enumerate(0);
        }

        public List<ControlDevice> GetDevices()
        {
            List<ControlDevice> devices = new List<ControlDevice>();
            foreach (IAuraSyncDevice device in _collection)
            {
                try
                {
                    AsusControlDevice ctrlDevice = new AsusControlDevice
                    {
                        Driver = this,
                        Name = device.Name,
                        InternalName = device.Name,
                        DeviceType = null,
                        ProductImage = GetImage(null)
                    };

                    IEnumerable<IAuraSyncDevice> e = _collection.Cast<IAuraSyncDevice>();
                    var query = e.Where(x => x.Name == ctrlDevice.InternalName);
                    ctrlDevice.device = query.First();

                    List<ControlDevice.LedUnit> leds = new List<ControlDevice.LedUnit>();

                    int ledIndex = 0;

                    foreach (IAuraRgbLight light in device.Lights)
                    {
                        leds.Add(new ControlDevice.LedUnit()
                        {
                            Data = new AsusLedData
                            {
                                LEDNumber = ledIndex,
                                AsusLedName = light.Name
                            },
                            LEDName = light.Name
                        });
                        ledIndex++;
                    }

                    ctrlDevice.LEDs = leds.ToArray();

                    switch (device.Type)
                    {
                        case 0x00010000: //Motherboard
                            ctrlDevice.DeviceType = DeviceTypes.MotherBoard;
                            ctrlDevice.ProductImage = GetImage("Motherboard");
                            ctrlDevice.Name = "Motherboard";
                            break;

                        case 0x00011000: //Motherboard LED Strip
                            ctrlDevice.DeviceType = DeviceTypes.LedStrip;
                            ctrlDevice.ProductImage = GetImage("AddressableHeader");
                            ctrlDevice.Name = device.Name.Replace("AddressableHeader", "ARGB Header");
                            break;

                        case 0x00020000: //VGA
                            ctrlDevice.DeviceType = DeviceTypes.GPU;
                            ctrlDevice.ProductImage = GetImage("GPU");
                            ctrlDevice.Name = device.Name.Replace("Vga", "GPU");
                            break;

                        case 0x00040000: //Headset
                            ctrlDevice.DeviceType = DeviceTypes.Headset;
                            ctrlDevice.ProductImage = GetImage("Headset");
                            break;

                        case 0x00070000: //DRAM
                            ctrlDevice.DeviceType = DeviceTypes.Memory;
                            ctrlDevice.ProductImage = GetImage("DRAM");
                            break;

                        case 0x00080000: //Keyboard
                            ctrlDevice.DeviceType = DeviceTypes.Keyboard;
                            ctrlDevice.ProductImage = GetImage("Keyboard");
                            break;

                        case 0x00081000: //Notebook Keyboard
                        case 0x00081001: //Notebook Keyboard(4 - zone type)
                            ctrlDevice.DeviceType = DeviceTypes.Keyboard;
                            ctrlDevice.ProductImage = GetImage("LaptopKeyboard");
                            break;

                        case 0x00090000: //Mouse
                            ctrlDevice.DeviceType = DeviceTypes.Keyboard;
                            ctrlDevice.ProductImage = GetImage("Mouse");
                            break;

                        case 0x00030000: //Display
                            ctrlDevice.DeviceType = DeviceTypes.Other;
                            ctrlDevice.ProductImage = GetImage("Monitor");
                            break;

                        case 0x000B0000: //Chassis
                            ctrlDevice.DeviceType = DeviceTypes.Other;
                            ctrlDevice.ProductImage = GetImage("Chassis");
                            break;

                        case 0x00050000: //Microphone
                            ctrlDevice.DeviceType = DeviceTypes.Other;
                            ctrlDevice.ProductImage = GetImage("Microphone");
                            break;

                        case 0x00060000: //External HDD
                            ctrlDevice.DeviceType = DeviceTypes.Other;
                            ctrlDevice.ProductImage = GetImage("HDD");
                            break;

                        case 0x000C0000: //Projector
                            ctrlDevice.DeviceType = DeviceTypes.Bulb;
                            ctrlDevice.ProductImage = GetImage("Projector");
                            break;

                        case 0x00000000: //All
                        case 0x00012000: //All - In - One PC
                        case 0x00061000: //External BD Drive
                            ctrlDevice.DeviceType = DeviceTypes.Other;
                            break;

                    }
                    devices.Add(ctrlDevice);
                }
                catch
                {

                }

            }



            return devices;
        }

        public void Dispose()
        {
            _sdk?.ReleaseControl(0);
            _sdk = null;
        }

        public T GetConfig<T>() where T : SLSConfigData
        {
            //TODO throw new NotImplementedException();
            return null;
        }

        public DriverProperties GetProperties()
        {
            return new DriverProperties
            {
                SupportsPull = false,
                SupportsPush = true,
                IsSource = false,
                Id = Guid.Parse("bcc35bad-d1ee-4303-a74f-5fa2d381e0af"),
                SupportsCustomConfig = false
            };
        }

        public string Name()
        {
            return "Asus";
        }

        public void Pull(ControlDevice controlDevice)
        {
            throw new NotImplementedException();
        }

        public static bool ApiInUse = false;
        public void Push(ControlDevice controlDevice)
        {
            if (ApiInUse) return;
            ApiInUse = true;
            AsusControlDevice asusDevice = (AsusControlDevice)controlDevice;

            for (int inc = 0; inc < controlDevice.LEDs.Length; inc++)
            {
                asusDevice.device.Lights[inc].Red = (byte)controlDevice.LEDs[inc].Color.Red;
                asusDevice.device.Lights[inc].Green = (byte)controlDevice.LEDs[inc].Color.Green;
                asusDevice.device.Lights[inc].Blue = (byte)controlDevice.LEDs[inc].Color.Blue;
            }

            asusDevice.device.Apply();
            ApiInUse = false;
        }

        public void PutConfig<T>(T config) where T : SLSConfigData
        {
            //throw new NotImplementedException();
        }
        public class AsusLedData : ControlDevice.LEDData
        {
            public string AsusLedName { get; set; }
        }

        public class AsusControlDevice : ControlDevice
        {
            public string InternalName { get; set; }

            public IAuraSyncDevice device { get; set; }
        }

        public Bitmap GetImage(string image)
        {
            Assembly myAssembly = Assembly.GetExecutingAssembly();

            try
            {
                Stream imageStream = myAssembly.GetManifestResourceStream("Driver.Asus.ProductImages." + image + ".png");
                return (Bitmap)Image.FromStream(imageStream);
            }
            catch
            {
                Stream placeholder = myAssembly.GetManifestResourceStream("Driver.Asus.AsusPlaceholder.png");
                return (Bitmap)Image.FromStream(placeholder);
            }
        }
    }
}
