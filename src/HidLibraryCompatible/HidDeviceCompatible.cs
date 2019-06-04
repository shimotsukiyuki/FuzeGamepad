using System;
using HidLibraryCompatible;

namespace HidLibrary
{
	
	public class HidDeviceData
	{
		public enum ReadStatus
		{
			Success = 0,
			WaitTimedOut = 1,
			WaitFail = 2,
			NoDataRead = 3,
			ReadError = 4,
			NotConnected = 5
		}

		public HidDeviceData(ReadStatus status)
		{
			Data = new byte[] { };
			Status = status;
		}

		public HidDeviceData(byte[] data, ReadStatus status)
		{
			Data = data;
			Status = status;
		}

		public byte[] Data { get; private set; }
		public ReadStatus Status { get; private set; }
	}
	
	public class HidDevice
	{
		readonly HidLibraryCompatible.HidDevice proxy;

		internal HidDevice(HidLibraryCompatible.HidDevice dev)
		{
			proxy = dev;
		}
		
		public static HidDevice wrapper(HidLibraryCompatible.HidDevice dev)
		{
			return new HidDevice(dev);
		}

		public IntPtr Handle { get; private set; }
		public bool IsOpen { get; private set; }
		public bool IsConnected { get { return proxy.IsConnected; } }
		public string Description { get { return proxy.Description; } }
		public HidDeviceCapabilities Capabilities { get { return proxy.Capabilities; } }
		public HidDeviceAttributes Attributes { get { return proxy.Attributes; } }
		public string DevicePath { get { return proxy.DevicePath; } }

		public override string ToString()
		{
			return proxy.ToString();
		}

		public void OpenDevice()
		{
			proxy.OpenDevice(false);
		}

		public void CloseDevice()
		{
			proxy.CloseDevice();
		}

		public HidDeviceData Read()
		{
			return Read(0);
		}

		public HidDeviceData Read(int timeout)
		{
			var buffer = new byte[proxy.Capabilities.InputReportByteLength];
			switch (proxy.ReadWithFileStream(buffer, timeout)) {
				case HidLibraryCompatible.HidDevice.ReadStatus.Success:
					return new HidDeviceData(buffer, HidDeviceData.ReadStatus.Success);
				case HidLibraryCompatible.HidDevice.ReadStatus.ReadError:
					return new HidDeviceData(HidDeviceData.ReadStatus.ReadError);
				case HidLibraryCompatible.HidDevice.ReadStatus.NoDataRead:
					return new HidDeviceData(HidDeviceData.ReadStatus.NoDataRead);
			}
			return new HidDeviceData(HidDeviceData.ReadStatus.WaitTimedOut);	
		}

		public HidReport ReadReport()
		{
			return ReadReport(0);
		}

		public HidReport ReadReport(int timeout)
		{
			return new HidReport(Capabilities.InputReportByteLength, Read(timeout));
		}

		/*
		public bool ReadFeatureData(out byte[] data, byte reportId = 0)
		{
			return false;
		}

		public bool ReadProduct(out byte[] data)
		{
			return false;
			
		}

		public bool ReadManufacturer(out byte[] data)
		{
			return false;
			
		}

		public bool ReadSerialNumber(out byte[] data)
		{
			return false;
			
		}
*/
		public bool Write(byte[] data)
		{
			return Write(data, 0);
		}

		public bool Write(byte[] data, int timeout)
		{
			// TODO
			return false;
		}

		public bool WriteReport(HidReport report)
		{
			return WriteReport(report, 0);
		}

		public bool WriteReport(HidReport report, int timeout)
		{
			return Write(report.GetBytes(), timeout);
		}


		public HidReport CreateReport()
		{
			return new HidReport(Capabilities.OutputReportByteLength);
		}

		public bool WriteFeatureData(byte[] data)
		{
			return false;
			
		}

		public void Dispose()
		{
			proxy.Dispose();
		}
	}
	
	public class HidReport
	{
		private byte _reportId;
		private byte[] _data = new byte[] { };

		private readonly HidDeviceData.ReadStatus _status;

		public HidReport(int reportSize)
		{
			Array.Resize(ref _data, reportSize - 1);
		}

		public HidReport(int reportSize, HidDeviceData deviceData)
		{
			_status = deviceData.Status;

			Array.Resize(ref _data, reportSize - 1);

			if ((deviceData.Data != null)) {

				if (deviceData.Data.Length > 0) {
					_reportId = deviceData.Data[0];
					Exists = true;

					if (deviceData.Data.Length > 1) {
						var dataLength = reportSize - 1;
						if (deviceData.Data.Length < reportSize - 1)
							dataLength = deviceData.Data.Length;
						Array.Copy(deviceData.Data, 1, _data, 0, dataLength);
					}
				} else
					Exists = false;
			} else
				Exists = false;
		}

		public bool Exists { get; private set; }
		public HidDeviceData.ReadStatus ReadStatus { get { return _status; } }

		public byte ReportId {
			get { return _reportId; }
			set {
				_reportId = value;
				Exists = true;
			}
		}

		public byte[] Data {
			get { return _data; }
			set {
				_data = value;
				Exists = true;
			}
		}

		public byte[] GetBytes()
		{
			byte[] data = null;
			Array.Resize(ref data, _data.Length + 1);
			data[0] = _reportId;
			Array.Copy(_data, 0, data, 1, _data.Length);
			return data;
		}
	}
}
