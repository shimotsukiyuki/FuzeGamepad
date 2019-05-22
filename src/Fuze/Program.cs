using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HidLibrary;
using ScpDriverInterface;
using System.Threading;
using System.Runtime.InteropServices;

namespace Fuze
{
    class Program
    {
        private static ScpBus global_scpBus;
        static bool ConsoleEventCallback(int eventType)
        {
            if (eventType == 2)
            {
                global_scpBus.UnplugAll();
            }
            return false;
        }
        static ConsoleEventDelegate handler;   // Keeps it from getting garbage collected
                                               // Pinvoke
        private delegate bool ConsoleEventDelegate(int eventType);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);



        public static string ByteArrayToHexString(byte[] bytes)
        {
            return string.Join(string.Empty, Array.ConvertAll(bytes, b => b.ToString("X2")));
        }



        static void Main(string[] args)
        {
            while (true)
            {
                try
                {
                    Init();
                }
                catch (Exception e)
                {
                    Log.WriteLine(e.Message,Log.LogType.exception);
                }

                Log.WriteLine("An exception occurred. Press ENTER to retry, or Alt+F4 to exit.");
                Console.ReadLine();

            }
        }

        private static void Init()
        {
            ScpBus scpBus = new ScpBus();
            scpBus.UnplugAll();
            global_scpBus = scpBus;

            handler = new ConsoleEventDelegate(ConsoleEventCallback);
            SetConsoleCtrlHandler(handler, true);

            var compatibleDevices = HidDevices.Enumerate(0x79, 0x181c).ToList();
            Thread.Sleep(400);

            FuzeGamepad[] gamepads = new FuzeGamepad[4];
            int index = 1;
            //compatibleDevices.RemoveRange(1, compatibleDevices.Count - 1);
            foreach (var deviceInstance in compatibleDevices)
            {
                //Console.WriteLine(deviceInstance);
                HidDevice Device = deviceInstance;
                try
                {
                    Device.OpenDevice(DeviceMode.Overlapped, DeviceMode.Overlapped, ShareMode.Exclusive);
                }
                catch
                {
                    Log.WriteLine("Could not open gamepad in exclusive mode. Try re-enable device.",Log.LogType.warning);
                    var instanceId = devicePathToInstanceId(deviceInstance.DevicePath);
                    if (TryReEnableDevice(instanceId))
                    {
                        try
                        {
                            Device.OpenDevice(DeviceMode.Overlapped, DeviceMode.Overlapped, ShareMode.Exclusive);
                            Log.WriteLine("Opened in exclusive mode.");
                        }
                        catch
                        {
                            Device.OpenDevice(DeviceMode.Overlapped, DeviceMode.Overlapped, ShareMode.ShareRead | ShareMode.ShareWrite);
                            Log.WriteLine("Opened in shared mode.");
                        }
                    }
                    else
                    {
                        Device.OpenDevice(DeviceMode.Overlapped, DeviceMode.Overlapped, ShareMode.ShareRead | ShareMode.ShareWrite);
                        Log.WriteLine("Opened in shared mode.");
                    }
                }

                //byte[] Vibration = { 0x20, 0x00, 0x00 };
                //if (Device.WriteFeatureData(Vibration) == false)
                //{
                //    Console.WriteLine("Could not write to gamepad (is it closed?), skipping");
                //    Device.CloseDevice();
                //    continue;
                //}

                byte[] serialNumber;
                byte[] product;
                Device.ReadSerialNumber(out serialNumber);
                Device.ReadProduct(out product);


                gamepads[index - 1] = new FuzeGamepad(Device, scpBus, index);
                ++index;

                if (index >= 5)
                {
                    break;
                }
            }

            Log.WriteLine(string.Format("{0} controllers connected", index - 1));

            while (true)
            {
                Thread.Sleep(1000);
            }
        }

