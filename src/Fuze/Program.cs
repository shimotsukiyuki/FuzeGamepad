using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HidLibraryCompatible;
using HidLibrary;
using ScpDriverInterface;
using System.Threading;
using System.Runtime.InteropServices;

namespace Fuze
{
	class Program
	{
		static readonly int[] VIDs = { 0x0079, 0x12D1 };
		static readonly int[] PIDs = { 0x181c, 0xA560 };
		const bool isExclusiveMode = false;
		
		static ScpBus global_scpBus;
		static bool ConsoleEventCallback(int eventType)
		{
			if (eventType == 2) {
				global_scpBus.UnplugAll();
			}
			return false;
		}
		static ConsoleEventDelegate handler;
		// Keeps it from getting garbage collected
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
			while (true) {
				try {
					Init();
				} catch (Exception e) {
					Log.WriteLine(e.Message, Log.LogType.exception);
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

			var compatibleDevices = HidDevices.Enumerate().ToList();
			Thread.Sleep(400);

			FuzeGamepad[] gamepads = new FuzeGamepad[4];
			int index = 1;
			
			foreach (var deviceInstance in compatibleDevices) {
				if (!((IList<int>)VIDs).Contains(deviceInstance.Attributes.VendorId)
				    || !((IList<int>)PIDs).Contains(deviceInstance.Attributes.ProductId)) {
					continue;
				}
				Console.WriteLine(deviceInstance);
				HidLibraryCompatible.HidDevice Device = deviceInstance;
				if (!Device.IsOpen) {
					try {
						Device.OpenDevice(isExclusiveMode);
					} catch (Exception e) {
						Log.WriteLine(string.Format("Open Hid device error\n{0}", e), Log.LogType.error);
					}
				}
				gamepads[index - 1] = new FuzeGamepad(HidLibrary.HidDevice.wrapper(Device), scpBus, index);
				if (++index >= 5) {
					break;
				}
			}

			Log.WriteLine(string.Format("{0} controllers connected", index - 1));

			while (true) {
				Thread.Sleep(1000);
			}
		}
		

	}
}