        private static bool TryReEnableDevice(string deviceInstanceId)
        {
            try
            {
                bool success;
                Guid hidGuid = new Guid();
                HidLibrary.NativeMethods.HidD_GetHidGuid(ref hidGuid);
                IntPtr deviceInfoSet = HidLibrary.NativeMethods.SetupDiGetClassDevs(ref hidGuid, deviceInstanceId, 0, HidLibrary.NativeMethods.DIGCF_PRESENT | HidLibrary.NativeMethods.DIGCF_DEVICEINTERFACE);
                HidLibrary.NativeMethods.SP_DEVINFO_DATA deviceInfoData = new HidLibrary.NativeMethods.SP_DEVINFO_DATA();
                deviceInfoData.cbSize = Marshal.SizeOf(deviceInfoData);
                success = HidLibrary.NativeMethods.SetupDiEnumDeviceInfo(deviceInfoSet, 0, ref deviceInfoData);
                if (!success)
                {
                    Log.WriteLine("Error getting device info data, error code = " + Marshal.GetLastWin32Error(),
                        Log.LogType.error);
                }
                success = HidLibrary.NativeMethods.SetupDiEnumDeviceInfo(deviceInfoSet, 1, ref deviceInfoData); // Checks that we have a unique device
                if (success)
                {
                    Log.WriteLine("Can't find unique device",Log.LogType.error);
                }

                HidLibrary.NativeMethods.SP_PROPCHANGE_PARAMS propChangeParams = new HidLibrary.NativeMethods.SP_PROPCHANGE_PARAMS();
                propChangeParams.classInstallHeader.cbSize = Marshal.SizeOf(propChangeParams.classInstallHeader);
                propChangeParams.classInstallHeader.installFunction = HidLibrary.NativeMethods.DIF_PROPERTYCHANGE;
                propChangeParams.stateChange = HidLibrary.NativeMethods.DICS_DISABLE;
                propChangeParams.scope = HidLibrary.NativeMethods.DICS_FLAG_GLOBAL;
                propChangeParams.hwProfile = 0;
                success = HidLibrary.NativeMethods.SetupDiSetClassInstallParams(deviceInfoSet, ref deviceInfoData, ref propChangeParams, Marshal.SizeOf(propChangeParams));
                if (!success)
                {
                    Log.WriteLine("Error setting class install params, error code = " + Marshal.GetLastWin32Error()
                        , Log.LogType.error);
                    return false;
                }
                success = HidLibrary.NativeMethods.SetupDiCallClassInstaller(HidLibrary.NativeMethods.DIF_PROPERTYCHANGE, deviceInfoSet, ref deviceInfoData);
                if (!success)
                {
                    Log.WriteLine("Error disabling device, error code = " + Marshal.GetLastWin32Error()
                        , Log.LogType.error);
                    return false;

                }
                propChangeParams.stateChange = HidLibrary.NativeMethods.DICS_ENABLE;
                success = HidLibrary.NativeMethods.SetupDiSetClassInstallParams(deviceInfoSet, ref deviceInfoData, ref propChangeParams, Marshal.SizeOf(propChangeParams));
                if (!success)
                {
                    Log.WriteLine("Error setting class install params, error code = " + Marshal.GetLastWin32Error()
                        , Log.LogType.error);
                    return false;
                }
                success = HidLibrary.NativeMethods.SetupDiCallClassInstaller(HidLibrary.NativeMethods.DIF_PROPERTYCHANGE, deviceInfoSet, ref deviceInfoData);
                if (!success)
                {
                    Log.WriteLine("Error enabling device, error code = " + Marshal.GetLastWin32Error()
                        , Log.LogType.error);
                    return false;
                }

                HidLibrary.NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);

                return true;
            }
            catch
            {
                Log.WriteLine("Can't reenable device"
                    , Log.LogType.error);
                return false;
            }
        }

        private static string devicePathToInstanceId(string devicePath)
        {
            string deviceInstanceId = devicePath;
            deviceInstanceId = deviceInstanceId.Remove(0, deviceInstanceId.LastIndexOf('\\') + 1);
            deviceInstanceId = deviceInstanceId.Remove(deviceInstanceId.LastIndexOf('{'));
            deviceInstanceId = deviceInstanceId.Replace('#', '\\');
            if (deviceInstanceId.EndsWith("\\"))
            {
                deviceInstanceId = deviceInstanceId.Remove(deviceInstanceId.Length - 1);
            }
            return deviceInstanceId;
        }
    }
}
